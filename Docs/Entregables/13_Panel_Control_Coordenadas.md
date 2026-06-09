# Panel de Control por Coordenadas
## Entorno E2 — Control Cinemático (IK)

---

## A. Descripción del Panel

El **Panel de Control por Coordenadas** (parte del tab E2) permite enviar una posición cartesiana (X, Y, Z) en milímetros. La cinemática inversa se calcula tanto en Unity (visualización) como en el ESP32 (control real). El panel valida el workspace en tiempo real antes de ejecutar el movimiento.

---

## B. Elementos del Panel (Inspector UIManager)

| Campo Inspector | Tipo | Función |
|---|---|---|
| `inputX` | `TMP_InputField` | Coordenada X en mm |
| `inputY` | `TMP_InputField` | Coordenada Y en mm |
| `inputZ` | `TMP_InputField` | Coordenada Z en mm |
| `btnGoTo` | `Button` | Ejecutar movimiento a XYZ |
| `workspaceImg` | `Image` | Indicador verde/rojo de alcanzabilidad |
| `ikStatusText` | `TMP_Text` | "OK θ1=45.2° θ2=-38.7°" o error IK |

---

## C. Flujo de Control IK (Unity + ESP32)

```
Usuario ingresa X=100, Y=80, Z=50
          │
          ▼
IKSolver.Solve(100, 80, 50)          ← Unity (C#)
  dist = √(100²+80²) = 128.06 mm
  cos(θ2) = (dist²-L1²-L2²)/(2·L1·L2)
  θ2 = -acos(-0.264) = -105.3°  → FUERA DE LÍMITE → indicador ROJO

Usuario ingresa X=100, Y=80, Z=20
          │
          ▼
IKSolver.Solve(100, 80, 20)
  dist = 128.06 mm  (dentro de workspace 1.9–299.9)
  θ2 = -43.8°  (dentro de -90° a +90°)
  θ1 = +28.6°  (dentro de -90° a +90°)
  → indicador VERDE → "OK θ1=28.6° θ2=-43.8°"
          │
          ▼ (click GoTo)
SerialManager.Send("P:100.0:80.0:20.0")   ← Unity → ESP32
          │
          ▼
ESP32: goToCartesian(100, 80, 20)
  ScaraKinematics.inverseKinematics(100, 80, θ1, θ2)
  moveJ1(θ1, 30)   moveJ2(θ2, 30)   moveZ(20, 5)
  ScaraKinematics.sendKinematics(θ1, θ2)   ← KINE:x,y,1
  sendStatus()
          │
          ▼
Unity recibe: "KINE:128.1,80.0,1"
Unity recibe: "STATUS:-90.00,-43.80,20.00,100.0,80.0,90,0"
  RobotController.UpdateFromESP32(...)
  Modelo 3D interpola a nueva posición
  CartesianPanel.MostrarObjetivo(100, 80)   → punto verde en mapa
```

---

## D. Script Unity — Lógica IK (UIManager.cs)

```csharp
void OnGoTo()
{
    if (!float.TryParse(inputX?.text, ..., out float x) ||
        !float.TryParse(inputY?.text, ..., out float y) ||
        !float.TryParse(inputZ?.text, ..., out float z))
    { ShowError("Valores inválidos. Usa números en X, Y, Z."); return; }

    state.target_X  = x;  state.target_Y = y;
    state.target_Z  = z;  state.hasTarget = true;

    CartesianPanel.Instance?.MostrarObjetivo(x, y);
    robot.GoToPosition(x, y, z);
}

// Validación en tiempo real mientras se escribe
void UpdateWorkspaceIndicator()
{
    var ik = IKSolver.Solve(x, y, z);
    if (workspaceImg) workspaceImg.color = ik.success ? C_OK : C_FAIL;
    if (ikStatusText) ikStatusText.text  = ik.success
        ? $"OK  θ1={ik.theta1:F1}°  θ2={ik.theta2:F1}°"
        : ik.error;
}
```

---

## E. Código Arduino Requerido (E2_Cinematica.ino)

El Panel de Control por Coordenadas requiere el sketch **E2_Cinematica.ino** con la librería **ScaraKinematics**.

### Librería ScaraKinematics.h

```arduino
// ScaraKinematics — Librería incluida en Arduino/libraries/ScaraKinematics/
// L1 = 150.9 mm,  L2 = 149.0 mm  (calibrado con robot real)

class ScaraKinematicsClass {
public:
    bool inverseKinematics(float x, float y, float& t1, float& t2);
    void forwardKinematics(float t1, float t2, float& x, float& y);
    bool isReachable(float x, float y);
    void sendKinematics(float t1, float t2);  // envía KINE:x,y,1 por Serial
};
extern ScaraKinematicsClass ScaraKinematics;
```

### Comando P:x:y:z en Arduino

```arduino
bool goToCartesian(float x, float y, float z)
{
    float t1, t2;
    if (!ScaraKinematics.inverseKinematics(x, y, t1, t2)) {
        Serial.print("UNREACHABLE:");
        Serial.print(x, 1); Serial.print(","); Serial.println(y, 1);
        return false;
    }
    moveJ1(t1, 30.0f);
    moveJ2(t2, 30.0f);
    moveZ(z,   5.0f);
    return true;
}
```

### Comando TARGET:x,y (desde visión artificial)

```arduino
if (cmd.startsWith("TARGET:")) {
    int comma = cmd.indexOf(',', 7);
    float x = cmd.substring(7, comma).toFloat();
    float y = cmd.substring(comma + 1).toFloat();
    if (goToCartesian(x, y, gZ_mm)) {
        Serial.print("ACK_TARGET:");
        Serial.print(x, 1); Serial.print(","); Serial.println(y, 1);
        sendStatus();
    }
}
```

---

## F. Validación de Workspace

| Condición | Resultado Unity | Resultado ESP32 |
|---|---|---|
| `dist > WorkspaceMax (299.9mm)` | Indicador rojo, "Posición fuera de alcance" | `UNREACHABLE:x,y` |
| `dist < WorkspaceMin (1.9mm)` | Indicador rojo, "Posición muy cerca del origen" | `UNREACHABLE:x,y` |
| `θ1 fuera de -90° a +90°` | Indicador rojo, "θ1=X° fuera de límites Motor2" | Sin movimiento |
| `θ2 fuera de -90° a +90°` | Indicador rojo, "θ2=X° fuera de límites Motor3" | Sin movimiento |
| `Z fuera de 0–200 mm` | Indicador rojo, "Z=X fuera de rango" | Sin movimiento |
| Todo OK | Indicador verde, "OK θ1=X° θ2=Y°" | Ejecuta movimiento |
