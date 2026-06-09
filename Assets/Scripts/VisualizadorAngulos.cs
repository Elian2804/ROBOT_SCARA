using UnityEngine;

/// <summary>
/// Dibuja arcos angulares en cada articulación mostrando la posición actual
/// dentro del rango permitido.
/// - Arco de fondo (gris): rango total de la articulación.
/// - Arco de color (amarillo→rojo): posición actual.
/// Usa GL.Lines en OnRenderObject() para dibujo inmediato sin crear objetos extra.
/// </summary>
public class VisualizadorAngulos : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Articulaciones")]
    [SerializeField] private Transform pivotArt1;   // pivot_art1 → J1
    [SerializeField] private Transform pivotArt2;   // pivot_art2 → J2
    [SerializeField] private Transform giroGarra;   // GiroGarra  → S1

    [Header("GestorSCARA (para leer estado)")]
    [SerializeField] private GestorSCARA gestorSCARA;

    // ─── CONFIGURACIÓN VISUAL ─────────────────────────────────────────────────
    [Header("Apariencia de arcos")]
    [SerializeField] private float radioArco      = 0.25f;   // Radio del arco en unidades Unity
    [SerializeField] private float alturaArco     = 0.02f;   // Elevación sobre la articulación
    [SerializeField] private int   segmentosArco  = 48;      // Resolución del arco
    [SerializeField] private bool  mostrar        = true;

    [Header("Dirección de arcos")]
    [Tooltip("Invierte el sentido de giro de todos los arcos (si apuntan al lado contrario, activa esto)")]
    [SerializeField] private bool  invertirDireccion = false;
    [Tooltip("Rota el punto de inicio de todos los arcos en grados (0=eje X, 90=eje Z, 180=-X, etc.)")]
    [SerializeField] private float offsetAnguloBase   = 0f;

    [Header("Colores")]
    [SerializeField] private Color colorFondo     = new Color(0.3f, 0.3f, 0.3f, 0.4f);
    [SerializeField] private Color colorActivo    = new Color(1f,   0.85f, 0f,   0.8f);   // Amarillo
    [SerializeField] private Color colorLimite    = new Color(0.95f, 0.25f, 0.25f, 0.9f); // Rojo al acercarse al límite

    // ─── MATERIAL GL ─────────────────────────────────────────────────────────
    private Material _materialGL;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        // Material para dibujo GL (usa shader Unlit/Color)
        _materialGL = new Material(Shader.Find("Hidden/Internal-Colored"));
        _materialGL.hideFlags = HideFlags.HideAndDontSave;
        _materialGL.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _materialGL.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _materialGL.SetInt("_Cull",      (int)UnityEngine.Rendering.CullMode.Off);
        _materialGL.SetInt("_ZWrite",    0);
    }

    private void OnRenderObject()
    {
        if (!mostrar || gestorSCARA == null) return;

        DatosRobot datos = gestorSCARA.ObtenerEstado();

        _materialGL.SetPass(0);

        // J1 en pivot_art1
        if (pivotArt1 != null)
            DibujarArco(pivotArt1.position + Vector3.up * alturaArco,
                        datos.anguloJ1,
                        ConfiguracionRobot.J1_Min,
                        ConfiguracionRobot.J1_Max);

        // J2 en pivot_art2
        if (pivotArt2 != null)
            DibujarArco(pivotArt2.position + Vector3.up * alturaArco,
                        datos.anguloJ2,
                        ConfiguracionRobot.J2_Min,
                        ConfiguracionRobot.J2_Max);

        // S1 en GiroGarra
        if (giroGarra != null)
            DibujarArco(giroGarra.position + Vector3.up * alturaArco,
                        datos.anguloS1,
                        ConfiguracionRobot.S1_Min,
                        ConfiguracionRobot.S1_Max);
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Muestra u oculta los arcos angulares.</summary>
    public void SetVisible(bool visible)
    {
        mostrar = visible;
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Dibuja el arco de fondo (rango total) y el arco activo (posición actual)
    /// sobre el plano horizontal en la posición indicada.
    /// </summary>
    private void DibujarArco(Vector3 centro, float anguloActual, float min, float max)
    {
        // Arco de fondo: rango completo
        DibujarSegmentoArco(centro, min, max, colorFondo);

        // Determinar color según cercanía al límite (10% del rango)
        float rango        = max - min;
        float umbral       = rango * 0.10f;
        bool  cercaLimite  = (anguloActual >= max - umbral) || (anguloActual <= min + umbral);
        Color colorArco    = cercaLimite ? colorLimite : colorActivo;

        // Arco activo: desde 0 hasta la posición actual (relativo al centro del rango)
        float anguloDesde = min;
        float anguloHasta = Mathf.Clamp(anguloActual, min, max);
        DibujarSegmentoArco(centro, anguloDesde, anguloHasta, colorArco);
    }

    private void DibujarSegmentoArco(Vector3 centro, float desdeGrados, float hastaGrados, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        float signo = invertirDireccion ? -1f : 1f;
        float desde = offsetAnguloBase + desdeGrados * signo;
        float hasta = offsetAnguloBase + hastaGrados * signo;

        float paso = (hasta - desde) / segmentosArco;
        if (Mathf.Abs(paso) < 0.0001f) { GL.End(); return; }

        for (int i = 0; i < segmentosArco; i++)
        {
            float ang1 = Mathf.Deg2Rad * (desde + paso * i);
            float ang2 = Mathf.Deg2Rad * (desde + paso * (i + 1));

            Vector3 p1 = centro + new Vector3(Mathf.Cos(ang1) * radioArco, 0f, Mathf.Sin(ang1) * radioArco);
            Vector3 p2 = centro + new Vector3(Mathf.Cos(ang2) * radioArco, 0f, Mathf.Sin(ang2) * radioArco);

            GL.Vertex(p1);
            GL.Vertex(p2);
        }

        GL.End();
    }

    private void OnDestroy()
    {
        if (_materialGL != null)
            DestroyImmediate(_materialGL);
    }
}
