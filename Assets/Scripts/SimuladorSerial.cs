using UnityEngine;

/// <summary>
/// Simulador de movimiento sin ESP32.
/// TECLAS ARTICULARES (mantener presionada = avance continuo):
///   Q / E  → J1 − / +
///   A / D  → J2 − / +
///   Z / X  → Z − / +
///
/// TECLAS DE ACCIÓN (pulsar una vez):
///   G / F  → Cerrar / Abrir gripper
///   H      → Home
///   U      → Iniciar trayectoria (M2) o PICK (M3)
///   I      → Reiniciar objetos (M2) o DROP (M3)
///   O      → HOME visión (M3)
///
/// El número de pasos por segundo se escala automáticamente con la ratio
/// de reducción seleccionada en los toggles del panel izquierdo.
/// Con 1:1 (100p=180°) a 30p/s: un barrido completo tarda ~3.3 segundos.
/// </summary>
public class SimuladorSerial : MonoBehaviour
{
    // ─── REFERENCIAS ─────────────────────────────────────────────────────────
    [Header("Sistema")]
    [SerializeField] private GestorSCARA        gestorSCARA;
    [SerializeField] private GestorTrayectorias gestorTrayectorias;
    [SerializeField] private GestorVision       gestorVision;

    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────
    [Header("Velocidad de avance (pasos/segundo, manteniendo tecla)")]
    [Tooltip("Pasos por segundo para J1/J2 (mantener tecla). " +
             "Con 1:1 (100p=180°): 30p/s = 54°/s — un barrido completo en ~3.3s.")]
    [SerializeField] private float pasosSegundo = 30f;

    [Tooltip("Velocidad del eje Z en mm/segundo (mantener tecla). " +
             "Z usa mm directamente porque es lineal. 20mm/s = recorre 200mm en 10s.")]
    [SerializeField] private float velocidadZ_mm_s = 20f;

    [Tooltip("Velocidad de rotación de la garra S1 en grados/segundo (teclas R/T). " +
             "60°/s = recorre los 180° en 3 segundos.")]
    [SerializeField] private float velocidadS1_deg_s = 60f;

    [Header("Teclado")]
    [SerializeField] private bool tecladoActivo = true;

    // ─── ACUMULADORES J1/J2 (fracción de paso pendiente entre frames) ────────
    private float _acumJ1 = 0f;
    private float _acumJ2 = 0f;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        if (gestorSCARA == null)
            Debug.LogError("[SimuladorSerial] gestorSCARA no asignado en Inspector.");
    }

    private void Update()
    {
        if (!tecladoActivo) return;

        float dt = Time.deltaTime;

        // ── Ejes articulares (mantener presionada) ────────────────────────────
        ProcesarEjeJ1(dt);
        ProcesarEjeJ2(dt);
        ProcesarEjeZMm(dt);   // Z usa mm/s directo
        ProcesarEjeS1(dt);    // R/T → rotación garra 0°–180°

        // ── Acciones de pulsación única ───────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.G)) CerrarGripper();
        if (Input.GetKeyDown(KeyCode.F)) AbrirGripper();
        if (Input.GetKeyDown(KeyCode.H)) IrHome();

        if (Input.GetKeyDown(KeyCode.U))
        {
            if (gestorTrayectorias != null) IniciarTrayectoria();
            else SimularPick();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (gestorTrayectorias != null) ReiniciarObjetos();
            else SimularPlace();
        }
        if (Input.GetKeyDown(KeyCode.O)) SimularHomeVision();
    }

    // ─── PROCESAMIENTO POR EJE ────────────────────────────────────────────────

    private void ProcesarEjeJ1(float dt)
    {
        float dir = ObtenerDireccion(KeyCode.Q, KeyCode.E);
        if (dir == 0f) { _acumJ1 = 0f; return; }

        _acumJ1 += dir * pasosSegundo * dt;
        int pasos = (int)_acumJ1;
        if (pasos != 0)
        {
            gestorSCARA?.SimularPasoJ1(pasos);
            _acumJ1 -= pasos;
        }
    }

    private void ProcesarEjeJ2(float dt)
    {
        float dir = ObtenerDireccion(KeyCode.A, KeyCode.D);
        if (dir == 0f) { _acumJ2 = 0f; return; }

        _acumJ2 += dir * pasosSegundo * dt;
        int pasos = (int)_acumJ2;
        if (pasos != 0)
        {
            gestorSCARA?.SimularPasoJ2(pasos);
            _acumJ2 -= pasos;
        }
    }

    // Z es lineal: usa mm/s directamente para que el movimiento visual sea correcto.
    private void ProcesarEjeZMm(float dt)
    {
        float dir = ObtenerDireccion(KeyCode.Z, KeyCode.X);
        if (dir == 0f) return;
        gestorSCARA?.SimularZ(dir * velocidadZ_mm_s * dt);
    }

    // S1 (GiroGarra): rotación continua 0°–180°. R = menos, T = más.
    private void ProcesarEjeS1(float dt)
    {
        float dir = ObtenerDireccion(KeyCode.R, KeyCode.T);
        if (dir == 0f) return;
        gestorSCARA?.SimularS1(dir * velocidadS1_deg_s * dt);
    }

    // Devuelve -1, 0 o +1 según las teclas de menos/más
    private static float ObtenerDireccion(KeyCode menos, KeyCode mas)
    {
        if (Input.GetKey(mas))   return  1f;
        if (Input.GetKey(menos)) return -1f;
        return 0f;
    }

    // ─── ACCIONES PÚBLICAS (también asignables desde Inspector) ──────────────

    public void AbrirGripper()  => gestorSCARA?.AbrirGripper();
    public void CerrarGripper() => gestorSCARA?.CerrarGripper();

    public void IrHome()
    {
        if (gestorSCARA == null) return;
        StartCoroutine(gestorSCARA.CorrutinaIrAHome());
    }

    public void IniciarTrayectoria()
    {
        if (gestorTrayectorias != null) gestorTrayectorias.IniciarSecuencia();
        else Debug.LogWarning("[SimuladorSerial] GestorTrayectorias no asignado.");
    }

    public void ReiniciarObjetos()
    {
        if (gestorTrayectorias != null) gestorTrayectorias.ReiniciarObjetos();
        else Debug.LogWarning("[SimuladorSerial] GestorTrayectorias no asignado.");
    }

    public void SimularPick()
    {
        if (gestorVision != null) gestorVision.EnviarComando("PICK");
        else Debug.LogWarning("[SimuladorSerial] GestorVision no asignado.");
    }

    public void SimularPlace()
    {
        if (gestorVision != null) gestorVision.EnviarComando("DROP");
        else Debug.LogWarning("[SimuladorSerial] GestorVision no asignado.");
    }

    public void SimularHomeVision()
    {
        if (gestorVision != null) gestorVision.EnviarComando("HOME");
        else Debug.LogWarning("[SimuladorSerial] GestorVision no asignado.");
    }
}
