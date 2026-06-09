/*
 * ================================================================
 *  ENTORNO 1 — CONTROL ARTICULAR
 *  Robot SCARA · Demostración para defensa académica
 * ================================================================
 *  ¿Qué hace este sketch?
 *  Mueve cada motor del robot de forma individual y secuencial,
 *  demostrando el control articular eje por eje.
 *  Al finalizar, el robot regresa exactamente a la posición inicial.
 *  Secuencia completa (~40 segundos):
 *    1. Pausa inicial          (5 s)
 *    2. Subir eje Z            → 3000 pasos
 *    3. Brazo J1 → derecha     → 200 pasos
 *    4. Antebrazo J2 → derecha → 200 pasos
 *       [Pausa visual: robot extendido]
 *    5. Antebrazo J2 → izq.   → 200 pasos (regreso)
 *    6. Brazo J1 → izq.       → 200 pasos (regreso)
 *       [Pausa visual: robot en home]
 *    7. Bajar eje Z            → 3000 pasos
 *    8. Repite desde el paso 1
 *  ¿Cómo se ve en Unity?
 *  El simulador muestra en tiempo real:
 *    · PanelControlArticular: contador de pasos + dirección + velocidad
 *    · Etiquetas 3D: ángulo calculado + paso_actual/pasos_referencia
 *    · Consola: "M2 - Brazo 1 (J1) | J1: 84.6° | paso +100/100"
 *  Para usar en robot FÍSICO sin Unity:
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
// ── PARÁMETROS DE MOVIMIENTO ──────────────────────────────────────
// Ajustar DELAY_US hasta obtener el movimiento deseado:
//   Más grande = más lento y más torque
//   Más pequeño = más rápido (riesgo de perder pasos si es muy bajo)
const int DELAY_J1J2   = 3000;   // µs entre pulsos para brazos
const int DELAY_Z      = 2000;   // µs entre pulsos para eje Z
// Pasos calibrados con el simulador Unity (ratio 1:1):
//   PASOS_BRAZO = 200 → barrido de un extremo al otro
//   PASOS_Z     = 3000 → recorrido completo Z (0 → 200mm)
const int PASOS_BRAZO  = 200;
const int PASOS_Z      = 3000;

// ── PAUSA ENTRE MOVIMIENTOS ──────────────────────────────────────
const int PAUSA_MS     = 1000;   // ms de espera entre fases

// ================================================================
//  SETUP
// ================================================================
void setup() {
    SCARA.begin(115200);   // inicia comunicación con Unity

    pinMode(EN_PIN,  OUTPUT);
    pinMode(DIR_J1,  OUTPUT);
    pinMode(STEP_J1, OUTPUT);
    pinMode(DIR_J2,  OUTPUT);
    pinMode(STEP_J2, OUTPUT);
    pinMode(DIR_Z,   OUTPUT);  pinMode(STEP_Z,  OUTPUT);
    pinMode(PIN_S1,  OUTPUT);
    pinMode(PIN_S2,  OUTPUT);

    digitalWriteScara(EN_PIN, LOW);   // habilitar drivers

    SCARA.sendLog("M1 Control Articular listo");
}
// Mueve un motor N pasos en la dirección indicada
void mover(int pinDir, int pinStep, int pasos, bool dirPositiva, int delayUs) {
    digitalWriteScara(pinDir, dirPositiva ? HIGH : LOW);
    for (int i = 0; i < pasos; i++) {
        digitalWriteScara(pinStep, HIGH);
        delayMicrosecondsScara(delayUs);
        digitalWriteScara(pinStep, LOW);
        delayMicrosecondsScara(delayUs);
    }
}
//SECUENCIA PRINCIPAL
void loop() {

    // ── PAUSA INICIAL ────────────────────────────────────────────
    SCARA.sendLog("Inicio ciclo - esperando...");
    delay(5000);

    // ── FASE 1: SUBIR EJE Z ──────────────────────────────────────
    SCARA.sendLog("Z: subiendo");
    mover(DIR_Z, STEP_Z, PASOS_Z, false, DELAY_Z);   // LOW = subir
    delay(PAUSA_MS);

    // ── FASE 2: BRAZO J1 → DIRECCIÓN POSITIVA ───────────────────
    SCARA.sendLog("J1: movimiento +");
    mover(DIR_J1, STEP_J1, PASOS_BRAZO, true, DELAY_J1J2);
    delay(PAUSA_MS);

    // ── FASE 3: ANTEBRAZO J2 → DIRECCIÓN POSITIVA ───────────────
    SCARA.sendLog("J2: movimiento +");
    mover(DIR_J2, STEP_J2, PASOS_BRAZO, true, DELAY_J1J2);
    delay(PAUSA_MS * 2);   // pausa visual: robot extendido

    // ── FASE 4: ANTEBRAZO J2 → REGRESO ──────────────────────────
    SCARA.sendLog("J2: regreso");
    mover(DIR_J2, STEP_J2, PASOS_BRAZO, false, DELAY_J1J2);
    delay(PAUSA_MS);

    // ── FASE 5: BRAZO J1 → REGRESO ──────────────────────────────
    SCARA.sendLog("J1: regreso");
    mover(DIR_J1, STEP_J1, PASOS_BRAZO, false, DELAY_J1J2);
    delay(PAUSA_MS * 2);   // pausa visual: robot en home

    // ── FASE 6: BAJAR EJE Z ──────────────────────────────────────
    SCARA.sendLog("Z: bajando - ciclo completo");
    mover(DIR_Z, STEP_Z, PASOS_Z, true, DELAY_Z);    // HIGH = bajar
    delay(PAUSA_MS);

    // El loop() repite → robot vuelve al inicio automáticamente
}
