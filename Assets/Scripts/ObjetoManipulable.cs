using UnityEngine;

/// <summary>
/// Representa un objeto físico que el robot SCARA puede recoger y depositar.
/// Se usa en el Módulo 2 (Trayectorias) y Módulo 3 (Visión BT).
/// No requiere Rigidbody — el movimiento es cinemático visual.
/// </summary>
public class ObjetoManipulable : MonoBehaviour
{
    // ─── CONFIGURACIÓN (Inspector) ────────────────────────────────────────────
    [Header("Identificación")]
    [SerializeField] public string nombreObjeto = "Pieza";
    [SerializeField] public Color  colorObjeto  = Color.red;

    [Header("Zona de depósito")]
    [Tooltip("Transform donde debe quedar el objeto al ser soltado (asignar zona de depósito).")]
    [SerializeField] private Transform zonaDeposito;

    [Header("Distancia de detección")]
    [Tooltip("Distancia máxima en unidades Unity para que el gripper lo detecte.")]
    [SerializeField] public float radioDeteccion = 5f;

    // ─── ESTADO ───────────────────────────────────────────────────────────────
    public enum EstadoObjeto { Libre, Transportado, Depositado }
    public EstadoObjeto Estado { get; private set; } = EstadoObjeto.Libre;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private Vector3    _posicionOrigen;
    private Quaternion _rotacionOrigen;
    private Renderer   _renderer;
    private Rigidbody  _rb;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        _posicionOrigen = transform.position;
        _rotacionOrigen = transform.rotation;
        _renderer       = GetComponent<Renderer>();

        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        // Rigidbody para física: el cubo se mueve al ser empujado por las garras
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.useGravity  = false;   // SCARA trabaja en plano horizontal
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezeRotation;
        }

        AplicarColor();
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>
    /// El gripper agarra el objeto: se parenta al TCP y lo sigue.
    /// </summary>
    public void Agarrar(Transform tcp)
    {
        if (Estado != EstadoObjeto.Libre) return;

        Estado              = EstadoObjeto.Transportado;
        if (_rb != null) _rb.isKinematic = true;   // desactivar física mientras es transportado
        transform.SetParent(tcp, worldPositionStays: true);
        AplicarColorEstado();
    }

    /// <summary>
    /// El gripper suelta el objeto en la posición actual (o zona de depósito si está configurada).
    /// </summary>
    public void Soltar()
    {
        if (Estado != EstadoObjeto.Transportado) return;

        transform.SetParent(null);
        if (_rb != null) _rb.isKinematic = false;   // reactivar física al soltar

        if (zonaDeposito != null)
        {
            transform.position = zonaDeposito.position;
            transform.rotation = zonaDeposito.rotation;
        }

        Estado = EstadoObjeto.Depositado;
        AplicarColorEstado();
    }

    /// <summary>
    /// Devuelve el objeto a su posición y estado inicial.
    /// </summary>
    public void Reiniciar()
    {
        transform.SetParent(null);
        if (_rb != null) { _rb.isKinematic = true; _rb.velocity = Vector3.zero; }
        transform.position = _posicionOrigen;
        transform.rotation = _rotacionOrigen;
        if (_rb != null) _rb.isKinematic = false;
        Estado             = EstadoObjeto.Libre;
        AplicarColorEstado();
    }

    /// <summary>True si el objeto está dentro del radio de detección del punto dado.</summary>
    public bool EstaCercaDe(Vector3 punto)
    {
        return Vector3.Distance(transform.position, punto) <= radioDeteccion;
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void AplicarColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = colorObjeto;
    }

    private void AplicarColorEstado()
    {
        if (_renderer == null) return;
        switch (Estado)
        {
            case EstadoObjeto.Libre:
                _renderer.material.color = colorObjeto;
                break;
            case EstadoObjeto.Transportado:
                _renderer.material.color = Color.Lerp(colorObjeto, Color.white, 0.4f);
                break;
            case EstadoObjeto.Depositado:
                _renderer.material.color = Color.Lerp(colorObjeto, Color.green, 0.5f);
                break;
        }
    }
}
