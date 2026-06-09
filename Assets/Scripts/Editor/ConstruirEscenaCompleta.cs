using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Constructor completo de la escena SCARA.
/// Crea TODA la jerarquía, Canvas, componentes y conexiones de una sola vez.
/// No modifica los GameObjects existentes del modelo 3D.
///
/// USO: Menú Unity → SCARA → Construir Escena Completa
///
/// IMPORTANTE: Ejecutar UNA SOLA VEZ en una escena limpia (con solo el robot 3D).
/// Si se ejecuta dos veces, eliminar primero los GameObjects creados anteriormente.
/// </summary>
public static class ConstruirEscenaCompleta
{
    // ─── IDs INTERNOS DE CAMPOS SERIALIZADOS ─────────────────────────────────
    // Se usan para wiring via SerializedObject. Deben coincidir exactamente
    // con los nombres de campos privados en cada script.

    // =========================================================================
    // AGREGAR ANALIZADOR A ESCENA EXISTENTE (no destructivo)
    // =========================================================================

    [MenuItem("SCARA/Agregar Analizador de Sketch")]
    public static void AgregarAnalizador()
    {
        // ── Buscar objetos existentes ────────────────────────────────────────
        GameObject gestorGO = GameObject.Find("GestorPrincipal");
        if (gestorGO == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'GestorPrincipal' en la escena.\n" +
                "Ejecuta primero 'SCARA → Construir Escena Completa'.", "OK");
            return;
        }

