# Guía de Sketches — Robot SCARA con Unity

Esta guía explica cómo usar los 4 sketches de demostración, qué datos
útiles te da el simulador Unity y cómo pasar directamente del simulador
al robot físico sin reescribir nada.

---

## Estructura de archivos

```
Assets/ESP32/
├── SCARA.h                    ← Librería (NECESARIA)
├── SCARA.cpp                  ← Librería (NECESARIA)
├── M1_ControlArticular/
│   └── M1_ControlArticular.ino
├── M2_Trayectorias/
│   └── M2_Trayectorias.ino
├── M3_VisionBT/
│   └── M3_VisionBT.ino
└── Demo_Figuras/
    └── Demo_Figuras.ino
```

---

## Cómo usar la librería SCARA

Copia `SCARA.h` y `SCARA.cpp` a la misma carpeta que tu sketch `.ino`.
Luego en tu sketch:

```cpp
#include "SCARA.h"

void setup() {
    SCARA.begin(115200);       // inicia Serial y el hilo de transmisión
    // SCARA.beginBluetooth("ROBOT"); // opcional, solo para M3
}

void loop() {
    digitalWriteScara(PIN, HIGH);    // en lugar de digitalWrite()
    delayMicrosecondsScara(3000);    // en lugar de delayMicroseconds()
}
```

**Por qué usar estas funciones:**
Cada `digitalWriteScara()` graba el evento (tiempo, pin, valor) en un
buffer. Un segundo hilo las envía por Serial al simulador Unity en
tiempo real, sin interrumpir el sketch.

---

## Sketch 1 — Control Articular (Entorno 1)

**Archivo:** `M1_ControlArticular/M1_ControlArticular.ino`

**Qué demuestra:** El control individual de cada motor (Z, J1, J2) de
forma secuencial. Cada eje se mueve, regresa y el robot termina en la
posición inicial.

**Secuencia visible en Unity:**
```
PanelControlArticular → pasos en tiempo real (contador que sube/baja)
EtiquetasArticulaciones → "J1: 84.6°  100/100p"
PanelConsola → "M2 - Brazo 1 | J1: 84.6° | paso +100/100"
```

**Parámetros ajustables:**
```cpp
const int DELAY_J1J2   = 3000;  // µs — velocidad de J1 y J2
const int DELAY_Z      = 2000;  // µs — velocidad de Z
const int PASOS_BRAZO  = 200;   // pasos de J1 y J2 por movimiento
const int PASOS_Z      = 3000;  // pasos Z para recorrido completo
```

**Qué seleccionar en Unity:**
- Ratio J1: `1:1` (si PASOS_BRAZO=200 en tu robot cubre 180°, usar 1:2)
- Ratio Z: `1:1` (3000 pasos = recorrido completo = 110 unidades Unity)

---

## Sketch 2 — Trayectorias (Entorno 2)

**Archivo:** `M2_Trayectorias/M2_Trayectorias.ino`

**Qué demuestra:** Pick-and-place automático entre dos posiciones
predefinidas. Los pasos se obtienen del simulador y se transcriben al
sketch directamente.

### Cómo obtener los valores de pasos desde Unity:

1. Abre Unity → Play → Entorno 2 (Trayectorias)
2. Mueve el robot con las teclas hasta la posición de recogida:
   - `Q/E` → J1  |  `A/D` → J2  |  `Z/X` → Z arriba/abajo
3. Presiona **"Guardar punto"** en el panel
4. El panel muestra:
   ```
   P01  M1:1500  M2:50  M3:40
         Z:100mm  J1:90°  J2:72°
   ```
5. Copia esos valores al sketch:
   ```cpp
   const int PICK_Z  = 1500;  // M1
   const int PICK_J1 = 50;    // M2
   const int PICK_J2 = 40;    // M3
   ```
6. Repite para la posición de depósito
7. Sube el sketch → el robot físico ejecuta exactamente esa trayectoria

**Secuencia visible en Unity:**
```
PanelTrayectorias → "Fase: hacia punto A (recogida)"
Consola → "PICK - objeto recogido"
VisualizadorTrayectoria → línea roja dibuja el camino recorrido
```

**Movimiento seguro (anti-colisión):**
El sketch sube Z antes de rotar los brazos, igual que el simulador.
```
1. Sube Z a ALT_TRANSPORTE
2. Rota J1 al destino
3. Rota J2 al destino
4. Baja Z al destino final
```

---

## Sketch 3 — Visión BT (Entorno 3)

**Archivo:** `M3_VisionBT/M3_VisionBT.ino`

**Qué demuestra:** El robot recibe comandos de texto por Bluetooth desde
Python (o cualquier app) y ejecuta la trayectoria correspondiente.

