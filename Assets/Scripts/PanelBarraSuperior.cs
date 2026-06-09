using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel superior de la interfaz: indicador serial, nombre del módulo,
/// selector de puerto, botón conectar y alerta de overflow.
/// Todas las referencias se asignan desde el Inspector.
/// </summary>
public class PanelBarraSuperior : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Indicador de conexión")]
    [SerializeField] private Image  imagenIndicadorSerial;
    [SerializeField] private TextMeshProUGUI textoNombrePuerto;
    [SerializeField] private TextMeshProUGUI textoModulo;

    [Header("Selector de puerto y botón")]
    [SerializeField] private TMP_Dropdown desplegablePuerto;
    [SerializeField] private Button       botonConectar;
    [SerializeField] private TextMeshProUGUI textoBotonConectar;

    [Header("Botón salir")]
    [SerializeField] private Button botonSalir;

    [Header("Alerta overflow (oculto por defecto)")]
    [SerializeField] private GameObject    contenedorAlertaOverflow;
    [SerializeField] private TextMeshProUGUI textoAlertaOverflow;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private GestorSCARA _gestor;
    private bool        _conectado = false;

    private float _timerRefrescoPuertos = 0f;
    private const float IntervaloRefrescoPuertosS = 2f;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Update()
    {
        if (_conectado) return;

        _timerRefrescoPuertos -= Time.deltaTime;
        if (_timerRefrescoPuertos <= 0f)
        {
            _timerRefrescoPuertos = IntervaloRefrescoPuertosS;
            ActualizarListaPuertos();
        }
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Inicializar referencias y estado inicial. Llamado por GestorSCARA.</summary>
    public void Inicializar(GestorSCARA gestor)
    {
        _gestor = gestor;

        // Borrar cualquier COM guardado de sesiones anteriores — arranque limpio siempre
        PlayerPrefs.DeleteKey("UltimoPuertoCOM");
        PlayerPrefs.Save();

        if (contenedorAlertaOverflow != null)
            contenedorAlertaOverflow.SetActive(false);

        ActualizarListaPuertos();
        ActualizarEstadoConexion(false, "");
    }

    /// <summary>
    /// Actualiza el indicador visual de conexión serial.
    /// Llamado por GestorSCARA después de conectar/desconectar.
    /// </summary>
    public void ActualizarEstadoConexion(bool conectado, string puerto)
    {
        _conectado = conectado;

        if (imagenIndicadorSerial != null)
            imagenIndicadorSerial.color = conectado ? ColoresUI.Exito : ColoresUI.Error;

        if (textoNombrePuerto != null)
            textoNombrePuerto.text = conectado ? puerto : "Desconectado";

        if (textoBotonConectar != null)
            textoBotonConectar.text = conectado ? "Desconectar" : "Conectar";
    }

    /// <summary>Muestra u oculta la alerta de overflow del buffer.</summary>
    public void MostrarAlertaOverflow(bool visible, int cantidadPerdidos = 0)
    {
        if (contenedorAlertaOverflow != null)
            contenedorAlertaOverflow.SetActive(visible);

        if (textoAlertaOverflow != null && visible)
            textoAlertaOverflow.text = $"OVERFLOW: {cantidadPerdidos} eventos perdidos";
    }

    /// <summary>Establece el texto del módulo activo en la barra superior.</summary>
    public void SetNombreModulo(string nombre)
    {
        if (textoModulo != null)
            textoModulo.text = nombre;
    }

    // ─── MÉTODOS PARA ASIGNAR DESDE INSPECTOR ────────────────────────────────

    /// <summary>
    /// Conectar o desconectar según estado actual.
    /// Asignar este método al onClick del BotonConectar desde el Inspector.
    /// </summary>
    public void AlPresionarConectar()
    {
        if (_gestor == null) return;

        if (_conectado)
        {
            _gestor.DesconectarSerial();
        }
        else
        {
            string puertoSeleccionado = ObtenerPuertoSeleccionado();
            if (!string.IsNullOrEmpty(puertoSeleccionado))
                _gestor.ConectarSerial(puertoSeleccionado);
        }
    }

    /// <summary>
    /// Refresca la lista de puertos COM disponibles.
    /// Asignar al botón de recargar puertos desde el Inspector.
    /// </summary>
    public void AlPresionarRefrescarPuertos()
    {
        ActualizarListaPuertos();
    }

    public void AlPresionarSalir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void ActualizarListaPuertos()
    {
        if (desplegablePuerto == null) return;

        string[] puertos = LectorSerial.ObtenerPuertosDisponibles();
        var nuevasOpciones = new System.Collections.Generic.List<string>();
        if (puertos.Length == 0)
            nuevasOpciones.Add("Sin puertos");
        else
            nuevasOpciones.AddRange(puertos);

        // No actualizar si la lista no cambió (evita resetear la selección)
        if (ListaIgual(nuevasOpciones)) return;

        // Recordar qué tenía seleccionado el usuario para no pisarlo en el refresco
        string seleccionActual = desplegablePuerto.options.Count > 0
            ? desplegablePuerto.options[desplegablePuerto.value].text
            : "";

        desplegablePuerto.ClearOptions();
        desplegablePuerto.AddOptions(nuevasOpciones);

        // Restaurar selección actual si sigue disponible; si no, índice 0 (sin memoria)
        int idx = string.IsNullOrEmpty(seleccionActual)
            ? 0
            : nuevasOpciones.IndexOf(seleccionActual);
        desplegablePuerto.value = idx >= 0 ? idx : 0;
    }

    private bool ListaIgual(System.Collections.Generic.List<string> nuevas)
    {
        if (desplegablePuerto.options.Count != nuevas.Count) return false;
        for (int i = 0; i < nuevas.Count; i++)
        {
            if (desplegablePuerto.options[i].text != nuevas[i]) return false;
        }
        return true;
    }

    private string ObtenerPuertoSeleccionado()
    {
        if (desplegablePuerto == null || desplegablePuerto.options.Count == 0)
            return string.Empty;

        string seleccion = desplegablePuerto.options[desplegablePuerto.value].text;
        return seleccion == "Sin puertos" ? string.Empty : seleccion;
    }
}
