using UnityEngine;

/// <summary>
/// Añadir al GameObject CuadriculaSuelo.
/// Ajustar parámetros en Inspector y llamar Reconstruir() con el botón de contexto
/// (clic derecho sobre el componente → Reconstruir) o desde código.
/// NO usa [ExecuteAlways] ni Update — sin fuga de memoria.
/// </summary>
public class ConfiguradorCuadricula : MonoBehaviour
{
    [Header("Dimensiones")]
    [Tooltip("Radio de la cuadrícula en unidades Unity (total = tamano × 2).")]
    [SerializeField] private float tamano   = 50f;
    [Tooltip("Separación entre líneas principales.")]
    [SerializeField] private float paso     = 10f;
    [Tooltip("Separación entre líneas secundarias (debe ser divisor de paso).")]
    [SerializeField] private float pasoMin  =  5f;

    [Header("Posición")]
    [Tooltip("Altura Y mundial de la cuadrícula.")]
    [SerializeField] private float posicionY = 0f;

    [Header("Colores")]
    [SerializeField] private Color colorPrincipal  = new Color(0.25f, 0.40f, 0.70f, 0.9f);
    [SerializeField] private Color colorSecundario = new Color(0.15f, 0.25f, 0.45f, 0.5f);

    [Header("Grosor")]
    [SerializeField] private float anchoPrincipal  = 0.2f;
    [SerializeField] private float anchoSecundario = 0.1f;

    // Material compartido — se crea una sola vez
    private Material _matPrincipal;
    private Material _matSecundario;

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reconstruye la cuadrícula con los valores actuales del Inspector.
    /// Llamar con clic derecho → Reconstruir, o desde código.
    /// </summary>
    [ContextMenu("Reconstruir")]
    public void Reconstruir()
    {
        // Borrar hijos existentes
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
#endif
                Destroy(transform.GetChild(i).gameObject);
        }

        // Crear/reutilizar materiales (solo 2 en total, sin fuga)
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                     ?? Shader.Find("Particles/Alpha Blended")
                     ?? Shader.Find("Sprites/Default");

        if (_matPrincipal  == null) _matPrincipal  = new Material(shader);
        if (_matSecundario == null) _matSecundario = new Material(shader);
        _matPrincipal.color  = colorPrincipal;
        _matSecundario.color = colorSecundario;

        transform.position = new Vector3(transform.position.x, posicionY, transform.position.z);

        int indice = 0;
        for (float x = -tamano; x <= tamano + 0.01f; x += pasoMin)
        {
            bool    esPpal  = Mathf.Abs(x % paso) < 0.1f;
            Material mat    = esPpal ? _matPrincipal  : _matSecundario;
            float   ancho   = esPpal ? anchoPrincipal : anchoSecundario;

            AgregarLinea($"Linea_{indice++}",
                new Vector3(x,       posicionY, -tamano),
                new Vector3(x,       posicionY,  tamano), mat, ancho);

            AgregarLinea($"Linea_{indice++}",
                new Vector3(-tamano, posicionY,  x),
                new Vector3( tamano, posicionY,  x), mat, ancho);
        }
    }

    private void OnDestroy()
    {
        if (_matPrincipal  != null) Destroy(_matPrincipal);
        if (_matSecundario != null) Destroy(_matSecundario);
    }

    // ─── PRIVADO ──────────────────────────────────────────────────────────────

    private void AgregarLinea(string nombre, Vector3 inicio, Vector3 fin,
                               Material mat, float ancho)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);

        LineRenderer lr    = go.AddComponent<LineRenderer>();
        lr.useWorldSpace   = true;
        lr.positionCount   = 2;
        lr.SetPosition(0, inicio);
        lr.SetPosition(1, fin);
        lr.startWidth      = ancho;
        lr.endWidth        = ancho;
        lr.startColor      = mat.color;
        lr.endColor        = mat.color;
        lr.sharedMaterial  = mat;       // compartido, sin fuga
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows  = false;
    }
}
