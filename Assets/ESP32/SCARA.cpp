#include "SCARA.h"
#include <BluetoothSerial.h>
// Instancia global
SCARAClass SCARA;
// Bluetooth Serial (solo se usa si se llama beginBluetooth)
static BluetoothSerial SerialBT;
// ═══════════════════════════════════════════════════════════════════
// FUNCIONES PUBLICAS
// ═══════════════════════════════════════════════════════════════════
void SCARAClass::begin(unsigned long baudRate) {
    Serial.begin(baudRate);
    // Esperar a que el serial este listo
    while (!Serial) {
        delay(10);
    }
    // Inicializar buffer
    _cabeza = 0;
    _cola = 0;
    _contadorOverflow = 0;
    // Crear tarea en el nucleo 1 para transmision serial
    xTaskCreatePinnedToCore(
        _tareaNucleo1,    // Funcion de la tarea
        "TareaSCARA",     // Nombre
        4096,             // Tamano del stack
        this,             // Parametro (puntero a esta instancia)
        1,                // Prioridad
        NULL,             // Handle (no lo necesitamos)
        1                 // Nucleo 1
    );
    // Enviar mensaje de inicio
    Serial.println("L:SCARA iniciada");
}
void SCARAClass::beginBluetooth(const char* nombre) {
    if (!_btIniciado) {
        SerialBT.begin(nombre);
        _btIniciado = true;
        _btConectadoAnterior = false;
        _encolarLog("Bluetooth iniciado como: " + String(nombre));
    }
}
String SCARAClass::readBluetoothCommand() {
    if (!_btIniciado) return "";
    
    if (SerialBT.available()) {
        String comando = SerialBT.readStringUntil('\n');
        comando.trim();
        if (comando.length() > 0) {
            _encolarLog("BT cmd: " + comando);
            return comando;
        }
    }
    return "";
}
bool SCARAClass::isBluetoothConnected() {
    if (!_btIniciado) return false;
    return SerialBT.hasClient();
}

void SCARAClass::sendLog(const String& msg) {
    _encolarLog(msg);
}
// ═══════════════════════════════════════════════════════════════════
// FUNCIONES GLOBALES
// ═══════════════════════════════════════════════════════════════════
void digitalWriteScara(uint8_t pin, uint8_t valor) {
    // 1. Ejecutar la accion fisica real
    digitalWrite(pin, valor);
    // 2. Registrar el evento en el buffer
    SCARA.registrarEvento(pin, valor);
}
void delayMicrosecondsScara(unsigned int us) {
    // Solo ejecuta la espera, no registra nada
    delayMicroseconds(us);
}
// ═══════════════════════════════════════════════════════════════════
// METODOS INTERNOS
// ═══════════════════════════════════════════════════════════════════
void SCARAClass::registrarEvento(uint8_t pin, uint8_t valor) {
    portENTER_CRITICAL(&_spinlock);
    uint16_t siguienteCabeza = (_cabeza + 1) % SCARA_BUFFER_SIZE;
    if (siguienteCabeza == _cola) {
        // Buffer lleno: se pierde el evento
        _contadorOverflow++;
        portEXIT_CRITICAL(&_spinlock);
        return;
    }
    // Registrar evento con timestamp
    _buffer[_cabeza].tiempoUs = micros();
    _buffer[_cabeza].pin = pin;
    _buffer[_cabeza].valor = valor;
    _cabeza = siguienteCabeza;
    
    portEXIT_CRITICAL(&_spinlock);
}
void SCARAClass::_encolarLog(const String& msg) {
    portENTER_CRITICAL(&_spinlockLog);
    
    uint8_t siguienteCabeza = (_cabezaLog + 1) % MAX_LOGS;
    
    if (siguienteCabeza != _colaLog) {
        _colaLogs[_cabezaLog] = msg;
        _cabezaLog = siguienteCabeza;
    }
    // Si la cola de logs esta llena, se descarta silenciosamente
    portEXIT_CRITICAL(&_spinlockLog);
}
// ═══════════════════════════════════════════════════════════════════
// TAREA DEL NUCLEO 1 (transmision serial)
// ═══════════════════════════════════════════════════════════════════
void SCARAClass::_tareaNucleo1(void* parametro) {
    SCARAClass* instancia = (SCARAClass*)parametro;
    for (;;) {
        instancia->_procesarTransmision();
        // Pequena espera para no saturar el serial
        delayMicroseconds(100);
    }
}
void SCARAClass::_procesarTransmision() {
    // ─── PRIORIDAD 1: Eventos T (movimiento del robot) ───────────
    portENTER_CRITICAL(&_spinlock);
    bool hayEvento = (_cola != _cabeza);
    EventoSCARA evento;
    if (hayEvento) {
        evento = _buffer[_cola];
        _cola = (_cola + 1) % SCARA_BUFFER_SIZE;
    }
    // Verificar overflow
    uint32_t overflow = _contadorOverflow;
    if (overflow > 0) {
        _contadorOverflow = 0;
    }
    portEXIT_CRITICAL(&_spinlock);
    // Transmitir evento T
    if (hayEvento) {
        Serial.print("T:");
        Serial.print(evento.tiempoUs);
        Serial.print(",P:");
        Serial.print(evento.pin);
        Serial.print(",V:");
        Serial.println(evento.valor);
        return; // Prioridad: solo un mensaje por ciclo
    }
    // ─── PRIORIDAD 2: Error de overflow E ─────────────────────────
    if (overflow > 0) {
        Serial.print("E:OVF,C:");
        Serial.println(overflow);
        return;
    }
    // ─── PRIORIDAD 3: Logs L ─────────────────────────────────────
    portENTER_CRITICAL(&_spinlockLog);
    bool hayLog = (_colaLog != _cabezaLog);
    String msgLog;
    if (hayLog) {
        msgLog = _colaLogs[_colaLog];
        _colaLog = (_colaLog + 1) % MAX_LOGS;
    }
    portEXIT_CRITICAL(&_spinlockLog);
    if (hayLog) {
        Serial.print("L:");
        Serial.println(msgLog);
        return;
    }
    // ─── PRIORIDAD 4: Estado Bluetooth B ─────────────────────────
    if (_btIniciado) {
        bool conectadoAhora = SerialBT.hasClient();
        if (conectadoAhora != _btConectadoAnterior) {
            _btConectadoAnterior = conectadoAhora;
            if (conectadoAhora) {
                Serial.println("B:CON");
            } else {
                Serial.println("B:DIS");
            }
        }
    }
}
