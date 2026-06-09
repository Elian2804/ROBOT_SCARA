/// <summary>
/// Estructura de datos que representa el estado actual del robot SCARA.
/// GestorSCARA la actualiza en cada frame y la pasa a los paneles de UI.
/// </summary>
[System.Serializable]
public struct DatosRobot
{
    // ─── ÁNGULOS / POSICIÓN ───────────────────────────────────────────────────
    /// <summary>Ángulo actual de J1 en grados (calculado de pasosJ1).</summary>
    public float anguloJ1;

    /// <summary>Ángulo actual de J2 en grados (calculado de pasosJ2).</summary>
    public float anguloJ2;

    /// <summary>Posición vertical actual del husillo en mm (calculada de pasosZ).</summary>
    public float posicionZ_mm;

    /// <summary>Ángulo actual del servo S1 (rotación de garra) en grados.</summary>
    public float anguloS1;

    /// <summary>Ángulo actual del servo S2 (apertura gripper) en grados.</summary>
    public float anguloS2;

    // ─── TCP (cinemática directa) ─────────────────────────────────────────────
    /// <summary>Posición X del TCP en mm. X = L1·cos(θ1) + L2·cos(θ1+θ2)</summary>
    public float tcpX_mm;

    /// <summary>Posición Y del TCP en mm. Y = L1·sin(θ1) + L2·sin(θ1+θ2)</summary>
    public float tcpY_mm;

    // ─── PASOS ACUMULADOS ─────────────────────────────────────────────────────
    /// <summary>Pasos acumulados de J1 desde el último home.</summary>
    public int pasosJ1;

    /// <summary>Pasos acumulados de J2 desde el último home.</summary>
    public int pasosJ2;

    /// <summary>Pasos acumulados del eje Z desde el último home.</summary>
    public int pasosZ;

    // ─── DIRECCIÓN ACTUAL ─────────────────────────────────────────────────────
    /// <summary>Dirección actual de M1: 1 = HIGH, -1 = LOW.</summary>
    public int dirM1;

    /// <summary>Dirección actual de M2: 1 = HIGH, -1 = LOW.</summary>
    public int dirM2;

    /// <summary>Dirección actual de M3: 1 = HIGH, -1 = LOW.</summary>
    public int dirM3;

    // ─── VELOCIDAD ────────────────────────────────────────────────────────────
    /// <summary>Velocidad de M1 en pasos/segundo (calculada del último periodo de STEP).</summary>
    public float velPasosM1;

    /// <summary>Velocidad de M2 en pasos/segundo.</summary>
    public float velPasosM2;

    /// <summary>Velocidad de M3 en pasos/segundo.</summary>
    public float velPasosM3;

    // ─── ESTADO ───────────────────────────────────────────────────────────────
    /// <summary>True si la conexión Bluetooth está activa (trama B:CON recibida).</summary>
    public bool bluetoothConectado;

    /// <summary>True si el gripper tiene un objeto agarrado.</summary>
    public bool objetoAgarrado;

    // ─── CINEMÁTICA DIRECTA ───────────────────────────────────────────────────

    /// <summary>
    /// Calcula las coordenadas XY del TCP usando cinemática directa.
    ///   X = L1·cos(θ1) + L2·cos(θ1+θ2)
    ///   Y = L1·sin(θ1) + L2·sin(θ1+θ2)
    /// Llamar después de actualizar anguloJ1 y anguloJ2.
    /// </summary>
    public void ActualizarTCP()
    {
        float j1Rad = UnityEngine.Mathf.Deg2Rad * anguloJ1;
        float j2Rad = UnityEngine.Mathf.Deg2Rad * (anguloJ1 + anguloJ2);

        tcpX_mm = ConfiguracionRobot.Eslablon1 * UnityEngine.Mathf.Cos(j1Rad)
                + ConfiguracionRobot.Eslablon2 * UnityEngine.Mathf.Cos(j2Rad);

        tcpY_mm = ConfiguracionRobot.Eslablon1 * UnityEngine.Mathf.Sin(j1Rad)
                + ConfiguracionRobot.Eslablon2 * UnityEngine.Mathf.Sin(j2Rad);
    }

    /// <summary>Radio TCP desde el origen en mm (para validar espacio de trabajo).</summary>
    public float RadioTCP =>
        UnityEngine.Mathf.Sqrt(tcpX_mm * tcpX_mm + tcpY_mm * tcpY_mm);
}
