# Diagrama de Componentes UML
## Robot SCARA Sim — Arquitectura del Sistema

---

## A. Diagrama de Componentes Principal

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            <<system>>                                           │
│                         SCARA Sim (Unity)                                       │
│                                                                                 │
│  ┌──────────────────────────────────┐   ┌──────────────────────────────────┐   │
│  │      <<namespace>>               │   │      <<namespace>>               │   │
│  │    ScaraRobot.Core               │   │  ScaraRobot.Communication        │   │
│  │                                  │   │                                  │   │
│  │  ┌─────────────┐                 │   │  ┌──────────────────────────┐    │   │
│  │  │ RobotConfig │ <<static>>      │   │  │     <<interface>>        │    │   │
│  │  │ L1,L2,Limits│                 │   │  │  ICommunicationManager   │    │   │
│  │  └─────────────┘                 │   │  │ Connect/Disconnect/Send  │    │   │
│  │                                  │   │  └──────────┬───────────────┘    │   │
│  │  ┌─────────────┐                 │   │             │implements          │   │
│  │  │  IKSolver   │ <<static>>      │   │  ┌──────────┴───┐  ┌──────────┐ │   │
│  │  │  Solve(x,y) │                 │   │  │SerialManager │  │Bluetooth │ │   │
│  │  │  FK(θ1,θ2)  │                 │   │  │ReadLoop()    │  │Manager   │ │   │
│  │  └──────┬──────┘                 │   │  │HandleMessage │  │ReadLoop  │ │   │
│  │         │uses                    │   │  └──────┬───────┘  └────┬─────┘ │   │
│  │  ┌──────▼──────┐                 │   │         │               │       │   │
│  │  │  RobotState │ <<SO>>          │   │  ┌──────▼───────────────▼────┐  │   │
│  │  │  motor1_Z   │                 │   │  │    PythonSocketServer     │  │   │
│  │  │  motor2_θ1  │                 │   │  │    TCP :5005              │  │   │
│  │  │  motor3_θ2  │◄────────────────┼───┼──│    ServerLoop()           │  │   │
│  │  │  tcp_X/Y/Z  │                 │   │  └───────────────────────────┘  │   │
│  │  └──────┬──────┘                 │   └──────────────────────────────────┘   │
│  │         │shared by all           │                                           │
│  │  ┌──────▼──────┐                 │   ┌──────────────────────────────────┐   │
│  │  │RobotCtrl    │                 │   │      <<namespace>>               │   │
│  │  │UpdateFromESP│◄────────────────┼───┤    ScaraRobot.UI                 │   │
│  │  │GoToPosition │                 │   │                                  │   │
│  │  │LerpModel()  │                 │   │  ┌──────────┐  ┌──────────────┐  │   │
│  │  └─────────────┘                 │   │  │UIManager │  │TelemetryPanel│  │   │
│  │                                  │   │  │tabs E1-E5│  │RefreshUI()   │  │   │
│  │  ┌─────────────┐                 │   │  └──────────┘  └──────────────┘  │   │
│  │  │TCPVisualizer│                 │   │  ┌──────────┐  ┌──────────────┐  │   │
│  │  │Sphere+Trail │                 │   │  │Cartesian │  │ VisionPanel  │  │   │
│  │  └─────────────┘                 │   │  │Panel     │  │ GRAB states  │  │   │
│  │                                  │   │  └──────────┘  └──────────────┘  │   │
│  │  ┌─────────────┐                 │   │  ┌──────────┐  ┌──────────────┐  │   │
│  │  │MetricsManager│                │   │  │LogPanel  │  │ MetricsPanel │  │   │
│  │  │MeasureError │                 │   │  │AddLine() │  │ Charts       │  │   │
│  │  └─────────────┘                 │   │  └──────────┘  └──────────────┘  │   │
│  │                                  │   └──────────────────────────────────┘   │
│  │  ┌─────────────┐                 │                                           │
│  │  │ DataLogger  │                 │   ┌──────────────────────────────────┐   │
│  │  │ LogRecord   │                 │   │      <<namespace>>               │   │
│  │  │ ExportCSV() │                 │   │    ScaraRobot.Editor             │   │
│  │  └─────────────┘                 │   │  CreateMetricsPanel              │   │
│  └──────────────────────────────────┘   └──────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## B. Diagrama de Componentes Externos

