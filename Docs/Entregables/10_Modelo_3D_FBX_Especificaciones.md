# Modelo 3D del Robot SCARA — Especificaciones FBX
## Optimizado para Simulación en Unity

---

## A. Especificaciones del Archivo FBX

| Parámetro | Valor |
|---|---|
| Formato | FBX Binary (.fbx) |
| Versión FBX | 7.4 (compatible Unity 2021+) |
| Unidades exportación | Centímetros (Unity convierte automáticamente) |
| Eje arriba (Up axis) | Y+ |
| Sistema de coordenadas | Derecho (Right-handed → Unity lo convierte) |
| Escala de importación Unity | 0.01 (100 cm → 1 m) o ajustada según modelo |
| Suavizado (Smoothing) | Por grupos de suavizado |
| LODs incluidos | No (único LOD; robot pequeño) |

---

## B. Jerarquía de Huesos / Pivotes Requerida

```
ROBOT_SCARA  (raíz — origen en suelo, centro de base)
├── BASE_FIJA          (geometría estática: base, motor Z)
│   └── ARTICULACION_VERTICAL  (pivot en parte superior del husillo)
│       └── pivot_art1         (pivot en eje de J1 — hombro)
│           ├── BRAZO_1        (eslabón L1, longitud 150.9 mm)
│           └── pivot_art2     (pivot en eje J2 — codo)
│               ├── BRAZO_2    (eslabón L2, longitud 149.0 mm)
│               └── GiroGarra  (pivot en TCP para muñeca)
│                   ├── HERRAMIENTA_BASE
│                   ├── Garra_1  (dedo izquierdo)
│                   ├── Garra_2  (dedo derecho, gira opuesto)
│                   └── TCP      (locator vacío — punto de herramienta)
```

---

## C. Pivot Points Críticos

| Objeto | Posición del Pivot | Eje de rotación en Unity |
|---|---|---|
| `ARTICULACION_VERTICAL` | Parte superior del husillo (donde inicia el brazo) | Traslación local Y+ (hacia arriba = Z aumenta) |
| `pivot_art1` | Centro del eje del motor J1 | Rotación local Z (giro horizontal) |
| `pivot_art2` | Centro del eje del motor J2 | Rotación local Z (giro horizontal) |
| `GiroGarra` | Centro del eje de la muñeca | Rotación local Y (rotación axial) |
| `Garra_1` | Bisagra del dedo 1 | Rotación local Z |
| `Garra_2` | Bisagra del dedo 2 | Rotación local Z (invertida: ×-1) |
| `TCP` | Punto de contacto de la herramienta | Solo referencia, sin rotación propia |

---

## D. Parámetros de Geometría Recomendados

| Elemento | Polígonos (aprox.) | Material |
|---|---|---|
| Base fija | 800–1200 tris | Metal gris oscuro `#2C3E50` |
| Husillo Z | 400–600 tris | Metal cromado |
| Eslabón 1 (L1=150.9mm) | 600–900 tris | Gris SCARA `#566573` |
| Eslabón 2 (L2=149.0mm) | 500–800 tris | Gris SCARA `#566573` |
| Cabezal garra | 400–600 tris | Aluminio `#AAB7B8` |
| Dedos garra ×2 | 300–500 tris c/u | Negro técnico `#1C2833` |
| **Total** | **~4000–5000 tris** | — |

---

## E. Configuración de Importación en Unity

```
Rig:
  Animation Type: None (no animación de clip; todo por script)

Geometry:
  Mesh Compression: Off
  Read/Write Enabled: OFF
  Optimize Mesh: ON
  Generate Lightmap UVs: OFF

Materials:
  Material Creation Mode: Import via MaterialDescription
  Location: Use External Materials (Legacy)
```

---

## F. Escala de Referencia

La escala real del robot se calibra con el factor `RobotConfig.MM_TO_UNITY = 0.55`:

```
Longitud física L1 = 150.9 mm
Longitud en Unity  = 150.9 × 0.55 = ~82.99 unidades Unity
(= ~0.83 m si 1 unidad = 1 cm, ajustar según importación)
```

El offset de altura del husillo en reposo (Z=0): `RobotConfig.ZOffsetUnity = 30` unidades.

---

## G. Nota sobre el Archivo en el Proyecto

El modelo FBX se ubica en:
```
Assets/
  Models/
    ROBOT_SCARA.fbx          ← modelo principal
    ROBOT_SCARA.fbx.meta     ← GUID de Unity (no mover sin Unity Editor)
```

Para modificar el modelo: editar en Blender/Maya y re-exportar a la misma ruta.
Unity detectará el cambio y reimportará automáticamente.
