using UnityEngine;

/// <summary>
/// Fuente única de verdad para todos los parámetros físicos y de configuración del robot SCARA.
/// NUNCA duplicar estos valores en otros scripts. Siempre referenciar ConfiguracionRobot.*
/// </summary>
public static class ConfiguracionRobot
{
    // ─── LONGITUDES REALES (mm) ───────────────────────────────────────────────
    /// <summary>Longitud del eslabón 1: centro motor M2 → centro motor M3. (15 cm)</summary>
    public const float Eslablon1 = 150.0f;

    /// <summary>Longitud del eslabón 2: centro motor M3 → TCP. (15 cm)</summary>
    public const float Eslablon2 = 150.0f;

    // ─── LÍMITES ANGULARES (grados) ──────────────────────────────────────────
    public const float J1_Min  = -90f;
    public const float J1_Max  =  90f;
    public const float HomeJ1  = -90f;
    public const float HomeJ2  = -90f;
    public const float J2_Min  = -90f;
    public const float J2_Max  =  90f;
    public const float S1_Min  =   0f;
    public const float S1_Max  = 180f;
    public const float S2_Min  =   0f;
    public const float S2_Max  =  20f;

    // ─── LÍMITES VERTICALES (mm) ─────────────────────────────────────────────
    public const float Z_Min_mm =   0f;
    public const float Z_Max_mm = 200f;

    // ─── POSICIÓN Y DEL HUSILLO EN UNITY ─────────────────────────────────────
    public const float Z_UnidadSuperior = -78f;
    public const float Z_UnidadInferior = -80f;

    // ─── ESPACIO DE TRABAJO (mm) ─────────────────────────────────────────────
    public const float EspacioMax = Eslablon1 + Eslablon2;   // 300 mm
    public const float EspacioMin = Eslablon1 - Eslablon2;   //   0 mm (pero evitar singularidad)

    // ─── ESCALA mm → unidades Unity ──────────────────────────────────────────
    public const float MM_A_UNITY = 0.01f;

    // ─── GARRA (unidades Unity) ──────────────────────────────────────────────
    public const float GarraCerrada = 0f;
    public const float GarraAbierta = 0.05f;

    // ─── CONVERSIÓN DE PASOS (lógica central) ────────────────────────────────
    // ─── RELACIONES DE REDUCCIÓN ─────────────────────────────────────────────
    // 1:1 → 100 pasos = 180° (o referencia Z)
    // 1:2 → 200 pasos = 180°  ← default
    // 1:3 → 300 pasos = 180°
    // 1:4 → 400 pasos = 180°

    /// <summary>Pasos para 180° en J1. 1:1=100, 1:2=200, 1:3=300, 1:4=400. Default 1:1.</summary>
    public static float Pasos180_J1 = 100f;

    /// <summary>Pasos para 180° en J2. 1:1=100, 1:2=200, 1:3=300, 1:4=400. Default 1:1.</summary>
    public static float Pasos180_J2 = 100f;

    /// <summary>Pasos para recorrer el rango completo de Z (0 a Z_Max_mm) a ratio 1:1. Calibrado: 3000p.</summary>
    public static float PasosRangoZ_1a1 = 3000f;

    /// <summary>Ratio seleccionada para Z: 1:1=100, 1:2=200, 1:3=300, 1:4=400. Default 1:1.</summary>
    public static float PasosRef_Z = 100f;

    /// <summary>
    /// Pasos efectivos para recorrer el rango completo de Z con la ratio actual.
    /// 1:1 = 3000p, 1:2 = 6000p, 1:3 = 9000p, 1:4 = 12000p.
    /// </summary>
    public static float PasosRangoZ => PasosRangoZ_1a1 * (PasosRef_Z / 100f);

    /// <summary>mm por paso en Z. Con 1:1: 200mm / 3000p = 0.0667mm/paso.</summary>
    public static float MmPorPaso_Z => Z_Max_mm / PasosRangoZ;

