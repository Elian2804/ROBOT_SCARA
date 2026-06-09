using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Popup modal de análisis del sketch. Se instancia en CanvasPrincipal via Abrir().
/// Construye su propia UI en runtime a partir de las propiedades de AnalizadorSketch.
/// Un solo scrollRect agrupa las 4 secciones.
/// </summary>
public class PanelAnalisisSketch : MonoBehaviour
{
    // ─── PUNTO DE ENTRADA ESTÁTICO ────────────────────────────────────────────

    /// <summary>
    /// Crea (o reutiliza) el popup en el canvas dado.
    /// Llámalo desde PanelConsola.AlPresionarAnalisis().
    /// </summary>
    public static void Abrir(AnalizadorSketch analizador, Canvas canvas)
    {
        if (analizador == null || canvas == null) return;

        // Si ya hay un popup abierto, cerrarlo primero
        var existente = FindObjectOfType<PanelAnalisisSketch>();
        if (existente != null) Destroy(existente.gameObject);

        // Crear raíz y colocarla al final del canvas (renderiza encima de todo)
        var go = new GameObject("PopupAnalisisSketch");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        var panel = go.AddComponent<PanelAnalisisSketch>();
        panel._analizador = analizador;
        panel.Construir();
    }

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────

    private AnalizadorSketch _analizador;

    // ─── CONSTRUCCIÓN DE UI ───────────────────────────────────────────────────

    private void Construir()
    {
        // Raíz: pantalla completa, bloquea clics al fondo
        var rtRaiz = gameObject.AddComponent<RectTransform>();
        Estirar(rtRaiz);
        var blocker = gameObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.65f);

        // Panel central 900×600
        var panelGO  = CrearGO("Panel", transform);
        var rtPanel  = panelGO.GetComponent<RectTransform>();
        rtPanel.sizeDelta        = new Vector2(900, 600);
        rtPanel.anchorMin        = new Vector2(0.5f, 0.5f);
        rtPanel.anchorMax        = new Vector2(0.5f, 0.5f);
        rtPanel.anchoredPosition = Vector2.zero;
        panelGO.AddComponent<Image>().color = ColoresUI.FondoPrincipal;

        // Barra de título (36px en la parte superior del panel)
        var barraGO = CrearGO("BarraTitulo", panelGO.transform);
        var rtBarra = barraGO.GetComponent<RectTransform>();
        rtBarra.anchorMin  = new Vector2(0, 1);
        rtBarra.anchorMax  = new Vector2(1, 1);
        rtBarra.offsetMin  = new Vector2(0, -36);
        rtBarra.offsetMax  = new Vector2(0, 0);
        barraGO.AddComponent<Image>().color = ColoresUI.FondoBarra;

        // Título
        var titulo = CrearTexto("Análisis del Sketch ESP32", barraGO.transform, 15,
            ColoresUI.AcentoAmarillo, TextAlignmentOptions.Left);
        var rtTit = titulo.GetComponent<RectTransform>();
        rtTit.anchorMin = Vector2.zero; rtTit.anchorMax = Vector2.one;
        rtTit.offsetMin = new Vector2(12, 0); rtTit.offsetMax = new Vector2(-50, 0);

        // Botón X
        var btnCerrarGO = CrearBoton("BotonCerrar", barraGO.transform, "X",
            ColoresUI.Error, new Vector2(30, 30));
        var rtX = btnCerrarGO.GetComponent<RectTransform>();
        rtX.anchorMin = new Vector2(1, 0.5f); rtX.anchorMax = new Vector2(1, 0.5f);
        rtX.pivot     = new Vector2(1, 0.5f);
        rtX.anchoredPosition = new Vector2(-4, 0);
        btnCerrarGO.GetComponent<Button>().onClick.AddListener(() => Destroy(gameObject));

        // Área de scroll (debajo de la barra de título)
        var scrollGO = CrearGO("AreaScroll", panelGO.transform);
        var rtScroll = scrollGO.GetComponent<RectTransform>();
        rtScroll.anchorMin = Vector2.zero;
        rtScroll.anchorMax = Vector2.one;
        rtScroll.offsetMin = new Vector2(0, 0);
        rtScroll.offsetMax = new Vector2(0, -36);
        scrollGO.AddComponent<Image>().color = ColoresUI.FondoSecundario;

        var scrollRect  = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        var vpGO = CrearGO("Viewport", scrollGO.transform);
        var rtVP = vpGO.GetComponent<RectTransform>();
        Estirar(rtVP);
        vpGO.AddComponent<Image>().color = Color.white;
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = rtVP;

        // Contenido con VerticalLayoutGroup
        var contenidoGO = CrearGO("Contenido", vpGO.transform);
        var rtContenido = contenidoGO.GetComponent<RectTransform>();
        rtContenido.anchorMin = new Vector2(0, 1);
        rtContenido.anchorMax = new Vector2(1, 1);
        rtContenido.pivot     = new Vector2(0.5f, 1f);
        rtContenido.offsetMin = Vector2.zero;
        rtContenido.offsetMax = Vector2.zero;

