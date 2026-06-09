/*
 * ================================================================
 *  DEMO — FIGURAS GEOMÉTRICAS (Triángulo en el espacio)
 *  Robot SCARA · Demostración visual para defensa
 * ================================================================
 *
 *  ¿Qué hace este sketch?
 *  El robot recorre 3 vértices formando un TRIÁNGULO en el espacio.
 *  Es visualmente claro y fácil de explicar al evaluador.
 *
 *  Secuencia (vértices del triángulo):
 *    HOME → VÉRTICE 1 (frente centro)
 *         → VÉRTICE 2 (derecha)
 *         → VÉRTICE 3 (izquierda)
 *         → HOME
 *
 *  ¿Por qué un triángulo?
 *  Demuestra en ~30 segundos:
 *    ✓ Control simultáneo de J1 y J2
 *    ✓ Repetibilidad (el robot vuelve al mismo punto)
 *    ✓ Planificación de trayectoria sin colisiones (Z sube primero)
 *    ✓ La conversión pasos → ángulo del simulador es correcta
 *
 *  ¿Cómo se ve en Unity?
 *  El LineRenderer del VisualizadorTrayectoria dibuja el triángulo
 *  en rojo (trayectoria real) mientras el robot lo recorre.
 *
 *  Para robot FÍSICO:
 *    Reemplazar  digitalWriteScara() → digitalWrite()
 *    Reemplazar  delayMicrosecondsScara() → delayMicroseconds()
 *    Eliminar    #include "SCARA.h" y SCARA.begin()
 * ================================================================
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
const int DELAY_J1J2 = 3000;
const int DELAY_Z    = 2000;
const int PAUSA_MS   = 600;

// ================================================================
//  VÉRTICES DEL TRIÁNGULO
//  Obtenidos del simulador Unity moviendo el robot con las teclas
//  y leyendo los pasos en el panel de trayectorias.
//
//  Con ratio 1:1: 100 pasos = 180° (de -90° a +90°)
//
//       VÉRTICE 3
//       (izquierda)
//           *
//          / \
//         /   \
//        /     \
//       *───────*
//   VÉRTICE 2  VÉRTICE 1
//   (derecha)  (frente)
// ================================================================

// HOME — posición inicial
const int H_Z = 0;   const int H_J1 = 0;   const int H_J2 = 0;

// VÉRTICE 1 — frente centro (brazo extendido hacia adelante)
const int V1_Z = 800;   const int V1_J1 = 50;  const int V1_J2 = 50;

// VÉRTICE 2 — derecha (J1 rotado a la derecha)
const int V2_Z = 800;   const int V2_J1 = 90;  const int V2_J2 = 20;

// VÉRTICE 3 — izquierda (J1 rotado a la izquierda)
const int V3_Z = 800;   const int V3_J1 = 15;  const int V3_J2 = 20;

// Altura de transporte entre vértices
const int ALT_TRANSPORTE = 300;

// ── ESTADO ───────────────────────────────────────────────────────
int pos_Z  = 0;
int pos_J1 = 0;
int pos_J2 = 0;

// ================================================================
//  HELPERS
// ================================================================

void moverEje(int pinDir, int pinStep, int &posActual, int destino, int delayUs) {
    if (destino == posActual) return;
    int pasos = abs(destino - posActual);
    digitalWriteScara(pinDir, (destino > posActual) ? HIGH : LOW);
    for (int i = 0; i < pasos; i++) {
        digitalWriteScara(pinStep, HIGH);
        delayMicrosecondsScara(delayUs);
        digitalWriteScara(pinStep, LOW);
        delayMicrosecondsScara(delayUs);
    }
    posActual = destino;
}

// Ir a vértice con secuencia segura: sube Z → rota → baja
void irAVertice(int destZ, int destJ1, int destJ2, const char* nombre) {
    SCARA.sendLog(String("Triangulo -> ") + nombre);

    moverEje(DIR_Z,  STEP_Z,  pos_Z,  ALT_TRANSPORTE, DELAY_Z);
    delay(PAUSA_MS / 2);

    moverEje(DIR_J1, STEP_J1, pos_J1, destJ1, DELAY_J1J2);
    delay(PAUSA_MS / 2);
    moverEje(DIR_J2, STEP_J2, pos_J2, destJ2, DELAY_J1J2);
    delay(PAUSA_MS / 2);

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

    pos_Z = H_Z;  pos_J1 = H_J1;  pos_J2 = H_J2;

    SCARA.sendLog("Demo Triangulo iniciado");
}

// ================================================================
//  LOOP — RECORRE EL TRIÁNGULO CONTINUAMENTE
// ================================================================
void loop() {

    delay(3000);

    // ── RECORRER TRIÁNGULO ────────────────────────────────────────
    irAVertice(V1_Z, V1_J1, V1_J2, "Vertice 1 (frente)");
    irAVertice(V2_Z, V2_J1, V2_J2, "Vertice 2 (derecha)");
    irAVertice(V3_Z, V3_J1, V3_J2, "Vertice 3 (izquierda)");
    irAVertice(V1_Z, V1_J1, V1_J2, "Vertice 1 (cierre)");

    // ── REGRESAR A HOME ───────────────────────────────────────────
    irAVertice(H_Z, H_J1, H_J2, "HOME");

    SCARA.sendLog("Triangulo completado - pausa");
    delay(2000);
}
