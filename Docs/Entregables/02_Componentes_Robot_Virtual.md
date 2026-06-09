# Tabla de Componentes del Robot Virtual SCARA
## Simulación Unity — SCARA Sim

---

## A. Componentes del Modelo 3D (Jerarquía de Transforms)

| # | GameObject / Transform | Tipo | Movimiento | Equivalente real | Rango |
|---|---|---|---|---|---|
| 1 | `ROBOT_SCARA` (raíz) | Padre | Estático | Base física del robot | — |
| 2 | `ARTICULACION_VERTICAL` | Transform | Traslación local Y | Husillo eje Z (motor paso a paso) | 0–200 mm → 0–110 u |
| 3 | `pivot_art1` | Transform | Rotación local Z | Articulación J1 (hombro) | -90° a +90° |
| 4 | `pivot_art2` | Transform | Rotación local Z | Articulación J2 (codo) | -90° a +90° |
| 5 | `GiroGarra` | Transform | Rotación local Y | Servo S1 (muñeca) | 0° a 180° |
| 6 | `Garra_1` | Transform | Rotación local Z | Servo S2 (dedo 1) | 0° a 20° |
| 7 | `Garra_2` | Transform | Rotación local Z | Servo S2 (dedo 2) | 0° a 20° (invertido) |
| 8 | `TCP` | Transform | Pasivo (sigue cadena) | Tool Center Point | Calculado por FK |

---

## B. Scripts de Control (MonoBehaviours)

| # | Script | Namespace | Patrón | Responsabilidad principal |
|---|---|---|---|---|
| 1 | `RobotController` | `ScaraRobot.Core` | Singleton | Interpola modelo 3D, envía comandos J/P al ESP32 |
| 2 | `SerialManager` | `ScaraRobot.Communication` | Singleton | Comunicación USB serial, parseo STATUS/KINE/TRAJ/CSV |
| 3 | `BluetoothManager` | `ScaraRobot.Communication` | Singleton | Puerto COM virtual Bluetooth, parseo STATUS |
| 4 | `PythonSocketServer` | `ScaraRobot.Communication` | Singleton | Servidor TCP 5005, recibe VISION:RED / TARGET / V:x:y:z |
| 5 | `UIManager` | `ScaraRobot.UI` | Singleton | Gestión de tabs E1–E5, botones, indicadores |
| 6 | `UILayoutBuilder` | `ScaraRobot.UI` | EditorTool | Constructor procedural del Canvas completo |
| 7 | `TelemetryPanel` | `ScaraRobot.UI` | Singleton | Visualiza θ1, θ2, Z, TCP, servos y barras de progreso |
| 8 | `CartesianPanel` | `ScaraRobot.UI` | Singleton | Mapa 2D, trayectoria, control por pasos |
| 9 | `VisionPanel` | `ScaraRobot.UI` | Singleton | Estado de visión, historial, mapa TCP/objeto, garra |
| 10 | `LogPanel` | `ScaraRobot.UI` | — | Historial serial, resumen DataLogger, exportar CSV |
| 11 | `MetricsPanel` | `ScaraRobot.UI` | — | Error actual/promedio/máximo, repetibilidad, gráfico |
| 12 | `TCPVisualizer` | `ScaraRobot.Core` | Singleton | Esfera roja + trail naranja del recorrido TCP |
| 13 | `WorkspaceVisualizer` | `ScaraRobot.Core` | — | Anillo visual del workspace alcanzable |
| 14 | `MetricsManager` | `ScaraRobot.Core` | Singleton | Medición de error, prueba de repetibilidad, exportar CSV |
| 15 | `DataLogger` | `ScaraRobot.Core` | Singleton | Log continuo de STATUS, exportar log y trayectoria TCP |
| 16 | `SplashScreen` | `ScaraRobot.UI` | — | Transición Splash → Main con fade in/out |

---

## C. Activos Estáticos (ScriptableObjects y Assets)

| # | Asset | Tipo | Descripción |
|---|---|---|---|
| 1 | `RobotState` | ScriptableObject | Estado compartido: ángulos, TCP, errores, flags conexión |
| 2 | `RobotConfig` | Clase estática | Constantes de calibración (L1, L2, límites, escala) |
| 3 | `IKSolver` | Clase estática | Cinemática inversa (ley del coseno) y FK planar |
| 4 | `ICommunicationManager` | Interface | Contrato común: Connect / Disconnect / Send |
| 5 | `ROBOT_SCARA.fbx` | Modelo 3D | Malla del robot con jerarquía de articulaciones |
| 6 | `LiberationSans SDF` | TMP FontAsset | Fuente tipográfica UI (TextMeshPro) |

---

## D. Dependencias Entre Componentes

```
RobotState (ScriptableObject)
    ├── RobotController.state
    ├── SerialManager.state
    ├── BluetoothManager.state
    ├── PythonSocketServer.state
    ├── TelemetryPanel.state
    ├── CartesianPanel.state
    ├── VisionPanel.state
    ├── MetricsManager.state
    └── DataLogger.state

RobotController.Instance
    ├── SerialManager     → UpdateFromESP32(), SetTCPFromKine()
    ├── BluetoothManager  → UpdateFromESP32()
    ├── PythonSocketServer → GoToPosition()
    └── UIManager         → robot.GoHome(), robot.GoToPosition()
```
