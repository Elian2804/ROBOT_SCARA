#ifndef SCARA_H
#define SCARA_H

#include <Arduino.h>

#ifndef SCARA_BUFFER_SIZE
#define SCARA_BUFFER_SIZE 4096
#endif

struct EventoSCARA {
    unsigned long tiempoUs;
    uint8_t pin;
    uint8_t valor;
};

class SCARAClass {
public:
    void begin(unsigned long baudRate);
    void beginBluetooth(const char* nombre);
    String readBluetoothCommand();
    bool isBluetoothConnected();
    void sendLog(const String& msg);
    void registrarEvento(uint8_t pin, uint8_t valor);

private:
    EventoSCARA _buffer[SCARA_BUFFER_SIZE];
    volatile uint16_t _cabeza = 0;
    volatile uint16_t _cola = 0;
    volatile uint32_t _contadorOverflow = 0;
    portMUX_TYPE _spinlock = portMUX_INITIALIZER_UNLOCKED;

    static const uint8_t MAX_LOGS = 16;
    String _colaLogs[MAX_LOGS];
    volatile uint8_t _cabezaLog = 0;
    volatile uint8_t _colaLog = 0;
    portMUX_TYPE _spinlockLog = portMUX_INITIALIZER_UNLOCKED;

    bool _btIniciado = false;
    bool _btConectadoAnterior = false;

    void _encolarLog(const String& msg);
    static void _tareaNucleo1(void* parametro);
    void _procesarTransmision();
};

extern SCARAClass SCARA;

void digitalWriteScara(uint8_t pin, uint8_t valor);
void delayMicrosecondsScara(unsigned int us);

#endif