        var vlg = contenidoGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing  = 6f;
        vlg.padding  = new RectOffset(10, 10, 8, 8);

        var csf = contenidoGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = rtContenido;

        // ── Las 4 secciones ──────────────────────────────────────────────────
        AgregarSeccionPines(contenidoGO.transform);
        AgregarSeccionMovimientos(contenidoGO.transform);
        AgregarSeccionPatrones(contenidoGO.transform);
        AgregarSeccionResumen(contenidoGO.transform);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 1 — PINES ACTIVOS
    // ═════════════════════════════════════════════════════════════════════════

    private void AgregarSeccionPines(Transform padre)
    {
        var pines = _analizador.PinesDetectados;
        var sec = CrearSeccion("Pines activos", padre,
            pines.Count == 0 ? 1 : pines.Count);

        if (pines.Count == 0)
        {
            AgregarLinea(sec, "Sin actividad registrada.", ColoresUI.TextoDeshabilitado);
            return;
        }

        foreach (var p in pines)
        {
            Color col   = p.esValido ? ColoresUI.Exito : ColoresUI.Error;
            string check = p.esValido ? "●" : "✕";
            string txt  = $"{check}  PIN {p.pin:D2} — {p.nombreActuador} — {p.tipo} — {p.cantidadCambios} cambios";
            AgregarLinea(sec, txt, col);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 2 — MOVIMIENTOS INFERIDOS
    // ═════════════════════════════════════════════════════════════════════════

    private void AgregarSeccionMovimientos(Transform padre)
    {
        var motores = _analizador.PorMotor;
        var servos  = _analizador.PorServo;
        int filas   = motores.Count * 3 + servos.Count + (motores.Count == 0 && servos.Count == 0 ? 1 : 0);
        var sec = CrearSeccion("Movimientos", padre, Mathf.Max(1, filas));

        if (motores.Count == 0 && servos.Count == 0)
        {
            AgregarLinea(sec, "Sin movimientos registrados.", ColoresUI.TextoDeshabilitado);
            return;
        }

        foreach (var m in motores)
        {
            // Línea 1: pasos totales → posición
            string posicion = CalcularPosicion(m);
            AgregarLinea(sec,
                $"  {m.nombreMotor}: {m.pasosTotales} pasos → {posicion}",
                ColoresUI.AcentoAmarillo);

            // Línea 2: velocidad
            AgregarLinea(sec,
                $"      Velocidad promedio: {m.velocidadPromedio_pps:F0} p/s   " +
                $"Máx: {m.velocidadMax_pps:F0} p/s",
                ColoresUI.TextoSecundario);

            // Línea 3: dirección predominante
            Color colDir = m.direccionPredominante == "equilibrada"
                ? ColoresUI.TextoSecundario : ColoresUI.TextoPrincipal;
            AgregarLinea(sec,
                $"      Dirección predominante: {m.direccionPredominante}   " +
                $"Resultante: {(m.movimientoResultante >= 0 ? "+" : "")}{m.movimientoResultante} pasos",
                colDir);
        }

        foreach (var s in servos)
        {
            AgregarLinea(sec,
                $"  {s.nombreServo}: pulso {s.ultimoPulso_us:F0} µs → {s.anguloCalculado:F1}°",
                ColoresUI.LogSketch);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 3 — PATRONES DETECTADOS
    // ═════════════════════════════════════════════════════════════════════════

    private void AgregarSeccionPatrones(Transform padre)
    {
        var pat = _analizador.PatronesDetectados;
        bool alguno = pat.esBucle || pat.esSimultaneo || pat.esSecuenciaCoordinada;
        var sec = CrearSeccion("Patrones detectados", padre, alguno ? 3 : 1);

        if (!alguno)
        {
            AgregarLinea(sec, "—   Sin patrones detectados.", ColoresUI.TextoDeshabilitado);
            return;
        }

        if (pat.esBucle)
            AgregarLinea(sec,
                $"✓   Bucle detectado: {pat.pasosBucle} pasos con intervalo regular (<20% variación)",
                ColoresUI.Exito);

        if (pat.esSimultaneo)
            AgregarLinea(sec,
                $"✓   Movimiento simultáneo: {pat.motoresSimultaneos} (pulsos en <500 µs)",
                ColoresUI.Exito);

        if (pat.esSecuenciaCoordinada)
            AgregarLinea(sec,
                "✓   Secuencia coordinada detectada: Z + brazos + servo en <5s",
                ColoresUI.Exito);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 4 — RESUMEN DE SESIÓN
    // ═════════════════════════════════════════════════════════════════════════

    private void AgregarSeccionResumen(Transform padre)
    {
        var res = _analizador.ResumenSesion;
        int filas = 5 + res.advertencias.Count + 1; // stats + advertencias + botón
        var sec = CrearSeccion("Resumen de sesión", padre, filas);

        AgregarLinea(sec,
            $"  Duración: {res.duracionSegundos:F1} s",
            ColoresUI.TextoPrincipal);
        AgregarLinea(sec,
            $"  Total eventos T: recibidos: {res.totalEventos}",
            ColoresUI.TextoPrincipal);
        AgregarLinea(sec,
            $"  Total pasos enviados: {res.totalPasos}",
            ColoresUI.TextoPrincipal);
        AgregarLinea(sec,
            $"  Motor más activo: {res.motorMasActivo}",
            ColoresUI.TextoPrincipal);
        AgregarLinea(sec,
            $"  Velocidad máxima registrada: {res.velocidadMaxRegistrada:F0} p/s",
            ColoresUI.TextoPrincipal);

        if (res.advertencias.Count > 0)
        {
            AgregarLinea(sec, "  Advertencias:", ColoresUI.Advertencia);
            foreach (var adv in res.advertencias)
                AgregarLinea(sec, $"    [{adv.codigo}] {adv.descripcion}", adv.color);
        }
        else
        {
            AgregarLinea(sec, "  Sin advertencias.", ColoresUI.Exito);
        }

        // Espaciado antes del botón
        AgregarLinea(sec, "", ColoresUI.TextoDeshabilitado);

        // Botón "Limpiar análisis"
        var btnGO  = CrearBoton("BotonLimpiar", sec, "Limpiar análisis",
            ColoresUI.Error, new Vector2(160, 34));
        var le     = btnGO.AddComponent<LayoutElement>();
        le.preferredHeight = 38f;
        le.flexibleWidth   = 0f;
        le.preferredWidth  = 160f;
        le.minWidth        = 160f;

        btnGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            _analizador.Limpiar();
            Destroy(gameObject);
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPERS DE UI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Crea el encabezado de una sección con fondo ligeramente distinto.</summary>
    private Transform CrearSeccion(string titulo, Transform padre, int filasEstimadas)
    {
        var sec = CrearGO("Seccion_" + titulo.Replace(" ", ""), padre);
        var le  = sec.AddComponent<LayoutElement>();
        le.preferredHeight = 28f + filasEstimadas * 22f;
        le.flexibleWidth   = 1f;
        sec.AddComponent<Image>().color = ColoresUI.FondoPrincipal;

        var vlg = sec.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing  = 2f;
        vlg.padding  = new RectOffset(6, 6, 4, 6);

        var csf = sec.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Encabezado de sección
        var tituloGO = CrearTexto(titulo, sec.transform, 13, ColoresUI.AcentoAmarillo,
            TextAlignmentOptions.Left);
        var leTit = tituloGO.AddComponent<LayoutElement>();
        leTit.preferredHeight = 24f;

        // Separador visual
        var sep = CrearGO("Separador", sec.transform);
        sep.AddComponent<Image>().color = ColoresUI.AcentoAmarilloSuave;
        var leSep = sep.AddComponent<LayoutElement>();
        leSep.preferredHeight = 1f;

        return sec.transform;
    }

    /// <summary>Agrega una línea de texto al contenedor de una sección.</summary>
    private void AgregarLinea(Transform padre, string texto, Color color)
    {
        var go = CrearTexto(texto, padre, 12, color, TextAlignmentOptions.Left);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 20f;
    }

    // ─── Helpers de creación de GameObjects ───────────────────────────────────

    private static GameObject CrearGO(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CrearTexto(string texto, Transform padre,
        int fontSize, Color color, TextAlignmentOptions alineacion)
    {
        var go  = CrearGO("Texto_" + texto.Substring(0, Mathf.Min(20, texto.Length)), padre);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = alineacion;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return go;
    }

    private static GameObject CrearBoton(string nombre, Transform padre, string etiqueta,
        Color colorFondo, Vector2 tamaño)
    {
        var go = CrearGO(nombre, padre);
        go.GetComponent<RectTransform>().sizeDelta = tamaño;

        var img  = go.AddComponent<Image>();
        img.color = colorFondo;

        var btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = colorFondo;
        cb.highlightedColor = new Color(colorFondo.r + 0.1f, colorFondo.g + 0.1f, colorFondo.b + 0.1f);
        cb.pressedColor     = new Color(colorFondo.r - 0.1f, colorFondo.g - 0.1f, colorFondo.b - 0.1f);
        cb.selectedColor    = colorFondo;
        btn.colors = cb;

        // Texto centrado
        var txtGO  = CrearGO("Texto", go.transform);
        var rtTxt  = txtGO.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = 13;
        tmp.color     = ColoresUI.TextoPrincipal;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ─── HELPER DE CONVERSIÓN ─────────────────────────────────────────────────

    private static string CalcularPosicion(AnalizadorSketch.InfoMotor m)
    {
        // Determinar si es Z (lineal) o brazo (rotacional) por nombre
        if (m.nombreMotor.Contains("Z"))
        {
            float mm = ConfiguracionRobot.PasosAMmZ(m.movimientoResultante);
            return $"{mm:F1} mm netos";
        }
        else if (m.nombreMotor.Contains("J1"))
        {
            float deg = ConfiguracionRobot.PasosAGradosJ1(m.movimientoResultante);
            return $"{deg + 90f:F1}° netos";
        }
        else
        {
            float deg = ConfiguracionRobot.PasosAGradosJ2(m.movimientoResultante);
            return $"{deg + 90f:F1}° netos";
        }
    }
}
