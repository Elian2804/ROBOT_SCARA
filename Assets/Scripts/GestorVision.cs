using UnityEngine;
using System.Collections;

/// <summary>
/// Módulo 3 — Visión BT.
/// Consume comandos que llegan por:
///   a) Socket TCP 5005 (Python OpenCV vía ReceptorVisionTCP).
///   b) Tramas L: del sketch del ESP32 (enviadas por GestorSCARA).
/// Ejecuta la trayectoria nombrada correspondiente usando GestorTrayectorias.
/// El estudiante pre-define las trayectorias "PICK" y "DROP" en el Módulo 2.
/// Python manda decisiones (PICK, HOME, DROP) — no pasos ni coordenadas.
/// </summary>
public class GestorVision : MonoBehaviour
{
    // ─── REFERENCIAS ─────────────────────────────────────────────────────────
    [Header("Sistema")]
    [SerializeField] private GestorSCARA        gestorSCARA;
    [SerializeField] private GestorTrayectorias gestorTrayectorias;
    [SerializeField] private ReceptorVisionTCP  receptorTCP;
    [SerializeField] private PanelVisionBT      panelVision;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private bool   _ocupado       = false;
    private string _ultimoComando = "";

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Update()
    {
        if (receptorTCP == null || _ocupado) return;

        if (receptorTCP.ColaComandos.TryDequeue(out string cmd))
            ProcesarComando(cmd);
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>
    /// Permite a GestorSCARA reenviar comandos recibidos por trama L:.
    /// Llamar cuando se recibe "L:PICK", "L:HOME" o "L:DROP".
    /// </summary>
    public void EnviarComando(string comando)
    {
        if (!_ocupado)
            ProcesarComando(comando.ToUpper().Trim());
    }

    // ─── PROCESAMIENTO ────────────────────────────────────────────────────────

    private void ProcesarComando(string cmd)
    {
        _ultimoComando = cmd;

        if (panelVision != null)
        {
            panelVision.RegistrarDeteccion(cmd);
            panelVision.ActualizarObjetoDetectado(cmd, true);
        }

        switch (cmd)
        {
            case "PICK":
                _ocupado = true;
                ActualizarFase("PICK recibido — buscando trayectoria");
                if (gestorTrayectorias == null || !gestorTrayectorias.EjecutarTrayectoriaNombrada("PICK"))
                {
                    ActualizarFase("Error: trayectoria PICK no definida. Grabe primero en Módulo 2.");
                    _ocupado = false;
                }
                else
                {
                    StartCoroutine(EsperarFinTrayectoria("PICK"));
                }
                break;

            case "DROP":
            case "PLACE":
                _ocupado = true;
                ActualizarFase("DROP recibido — buscando trayectoria");
                if (gestorTrayectorias == null || !gestorTrayectorias.EjecutarTrayectoriaNombrada("DROP"))
                {
                    // Intentar alias PLACE
                    if (gestorTrayectorias == null || !gestorTrayectorias.EjecutarTrayectoriaNombrada("PLACE"))
                    {
                        ActualizarFase("Error: trayectoria DROP/PLACE no definida.");
                        _ocupado = false;
                        break;
                    }
                }
                StartCoroutine(EsperarFinTrayectoria("DROP"));
                break;

            case "HOME":
                StartCoroutine(CorrutinaHome());
                break;

            default:
                Debug.LogWarning($"[GestorVision] Comando desconocido: '{cmd}'");
                break;
        }
    }

    // ─── CORRUTINAS ───────────────────────────────────────────────────────────

    private IEnumerator EsperarFinTrayectoria(string nombre)
    {
        // Espera hasta que GestorSCARA llegue a posición (GestorTrayectorias maneja el timing)
        yield return new WaitUntil(() => gestorSCARA == null || gestorSCARA.EstaEnPosicion());
        ActualizarFase($"{nombre} completado");
        if (panelVision != null) panelVision.ActualizarObjetoDetectado("", false);
        _ocupado = false;
    }

    private IEnumerator CorrutinaHome()
    {
        _ocupado = true;
        ActualizarFase("HOME");
        if (gestorSCARA != null)
        {
            gestorSCARA.AbrirGripper();
            gestorSCARA.IrAHome();
            yield return new WaitUntil(() => gestorSCARA.EstaEnPosicion());
        }
        ActualizarFase("HOME completado");
        if (panelVision != null) panelVision.ActualizarObjetoDetectado("", false);
        _ocupado = false;
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void ActualizarFase(string fase)
    {
        if (panelVision != null) panelVision.ActualizarFaseVision(fase);
        Debug.Log($"[GestorVision] {fase}");
    }
}
