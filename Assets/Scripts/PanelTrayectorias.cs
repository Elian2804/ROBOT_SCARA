using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel derecho exclusivo del Módulo 2 (Trayectorias).
/// El estudiante mueve el robot con botones de pasos y graba posiciones.
/// No hay entrada de coordenadas XYZ ni cinemática inversa.
/// </summary>
public class PanelTrayectorias : MonoBehaviour
{
    // ─── SLIDERS / BOTONES DE MOVIMIENTO ─────────────────────────────────────
    [Header("Control de movimiento por pasos")]
    [Tooltip("GestorSCARA para llamar SimularPasoJ1/J2/Z desde el Inspector.")]
    [SerializeField] private GestorSCARA gestorSCARA;
    [SerializeField] private TMP_InputField inputPasosPorClick;   // pasos por pulsación (default 5)

    // ─── LISTA DE PUNTOS GRABADOS ─────────────────────────────────────────────
    [Header("Puntos grabados")]
    [SerializeField] private Transform        contenedorPuntos;   // VerticalLayoutGroup + ScrollRect
    [SerializeField] private GameObject       prefabItemPunto;    // TextMeshProUGUI con el resumen del punto
    [SerializeField] private ScrollRect       scrollPuntos;
    [SerializeField] private TextMeshProUGUI  textoContadorPuntos;

    // ─── BOTONES DE TRAYECTORIA ───────────────────────────────────────────────
    [Header("Botones de trayectoria")]
    [SerializeField] private Button           botonGuardarPunto;
    [SerializeField] private Button           botonEjecutar;      // habilitado cuando ≥2 puntos
    [SerializeField] private Button           botonLimpiar;
    [SerializeField] private TMP_InputField   inputNombreTrayectoria;   // "PICK", "DROP", etc.
    [SerializeField] private Button           botonGuardarNombre;
    [SerializeField] private Button           botonReiniciarObjetos;

    // ─── CONTROL DE GRIPPER EN CADA PUNTO ────────────────────────────────────
    [Header("Gripper en puntos (índice 0-based)")]
    [SerializeField] private TMP_InputField   inputIndiceCerrar;  // índice donde cerrar gripper
    [SerializeField] private TMP_InputField   inputIndiceAbrir;   // índice donde abrir gripper

    // ─── ESTADO TRAYECTORIA ───────────────────────────────────────────────────
    [Header("Estado de ejecución")]
    [SerializeField] private TextMeshProUGUI  textoFaseActual;
    [SerializeField] private Image            imagenIndicadorTrayectoria;

    // ─── ESTADO GRIPPER ───────────────────────────────────────────────────────
    [Header("Estado del gripper")]
    [SerializeField] private TextMeshProUGUI  textoEstadoGripper;
    [SerializeField] private Image            imagenEstadoGripper;

    // ─── CONTADORES ───────────────────────────────────────────────────────────
    [Header("Contadores pick-and-place")]
    [SerializeField] private TextMeshProUGUI textoExitos;
    [SerializeField] private TextMeshProUGUI textoFallos;
    [SerializeField] private TextMeshProUGUI textoTotalCiclos;

    // ─── EVENTOS PÚBLICOS ─────────────────────────────────────────────────────
    public System.Action                 OnGuardarPunto;
    public System.Action                 OnEjecutarTrayectoria;
    public System.Action                 OnLimpiarPuntos;
    public System.Action<string>         OnGuardarComoNombre;
    public System.Action                 OnReiniciarObjetos;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private int _contadorExitos  = 0;
    private int _contadorFallos  = 0;
    private int _cantidadPuntos  = 0;
    private int _indiceCerrar    = -1;
    private int _indiceAbrir     = -1;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        if (inputPasosPorClick != null && string.IsNullOrEmpty(inputPasosPorClick.text))
            inputPasosPorClick.text = "5";
        if (botonEjecutar != null) botonEjecutar.interactable = false;
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    public void Inicializar()
    {
        ReiniciarContadores();
        ActualizarFase("En espera");
        _cantidadPuntos = 0;
        ActualizarContadorPuntos();
        if (botonEjecutar != null) botonEjecutar.interactable = false;
    }

    public void ActualizarEstado(DatosRobot datos)
    {
        ActualizarGripper(datos);
    }

    public void RegistrarExito()
    {
        _contadorExitos++;
        ActualizarContadores();
    }

    public void RegistrarFallo()
    {
        _contadorFallos++;
        ActualizarContadores();
    }

    public void ActualizarFase(string nombreFase)
    {
        if (textoFaseActual != null)
            textoFaseActual.text = $"Fase: {nombreFase}";
    }

    /// <summary>
    /// Recibe la desviación del TCP respecto a la trayectoria de referencia.
    /// Llamado por VisualizadorTrayectoria en cada LateUpdate.
    /// </summary>
    public void ActualizarDesviacion(float desvMm)
    {
        if (imagenIndicadorTrayectoria == null) return;
        Color color = desvMm < 2f ? ColoresUI.Exito
                    : desvMm < 5f ? ColoresUI.Advertencia
                    : ColoresUI.Error;
        imagenIndicadorTrayectoria.color = color;
    }

