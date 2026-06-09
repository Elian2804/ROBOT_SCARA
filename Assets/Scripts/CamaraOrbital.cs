using UnityEngine;

/// <summary>
/// Cámara orbital libre para inspeccionar el robot en 3D.
/// Controles:
///   - Click izquierdo O derecho + arrastrar → rotar alrededor del objetivo
///   - Rueda del mouse                        → zoom (acercar/alejar)
///   - Click medio + arrastrar                → desplazar (pan)
///   - Teclas 1,2,3,4 o R                    → vistas predefinidas / reset
/// Restricción: la cámara NUNCA baja del plano horizontal del robot.
/// </summary>
public class CamaraOrbital : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Punto de pivote")]
    [SerializeField] private Transform puntoObjetivo;   // El robot o su base

    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────
    [Header("Velocidades")]
    [SerializeField] private float velocidadRotacion  = 200f;
    [SerializeField] private float velocidadZoom      =   5f;
    [SerializeField] private float velocidadPan       =   0.5f;
    [SerializeField] private float velocidadTransicion = 5f;    // Lerp a vistas predefinidas

    [Header("Límites de zoom")]
    [SerializeField] private float distanciaMin = 0.5f;
    [SerializeField] private float distanciaMax = 10f;

    [Header("Límites verticales")]
    [Tooltip("Ángulo mínimo sobre el horizonte (>0 impide ver desde abajo).")]
    [SerializeField] private float anguloXMinimo = 2f;
    [Tooltip("Ángulo máximo (89 = casi cenital).")]
    [SerializeField] private float anguloXMaximo = 89f;

    [Header("Vista por defecto (isométrica)")]
    [SerializeField] private float anguloXDefecto =  35f;
    [SerializeField] private float anguloYDefecto = 225f;
    [SerializeField] private float distanciaDefecto = 4f;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private float _anguloX;
    private float _anguloY;
    private float _distancia;
    private Vector3 _desplazamiento = Vector3.zero;

    // Objetivos para transición suave a vistas predefinidas
    private float  _anguloXObjetivo;
    private float  _anguloYObjetivo;
    private float  _distanciaObjetivo;
    private bool   _enTransicion = false;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        // Iniciar en la vista isométrica por defecto
        _anguloX         = anguloXDefecto;
        _anguloY         = anguloYDefecto;
        _distancia       = distanciaDefecto;
        _anguloXObjetivo = anguloXDefecto;
        _anguloYObjetivo = anguloYDefecto;
        _distanciaObjetivo = distanciaDefecto;

        AplicarPosicion();
    }

    private void LateUpdate()
    {
        if (_enTransicion)
        {
            InterpolarAVistaPredefinida();
            return;
        }

        ProcesarRotacion();
        ProcesarZoom();
        ProcesarPan();
        AplicarPosicion();
    }

    // ─── MÉTODOS PÚBLICOS (asignar a botones desde Inspector) ────────────────

    /// <summary>Vista frontal del robot. Asignar al botón Vista Frontal.</summary>
    public void IrAVistaFrontal()
    {
        IniciarTransicion(20f, 180f, distanciaDefecto);
    }

    /// <summary>Vista lateral derecha. Asignar al botón Vista Lateral.</summary>
    public void IrAVistaLateral()
    {
        IniciarTransicion(20f, 90f, distanciaDefecto);
    }

    /// <summary>Vista superior cenital. Asignar al botón Vista Superior.</summary>
    public void IrAVistaSuperior()
    {
        IniciarTransicion(89f, 0f, distanciaDefecto);
    }

    /// <summary>Vista isométrica por defecto. Asignar al botón Reset Cámara.</summary>
    public void IrAVistaIsometrica()
    {
        IniciarTransicion(anguloXDefecto, anguloYDefecto, distanciaDefecto);
    }

    // ─── ENTRADA DE TECLADO ───────────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) IrAVistaFrontal();
        if (Input.GetKeyDown(KeyCode.Alpha2)) IrAVistaLateral();
        if (Input.GetKeyDown(KeyCode.Alpha3)) IrAVistaSuperior();
        if (Input.GetKeyDown(KeyCode.Alpha4)) IrAVistaIsometrica();
        if (Input.GetKeyDown(KeyCode.R))      IrAVistaIsometrica();
    }

    // ─── CONTROLES DE CÁMARA ─────────────────────────────────────────────────

    private void ProcesarRotacion()
    {
        // Click izquierdo (0) o click derecho (1) — cualquiera rota la cámara
        bool rotando = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        if (!rotando) return;

        // Click izquierdo NO debe interferir con botones UI — verificar que el cursor
        // no esté sobre un elemento de UI antes de rotar
        if (Input.GetMouseButton(0) &&
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        _anguloY += Input.GetAxis("Mouse X") * velocidadRotacion * Time.deltaTime;
        _anguloX -= Input.GetAxis("Mouse Y") * velocidadRotacion * Time.deltaTime;

        // Bloquear vista desde abajo: mínimo anguloXMinimo grados sobre el horizonte
        _anguloX  = Mathf.Clamp(_anguloX, anguloXMinimo, anguloXMaximo);
    }

    private void ProcesarZoom()
    {
        float rueda = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(rueda) < 0.001f) return;

        _distancia -= rueda * velocidadZoom;
        _distancia  = Mathf.Clamp(_distancia, distanciaMin, distanciaMax);
    }

    private void ProcesarPan()
    {
        if (!Input.GetMouseButton(2)) return;   // Click medio

        float dx = -Input.GetAxis("Mouse X") * velocidadPan;
        float dy = -Input.GetAxis("Mouse Y") * velocidadPan;

        _desplazamiento += transform.right * dx + transform.up * dy;
    }

    private void AplicarPosicion()
    {
        Quaternion rotacion = Quaternion.Euler(_anguloX, _anguloY, 0f);
        Vector3 origen = (puntoObjetivo != null ? puntoObjetivo.position : Vector3.zero) + _desplazamiento;

        transform.position = origen + rotacion * new Vector3(0f, 0f, -_distancia);
        transform.LookAt(origen);
    }

    // ─── TRANSICIÓN SUAVE ─────────────────────────────────────────────────────

    private void IniciarTransicion(float anguloX, float anguloY, float distancia)
    {
        _anguloXObjetivo   = anguloX;
        _anguloYObjetivo   = anguloY;
        _distanciaObjetivo = distancia;
        _desplazamiento    = Vector3.zero;
        _enTransicion      = true;
    }

    private void InterpolarAVistaPredefinida()
    {
        float t = velocidadTransicion * Time.deltaTime;

        _anguloX   = Mathf.LerpAngle(_anguloX,   _anguloXObjetivo,   t);
        _anguloY   = Mathf.LerpAngle(_anguloY,   _anguloYObjetivo,   t);
        _distancia = Mathf.Lerp(_distancia, _distanciaObjetivo, t);

        AplicarPosicion();

        // Terminar transición cuando la diferencia es mínima
        bool llegoCercaniaAngX = Mathf.Abs(Mathf.DeltaAngle(_anguloX, _anguloXObjetivo)) < 0.1f;
        bool llegoCercaniaAngY = Mathf.Abs(Mathf.DeltaAngle(_anguloY, _anguloYObjetivo)) < 0.1f;
        bool llegoCercaniaDist = Mathf.Abs(_distancia - _distanciaObjetivo) < 0.01f;

        if (llegoCercaniaAngX && llegoCercaniaAngY && llegoCercaniaDist)
        {
            _anguloX   = _anguloXObjetivo;
            _anguloY   = _anguloYObjetivo;
            _distancia = _distanciaObjetivo;
            _enTransicion = false;
        }
    }
}
