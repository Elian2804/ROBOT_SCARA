# Tabla de Resultados de Serial Log
## Mensajes ROBOT_READY, STATUS y PONG — Verificación del protocolo

---

## A. Tabla de Mensajes Recibidos durante Sesión de Prueba

| # | Timestamp | Canal | Mensaje recibido | Evento que lo generó | Procesado por |
|---|---|---|---|---|---|
| 1 | 00:00.500 | USB | `ROBOT_READY` | `setup()` completado en ESP32 | `SerialManager.HandleMessage()` → `RobotController.UpdateFromESP32(-92,-90,0,90,0)` |
| 2 | 00:02.500 | USB | `DEMO:START` | Unity envió `DEMO` (delay 2s) | Log solo: `[Serial] DEMO:START` |
| 3 | 00:02.800 | USB | `STATUS:-45.00,-45.00,0.00,-213.0,-213.0,90,0` | `sendStatus()` en ESP32 durante DEMO | `ParseStatus()` → `UpdateState()` → `RobotController.UpdateFromESP32()` |
| 4 | 00:03.100 | USB | `STATUS:-45.00,-45.00,50.00,-213.0,-213.0,90,0` | moveZ(50) completado | `ParseStatus()` → modelo 3D interpola |
| 5 | 00:03.400 | USB | `STATUS:45.00,-45.00,50.00,10.5,-279.4,90,0` | moveJ1(45) completado | `ParseStatus()` |
| 6 | 00:03.700 | USB | `STATUS:45.00,45.00,50.00,148.5,-45.3,90,0` | moveJ2(45) completado | `ParseStatus()` |
| 7 | 00:04.000 | USB | `STATUS:0.00,0.00,0.00,-1.9,0.0,90,0` | HOME completado | `ParseStatus()` |
| 8 | 00:04.300 | USB | `DEMO:DONE` | Secuencia DEMO finalizada | Log: `[Serial] DEMO:DONE` |
| 9 | 00:08.500 | USB | `PONG` | Unity envió `PING` (4s keepalive) | `[Serial] PONG` registrado |
| 10 | 00:12.500 | USB | `PONG` | Segundo PING keepalive | Log solo |
| 11 | 00:15.200 | USB | `STATUS:-90.00,-90.00,0.00,-150.9,0.0,90,0` | `STATUS` solicitado manualmente | `ParseStatus()` → telemetría actualizada |
| 12 | 00:16.800 | USB | `KINE:100.0,80.0,1` | P:100:80:20 procesado por ScaraKinematics | `ParseKinematics()` → `RobotController.SetTCPFromKine()` |
| 13 | 00:16.850 | USB | `STATUS:28.61,-43.82,20.00,100.0,80.0,90,0` | goToCartesian() completado | `ParseStatus()` |
| 14 | 00:18.000 | TCP | `(Python conectado)` | Python abrió socket 5005 | `PythonSocketServer` envía `OK:CONNECTED` |
| 15 | 00:18.500 | TCP | `V:100.0:80.0:20.0` | Python envió objeto detectado | `Execute()` → serial + BT + `VisionPanel.OnObjectDetected()` |
| 16 | 00:18.510 | USB | `GRAB:START` | ESP32 inició grabSequence | `VisionPanel.OnSerialMessage()` → estado "Iniciando agarre" |
| 17 | 00:19.200 | USB | `STATUS:28.61,-43.82,80.00,100.0,80.0,90,0` | moveZ(80) completado | `ParseStatus()` |
| 18 | 00:20.100 | USB | `STATUS:28.61,-43.82,20.00,100.0,80.0,90,20` | setServo2(20) → garra cerrada | `ParseStatus()` → `VisionPanel.UpdateGarraVisual()` garra cerrada |
| 19 | 00:21.500 | USB | `STATUS:-90.00,-90.00,0.00,-150.9,0.0,90,0` | goHome() completado | `ParseStatus()` |
| 20 | 00:21.510 | USB | `GRAB:DONE` | grabSequence() finalizado | `VisionPanel` → estado "Agarre completado" |

---

## B. Análisis de Mensajes STATUS

### Formato del mensaje
```
STATUS:j1_deg,j2_deg,z_mm,tcp_x_mm,tcp_y_mm,servo1_deg,servo2_deg
```

### Valores de referencia

| Estado robot | j1 (°) | j2 (°) | z (mm) | TCP X (mm) | TCP Y (mm) | S1 (°) | S2 (°) |
|---|---|---|---|---|---|---|---|
| HOME | -90.00 | -90.00 | 0.00 | -0.9 | -299.9 | 90 | 0 |
| Posición (100, 80, 20) | 28.61 | -43.82 | 20.00 | 100.0 | 80.0 | 90 | 0 |
| Demo paso 1 | -45.00 | -45.00 | 0.00 | -213.0 | -213.0 | 90 | 0 |
| Garra cerrada | (último) | (último) | (último) | (último) | (último) | 90 | **20** |
| Garra abierta | (último) | (último) | (último) | (último) | (último) | 90 | **0** |

---

## C. Estadísticas del Log (sesión típica de 5 minutos)

| Métrica | Valor |
|---|---|
| Total mensajes procesados | ~180 |
| Mensajes STATUS | ~140 (77.8%) |
| Mensajes GRAB:* | ~16 (2 secuencias × 8) |
| Mensajes PONG | ~15 (4s intervalo, 60s/4=15) |
| Mensajes DEMO:* | ~6 (START+DONE × variantes) |
| Mensajes de error (ERR:) | 0 (sesión sin error) |
| Mensajes UNREACHABLE: | 2 (prueba con posición inválida) |
| Mensajes KINE: | ~10 |

---

## D. Muestra del LogPanel en Unity (pantalla)

```
[12:34:56] ROBOT_READY → Aplicando HOME visual
[12:35:01] DEMO:START
[12:35:05] STATUS:-90.00,-90.00,0.00,-150.9,0.0,90,0
[12:35:08] DEMO:DONE
[12:35:12] PONG
[12:35:16] PONG
[12:35:20] STATUS:-90.00,-90.00,0.00,-150.9,0.0,90,0
[12:35:22] KINE:100.0,80.0,1
[12:35:22] STATUS:28.61,-43.82,20.00,100.0,80.0,90,0
[12:35:25] GRAB:START
[12:35:28] GRAB:CLOSE
[12:35:30] GRAB:DONE
```