    /// <summary>
    /// Recibe la lista actualizada de puntos de GestorTrayectorias
    /// y la muestra en el contenedor con sus valores de pasos.
    /// </summary>
    public void ActualizarListaPuntos(List<GestorTrayectorias.PuntoTrayectoria> puntos)
    {
        if (contenedorPuntos == null || prefabItemPunto == null) return;

        // Destruir items anteriores
        foreach (Transform hijo in contenedorPuntos)
            Destroy(hijo.gameObject);

        _cantidadPuntos = puntos.Count;

        for (int i = 0; i < puntos.Count; i++)
        {
            var pt = puntos[i];
            GameObject item = Instantiate(prefabItemPunto, contenedorPuntos);
            TextMeshProUGUI tmp = item.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                float j1  = ConfiguracionRobot.PasosAGradosJ1(pt.pasosM2);
                float j2  = ConfiguracionRobot.PasosAGradosJ2(pt.pasosM3);
                float zmm = ConfiguracionRobot.PasosAMmZ(pt.pasosM1);
                tmp.text  = $"<b>{pt.nombre}</b>  M1:{pt.pasosM1}  M2:{pt.pasosM2}  M3:{pt.pasosM3}\n" +
                            $"  J1:{j1:F1}°  J2:{j2:F1}°  Z:{zmm:F1}mm";
                tmp.color = ColoresUI.TextoSecundario;
            }
        }

        ActualizarContadorPuntos();
        if (botonEjecutar != null) botonEjecutar.interactable = puntos.Count >= 2;
    }

    /// <summary>Consulta de GestorTrayectorias: ¿debe cerrarse el gripper en este índice?</summary>
    public bool DebeAgarrarEnPunto(int indice) => indice == _indiceCerrar;

    /// <summary>Consulta de GestorTrayectorias: ¿debe abrirse el gripper en este índice?</summary>
    public bool DebebSoltarEnPunto(int indice) => indice == _indiceAbrir;

    // ─── HANDLERS DE BOTONES (asignar desde Inspector) ───────────────────────

    public void AlPresionarGuardarPunto()     => OnGuardarPunto?.Invoke();
    public void AlPresionarEjecutar()         => OnEjecutarTrayectoria?.Invoke();
    public void AlPresionarLimpiar()          => OnLimpiarPuntos?.Invoke();
    public void AlPresionarReiniciarObjetos() => OnReiniciarObjetos?.Invoke();

    public void AlPresionarGuardarNombre()
    {
        string nombre = inputNombreTrayectoria != null ? inputNombreTrayectoria.text.Trim() : "";
        if (!string.IsNullOrEmpty(nombre))
            OnGuardarComoNombre?.Invoke(nombre);
    }

    public void AlCambiarIndiceCerrar(string valor)
    {
        _indiceCerrar = int.TryParse(valor, out int v) ? v : -1;
    }

    public void AlCambiarIndiceAbrir(string valor)
    {
        _indiceAbrir = int.TryParse(valor, out int v) ? v : -1;
    }

    // ─── BOTONES DE MOVIMIENTO POR PASOS (asignar desde Inspector) ───────────

    public void J1Mas()    => gestorSCARA?.SimularPasoJ1(+ObtenerPasos());
    public void J1Menos()  => gestorSCARA?.SimularPasoJ1(-ObtenerPasos());
    public void J2Mas()    => gestorSCARA?.SimularPasoJ2(+ObtenerPasos());
    public void J2Menos()  => gestorSCARA?.SimularPasoJ2(-ObtenerPasos());
    public void ZMas()     => gestorSCARA?.SimularPasoZ(+ObtenerPasos());
    public void ZMenos()   => gestorSCARA?.SimularPasoZ(-ObtenerPasos());

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private int ObtenerPasos()
    {
        if (inputPasosPorClick == null || !int.TryParse(inputPasosPorClick.text, out int v))
            return 5;
        return Mathf.Max(1, v);
    }

    private void ActualizarGripper(DatosRobot datos)
    {
        bool cerrado = datos.anguloS2 < (ConfiguracionRobot.S2_Max * 0.2f);
        if (textoEstadoGripper != null)
        {
            if (datos.objetoAgarrado)
            {
                textoEstadoGripper.text  = "AGARRADO";
                textoEstadoGripper.color = ColoresUI.Exito;
            }
            else
            {
                textoEstadoGripper.text  = cerrado ? "CERRADO" : "ABIERTO";
                textoEstadoGripper.color = cerrado ? ColoresUI.TextoPrincipal : ColoresUI.Advertencia;
            }
        }
        if (imagenEstadoGripper != null)
            imagenEstadoGripper.color = datos.objetoAgarrado ? ColoresUI.Exito : ColoresUI.TextoSecundario;
    }

    private void ReiniciarContadores()
    {
        _contadorExitos = _contadorFallos = 0;
        ActualizarContadores();
    }

    private void ActualizarContadores()
    {
        int total = _contadorExitos + _contadorFallos;
        if (textoExitos      != null) textoExitos.text      = $"Éxitos: {_contadorExitos}";
        if (textoFallos      != null) textoFallos.text      = $"Fallos: {_contadorFallos}";
        if (textoTotalCiclos != null) textoTotalCiclos.text = $"Total:  {total}";
        if (textoExitos != null)
            textoExitos.color = _contadorExitos > 0 ? ColoresUI.Exito : ColoresUI.TextoSecundario;
        if (textoFallos != null)
            textoFallos.color = _contadorFallos > 0 ? ColoresUI.Error : ColoresUI.TextoSecundario;
    }

    private void ActualizarContadorPuntos()
    {
        if (textoContadorPuntos != null)
            textoContadorPuntos.text = $"Puntos: {_cantidadPuntos}";
    }
}
