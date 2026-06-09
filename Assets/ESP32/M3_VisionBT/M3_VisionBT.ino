#include "SCARA.h"

#define DIR_SUBEBAJA   2
#define STEP_SUBEBAJA  15
#define DIR_BRAZO      18
#define STEP_BRAZO     19
#define DIR_ANTEBRAZO  4
#define STEP_ANTEBRAZO 16
#define EN_PIN         5

const int STEP_DELAY_US = 1000;

void setup() {
    SCARA.begin(115200);
    SCARA.beginBluetooth("SCARA_ROBOT_1");

    pinMode(EN_PIN, OUTPUT);
    pinMode(DIR_SUBEBAJA,   OUTPUT);
    pinMode(STEP_SUBEBAJA,  OUTPUT);
    pinMode(DIR_BRAZO,      OUTPUT);
    pinMode(STEP_BRAZO,     OUTPUT);
    pinMode(DIR_ANTEBRAZO,  OUTPUT);
    pinMode(STEP_ANTEBRAZO, OUTPUT);
    digitalWriteScara(EN_PIN, LOW);
}

void moverMotor(int stepPin, int dirPin, int pasos, int direccion) {
    if (pasos <= 0) return;
    digitalWriteScara(dirPin, direccion == 1 ? HIGH : LOW);
    for (int i = 0; i < pasos; i++) {
        digitalWriteScara(stepPin, HIGH);
        delayMicrosecondsScara(STEP_DELAY_US);
        digitalWriteScara(stepPin, LOW);
        delayMicrosecondsScara(STEP_DELAY_US);
    }
}

void loop() {
    String comando = SCARA.readBluetoothCommand();
    if (comando.length() == 0) return;
    comando.trim();

    int c1 = comando.indexOf(',');
    int c2 = comando.indexOf(',', c1 + 1);

    if (c1 != -1 && c2 != -1) {
        char motor     = comando.charAt(0);
        int  pasos     = comando.substring(c1 + 1, c2).toInt();
        int  direccion = comando.substring(c2 + 1).toInt();

        SCARA.sendLog(comando.c_str());

        if      (motor == 'z') moverMotor(STEP_SUBEBAJA,  DIR_SUBEBAJA,  pasos, direccion);
        else if (motor == 'a') moverMotor(STEP_BRAZO,     DIR_BRAZO,     pasos, direccion);
        else if (motor == 'f') moverMotor(STEP_ANTEBRAZO, DIR_ANTEBRAZO, pasos, direccion);
    }
}
