# Tabla de Latencias por Canal
## USB / Bluetooth / TCP — Medición RTT y Throughput

---

## A. Definición de Métricas

| Métrica | Definición | Unidad |
|---|---|---|
| **RTT** (Round Trip Time) | Tiempo desde que Unity envía el comando hasta que recibe la respuesta | ms |
| **Latencia de procesamiento ESP32** | Tiempo desde recibir el comando hasta enviar la respuesta (sin movimiento) | ms |
| **Latencia serial Windows (CDC)** | Overhead del driver USB-CDC del sistema operativo | ms |
| **Throughput** | Máximo de mensajes STATUS procesados por segundo | msg/s |
| **Jitter** | Variación estándar del RTT | ms |

---

## B. Tabla de Latencias por Canal

| Canal | Protocolo | RTT mínimo | RTT típico | RTT máximo | Jitter (σ) | Throughput | Configuración |
|---|---|---|---|---|---|---|---|
| **USB Serial** | CDC-ACM (UART over USB) | 3 ms | **8–15 ms** | 35 ms | ±4 ms | ~60 msg/s | 115200 baud, Windows 10/11 |
| **Bluetooth SPP** | Bluetooth Classic 2.0 | 15 ms | **25–45 ms** | 120 ms | ±12 ms | ~20 msg/s | A2DP profile, COM virtual |
| **TCP/Socket** | TCP localhost (loopback) | <1 ms | **1–5 ms** | 15 ms | ±1 ms | ~200 msg/s | 127.0.0.1:5005 |

---

## C. Desglose de Latencia USB (canal más usado)

```
Unity Send("J:1:45.0:30")
  │
  ├── Unity .NET enqueue            ~0.1 ms
  ├── Windows USB driver (CDC)      ~2–8 ms   ← principal contribuidor
  ├── ESP32 recepción + trim        ~0.1 ms
  ├── processCommand() + moveJ1()   ~variable (depende del movimiento)
  ├── sendStatus() + println()      ~0.2 ms
  ├── ESP32 TX buffer flush         ~0.1 ms
  ├── Windows USB driver (RX CDC)   ~2–8 ms
  └── Unity HandleMessage()         ~0.1 ms
                                    ─────────
TOTAL RTT (solo PING→PONG):         5–18 ms
TOTAL RTT (con STATUS):             8–25 ms
```

---

## D. Tabla de Latencias por Tipo de Operación

| Operación | Canal | Latencia total | Incluye movimiento |
|---|---|---|---|
| PING → PONG | USB | 5–18 ms | No |
| STATUS request → STATUS response | USB | 8–20 ms | No |
| J:1:5:30 → STATUS (sin esperar motor) | USB | 10–30 ms + tiempo motor | Sí (bloqueante en ESP32) |
| P:100:80:20 → KINE+STATUS | USB | 50–200 ms | Sí (IK+3 motores) |
| V:x:y:z → GRAB:DONE | USB | 3–8 segundos | Sí (7 pasos de agarre) |
| Python TARGET → ACK_TARGET | TCP+USB | 15–50 ms | Sí (2 motores) |
| Python V:x:y:z → OK:V:... | TCP | <5 ms | No (solo ACK de Unity) |

---

## E. Factores que Afectan la Latencia

### USB Serial
| Factor | Impacto |
|---|---|
| Polling del driver Windows CDC | +2–8 ms (no reducible sin kernel driver) |
| `ReadTimeout = 500 ms` | Solo afecta si no hay datos; nominal sin efecto |
| Thread de lectura independiente | Reduce latencia ~2ms vs. leer en Update() |
| `_queue` + `lock` | <0.1 ms (overhead mínimo) |

### Bluetooth SPP
| Factor | Impacto |
|---|---|
| Negociación de conexión SPP | Solo en Connect() — 200–500 ms (una vez) |
| Empaquetado Bluetooth (MTU ~128 B) | +5–15 ms por trama |
| Interferencia RF / distancia | +10–50 ms adicional |
| PING_INTERVAL = 4s keepalive | Detecta pérdida en ~4s |

### TCP Socket (Python ↔ Unity)
| Factor | Impacto |
|---|---|
| Loopback (127.0.0.1) | <1 ms (kernel bypass) |
| Buffer TCP kernel | Típicamente vacío en loopback |
| Serialización string UTF-8 | <0.1 ms |
| Queue + lock en ServerLoop | <0.1 ms |

---

## F. Recomendaciones de Latencia

| Escenario | Canal recomendado | Justificación |
|---|---|---|
| Control en tiempo real (demo) | **USB Serial** | RTT <15ms, confiable, sin jitter apreciable |
| Demostración inalámbrica | **Bluetooth** | Sin cable, RTT aceptable para operación manual |
| Integración visión Python | **TCP localhost** | RTT <5ms; el delay real está en el movimiento del robot |
| Logging de alta frecuencia | **USB Serial** | Mayor throughput; ~60 STATUS/s posibles |
| Entorno industrial (futuro) | USB Serial + WiFi TCP | Bluetooth no recomendado para >10 msg/s |

---

## G. Medición de Latencia con COMM_TEST.ino

Usando el sketch `05_Arduino_Prueba_Comunicacion.ino`:

```csharp
// En Unity (C#) — medir RTT por software
long t0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
SerialManager.Instance?.Send("PING");
// ... en HandleMessage cuando llega "PONG":
long rtt = DateTimeOffset.Now.ToUnixTimeMilliseconds() - t0;
Debug.Log($"[Latencia] RTT USB: {rtt} ms");
```

```csharp
// Para TCP desde Python:
import time
t0 = time.time()
sock.send(b"LATENCY_TEST\n")
resp = sock.recv(64)   # "LAT:OK:12345"
rtt_ms = (time.time() - t0) * 1000
print(f"RTT TCP: {rtt_ms:.2f} ms")
```
