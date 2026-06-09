# Tabla de Módulos, Periféricos y Funciones del ESP32
## Robot SCARA — Control Embebido

| # | Módulo / Periférico | Tipo | Función en el sistema | Pin(s) ESP32 | Configuración |
|---|---|---|---|---|---|
| 1 | **UART0 (USB-Serial)** | Comunicación | Intercambio de comandos y telemetría con Unity vía cable USB | TX=1, RX=3 | 115200 baud, 8N1, `\n` terminador |
| 2 | **Bluetooth Classic SPP** | Comunicación | COM virtual inalámbrico; empareja como "ROBOT_SCARA" | Interno | PIN 1234, misma tasa 115200 baud |
| 3 | **WiFi / TCP Server** | Comunicación | Socket TCP para visión artificial Python | Interno | IP 127.0.0.1, Puerto 5005 |
| 4 | **Driver J1 (A4988/DRV8825)** | Actuador | Motor NEMA-17 articulación base (θ1) | STEP=18, DIR=19, EN=21 | ENABLE activo LOW; microstepping ×1 |
| 5 | **Driver J2 (A4988/DRV8825)** | Actuador | Motor NEMA-17 articulación codo (θ2) | STEP=22, DIR=23, EN=25 | ENABLE activo LOW; microstepping ×1 |
| 6 | **Driver Z (A4988/DRV8825)** | Actuador | Motor NEMA-17 husillo eje vertical | STEP=26, DIR=27, EN=14 | ENABLE activo LOW; tornillo 0.8 mm/rev |
| 7 | **LEDC Canal 0 (PWM)** | Actuador | Servo S1 — rotación muñeca | GPIO 32 | 50 Hz, 16 bits; duty 1638–8192 (0°–180°) |
| 8 | **LEDC Canal 1 (PWM)** | Actuador | Servo S2 — apertura/cierre garra | GPIO 33 | 50 Hz, 16 bits; duty 1638–8192 (0°–20°) |
| 9 | **Timer Hardware (delayMicroseconds)** | Temporización | Generación precisa de pulsos de paso para los drivers | N/A | Resolución 1 µs; retardo configurable por velocidad |
| 10 | **Flash NVS / SPIFFS** | Almacenamiento | Parámetros de calibración y offsets de posición | SPI interno | 4 MB total; partición NVS 20 KB |
| 11 | **Procesador dual-core LX6** | CPU | Núcleo 0: loop() + serial; Núcleo 1: tareas BT/WiFi | N/A | 240 MHz; FreeRTOS integrado |
| 12 | **SRAM interna** | Memoria | Stack, variables de estado, buffers de comandos | N/A | 520 KB total; uso estimado ~8 KB |

---

## Funciones de Software Implementadas en ESP32

| Función | Comando recibido | Respuesta enviada | Entorno(s) |
|---|---|---|---|
| Mover articulación individual | `J:1:angle:speed` / `J:2:angle:speed` / `J:3:z:speed` | `STATUS:j1,j2,z,x,y,s1,s2` | E1, E2, E3, E4, E5 |
| Control servo muñeca | `S1:angle` | `STATUS:...` | E1–E5 |
| Control servo garra | `S2:angle` | `STATUS:...` | E1–E5 |
| Ir a HOME | `HOME` | `STATUS:...` | E1–E5 |
| Parada de emergencia | `STOP` | — | E1–E5 |
| Solicitar estado | `STATUS` | `STATUS:j1,j2,z,x,y,s1,s2` | E1–E5 |
| Keep-alive | `PING` | `PONG` | E1–E5 |
| Secuencia demo | `DEMO` | `DEMO:START`, `DEMO:DONE` | E1–E5 |
| Mover a posición cartesiana (IK) | `P:x:y:z` | `STATUS:...` o `UNREACHABLE:x,y` | E2, E4 |
| Target de visión | `TARGET:x,y` | `ACK_TARGET:x,y` | E2, E5 |
| Target visión con Z | `V:x:y:z` | Estado GRAB secuencial | E5 |
| Ejecutar trayectoria PTP | `TRAJ:PTP:x1,y1,z1:x2,y2,z2` | `TRAJ:step,total,type,x,y` | E3 |
| Iniciar agarre automático | `VISION:RED` | `GRAB:START` … `GRAB:DONE` | E5 |
| Exportar log CSV | `LOG:EXPORT` | `CSV_START`, `CSV:...`, `CSV_END` | E4 |
| Prueba de precisión | `EXP:PRECISION` | `PRECISION_TEST:...` | E4 |
| Prueba de velocidad | `EXP:SPEED` | `SPEED_TEST:...` | E4 |

---

## Protocolo de Comunicación Serial

```
Dirección     Formato                           Ejemplo
─────────────────────────────────────────────────────────────────
Unity → ESP32  <COMANDO>\n                      J:1:45.0:30\n
ESP32 → Unity  <RESPUESTA>\n                   STATUS:-45.00,-30.00,50.00,120.5,85.3,90,0\n
```

- **Velocidad:** 115 200 baud (≈ 11 520 bytes/s)
- **Latencia típica USB:** 5–15 ms (Windows CDC)
- **Latencia típica BT:** 20–50 ms (SPP clásico)
- **Ping interval:** 4 s (heartbeat Unity → ESP32)
