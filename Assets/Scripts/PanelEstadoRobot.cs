using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel izquierdo de la interfaz.
/// Muestra el estado en tiempo real de cada motor/servo,
/// la posición XYZ del TCP y el estado del gripper.
/// Todas las referencias se asignan desde el Inspector.
/// </summary>
public class PanelEstadoRobot : MonoBehaviour
{
    // ─── SECCIÓN STEPPERS ─────────────────────────────────────────────────────
    [Header("M1 — Eje Z")]
    [SerializeField] private TextMeshProUGUI textoM1_Posicion;
    [SerializeField] private TextMeshProUGUI textoM1_Direccion;
    [SerializeField] private Slider          barraM1_Progreso;

    [Header("M2 — Brazo 1 (J1)")]
    [SerializeField] private TextMeshProUGUI textoM2_Angulo;
    [SerializeField] private TextMeshProUGUI textoM2_Direccion;
    [SerializeField] private Slider          barraM2_Progreso;

    [Header("M3 — Brazo 2 (J2)")]
    [SerializeField] private TextMeshProUGUI textoM3_Angulo;
    [SerializeField] private TextMeshProUGUI textoM3_Direccion;
    [SerializeField] private Slider          barraM3_Progreso;

    // ─── SECCIÓN SERVOS ───────────────────────────────────────────────────────
    [Header("S1 — Rotación Garra")]
    [SerializeField] private TextMeshProUGUI textoS1_Angulo;
    [SerializeField] private Slider          barraS1_Progreso;

    [Header("S2 — Gripper")]
    [SerializeField] private TextMeshProUGUI textoS2_Angulo;
    [SerializeField] private Image           imagenEstadoGripper;
    [SerializeField] private TextMeshProUGUI textoEstadoGripper;
    [SerializeField] private Slider          barraS2_Progreso;

    // ─── SECCIÓN TCP / XYZ ───────────────────────────────────────────────────
    [Header("Posición TCP")]
    [SerializeField] private TextMeshProUGUI textoPosicionX;
    [SerializeField] private TextMeshProUGUI textoPosicionY;
    [SerializeField] private TextMeshProUGUI textoPosicionZ;
    [SerializeField] private TextMeshProUGUI textoRadioTCP;
    [SerializeField] private TextMeshProUGUI textoEspacioTrabajo;

    // ─── REDUCCIÓN — RELACIÓN DE TRANSMISIÓN ────────────────────────────────
    // Un ToggleGroup por motor. Solo un Toggle activo a la vez por grupo.
    // 1:1=100p  1:2=200p  1:3=300p  1:4=400p
    [Header("Reducción M2 — Brazo 1 (J1)")]
    [SerializeField] private Toggle toggleJ1_1a1;
    [SerializeField] private Toggle toggleJ1_1a2;
    [SerializeField] private Toggle toggleJ1_1a3;
    [SerializeField] private Toggle toggleJ1_1a4;

    [Header("Reducción M3 — Brazo 2 (J2)")]
    [SerializeField] private Toggle toggleJ2_1a1;
    [SerializeField] private Toggle toggleJ2_1a2;
    [SerializeField] private Toggle toggleJ2_1a3;
    [SerializeField] private Toggle toggleJ2_1a4;

    [Header("Reducción M1 — Eje Z")]
    [SerializeField] private Toggle toggleZ_1a1;
    [SerializeField] private Toggle toggleZ_1a2;
    [SerializeField] private Toggle toggleZ_1a3;
    [SerializeField] private Toggle toggleZ_1a4;

    // ─── CONFIGURACIÓN VISUAL ─────────────────────────────────────────────────
    [Header("Colores")]
    [SerializeField] private Color colorNormal     = ColoresUI.TextoPrincipal;
    [SerializeField] private Color colorAdvertencia = ColoresUI.Advertencia;
    [SerializeField] private Color colorError       = ColoresUI.Error;
    [SerializeField] private Color colorExito       = ColoresUI.Exito;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    /// <summary>Inicializar valores por defecto. Llamado por GestorSCARA.</summary>
    public void Inicializar()
    {
        LimpiarTextos();
        // Activar el toggle que corresponde al valor actual (default 1:2)
        ActivarToggleSegunValor(
            ConfiguracionRobot.Pasos180_J1,
            toggleJ1_1a1, toggleJ1_1a2, toggleJ1_1a3, toggleJ1_1a4);
        ActivarToggleSegunValor(
            ConfiguracionRobot.Pasos180_J2,
            toggleJ2_1a1, toggleJ2_1a2, toggleJ2_1a3, toggleJ2_1a4);
        ActivarToggleSegunValor(
            ConfiguracionRobot.PasosRef_Z,
            toggleZ_1a1, toggleZ_1a2, toggleZ_1a3, toggleZ_1a4);
    }

    // ─── HANDLERS DE REDUCCIÓN (onValueChanged asignado desde Inspector/Builder) ─

