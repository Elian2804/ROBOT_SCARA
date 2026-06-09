<div align="center">

# 🤖 ROBOT SCARA — Simulador & Monitor Unity

**Simulador 3D interactivo y monitor serial para brazo robótico SCARA con ESP32**

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-black?logo=unity&logoColor=white)](https://unity.com/)
[![ESP32](https://img.shields.io/badge/ESP32-Arduino-red?logo=arduino&logoColor=white)](https://www.espressif.com/)
[![Python](https://img.shields.io/badge/Python-3.8+-blue?logo=python&logoColor=white)](https://www.python.org/)
[![Licencia](https://img.shields.io/badge/Licencia-MIT-green)](LICENSE)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows-0078d4?logo=windows&logoColor=white)](https://github.com)

---

### ⬇️ DESCARGA EL SIMULADOR

> **No necesitas instalar Unity.** El ejecutable ya incluye todo lo necesario.

<!-- Actualiza este enlace con el de tu Release en GitHub -->
[![Descargar ZIP](https://img.shields.io/badge/⬇️%20Descargar%20ROBOT__SCARA.zip-Última%20versión-FFD900?style=for-the-badge&labelColor=121C33)](../../releases/latest)

</div>

---

## ¿Qué incluye el ZIP?

```
ROBOT_SCARA_v1.0/
├── ROBOT_SCARA.exe          ← Ejecutable principal (doble clic para abrir)
├── ROBOT_SCARA_Data/        ← Recursos del simulador
├── UnityCrashHandler64.exe
├── UnityPlayer.dll
│
├── ESP32/                   ← Sketches y librería para el hardware
│   ├── SCARA.h              ← Librería ESP32 — cabecera
│   ├── SCARA.cpp            ← Librería ESP32 — implementación
│   ├── M1_ControlArticular/ ← Módulo 1: control de articulaciones
│   ├── M2_Trayectorias/     ← Módulo 2: trayectorias grabadas
│   ├── M3_VisionBT/         ← Módulo 3: visión artificial + Bluetooth
│   └── Demo_Figuras/        ← Sketch de demostración
│
└── Python/                  ← Scripts de visión por computadora
    ├── vision_scara.py
    ├── vision_gesto.py
    └── requirements.txt
```

---

## Módulos del sistema

### Módulo 1 — Control Articular
Monitor en tiempo real de los motores vía serial.  
Muestra pasos acumulados, dirección y velocidad de cada eje (Z, J1, J2) exactamente como los envía el ESP32.

### Módulo 2 — Trayectorias
Graba posiciones con el teclado y reproduce secuencias seguras (sube Z → rota brazos → baja Z).  
Los puntos guardados muestran los pasos exactos que puedes transcribir directamente al sketch de Arduino.

### Módulo 3 — Visión BT
Recibe comandos desde Python/OpenCV vía TCP (puerto 5005) y los ejecuta como trayectorias pregrabadas.  
El script de Python manda decisiones (`PICK`, `DROP`, `HOME`), no coordenadas.

---

## Inicio rápido — solo el `.exe`

1. Descomprime el ZIP en cualquier carpeta.
2. Ejecuta `ROBOT_SCARA.exe`.
3. Usa el teclado para mover el robot en el simulador:

| Tecla | Acción |
|-------|--------|
| `Q` / `E` | Brazo 1 (J1) − / + |
| `A` / `D` | Brazo 2 (J2) − / + |
| `Z` / `X` | Eje Z − / + |
| `R` / `T` | Giro garra − / + |
| `G` / `F` | Cerrar / Abrir gripper |
| `H` | Home (secuencia segura) |
| `U` | Ejecutar PICK |
| `I` | Ejecutar DROP |

---

## Conectar el hardware ESP32

### Requisitos
- Arduino IDE 1.8+ o 2.x
- Placa: **ESP32 Dev Module**
- Baud rate: `115200`

### Instalar la librería SCARA

1. Copia `SCARA.h` y `SCARA.cpp` (carpeta `ESP32/` del ZIP) a la carpeta de tu sketch.
2. Incluye la librería en tu código:

```cpp
#include "SCARA.h"

void setup() {
    SCARA.begin(115200);
    pinMode(EN_PIN, OUTPUT);
    digitalWriteScara(EN_PIN, LOW);  // Activa drivers (LOW = activo)
}

void loop() {
    // Usa digitalWriteScara() y delayMicrosecondsScara()
    // en lugar de las funciones estándar de Arduino
}
```

### Pines del hardware

| Motor / Señal | Pin DIR | Pin STEP/señal |
|---------------|---------|----------------|
| M1 Eje Z | 2 | 15 |
| M2 Brazo 1 (J1) | 18 | 19 |
| M3 Brazo 2 (J2) | 4 | 16 |
| EN (enable drivers) | — | 5 |
| S1 Giro garra | — | 35 |
| S2 Gripper | — | 34 |

### Sketches de prueba incluidos

| Sketch | Descripción |
|--------|-------------|
| `M1_ControlArticular` | Control básico articulaciones, protocolo serial completo |
| `M2_Trayectorias` | Ejecuta trayectorias punto a punto |
| `M3_VisionBT` | Integración Bluetooth + comandos TCP desde Python |
| `Demo_Figuras` | Dibuja figuras para verificar calibración |

---

## Módulo de visión artificial (Python)

Requiere Python 3.8+ y OpenCV:

```bash
pip install -r Python/requirements.txt
python Python/vision_scara.py
```

El script se conecta al simulador (o al ESP32 vía Bluetooth) por TCP en `localhost:5005`.

---

## Compilar desde el código fuente

> Solo necesario si quieres modificar la interfaz Unity.

**Requisitos:**
- Unity 2022.3.62f3 (LTS)
- Visual Studio 2022 o VS Code

```
1. Clona este repositorio
2. Abre Unity Hub → Add → selecciona la carpeta del proyecto
3. Abre la escena: Assets/ROBOT_SCARA.unity
4. Menú SCARA → Construir Escena Completa (si los paneles no aparecen)
5. File → Build Settings → Build
```

---

## Estructura del repositorio

```
Assets/
├── Scripts/          ← C# runtime (simulador, serial, UI)
│   └── Editor/       ← Herramientas del editor Unity
├── ESP32/            ← Firmware ESP32 + librería SCARA
├── Python/           ← Visión artificial
├── Datos/Motores/    ← Assets de configuración de motores
├── Scenes/           ← Escena principal Unity
└── images/           ← Recursos visuales
Docs/                 ← Documentación técnica completa
ProjectSettings/      ← Configuración del proyecto Unity
Packages/             ← Dependencias Unity
```

---

## Documentación técnica

La carpeta `Docs/Entregables/` contiene la documentación completa del proyecto:
casos de uso, requerimientos, diagramas UML, resultados de pruebas y análisis de latencias.

---

<div align="center">

Hecho con ♥ para control robótico educativo  
**ROBOT SCARA — Unity 2022.3 + ESP32**

</div>