        GameObject panelInferior = GameObject.Find("PanelInferior");
        if (panelInferior == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'PanelInferior' en la escena.\n" +
                "Ejecuta primero 'SCARA → Construir Escena Completa'.", "OK");
            return;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró ningún Canvas en la escena.", "OK");
            return;
        }

        // ── 1. AnalizadorSketch en GestorPrincipal ───────────────────────────
        if (gestorGO.GetComponent<AnalizadorSketch>() == null)
        {
            Undo.AddComponent<AnalizadorSketch>(gestorGO);
            Debug.Log("[SCARA] AnalizadorSketch agregado a GestorPrincipal.");
        }
        else
        {
            Debug.Log("[SCARA] AnalizadorSketch ya existe en GestorPrincipal, sin cambios.");
        }

        // ── 2. Botón ⚙ Analizar en BarraBotones ─────────────────────────────
        Transform barraBotones = panelInferior.transform.Find("BarraBotones");
        if (barraBotones == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'BarraBotones' dentro de PanelInferior.", "OK");
            return;
        }

        GameObject btnExistente = barraBotones.Find("BotonAnalisis")?.gameObject;
        if (btnExistente == null)
        {
            GameObject btnAnalisis = CrearBoton("BotonAnalisis", barraBotones, "Analizar");
            Undo.RegisterCreatedObjectUndo(btnAnalisis, "Agregar BotonAnalisis");
            PosicionarAbsoluto(btnAnalisis, new Vector2(350, -18));
            Debug.Log("[SCARA] BotonAnalisis creado en BarraBotones.");
        }
        else
        {
            Debug.Log("[SCARA] BotonAnalisis ya existe, sin cambios.");
        }

        // ── 3. Wiring de PanelConsola ────────────────────────────────────────
        PanelConsola panelConsola = panelInferior.GetComponent<PanelConsola>();
        if (panelConsola != null)
        {
            var so = new SerializedObject(panelConsola);

            var propBtn = so.FindProperty("botonAnalisis");
            if (propBtn != null && propBtn.objectReferenceValue == null)
                propBtn.objectReferenceValue =
                    barraBotones.Find("BotonAnalisis")?.GetComponent<Button>();

            var propAnaliz = so.FindProperty("analizador");
            if (propAnaliz != null && propAnaliz.objectReferenceValue == null)
                propAnaliz.objectReferenceValue = gestorGO.GetComponent<AnalizadorSketch>();

            var propCanvas = so.FindProperty("canvasPrincipal");
            if (propCanvas != null && propCanvas.objectReferenceValue == null)
                propCanvas.objectReferenceValue = canvas;

            so.ApplyModifiedProperties();
            Debug.Log("[SCARA] PanelConsola: campos de analizador cableados.");
        }

        // ── 4. Asignar onClick del botón ─────────────────────────────────────
        Button btnComp = barraBotones.Find("BotonAnalisis")?.GetComponent<Button>();
        if (btnComp != null && panelConsola != null)
        {
            btnComp.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(btnComp.onClick, panelConsola.AlPresionarAnalisis);
            Debug.Log("[SCARA] BotonAnalisis → onClick → PanelConsola.AlPresionarAnalisis asignado.");
        }

        // ── Marcar escena sucia ──────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Listo",
            "Analizador de Sketch agregado y cableado correctamente.\n\n" +
            "Guarda la escena (Ctrl+S).", "Aceptar");
    }

    [MenuItem("SCARA/Construir Escena Completa")]
    public static void ConstruirTodo()
    {
        bool confirmar = EditorUtility.DisplayDialog(
            "Construir Escena Completa",
            "Esto creará todos los GameObjects, Canvas y conexiones de la escena SCARA.\n\n" +
            "• No modifica el modelo 3D existente.\n" +
            "• Si ya existen objetos SCARA, elimínalos primero.\n\n" +
            "¿Continuar?",
            "Construir", "Cancelar");

        if (!confirmar) return;

        // ── Paso 1: EventSystem ──────────────────────────────────────────────
        AsegurarEventSystem();

        // ── Paso 2: Gestor principal ─────────────────────────────────────────
        GameObject gestorGO = CrearGestorPrincipal();

        // ── Paso 3: Canvas ───────────────────────────────────────────────────
        GameObject canvasGO = CrearCanvas();

        // ── Paso 4: Plantillas (templates para Instantiate) ──────────────────
        GameObject plantillasGO = CrearPlantillas(canvasGO);

        // ── Paso 5: Paneles del Canvas ────────────────────────────────────────
        GameObject goBarraSup  = CrearPanelBarraSuperior(canvasGO);
        GameObject goIzquierdo = CrearPanelIzquierdo(canvasGO);
        GameObject goDerecho   = CrearPanelDerecho(canvasGO);
        GameObject goInferior  = CrearPanelInferior(canvasGO, plantillasGO);
        GameObject goErrores   = CrearPanelErrores(canvasGO, plantillasGO);

        // ── Paso 6: Cámara orbital ────────────────────────────────────────────
        ConfigurarCamara();

        // ── Paso 7: Wiring completo ───────────────────────────────────────────
        WirarTodo(gestorGO, canvasGO, goBarraSup, goIzquierdo,
                  goDerecho, goInferior, goErrores, plantillasGO);

        // ── Paso 8: Guardar ───────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA] Escena construida correctamente. Guarda la escena (Ctrl+S).");
        EditorUtility.DisplayDialog("Listo",
            "Escena construida correctamente.\n\n" +
            "Recuerda:\n" +
            "1. Crea los assets de motores (SCARA → Crear Assets de Motores)\n" +
            "2. Arrastra los 5 assets al GestorPrincipal → GestorSCARA\n" +
            "3. Guarda la escena (Ctrl+S)",
            "Aceptar");
    }

    // =========================================================================
    // PASO 1 — EVENT SYSTEM
    // =========================================================================

    private static void AsegurarEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
    }

    // =========================================================================
    // PASO 2 — GESTOR PRINCIPAL
    // =========================================================================

    private static GameObject CrearGestorPrincipal()
    {
        // Si ya existe GestorPrincipal, editarlo en vez de crear uno nuevo
        GameObject go = GameObject.Find("GestorPrincipal");
        if (go == null)
        {
            go = new GameObject("GestorPrincipal");
            Undo.RegisterCreatedObjectUndo(go, "Crear GestorPrincipal");
        }

        if (go.GetComponent<LectorSerial>()     == null) go.AddComponent<LectorSerial>();
        if (go.GetComponent<GestorSCARA>()      == null) go.AddComponent<GestorSCARA>();
        if (go.GetComponent<AnalizadorSketch>() == null) go.AddComponent<AnalizadorSketch>();

        return go;
    }

    // =========================================================================
    // PASO 3 — CANVAS PRINCIPAL
    // =========================================================================

    private static GameObject CrearCanvas()
    {
        GameObject go = new GameObject("CanvasPrincipal");
        Undo.RegisterCreatedObjectUndo(go, "Crear CanvasPrincipal");

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        return go;
    }

    // =========================================================================
    // PASO 4 — PLANTILLAS (GameObjects desactivados usados como prefabs)
    // =========================================================================

    private static GameObject CrearPlantillas(GameObject canvas)
    {
        // Contenedor padre desactivado — sus hijos tampoco se activarán
        GameObject cont = CrearGOVacio("_Plantillas", canvas.transform);
        cont.SetActive(false);

        // Plantilla: línea de log (PanelConsola)
        GameObject lineaLog = CrearTextoSimple("Plantilla_LineaLog", cont.transform,
            "Mensaje de log", 14, ColoresUI.TextoPrincipal);

        // Plantilla: ítem de error (PanelErrores)
        GameObject itemError = CrearGOConImagen("Plantilla_ItemError", cont.transform,
            ColoresUI.FondoSecundario, new Vector2(280, 50));

        HorizontalLayoutGroup hlgError = itemError.AddComponent<HorizontalLayoutGroup>();
        hlgError.padding            = new RectOffset(8, 8, 4, 4);
        hlgError.spacing            = 6f;
        hlgError.childAlignment     = TextAnchor.MiddleLeft;
        hlgError.childControlWidth  = true;
        hlgError.childControlHeight = true;
        hlgError.childForceExpandWidth  = false;
        hlgError.childForceExpandHeight = true;

        GameObject txtCodigo   = CrearTextoSimple("TextoCodigo",   itemError.transform, "E01",         13, ColoresUI.Error);
        GameObject txtMensaje  = CrearTextoSimple("TextoMensaje",  itemError.transform, "Descripción", 12, ColoresUI.TextoPrincipal);
        GameObject txtContador = CrearTextoSimple("TextoContador", itemError.transform, "×1",          12, ColoresUI.TextoSecundario);

        txtCodigo.AddComponent<LayoutElement>().preferredWidth   = 40;
        txtMensaje.AddComponent<LayoutElement>().preferredWidth  = 180;
        txtContador.AddComponent<LayoutElement>().preferredWidth = 36;

        // Plantilla: ítem de historial (PanelControlArticular)
        GameObject itemHistorial = CrearTextoSimple("Plantilla_ItemHistorial", cont.transform,
            "#000  J1:0°  J2:0°  Z:0mm", 12, ColoresUI.TextoSecundario);

        // Plantilla: ítem de detección BT (PanelVisionBT)
        GameObject itemDeteccion = CrearTextoSimple("Plantilla_ItemDeteccion", cont.transform,
            "[00:00:00] Detección", 13, ColoresUI.LogSketch);

        return cont;
    }

    // =========================================================================
    // PASO 5A — PANEL BARRA SUPERIOR
    // =========================================================================

    private static GameObject CrearPanelBarraSuperior(GameObject canvas)
    {
        // Panel raíz — fijo en la parte superior
        GameObject panel = CrearPanelAnclado("PanelBarraSuperior", canvas.transform,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -60), new Vector2(0, 0));
        AplicarColor(panel, ColoresUI.FondoBarra);
        panel.AddComponent<PanelBarraSuperior>();

        // Indicador serial (círculo de color)
        GameObject indicador = CrearGOConImagen("IndicadorSerial", panel.transform,
            ColoresUI.Error, new Vector2(20, 20));
        PosicionarAnclado(indicador, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(20, 0), new Vector2(20, 20));

        // Texto puerto
        GameObject txtPuerto = CrearTexto("TextoPuerto", panel.transform,
            "Desconectado", 14, ColoresUI.TextoPrincipal);
        PosicionarAnclado(txtPuerto, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(45, 0), new Vector2(180, 30));

        // Dropdown de puertos
        GameObject dropdown = CrearDropdown("DesplegablePuerto", panel.transform);
        PosicionarAnclado(dropdown, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(230, 0), new Vector2(160, 36));

        // Botón conectar
        GameObject btnConectar = CrearBoton("BotonConectar", panel.transform, "Conectar");
        PosicionarAnclado(btnConectar, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(400, 0), new Vector2(120, 36));

        // Texto módulo (centro)
        GameObject txtModulo = CrearTexto("TextoModulo", panel.transform,
            "Módulo 1 — Control Articular", 16, ColoresUI.AcentoAmarillo);
        PosicionarAnclado(txtModulo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 0), new Vector2(500, 40));
        txtModulo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Alerta overflow (oculta por defecto)
        GameObject alertaOvf = CrearGOConImagen("AlertaOverflow", panel.transform,
            new Color(ColoresUI.Error.r, ColoresUI.Error.g, ColoresUI.Error.b, 0.85f),
            new Vector2(280, 40));
        PosicionarAnclado(alertaOvf, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-150, 0), new Vector2(280, 40));
        CrearTexto("TextoAlertaOverflow", alertaOvf.transform,
            "OVERFLOW: 0 eventos perdidos", 13, ColoresUI.TextoPrincipal);
        alertaOvf.SetActive(false);

        return panel;
    }

    // =========================================================================
    // PASO 5B — PANEL IZQUIERDO (estado del robot)
    // =========================================================================

    private static GameObject CrearPanelIzquierdo(GameObject canvas)
    {
        GameObject panel = CrearPanelAnclado("PanelIzquierdo", canvas.transform,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 200), new Vector2(320, -60));
        AplicarColor(panel, ColoresUI.FondoPrincipal);

        // Componente de estado
        panel.AddComponent<PanelEstadoRobot>();

        // ── Sección título ────────────────────────────────────────────────────
        CrearEtiquetaSeccion("Estado del Robot", panel.transform, 10);

        float yOffset = -45f;

        // ── M1 Eje Z ──────────────────────────────────────────────────────────
        yOffset = CrearItemMotor(panel.transform, "M1 - Eje Z",
            "TextoM1_Posicion", "BarraM1_Progreso", yOffset,
            ConfiguracionRobot.Z_Min_mm, ConfiguracionRobot.Z_Max_mm);

        // ── M2 Brazo 1 ────────────────────────────────────────────────────────
        yOffset = CrearItemMotor(panel.transform, "M2 - Brazo 1 (J1)",
            "TextoM2_Angulo", "BarraM2_Progreso", yOffset,
            ConfiguracionRobot.J1_Min, ConfiguracionRobot.J1_Max);

        // ── M3 Brazo 2 ────────────────────────────────────────────────────────
        yOffset = CrearItemMotor(panel.transform, "M3 - Brazo 2 (J2)",
            "TextoM3_Angulo", "BarraM3_Progreso", yOffset,
            ConfiguracionRobot.J2_Min, ConfiguracionRobot.J2_Max);

        // ── Separador ─────────────────────────────────────────────────────────
        CrearSeparador(panel.transform, yOffset);
        yOffset -= 15f;

        // ── S1 ────────────────────────────────────────────────────────────────
        yOffset = CrearItemMotor(panel.transform, "S1 - GiroGarra",
            "TextoS1_Angulo", "BarraS1_Progreso", yOffset,
            ConfiguracionRobot.S1_Min, ConfiguracionRobot.S1_Max);

        // ── S2 / Gripper ──────────────────────────────────────────────────────
        yOffset = CrearItemMotor(panel.transform, "S2 - Gripper",
            "TextoS2_Angulo", "BarraS2_Progreso", yOffset,
            ConfiguracionRobot.S2_Min, ConfiguracionRobot.S2_Max);

        // Estado gripper
        GameObject estadoGripper = CrearGOConImagen("ImagenEstadoGripper", panel.transform,
            ColoresUI.TextoSecundario, new Vector2(12, 12));
        PosicionarAbsoluto(estadoGripper, new Vector2(15, yOffset));
        GameObject txtGripper = CrearTexto("TextoEstadoGripper", panel.transform,
            "ABIERTO", 13, ColoresUI.Advertencia);
        PosicionarAbsoluto(txtGripper, new Vector2(32, yOffset));
        yOffset -= 30f;

        // ── Separador TCP ─────────────────────────────────────────────────────
        CrearSeparador(panel.transform, yOffset);
        yOffset -= 15f;
        CrearEtiquetaSeccion("Posición TCP", panel.transform, -yOffset);
        yOffset -= 30f;

        // Campos XYZ y radio
        string[] etiquetasTCP  = { "TextoPosicionX", "TextoPosicionY", "TextoPosicionZ",
                                    "TextoRadioTCP",  "TextoEspacioTrabajo" };
        string[] textosTCP     = { "X: 0.0 mm", "Y: 0.0 mm", "Z: 0.0 mm",
                                    "R: 0.0 mm", "En espacio de trabajo" };

        foreach (var (nombre, texto) in System.Linq.Enumerable.Zip(etiquetasTCP, textosTCP,
            (a, b) => (a, b)))
        {
            GameObject campo = CrearTexto(nombre, panel.transform, texto, 13,
                nombre == "TextoEspacioTrabajo" ? ColoresUI.Exito : ColoresUI.TextoPrincipal);
            PosicionarAbsoluto(campo, new Vector2(10, yOffset));
            yOffset -= 22f;
        }

        return panel;
    }

    // =========================================================================
    // PASO 5C — PANEL DERECHO (módulos M1, M2, M3)
    // =========================================================================

    private static GameObject CrearPanelDerecho(GameObject canvas)
    {
        GameObject panel = CrearPanelAnclado("PanelDerecho", canvas.transform,
            new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(-300, 200), new Vector2(0, -60));
        AplicarColor(panel, ColoresUI.FondoPrincipal);

        // ── Panel Módulo 1 (Control Articular) ────────────────────────────────
        GameObject panelM1 = CrearGOVacio("PanelControlArticular", panel.transform);
        EstirarEnPadre(panelM1);
        panelM1.AddComponent<PanelControlArticular>();
        PopularPanelControlArticular(panelM1);

        // ── Panel Módulo 2 (Trayectorias) ─────────────────────────────────────
        GameObject panelM2 = CrearGOVacio("PanelTrayectorias", panel.transform);
        EstirarEnPadre(panelM2);
        panelM2.AddComponent<PanelTrayectorias>();
        PopularPanelTrayectorias(panelM2);
        panelM2.SetActive(false);   // Inactivo por defecto

        // ── Panel Módulo 3 (Visión BT) ────────────────────────────────────────
        GameObject panelM3 = CrearGOVacio("PanelVisionBT", panel.transform);
        EstirarEnPadre(panelM3);
        panelM3.AddComponent<PanelVisionBT>();
        PopularPanelVisionBT(panelM3);
        panelM3.SetActive(false);   // Inactivo por defecto

        return panel;
    }

    private static void PopularPanelControlArticular(GameObject panel)
    {
        CrearEtiquetaSeccion("Control Articular — M1", panel.transform, 10);
        float y = -45f;

        string[] campos  = { "TextoJ1_Angulo","TextoJ2_Angulo","TextoZ_Posicion",
                              "TextoS1_Angulo","TextoS2_Angulo" };
        string[] valores = { "J1: 0.00°","J2: 0.00°","Z: 0.00 mm","S1: 0.0°","S2: 0.0°" };

        foreach (var (c, v) in System.Linq.Enumerable.Zip(campos, valores, (a, b) => (a, b)))
        {
            var go = CrearTexto(c, panel.transform, v, 14, ColoresUI.AcentoAmarillo);
            PosicionarAbsoluto(go, new Vector2(10, y));
            y -= 28f;
        }

        CrearSeparador(panel.transform, y);
        y -= 15f;
        CrearEtiquetaSeccion("TCP", panel.transform, -y);
        y -= 30f;

        string[] camposTCP  = { "TextoTCP_X","TextoTCP_Y","TextoTCP_Radio","TextoEspacioEstado" };
        string[] valoresTCP = { "X: 0.00 mm","Y: 0.00 mm","R: 0.00 mm","Dentro del espacio de trabajo" };
        Color[]  coloresTCP = { ColoresUI.TextoPrincipal, ColoresUI.TextoPrincipal,
                                 ColoresUI.TextoPrincipal, ColoresUI.Exito };

        foreach (var (idx, (c, v)) in System.Linq.Enumerable.Select(
            System.Linq.Enumerable.Zip(camposTCP, valoresTCP, (a, b) => (a, b)),
            (item, i) => (i, item)))
        {
            var go = CrearTexto(c, panel.transform, v, 13, coloresTCP[idx]);
            PosicionarAbsoluto(go, new Vector2(10, y));
            y -= 22f;
            // Guardar referencia de indicador espacio
            if (c == "TextoEspacioEstado")
            {
                GameObject ind = CrearGOConImagen("ImagenIndicadorEspacio",
                    panel.transform, ColoresUI.Exito, new Vector2(14, 14));
                PosicionarAbsoluto(ind, new Vector2(280, y + 22f));
            }
        }

        // Historial
        CrearSeparador(panel.transform, y - 5f);
        y -= 20f;
        CrearEtiquetaSeccion("Historial de posiciones", panel.transform, -y);
        y -= 25f;

        // ScrollRect para historial
        GameObject scrollHistorial = CrearScrollRect("ScrollHistorial",
            panel.transform, new Vector2(10, y), new Vector2(280, 180));

        // Renombrar el contenido interno a "ContenedorHistorial" para que el wiring lo encuentre
        Transform contenidoHistorial = scrollHistorial.transform.Find("Viewport/Contenido");
        if (contenidoHistorial != null)
        {
            contenidoHistorial.name = "ContenedorHistorial";
            VerticalLayoutGroup vlgH = contenidoHistorial.GetComponent<VerticalLayoutGroup>();
            if (vlgH != null) vlgH.childForceExpandWidth = true;
        }

        // Botón limpiar historial
        y -= 195f;
        GameObject btnLimpiar = CrearBoton("BotonLimpiarHistorial", panel.transform, "Limpiar historial");
        PosicionarAbsoluto(btnLimpiar, new Vector2(10, y));
    }

    private static void PopularPanelTrayectorias(GameObject panel)
    {
        CrearEtiquetaSeccion("Trayectorias — M2", panel.transform, 10);
        float y = -45f;

        CrearTexto("TextoFaseActual", panel.transform, "Fase: En espera", 14, ColoresUI.AcentoAmarillo);
        PosicionarAbsoluto(panel.transform.Find("TextoFaseActual").gameObject, new Vector2(10, y));
        y -= 30f;

        string[] camposDesv  = { "TextoDesvActual","TextoDesvPromedio","TextoDesvMaxima" };
        string[] valoresDesv = { "Actual:   0.00 mm","Promedio: 0.00 mm","Máxima:   0.00 mm" };
        foreach (var (c, v) in System.Linq.Enumerable.Zip(camposDesv, valoresDesv, (a, b) => (a, b)))
        {
            GameObject ind = c == "TextoDesvActual"
                ? CrearGOConImagen("ImagenIndicadorTrayectoria", panel.transform,
                    ColoresUI.Exito, new Vector2(12, 12)) : null;
            if (ind != null) PosicionarAbsoluto(ind, new Vector2(10, y));

            var go = CrearTexto(c, panel.transform, v, 13, ColoresUI.TextoPrincipal);
            PosicionarAbsoluto(go, new Vector2(28, y));
            y -= 22f;
        }

        CrearSeparador(panel.transform, y);
        y -= 15f;

        // Gripper
        CrearTexto("TextoEstadoGripper", panel.transform, "ABIERTO", 14, ColoresUI.Advertencia);
        PosicionarAbsoluto(panel.transform.Find("TextoEstadoGripper").gameObject, new Vector2(10, y));
        y -= 25f;
        var imgGripper = CrearGOConImagen("ImagenEstadoGripper", panel.transform,
            ColoresUI.TextoSecundario, new Vector2(14, 14));
        PosicionarAbsoluto(imgGripper, new Vector2(10, y));
        CrearTexto("TextoObjetoAgarrado", panel.transform, "Objeto: Ninguno", 13, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(panel.transform.Find("TextoObjetoAgarrado").gameObject, new Vector2(30, y));
        y -= 30f;

        CrearSeparador(panel.transform, y);
        y -= 15f;

        // Contadores
        string[] contadores = { "TextoExitos","TextoFallos","TextoTotalCiclos" };
        string[] valsCount  = { "Éxitos:  0","Fallos:  0","Total:   0" };
        Color[]  colsCount  = { ColoresUI.Exito, ColoresUI.Error, ColoresUI.TextoPrincipal };
        for (int i = 0; i < contadores.Length; i++)
        {
            var go = CrearTexto(contadores[i], panel.transform, valsCount[i], 13, colsCount[i]);
            PosicionarAbsoluto(go, new Vector2(10, y));
            y -= 22f;
        }

        y -= 10f;
        string[] botonesT = { "BotonRepetirSecuencia","BotonLimpiarTrayectoria",
                               "BotonReiniciarCiclos","BotonReiniciarObjetos" };
        string[] texBtT   = { "Repetir secuencia","Limpiar trayectoria",
                               "Reiniciar ciclos","Reiniciar objetos" };
        for (int i = 0; i < botonesT.Length; i++)
        {
            var btn = CrearBoton(botonesT[i], panel.transform, texBtT[i]);
            PosicionarAbsoluto(btn, new Vector2(10, y));
            y -= 45f;
        }
    }

    private static void PopularPanelVisionBT(GameObject panel)
    {
        CrearEtiquetaSeccion("Visión Artificial + BT — M3", panel.transform, 10);
        float y = -45f;

        // Indicador BT
        var imgBT = CrearGOConImagen("ImagenIndicadorBT", panel.transform,
            ColoresUI.BTDesconectado, new Vector2(18, 18));
        PosicionarAbsoluto(imgBT, new Vector2(10, y));
        var txtBT = CrearTexto("TextoEstadoBT", panel.transform,
            "Bluetooth: DESCONECTADO", 14, ColoresUI.BTDesconectado);
        PosicionarAbsoluto(txtBT, new Vector2(34, y));
        y -= 30f;

        // Nombre del dispositivo BT
        var txtNombre = CrearTexto("TextoNombreDispositivo", panel.transform,
            "Dispositivo: SCARA_ESP32", 12, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtNombre, new Vector2(10, y));
        y -= 22f;

        // Timestamp de última actividad BT
        var txtStamp = CrearTexto("TextoTimestamp", panel.transform,
            "Último BT: —", 12, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtStamp, new Vector2(10, y));
        y -= 22f;

        // Advertencia de timeout (oculta por defecto)
        var txtAdvert = CrearTexto("TextoAdvertenciaBT", panel.transform,
            "Sin actividad BT: 0s", 12, ColoresUI.Advertencia);
        PosicionarAbsoluto(txtAdvert, new Vector2(10, y));
        txtAdvert.GetComponent<RectTransform>();   // asegurar RT
        txtAdvert.SetActive(false);
        y -= 22f;

        // Fuente de señal
        var imgSerial = CrearGOConImagen("ImagenFuenteSerial", panel.transform,
            ColoresUI.Exito, new Vector2(12, 12));
        PosicionarAbsoluto(imgSerial, new Vector2(10, y));
        var imgFuenteBT = CrearGOConImagen("ImagenFuenteBT", panel.transform,
            ColoresUI.BTDesconectado, new Vector2(12, 12));
        PosicionarAbsoluto(imgFuenteBT, new Vector2(30, y));
        var txtFuente = CrearTexto("TextoFuenteActiva", panel.transform,
            "Señal: Solo Serial", 12, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtFuente, new Vector2(50, y));
        y -= 28f;

        CrearSeparador(panel.transform, y);
        y -= 15f;

        // Último comando BT
        var txtUltimo = CrearTexto("TextoUltimoComando", panel.transform,
            "Último: —", 13, ColoresUI.AcentoAmarillo);
        PosicionarAbsoluto(txtUltimo, new Vector2(10, y));
        y -= 25f;

        // Fase de visión
        var txtFase = CrearTexto("TextoFaseVision", panel.transform,
            "Fase: En espera", 14, ColoresUI.TextoPrincipal);
        PosicionarAbsoluto(txtFase, new Vector2(10, y));
        y -= 25f;

        // Objeto detectado
        var imgAgarre = CrearGOConImagen("ImagenEstadoAgarre", panel.transform,
            ColoresUI.TextoSecundario, new Vector2(14, 14));
        PosicionarAbsoluto(imgAgarre, new Vector2(10, y));
        var txtObjeto = CrearTexto("TextoObjetoDetectado", panel.transform,
            "Objeto: No detectado", 13, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtObjeto, new Vector2(30, y));
        y -= 30f;

        CrearSeparador(panel.transform, y);
        y -= 15f;

        // Contador detecciones
        var txtCount = CrearTexto("TextoContadorDetecciones", panel.transform,
            "Detecciones: 0", 13, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtCount, new Vector2(10, y));
        y -= 25f;

        // ScrollRect de detecciones (altura reducida para acomodar nuevos campos)
        var scrollDet = CrearScrollRect("ScrollDetecciones", panel.transform,
            new Vector2(5, y), new Vector2(288, 140));
        y -= 150f;
    }

    // =========================================================================
    // PASO 5D — PANEL INFERIOR (consola de log)
    // =========================================================================

    private static GameObject CrearPanelInferior(GameObject canvas, GameObject plantillas)
    {
        GameObject panel = CrearPanelAnclado("PanelInferior", canvas.transform,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 200));
        AplicarColor(panel, ColoresUI.FondoBarra);
        panel.AddComponent<PanelConsola>();

        // Barra de botones superior del panel
        GameObject barraBtn = CrearPanelAnclado("BarraBotones", panel.transform,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -36), new Vector2(0, 0));
        AplicarColor(barraBtn, ColoresUI.FondoPrincipal);

        float xBtn = 10f;
        GameObject btnPausa    = CrearBoton("BotonPausa",    barraBtn.transform, "Pausar");
        GameObject btnLimpiar  = CrearBoton("BotonLimpiar",  barraBtn.transform, "Limpiar");
        GameObject btnExportar = CrearBoton("BotonExportar", barraBtn.transform, "Exportar .txt");
        GameObject btnAnalisis = CrearBoton("BotonAnalisis", barraBtn.transform, "Analizar");
        PosicionarAbsoluto(btnPausa,    new Vector2(xBtn,       -18));
        PosicionarAbsoluto(btnLimpiar,  new Vector2(xBtn + 110, -18));
        PosicionarAbsoluto(btnExportar, new Vector2(xBtn + 220, -18));
        PosicionarAbsoluto(btnAnalisis, new Vector2(xBtn + 340, -18));

        // Toggles de filtro (desplazados +100 respecto a la posición original)
        string[] filtros = { "FiltroT", "FiltroE", "FiltroL", "FiltroB" };
        string[] etiqFiltros = { "T", "E", "L", "B" };
        for (int i = 0; i < filtros.Length; i++)
        {
            GameObject tog = CrearToggle(filtros[i], barraBtn.transform, etiqFiltros[i], true);
            PosicionarAbsoluto(tog, new Vector2(470 + i * 60, -18));
        }

        // Contador de mensajes
        GameObject txtCont = CrearTexto("TextoContador", barraBtn.transform,
            "Mensajes: 0", 12, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(txtCont, new Vector2(720, -18));

        // ScrollRect de mensajes
        GameObject scroll = CrearScrollRect("ScrollConsola", panel.transform,
            new Vector2(0, 0), new Vector2(0, 0));
        EstirarConOffset(scroll, new Vector2(0, 0), new Vector2(0, -36));
        scroll.name = "ConsolaLog";

        // Plantilla de línea de log ya está en plantillas, se referenciará en wiring

        return panel;
    }

    // =========================================================================
    // PASO 5E — PANEL DE ERRORES ACTIVOS (flotante)
    // =========================================================================

    private static GameObject CrearPanelErrores(GameObject canvas, GameObject plantillas)
    {
        GameObject panel = CrearPanelAnclado("PanelErroresActivos", canvas.transform,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-305, -310), new Vector2(-5, -65));
        AplicarColor(panel, new Color(
            ColoresUI.FondoBarra.r, ColoresUI.FondoBarra.g, ColoresUI.FondoBarra.b, 0.92f));
        panel.AddComponent<PanelErrores>();

        // Título
        GameObject titulo = CrearTexto("TituloErrores", panel.transform,
            "Errores activos", 14, ColoresUI.Error);
        PosicionarAbsoluto(titulo, new Vector2(8, -8));

        // Contador de sesión
        GameObject contador = CrearTexto("TextoContadorSesion", panel.transform,
            "0 errores en sesión", 11, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(contador, new Vector2(8, -28));

        // Botón limpiar errores
        GameObject btnLimpiar = CrearBoton("BotonLimpiarErrores", panel.transform, "Limpiar");
        PosicionarAbsoluto(btnLimpiar, new Vector2(210, -18));

        // Contenedor de errores (VerticalLayoutGroup)
        GameObject contenedor = CrearGOVacio("ContenedorErrores", panel.transform);
        EstirarConOffset(contenedor, new Vector2(0, 0), new Vector2(0, -50));
        VerticalLayoutGroup vlg = contenedor.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 2f;
        vlg.padding             = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = contenedor.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return panel;
    }

    // =========================================================================
    // PASO 6 — CÁMARA ORBITAL
    // =========================================================================

    private static void ConfigurarCamara()
    {
        Camera camPrincipal = Camera.main;
        if (camPrincipal == null)
        {
            Debug.LogWarning("[SCARA] No se encontró la cámara principal. " +
                             "Agrega CamaraOrbital manualmente.");
            return;
        }

        if (camPrincipal.GetComponent<CamaraOrbital>() == null)
            camPrincipal.gameObject.AddComponent<CamaraOrbital>();

        // Fondo azul marino oscuro
        camPrincipal.backgroundColor = ColoresUI.FondoPrincipal;
        camPrincipal.clearFlags       = CameraClearFlags.SolidColor;
    }

    // =========================================================================
    // PASO 7 — WIRING COMPLETO (todos los SerializeField)
    // =========================================================================

    private static void WirarTodo(
        GameObject gestorGO, GameObject canvasGO,
        GameObject goBarraSup, GameObject goIzquierdo,
        GameObject goDerecho, GameObject goInferior,
        GameObject goErrores, GameObject plantillasGO)
    {
        GestorSCARA gestor       = gestorGO.GetComponent<GestorSCARA>();
        LectorSerial lector      = gestorGO.GetComponent<LectorSerial>();

        // ── Wiring de GestorSCARA ────────────────────────────────────────────
        var soGestor = new SerializedObject(gestor);

        // LectorSerial
        soGestor.FindProperty("lectorSerial").objectReferenceValue = lector;

        // Transforms del robot (buscar por nombre en la jerarquía activa)
        soGestor.FindProperty("articulacionVertical").objectReferenceValue =
            BuscarEnEscena("ARTICULACION_VERTICAL");
        soGestor.FindProperty("pivotArt1").objectReferenceValue =
            BuscarEnEscena("pivot_art1");
        soGestor.FindProperty("pivotArt2").objectReferenceValue =
            BuscarEnEscena("pivot_art2");
        soGestor.FindProperty("giroGarra").objectReferenceValue =
            BuscarEnEscena("GiroGarra");
        soGestor.FindProperty("garra1").objectReferenceValue =
            BuscarEnEscena("Garra_1");
        soGestor.FindProperty("garra2").objectReferenceValue =
            BuscarEnEscena("Garra_2");

        // Paneles
        soGestor.FindProperty("panelBarra").objectReferenceValue =
            goBarraSup.GetComponent<PanelBarraSuperior>();
        soGestor.FindProperty("panelEstado").objectReferenceValue =
            goIzquierdo.GetComponent<PanelEstadoRobot>();
        soGestor.FindProperty("panelConsola").objectReferenceValue =
            goInferior.GetComponent<PanelConsola>();
        soGestor.FindProperty("panelErrores").objectReferenceValue =
            goErrores.GetComponent<PanelErrores>();
        soGestor.FindProperty("panelControlArticular").objectReferenceValue =
            goDerecho.transform.Find("PanelControlArticular")?.GetComponent<PanelControlArticular>();
        soGestor.FindProperty("panelTrayectorias").objectReferenceValue =
            goDerecho.transform.Find("PanelTrayectorias")?.GetComponent<PanelTrayectorias>();
        soGestor.FindProperty("panelVisionBT").objectReferenceValue =
            goDerecho.transform.Find("PanelVisionBT")?.GetComponent<PanelVisionBT>();

        soGestor.ApplyModifiedProperties();

        // ── Wiring de PanelBarraSuperior ─────────────────────────────────────
        WirarBarraSuperior(goBarraSup, gestorGO.GetComponent<GestorSCARA>());

        // ── Wiring de PanelEstadoRobot ────────────────────────────────────────
        WirarPanelEstado(goIzquierdo);

        // ── Wiring de PanelConsola ────────────────────────────────────────────
        WirarPanelConsola(goInferior, plantillasGO, gestorGO, canvasGO);

        // ── Wiring de PanelErrores ────────────────────────────────────────────
        WirarPanelErrores(goErrores, plantillasGO);

        // ── Wiring de paneles de módulo ───────────────────────────────────────
        WirarPanelControlArticular(goDerecho.transform.Find("PanelControlArticular")?.gameObject,
                                   plantillasGO);
        WirarPanelTrayectorias(goDerecho.transform.Find("PanelTrayectorias")?.gameObject);
        WirarPanelVisionBT(goDerecho.transform.Find("PanelVisionBT")?.gameObject, plantillasGO);

        // ── Wiring de CamaraOrbital ───────────────────────────────────────────
        WirarCamara();

        Debug.Log("[SCARA] Wiring completo.");
    }

    private static void WirarBarraSuperior(GameObject panel, GestorSCARA gestor)
    {
        var comp = panel.GetComponent<PanelBarraSuperior>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        so.FindProperty("imagenIndicadorSerial").objectReferenceValue =
            panel.transform.Find("IndicadorSerial")?.GetComponent<Image>();
        so.FindProperty("textoNombrePuerto").objectReferenceValue =
            panel.transform.Find("TextoPuerto")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("textoModulo").objectReferenceValue =
            panel.transform.Find("TextoModulo")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("desplegablePuerto").objectReferenceValue =
            panel.transform.Find("DesplegablePuerto")?.GetComponent<TMP_Dropdown>();
        so.FindProperty("botonConectar").objectReferenceValue =
            panel.transform.Find("BotonConectar")?.GetComponent<Button>();
        so.FindProperty("textoBotonConectar").objectReferenceValue =
            panel.transform.Find("BotonConectar/Texto")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("contenedorAlertaOverflow").objectReferenceValue =
            panel.transform.Find("AlertaOverflow")?.gameObject;
        so.FindProperty("textoAlertaOverflow").objectReferenceValue =
            panel.transform.Find("AlertaOverflow/TextoAlertaOverflow")?.GetComponent<TextMeshProUGUI>();

        so.ApplyModifiedProperties();
    }

    private static void WirarPanelEstado(GameObject panel)
    {
        var comp = panel.GetComponent<PanelEstadoRobot>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        var campos = new System.Collections.Generic.Dictionary<string, string>
        {
            { "textoM1_Posicion",  "TextoM1_Posicion"  },
            { "textoM2_Angulo",    "TextoM2_Angulo"    },
            { "textoM3_Angulo",    "TextoM3_Angulo"    },
            { "textoS1_Angulo",    "TextoS1_Angulo"    },
            { "textoS2_Angulo",    "TextoS2_Angulo"    },
            { "textoEstadoGripper","TextoEstadoGripper"},
            { "textoPosicionX",    "TextoPosicionX"    },
            { "textoPosicionY",    "TextoPosicionY"    },
            { "textoPosicionZ",    "TextoPosicionZ"    },
            { "textoRadioTCP",     "TextoRadioTCP"     },
            { "textoEspacioTrabajo","TextoEspacioTrabajo"}
        };

        foreach (var par in campos)
            so.FindProperty(par.Key).objectReferenceValue =
                panel.transform.Find(par.Value)?.GetComponent<TextMeshProUGUI>();

        so.FindProperty("imagenEstadoGripper").objectReferenceValue =
            panel.transform.Find("ImagenEstadoGripper")?.GetComponent<Image>();
        so.FindProperty("barraM1_Progreso").objectReferenceValue =
            panel.transform.Find("BarraM1_Progreso")?.GetComponent<Slider>();
        so.FindProperty("barraM2_Progreso").objectReferenceValue =
            panel.transform.Find("BarraM2_Progreso")?.GetComponent<Slider>();
        so.FindProperty("barraM3_Progreso").objectReferenceValue =
            panel.transform.Find("BarraM3_Progreso")?.GetComponent<Slider>();
        so.FindProperty("barraS1_Progreso").objectReferenceValue =
            panel.transform.Find("BarraS1_Progreso")?.GetComponent<Slider>();
        so.FindProperty("barraS2_Progreso").objectReferenceValue =
            panel.transform.Find("BarraS2_Progreso")?.GetComponent<Slider>();

        so.ApplyModifiedProperties();
    }

    private static void WirarPanelConsola(GameObject panel, GameObject plantillas,
        GameObject gestorGO, GameObject canvasGO)
    {
        var comp = panel.GetComponent<PanelConsola>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        var scroll = panel.transform.Find("ConsolaLog");
        so.FindProperty("scrollConsola").objectReferenceValue =
            scroll?.GetComponent<ScrollRect>();
        so.FindProperty("contenedorMensajes").objectReferenceValue =
            scroll?.Find("Viewport/Contenido");
        so.FindProperty("prefabLineaLog").objectReferenceValue =
            plantillas.transform.Find("Plantilla_LineaLog")?.gameObject;

        so.FindProperty("botonPausa").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonPausa")?.GetComponent<Button>();
        so.FindProperty("botonLimpiar").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonLimpiar")?.GetComponent<Button>();
        so.FindProperty("botonExportar").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonExportar")?.GetComponent<Button>();
        so.FindProperty("botonAnalisis").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonAnalisis")?.GetComponent<Button>();
        so.FindProperty("textoBotonPausa").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonPausa/Texto")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("filtroT").objectReferenceValue =
            panel.transform.Find("BarraBotones/FiltroT")?.GetComponent<Toggle>();
        so.FindProperty("filtroE").objectReferenceValue =
            panel.transform.Find("BarraBotones/FiltroE")?.GetComponent<Toggle>();
        so.FindProperty("filtroL").objectReferenceValue =
            panel.transform.Find("BarraBotones/FiltroL")?.GetComponent<Toggle>();
        so.FindProperty("filtroB").objectReferenceValue =
            panel.transform.Find("BarraBotones/FiltroB")?.GetComponent<Toggle>();
        so.FindProperty("textoContador").objectReferenceValue =
            panel.transform.Find("BarraBotones/TextoContador")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("analizador").objectReferenceValue =
            gestorGO?.GetComponent<AnalizadorSketch>();
        so.FindProperty("canvasPrincipal").objectReferenceValue =
            canvasGO?.GetComponent<Canvas>();

        so.ApplyModifiedProperties();

        // onClick del BotonAnalisis → PanelConsola.AlPresionarAnalisis
        Button btnAnalisis = panel.transform.Find("BarraBotones/BotonAnalisis")
            ?.GetComponent<Button>();
        if (btnAnalisis != null)
        {
            btnAnalisis.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(btnAnalisis.onClick, comp.AlPresionarAnalisis);
        }
    }

    private static void WirarPanelErrores(GameObject panel, GameObject plantillas)
    {
        var comp = panel.GetComponent<PanelErrores>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        so.FindProperty("contenedorErrores").objectReferenceValue =
            panel.transform.Find("ContenedorErrores");
        so.FindProperty("prefabItemError").objectReferenceValue =
            plantillas.transform.Find("Plantilla_ItemError")?.gameObject;
        so.FindProperty("textoContadorSesion").objectReferenceValue =
            panel.transform.Find("TextoContadorSesion")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("botonLimpiarErrores").objectReferenceValue =
            panel.transform.Find("BotonLimpiarErrores")?.GetComponent<Button>();

        so.ApplyModifiedProperties();
    }

    private static void WirarPanelControlArticular(GameObject panel, GameObject plantillas)
    {
        if (panel == null) return;
        var comp = panel.GetComponent<PanelControlArticular>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        string[] campos = { "textoJ1_Angulo","textoJ2_Angulo","textoZ_Posicion",
                             "textoS1_Angulo","textoS2_Angulo",
                             "textoTCP_X","textoTCP_Y","textoTCP_Radio","textoEspacioEstado" };
        string[] gos    = { "TextoJ1_Angulo","TextoJ2_Angulo","TextoZ_Posicion",
                             "TextoS1_Angulo","TextoS2_Angulo",
                             "TextoTCP_X","TextoTCP_Y","TextoTCP_Radio","TextoEspacioEstado" };
        for (int i = 0; i < campos.Length; i++)
            so.FindProperty(campos[i]).objectReferenceValue =
                panel.transform.Find(gos[i])?.GetComponent<TextMeshProUGUI>();

        so.FindProperty("imagenIndicadorEspacio").objectReferenceValue =
            panel.transform.Find("ImagenIndicadorEspacio")?.GetComponent<Image>();

        var scrollH = panel.transform.Find("ScrollHistorial");
        so.FindProperty("contenedorHistorial").objectReferenceValue =
            scrollH?.Find("Viewport/ContenedorHistorial");
        so.FindProperty("prefabItemHistorial").objectReferenceValue =
            plantillas.transform.Find("Plantilla_ItemHistorial")?.gameObject;
        so.FindProperty("botonLimpiarHistorial").objectReferenceValue =
            panel.transform.Find("BotonLimpiarHistorial")?.GetComponent<Button>();

        so.ApplyModifiedProperties();
    }

    private static void WirarPanelTrayectorias(GameObject panel)
    {
        if (panel == null) return;
        var comp = panel.GetComponent<PanelTrayectorias>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        string[] campos = { "textoFaseActual","textoDesvActual","textoDesvPromedio","textoDesvMaxima",
                             "textoEstadoGripper","textoObjetoAgarrado",
                             "textoExitos","textoFallos","textoTotalCiclos" };
        string[] gos    = { "TextoFaseActual","TextoDesvActual","TextoDesvPromedio","TextoDesvMaxima",
                             "TextoEstadoGripper","TextoObjetoAgarrado",
                             "TextoExitos","TextoFallos","TextoTotalCiclos" };
        for (int i = 0; i < campos.Length; i++)
            so.FindProperty(campos[i]).objectReferenceValue =
                panel.transform.Find(gos[i])?.GetComponent<TextMeshProUGUI>();

        so.FindProperty("imagenIndicadorTrayectoria").objectReferenceValue =
            panel.transform.Find("ImagenIndicadorTrayectoria")?.GetComponent<Image>();
        so.FindProperty("imagenEstadoGripper").objectReferenceValue =
            panel.transform.Find("ImagenEstadoGripper")?.GetComponent<Image>();

        so.FindProperty("botonRepetirSecuencia").objectReferenceValue =
            panel.transform.Find("BotonRepetirSecuencia")?.GetComponent<Button>();
        so.FindProperty("botonLimpiarTrayectoria").objectReferenceValue =
            panel.transform.Find("BotonLimpiarTrayectoria")?.GetComponent<Button>();
        so.FindProperty("botonReiniciarCiclos").objectReferenceValue =
            panel.transform.Find("BotonReiniciarCiclos")?.GetComponent<Button>();
        so.FindProperty("botonReiniciarObjetos").objectReferenceValue =
            panel.transform.Find("BotonReiniciarObjetos")?.GetComponent<Button>();

        so.ApplyModifiedProperties();
    }

    private static void WirarPanelVisionBT(GameObject panel, GameObject plantillas)
    {
        if (panel == null) return;
        var comp = panel.GetComponent<PanelVisionBT>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        string[] campos = { "textoEstadoBT","textoUltimoComando","textoFuenteActiva",
                             "textoFaseVision","textoObjetoDetectado","textoContadorDetecciones",
                             "textoNombreDispositivo","textoTimestamp","textoAdvertenciaBT" };
        string[] gos    = { "TextoEstadoBT","TextoUltimoComando","TextoFuenteActiva",
                             "TextoFaseVision","TextoObjetoDetectado","TextoContadorDetecciones",
                             "TextoNombreDispositivo","TextoTimestamp","TextoAdvertenciaBT" };
        for (int i = 0; i < campos.Length; i++)
            so.FindProperty(campos[i]).objectReferenceValue =
                panel.transform.Find(gos[i])?.GetComponent<TextMeshProUGUI>();

        so.FindProperty("imagenIndicadorBT").objectReferenceValue =
            panel.transform.Find("ImagenIndicadorBT")?.GetComponent<Image>();
        so.FindProperty("imagenFuenteSerial").objectReferenceValue =
            panel.transform.Find("ImagenFuenteSerial")?.GetComponent<Image>();
        so.FindProperty("imagenFuenteBT").objectReferenceValue =
            panel.transform.Find("ImagenFuenteBT")?.GetComponent<Image>();
        so.FindProperty("imagenEstadoAgarre").objectReferenceValue =
            panel.transform.Find("ImagenEstadoAgarre")?.GetComponent<Image>();

        var scrollDet = panel.transform.Find("ScrollDetecciones");
        so.FindProperty("scrollDetecciones").objectReferenceValue =
            scrollDet?.GetComponent<ScrollRect>();
        so.FindProperty("contenedorDetecciones").objectReferenceValue =
            scrollDet?.Find("Viewport/Contenido");
        so.FindProperty("prefabItemDeteccion").objectReferenceValue =
            plantillas.transform.Find("Plantilla_ItemDeteccion")?.gameObject;

        so.ApplyModifiedProperties();
    }

    private static void WirarCamara()
    {
        CamaraOrbital camOrbital = Object.FindObjectOfType<CamaraOrbital>();
        if (camOrbital == null) return;

        // Buscar la base del robot como punto de pivote
        Transform pivote = BuscarEnEscena("SCARA_ROBOT") ?? BuscarEnEscena("Base");
        if (pivote == null) return;

        var so = new SerializedObject(camOrbital);
        so.FindProperty("puntoObjetivo").objectReferenceValue = pivote;
        so.ApplyModifiedProperties();
    }

    // =========================================================================
    // HELPERS DE CREACIÓN DE UI
    // =========================================================================

    private static GameObject CrearGOVacio(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CrearPanelAnclado(string nombre, Transform padre,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;
        go.AddComponent<Image>().color = ColoresUI.FondoPrincipal;
        return go;
    }

    private static void AplicarColor(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
    }

    private static GameObject CrearTexto(string nombre, Transform padre,
        string texto, int fontSize, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 24);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Left;
        return go;
    }

    private static GameObject CrearTextoSimple(string nombre, Transform padre,
        string texto, int fontSize, Color color)
        => CrearTexto(nombre, padre, texto, fontSize, color);

    private static GameObject CrearBoton(string nombre, Transform padre, string etiqueta)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 32);

        Image img = go.AddComponent<Image>();
        img.color = ColoresUI.FondoBoton;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = ColoresUI.FondoBoton;
        cb.highlightedColor = ColoresUI.FondoBotonHover;
        cb.pressedColor     = ColoresUI.FondoBotonPress;
        cb.selectedColor    = ColoresUI.FondoBoton;
        btn.colors = cb;

        // Texto del botón
        GameObject txtGO = CrearTexto("Texto", go.transform, etiqueta, 13, ColoresUI.TextoPrincipal);
        RectTransform rtTxt = txtGO.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;
        txtGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static GameObject CrearDropdown(string nombre, Transform padre)
    {
        // Raíz
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rtRoot = go.AddComponent<RectTransform>();
        rtRoot.sizeDelta = new Vector2(160, 36);
        go.AddComponent<Image>().color = ColoresUI.FondoBoton;
        TMP_Dropdown dd = go.AddComponent<TMP_Dropdown>();

        // Caption (texto del valor seleccionado)
        GameObject captionGO = new GameObject("Label");
        captionGO.transform.SetParent(go.transform, false);
        RectTransform rtCap = captionGO.AddComponent<RectTransform>();
        rtCap.anchorMin        = Vector2.zero;
        rtCap.anchorMax        = Vector2.one;
        rtCap.offsetMin        = new Vector2(10, 2);
        rtCap.offsetMax        = new Vector2(-30, -2);
        TextMeshProUGUI captionTMP = captionGO.AddComponent<TextMeshProUGUI>();
        captionTMP.text      = "COM3";
        captionTMP.fontSize  = 13;
        captionTMP.color     = ColoresUI.TextoPrincipal;
        captionTMP.alignment = TextAlignmentOptions.Left;

        // Flecha
        GameObject arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        RectTransform rtArrow = arrowGO.AddComponent<RectTransform>();
        rtArrow.anchorMin        = new Vector2(1, 0.5f);
        rtArrow.anchorMax        = new Vector2(1, 0.5f);
        rtArrow.pivot            = new Vector2(1, 0.5f);
        rtArrow.anchoredPosition = new Vector2(-5, 0);
        rtArrow.sizeDelta        = new Vector2(16, 16);
        arrowGO.AddComponent<Image>().color = ColoresUI.TextoPrincipal;

        // Template (desactivado — TMP_Dropdown lo activa al abrir)
        GameObject templateGO = new GameObject("Template");
        templateGO.transform.SetParent(go.transform, false);
        RectTransform rtTpl = templateGO.AddComponent<RectTransform>();
        rtTpl.anchorMin        = new Vector2(0, 0);
        rtTpl.anchorMax        = new Vector2(1, 0);
        rtTpl.pivot            = new Vector2(0.5f, 1);
        rtTpl.anchoredPosition = new Vector2(0, 2);
        rtTpl.sizeDelta        = new Vector2(0, 150);
        templateGO.AddComponent<Image>().color = ColoresUI.FondoPrincipal;
        ScrollRect sr = templateGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        templateGO.AddComponent<Canvas>();
        templateGO.AddComponent<GraphicRaycaster>();

        // Viewport dentro de Template
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(templateGO.transform, false);
        RectTransform rtVP = viewportGO.AddComponent<RectTransform>();
        rtVP.anchorMin = Vector2.zero;
        rtVP.anchorMax = Vector2.one;
        rtVP.offsetMin = new Vector2(0, 0);
        rtVP.offsetMax = new Vector2(-18, 0);
        // Color.white (alpha=1) es obligatorio para que Mask escriba el stencil.
        // showMaskGraphic=false lo hace invisible visualmente pero funcional.
        viewportGO.AddComponent<Image>().color = Color.white;
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;

        // Content dentro de Viewport
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform rtContent = contentGO.AddComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0, 1);
        rtContent.anchorMax = new Vector2(1, 1);
        rtContent.pivot     = new Vector2(0.5f, 1);
        rtContent.sizeDelta = new Vector2(0, 28);
        sr.viewport = rtVP;
        sr.content  = rtContent;

        // Item (Toggle) dentro de Content
        GameObject itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO.transform, false);
        RectTransform rtItem = itemGO.AddComponent<RectTransform>();
        rtItem.anchorMin = new Vector2(0, 0.5f);
        rtItem.anchorMax = new Vector2(1, 0.5f);
        rtItem.sizeDelta = new Vector2(0, 28);
        Toggle toggle = itemGO.AddComponent<Toggle>();

        // Item Background
        GameObject bgGO = new GameObject("Item Background");
        bgGO.transform.SetParent(itemGO.transform, false);
        RectTransform rtBg = bgGO.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero;
        rtBg.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = ColoresUI.FondoBoton;
        toggle.targetGraphic = bgImg;

        // Item Checkmark
        GameObject checkGO = new GameObject("Item Checkmark");
        checkGO.transform.SetParent(itemGO.transform, false);
        RectTransform rtCheck = checkGO.AddComponent<RectTransform>();
        rtCheck.anchorMin        = new Vector2(0, 0.5f);
        rtCheck.anchorMax        = new Vector2(0, 0.5f);
        rtCheck.sizeDelta        = new Vector2(16, 16);
        rtCheck.anchoredPosition = new Vector2(10, 0);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = ColoresUI.AcentoAmarillo;
        toggle.graphic = checkImg;

        // Item Label
        GameObject itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        RectTransform rtItemLabel = itemLabelGO.AddComponent<RectTransform>();
        rtItemLabel.anchorMin = Vector2.zero;
        rtItemLabel.anchorMax = Vector2.one;
        rtItemLabel.offsetMin = new Vector2(22, 2);
        rtItemLabel.offsetMax = new Vector2(-5, -2);
        TextMeshProUGUI itemTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemTMP.text      = "Opción";
        itemTMP.fontSize  = 13;
        itemTMP.color     = ColoresUI.TextoPrincipal;
        itemTMP.alignment = TextAlignmentOptions.Left;
        // Asignar todas las referencias internas via SerializedObject
        // (asignación directa no se serializa en modo Editor)
        SerializedObject soDd = new SerializedObject(dd);
        soDd.FindProperty("m_CaptionText").objectReferenceValue    = captionTMP;
        soDd.FindProperty("m_ItemText").objectReferenceValue       = itemTMP;
        soDd.FindProperty("m_Template").objectReferenceValue       = rtTpl;
        soDd.ApplyModifiedProperties();

        templateGO.SetActive(false);

        return go;
    }

    private static GameObject CrearToggle(string nombre, Transform padre,
        string etiqueta, bool valor)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(50, 28);

        Toggle tog = go.AddComponent<Toggle>();
        tog.isOn = valor;

        GameObject bg = CrearGOConImagen("Background", go.transform,
            ColoresUI.FondoBoton, new Vector2(20, 20));
        GameObject checkmark = CrearGOConImagen("Checkmark", bg.transform,
            ColoresUI.AcentoAmarillo, new Vector2(14, 14));
        EstirarEnPadre(checkmark);
        tog.graphic = checkmark.GetComponent<Image>();
        tog.targetGraphic = bg.GetComponent<Image>();

        GameObject label = CrearTexto("Label", go.transform, etiqueta, 13, ColoresUI.TextoPrincipal);
        PosicionarAbsoluto(label, new Vector2(22, 0));

        return go;
    }

    private static GameObject CrearGOConImagen(string nombre, Transform padre,
        Color color, Vector2 tamaño)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = tamaño;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject CrearScrollRect(string nombre, Transform padre,
        Vector2 posicion, Vector2 tamaño)
    {
        // Contenedor ScrollRect
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta        = tamaño;
        rt.anchoredPosition = posicion;
        go.AddComponent<Image>().color = ColoresUI.BarraFondo;

        ScrollRect sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(go.transform, false);
        RectTransform rtVP = viewport.AddComponent<RectTransform>();
        rtVP.anchorMin = Vector2.zero;
        rtVP.anchorMax = Vector2.one;
        rtVP.offsetMin = Vector2.zero;
        rtVP.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Contenido
        GameObject contenido = new GameObject("Contenido");
        contenido.transform.SetParent(viewport.transform, false);
        RectTransform rtC = contenido.AddComponent<RectTransform>();
        rtC.anchorMin = new Vector2(0, 1);
        rtC.anchorMax = new Vector2(1, 1);
        rtC.pivot     = new Vector2(0.5f, 1);
        rtC.offsetMin = Vector2.zero;
        rtC.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contenido.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing  = 2f;
        vlg.padding  = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter csf = contenido.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport    = rtVP;
        sr.content     = rtC;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return go;
    }

    private static void CrearEtiquetaSeccion(string titulo, Transform padre, float topOffset)
    {
        GameObject go = CrearTexto("Etiqueta_" + titulo.Replace(" ", "_"),
            padre, titulo.ToUpper(), 11, ColoresUI.AcentoAmarillo);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0, 1);
        rt.anchorMax  = new Vector2(1, 1);
        rt.offsetMin  = new Vector2(8, -topOffset - 18);
        rt.offsetMax  = new Vector2(-8, -topOffset);
        go.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
    }

    private static void CrearSeparador(Transform padre, float yPos)
    {
        GameObject sep = CrearGOConImagen("Separador", padre,
            ColoresUI.BordePanelInactivo, new Vector2(0, 1));
        RectTransform rt = sep.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(8, yPos - 1);
        rt.offsetMax = new Vector2(-8, yPos);
    }

    private static float CrearItemMotor(Transform padre, string nombreMotor,
        string nombreCampoTexto, string nombreCampoSlider,
        float yOffset, float min, float max)
    {
        // Etiqueta del motor
        GameObject etq = CrearTexto("Etq_" + nombreCampoTexto, padre,
            nombreMotor, 11, ColoresUI.TextoSecundario);
        PosicionarAbsoluto(etq, new Vector2(10, yOffset));
        yOffset -= 18f;

        // Texto del valor
        GameObject txt = CrearTexto(nombreCampoTexto, padre,
            "—", 14, ColoresUI.TextoPrincipal);
        PosicionarAbsoluto(txt, new Vector2(10, yOffset));
        yOffset -= 20f;

        // Slider de progreso
        GameObject sliderGO = new GameObject(nombreCampoSlider);
        sliderGO.transform.SetParent(padre, false);
        RectTransform rtS = sliderGO.AddComponent<RectTransform>();
        rtS.anchorMin = new Vector2(0, 1);
        rtS.anchorMax = new Vector2(1, 1);
        rtS.offsetMin = new Vector2(10, yOffset - 8);
        rtS.offsetMax = new Vector2(-10, yOffset);

        Slider sl = sliderGO.AddComponent<Slider>();
        sl.minValue = min;
        sl.maxValue = max;
        sl.value    = 0;
        sl.interactable = false;

        // Fondo del slider
        GameObject fondoSlider = CrearGOConImagen("Background", sliderGO.transform,
            ColoresUI.BarraFondo, Vector2.zero);
        EstirarEnPadre(fondoSlider);

        // Área de relleno
        GameObject fillArea = CrearGOVacio("Fill Area", sliderGO.transform);
        EstirarEnPadre(fillArea);
        GameObject fill = CrearGOConImagen("Fill", fillArea.transform,
            ColoresUI.BarraRelleno, Vector2.zero);
        EstirarEnPadre(fill);
        sl.fillRect = fill.GetComponent<RectTransform>();

        yOffset -= 16f;
        return yOffset;
    }

    // ─── HELPERS DE POSICIONAMIENTO ───────────────────────────────────────────

    private static void PosicionarAnclado(GameObject go,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 posicion, Vector2 tamaño)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = posicion;
        rt.sizeDelta        = tamaño;
    }

    private static void PosicionarAbsoluto(GameObject go, Vector2 posicion)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = posicion;
    }

    private static void EstirarEnPadre(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EstirarConOffset(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // ─── BÚSQUEDA EN ESCENA ───────────────────────────────────────────────────

    /// <summary>
    /// Busca un Transform por nombre en toda la escena.
    /// Solo se usa en el Editor script, nunca en runtime.
    /// </summary>
    private static Transform BuscarEnEscena(string nombreGO)
    {
        GameObject go = GameObject.Find(nombreGO);
        if (go == null)
            Debug.LogWarning($"[SCARA] No se encontró '{nombreGO}' en la escena. " +
                             "Asigna la referencia manualmente en el Inspector.");
        return go?.transform;
    }

    // =========================================================================
    // ESCENA M2 — TRAYECTORIAS
    // =========================================================================

    [MenuItem("SCARA/Construir Escena M2 — Trayectorias")]
    public static void ConstruirEscenaM2()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Construir Escena M2",
            "Agrega los componentes de Trayectorias (M2) sobre la escena M1 existente.\n\n" +
            "Asegúrate de que la escena M1 ya está construida.",
            "Construir M2", "Cancelar");
        if (!ok) return;

        GameObject gestorGO = GameObject.Find("GestorPrincipal") ?? new GameObject("GestorPrincipal");
        Undo.RegisterCreatedObjectUndo(gestorGO, "GestorPrincipal M2");

        if (gestorGO.GetComponent<GestorTrayectorias>() == null)
            gestorGO.AddComponent<GestorTrayectorias>();
        if (gestorGO.GetComponent<SimuladorSerial>() == null)
            gestorGO.AddComponent<SimuladorSerial>();

        // LineRenderer para trayectoria
        GameObject vizGO = new GameObject("VisualizadorTrayectoria");
        Undo.RegisterCreatedObjectUndo(vizGO, "VisualizadorTrayectoria");
        LineRenderer lr = vizGO.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.02f;
        lr.material   = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0f, 0.8f, 1f, 0.8f);
        lr.endColor   = new Color(1f, 1f, 0f, 0.8f);
        vizGO.AddComponent<VisualizadorTrayectoria>();

        // Zona de recogida (marcador visual)
        GameObject zonaRec = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zonaRec.name = "ZonaRecogida";
        zonaRec.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
        zonaRec.transform.position   = new Vector3(2f, -0.79f, 0f);
        zonaRec.GetComponent<Renderer>().material.color = new Color(0f, 1f, 0.3f, 0.5f);
        Undo.RegisterCreatedObjectUndo(zonaRec, "ZonaRecogida");

        // Zona de depósito
        GameObject zonaDep = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zonaDep.name = "ZonaDestino";
        zonaDep.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
        zonaDep.transform.position   = new Vector3(-1.5f, -0.79f, 1.5f);
        zonaDep.GetComponent<Renderer>().material.color = new Color(1f, 0.6f, 0f, 0.5f);
        Undo.RegisterCreatedObjectUndo(zonaDep, "ZonaDestino");

        // Objeto manipulable de ejemplo
        GameObject obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj1.name = "ObjetoManipulable_1";
        obj1.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        obj1.transform.position   = new Vector3(2f, -0.74f, 0f);
        if (obj1.GetComponent<ObjetoManipulable>() == null) obj1.AddComponent<ObjetoManipulable>();
        if (obj1.GetComponent<Rigidbody>() == null) { var rb = obj1.AddComponent<Rigidbody>(); rb.mass = 0.1f; }
        obj1.GetComponent<Renderer>().material.color = new Color(0.9f, 0.3f, 0.1f);
        Undo.RegisterCreatedObjectUndo(obj1, "ObjetoManipulable_1");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA] Escena M2 construida.");
        EditorUtility.DisplayDialog("M2 listo",
            "Componentes M2 agregados.\n\nAsigna en el Inspector de GestorPrincipal:\n" +
            "• GestorTrayectorias → gestorSCARA, panelTrayectorias, visualizador, objetos[]",
            "Aceptar");
    }

    // =========================================================================
    // ESCENA M3 — VISIÓN BT
    // =========================================================================

    [MenuItem("SCARA/Construir Escena M3 — Visión BT")]
    public static void ConstruirEscenaM3()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Construir Escena M3",
            "Agrega los componentes de Visión BT (M3) sobre la escena M2.",
            "Construir M3", "Cancelar");
        if (!ok) return;

        GameObject gestorGO = GameObject.Find("GestorPrincipal") ?? new GameObject("GestorPrincipal");
        Undo.RegisterCreatedObjectUndo(gestorGO, "GestorPrincipal M3");

        if (gestorGO.GetComponent<ReceptorVisionTCP>() == null)
            gestorGO.AddComponent<ReceptorVisionTCP>();
        if (gestorGO.GetComponent<GestorVision>() == null)
            gestorGO.AddComponent<GestorVision>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA] Escena M3 construida.");
        EditorUtility.DisplayDialog("M3 listo",
            "Componentes M3 agregados.\n\nAsigna en el Inspector:\n" +
            "• GestorVision → gestorSCARA, gestorTrayectorias, receptorTCP, panelVision\n\n" +
            "Graba trayectorias 'PICK' y 'DROP' en Módulo 2 antes de usar M3.",
            "Aceptar");
    }

    // =========================================================================
    // RECONSTRUIR PANEL VISIÓN BT — aplica cambios de UI sin tocar GestorPrincipal
    // =========================================================================

    [MenuItem("SCARA/Reconstruir Panel Visión BT (M3)")]
    public static void ReconstruirPanelVisionBT()
    {
        Transform panelVisionTr = GameObject.Find("PanelDerecho")
            ?.transform.Find("PanelVisionBT");

        if (panelVisionTr == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró PanelVisionBT en la escena.\n" +
                "Ejecuta primero SCARA → Construir Escena Completa.",
                "Aceptar");
            return;
        }

        GameObject panel = panelVisionTr.gameObject;
        GameObject plantillasGO = GameObject.Find("Plantillas");

        if (plantillasGO == null)
        {
            plantillasGO = new GameObject("Plantillas");
            plantillasGO.SetActive(false);
            Undo.RegisterCreatedObjectUndo(plantillasGO, "Plantillas");
        }

        // Eliminar hijos anteriores para evitar duplicados
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(panel.transform.GetChild(i).gameObject);

        PopularPanelVisionBT(panel);
        WirarPanelVisionBT(panel, plantillasGO);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA] PanelVisionBT reconstruido. Guarda la escena (Ctrl+S).");
        EditorUtility.DisplayDialog("Listo",
            "PanelVisionBT reconstruido con los nuevos campos BT.\n\n" +
            "Guarda la escena (Ctrl+S).",
            "Aceptar");
    }
}
