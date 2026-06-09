using UnityEngine;

/// <summary>
/// Muestra etiquetas 3D flotantes sobre cada articulación del robot con sus valores actuales.
/// Siempre miran hacia la cámara principal (billboard).
/// Agregar al mismo GameObject que GestorSCARA y asignar los transforms del robot.
/// </summary>
public class EtiquetasArticulaciones : MonoBehaviour
{
    [Header("Articulaciones (arrastrar desde jerarquía)")]
    [SerializeField] private Transform pivotArt1;           // pivot_art1  → J1
    [SerializeField] private Transform pivotArt2;           // pivot_art2  → J2
    [SerializeField] private Transform giroGarra;           // GiroGarra   → S1
    [SerializeField] private Transform articulacionVertical; // ARTICULACION_VERTICAL → Z
    [SerializeField] private Transform tcp;                 // TCP         → posición

    [Header("Apariencia")]
    [Tooltip("Tamaño del texto. Subir si el robot es grande en escena (prueba 5-15).")]
    [SerializeField] private float   tamanoFuente   = 8f;
    [Tooltip("Amarillo #FFD900 visible sobre fondo azul oscuro del proyecto.")]
    [SerializeField] private Color   colorTexto     = new Color(1f, 0.851f, 0f); // #FFD900
    [Tooltip("Desplazamiento en espacio MUNDO sobre cada articulación.")]
    [SerializeField] private Vector3 offsetMundo    = new Vector3(0f, 2f, 0f);
    [Tooltip("Mostrar u ocultar todas las etiquetas.")]
    [SerializeField] private bool    visible        = true;

    [Header("Sistema")]
    [SerializeField] private GestorSCARA gestorSCARA;

    // Etiquetas creadas en runtime
    private TextMesh _etJ1;
    private TextMesh _etJ2;
    private TextMesh _etS1;
    private TextMesh _etZ;
    private TextMesh _etTCP;

    private Camera _camara;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        _camara = Camera.main;

        _etJ1  = CrearEtiqueta("Etiqueta_J1",  pivotArt1,            "J1: --");
        _etJ2  = CrearEtiqueta("Etiqueta_J2",  pivotArt2,            "J2: --");
        _etS1  = CrearEtiqueta("Etiqueta_S1",  giroGarra,            "S1: --");
        _etZ   = CrearEtiqueta("Etiqueta_Z",   articulacionVertical, "Z: --");
        _etTCP = CrearEtiqueta("Etiqueta_TCP", tcp,                  "TCP");
    }

    private void LateUpdate()
    {
        if (_camara == null) _camara = Camera.main;

        if (gestorSCARA != null)
            ActualizarTextos(gestorSCARA.ObtenerEstado());

        // Posicionar en mundo (offset siempre vertical, independiente de rotación del padre)
        PosicionarEnMundo(_etJ1,  pivotArt1);
        PosicionarEnMundo(_etJ2,  pivotArt2);
        PosicionarEnMundo(_etS1,  giroGarra);
        PosicionarEnMundo(_etZ,   articulacionVertical);
        PosicionarEnMundo(_etTCP, tcp);

        ActualizarVisibilidad();
        OrientarHaciaCamara();
    }

    private void PosicionarEnMundo(TextMesh et, Transform referencia)
    {
        if (et == null || referencia == null) return;
        et.transform.position = referencia.position + offsetMundo;
    }

    // ─── CREACIÓN ─────────────────────────────────────────────────────────────

    private TextMesh CrearEtiqueta(string nombre, Transform padre, string textoInicial)
    {
        if (padre == null) return null;

        GameObject go = new GameObject(nombre);
        // Hijo del padre pero posición se controla en mundo desde LateUpdate
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text          = textoInicial;
        tm.fontSize      = 60;
        tm.characterSize = tamanoFuente * 0.1f;
        tm.color         = colorTexto;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        // NO reemplazar el material — TextMesh usa su propio material con la textura de la fuente

        return tm;
    }

    // ─── ACTUALIZACIÓN ────────────────────────────────────────────────────────

    private void ActualizarTextos(DatosRobot datos)
    {
        int p180J1 = (int)ConfiguracionRobot.Pasos180_J1;
        int p180J2 = (int)ConfiguracionRobot.Pasos180_J2;
        int pRefZ  = (int)ConfiguracionRobot.PasosRef_Z;

        // Formato: valor convertido + paso_actual/pasos_referencia
        if (_etJ1  != null) _etJ1.text  = $"J1\n{datos.anguloJ1 + 90f:F1}°\n{datos.pasosJ1}/{p180J1}p";
        if (_etJ2  != null) _etJ2.text  = $"J2\n{datos.anguloJ2 + 90f:F1}°\n{datos.pasosJ2}/{p180J2}p";
        if (_etS1  != null) _etS1.text  = $"S1\n{datos.anguloS1:F1}°";
        if (_etZ   != null) _etZ.text   = $"Z\n{datos.posicionZ_mm:F2}mm\n{datos.pasosZ}/{pRefZ}p";
        if (_etTCP != null) _etTCP.text = $"TCP\n({datos.tcpX_mm:F0},{datos.tcpY_mm:F0})";
    }

    private void ActualizarVisibilidad()
    {
        SetVisible(_etJ1,  visible);
        SetVisible(_etJ2,  visible);
        SetVisible(_etS1,  visible);
        SetVisible(_etZ,   visible);
        SetVisible(_etTCP, visible);
    }

    private void SetVisible(TextMesh et, bool vis)
    {
        if (et != null) et.gameObject.SetActive(vis);
    }

    private void OrientarHaciaCamara()
    {
        if (_camara == null) return;

        OrientarEtiqueta(_etJ1);
        OrientarEtiqueta(_etJ2);
        OrientarEtiqueta(_etS1);
        OrientarEtiqueta(_etZ);
        OrientarEtiqueta(_etTCP);
    }

    private void OrientarEtiqueta(TextMesh et)
    {
        if (et == null) return;
        et.transform.rotation = Quaternion.LookRotation(
            et.transform.position - _camara.transform.position);
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>Muestra u oculta todas las etiquetas en runtime.</summary>
    public void SetVisibilidad(bool vis) => visible = vis;
}
