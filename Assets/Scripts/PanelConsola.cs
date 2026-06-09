using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Panel inferior: log cronológico de todos los eventos del sistema.
/// Usa un único TextMeshProUGUI con rich text — sin Instantiate por línea.
/// </summary>
public class PanelConsola : MonoBehaviour
{
    [Header("Visualización")]
    [SerializeField] private TextMeshProUGUI textoConsola;
    [SerializeField] private ScrollRect      scrollConsola;

    [Header("Botones de control")]
    [SerializeField] private Button          botonPausa;
    [SerializeField] private Button          botonLimpiar;
    [SerializeField] private Button          botonExportar;
    [SerializeField] private TextMeshProUGUI textoBotonPausa;

    [Header("Filtros por tipo")]
    [SerializeField] private Toggle filtroT;
    [SerializeField] private Toggle filtroE;
    [SerializeField] private Toggle filtroL;
    [SerializeField] private Toggle filtroB;

    [Header("Contador de mensajes")]
    [SerializeField] private TextMeshProUGUI textoContador;

    [Header("Análisis de sketch")]
    [SerializeField] private Button          botonAnalisis;
    [SerializeField] private AnalizadorSketch analizador;
    [SerializeField] private Canvas          canvasPrincipal;

    [Header("Configuración")]
    [SerializeField] private int maxLineas = 200;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private bool _pausado      = false;
    private int  _contadorTotal = 0;

    private readonly List<string> _lineasVisibles   = new List<string>();
    private readonly List<string> _historialCompleto = new List<string>();

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    public void Inicializar()
    {
        _pausado       = false;
        _contadorTotal = 0;
        LimpiarConsola();
    }

    public void AgregarMensaje(string mensaje, TipoMensaje tipo)
    {
        if (!FiltroActivo(tipo)) return;

        string timestamp    = DateTime.Now.ToString("HH:mm:ss.fff");
        string lineaExport  = $"[{timestamp}] [{tipo}] {mensaje}";
        _historialCompleto.Add(lineaExport);

        _contadorTotal++;
        if (textoContador != null)
            textoContador.text = $"Mensajes: {_contadorTotal}";

        if (_pausado) return;

        string hex   = "#" + ColorUtility.ToHtmlStringRGB(ObtenerColor(tipo));
        string linea = $"<color={hex}>[{timestamp}] {mensaje}</color>";
        _lineasVisibles.Insert(0, linea);  // más nuevo arriba

        while (_lineasVisibles.Count > maxLineas)
            _lineasVisibles.RemoveAt(_lineasVisibles.Count - 1);

        RefrescarTexto();
    }

    // ─── MÉTODOS PARA ASIGNAR DESDE INSPECTOR ────────────────────────────────

    public void AlPresionarPausa()
    {
        _pausado = !_pausado;
        if (textoBotonPausa != null)
            textoBotonPausa.text = _pausado ? "Reanudar" : "Pausar";
    }

    public void AlPresionarLimpiar()
    {
        LimpiarConsola();
    }

    public void AlPresionarExportar()
    {
        ExportarLog();
    }

    public void AlPresionarAnalisis()
    {
        PanelAnalisisSketch.Abrir(analizador, canvasPrincipal);
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void RefrescarTexto()
    {
        if (textoConsola == null) return;
        textoConsola.text = string.Join("\n", _lineasVisibles);
    }

    private void LimpiarConsola()
    {
        _lineasVisibles.Clear();
        _contadorTotal = 0;

        if (textoConsola != null)
            textoConsola.text = "";
        if (textoContador != null)
            textoContador.text = "Mensajes: 0";
    }

    private void ExportarLog()
    {
        if (_historialCompleto.Count == 0)
        {
            AgregarMensaje("No hay mensajes para exportar.", TipoMensaje.Advertencia);
            Debug.LogWarning("[PanelConsola] _historialCompleto está vacío.");
            return;
        }

        string nombre      = $"SCARA_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        string carpeta     = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string rutaArchivo = Path.Combine(carpeta, nombre);

        try
        {
            File.WriteAllLines(rutaArchivo, _historialCompleto);
            AgregarMensaje($"Exportado ({_historialCompleto.Count} líneas): {rutaArchivo}", TipoMensaje.Exito);
            Debug.Log($"[PanelConsola] Log guardado en: {rutaArchivo}");
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{rutaArchivo}\"");
        }
        catch (Exception ex)
        {
            AgregarMensaje($"Error al exportar: {ex.Message}", TipoMensaje.Error);
            Debug.LogError($"[PanelConsola] Error exportando: {ex.Message}");
        }
    }

    private bool FiltroActivo(TipoMensaje tipo)
    {
        switch (tipo)
        {
            case TipoMensaje.Normal:      return filtroT == null || filtroT.isOn;
            case TipoMensaje.Error:       return filtroE == null || filtroE.isOn;
            case TipoMensaje.Log:         return filtroL == null || filtroL.isOn;
            case TipoMensaje.Exito:       return filtroB == null || filtroB.isOn;
            default:                      return true;
        }
    }

    private Color ObtenerColor(TipoMensaje tipo)
    {
        switch (tipo)
        {
            case TipoMensaje.Advertencia: return ColoresUI.Advertencia;
            case TipoMensaje.Error:       return ColoresUI.Error;
            case TipoMensaje.Exito:       return ColoresUI.Exito;
            case TipoMensaje.Log:         return ColoresUI.LogSketch;
            default:                      return ColoresUI.EventoNormal;
        }
    }
}
