# Diagrama de Flujo del Proceso Operacional y Señales del Robot SCARA

---

## A. Flujo General del Sistema

```
╔══════════════════════════════════════════════════════════════════╗
║                    INICIO — Unity Play Mode                      ║
╚═══════════════════════════╦══════════════════════════════════════╝
                            ║
                            ▼
            ┌───────────────────────────────┐
            │        SplashScreen           │
            │   Fade in → botón EMPEZAR     │
            └───────────────┬───────────────┘
                            │ onClick()
                            ▼
            ┌───────────────────────────────┐
            │    UILayoutBuilder.BuildUI()  │
            │   Genera Canvas_Main completo │
            │   Auto-asigna todos los refs  │
            └───────────────┬───────────────┘
                            │
            ┌───────────────┴────────────────────────────┐
            │              INICIALIZACIÓN PARALELA        │
            ├─────────────────┬────────────────┬──────────┤
            ▼                 ▼                ▼          ▼
  ┌─────────────────┐ ┌────────────┐ ┌──────────────┐ ┌──────────┐
  │ PythonSocket    │ │ SerialMgr  │ │BluetoothMgr  │ │RobotCtrl │
  │ StartServer()   │ │ GetPorts() │ │  (standby)   │ │ state.   │
  │ Escucha :5005   │ │ ScanPorts  │ │              │ │Initialize│
  └─────────────────┘ └────────────┘ └──────────────┘ └──────────┘
                            │
                            ▼
            ┌───────────────────────────────┐
            │     ¿Usuario conecta serial?   │
            └────────┬──────────────────────┘
                     │ btnConnect
                     ▼
            ┌───────────────────────────────┐
            │   SerialManager.Connect(COM)   │
            │   _port.Open() → ReadLoop()   │
            └───────────────┬───────────────┘
                            │
                            ▼
            ┌───────────────────────────────┐
            │   ESP32 envía: ROBOT_READY    │
            │   Unity: SetImmediate(-90,-90)│
            │   DEMO enviado (delay 2s)     │
            └───────────────┬───────────────┘
                            │
╔═══════════════════════════╩══════════════════════════════════════╗
║                   BUCLE PRINCIPAL (Update)                       ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## B. Flujo de Comando y Señal (Unity → ESP32 → Unity)

```
USUARIO                    UNITY                        ESP32
  │                           │                            │
  │  Presiona botón J1+       │                            │
  │──────────────────────────►│                            │
  │                    UIManager.OnClick()                 │
  │                    robot.MoveMotor2(+5°)               │
  │                           │                            │
  │                    Actualiza _tTheta1                  │
  │                    SerialManager.Send("J:1:5.0:30")    │
  │                           │──────────────────────────►│
  │                           │                   processCommand()
  │                           │                   moveJ1(5.0, 30)
  │                           │                   stepMotor(...)
  │                           │                   sendStatus()
  │                           │◄──────────────────────────│
  │                    STATUS:-85.00,-90.00,0.00,...       │
  │                    HandleMessage()                     │
  │                    ParseStatus()                       │
  │                    UpdateState()                       │
  │                    RobotController.UpdateFromESP32()   │
  │                           │                            │
  │                    LerpModel() → pivotArt1.rotation    │
  │                    TelemetryPanel.RefreshUI()          │
  │◄──────────────────────────│                            │
  │  Visualización 3D         │                            │
  │  actualizada              │                            │
```

---

## C. Flujo de Visión Artificial (Python → TCP → Unity → ESP32)

```
CÁMARA            PYTHON              UNITY (TCP)         ESP32
  │                  │                    │                  │
  │  frame           │                    │                  │
  │─────────────────►│                    │                  │
  │          detectar círculo rojo        │                  │
  │          calcular px→mm              │                  │
  │          "V:100.0:80.0:20.0\n"        │                  │
  │                  │───────────────────►│                  │
  │                  │         PythonSocketServer           │
  │                  │         Execute("V:100.0:80.0:20.0") │
  │                  │         robot.GoToPosition(100,80,20) │
  │                  │         SerialManager.Send(V:...)    │
  │                  │                    │─────────────────►│
  │                  │                    │         grabSequence()
  │                  │                    │◄─────────────────│
  │                  │         "GRAB:START"                  │
  │                  │         "GRAB:LOWER"                 │
  │                  │         "GRAB:CLOSE"                 │
  │                  │         "GRAB:DONE"                  │
  │                  │◄───────────────────│                  │
  │                  │  "OK:V:100.0:80.0:20.0"              │
  │                  │                    │                  │
```

---

## D. Flujo de Estados de Conexión

```
           DESCONECTADO
               │
               │ btnConnect / Connect(COM)
               ▼
           CONECTANDO
               │
               │ _port.Open() OK → ROBOT_READY recibido
               ▼
           CONECTADO ◄─────────── PING cada 4s
               │                      │
               │                  PONG recibido
               │ Error lectura /
               │ Disconnect()
               ▼
           DESCONECTADO
        (state.serialConnected = false)
        (OnConnectionChanged?.Invoke(false))
```

---

## E. Flujo de Control por Entornos

```
                    ┌──────────────────────────────┐
                    │         TABS (E1–E5)          │
                    └──┬──────┬──────┬──────┬───────┘
                       │      │      │      │      │
                      E1     E2     E3     E4     E5
                       │      │      │      │      │
                ┌──────┘      │      │      │      └───────────┐
                │             │      │      │                  │
        Articular       Cinemática  Trayect. Análisis      Visión
        J:1:θ:v         P:x:y:z    TRAJ:PTP  EXP:PREC    V:x:y:z
        J:2:θ:v         TARGET:x,y TRAJ:LIN  EXP:SPEED   TARGET:x,y
        J:3:z:v         KINE:x,y,r TRAJ:ARC  LOG:EXPORT  VISION:RED
        S1/S2            IK en ESP32 ScaraTraj ScaraLogger  grabSequence
```

---

## F. Señales del Sistema

| Señal | Origen | Destino | Frecuencia / Trigger |
|---|---|---|---|
| `STATUS:j1,j2,z,x,y,s1,s2` | ESP32 | Unity | Tras cada movimiento + STATUS cmd |
| `ROBOT_READY` | ESP32 | Unity | Al iniciar (setup completado) |
| `PONG` | ESP32 | Unity | Respuesta a PING cada 4s |
| `KINE:x,y,r` | ESP32 (E2) | Unity | Tras calcular IK |
| `UNREACHABLE:x,y` | ESP32 | Unity | IK sin solución |
| `GRAB:START/DONE` | ESP32 (E5) | Unity | Inicio/fin secuencia agarre |
| `DEMO:START/DONE` | ESP32 | Unity | Inicio/fin demo |
| `OK:CONNECTED` | Unity (TCP) | Python | Al conectarse al socket TCP |
| `STATUS:...` | Unity (TCP) | Python | Respuesta a solicitud STATUS |
| `ACK:TARGET:x,y` | Unity (TCP) | Python | Confirmación TARGET recibido |
