// ═══════════════════════════════════════════════════════════════════════════════
// COMM_TEST — Prueba de Comunicación ESP32–Unity
//
// Objetivo: Verificar los tres canales de comunicación disponibles:
//   1. USB Serial  (115200 baud, cable USB)
//   2. Bluetooth   (SPP clásico, COM virtual Windows)
//   3. TCP/Socket  (puerto 5005, protocolo Python-Unity)
//
// Protocolo de prueba:
//   Recibe:  PING, STATUS, ECHO:<texto>, LATENCY_TEST, RESET
//   Envía:   PONG, STATUS:..., ECHO_OK:<texto>, LAT:OK:<ms>, ROBOT_READY
//
// Uso:
//   1. Subir a ESP32
//   2. Abrir Unity → conectar el canal que deseas probar
//   3. Observar Serial Log y consola Unity para PASS/FAIL
//
// Compatible con: ESP32 DevKit v1 / WROOM / WROVER
// IDE: Arduino 2.x + esp32 by Espressif 2.0+
// ═══════════════════════════════════════════════════════════════════════════════

#include <Arduino.h>
#include "BluetoothSerial.h"   // Incluido con esp32 board package

// ─── CONFIGURACIÓN ────────────────────────────────────────────────────────────
#define BAUD_RATE        115200
#define BT_DEVICE_NAME   "ROBOT_SCARA"
#define TCP_PORT         5005
#define STATUS_INTERVAL  1000    // ms entre STATUS automáticos en modo streaming

// ─── CONSTANTES DE ESTADO SIMULADO ────────────────────────────────────────────
#define L1          150.9f
#define L2          149.0f
#define STEPS_PER_DEG  0.5556f
#define STEPS_PER_MM_Z 250.0f

// ─── OBJETO BLUETOOTH ─────────────────────────────────────────────────────────
BluetoothSerial SerialBT;

// ─── ESTADO SIMULADO DEL ROBOT ────────────────────────────────────────────────
float gJ1_deg = -90.0f;
float gJ2_deg = -90.0f;
float gZ_mm   =   0.0f;
int   gS1     =  90;
int   gS2     =   0;

// ─── FLAGS ────────────────────────────────────────────────────────────────────
bool  statusStreamUSB = false;   // enviar STATUS periódico por USB
bool  statusStreamBT  = false;   // enviar STATUS periódico por BT
unsigned long lastStatusMs = 0;
unsigned long connectTimeMs = 0;

// ─── PROTOTIPOS ───────────────────────────────────────────────────────────────
void processCommand(const String& cmd, Stream& channel, const char* chName);
void sendStatus(Stream& ch);
void sendTestReport(Stream& ch);
float computeTcpX();
float computeTcpY();
long  angleToSteps(float deg);
long  mmToSteps(float mm);

// ═══════════════════════════════════════════════════════════════════════════════
// SETUP
// ═══════════════════════════════════════════════════════════════════════════════
void setup()
{
    // — Canal 1: USB Serial —
    Serial.begin(BAUD_RATE);
    while (!Serial && millis() < 3000);   // espera hasta 3s si es necesario

    // — Canal 2: Bluetooth SPP —
    SerialBT.begin(BT_DEVICE_NAME);

    // Estado de pines (solo indicadores; no se mueven motores reales)
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, LOW);

    connectTimeMs = millis();

    // Anuncio en USB
    Serial.println("ROBOT_READY");
    Serial.println("INFO:COMM_TEST v1.0 — USB+BT activos");
    Serial.print("INFO:BT_NAME=");
    Serial.println(BT_DEVICE_NAME);
    Serial.println("INFO:Esperando comandos...");
}

