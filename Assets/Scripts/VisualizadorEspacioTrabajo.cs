using UnityEngine;

/// <summary>
/// Dibuja un anillo semitransparente en el suelo que representa el espacio de trabajo
/// alcanzable por el TCP del robot SCARA.
/// Radio interior = ConfiguracionRobot.EspacioMin * MM_A_UNITY
/// Radio exterior = ConfiguracionRobot.EspacioMax * MM_A_UNITY
/// Se dibuja mediante un Mesh generado por procedimiento (torus aplanado).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisualizadorEspacioTrabajo : MonoBehaviour
{
    // ─── CONFIGURACIÓN VISUAL (asignar desde Inspector) ───────────────────────
    [Header("Apariencia")]
    [SerializeField] private Color colorAnillo        = new Color(1f, 0.85f, 0f, 0.18f);   // Amarillo semitransparente
    [SerializeField] private Color colorBordeExterior = new Color(1f, 0.85f, 0f, 0.55f);   // Amarillo más opaco
    [SerializeField] private bool  mostrar            = true;

    [Header("Geometría")]
    [SerializeField] private int   segmentos         = 64;    // Más segmentos = anillo más suave
    [SerializeField] private float multiplicadorEscala = 10f; // Factor para ajustar el anillo al modelo en metros

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private MeshFilter   _filtroMesh;
    private MeshRenderer _renderMesh;
    private Material     _material;
    private float        _radioInteriorActual = -1f;
    private float        _radioExteriorActual = -1f;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        _filtroMesh  = GetComponent<MeshFilter>();
        _renderMesh  = GetComponent<MeshRenderer>();

        _material = new Material(Shader.Find("Sprites/Default"));
        _material.color = colorAnillo;
        _material.renderQueue = 3000;   // Transparente, sobre el suelo
        _renderMesh.material  = _material;
        _renderMesh.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderMesh.receiveShadows     = false;

        ConstruirAnillo();
    }

    private void Update()
    {
        float radioInt = ConfiguracionRobot.EspacioMin  * ConfiguracionRobot.MM_A_UNITY * multiplicadorEscala;
        float radioExt = ConfiguracionRobot.EspacioMax  * ConfiguracionRobot.MM_A_UNITY * multiplicadorEscala;

        if (!Mathf.Approximately(radioInt, _radioInteriorActual) ||
            !Mathf.Approximately(radioExt, _radioExteriorActual))
        {
            ConstruirAnillo();
        }

        if (_renderMesh != null)
            _renderMesh.enabled = mostrar;
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Muestra u oculta el anillo.</summary>
    public void SetVisible(bool visible)
    {
        mostrar = visible;
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void ConstruirAnillo()
    {
        float radioInt = ConfiguracionRobot.EspacioMin * ConfiguracionRobot.MM_A_UNITY * multiplicadorEscala;
        float radioExt = ConfiguracionRobot.EspacioMax * ConfiguracionRobot.MM_A_UNITY * multiplicadorEscala;

        _radioInteriorActual = radioInt;
        _radioExteriorActual = radioExt;

        Mesh malla = new Mesh();
        malla.name = "AnilloEspacioTrabajo";

        int cantVertices = segmentos * 2;
        Vector3[] vertices  = new Vector3[cantVertices];
        Vector2[] uvs       = new Vector2[cantVertices];
        int[]     triangulos = new int[segmentos * 6];

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (float)i / segmentos * Mathf.PI * 2f;
            float cos    = Mathf.Cos(angulo);
            float sin    = Mathf.Sin(angulo);

            // Vértice interior
            vertices[i * 2]     = new Vector3(cos * radioInt, 0f, sin * radioInt);
            // Vértice exterior
            vertices[i * 2 + 1] = new Vector3(cos * radioExt, 0f, sin * radioExt);

            float u = (float)i / segmentos;
            uvs[i * 2]     = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        // Construir triángulos (quad por segmento)
        for (int i = 0; i < segmentos; i++)
        {
            int siguiente = (i + 1) % segmentos;
            int b = i * 6;

            triangulos[b]     = i * 2;
            triangulos[b + 1] = siguiente * 2;
            triangulos[b + 2] = i * 2 + 1;

            triangulos[b + 3] = siguiente * 2;
            triangulos[b + 4] = siguiente * 2 + 1;
            triangulos[b + 5] = i * 2 + 1;
        }

        malla.vertices  = vertices;
        malla.uv        = uvs;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        malla.RecalculateBounds();

        _filtroMesh.mesh = malla;
    }
}
