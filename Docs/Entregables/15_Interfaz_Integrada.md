# Interfaz Integrada con el Robot SCARA y Entornos Virtuales
## SCARA Sim — Descripción Completa de la Interfaz

---

## A. Layout General de la Interfaz (1920×1080 px)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  TOP BAR (50px)                                                              │
│  [SCARA Sim]  [E1 Articular][E2 Cinemática][E3 Trayect.][E4 Análisis][E5 Vis]│
│                              ● COM3 CONNECTED          [EMERGENCY STOP]      │
├──────────────┬───────────────────────────────────────────────────────────────┤
│  MINI CAM    │                                                               │
│  265×220 px  │               VIEWPORT 3D (Camera.main)                      │
│  (top view)  │               Robot SCARA modelo FBX                         │
│              │               Esfera TCP roja + trail naranja                │
│              │               WorkspaceVisualizer (anillo)                   │
├──────────────┤                                                               │
│  ZOOM        │                                                               │
│  CONTROLS    │                                                               │
│  (bottom-    │                                                               │
│   left)      │                                                    ┌──────────┤
│              │                                                    │ RIGHT    │
│              │                                                    │ PANEL    │
│              │                                                    │ 290px    │
└──────────────┴────────────────────────────────────────────────────┴──────────┘
```

---

## B. Panel Derecho — Estructura Completa (290 px de ancho)

```
┌────────────────────────────────────────────┐
│  ▼ CONEXIÓN                                │
│  ◄ COM3 ► [SCAN] [CONNECT]  ● verde        │
│  BT: [COM10 ▼] [Conectar BT] ● rojo       │
│  Python: ● 127.0.0.1:5005 [STOP]          │
├────────────────────────────────────────────┤
│  ▼ CONFIGURACIÓN                           │
│  [200 pasos] [400 pasos]  Resolución: 200  │
│  Step size: [5.0°]                         │
├────────────────────────────────────────────┤
│  ▼ CONTROL (contenido del tab activo)      │
│  ─── E1: ARTICULAR ───────────────────────│
│  Z:  [↑ ARRIBA] [↓ ABAJO]   Z: 0.00 mm   │
│  J1: [◄ IZQ]   [► DER]      J1: -90.0°   │
│  J2: [◄ IZQ]   [► DER]      J2: -90.0°   │
│  Muñeca: ├────────────────┤ 90°           │
│  Garra:  ├──┤ 0°                          │
│  [HOME]  [DEMO]  [SET OFFSET]  [RESUME]   │
├────────────────────────────────────────────┤
│  ▼ TELEMETRÍA                              │
│  θ1: -90.00°   J1: -50 pasos             │
│  θ2: -90.00°   J2: -50 pasos             │
│  Z:   0.00mm   Z:   0 pasos              │
│  TCP X:  -0.9mm  TCP Y: -299.9mm          │
│  Dist: 299.9mm                            │
│  S1: 90°  S2: 0°                          │
│  ─ J1 ─[■■■■■■■■░░░░░░░░]─               │
│  ─ J2 ─[■■■■■■■■░░░░░░░░]─               │
│  ─ Z  ─[░░░░░░░░░░░░░░░░]▓               │
├────────────────────────────────────────────┤
│  ▼ SERIAL LOG                              │
│  [12:34:56] STATUS:-90.00,-90.00,...       │
│  [12:35:01] DEMO:START                    │
│  [12:35:05] GRAB:DONE                     │
│  [EXPORT LOG] [EXPORT TCP] [CLEAR]        │
└────────────────────────────────────────────┘
```

---

## C. Contenido del Tab por Entorno

### E1 — Control Articular (Panel Derecho, sección CONTROL)
- Botones ↑↓ para Z, ◄► para J1, ◄► para J2
- Sliders S1 (muñeca 0–180°) y S2 (garra 0–20°)
- Labels de posición actual
- Botones: HOME, DEMO, EMERGENCY STOP, SET OFFSET, RESUME

### E2 — Control Cinemático (Panel Derecho + mapa central)
- Campos de entrada X, Y, Z (mm)
- Indicador workspace OK/FAIL + resultado IK
- Botón GoTo
- Mapa 2D cartesiano con punto TCP (azul) y objetivo (verde)
- Control por pasos J1/J2 con campos numéricos
- Toggle trayectoria + botón Limpiar

### E3 — Trayectorias (Placeholder en desarrollo)
- Mensaje "En desarrollo — próximamente"
- Dimmed (opacidad reducida en tab)

### E4 — Análisis (Placeholder en desarrollo)
- Mensaje "En desarrollo — próximamente"
- Dimmed en tab

### E5 — Visión Artificial
- Estado visual con color dinámico
- Coordenadas del objeto detectado
- Mapa 2D con puntero objeto (rojo) y TCP (azul)
- Indicador de garra abierta/cerrada
- Barra de progreso de la secuencia (10 pasos)
- Historial de últimas 6 detecciones

---

## D. Top Bar — Elementos Fijos

| Elemento | Tipo | Posición | Función |
|---|---|---|---|
| Logo "SCARA Sim" | `TMP_Text` | Izquierda | Branding |
| Botón E1 | `Button` | Centro | Tab E1 Articular |
| Botón E2 | `Button` | Centro | Tab E2 Cinemática |
| Botón E3 | `Button` | Centro | Tab E3 Trayectorias (dimmed) |
| Botón E4 | `Button` | Centro | Tab E4 Análisis (dimmed) |
| Botón E5 | `Button` | Centro | Tab E5 Visión |
| Indicador Serial | `Image` + `TMP_Text` | Derecha-centro | Verde=conectado, Rojo=desconectado |
| EMERGENCY STOP | `Button` (rojo) | Extremo derecho | Parada inmediata + flag `emergencyStop` |

---

## E. Paleta de Colores (UITheme.cs)

| Rol | Color Hex | RGBA |
|---|---|---|
| Fondo principal | `#0A0D16` | (10,13,22,255) |
| Fondo panel | `#0D111F` | (13,17,31,255) |
| Acento cian | `#00D4FF` | (0,212,255,255) |
| Texto primario | `#FFFFFF` | blanco |
| Texto secundario | `#63748C` | (99,116,140) |
| Indicador OK | `#22C55E` | verde |
| Indicador FAIL | `#EF4444` | rojo |
| Indicador WARN | `#EAB308` | amarillo |
| Emergency button | `#DC2626` | rojo intenso |
| Tab activo | `#0078A5` | azul-cian |
| Tab inactivo | `#0B1222` | azul-oscuro |

---

## F. Generación Procedural de la UI

La interfaz completa se genera ejecutando `UILayoutBuilder` en el Inspector de Unity:

```
1. Adjuntar UILayoutBuilder a un GameObject vacío
2. Asignar en Inspector:
   - robotState → RobotState (ScriptableObject)
   - robotController → RobotController
   - dataLogger → DataLogger
   - cameraSuperior → Camera (vista superior)
3. Click derecho sobre el componente → "Rebuild Full UI"
   O desde menú: SCARA → Rebuild UI

Resultado: Canvas_Main con todos los paneles, referencias auto-asignadas
```
