# Tabla de Funcionalidades y Entornos
## Robot SCARA Sim — Cinco Entornos de Operación

---

## A. Resumen de Entornos

| Entorno | Nombre | Tab | Estado | Sketch Arduino | Descripción |
|---|---|---|---|---|---|
| **E1** | Control Articular | E1 Articular | Activo ✅ | `E1_Articular.ino` | Movimiento manual de cada articulación con botones y sliders |
| **E2** | Control Cinemático | E2 Cinemática | Activo ✅ | `E2_Cinematica.ino` | IK en ESP32; entrada por coordenadas cartesianas X,Y,Z |
| **E3** | Trayectorias | E3 Trayect. | Placeholder ⬜ | `E3_Trayectorias.ino` | Trayectorias PTP/lineal/circular vía ScaraTrajectory |
| **E4** | Análisis | E4 Análisis | Placeholder ⬜ | `E4_Analisis.ino` | Métricas de precisión, repetibilidad y exportar logs |
| **E5** | Visión Artificial | E5 Vision | Activo ✅ | `E5_Vision.ino` | Detección de objetos y agarre automático guiado por Python |

---

## B. Funcionalidades por Entorno

### E1 — Control Articular Directo

| Funcionalidad | Comando enviado | Respuesta | Script Unity |
|---|---|---|---|
| Mover J1 (hombro) ±step | `J:1:±θ:speed` | `STATUS:...` | `UIManager.btnM2R/L` |
| Mover J2 (codo) ±step | `J:2:±θ:speed` | `STATUS:...` | `UIManager.btnM3R/L` |
| Mover eje Z ±step | `J:3:±z:speed` | `STATUS:...` | `UIManager.btnM1Up/Down` |
| Control servo muñeca (S1) | `S1:α` | `STATUS:...` | `UIManager.sliderS1` |
| Control servo garra (S2) | `S2:β` | `STATUS:...` | `UIManager.sliderS2` |
| Ir a HOME | `HOME` | `STATUS:...` | `UIManager.btnHome` |
| Ejecutar DEMO | `DEMO` | `DEMO:START` … `DEMO:DONE` | `UIManager.btnDemo` |
| Parada emergencia | `STOP` | — | `UIManager.btnStop` |
| Configurar resolución | 200/400 pasos | — | `UIManager.btn200/400` |

### E2 — Control Cinemático (IK)

| Funcionalidad | Comando enviado | Respuesta | Script Unity |
|---|---|---|---|
| Mover a posición XYZ | `P:x:y:z` | `KINE:x,y,1` o `UNREACHABLE:x,y` | `UIManager.OnGoTo()` |
| Validar workspace en tiempo real | — (solo visual) | Indicador OK/FAIL | `UIManager.UpdateWorkspaceIndicator()` |
| Retransmitir TARGET desde Python | `TARGET:x,y` | `ACK_TARGET:x,y` | `PythonSocketServer.Execute()` |
| Mostrar mapa cartesiano 2D | — (FK continuo) | — | `CartesianPanel.ActualizarTCPenMapa()` |
| Dibujar trayectoria TCP | — (pasivo) | — | `CartesianPanel.AgregarPuntoTrayectoria()` |
| Control por pasos directos | `J:1:steps` / `J:2:steps` | `STATUS:...` | `CartesianPanel.OnIrPasosJ1/J2()` |

### E3 — Trayectorias (Placeholder — en desarrollo)

| Funcionalidad | Comando | Respuesta | Librería Arduino |
|---|---|---|---|
| Trayectoria punto a punto | `TRAJ:PTP:x1,y1,z1:x2,y2,z2` | `TRAJ:step,total,PTP,x,y` | `ScaraTrajectory` |
| Trayectoria lineal | `TRAJ:LIN:x1,y1:x2,y2:steps` | `TRAJ:step,total,LIN,x,y` | `ScaraTrajectory` |
| Trayectoria circular | `TRAJ:ARC:cx,cy:r:a0:a1:steps` | `TRAJ:step,total,ARC,x,y` | `ScaraTrajectory` |
| Completado de trayectoria | — | `TRAJ_DONE` | `ScaraTrajectory` |

### E4 — Análisis Experimental (Placeholder — en desarrollo)

| Funcionalidad | Comando | Respuesta | Script Unity |
|---|---|---|---|
| Prueba de precisión | `EXP:PRECISION` | `PRECISION_TEST:...` | `MetricsManager` |
| Prueba de velocidad | `EXP:SPEED` | `SPEED_TEST:...` | `MetricsManager` |
| Exportar log del ESP32 | `LOG:EXPORT` | `CSV_START` / `CSV:...` / `CSV_END` | `SerialManager.SaveCSV()` |
| Medir error posicional | (interno Unity) | — | `MetricsManager.MeasureCurrentError()` |
| Test repetibilidad | (interno Unity) | — | `MetricsManager.StartRepetibilityTest()` |

### E5 — Visión Artificial

| Funcionalidad | Comando recibido de Python | Retransmitido a ESP32 | Respuesta |
|---|---|---|---|
| Detección círculo rojo | `VISION:RED` (TCP) | `VISION:RED` (serial) | `ACK:VISION_RED` → ESP32 mueve J1 a 90° |
| Target xy detectado | `TARGET:x,y` (TCP) | `TARGET:x,y` (serial) | `ACK:TARGET:x,y` → ESP32 IK+mover |
| Agarre completo xyz | `V:x:y:z` (TCP) | `V:x:y:z` (serial) | `GRAB:START/OPEN/APPROACH/LOWER/CLOSE/LIFT/DEPOSIT/DONE` |
| Estado actual | `STATUS` (TCP) | — | `STATUS:j1,j2,z,x,y,s1,s2` (TCP) |
| Indicador garra | — (pasivo) | — | `VisionPanel.UpdateGarraVisual()` |

---

## C. Funcionalidades Comunes a Todos los Entornos

| Funcionalidad | Descripción | Script |
|---|---|---|
| Telemetría en tiempo real | θ1, θ2, Z, TCP X/Y, S1, S2 + barras de progreso | `TelemetryPanel` |
| Log serial | Últimos 20 mensajes con timestamp | `LogPanel` |
| Indicadores de conexión | USB / BT / Python — verde/rojo | `UIManager` |
| Vista superior (top view) | Toggle entre perspectiva y plano XY | `UIManager.ToggleTopView()` |
| Mini-cámara | RenderTexture de cámara superior en panel lateral | `UIManager` |
| EMERGENCY STOP | Bloquea modelo 3D y envía STOP | `UIManager` + `RobotController` |
| HOME | Retorna a posición inicial (-90,-90,0) | `RobotController.GoHome()` |
| Trail TCP | Esfera roja + rastro naranja del recorrido | `TCPVisualizer` |
| Exportar CSV | Registro de telemetría y métricas | `DataLogger`, `MetricsManager` |