```
┌────────────────────────┐         ┌─────────────────────────┐
│   Python (OpenCV)      │         │        ESP32            │
│   vision_compleja.py   │         │   E1/E2/E3/E4/E5.ino    │
│                        │  TCP    │                          │
│   detectRedCircle()    │◄───────►│   ScaraKinematics.h      │
│   px → mm calibración  │  :5005  │   ScaraTrajectory.h      │
│   send("V:x:y:z\n")   │         │   ScaraLogger.h          │
└────────────────────────┘         │                          │
                                   │   stepMotor()            │
┌────────────────────────┐  USB    │   moveJ1/J2/Z()          │
│   Unity (C#)           │◄───────►│   grabSequence()         │
│   SerialManager        │  115200 │   sendStatus()           │
│   BluetoothManager     │         │                          │
│   PythonSocketServer   │  BT     └─────────────────────────┘
│   RobotController      │◄───────►  Drivers A4988
│   IKSolver             │  SPP    ┌─────────────────────────┐
│   UIManager            │         │  Motores NEMA-17 (×3)   │
└────────────────────────┘         │  Servos SG90 (×2)       │
                                   └─────────────────────────┘
```

---

## C. Diagrama de Secuencia de Arranque

```
Unity         SplashScreen    UILayoutBuilder   SerialManager   ESP32
  │                │                │                │            │
  │   Start()      │                │                │            │
  │───────────────►│                │                │            │
  │                │ fade in        │                │            │
  │                │◄──────────────►│                │            │
  │                │ click EMPEZAR  │                │            │
  │                │───────────────►│                │            │
  │                │         BuildUI()               │            │
  │                │         Canvas_Main creado      │            │
  │                │                │ ScanPorts()    │            │
  │                │                │───────────────►│            │
  │                │                │  GetPortNames()│            │
  │                │                │◄───────────────│            │
  │                │                │ {"COM3","COM5"}│            │
  │      click CONNECT              │                │            │
  │────────────────────────────────────────────────►│            │
  │                │                │    Connect("COM3")          │
  │                │                │                │ _port.Open│
  │                │                │                │───────────►│
  │                │                │                │  ROBOT_READY
  │                │                │                │◄───────────│
  │                │           SetImmediate(-90,-90)  │            │
  │                │                │                │            │
  │  (2s delay)    │                │          Send("DEMO")       │
  │                │                │                │───────────►│
  │                │                │                │  DEMO:START│
  │                │                │                │◄───────────│
  │                │                │                │  STATUS:...│
  │                │                │                │◄───────────│
```

---

## D. Interfaces y Dependencias (tabla)

| Componente | Depende de | Expone |
|---|---|---|
| `RobotController` | `RobotState`, `SerialManager`, `BluetoothManager`, `UIManager` | `UpdateFromESP32()`, `GoToPosition()`, `SetTCPFromKine()` |
| `SerialManager` | `RobotState`, `UIManager`, `RobotController`, `DataLogger` | `Connect()`, `Disconnect()`, `Send()`, `OnMessage`, `OnConnectionChanged` |
| `BluetoothManager` | `RobotState`, `UIManager`, `RobotController`, `DataLogger` | `Connect()`, `Disconnect()`, `Send()`, `OnConnectionChanged` |
| `PythonSocketServer` | `RobotState`, `RobotController`, `SerialManager`, `BluetoothManager`, `VisionPanel` | `StartServer()`, `StopServer()`, `SendToPython()`, `OnCommand` |
| `UIManager` | `RobotState`, `RobotController`, `SerialManager`, `BluetoothManager`, `PythonSocketServer`, `CartesianPanel` | `ShowError()`, `CambiarEntorno()` |
| `TelemetryPanel` | `RobotState`, `SerialManager` | `RefreshUI()` |
| `VisionPanel` | `RobotState`, `SerialManager`, `IKSolver`, `RobotConfig` | `OnObjectDetected()` |
| `CartesianPanel` | `RobotState`, `IKSolver`, `RobotConfig`, `RobotController`, `TCPVisualizer` | `MostrarObjetivo()`, `LimpiarTrayectoria()` |
| `MetricsManager` | `RobotState`, `IKSolver`, `RobotConfig`, `RobotController` | `MeasureCurrentError()`, `StartRepetibilityTest()`, `ExportToCSV()` |
| `DataLogger` | `RobotState`, `SerialManager` | `LogCurrentState()`, `ExportFullLog()` |
| `IKSolver` | `RobotConfig` | `Solve()`, `ForwardKinematics()`, `IsReachable()` |
