using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// Servidor TCP en el puerto 5005.
/// Recibe comandos de texto desde Python/OpenCV (PICK, PLACE, HOME) en un hilo separado
/// y los encola para procesarlos en el hilo principal de Unity.
/// Uso: Agrega este componente al GestorPrincipal junto con GestorVision.
/// </summary>
public class ReceptorVisionTCP : MonoBehaviour
{
    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────
    [Header("Servidor TCP")]
    [SerializeField] private int  puerto         = 5005;
    [SerializeField] private bool iniciarAuto    = true;   // arrancar al entrar en Play

    // ─── ESTADO PÚBLICO ───────────────────────────────────────────────────────
    /// <summary>Cola de comandos recibidos — leer en Update() desde el hilo principal.</summary>
    public ConcurrentQueue<string> ColaComandos { get; } = new ConcurrentQueue<string>();

    /// <summary>True si el servidor está escuchando.</summary>
    public bool Activo { get; private set; } = false;

    /// <summary>Número de clientes conectados desde que arrancó.</summary>
    public int ClientesAtendidos { get; private set; } = 0;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private TcpListener _servidor;
    private Thread      _hiloServidor;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        if (iniciarAuto) IniciarServidor();
    }

    private void OnDestroy()
    {
        DetenerServidor();
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Arranca el servidor TCP en el puerto configurado.</summary>
    public void IniciarServidor()
    {
        if (Activo) return;

        try
        {
            _servidor = new TcpListener(IPAddress.Any, puerto);
            _servidor.Start();
            Activo = true;

            _hiloServidor = new Thread(BucleServidor) { IsBackground = true, Name = "TCP_Vision" };
            _hiloServidor.Start();

            Debug.Log($"[ReceptorVisionTCP] Servidor iniciado en puerto {puerto}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReceptorVisionTCP] No se pudo iniciar el servidor: {e.Message}");
        }
    }

    /// <summary>Detiene el servidor TCP.</summary>
    public void DetenerServidor()
    {
        Activo = false;
        try { _servidor?.Stop(); } catch { }
        _servidor = null;
    }

    // ─── HILO SERVIDOR ────────────────────────────────────────────────────────

    private void BucleServidor()
    {
        while (Activo)
        {
            try
            {
                // Esperar conexión entrante (bloqueante)
                TcpClient cliente = _servidor.AcceptTcpClient();
                ClientesAtendidos++;

                // Atender en hilo separado para no bloquear nuevas conexiones
                Thread hiloCliente = new Thread(() => AtenderCliente(cliente))
                {
                    IsBackground = true,
                    Name = "TCP_Cliente"
                };
                hiloCliente.Start();
            }
            catch (SocketException)
            {
                // El servidor fue detenido normalmente — salir del bucle
                break;
            }
            catch (ThreadAbortException)
            {
                // Unity detuvo el Play — salir en silencio
                break;
            }
            catch (Exception e)
            {
                if (Activo)
                    Debug.LogWarning($"[ReceptorVisionTCP] Error en bucle: {e.Message}");
            }
        }
    }

    private void AtenderCliente(TcpClient cliente)
    {
        try
        {
            using (cliente)
            using (NetworkStream flujo = cliente.GetStream())
            {
                byte[] buffer   = new byte[256];
                string acumulado = "";

                int bytesLeidos;
                while ((bytesLeidos = flujo.Read(buffer, 0, buffer.Length)) > 0)
                {
                    acumulado += Encoding.UTF8.GetString(buffer, 0, bytesLeidos);

                    // Procesar líneas completas (terminadas en \n)
                    while (acumulado.Contains('\n'))
                    {
                        int idx   = acumulado.IndexOf('\n');
                        string cmd = acumulado.Substring(0, idx).Trim();
                        acumulado  = acumulado.Substring(idx + 1);

                        if (!string.IsNullOrEmpty(cmd))
                            ColaComandos.Enqueue(cmd.ToUpper());
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (Activo)
                Debug.LogWarning($"[ReceptorVisionTCP] Error con cliente: {e.Message}");
        }
    }
}
