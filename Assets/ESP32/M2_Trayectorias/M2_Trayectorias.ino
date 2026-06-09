/*
 * ================================================================
 *  ENTORNO 2 — TRAYECTORIAS (Pick and Place)
 *  Robot SCARA · Demostración para defensa académica
 * ================================================================
 *  ¿Qué hace este sketch?
 *  Ejecuta una secuencia de pick-and-place entre dos posiciones.
 *  Los valores de pasos por posición fueron obtenidos del simulador
 *  Unity (Entorno 2 → Guardar Punto → Panel de trayectorias).
 *  Flujo de trabajo (cómo se diseñó esta trayectoria):
 *    1. Se movió el robot virtual con las teclas (Q/E, A/D, Z/X)
 *    2. Se presionó "Guardar punto" en cada posición deseada
 *    3. El panel mostró los pasos exactos: M1:xxx M2:xxx M3:xxx
 *    4. Esos valores se copiaron aquí como constantes
 *  Secuencia de movimiento:
 *    HOME → SUBIR Z → POSICIÓN A → BAJAR → PICK →
 *    SUBIR → POSICIÓN B → BAJAR → PLACE → SUBIR → HOME
 *  ¿Cómo se ve en Unity?
 *    · PanelTrayectorias: fase actual + lista de puntos con pasos
 *    · El robot ejecuta la misma secuencia sin colisiones (sube Z primero)
 *    · Consola: eventos T: con pasos/ángulo en tiempo real

 *  Para robot FÍSICO:
 *    Reemplazar  digitalWriteScara() → digitalWrite()
 *    Reemplazar  delayMicrosecondsScara() → delayMicroseconds()
 *    Eliminar    #include "SCARA.h" y SCARA.begin()
 */
#include "SCARA.h"
// ── PINES ────────────────────────────────────────────────────────
#define DIR_Z        2
#define STEP_Z      15
#define DIR_J1      18
#define STEP_J1     19
#define DIR_J2       4
#define STEP_J2     16
#define EN_PIN       5
#define PIN_S1      35   // GiroGarra (servo)
#define PIN_S2      34   // Gripper   (servo)
// ── PARÁMETROS ───────────────────────────────────────────────────
const int DELAY_J1J2 = 3000;   // µs brazos
const int DELAY_Z    = 2000;   // µs eje Z
const int PAUSA_MS   = 800;    // ms entre fases
// ================================================================
//  PUNTOS DE TRAYECTORIA — Obtenidos del simulador Unity
//  Panel Entorno 2 → Trayectorias → Lista de puntos guardados
//  Formato: M1 = pasos Z | M2 = pasos J1 | M3 = pasos J2
//  Con ratio 1:1: 100 pasos J1/J2 = 180°  |  3000 pasos Z = 200mm
// ================================================================
// PUNTO HOME — posición inicial (robot recogido)
const int HOME_Z  = 0;    // Z en posición superior
const int HOME_J1 = 0;    // Brazo 1 en home (-90°)
const int HOME_J2 = 0;    // Brazo 2 en home (-90°)
// PUNTO A — posición de recogida (objeto 1)
// Obtenido del simulador: P01  M1:1500  M2:50  M3:40
const int A_Z     = 1500; // Z baja a media altura
const int A_J1    = 50;   // Brazo 1 gira 90°
const int A_J2    = 40;   // Brazo 2 gira 72°
// PUNTO B — posición de depósito (destino)
// Obtenido del simulador: P02  M1:1500  M2:100  M3:70
const int B_Z     = 1500;
const int B_J1    = 100;  // Brazo 1 a +90°
const int B_J2    = 70;   // Brazo 2 extendido
// Altura de transporte (Z alto para evitar colisiones al rotar)
const int ALTURA_TRANSPORTE = 500;
// ================================================================
//  VARIABLES DE ESTADO (seguimiento de posición)
// ================================================================
int pos_Z  = 0;
int pos_J1 = 0;
int pos_J2 = 0;
// ================================================================
//  HELPERS
// ================================================================
void moverEje(int pinDir, int pinStep, int &posActual, int destino, int delayUs) {
    if (destino == posActual) return;
    int pasos = abs(destino - posActual);
    bool dirPos = (destino > posActual);
    digitalWriteScara(pinDir, dirPos ? HIGH : LOW);
    for (int i = 0; i < pasos; i++) {
        digitalWriteScara(pinStep, HIGH);
        delayMicrosecondsScara(delayUs);
        digitalWriteScara(pinStep, LOW);
        delayMicrosecondsScara(delayUs);
    }
    posActual = destino;
}
// Movimiento seguro: sube Z primero, luego rota, luego baja Z
void irAPunto(int destZ, int destJ1, int destJ2) {
    // 1. Subir Z a transporte (evita colisiones al rotar)
    moverEje(DIR_Z,  STEP_Z,  pos_Z,  ALTURA_TRANSPORTE, DELAY_Z);
    delay(PAUSA_MS / 2);
    // 2. Rotar brazos a destino
    moverEje(DIR_J1, STEP_J1, pos_J1, destJ1, DELAY_J1J2);
    delay(PAUSA_MS / 2);
    moverEje(DIR_J2, STEP_J2, pos_J2, destJ2, DELAY_J1J2);
    delay(PAUSA_MS / 2);
    // 3. Bajar Z al destino final
    moverEje(DIR_Z,  STEP_Z,  pos_Z,  destZ, DELAY_Z);
    delay(PAUSA_MS);
}
// ================================================================
//  SETUP
// ================================================================
void setup() {
    SCARA.begin(115200);
    pinMode(EN_PIN,  OUTPUT);
    pinMode(DIR_J1,  OUTPUT);  pinMode(STEP_J1, OUTPUT);
    pinMode(DIR_J2,  OUTPUT);  pinMode(STEP_J2, OUTPUT);
    pinMode(DIR_Z,   OUTPUT);  pinMode(STEP_Z,  OUTPUT);
    pinMode(PIN_S1,  OUTPUT);
    pinMode(PIN_S2,  OUTPUT);
    digitalWriteScara(EN_PIN, LOW);
    pos_Z = HOME_Z;  pos_J1 = HOME_J1;  pos_J2 = HOME_J2;
    SCARA.sendLog("M2 Trayectorias listo");
}
// ================================================================
//  LOOP — PICK AND PLACE
// ================================================================
void loop() {
    delay(4000);
    // ── 1. IR A POSICIÓN DE RECOGIDA ─────────────────────────────
    SCARA.sendLog("Trayectoria: hacia punto A (recogida)");
    irAPunto(A_Z, A_J1, A_J2);
    digitalWriteScara(PIN_S2, HIGH);   // cerrar gripper
    SCARA.sendLog("PICK - objeto recogido");
    delay(1000);
    // ── 2. IR A POSICIÓN DE DEPÓSITO ─────────────────────────────
    SCARA.sendLog("Trayectoria: hacia punto B (deposito)");
    irAPunto(B_Z, B_J1, B_J2);
    digitalWriteScara(PIN_S2, LOW);    // abrir gripper
    SCARA.sendLog("PLACE - objeto depositado");
    delay(1000);
    // ── 3. REGRESAR A HOME ───────────────────────────────────────
    SCARA.sendLog("Regresando a HOME");
    irAPunto(HOME_Z, HOME_J1, HOME_J2);
    SCARA.sendLog("Ciclo completado");
    delay(2000);
}