    // ─── PINES FIJOS DEL ROBOT ESP32 (inmutables) ───────────────────────────
    public const int M1_PinDir   =  2;    // DIR  Eje Z
    public const int M1_PinSenal = 15;    // STEP Eje Z
    public const int M2_PinDir   = 18;    // DIR  Brazo 1 (J1)
    public const int M2_PinSenal = 19;    // STEP Brazo 1 (J1)
    public const int M3_PinDir   =  4;    // DIR  Brazo 2 (J2)
    public const int M3_PinSenal = 16;    // STEP Brazo 2 (J2)
    public const int EN_Pin      =  5;    // ENABLE drivers (LOW = activo)
    public const int S1_Pin      = 35;    // PWM Servo S1 (GiroGarra)
    public const int S2_Pin      = 34;    // PWM Servo S2 (Gripper)

    // ─── PARÁMETROS SERVO ────────────────────────────────────────────────────
    public const float ServoPulsoMin_us = 1000f;
    public const float ServoPulsoMax_us = 2000f;

    // ─── COMUNICACIÓN SERIAL ─────────────────────────────────────────────────
    public const int VelocidadSerial = 115200;
    public const int TiempoEsperaMs  = 500;

    // ─── VELOCIDADES VISUALES ────────────────────────────────────────────────
    public const float VelocidadRotacion = 3.0f;
    public const float VelocidadVertical = 2.0f;

    // ─── RESOLUCIÓN DE PANTALLA ──────────────────────────────────────────────
    public const int PantallaAncho = 1920;
    public const int PantallaAlto  = 1080;

    // ─── HELPERS DE CONVERSIÓN ───────────────────────────────────────────────

    /// <summary>
    /// Dispara cuando el usuario cambia la relación de reducción de un motor.
    /// Parámetros: nombre del motor ("J1","J2","Z"), nuevos pasos de referencia.
    /// GestorSCARA se suscribe para loguear el cambio en PanelConsola.
    /// </summary>
    public static System.Action<string, float> OnReduccionCambiada;

    /// <summary>
    /// Convierte pasos acumulados de J1 a grados.
    /// Paso 0 = HomeJ1 (-90°). Paso Pasos180_J1 = HomeJ1+180° (+90°).
    /// </summary>
    public static float PasosAGradosJ1(float pasos)
        => HomeJ1 + (pasos / Pasos180_J1) * 180f;

    /// <summary>
    /// Convierte pasos acumulados de J2 a grados.
    /// Paso 0 = HomeJ2 (-90°). Paso Pasos180_J2 = HomeJ2+180° (+90°).
    /// </summary>
    public static float PasosAGradosJ2(float pasos)
        => HomeJ2 + (pasos / Pasos180_J2) * 180f;

    /// <summary>Convierte pasos del eje Z a milímetros.</summary>
    public static float PasosAMmZ(float pasos)
        => pasos * MmPorPaso_Z;

    /// <summary>Convierte posición Z en mm a coordenada Y local de ARTICULACION_VERTICAL.</summary>
    public static float ZmmAUnity(float zMm)
        => Z_UnidadSuperior - (zMm * MM_A_UNITY);

    /// <summary>Convierte coordenada Y local de ARTICULACION_VERTICAL a mm reales.</summary>
    public static float UnityAZmm(float yUnity)
        => (Z_UnidadSuperior - yUnity) / MM_A_UNITY;

    /// <summary>
    /// Indica si un pin dado pertenece a algún actuador stepper conocido.
    /// Devuelve false para pines no configurados → señal se descarta con A08.
    /// </summary>
    public static bool EsPinValido(int pin)
    {
        return pin == M1_PinDir   || pin == M1_PinSenal ||
               pin == M2_PinDir   || pin == M2_PinSenal ||
               pin == M3_PinDir   || pin == M3_PinSenal ||
               pin == S1_Pin      || pin == S2_Pin      ||
               pin == EN_Pin;
    }
}