    public void SeleccionarJ1_1a1(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J1, 100f, "J1", "1:1"); }
    public void SeleccionarJ1_1a2(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J1, 200f, "J1", "1:2"); }
    public void SeleccionarJ1_1a3(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J1, 300f, "J1", "1:3"); }
    public void SeleccionarJ1_1a4(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J1, 400f, "J1", "1:4"); }

    public void SeleccionarJ2_1a1(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J2, 100f, "J2", "1:1"); }
    public void SeleccionarJ2_1a2(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J2, 200f, "J2", "1:2"); }
    public void SeleccionarJ2_1a3(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J2, 300f, "J2", "1:3"); }
    public void SeleccionarJ2_1a4(bool v) { if (v) AplicarReduccion(ref ConfiguracionRobot.Pasos180_J2, 400f, "J2", "1:4"); }

    public void SeleccionarZ_1a1(bool v)  { if (v) AplicarReduccion(ref ConfiguracionRobot.PasosRef_Z, 100f, "Z", "1:1"); }
    public void SeleccionarZ_1a2(bool v)  { if (v) AplicarReduccion(ref ConfiguracionRobot.PasosRef_Z, 200f, "Z", "1:2"); }
    public void SeleccionarZ_1a3(bool v)  { if (v) AplicarReduccion(ref ConfiguracionRobot.PasosRef_Z, 300f, "Z", "1:3"); }
    public void SeleccionarZ_1a4(bool v)  { if (v) AplicarReduccion(ref ConfiguracionRobot.PasosRef_Z, 400f, "Z", "1:4"); }

    private static void AplicarReduccion(ref float campo, float valor, string motor, string relacion)
    {
        campo = valor;
        ConfiguracionRobot.OnReduccionCambiada?.Invoke(motor, valor);
    }

    /// <summary>
    /// Actualiza todos los campos con los datos actuales del robot.
    /// Llamado por GestorSCARA en cada Update().
    /// </summary>
    public void ActualizarEstado(DatosRobot datos)
    {
        ActualizarSteppers(datos);
        ActualizarServos(datos);
        ActualizarTCP(datos);
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void ActualizarSteppers(DatosRobot datos)
    {
        // ── M1 — Eje Z ────────────────────────────────────────────────────────
        // Slider: 0 → PasosRangoZ (recorrido completo según ratio. 1:1=3000, 1:2=6000...)
        int rangoZ = Mathf.Max(1, (int)ConfiguracionRobot.PasosRangoZ);
        if (textoM1_Posicion != null)
            textoM1_Posicion.text = $"Z: {datos.pasosZ}p/{rangoZ} → {datos.posicionZ_mm:F2}mm";
        if (textoM1_Direccion != null)
            textoM1_Direccion.text = datos.dirM1 >= 0 ? "DIR ▲" : "DIR ▼";
        if (barraM1_Progreso != null)
        {
            barraM1_Progreso.minValue = 0;
            barraM1_Progreso.maxValue = rangoZ;
            barraM1_Progreso.value    = Mathf.Clamp(Mathf.Abs(datos.pasosZ), 0, rangoZ);
        }

        // ── M2 — J1 ───────────────────────────────────────────────────────────
        // Slider: 0 → Pasos180_J1 (recorrido completo -90° a +90°)
        int p180J1 = (int)ConfiguracionRobot.Pasos180_J1;
        if (textoM2_Angulo != null)
        {
            textoM2_Angulo.text  = $"J1: {datos.pasosJ1}p → {datos.anguloJ1 + 90f:F1}°";
            textoM2_Angulo.color = DeterminarColorPasos(datos.pasosJ1, 0, p180J1);
        }
        if (textoM2_Direccion != null)
            textoM2_Direccion.text = datos.dirM2 >= 0 ? "DIR ▲" : "DIR ▼";
        if (barraM2_Progreso != null)
        {
            barraM2_Progreso.minValue = 0;
            barraM2_Progreso.maxValue = p180J1;
            barraM2_Progreso.value    = Mathf.Clamp(datos.pasosJ1, 0, p180J1);
        }

        // ── M3 — J2 ───────────────────────────────────────────────────────────
        int p180J2 = (int)ConfiguracionRobot.Pasos180_J2;
        if (textoM3_Angulo != null)
        {
            textoM3_Angulo.text  = $"J2: {datos.pasosJ2}p → {datos.anguloJ2 + 90f:F1}°";
            textoM3_Angulo.color = DeterminarColorPasos(datos.pasosJ2, 0, p180J2);
        }
        if (textoM3_Direccion != null)
            textoM3_Direccion.text = datos.dirM3 >= 0 ? "DIR ▲" : "DIR ▼";
        if (barraM3_Progreso != null)
        {
            barraM3_Progreso.minValue = 0;
            barraM3_Progreso.maxValue = p180J2;
            barraM3_Progreso.value    = Mathf.Clamp(datos.pasosJ2, 0, p180J2);
        }
    }

    // Colorea el texto según qué tan cerca está del límite del recorrido
    private Color DeterminarColorPasos(float pasos, float min, float max)
    {
        float umbral = (max - min) * 0.10f;
        if (pasos >= max - umbral || pasos <= min + umbral)
            return colorAdvertencia;
        return colorNormal;
    }

    private void ActualizarServos(DatosRobot datos)
    {
        // S1
        if (textoS1_Angulo != null)
            textoS1_Angulo.text = $"S1: {datos.anguloS1:F1}°";

        if (barraS1_Progreso != null)
        {
            barraS1_Progreso.minValue = ConfiguracionRobot.S1_Min;
            barraS1_Progreso.maxValue = ConfiguracionRobot.S1_Max;
            barraS1_Progreso.value    = datos.anguloS1;
        }

        // S2 / Gripper
        if (textoS2_Angulo != null)
            textoS2_Angulo.text = $"S2: {datos.anguloS2:F1}°";

        if (barraS2_Progreso != null)
        {
            barraS2_Progreso.minValue = ConfiguracionRobot.S2_Min;
            barraS2_Progreso.maxValue = ConfiguracionRobot.S2_Max;
            barraS2_Progreso.value    = datos.anguloS2;
        }

        bool gripperCerrado = datos.anguloS2 < (ConfiguracionRobot.S2_Max * 0.2f);

        if (textoEstadoGripper != null)
        {
            if (datos.objetoAgarrado)
            {
                textoEstadoGripper.text  = "AGARRADO";
                textoEstadoGripper.color = colorExito;
            }
            else if (gripperCerrado)
            {
                textoEstadoGripper.text  = "CERRADO";
                textoEstadoGripper.color = colorNormal;
            }
            else
            {
                textoEstadoGripper.text  = "ABIERTO";
                textoEstadoGripper.color = colorAdvertencia;
            }
        }

        if (imagenEstadoGripper != null)
            imagenEstadoGripper.color = datos.objetoAgarrado ? colorExito : colorNormal;
    }

    private void ActualizarTCP(DatosRobot datos)
    {
        if (textoPosicionX != null)
            textoPosicionX.text = $"X: {datos.tcpX_mm:F1} mm";

        if (textoPosicionY != null)
            textoPosicionY.text = $"Y: {datos.tcpY_mm:F1} mm";

        if (textoPosicionZ != null)
            textoPosicionZ.text = $"Z: {datos.posicionZ_mm:F1} mm";

        if (textoRadioTCP != null)
        {
            float radio = datos.RadioTCP;
            textoRadioTCP.text  = $"R: {radio:F1} mm";
            textoRadioTCP.color = DeterminarColorRadio(radio);
        }

        if (textoEspacioTrabajo != null)
        {
            float radio      = datos.RadioTCP;
            bool  enEspacio  = radio >= ConfiguracionRobot.EspacioMin
                            && radio <= ConfiguracionRobot.EspacioMax;
            textoEspacioTrabajo.text  = enEspacio ? "En espacio de trabajo" : "FUERA DE RANGO";
            textoEspacioTrabajo.color = enEspacio ? colorExito : colorError;
        }
    }

    /// <summary>Asigna color según qué tan cerca está el valor del límite.</summary>
    private Color DeterminarColorAngulo(float valor, float min, float max)
    {
        float rangoTotal = max - min;
        float umbralAdvertencia = rangoTotal * 0.10f;   // 10% del extremo

        if (valor >= max - umbralAdvertencia || valor <= min + umbralAdvertencia)
            return colorAdvertencia;
        return colorNormal;
    }

    private Color DeterminarColorRadio(float radio)
    {
        if (radio > ConfiguracionRobot.EspacioMax || radio < ConfiguracionRobot.EspacioMin)
            return colorError;
        if (radio > ConfiguracionRobot.EspacioMax * 0.90f)
            return colorAdvertencia;
        return colorNormal;
    }

    private void LimpiarTextos()
    {
        var campos = new TextMeshProUGUI[]
        {
            textoM1_Posicion, textoM2_Angulo, textoM3_Angulo,
            textoS1_Angulo, textoS2_Angulo, textoEstadoGripper,
            textoPosicionX, textoPosicionY, textoPosicionZ,
            textoRadioTCP, textoEspacioTrabajo
        };
        foreach (var campo in campos)
            if (campo != null) campo.text = "—";
    }

    // Activa el toggle que corresponde al valor de pasos (100/200/300/400)
    private static void ActivarToggleSegunValor(float valor,
        Toggle t1a1, Toggle t1a2, Toggle t1a3, Toggle t1a4)
    {
        float[] valores = { 100f, 200f, 300f, 400f };
        Toggle[] ts     = { t1a1, t1a2, t1a3, t1a4 };
        int idx = 0;  // default 1:1
        for (int i = 0; i < valores.Length; i++)
            if (Mathf.Approximately(valor, valores[i])) { idx = i; break; }
        for (int i = 0; i < ts.Length; i++)
            if (ts[i] != null) ts[i].isOn = (i == idx);
    }
}
