# Panel de Control por Visión Artificial
## Entorno E5 — VisionPanel

---

## A. Descripción del Sistema

El panel de visión artificial conecta tres componentes:
1. **Python (OpenCV)** — detecta objetos con cámara real
2. **Unity (TCP socket)** — recibe coordenadas y gestiona la lógica
3. **ESP32 (Serial/BT)** — ejecuta el agarre físico

---

## B. Elementos del Panel (Inspector)

| Campo Inspector | Tipo | Función |
|---|---|---|
| `lblEstado` | `TMP_Text` | Estado actual: "Detectando", "Moviendo", "Agarrando", "Listo" |
| `imgEstadoColor` | `Image` | Indicador de color según estado (amarillo/azul/verde/gris) |
| `lblObjCoords` | `TMP_Text` | "Objeto detectado X: 100.0 mm Y: 80.0 mm Z: 20.0 mm" |
| `lblHistorial` | `TMP_Text` | Últimas 6 detecciones con timestamp |
| `panelMapa2D` | `RectTransform` | Vista superior del workspace |
| `punteroObjeto` | `RectTransform` | Punto rojo — posición del objeto detectado |
| `punteroTCP` | `RectTransform` | Punto azul — posición TCP actual |
| `imgGarraAbierta` | `Image` | Visible cuando servo2 > 5° |
| `imgGarraCerrada` | `Image` | Visible cuando servo2 ≤ 5° |
| `lblGarraStatus` | `TMP_Text` | "Garra: Abierta" / "Garra: Cerrada" |
| `barraProgreso` | `Image` | Progreso de secuencia de agarre (Image.Type.Filled) |
| `lblPasoActual` | `TMP_Text` | "Paso 3/10: Bajando al objeto" |

---

## C. Estados Visuales del Panel

| Estado | Color | Mensajes GRAB que lo activan |
|---|---|---|
| Listo | Gris `(0.6, 0.6, 0.6)` | Inicio, `OK:DEMO` |
| Detectando | Amarillo `(0.95, 0.72, 0.10)` | Objeto detectado, `GRAB:START` |
| Moviendo | Azul `(0.18, 0.55, 0.95)` | `GRAB:OPEN`, `GRAB:APPROACH`, `GRAB:LOWER` |
| Agarrando | Verde `(0.18, 0.88, 0.28)` | `GRAB:CLOSE`, `GRAB:LIFT`, `GRAB:DEPOSIT` |
| Completado | Gris | `GRAB:DONE` |
| Error | Rojo `(0.90, 0.20, 0.20)` | `ERR:...` |

---

## D. Scripts de Configuración Unity

### VisionPanelBuilder.cs (generador procedural)

```
ContextMenu: "GENERAR PANEL VISION"

Jerarquía generada:
  VisionPanel/
    lblEstado          (TMP blanco, "Listo")
    imgEstadoColor     (Image verde 20×20 px)
    lblObjCoords       (TMP blanco, "Objeto: ---")
    lblHistorial       (TMP blanco, "Historial:")
    PanelMapa2D        (Image negro, LayoutElement h=150)
      PunteroObjeto    (Image rojo, 14×14 px, centrado)
      PunteroTCP       (Image azul, 10×10 px, centrado)
    lblGarraStatus     (TMP blanco, "Garra: Cerrada")
    BarraProgreso      (Image verde filled, h=15)
    lblPasoActual      (TMP blanco, "Paso 0/10")
```

### VisionPanel.cs — Suscripción de eventos

```csharp
void Start()
{
    // Escuchar mensajes del ESP32 para actualizar estado de agarre
    if (SerialManager.Instance != null)
        SerialManager.Instance.OnMessage += OnSerialMessage;

    SetEstado("Listo", C_LISTO);
    UpdateMapa2D();
}

void OnSerialMessage(string msg)
{
    switch (msg) {
        case "GRAB:START":    SetEstado("Iniciando agarre", C_DETECTANDO); SetPaso(1); break;
        case "GRAB:OPEN":     SetEstado("Abriendo garra",   C_MOVIENDO);   SetPaso(2); break;
        case "GRAB:APPROACH": SetEstado("Acercándose",      C_MOVIENDO);   SetPaso(3); break;
        case "GRAB:LOWER":    SetEstado("Bajando al objeto", C_MOVIENDO);  SetPaso(5); break;
        case "GRAB:CLOSE":    SetEstado("Cerrando garra",   C_AGARRANDO);  SetPaso(6); break;
        case "GRAB:LIFT":     SetEstado("Levantando objeto", C_AGARRANDO); SetPaso(7); break;
        case "GRAB:DEPOSIT":  SetEstado("Depositando",      C_AGARRANDO);  SetPaso(8); break;
        case "GRAB:DONE":     SetEstado("Agarre completado", C_LISTO);     SetPaso(10); break;
    }
}
```

