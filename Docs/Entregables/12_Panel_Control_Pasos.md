# Panel de Control por Pasos
## Entorno E2 — CartesianPanel

---

## A. Descripción del Panel

El **Panel de Control por Pasos** (tab E2 — Cinemática) permite mover las articulaciones J1 y J2 especificando directamente el número de pasos del motor, y ver la posición TCP resultante en un mapa 2D en tiempo real.

---

## B. Elementos del Panel (Inspector)

| Campo Inspector | Tipo | Función |
|---|---|---|
| `mapaContainer` | `RectTransform` | Contenedor del plano 2D (fondo negro, cuadrícula) |
| `tcpPunto` | `RectTransform` | Punto azul del TCP en tiempo real |
| `objetivoPunto` | `RectTransform` | Punto verde del objetivo actual |
| `lblTCPx` | `TMP_Text` | "TCP X: 212.30 mm" |
| `lblTCPy` | `TMP_Text` | "TCP Y: 88.10 mm" |
| `lblDistOrigen` | `TMP_Text` | "Dist: 229.50 mm" |
| `lblJ1` | `TMP_Text` | "J1: 45.00°" |
| `lblJ2` | `TMP_Text` | "J2: -45.00°" |
| `inputPasosJ1` | `TMP_InputField` | Número de pasos para J1 |
| `inputPasosJ2` | `TMP_InputField` | Número de pasos para J2 |
| `btnIrPasosJ1` | `Button` | Mover J1 a N pasos |
| `btnIrPasosJ2` | `Button` | Mover J2 a N pasos |
| `btnPasoMasJ1` | `Button` | +1 paso en J1 |
| `btnPasoMenosJ1` | `Button` | -1 paso en J1 |
| `btnPasoMasJ2` | `Button` | +1 paso en J2 |
| `btnPasoMenosJ2` | `Button` | -1 paso en J2 |
| `lblPasosActualesJ1` | `TMP_Text` | "J1: -50 pasos" |
| `lblPasosActualesJ2` | `TMP_Text` | "J2: -50 pasos" |
| `btnLimpiarTrayectoria` | `Button` | Elimina puntos de trail del mapa |
| `toggleTrayectoria` | `Toggle` | Activa/desactiva dibujo de trayectoria |
| `imgWorkspaceCirculo` | `Image` | Círculo azul translúcido del workspace |

---

## C. Lógica de Conversión Pasos ↔ Grados

```csharp
// Pasos → Grados
float grados = pasos / RobotConfig.StepsPerDeg;   // 0.5556 pasos/°

// +1 paso en J1
float unGrado = 1f / RobotConfig.StepsPerDeg;     // = 1.8°
RobotController.Instance?.MoveJ1(unGrado * signo);
```

**Referencia:** `RobotConfig.StepsPerDeg = 200 / 360 = 0.5556 pasos/°`

---

## D. Script Unity — CartesianPanel.cs (fragmento clave)

```csharp
// Ir a N pasos en J1
void OnIrPasosJ1()
{
    if (!int.TryParse(inputPasosJ1?.text, out int pasos)) return;
    float grados = pasos / RobotConfig.StepsPerDeg;
    RobotController.Instance?.SetJ1(grados);
}

// +1/-1 paso en J1 o J2
void MoverUnPaso(int joint, int signo)
{
    float unGrado = 1f / RobotConfig.StepsPerDeg;
    if (joint == 1) RobotController.Instance?.MoveJ1(unGrado * signo);
    if (joint == 2) RobotController.Instance?.MoveJ2(unGrado * signo);
}
```

---

## E. Script de Configuración del Panel (CartesianPanelBuilder.cs)

El archivo `CartesianPanelBuilder.cs` (namespace `ScaraRobot.UI`) genera el panel proceduralmente:

```
ContextMenu: "GENERAR PANEL CARTESIANO"

Jerarquía generada:
  CartesianPanel/
    MapaContainer        (Image negro, LayoutElement h=200)
      TCPPunto           (Image azul, 12×12 px, centrado)
      ObjetivoPunto      (Image verde, 12×12 px, inactivo)
      WorkspaceCirculo   (Image azul translúcido, 200×200 px)
    PanelCoords          (VerticalLayoutGroup)
      lblTCPx, lblTCPy, lblDistOrigen, lblJ1, lblJ2
    PanelPasos           (VerticalLayoutGroup)
      FilaJ1             (HorizontalLayoutGroup)
        lblJ1, inputPasosJ1, btnIrPasosJ1, btnPasoMasJ1, btnPasoMenosJ1
        lblPasosActualesJ1
      FilaJ2             (HorizontalLayoutGroup)
        lblJ2, inputPasosJ2, btnIrPasosJ2, btnPasoMasJ2, btnPasoMenosJ2
        lblPasosActualesJ2
    toggleTrayectoria
    btnLimpiarTrayectoria
```

---

## F. Código Arduino Requerido (E2_Cinematica.ino)

El Panel de Control por Pasos en Unity se comunica con el sketch **E2_Cinematica.ino**.
Los comandos J:1 y J:2 son procesados por `processCommand()`:

```arduino
// Comando: J:1:angle:speed  →  mueve J1 a `angle` grados a velocidad `speed` °/s
void processCommand(String cmd)
{
    if (cmd.startsWith("J:")) {
        int c1 = cmd.indexOf(':', 2);
        int c2 = cmd.indexOf(':', c1 + 1);
        int joint = cmd.substring(2, c1).toInt();
        float angle = cmd.substring(c1 + 1, c2).toFloat();
        float speed = cmd.substring(c2 + 1).toFloat();
        switch (joint) {
            case 1: moveJ1(angle, speed); break;
            case 2: moveJ2(angle, speed); break;
            case 3: moveZ(angle, speed);  break;
        }
        sendStatus();
    }
}

// Conversión grados → pasos → pulsos
void moveJ1(float targetDeg, float speedDeg_s)
{
    targetDeg = constrain(targetDeg, -90.0f, 90.0f);
    float delta = targetDeg - gJ1_deg;
    long steps = (long)(abs(delta) * STEPS_PER_DEG);     // 0.5556 pasos/°
    unsigned long stepDelay = (unsigned long)(1e6f / (speedDeg_s * STEPS_PER_DEG));
    digitalWrite(PIN_J1_DIR, delta >= 0 ? HIGH : LOW);
    stepMotor(PIN_J1_STEP, PIN_J1_DIR, steps, stepDelay);
    gJ1_deg = targetDeg;
}
```

**Pines del motor J1:** STEP=18, DIR=19, EN=21 (driver A4988/DRV8825, ENABLE LOW)
