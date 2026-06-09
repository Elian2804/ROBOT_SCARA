using UnityEngine;

/// <summary>
/// Tipos de actuador que puede representar un ConfiguracionMotor.
/// </summary>
public enum TipoActuador
{
    Stepper,
    Servo
}

/// <summary>
/// ScriptableObject con los parámetros de un actuador del robot SCARA.
/// Crear desde el menú: Create > SCARA > ConfiguracionMotor
/// Un asset por actuador: M1_EjeZ, M2_Brazo1, M3_Brazo2, S1_GiroGarra, S2_Gripper.
/// </summary>
[CreateAssetMenu(fileName = "ConfiguracionMotor", menuName = "SCARA/ConfiguracionMotor")]
public class ConfiguracionMotor : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre descriptivo del actuador. Ej: M1 - Eje Z")]
    public string nombreActuador = "Actuador";

    [Tooltip("Tipo: Stepper para motores paso a paso, Servo para servomotores.")]
    public TipoActuador tipo = TipoActuador.Stepper;

    [Header("Pines ESP32")]
    [Tooltip("Pin de señal (STEP para steppers, PWM para servos).")]
    public int pinSenal = 0;

    [Tooltip("Pin de dirección (solo steppers). Ignorado en servos.")]
    public int pinDir = 0;

    [Header("Parámetros Stepper (ignorar si es Servo)")]
    [Tooltip("Velocidad máxima permitida en pasos por segundo antes de generar A01.")]
    public float velocidadMaxPasos = 10000f;

    [Header("Límites")]
    [Tooltip("Límite mínimo: grados para rotacional, mm para lineal.")]
    public float limiteMin = -90f;

    [Tooltip("Límite máximo: grados para rotacional, mm para lineal.")]
    public float limiteMax =  90f;

    [Header("Parámetros Servo (ignorar si es Stepper)")]
    [Tooltip("Ancho de pulso mínimo en microsegundos (corresponde a limiteMin).")]
    public float pulsoMin_us = ConfiguracionRobot.ServoPulsoMin_us;

    [Tooltip("Ancho de pulso máximo en microsegundos (corresponde a limiteMax).")]
    public float pulsoMax_us = ConfiguracionRobot.ServoPulsoMax_us;

    // ─── HELPER SERVO ─────────────────────────────────────────────────────────

    /// <summary>Convierte ancho de pulso en microsegundos al ángulo del servo.</summary>
    public float PulsoAAngulo(float pulsoUs)
    {
        return Mathf.Lerp(limiteMin, limiteMax,
            Mathf.InverseLerp(pulsoMin_us, pulsoMax_us, pulsoUs));
    }

    /// <summary>Verifica si el valor (grados o mm) está dentro de los límites configurados.</summary>
    public bool DentroDeRango(float valor)
        => valor >= limiteMin && valor <= limiteMax;
}