### PythonSocketServer.cs — Comando V:x:y:z

```csharp
if (cmd.StartsWith("V:")) {
    // Parsear coordenadas
    float x, y, z;   // z = RobotConfig.DefaultVisionZ si no se provee

    // Enviar al ESP32
    SerialManager.Instance?.Send($"V:{x:F1}:{y:F1}:{z:F1}");
    BluetoothManager.Instance?.Send($"V:{x:F1}:{y:F1}:{z:F1}");

    // Mover modelo 3D
    robot?.GoToPosition(x, y, z);

    // Notificar panel de visión
    VisionPanel.Instance?.OnObjectDetected(x, y, z);

    // Confirmar a Python
    SendToPython($"OK:V:{x:F1}:{y:F1}:{z:F1}");
}
```

---

## E. Código Arduino Requerido — E5_Vision.ino

### Secuencia Completa de Agarre (7 pasos)

```arduino
void grabSequence(float t1, float t2, float objZ)
{
    Serial.println("GRAB:START");

    // Paso 1-3: Posicionarse sobre el objeto a altura de transporte
    moveZ(Z_TRANSPORT_MM, 5.0f);   // Z=80mm (altura segura)
    moveJ1(t1, 30.0f);             // Mover J1 (θ1 calculado por IK)
    moveJ2(t2, 30.0f);             // Mover J2 (θ2 calculado por IK)
    sendStatus();

    // Paso 4: Bajar al objeto
    moveZ(objZ, 3.0f);             // Z=20mm (altura de agarre)
    sendStatus();

    // Paso 5: Cerrar pinza
    setServo2(20);                 // S2=20° → garra cerrada
    delay(500);
    sendStatus();

    // Paso 6: Subir con objeto
    moveZ(Z_TRANSPORT_MM, 5.0f);
    sendStatus();

    // Paso 7: Ir a zona de depósito (0°, 0°)
    moveJ1(0.0f, 25.0f);
    moveJ2(0.0f, 25.0f);
    sendStatus();

    // Paso 8-9: Bajar, soltar y regresar
    moveZ(20.0f, 3.0f);
    setServo2(0);                  // S2=0° → garra abierta
    delay(300);
    moveZ(Z_TRANSPORT_MM, 5.0f);
    goHome();

    Serial.println("GRAB:DONE");
}
```

---

## F. Script Python — vision_compleja.py

### Parámetros de invocación

```bash
python vision_compleja.py \
    --mm-per-px 0.5      \   # Calibración píxeles → mm
    --origin-x  320      \   # Centro de cámara en píxeles (x)
    --origin-y  240      \   # Centro de cámara en píxeles (y)
    --cam       0        \   # Índice de cámara (default: webcam)
    --grab               \   # Activar envío de V:x:y:z (agarre completo)
```

### Flujo del script

```python
# 1. Conectar al socket TCP de Unity
sock = socket.connect(("127.0.0.1", 5005))
# Unity responde: OK:CONNECTED

# 2. Loop principal OpenCV
while True:
    frame = cap.read()

    # Detectar círculo rojo (HSV + HoughCircles)
    circles = detectRedCircle(frame)

    if circles:
        # Convertir píxeles → milímetros
        px_x, px_y = circles[0]
        mm_x = (px_x - origin_x) * mm_per_px
        mm_y = (px_y - origin_y) * mm_per_px

        if --grab:
            sock.send(f"V:{mm_x:.1f}:{mm_y:.1f}:{z:.1f}\n")
        else:
            sock.send(f"TARGET:{mm_x:.1f},{mm_y:.1f}\n")

    # Hilo receptor: recibe STATUS / UNREACHABLE / ACK_TARGET de Unity
```

### Protocolo de mensajes Python ↔ Unity (TCP port 5005)

| Dirección | Mensaje | Significado |
|---|---|---|
| Unity → Python | `OK:CONNECTED` | Confirmación de conexión |
| Python → Unity | `V:x:y:z` | Objeto detectado, ejecutar agarre completo |
| Python → Unity | `TARGET:x,y` | Objeto detectado, solo posicionar |
| Python → Unity | `VISION:RED` | Círculo rojo detectado (solo J1 a 90°) |
| Python → Unity | `STATUS` | Solicitar estado actual |
| Unity → Python | `OK:V:x:y:z` | Confirmación de comando V recibido |
| Unity → Python | `ACK:TARGET:x,y` | Confirmación de TARGET recibido |
| Unity → Python | `STATUS:j1,j2,z,x,y,s1,s2` | Estado actual del robot |
| Unity → Python | `ACK:VISION_RED` | Confirmación VISION:RED recibido |
