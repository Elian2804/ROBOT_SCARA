using UnityEngine;

/// <summary>
/// Posiciona una esfera marcadora en el punto TCP (Tool Center Point) del robot.
/// Se actualiza en LateUpdate() para seguir el Transform del TCP después de
/// que GestorSCARA haya movido el modelo en Update().
/// </summary>
public class VisualizadorTCP : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Referencia al TCP del robot")]
    [SerializeField] private Transform transformTCP;   // Arrastrar el GameObject TCP de la jerarquía

    [Header("Esfera marcadora")]
    [SerializeField] private GameObject esferaTCP;     // Esfera ya existente en la escena o creada aquí
    [SerializeField] private Material   materialTCP;   // Material de la esfera (amarillo brillante)

    [Header("Configuración visual")]
    [SerializeField] private float escalaEsfera = 10f;   // Tamaño de la esfera en unidades Unity (metros)
    [SerializeField] private bool  mostrar       = true;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private GameObject _esferaInstancia;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        CrearEsferaSiNoExiste();
    }

    private void LateUpdate()
    {
        ActualizarPosicion();
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Muestra u oculta la esfera del TCP.</summary>
    public void SetVisible(bool visible)
    {
        mostrar = visible;
        if (_esferaInstancia != null)
            _esferaInstancia.SetActive(visible);
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void CrearEsferaSiNoExiste()
    {
        // Si ya hay una esfera asignada en el Inspector, usarla directamente
        if (esferaTCP != null)
        {
            _esferaInstancia = esferaTCP;
            AplicarEscalaYMaterial();
            return;
        }

        // Crear una esfera primitiva nueva
        _esferaInstancia = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _esferaInstancia.name = "TCP_Marcador";

        // Eliminar el collider para no interferir con la física
        Collider col = _esferaInstancia.GetComponent<Collider>();
        if (col != null) Destroy(col);

        AplicarEscalaYMaterial();
    }

    private void AplicarEscalaYMaterial()
    {
        if (_esferaInstancia == null) return;

        _esferaInstancia.transform.localScale = Vector3.one * escalaEsfera;

        if (materialTCP != null)
        {
            Renderer rend = _esferaInstancia.GetComponent<Renderer>();
            if (rend != null) rend.material = materialTCP;
        }
        else
        {
            // Color por defecto: amarillo del sistema de colores
            Renderer rend = _esferaInstancia.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = ColoresUI.AcentoAmarillo;
        }

        _esferaInstancia.SetActive(mostrar);
    }

    private void ActualizarPosicion()
    {
        if (_esferaInstancia == null || transformTCP == null) return;

        _esferaInstancia.transform.position = transformTCP.position;
    }
}
