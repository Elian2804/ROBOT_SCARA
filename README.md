<div align="center">

# ROBOT VIRTUAL SCARA

**Simulador 3D interactivo y monitor serial para brazo robótico SCARA con ESP32**

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-black?logo=unity&logoColor=white)](https://unity.com/)
[![ESP32](https://img.shields.io/badge/ESP32-Arduino-red?logo=arduino&logoColor=white)](https://www.espressif.com/)
[![Python](https://img.shields.io/badge/Python-3.8+-blue?logo=python&logoColor=white)](https://www.python.org/)
[![Licencia](https://img.shields.io/badge/Licencia-MIT-green)](LICENSE)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows-0078d4?logo=windows&logoColor=white)](https://github.com)

---

### ⬇️ DESCARGA

> **No necesitas instalar Unity.** El ejecutable ya incluye todo lo necesario.

[![Descargar ZIP](https://img.shields.io/badge/⬇️%20Descargar%20ROBOT__SCARA.zip-Última%20versión-FFD900?style=for-the-badge&labelColor=121C33)](../../releases/latest)

</div>

---

## Manual de usuario

Consulta el manual completo para instrucciones detalladas de instalación, uso de cada módulo y solución de problemas.

[![Manual de usuario](https://img.shields.io/badge/📄%20Manual%20de%20Usuario-Descargar%20PDF-FFD900?style=for-the-badge&labelColor=121C33)](../../releases/latest)

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

<div align="center">

Hecho con ♥ para control robótico educativo  
**ROBOT VIRTUAL SCARA — Unity 2022.3 + ESP32**

</div>
