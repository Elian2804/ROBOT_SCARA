using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Panel derecho exclusivo del Módulo 3 (Visión Artificial y Bluetooth).
/// Muestra el estado BT, el último comando recibido, el log de detecciones
/// (via trama L:) y el indicador de fuente de señal.
/// Todas las referencias se asignan desde el Inspector.
/// </summary>
public class PanelVisionBT : MonoBehaviour
{
    // ─── ESTADO BLUETOOTH ────────────────────────────────────────────────────
    [Header("Estado Bluetooth")]
    [SerializeField] private Image           imagenIndicadorBT;
    [SerializeField] private TextMeshProUGUI textoEstadoBT;
    [SerializeField] private TextMeshProUGUI textoUltimoComando;

    [Header("Información del dispositivo BT")]
    [SerializeField] private TextMeshProUGUI textoNombreDispositivo;
    [SerializeField] private TextMeshProUGUI textoTimestamp;
    [SerializeField] private TextMeshProUGUI textoAdvertenciaBT;
    [Tooltip("Nombre del dispositivo ESP32 tal como aparece en el Bluetooth del PC.")]
    [SerializeField] private string          nombreDispositivoESP32 = "SCARA_ESP32";
    [Tooltip("Segundos sin actividad BT para mostrar aviso de timeout.")]
    [SerializeField] private float           timeoutInactividadBT   = 5f;

    // ─── FUENTE DE SEÑAL ─────────────────────────────────────────────────────
    [Header("Fuente de señal activa")]
    [SerializeField] private Image           imagenFuenteSerial;
    [SerializeField] private Image           imagenFuenteBT;
    [SerializeField] private TextMeshProUGUI textoFuenteActiva;

    // ─── LOG DE DETECCIONES ───────────────────────────────────────────────────
    [Header("Log de detecciones (trama L:)")]
    [SerializeField] private Transform       contenedorDetecciones;   // VerticalLayoutGroup
    [SerializeField] private GameObject      prefabItemDeteccion;
    [SerializeField] private ScrollRect      scrollDetecciones;
    [SerializeField] private TextMeshProUGUI textoContadorDetecciones;
    [SerializeField] private int             maxDeteccionesVisibles = 15;

    // ─── ESTADO GRIPPER / VISIÓN ──────────────────────────────────────────────
    [Header("Estado de visión y agarre")]
    [SerializeField] private TextMeshProUGUI textoFaseVision;
    [SerializeField] private TextMeshProUGUI textoObjetoDetectado;
    [SerializeField] private Image           imagenEstadoAgarre;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private int        _contadorDetecciones = 0;
    private bool       _btConectado         = false;
    private GestorSCARA _gestor             = null;
    private float      _ultimaActividadBT   = 0f;
    private bool       _advertenciaMostrada = false;

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Inicializar el panel. Llamado por GestorSCARA al inicio de Play.</summary>
    public void Inicializar(GestorSCARA gestor)
    {
        // Desuscribir de gestor anterior si lo hubiera (re-inicio de Play)
        if (_gestor != null)
        {
            _gestor.OnBluetoothStateChanged    -= AlCambiarEstadoBluetooth;
            _gestor.OnBluetoothCommandReceived -= AlRecibirComandoBT;
        }

        _gestor = gestor;

        if (_gestor != null)
        {
            _gestor.OnBluetoothStateChanged    += AlCambiarEstadoBluetooth;
            _gestor.OnBluetoothCommandReceived += AlRecibirComandoBT;
        }

        ActualizarEstadoBluetooth(false);
        LimpiarDetecciones();
        ActualizarFaseVision("En espera");
        ActualizarNombreDispositivo();
        MostrarAdvertenciaBT(false);
    }

    private void OnDestroy()
    {
        if (_gestor != null)
        {
            _gestor.OnBluetoothStateChanged    -= AlCambiarEstadoBluetooth;
            _gestor.OnBluetoothCommandReceived -= AlRecibirComandoBT;
        }
    }

    private void Update()
    {
        // Detectar timeout de inactividad BT: BT estaba conectado pero no llegan comandos
        if (!_btConectado || _ultimaActividadBT <= 0f) return;
        float inactivo = Time.time - _ultimaActividadBT;
        if (inactivo > timeoutInactividadBT)
            MostrarAdvertenciaBT(true, (int)inactivo);
    }

    /// <summary>
    /// Actualiza el panel con el estado actual del robot.
    /// Llamado por GestorSCARA en Update() cuando el módulo 3 está activo.
    /// </summary>
    public void ActualizarEstado(DatosRobot datos)
    {
        ActualizarGripper(datos);
    }

    /// <summary>
    /// Actualiza el indicador visual de Bluetooth.
    /// Llamado por suscripción al evento OnBluetoothStateChanged de GestorSCARA.
    /// </summary>
    public void ActualizarEstadoBluetooth(bool conectado)
    {
        _btConectado = conectado;

        if (conectado)
        {
            _ultimaActividadBT = Time.time;
            MostrarAdvertenciaBT(false);
        }

        if (imagenIndicadorBT != null)
            imagenIndicadorBT.color = conectado ? ColoresUI.BTConectado : ColoresUI.BTDesconectado;

        if (textoEstadoBT != null)
        {
            textoEstadoBT.text  = conectado ? "Bluetooth: CONECTADO" : "Bluetooth: DESCONECTADO";
            textoEstadoBT.color = conectado ? ColoresUI.BTConectado   : ColoresUI.BTDesconectado;
        }

        ActualizarNombreDispositivo();
        ActualizarIndicadorFuente(conectado);
    }