**Comandos disponibles:**
| Comando | Acción |
|---------|--------|
| `PICK`  | Ir a posición de recogida |
| `DROP`  | Ir a posición de depósito |
| `HOME`  | Regresar a posición inicial |

### Enviar comandos desde Python:
```python
import bluetooth

# Conectar al ESP32 (busca "SCARA_ROBOT" en dispositivos Bluetooth)
sock = bluetooth.BluetoothSocket(bluetooth.RFCOMM)
sock.connect(("XX:XX:XX:XX:XX:XX", 1))  # reemplaza con la MAC del ESP32

# Enviar comandos
sock.send("PICK\n")   # el \n indica fin de comando
import time; time.sleep(10)  # esperar que el robot ejecute
sock.send("DROP\n")
time.sleep(10)
sock.send("HOME\n")

sock.close()
```

### Con la app "Serial Bluetooth Terminal" (Android):
1. Conectar al dispositivo "SCARA_ROBOT"
2. Escribir `PICK` y enviar → el robot recoge
3. Escribir `DROP` → el robot deposita
4. Escribir `HOME` → regresa

**Qué se ve en Unity:**
```
PanelVisionBT → indicador azul "BT Conectado"
PanelVisionBT → "Último comando: PICK"
PanelVisionBT → log con timestamp de cada comando
Consola → "B:CON" al conectar, "CMD: PICK - yendo a recogida"
```

**Nota:** Los puntos PICK/DROP en M3 son los mismos que en M2.
Una vez calibrados en el Entorno 2, se copian al Entorno 3.

---

## Demo — Figuras Geométricas (Triángulo)

**Archivo:** `Demo_Figuras/Demo_Figuras.ino`

**Qué demuestra:** El robot recorre 3 puntos formando un triángulo en
el espacio. Visualmente claro y fácil de explicar.

**Lo que el evaluador verá:**
- El brazo se mueve de vértice en vértice de forma fluida
- El LineRenderer en Unity dibuja el triángulo en rojo
- El robot regresa exactamente al punto inicial (repetibilidad)

**Parámetros para ajustar el tamaño del triángulo:**
```cpp
const int V1_J1 = 50;  const int V1_J2 = 50;  // Vértice 1 (frente)
const int V2_J1 = 90;  const int V2_J2 = 20;  // Vértice 2 (derecha)
const int V3_J1 = 15;  const int V3_J2 = 20;  // Vértice 3 (izquierda)
```
Obtener estos valores desde el simulador Unity moviendo el robot con
el teclado y copiando los pasos del panel.

---

## Del simulador al robot físico en 3 pasos

Esta es la ventaja principal del sistema:

### Paso 1 — Calibrar en Unity
```
1. Play en Unity → Entorno 1
2. Conectar ESP32 por USB
3. Mover el brazo físico y observar el virtual en pantalla
4. Ajustar los toggles de ratio hasta que los ángulos coincidan
   (ejemplo: si 200 pasos = 90° físico → la ratio es 1:2)
```

### Paso 2 — Obtener los pasos de cada posición
```
1. Play → Entorno 2
2. Usar el teclado para mover el robot virtual
3. Guardar puntos → el panel muestra M1:xxx M2:xxx M3:xxx
4. Esos números son los pasos que necesitas en el sketch
```

### Paso 3 — Convertir a sketch físico
Simplemente elimina las 2 diferencias:

```cpp
// SIMULADOR Unity:
#include "SCARA.h"
SCARA.begin(115200);
digitalWriteScara(PIN, HIGH);
delayMicrosecondsScara(3000);

// ROBOT FÍSICO (eliminar SCARA):
// (sin #include ni SCARA.begin)
digitalWrite(PIN, HIGH);
delayMicroseconds(3000);
```

El resto del sketch (lógica, pasos, secuencias, tiempos) es
**exactamente el mismo** para virtual y físico.

---

## Referencia de pines

| Motor | DIR | STEP |
|-------|-----|------|
| M1 Eje Z | 2 | 15 |
| M2 Brazo 1 (J1) | 18 | 19 |
| M3 Brazo 2 (J2) | 4 | 16 |
| EN_PIN (LOW = activo) | — | 5 |
| S1 GiroGarra (servo) | — | 35 |
| S2 Gripper (servo) | — | 34 |

## Referencia de conversión (ratio 1:1)

| Eje | Pasos | Equivale a |
|-----|-------|------------|
| J1 / J2 | 100 pasos | 180° (de -90° a +90°) |
| J1 / J2 | 50 pasos | 90° (de home a centro) |
| Z | 3000 pasos | 200mm (recorrido completo) |
| Z | 1500 pasos | 100mm (mitad del recorrido) |

Con otras ratios, multiplicar: 1:2 → ×2 pasos para el mismo ángulo.
