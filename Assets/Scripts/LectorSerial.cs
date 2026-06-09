using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>
/// Gestiona la lectura del puerto serial en un hilo separado.
/// Llena una ConcurrentQueue con las líneas recibidas del ESP32.
/// NUNCA bloquea el hilo principal de Unity.
/// </summary>
public class LectorSerial : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Configuración Serial")]
    [SerializeField] private string nombrePuerto = "COM3";
    [SerializeField] private int    velocidad    = ConfiguracionRobot.VelocidadSerial;
    [SerializeField] private int    tiempoEspera = ConfiguracionRobot.TiempoEsperaMs;

    // ─── ESTADO PÚBLICO (solo lectura) ───────────────────────────────────────
    /// <summary>True si el puerto serial está abierto y el hilo activo.</summary>
    public bool Conectado => _ejecutando && _puerto != null && _puerto.IsOpen;

    /// <summary>Cola segura de líneas recibidas. GestorSCARA la consume en Update().</summary>
    public ConcurrentQueue<string> ColaLineas { get; private set; } = new ConcurrentQueue<string>();

    /// <summary>Último error registrado. Vacío si no hay error.</summary>
    public string UltimoError { get; private set; } = string.Empty;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private SerialPort _puerto;
    private Thread     _hiloLectura;
    private volatile bool _ejecutando = false;

    // ─── PUERTOS DISPONIBLES (útil para UI de selección) ─────────────────────

    /// <summary>
    /// Devuelve todos los puertos COM disponibles, ordenados numéricamente.
    /// Combina GetPortNames() con búsqueda directa en el árbol de hardware de Windows
    /// para capturar puertos (ej. ESP32 con BT activo) que el método estándar omite.
    /// </summary>
    public static string[] ObtenerPuertosDisponibles()
    {
        var puertos = new HashSet<string>(SerialPort.GetPortNames());

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // Algunos drivers no actualizan DEVICEMAP\SERIALCOMM a tiempo.
        // La búsqueda en el árbol USB del registro captura esos casos.
        try { AgregarPuertosDesdeRegistroUSB(puertos); } catch { }
#endif

        // Orden numérico: COM1, COM2 ... COM9, COM10, COM11...
        var lista = new List<string>(puertos);
        lista.Sort((a, b) =>
        {
            bool okA = int.TryParse(a.Replace("COM", ""), out int na);
            bool okB = int.TryParse(b.Replace("COM", ""), out int nb);
            if (!okA && !okB) return string.Compare(a, b, StringComparison.Ordinal);
            if (!okA) return 1;
            if (!okB) return -1;
            return na.CompareTo(nb);
        });

        return lista.ToArray();
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    /// <summary>
    /// Recorre SYSTEM\CurrentControlSet\Enum\USB buscando entradas con PortName.
    /// Captura puertos seriales USB cuyo driver aún no actualizó DEVICEMAP\SERIALCOMM.
    /// </summary>
    private static void AgregarPuertosDesdeRegistroUSB(HashSet<string> puertos)
    {
        using var usb = Microsoft.Win32.Registry.LocalMachine
                            .OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
        if (usb == null) return;

        foreach (string vidPid in usb.GetSubKeyNames())
        {
            using var vidPidKey = usb.OpenSubKey(vidPid);
            if (vidPidKey == null) continue;

            foreach (string instancia in vidPidKey.GetSubKeyNames())
            {
                try
                {
                    using var devParams = vidPidKey.OpenSubKey(
                        instancia + @"\Device Parameters");
                    if (devParams == null) continue;

                    string puerto = devParams.GetValue("PortName")?.ToString();
                    if (!string.IsNullOrEmpty(puerto) && puerto.StartsWith("COM"))
                        puertos.Add(puerto);
                }
                catch { }
            }
        }
    }
#endif

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>
    /// Abre el puerto serial e inicia el hilo de lectura.
    /// Llamar desde GestorSCARA al pulsar "Conectar".
    /// </summary>
    /// <param name="puerto">Nombre del puerto, ej. "COM3".</param>
    /// <returns>True si la conexión fue exitosa.</returns>
    public bool Conectar(string puerto)
    {
        if (Conectado)
        {
            Debug.LogWarning("[LectorSerial] Ya existe una conexión activa. Desconectar primero.");
            return false;
        }

        nombrePuerto = puerto;
        UltimoError  = string.Empty;

        try
        {
            _puerto = new SerialPort(nombrePuerto, velocidad, Parity.None, 8, StopBits.One)
            {
                ReadTimeout  = tiempoEspera,
                WriteTimeout = tiempoEspera,
                NewLine      = "\n",
                DtrEnable    = false,
                RtsEnable    = false
            };

            _puerto.Open();
            _puerto.DiscardInBuffer();   // limpiar datos previos a la conexión
            _puerto.DiscardOutBuffer();

            // Vaciar cola de iteraciones anteriores
            while (ColaLineas.TryDequeue(out _)) { }

            _ejecutando  = true;
            _hiloLectura = new Thread(BucleLectura)
            {
                IsBackground = true,
                Name         = "HiloSerialSCARA"
            };
            _hiloLectura.Start();

            Debug.Log($"[LectorSerial] Conectado a {nombrePuerto} a {velocidad} baudios.");
            return true;
        }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            Debug.LogError($"[LectorSerial] Error al conectar en {nombrePuerto}: {ex.Message}");
            CerrarPuerto();
            return false;
        }
    }

    /// <summary>
    /// Detiene el hilo de lectura y cierra el puerto serial.
    /// Seguro llamar múltiples veces aunque no haya conexión activa.
    /// </summary>
    public void Desconectar()
    {
        if (!_ejecutando && (_hiloLectura == null || !_hiloLectura.IsAlive))
            return;

        _ejecutando = false;

        // Cerrar el puerto ANTES del Join: interrumpe ReadLine() de inmediato
        // (ReadLine lanza IOException → hilo sale sin esperar el ReadTimeout)
        CerrarPuerto();

        if (_hiloLectura != null && _hiloLectura.IsAlive)
            _hiloLectura.Join(500);   // suficiente — el cierre ya forzó la salida

        _hiloLectura = null;
        Debug.Log("[LectorSerial] Desconectado.");
    }

    // ─── CICLO DE VIDA UNITY ─────────────────────────────────────────────────

    private void OnDisable()
    {
        // Se dispara al detener Play en el Editor — cierra el puerto limpiamente
        // para que al volver a darle Play el COM quede libre.
        Desconectar();
    }

    private void OnApplicationQuit()
    {
        Desconectar();
    }

    private void OnDestroy()
    {
        Desconectar();
    }

    // ─── HILO DE LECTURA (se ejecuta fuera del hilo principal) ───────────────

    /// <summary>
    /// Bucle infinito que corre en el hilo separado.
    /// Lee líneas del puerto y las encola. No toca objetos de Unity.
    /// </summary>
    private void BucleLectura()
    {
        while (_ejecutando)
        {
            try
            {
                if (_puerto == null || !_puerto.IsOpen)
                {
                    // Puerto cerrado inesperadamente
                    _ejecutando = false;
                    UltimoError = "Puerto cerrado inesperadamente.";
                    break;
                }

                string linea = _puerto.ReadLine();

                if (!string.IsNullOrEmpty(linea))
                {
                    // Limpiar espacios/retornos residuales
                    linea = linea.Trim();
                    ColaLineas.Enqueue(linea);
                }
            }
            catch (TimeoutException)
            {
                // Timeout normal: no hay datos en este ciclo, continuar
            }
            catch (OperationCanceledException)
            {
                // Cierre solicitado
                break;
            }
            catch (Exception ex)
            {
                if (_ejecutando)
                {
                    UltimoError = ex.Message;
                    // Encolar aviso interno para que GestorSCARA lo procese
                    ColaLineas.Enqueue($"L:ERROR_SERIAL:{ex.Message}");
                    _ejecutando = false;
                }
                break;
            }
        }
    }

    /// <summary>Cierra y libera el puerto serial de forma segura.</summary>
    private void CerrarPuerto()
    {
        try
        {
            if (_puerto != null)
            {
                if (_puerto.IsOpen)
                    _puerto.Close();

                _puerto.Dispose();
                _puerto = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LectorSerial] Error al cerrar puerto: {ex.Message}");
        }
    }
}
