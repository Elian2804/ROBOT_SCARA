# Diagrama de Casos de Uso
## Robot SCARA — Sistema de Simulación y Control

---

## A. Diagrama Principal (UML texto)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    Sistema SCARA Sim (Unity + ESP32)                         │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │                         <<subsystem>>                                │    │
│  │                    Interfaz de Control                               │    │
│  │                                                                      │    │
│  │   (UC-01) Conectar / Desconectar Serial                              │    │
│  │   (UC-02) Conectar / Desconectar Bluetooth                           │    │
│  │   (UC-03) Controlar articulaciones manualmente                       │    │
│  │   (UC-04) Mover a posición cartesiana                                │    │
│  │   (UC-05) Ejecutar HOME                                              │    │
│  │   (UC-06) Ejecutar DEMO                                              │    │
│  │   (UC-07) Activar EMERGENCY STOP                                     │    │
│  │   (UC-08) Monitorear telemetría en tiempo real                       │    │
│  │   (UC-09) Visualizar mapa 2D y trayectoria TCP                       │    │
│  │   (UC-10) Controlar por pasos directos (J1, J2)                      │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │                    Módulo de Visión Artificial                        │    │
│  │                                                                      │    │
│  │   (UC-11) Recibir coordenadas de objeto detectado                    │    │
│  │   (UC-12) Ejecutar secuencia de agarre automático                    │    │
│  │   (UC-13) Mostrar historial de detecciones                           │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │                    Módulo de Métricas                                 │    │
│  │                                                                      │    │
│  │   (UC-14) Medir error posicional actual                              │    │
│  │   (UC-15) Ejecutar prueba de repetibilidad                           │    │
│  │   (UC-16) Calcular resolución espacial                               │    │
│  │   (UC-17) Exportar datos a CSV                                       │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## B. Actores del Sistema

```
┌─────────────────────────────────────────────────────────┐
│  ACTORES                                                 │
├──────────────────────┬──────────────────────────────────┤
│  Operador            │ Persona que controla el robot     │
│                      │ desde la interfaz Unity           │
├──────────────────────┼──────────────────────────────────┤
│  Investigador        │ Usa E4 para medir precisión y     │
│                      │ exportar datos de análisis        │
├──────────────────────┼──────────────────────────────────┤
│  Sistema Python      │ Script externo de visión que      │
│                      │ envía coordenadas via TCP         │
├──────────────────────┼──────────────────────────────────┤
│  ESP32               │ Microcontrolador embebido que     │
│                      │ ejecuta los movimientos reales    │
└──────────────────────┴──────────────────────────────────┘
```

---

## C. Relaciones Actor–Caso de Uso

```
Operador ──────────────┬── UC-01  Conectar/Desconectar Serial
                       ├── UC-02  Conectar/Desconectar Bluetooth
                       ├── UC-03  Controlar articulaciones (E1)
                       ├── UC-04  Mover a posición cartesiana (E2)
                       ├── UC-05  Ejecutar HOME
                       ├── UC-06  Ejecutar DEMO
                       ├── UC-07  EMERGENCY STOP
                       ├── UC-08  Monitorear telemetría
                       ├── UC-09  Ver mapa 2D
                       └── UC-10  Control por pasos

Investigador ──────────┬── UC-14  Medir error posicional
                       ├── UC-15  Prueba de repetibilidad
                       ├── UC-16  Calcular resolución espacial
                       └── UC-17  Exportar CSV

Sistema Python ─────────── UC-11  Enviar coordenadas objeto detectado

(UC-11) ──include──► UC-12  Ejecutar secuencia de agarre automático
(UC-12) ──include──► UC-13  Mostrar historial de detecciones

(UC-03) ──extend──►  UC-08  (la telemetría se actualiza con cualquier movimiento)
(UC-04) ──extend──►  UC-09  (el mapa muestra el objetivo al usar IK)
```

---

## D. Tabla de Casos de Uso Detallados

| ID | Nombre | Actor | Precondición | Flujo principal | Postcondición |
|---|---|---|---|---|---|
| UC-01 | Conectar Serial | Operador | Puerto COM disponible | 1. Escanear puertos · 2. Seleccionar COM · 3. Click CONNECT | `state.serialConnected = true`; indicador verde |
| UC-02 | Conectar BT | Operador | ROBOT_SCARA emparejado | 1. Seleccionar COM-BT · 2. Click Conectar BT | `state.bluetoothConnected = true` |
| UC-03 | Control articular | Operador | Serial o BT conectado | 1. Seleccionar tab E1 · 2. Presionar btn+/− · 3. Ajustar servo con slider | ESP32 mueve articulación; STATUS retorna; modelo 3D actualiza |
| UC-04 | Mover cartesiano | Operador | Serial o BT conectado | 1. Tab E2 · 2. Ingresar X,Y,Z · 3. Verificar indicador IK · 4. Click GoTo | ESP32 ejecuta P:x:y:z; modelo alcanza posición |
| UC-05 | HOME | Operador | Conectado | Click HOME | Robot va a (-90°,-90°,0mm); modelo reset |
| UC-06 | DEMO | Operador | Conectado | Click DEMO | ESP32 recorre secuencia predefinida; DEMO:START … DEMO:DONE |
| UC-07 | EMERGENCY STOP | Operador | En operación | Click STOP (TopBar) | `emergencyStop=true`; STOP enviado a ESP32; LerpModel bloqueado |
| UC-08 | Telemetría | Operador | Conectado | Pasivo (automático cada frame) | TelemetryPanel muestra θ1,θ2,Z,TCP,S1,S2 actualizados |
| UC-09 | Mapa 2D | Operador | Tab E2 visible | Pasivo (automático) | Punto TCP se mueve en plano XY; trayectoria se dibuja |
| UC-10 | Control por pasos | Operador | Tab E2, conectado | 1. Ingresar nº pasos · 2. Click Ir / +1 / -1 | Motor mueve exactamente N pasos; actualiza UI |
| UC-11 | Recibir visión | Python | TCP socket activo | Python envía V:x:y:z o TARGET:x,y | Unity retransmite a ESP32; VisionPanel muestra objeto |
| UC-12 | Agarre automático | Python, ESP32 | UC-11 completado | ESP32 ejecuta grabSequence() | GRAB:DONE recibido; objeto recogido y depositado |
| UC-13 | Historial detecciones | Operador | Tab E5 visible | Pasivo | Últimas 6 detecciones listadas con hora |
| UC-14 | Medir error | Investigador | hasTarget=true | Click "Medir" · FK calcula TCP real · dist(real,target) | Registro añadido; error mostrado en métricas |
| UC-15 | Repetibilidad | Investigador | Robot conectado | Click "Test Rep." · N ciclos HOME→Target→Medir | Variación calculada; gráfico de barras |
| UC-16 | Resolución espacial | Investigador | Robot conectado | Click "Medir Resolución" · FK(θ+1paso) - FK(θ) | mm/paso mostrado |
| UC-17 | Exportar CSV | Investigador | Registros disponibles | Click "Exportar CSV" | Archivo .csv en persistentDataPath |