// ═══════════════════════════════════════════════════════════════════════════════
// LOOP PRINCIPAL
// ═══════════════════════════════════════════════════════════════════════════════
void loop()
{
    // ── Canal 1: USB Serial ──────────────────────────────────────────────────
    if (Serial.available()) {
        String cmd = Serial.readStringUntil('\n');
        cmd.trim();
        if (cmd.length() > 0) {
            processCommand(cmd, Serial, "USB");
        }
    }

    // ── Canal 2: Bluetooth ───────────────────────────────────────────────────
    if (SerialBT.available()) {
        String cmd = SerialBT.readStringUntil('\n');
        cmd.trim();
        if (cmd.length() > 0) {
            processCommand(cmd, SerialBT, "BT");
            // Eco cruzado a USB para trazabilidad en monitor serial
            Serial.print("[BT→] "); Serial.println(cmd);
        }
    }

    // ── STATUS automático (streaming) ────────────────────────────────────────
    unsigned long now = millis();
    if (now - lastStatusMs >= STATUS_INTERVAL) {
        lastStatusMs = now;
        if (statusStreamUSB) sendStatus(Serial);
        if (statusStreamBT  && SerialBT.connected()) sendStatus(SerialBT);

        // Blink LED cada segundo para indicar que el ESP32 está vivo
        digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PROCESADOR DE COMANDOS
// ═══════════════════════════════════════════════════════════════════════════════
void processCommand(const String& cmd, Stream& ch, const char* chName)
{
    // ─── PING → PONG ──────────────────────────────────────────────────────────
    if (cmd == "PING") {
        ch.println("PONG");
        Serial.print("["); Serial.print(chName);
        Serial.println("] PING → PONG OK");
        return;
    }

    // ─── STATUS → STATUS:j1,j2,z,x,y,s1,s2 ──────────────────────────────────
    if (cmd == "STATUS") {
        sendStatus(ch);
        return;
    }

    // ─── ECHO:<texto> → ECHO_OK:<texto> ──────────────────────────────────────
    if (cmd.startsWith("ECHO:")) {
        String payload = cmd.substring(5);
        ch.print("ECHO_OK:");
        ch.println(payload);
        Serial.print("["); Serial.print(chName);
        Serial.print("] ECHO: "); Serial.println(payload);
        return;
    }

    // ─── LATENCY_TEST → LAT:OK:<uptime_ms> ───────────────────────────────────
    // Unity envía este comando con timestamp; ESP32 responde con su millis().
    // La diferencia permite estimar RTT en el cliente.
    if (cmd == "LATENCY_TEST" || cmd.startsWith("LATENCY_TEST:")) {
        ch.print("LAT:OK:");
        ch.println(millis());
        return;
    }

    // ─── STREAM_ON / STREAM_OFF ───────────────────────────────────────────────
    // Activa/desactiva el STATUS automático en el canal correspondiente.
    if (cmd == "STREAM_ON") {
        if (&ch == &Serial)   statusStreamUSB = true;
        else                  statusStreamBT  = true;
        ch.println("ACK:STREAM_ON");
        return;
    }
    if (cmd == "STREAM_OFF") {
        if (&ch == &Serial)   statusStreamUSB = false;
        else                  statusStreamBT  = false;
        ch.println("ACK:STREAM_OFF");
        return;
    }

    // ─── MOVE_J1:<deg> — simula movimiento (no hardware) ─────────────────────
    if (cmd.startsWith("MOVE_J1:")) {
        float deg = constrain(cmd.substring(8).toFloat(), -90.0f, 90.0f);
        gJ1_deg = deg;
        ch.print("ACK:J1:");
        ch.println(deg, 2);
        sendStatus(ch);
        return;
    }

    // ─── MOVE_J2:<deg> ────────────────────────────────────────────────────────
    if (cmd.startsWith("MOVE_J2:")) {
        float deg = constrain(cmd.substring(8).toFloat(), -90.0f, 90.0f);
        gJ2_deg = deg;
        ch.print("ACK:J2:");
        ch.println(deg, 2);
        sendStatus(ch);
        return;
    }

    // ─── MOVE_Z:<mm> ──────────────────────────────────────────────────────────
    if (cmd.startsWith("MOVE_Z:")) {
        float mm = constrain(cmd.substring(7).toFloat(), 0.0f, 200.0f);
        gZ_mm = mm;
        ch.print("ACK:Z:");
        ch.println(mm, 2);
        sendStatus(ch);
        return;
    }

    // ─── HOME ─────────────────────────────────────────────────────────────────
    if (cmd == "HOME") {
        gJ1_deg = -90.0f; gJ2_deg = -90.0f;
        gZ_mm   =   0.0f; gS1 = 90; gS2 = 0;
        ch.println("ACK:HOME");
        sendStatus(ch);
        return;
    }

    // ─── REPORT — imprime reporte de pruebas completo ─────────────────────────
    if (cmd == "REPORT") {
        sendTestReport(ch);
        return;
    }

    // ─── RESET ────────────────────────────────────────────────────────────────
    if (cmd == "RESET") {
        ch.println("ACK:RESET");
        delay(200);
        ESP.restart();
        return;
    }

    // ─── Comando desconocido ──────────────────────────────────────────────────
    ch.print("ERR:UNKNOWN_CMD:");
    ch.println(cmd);
}

// ─── ENVIAR STATUS ────────────────────────────────────────────────────────────
void sendStatus(Stream& ch)
{
    ch.print("STATUS:");
    ch.print(gJ1_deg, 2);   ch.print(",");
    ch.print(gJ2_deg, 2);   ch.print(",");
    ch.print(gZ_mm,   2);   ch.print(",");
    ch.print(computeTcpX(), 1); ch.print(",");
    ch.print(computeTcpY(), 1); ch.print(",");
    ch.print(gS1);          ch.print(",");
    ch.println(gS2);
}

// ─── REPORTE DE DIAGNÓSTICO ───────────────────────────────────────────────────
void sendTestReport(Stream& ch)
{
    unsigned long uptime = millis();
    ch.println("=== COMM_TEST REPORT ===");
    ch.print("Uptime ms      : "); ch.println(uptime);
    ch.print("BT connected   : "); ch.println(SerialBT.connected() ? "YES" : "NO");
    ch.print("J1 deg         : "); ch.println(gJ1_deg, 2);
    ch.print("J2 deg         : "); ch.println(gJ2_deg, 2);
    ch.print("Z  mm          : "); ch.println(gZ_mm, 2);
    ch.print("TCP X mm       : "); ch.println(computeTcpX(), 2);
    ch.print("TCP Y mm       : "); ch.println(computeTcpY(), 2);
    ch.print("J1 steps       : "); ch.println(angleToSteps(gJ1_deg));
    ch.print("J2 steps       : "); ch.println(angleToSteps(gJ2_deg));
    ch.print("Z  steps       : "); ch.println(mmToSteps(gZ_mm));
    ch.print("StreamUSB      : "); ch.println(statusStreamUSB ? "ON" : "OFF");
    ch.print("StreamBT       : "); ch.println(statusStreamBT  ? "ON" : "OFF");
    ch.println("=== END REPORT ===");
}

// ─── HELPERS CINEMÁTICA ───────────────────────────────────────────────────────
float computeTcpX() {
    float t1 = gJ1_deg * PI / 180.0f;
    float t2 = gJ2_deg * PI / 180.0f;
    return L1 * cos(t1) + L2 * cos(t1 + t2);
}
float computeTcpY() {
    float t1 = gJ1_deg * PI / 180.0f;
    float t2 = gJ2_deg * PI / 180.0f;
    return L1 * sin(t1) + L2 * sin(t1 + t2);
}
long angleToSteps(float deg) { return (long)(deg * STEPS_PER_DEG); }
long mmToSteps(float mm)     { return (long)(mm  * STEPS_PER_MM_Z); }