    /// <summary>
    /// Registra una detección recibida por trama L: desde Python/OpenCV.
    /// Llamado al recibir mensajes L: que contengan información de detección.
    /// </summary>
    public void RegistrarDeteccion(string mensajeDeteccion)
    {
        _contadorDetecciones++;

        if (textoContadorDetecciones != null)
            textoContadorDetecciones.text = $"Detecciones: {_contadorDetecciones}";

        if (textoUltimoComando != null)
            textoUltimoComando.text = $"Último: {mensajeDeteccion}";

        AgregarItemDeteccion(mensajeDeteccion);
    }

    /// <summary>Actualiza el texto de la fase actual de visión.</summary>
    public void ActualizarFaseVision(string fase)
    {
        if (textoFaseVision != null)
            textoFaseVision.text = $"Fase: {fase}";
    }

    /// <summary>Actualiza el nombre del objeto detectado por la cámara.</summary>
    public void ActualizarObjetoDetectado(string nombreObjeto, bool detectado)
    {
        if (textoObjetoDetectado != null)
        {
            textoObjetoDetectado.text  = detectado
                ? $"Objeto: {nombreObjeto}"
                : "Objeto: No detectado";
            textoObjetoDetectado.color = detectado ? ColoresUI.AcentoAmarillo : ColoresUI.TextoSecundario;
        }
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void ActualizarGripper(DatosRobot datos)
    {
        if (imagenEstadoAgarre != null)
        {
            imagenEstadoAgarre.color = datos.objetoAgarrado
                ? ColoresUI.Exito
                : ColoresUI.TextoSecundario;
        }
    }

    // ─── HANDLERS DE EVENTOS (suscritos en Inicializar) ──────────────────────

    private void AlCambiarEstadoBluetooth(bool conectado)
    {
        ActualizarEstadoBluetooth(conectado);
    }

    private void AlRecibirComandoBT(string cmd)
    {
        _ultimaActividadBT = Time.time;
        MostrarAdvertenciaBT(false);

        // Actualizar timestamp de última actividad
        if (textoTimestamp != null)
            textoTimestamp.text = $"Último BT: {DateTime.Now:HH:mm:ss}";

        // Registrar en log de detecciones y actualizar "último comando"
        RegistrarDeteccion(cmd);
    }

    private void ActualizarNombreDispositivo()
    {
        if (textoNombreDispositivo != null)
            textoNombreDispositivo.text = $"Dispositivo: {nombreDispositivoESP32}";
    }

    private void MostrarAdvertenciaBT(bool mostrar, int segundos = 0)
    {
        if (textoAdvertenciaBT == null) return;

        bool estado = mostrar && !_advertenciaMostrada || !mostrar;
        _advertenciaMostrada = mostrar;

        textoAdvertenciaBT.gameObject.SetActive(mostrar);
        if (mostrar)
            textoAdvertenciaBT.text = $"Sin actividad BT: {segundos}s";
    }

    private void ActualizarIndicadorFuente(bool btActivo)
    {
        // Serial siempre activo, BT solo cuando está conectado
        if (imagenFuenteSerial != null)
            imagenFuenteSerial.color = ColoresUI.Exito;

        if (imagenFuenteBT != null)
            imagenFuenteBT.color = btActivo ? ColoresUI.BTConectado : ColoresUI.BTDesconectado;

        if (textoFuenteActiva != null)
            textoFuenteActiva.text = btActivo
                ? "Señal: Serial + Bluetooth"
                : "Señal: Solo Serial";
    }

    private void AgregarItemDeteccion(string mensaje)
    {
        if (contenedorDetecciones == null || prefabItemDeteccion == null) return;

        // Eliminar items más antiguos si se supera el límite
        if (contenedorDetecciones.childCount >= maxDeteccionesVisibles)
        {
            Transform primero = contenedorDetecciones.GetChild(0);
            if (primero != null) Destroy(primero.gameObject);
        }

        GameObject nuevoItem = Instantiate(prefabItemDeteccion, contenedorDetecciones);
        TextMeshProUGUI tmp = nuevoItem.GetComponent<TextMeshProUGUI>();

        if (tmp != null)
        {
            string hora = System.DateTime.Now.ToString("HH:mm:ss");
            tmp.text  = $"[{hora}] {mensaje}";
            tmp.color = ColoresUI.LogSketch;
        }

        // Auto-scroll al fondo
        Canvas.ForceUpdateCanvases();
        if (scrollDetecciones != null)
            scrollDetecciones.verticalNormalizedPosition = 0f;
    }

    private void LimpiarDetecciones()
    {
        if (contenedorDetecciones != null)
        {
            foreach (Transform hijo in contenedorDetecciones)
                Destroy(hijo.gameObject);
        }
        _contadorDetecciones = 0;
    }
}
