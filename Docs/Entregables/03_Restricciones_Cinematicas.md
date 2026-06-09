# Tabla de Restricciones Cinemáticas y Dimensionales
## Robot SCARA Virtual — Parámetros de Configuración

---

## A. Parámetros Geométricos (Denavit-Hartenberg simplificado SCARA)

| Parámetro | Símbolo | Valor | Unidad | Origen en código |
|---|---|---|---|---|
| Longitud eslabón 1 (hombro→codo) | L1 | **150.9** | mm | `RobotConfig.L1` |
| Longitud eslabón 2 (codo→TCP) | L2 | **149.0** | mm | `RobotConfig.L2` |
| Radio de alcance máximo | r_max = L1+L2 | **299.9** | mm | `RobotConfig.WorkspaceMax` |
| Radio de alcance mínimo | r_min = |L1−L2| | **1.9** | mm | `RobotConfig.WorkspaceMin` |
| Altura de trabajo mínima | Z_min | **0** | mm | `RobotConfig.ZMin` |
| Altura de trabajo máxima | Z_max | **200** | mm | `RobotConfig.ZMax` |

---

## B. Límites Angulares de Articulaciones

| Articulación | Símbolo | Mínimo | Máximo | Unidad | Fuente |
|---|---|---|---|---|---|
| Motor J1 — base (hombro) | θ1 | **-90** | **+90** | ° | `RobotConfig.Motor2_Min/Max` |
| Motor J2 — codo | θ2 | **-90** | **+90** | ° | `RobotConfig.Motor3_Min/Max` |
| Servo S1 — muñeca (rotación) | α | **0** | **180** | ° | `RobotConfig.Servo1_Min/Max` |
| Servo S2 — garra (apertura) | β | **0** | **20** | ° | `RobotConfig.Servo2_Min/Max` |
| Umbral garra abierta | β_open | **> 5** | — | ° | `RobotConfig.GripperOpenThreshold` |

---

## C. Parámetros de Transmisión Mecánica

| Parámetro | Símbolo | Valor | Unidad | Fuente |
|---|---|---|---|---|
| Pasos por revolución del motor | SPR | **200** | pasos/rev | `RobotConfig.StepsPerRev` |
| Resolución angular motores J1/J2 | — | **0.5556** | pasos/° | `RobotConfig.StepsPerDeg` |
| Resolución angular real | — | **1.8** | °/paso | 360°/200 |
| Paso del tornillo eje Z | p | **0.8** | mm/rev | `RobotConfig.MmPerRevZ` |
| Resolución lineal eje Z | — | **250** | pasos/mm | `RobotConfig.StepsPerMmZ` |
| Recorrido máximo Z en pasos | — | **50 000** | pasos | `RobotConfig.ZMaxSteps` |
| Rango J1 en pasos (lógico) | — | -51 a +49 | pasos | `RobotConfig.J1MinSteps/MaxSteps` |
| Rango J2 en pasos (lógico) | — | -50 a +50 | pasos | `RobotConfig.J2MinSteps/MaxSteps` |

---

## D. Escala Virtual ↔ Real

| Parámetro | Valor | Unidad | Descripción |
|---|---|---|---|
| Factor de escala MM → Unity | **0.55** | u/mm | `RobotConfig.MM_TO_UNITY` (calibrado con modelo FBX) |
| Offset Y husillo en reposo | **30** | u | `RobotConfig.ZOffsetUnity` (posición Y de `ARTICULACION_VERTICAL` cuando Z=0 mm) |
| Resolución de visualización | 1920 × 1080 | px | Fija en `SplashScreen.Start()` |
| Distancia mínima entre puntos trail | **3** | px | `RobotConfig.TrajectoryMinDistPixels` |
| Z por defecto visión (sin dato Z) | **20** | mm | `RobotConfig.DefaultVisionZ` |

---

## E. Ecuaciones Cinemáticas Implementadas

### Cinemática Inversa (IKSolver.Solve)
```
dist = √(x² + y²)

cos(θ2) = (dist² - L1² - L2²) / (2·L1·L2)        [ley del coseno]
θ2 = -acos(cos(θ2))                                 [solución codo abajo]

α = atan2(y, x)
β = atan2(L2·sin(θ2), L1 + L2·cos(θ2))
θ1 = α - β
```

### Cinemática Directa (IKSolver.ForwardKinematics)
```
x_TCP = L1·cos(θ1) + L2·cos(θ1 + θ2)
y_TCP = L1·sin(θ1) + L2·sin(θ1 + θ2)
```

### Restricciones de validación
```
r_min ≤ dist ≤ r_max              [alcance workspace]
Z_min ≤ Z ≤ Z_max                 [recorrido husillo]
Motor2_Min ≤ θ1 ≤ Motor2_Max      [límite J1]
Motor3_Min ≤ θ2 ≤ Motor3_Max      [límite J2]
```
