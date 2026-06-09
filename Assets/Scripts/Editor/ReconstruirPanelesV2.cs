using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Actualiza los paneles que cambiaron en la v2 de lógica (sin IK, basada en pasos).
/// Ejecutar UNA VEZ sobre una escena ya construida con ConstruirEscenaCompleta.
///
/// USO: Menú Unity → SCARA → Reconstruir Paneles v2
///
/// Hace:
///  1. Limpia y reconstruye PanelControlArticular (pasos/dir/vel por motor)
///  2. Limpia y reconstruye PanelTrayectorias     (botones de pasos + lista de puntos)
///  3. Agrega sección Pasos180 al PanelEstadoRobot
///  4. Cablea transformTCP en GestorSCARA
///  5. Crea prefab de ítem de punto en Plantillas
///  6. Cablea todos los campos y botones vía SerializedObject
/// </summary>
public static class ReconstruirPanelesV2
{
    // ── Constantes de layout ─────────────────────────────────────────────────
    private const float AltoFila    = 28f;
    private const float AltoBoton   = 30f;
    private const float AltoInput   = 28f;
    private const float MargenX     = 8f;
    private const float FontNormal  = 14f;
    private const float FontTitulo  = 15f;
    private const float FontBoton   = 13f;

    // =========================================================================

    [MenuItem("SCARA/Reconstruir Paneles v2 — Aplicar cambios de lógica")]
    public static void ReconstruirTodo()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Reconstruir Paneles v2",
            "Este proceso:\n" +
            "• Borra y recrea el contenido de PanelControlArticular\n" +
            "• Borra y recrea el contenido de PanelTrayectorias\n" +
            "• Agrega campos de reducción al PanelEstadoRobot\n" +
            "• Cablea transformTCP en GestorSCARA\n\n" +
            "Guarda la escena antes de continuar.\n¿Continuar?",
            "Reconstruir", "Cancelar");
        if (!ok) return;

        // Localizar GameObjects clave
        GameObject gestorGO  = GameObject.Find("GestorPrincipal");
        GameObject panelDer  = GameObject.Find("PanelDerecho");
        GameObject panelIzq  = GameObject.Find("PanelIzquierdo");
        GameObject plantillas = GameObject.Find("Plantillas");

        if (gestorGO == null || panelDer == null || panelIzq == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró GestorPrincipal, PanelDerecho o PanelIzquierdo.\n" +
                "Ejecuta primero SCARA → Construir Escena Completa.",
                "Aceptar");
            return;
        }

        if (plantillas == null)
        {
            plantillas = new GameObject("Plantillas");
            plantillas.SetActive(false);
            Undo.RegisterCreatedObjectUndo(plantillas, "Plantillas");
        }

        // ── 1. PanelControlArticular ─────────────────────────────────────────
        GameObject panelM1 = panelDer.transform.Find("PanelControlArticular")?.gameObject;
        if (panelM1 != null)
        {
            LimpiarHijos(panelM1);
            PopularPanelControlArticular(panelM1);
            WirarPanelControlArticular(panelM1);
        }
        else Debug.LogWarning("[v2] PanelControlArticular no encontrado en PanelDerecho.");

        // ── 2. PanelTrayectorias ─────────────────────────────────────────────
        GameObject panelM2 = panelDer.transform.Find("PanelTrayectorias")?.gameObject;
        if (panelM2 != null)
        {
            LimpiarHijos(panelM2);
            GameObject prefabPunto = CrearPrefabItemPunto(plantillas.transform);
            PopularPanelTrayectorias(panelM2);
            WirarPanelTrayectorias(panelM2, gestorGO, prefabPunto);
        }
        else Debug.LogWarning("[v2] PanelTrayectorias no encontrado en PanelDerecho.");

        // ── 3. PanelEstadoRobot — agregar sección Reducción ──────────────────
        AsegurarScrollEnPanelIzq(panelIzq);
        AgregarSeccionReduccion(panelIzq);
        WirarSeccionReduccion(panelIzq);

        // ── 4. GestorSCARA — wiring de transformTCP ──────────────────────────
        WirarTransformTCP(gestorGO);

        // ── 5. Guardar ────────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA v2] Paneles reconstruidos correctamente. Guarda la escena (Ctrl+S).");
        EditorUtility.DisplayDialog("Listo",
            "Paneles reconstruidos y cableados.\n\n" +
            "Recuerda:\n" +
            "1. SCARA → Crear Assets de Motores (sobreescribir los 5)\n" +
            "2. Asignar los 5 assets al GestorPrincipal\n" +
            "3. Guardar la escena (Ctrl+S)",
            "Aceptar");
    }

    // =========================================================================
    // PANEL CONTROL ARTICULAR — Contenido nuevo (pasos / dir / vel)
    // =========================================================================

    private static void PopularPanelControlArticular(GameObject panel)
    {
        float y = -10f;

        // Título
        CrearTexto("TituloM1", panel.transform, "CONTROL ARTICULAR — M1",
            FontTitulo, ColoresUI.AcentoAmarillo, y, AltoFila, bold: true);
        y -= AltoFila + 4f;

        CrearSeparadorH(panel.transform, y); y -= 6f;

        // M1 — Eje Z
        CrearTexto("EtqM1", panel.transform, "M1 — Eje Z",
            FontNormal, ColoresUI.TextoPrincipal, y, AltoFila, bold: true);
        y -= AltoFila;
        CrearTexto("TextoM1_Pasos",     panel.transform, "Pasos M1: 0",   FontNormal, ColoresUI.AcentoAmarillo, y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM1_Direccion", panel.transform, "DIR: —",        FontNormal, ColoresUI.TextoPrincipal,  y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM1_Velocidad", panel.transform, "Vel: 0 p/s",    FontNormal, ColoresUI.TextoSecundario, y, AltoFila); y -= AltoFila + 4f;

        CrearSeparadorH(panel.transform, y); y -= 6f;

        // M2 — Brazo 1
        CrearTexto("EtqM2", panel.transform, "M2 — Brazo 1 (J1)",
            FontNormal, ColoresUI.TextoPrincipal, y, AltoFila, bold: true);
        y -= AltoFila;
        CrearTexto("TextoM2_Pasos",     panel.transform, "Pasos M2: 0",   FontNormal, ColoresUI.AcentoAmarillo, y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM2_Direccion", panel.transform, "DIR: —",        FontNormal, ColoresUI.TextoPrincipal,  y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM2_Velocidad", panel.transform, "Vel: 0 p/s",    FontNormal, ColoresUI.TextoSecundario, y, AltoFila); y -= AltoFila + 4f;

        CrearSeparadorH(panel.transform, y); y -= 6f;

        // M3 — Brazo 2
        CrearTexto("EtqM3", panel.transform, "M3 — Brazo 2 (J2)",
            FontNormal, ColoresUI.TextoPrincipal, y, AltoFila, bold: true);
        y -= AltoFila;
        CrearTexto("TextoM3_Pasos",     panel.transform, "Pasos M3: 0",   FontNormal, ColoresUI.AcentoAmarillo, y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM3_Direccion", panel.transform, "DIR: —",        FontNormal, ColoresUI.TextoPrincipal,  y, AltoFila); y -= AltoFila;
        CrearTexto("TextoM3_Velocidad", panel.transform, "Vel: 0 p/s",    FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
    }

    private static void WirarPanelControlArticular(GameObject panel)
    {
        var comp = panel.GetComponent<PanelControlArticular>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        Asignar(so, "textoM1_Pasos",     BuscarTMP(panel, "TextoM1_Pasos"));
        Asignar(so, "textoM1_Direccion", BuscarTMP(panel, "TextoM1_Direccion"));
        Asignar(so, "textoM1_Velocidad", BuscarTMP(panel, "TextoM1_Velocidad"));
        Asignar(so, "textoM2_Pasos",     BuscarTMP(panel, "TextoM2_Pasos"));
        Asignar(so, "textoM2_Direccion", BuscarTMP(panel, "TextoM2_Direccion"));
        Asignar(so, "textoM2_Velocidad", BuscarTMP(panel, "TextoM2_Velocidad"));
        Asignar(so, "textoM3_Pasos",     BuscarTMP(panel, "TextoM3_Pasos"));
        Asignar(so, "textoM3_Direccion", BuscarTMP(panel, "TextoM3_Direccion"));
        Asignar(so, "textoM3_Velocidad", BuscarTMP(panel, "TextoM3_Velocidad"));

        so.ApplyModifiedProperties();
    }

    // =========================================================================
    // PANEL TRAYECTORIAS — Contenido nuevo (pasos + puntos grabados)
    // =========================================================================

    private static void PopularPanelTrayectorias(GameObject panel)
    {
        float y = -10f;

        // Título
        CrearTexto("TituloM2", panel.transform, "TRAYECTORIAS — M2",
            FontTitulo, ColoresUI.AcentoAmarillo, y, AltoFila, bold: true);
        y -= AltoFila + 4f;
        CrearSeparadorH(panel.transform, y); y -= 8f;

        // ── MOVIMIENTO POR PASOS ──────────────────────────────────────────────
        CrearTexto("EtqMovimiento", panel.transform, "Mover robot (pasos):",
            FontNormal, ColoresUI.TextoPrincipal, y, AltoFila, bold: true);
        y -= AltoFila;

        CrearTexto("EtqPasosPorClick", panel.transform, "Pasos por click:",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearInputField("InputPasosPorClick", panel.transform, "5", y, AltoInput); y -= AltoInput + 4f;

        // Fila J1
        CrearTexto("EtqJ1", panel.transform, "J1 (Brazo 1):",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearBotonMitad("BotonJ1Menos", panel.transform, "J1 −", y, izquierda: true);
        CrearBotonMitad("BotonJ1Mas",   panel.transform, "J1 +", y, izquierda: false);
        y -= AltoBoton + 4f;

        // Fila J2
        CrearTexto("EtqJ2", panel.transform, "J2 (Brazo 2):",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearBotonMitad("BotonJ2Menos", panel.transform, "J2 −", y, izquierda: true);
        CrearBotonMitad("BotonJ2Mas",   panel.transform, "J2 +", y, izquierda: false);
        y -= AltoBoton + 4f;

        // Fila Z
        CrearTexto("EtqZ", panel.transform, "Z (Eje vertical):",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearBotonMitad("BotonZMenos", panel.transform, "Z −", y, izquierda: true);
        CrearBotonMitad("BotonZMas",   panel.transform, "Z +", y, izquierda: false);
        y -= AltoBoton + 6f;

        CrearSeparadorH(panel.transform, y); y -= 8f;

        // ── PUNTOS GRABADOS ───────────────────────────────────────────────────
        CrearTexto("EtqPuntos", panel.transform, "Puntos grabados:",
            FontNormal, ColoresUI.TextoPrincipal, bold: true, yPos: y, alto: AltoFila);
        y -= AltoFila;
        CrearTexto("TextoContadorPuntos", panel.transform, "Puntos: 0",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila + 2f;

        CrearBotonCompleto("BotonGuardarPunto", panel.transform, "Guardar punto actual", y, ColoresUI.Exito);
        y -= AltoBoton + 2f;

        // ScrollRect de puntos
        float altoScroll = 80f;
        CrearScrollRectPuntos("ScrollPuntos", panel.transform, y, altoScroll);
        y -= altoScroll + 4f;

        CrearBotonCompleto("BotonEjecutar", panel.transform, "Ejecutar trayectoria", y, ColoresUI.AcentoAmarillo);
        y -= AltoBoton + 2f;
        CrearBotonCompleto("BotonLimpiar",  panel.transform, "Limpiar puntos",        y, ColoresUI.Error);
        y -= AltoBoton + 6f;

        CrearSeparadorH(panel.transform, y); y -= 8f;

        // ── GUARDAR COMO NOMBRE ───────────────────────────────────────────────
        CrearTexto("EtqNombre", panel.transform, "Guardar secuencia como:",
            FontNormal, ColoresUI.TextoPrincipal, bold: true, yPos: y, alto: AltoFila);
        y -= AltoFila;
        CrearInputField("InputNombreTrayectoria", panel.transform, "PICK / DROP", y, AltoInput);
        y -= AltoInput + 2f;
        CrearBotonCompleto("BotonGuardarNombre", panel.transform, "Guardar nombre", y, ColoresUI.TextoPrincipal);
        y -= AltoBoton + 6f;

        CrearSeparadorH(panel.transform, y); y -= 8f;

        // ── GRIPPER EN PUNTO ──────────────────────────────────────────────────
        CrearTexto("EtqGripper", panel.transform, "Gripper en punto (índice):",
            FontNormal, ColoresUI.TextoPrincipal, bold: true, yPos: y, alto: AltoFila);
        y -= AltoFila;
        CrearTexto("EtqCerrar", panel.transform, "Cerrar en índice:",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearInputField("InputIndiceCerrar", panel.transform, "-1 (ninguno)", y, AltoInput);
        y -= AltoInput + 2f;
        CrearTexto("EtqAbrir", panel.transform, "Abrir en índice:",
            FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearInputField("InputIndiceAbrir", panel.transform, "-1 (ninguno)", y, AltoInput);
        y -= AltoInput + 6f;

        CrearSeparadorH(panel.transform, y); y -= 8f;

        // ── ESTADO ────────────────────────────────────────────────────────────
        CrearTexto("TextoFaseActual", panel.transform, "Fase: En espera",
            FontNormal, ColoresUI.AcentoAmarillo, y, AltoFila);
        y -= AltoFila;

        GameObject imgIndGO = CrearGOImagen("ImagenIndicadorTrayectoria", panel.transform,
            ColoresUI.Exito, new Vector2(14, 14), new Vector2(MargenX, y - 14));
        y -= 20f;

        CrearTexto("TextoEstadoGripper", panel.transform, "ABIERTO",
            FontNormal, ColoresUI.Advertencia, y, AltoFila);
        y -= AltoFila;

        CrearGOImagen("ImagenEstadoGripper", panel.transform,
            ColoresUI.TextoSecundario, new Vector2(12, 12), new Vector2(MargenX, y - 12));

        CrearTexto("TextoExitos",      panel.transform, "Éxitos: 0", FontNormal, ColoresUI.Exito,         y, AltoFila); y -= AltoFila;
        CrearTexto("TextoFallos",      panel.transform, "Fallos: 0", FontNormal, ColoresUI.Error,          y, AltoFila); y -= AltoFila;
        CrearTexto("TextoTotalCiclos", panel.transform, "Total:  0", FontNormal, ColoresUI.TextoPrincipal, y, AltoFila); y -= AltoFila + 4f;

        CrearBotonCompleto("BotonReiniciarObjetos", panel.transform, "Reiniciar objetos", y, ColoresUI.TextoSecundario);
    }

    private static void WirarPanelTrayectorias(GameObject panel, GameObject gestorGO, GameObject prefabPunto)
    {
        var comp = panel.GetComponent<PanelTrayectorias>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        // GestorSCARA
        so.FindProperty("gestorSCARA").objectReferenceValue =
            gestorGO.GetComponent<GestorSCARA>();

        // InputFields
        Asignar(so, "inputPasosPorClick",       BuscarInput(panel, "InputPasosPorClick"));
        Asignar(so, "inputNombreTrayectoria",   BuscarInput(panel, "InputNombreTrayectoria"));
        Asignar(so, "inputIndiceCerrar",        BuscarInput(panel, "InputIndiceCerrar"));
        Asignar(so, "inputIndiceAbrir",         BuscarInput(panel, "InputIndiceAbrir"));

        // Textos
        Asignar(so, "textoContadorPuntos",      BuscarTMP(panel, "TextoContadorPuntos"));
        Asignar(so, "textoFaseActual",          BuscarTMP(panel, "TextoFaseActual"));
        Asignar(so, "textoEstadoGripper",       BuscarTMP(panel, "TextoEstadoGripper"));
        Asignar(so, "textoExitos",              BuscarTMP(panel, "TextoExitos"));
        Asignar(so, "textoFallos",              BuscarTMP(panel, "TextoFallos"));
        Asignar(so, "textoTotalCiclos",         BuscarTMP(panel, "TextoTotalCiclos"));

        // Imágenes
        Asignar(so, "imagenIndicadorTrayectoria", BuscarImagen(panel, "ImagenIndicadorTrayectoria"));
        Asignar(so, "imagenEstadoGripper",        BuscarImagen(panel, "ImagenEstadoGripper"));

        // ScrollRect y contenedor
        var scrollGO = panel.transform.Find("ScrollPuntos");
        if (scrollGO != null)
        {
            so.FindProperty("scrollPuntos").objectReferenceValue =
                scrollGO.GetComponent<ScrollRect>();
            so.FindProperty("contenedorPuntos").objectReferenceValue =
                scrollGO.Find("Viewport/ContenedorPuntos");
        }

        // Prefab ítem punto
        so.FindProperty("prefabItemPunto").objectReferenceValue = prefabPunto;

        // Botones con onCLick
        Button btnGuardar  = BuscarBoton(panel, "BotonGuardarPunto");
        Button btnEjecutar = BuscarBoton(panel, "BotonEjecutar");
        Button btnLimpiar  = BuscarBoton(panel, "BotonLimpiar");
        Button btnNombre   = BuscarBoton(panel, "BotonGuardarNombre");
        Button btnReinObj  = BuscarBoton(panel, "BotonReiniciarObjetos");

        // Botones de movimiento por pasos (onClick → métodos del panel)
        Button btnJ1M  = BuscarBoton(panel, "BotonJ1Menos");
        Button btnJ1P  = BuscarBoton(panel, "BotonJ1Mas");
        Button btnJ2M  = BuscarBoton(panel, "BotonJ2Menos");
        Button btnJ2P  = BuscarBoton(panel, "BotonJ2Mas");
        Button btnZM   = BuscarBoton(panel, "BotonZMenos");
        Button btnZP   = BuscarBoton(panel, "BotonZMas");

        Asignar(so, "botonGuardarPunto",     btnGuardar);
        Asignar(so, "botonEjecutar",         btnEjecutar);
        Asignar(so, "botonLimpiar",          btnLimpiar);
        Asignar(so, "botonGuardarNombre",    btnNombre);
        Asignar(so, "botonReiniciarObjetos", btnReinObj);

        // Desactivar botonEjecutar inicialmente
        if (btnEjecutar != null)
        {
            var soBE = new SerializedObject(btnEjecutar);
            soBE.FindProperty("m_Interactable").boolValue = false;
            soBE.ApplyModifiedProperties();
        }

        so.ApplyModifiedProperties();

        // Cablear onClicks de botones al componente PanelTrayectorias
        AsignarClick(btnGuardar,  comp, "AlPresionarGuardarPunto");
        AsignarClick(btnEjecutar, comp, "AlPresionarEjecutar");
        AsignarClick(btnLimpiar,  comp, "AlPresionarLimpiar");
        AsignarClick(btnNombre,   comp, "AlPresionarGuardarNombre");
        AsignarClick(btnReinObj,  comp, "AlPresionarReiniciarObjetos");
        AsignarClick(btnJ1M,      comp, "J1Menos");
        AsignarClick(btnJ1P,      comp, "J1Mas");
        AsignarClick(btnJ2M,      comp, "J2Menos");
        AsignarClick(btnJ2P,      comp, "J2Mas");
        AsignarClick(btnZM,       comp, "ZMenos");
        AsignarClick(btnZP,       comp, "ZMas");

        // Cablear onEndEdit de InputFields al componente
        AsignarEndEdit(BuscarInput(panel, "InputIndiceCerrar"), comp, "AlCambiarIndiceCerrar");
        AsignarEndEdit(BuscarInput(panel, "InputIndiceAbrir"),  comp, "AlCambiarIndiceAbrir");
    }

    // =========================================================================
    // PANEL ESTADO ROBOT — Sección Reducción con Toggles (1:1 / 1:2 / 1:3 / 1:4)
    // =========================================================================

    private static readonly string[] _ratioLabels  = { "1:1", "1:2", "1:3", "1:4" };
    private static readonly float[]  _ratioValores  = { 100f, 200f, 300f, 400f };

    // Extiende el panel izquierdo hacia abajo si el contenido nuevo no entraría
    private static void AsegurarScrollEnPanelIzq(GameObject panelIzq)
    {
        var rt = panelIzq.GetComponent<RectTransform>();
        if (rt == null) return;

        // La sección de reducción ocupa ~220px (títulos + 3 grupos de toggles)
        const float alturaExtra = 220f;
        float alturaActual = Mathf.Abs(rt.offsetMin.y - rt.offsetMax.y);

        // Si el panel mide menos de 900px, extender el borde inferior
        if (alturaActual < 900f)
        {
            var so = new SerializedObject(rt);
            var offsetMin = so.FindProperty("m_AnchoredPosition");  // no aplica directo
            // Ajustar el offset inferior para hacer el panel más alto
            Vector2 oMin = rt.offsetMin;
            oMin.y -= alturaExtra;
            rt.offsetMin = oMin;
            Debug.Log($"[v2] PanelIzquierdo extendido {alturaExtra}px hacia abajo para mostrar reducción.");
        }
    }

    private static void AgregarSeccionReduccion(GameObject panelIzq)
    {
        // Eliminar versión anterior (InputFields o Toggles) para recrear limpio
        EliminarSeccionReduccionAntigua(panelIzq);

        // Calcular Y bajo el último hijo directo del panel
        float y = CalcularYFinalPanel(panelIzq) - 8f;

        CrearSeparadorH(panelIzq.transform, y); y -= 8f;

        CrearTexto("EtqReduccion", panelIzq.transform,
            "REDUCCIÓN — RELACIÓN DE TRANSMISIÓN",
            FontTitulo, ColoresUI.AcentoAmarillo, y, AltoFila, bold: true);
        y -= AltoFila + 4f;

        // Fila J1
        CrearTexto("EtqReducJ1", panelIzq.transform,
            "M2 Brazo 1 (J1)", FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearFilaToggles("GrupoJ1", panelIzq.transform, y, "J1"); y -= 32f + 4f;

        // Fila J2
        CrearTexto("EtqReducJ2", panelIzq.transform,
            "M3 Brazo 2 (J2)", FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearFilaToggles("GrupoJ2", panelIzq.transform, y, "J2"); y -= 32f + 4f;

        // Fila Z
        CrearTexto("EtqReducZ", panelIzq.transform,
            "M1 Eje Z", FontNormal, ColoresUI.TextoSecundario, y, AltoFila);
        y -= AltoFila;
        CrearFilaToggles("GrupoZ", panelIzq.transform, y, "Z");
    }

    // Elimina hijos del panel que pertenezcan a la sección de reducción anterior
    private static void EliminarSeccionReduccionAntigua(GameObject panel)
    {
        var aEliminar = new System.Collections.Generic.List<GameObject>();
        foreach (Transform hijo in panel.transform)
        {
            string n = hijo.name;
            if (n == "EtqReduccion" || n == "EtqReducJ1" || n == "EtqReducJ2" ||
                n == "EtqReducZ"    || n == "GrupoJ1"    || n == "GrupoJ2"    ||
                n == "GrupoZ"       || n == "EtqLabelJ1" || n == "EtqLabelJ2" ||
                n == "InputPasos180_J1" || n == "InputPasos180_J2")
                aEliminar.Add(hijo.gameObject);
        }
        foreach (var go in aEliminar)
            Undo.DestroyObjectImmediate(go);
    }

    // Devuelve la Y más baja (más negativa) entre los hijos directos del panel
    private static float CalcularYFinalPanel(GameObject panel)
    {
        float minY = -30f;
        foreach (Transform hijo in panel.transform)
        {
            var rt = hijo.GetComponent<RectTransform>();
            if (rt == null) continue;
            // anchoredPosition.y es la esquina superior del hijo (pivot top)
            float base_ = rt.anchoredPosition.y;
            float fondo = base_ - Mathf.Abs(rt.sizeDelta.y);
            if (fondo < minY) minY = fondo;
        }
        return minY;
    }

    // Crea una fila de 4 Toggles agrupados (1:1 / 1:2 / 1:3 / 1:4)
    private static void CrearFilaToggles(string nombreGrupo, Transform padre,
        float yPos, string motor)
    {
        // Contenedor con ToggleGroup
        GameObject cont = new GameObject(nombreGrupo);
        cont.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(cont, nombreGrupo);

        RectTransform rt = cont.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX, yPos - 32f);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        ToggleGroup tg = cont.AddComponent<ToggleGroup>();
        tg.allowSwitchOff = false;

        float ancho = 1f / _ratioLabels.Length;
        for (int i = 0; i < _ratioLabels.Length; i++)
        {
            string nombre = $"Toggle{motor}_{_ratioLabels[i].Replace(":", "a")}";
            CrearToggleEnFila(nombre, cont.transform, _ratioLabels[i],
                new Vector2(ancho * i, 0),
                new Vector2(ancho * (i + 1), 1),
                tg, i == 0);   // default: índice 0 = 1:1
        }
    }

    private static void CrearToggleEnFila(string nombre, Transform padre, string etiqueta,
        Vector2 ancMin, Vector2 ancMax, ToggleGroup grupo, bool activoPorDefecto)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(2, 2);
        rt.offsetMax = new Vector2(-2, -2);

        // Fondo (Background del toggle)
        Image bg = go.AddComponent<Image>();
        bg.color = activoPorDefecto
            ? ColoresUI.AcentoAmarillo
            : new Color(0.12f, 0.18f, 0.30f);

        Toggle tg = go.AddComponent<Toggle>();
        tg.targetGraphic = bg;
        tg.group         = grupo;
        tg.isOn          = activoPorDefecto;

        // Colores del toggle
        ColorBlock cb = tg.colors;
        cb.normalColor      = new Color(0.12f, 0.18f, 0.30f);
        cb.selectedColor    = ColoresUI.AcentoAmarillo;
        cb.highlightedColor = new Color(0.20f, 0.28f, 0.45f);
        cb.pressedColor     = ColoresUI.AcentoAmarillo;
        tg.colors = cb;

        // Texto de la ratio
        GameObject txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = 12f;
        tmp.color     = activoPorDefecto ? Color.black : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    private static void WirarSeccionReduccion(GameObject panelIzq)
    {
        var comp = panelIzq.GetComponent<PanelEstadoRobot>();
        if (comp == null) return;
        var so = new SerializedObject(comp);

        // Wiring de los 12 toggles (3 motores × 4 ratios)
        string[] motores  = { "J1", "J2", "Z" };
        string[] sufijos  = { "1a1", "1a2", "1a3", "1a4" };
        string[] campos   = { "toggleJ1_1a1","toggleJ1_1a2","toggleJ1_1a3","toggleJ1_1a4",
                               "toggleJ2_1a1","toggleJ2_1a2","toggleJ2_1a3","toggleJ2_1a4",
                               "toggleZ_1a1", "toggleZ_1a2", "toggleZ_1a3", "toggleZ_1a4" };
        string[] metodos  = { "SeleccionarJ1_1a1","SeleccionarJ1_1a2","SeleccionarJ1_1a3","SeleccionarJ1_1a4",
                               "SeleccionarJ2_1a1","SeleccionarJ2_1a2","SeleccionarJ2_1a3","SeleccionarJ2_1a4",
                               "SeleccionarZ_1a1", "SeleccionarZ_1a2", "SeleccionarZ_1a3", "SeleccionarZ_1a4" };

        int idx = 0;
        foreach (string motor in motores)
        {
            string grupoNombre = $"GrupoJ{(motor == "Z" ? "Z" : motor)}";
            // GrupoJ1, GrupoJ2, GrupoZ
            string grupo = motor == "J1" ? "GrupoJ1" : motor == "J2" ? "GrupoJ2" : "GrupoZ";
            Transform grupoTr = panelIzq.transform.Find(grupo);

            foreach (string suf in sufijos)
            {
                string toggleName = $"Toggle{motor}_{suf}";
                Toggle tg = grupoTr != null
                    ? grupoTr.Find(toggleName)?.GetComponent<Toggle>()
                    : null;

                Asignar(so, campos[idx], tg);
                AsignarToggleValueChanged(tg, comp, metodos[idx]);
                idx++;
            }
        }

        so.ApplyModifiedProperties();
    }

    // =========================================================================
    // GESTOR SCARA — Wiring de transformTCP
    // =========================================================================

    private static void WirarTransformTCP(GameObject gestorGO)
    {
        var gestor = gestorGO.GetComponent<GestorSCARA>();
        if (gestor == null) return;

        GameObject tcpGO = GameObject.Find("TCP");
        if (tcpGO == null)
        {
            Debug.LogWarning("[v2] GameObject 'TCP' no encontrado. Asígnalo manualmente en GestorSCARA.");
            return;
        }

        var so = new SerializedObject(gestor);
        so.FindProperty("transformTCP").objectReferenceValue = tcpGO.transform;
        so.ApplyModifiedProperties();
    }

    // =========================================================================
    // PREFAB ÍTEM PUNTO
    // =========================================================================

    private static GameObject CrearPrefabItemPunto(Transform padre)
    {
        // No duplicar
        Transform existente = padre.Find("Plantilla_ItemPunto");
        if (existente != null) return existente.gameObject;

        GameObject go = new GameObject("Plantilla_ItemPunto");
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280f, 40f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11f;
        tmp.color    = ColoresUI.TextoSecundario;
        tmp.text     = "#000  M1:0  M2:0  M3:0";

        Undo.RegisterCreatedObjectUndo(go, "Plantilla_ItemPunto");
        return go;
    }

    // =========================================================================
    // HELPERS DE CREACIÓN DE UI
    // =========================================================================

    private static TextMeshProUGUI CrearTexto(string nombre, Transform padre,
        string texto, float fontSize, Color color,
        float yPos, float alto, bool bold = false)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX, yPos - alto);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = texto;
        tmp.fontSize = fontSize;
        tmp.color    = color;
        if (bold) tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    private static void CrearSeparadorH(Transform padre, float yPos)
    {
        GameObject go = new GameObject("Separador");
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, "Separador");

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX, yPos - 2f);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.15f);
    }

    private static GameObject CrearBotonCompleto(string nombre, Transform padre,
        string etiqueta, float yPos, Color colorFondo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX,  yPos - AltoBoton);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        Image img = go.AddComponent<Image>();
        img.color = colorFondo;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.white;
        btn.colors = cb;

        // Texto del botón
        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = FontBoton;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static void CrearBotonMitad(string nombre, Transform padre,
        string etiqueta, float yPos, bool izquierda)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        if (!izquierda)
        {
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(1, 1);
        }
        rt.pivot     = new Vector2(0.5f, 1);
        float offX   = izquierda ? 2f : 2f;
        rt.offsetMin = new Vector2(MargenX, yPos - AltoBoton);
        rt.offsetMax = new Vector2(-2f, yPos);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.22f, 0.38f);
        go.AddComponent<Button>();

        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = etiqueta;
        tmp.fontSize  = FontBoton;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject CrearInputField(string nombre, Transform padre,
        string placeholder, float yPos, float alto)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX,  yPos - alto);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.22f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();

        // Text Area
        GameObject taGO = new GameObject("Text Area");
        taGO.transform.SetParent(go.transform, false);
        RectTransform tart = taGO.AddComponent<RectTransform>();
        tart.anchorMin = Vector2.zero;
        tart.anchorMax = Vector2.one;
        tart.offsetMin = new Vector2(5, 2);
        tart.offsetMax = new Vector2(-5, -2);
        Mask mask = taGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        taGO.AddComponent<Image>().color = Color.clear;

        // Placeholder
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(taGO.transform, false);
        RectTransform phrt = phGO.AddComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
        phrt.offsetMin = phrt.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text     = placeholder;
        phTMP.fontSize = FontNormal;
        phTMP.color    = new Color(1, 1, 1, 0.3f);

        // Text
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(taGO.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.fontSize = FontNormal;
        txtTMP.color    = Color.white;

        input.textViewport   = tart;
        input.placeholder    = phTMP;
        input.textComponent  = txtTMP;

        return go;
    }

    private static void CrearScrollRectPuntos(string nombre, Transform padre,
        float yPos, float alto)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(MargenX,  yPos - alto);
        rt.offsetMax = new Vector2(-MargenX, yPos);

        go.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f);
        ScrollRect sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false;

        // Viewport
        GameObject vp = new GameObject("Viewport");
        vp.transform.SetParent(go.transform, false);
        RectTransform vprt = vp.AddComponent<RectTransform>();
        vprt.anchorMin = Vector2.zero;
        vprt.anchorMax = Vector2.one;
        vprt.offsetMin = vprt.offsetMax = Vector2.zero;
        vp.AddComponent<Image>().color = Color.clear;
        Mask vpMask = vp.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        // Contenedor
        GameObject cont = new GameObject("ContenedorPuntos");
        cont.transform.SetParent(vp.transform, false);
        RectTransform contRT = cont.AddComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0, 1);
        contRT.anchorMax = new Vector2(1, 1);
        contRT.pivot     = new Vector2(0.5f, 1);
        contRT.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup vlg = cont.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        ContentSizeFitter csf = cont.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content   = contRT;
        sr.viewport  = vprt;
    }

    private static GameObject CrearGOImagen(string nombre, Transform padre,
        Color color, Vector2 tamaño, Vector2 posicion)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Undo.RegisterCreatedObjectUndo(go, nombre);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamaño;

        go.AddComponent<Image>().color = color;
        return go;
    }

    // =========================================================================
    // HELPERS DE WIRING
    // =========================================================================

    private static void Asignar(SerializedObject so, string campo, Object valor)
    {
        var prop = so.FindProperty(campo);
        if (prop != null) prop.objectReferenceValue = valor;
        else Debug.LogWarning($"[v2] Campo '{campo}' no encontrado en {so.targetObject.name}");
    }

    private static void AsignarClick(Button btn, MonoBehaviour target, string metodo)
    {
        if (btn == null) return;
        var so   = new SerializedObject(btn);
        var oc   = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        oc.arraySize = 1;
        var call = oc.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue      = metodo;
        call.FindPropertyRelative("m_Mode").intValue               = 1;   // void, sin args
        call.FindPropertyRelative("m_CallState").intValue          = 2;   // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    // Cablea toggle.onValueChanged → método(bool) en el componente target
    private static void AsignarToggleValueChanged(Toggle toggle, MonoBehaviour target, string metodo)
    {
        if (toggle == null) return;
        var so   = new SerializedObject(toggle);
        var oc   = so.FindProperty("onValueChanged.m_PersistentCalls.m_Calls");
        if (oc == null) return;
        oc.arraySize = 1;
        var call = oc.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue      = metodo;
        call.FindPropertyRelative("m_Mode").intValue               = 0;   // EventDefined → pasa el bool del evento
        call.FindPropertyRelative("m_CallState").intValue          = 2;   // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    private static void AsignarEndEdit(TMP_InputField input, MonoBehaviour target, string metodo)
    {
        if (input == null) return;
        var so   = new SerializedObject(input);
        var oe   = so.FindProperty("m_OnEndEdit.m_PersistentCalls.m_Calls");
        if (oe == null) return;
        oe.arraySize = 1;
        var call = oe.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue      = metodo;
        call.FindPropertyRelative("m_Mode").intValue               = 5;   // string arg
        call.FindPropertyRelative("m_CallState").intValue          = 2;
        so.ApplyModifiedProperties();
    }

    private static TextMeshProUGUI BuscarTMP(GameObject raiz, string nombre)
        => raiz.transform.Find(nombre)?.GetComponent<TextMeshProUGUI>();

    private static Button BuscarBoton(GameObject raiz, string nombre)
        => raiz.transform.Find(nombre)?.GetComponent<Button>();

    private static Image BuscarImagen(GameObject raiz, string nombre)
        => raiz.transform.Find(nombre)?.GetComponent<Image>();

    private static TMP_InputField BuscarInput(GameObject raiz, string nombre)
        => raiz.transform.Find(nombre)?.GetComponent<TMP_InputField>();

    private static void LimpiarHijos(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(go.transform.GetChild(i).gameObject);
        }
    }

    // =========================================================================
    // REPARAR CONSOLA Y ERRORES
    // =========================================================================

    [MenuItem("SCARA/Reparar PanelConsola y PanelErrores")]
    public static void RepararConsolaYErrores()
    {
        GameObject panelInferior = GameObject.Find("PanelInferior");
        GameObject panelErrores  = GameObject.Find("PanelErroresActivos");
        GameObject gestorGO      = GameObject.Find("GestorPrincipal");

        if (panelInferior == null && panelErrores == null && gestorGO == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró PanelInferior, PanelErroresActivos ni GestorPrincipal.\n" +
                "Ejecuta primero SCARA → Construir Escena Completa.",
                "Aceptar");
            return;
        }

        if (gestorGO      != null) RepararFlagsGestorSCARA(gestorGO);
        if (panelInferior != null) RepararPanelConsola(panelInferior);
        if (panelErrores  != null) RepararPanelErrores(panelErrores);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Listo",
            "Consola y Errores reparados.\nGuarda la escena (Ctrl+S).",
            "Aceptar");
    }

    // =========================================================================
    // PANTALLA DE INICIO + BOTÓN SALIR
    // =========================================================================

    [MenuItem("SCARA/Crear Pantalla de Inicio y Botón Salir")]
    public static void CrearPantallaInicioYSalir()
    {
        CrearPantallaInicio();
        AgregarBotonSalir();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Listo",
            "Pantalla de inicio y botón Salir creados.\n\n" +
            "Recuerda:\n" +
            "• Asigna tu imagen izquierda al campo 'ImagenPrincipal' (CanvasPresentacion)\n" +
            "• Asigna tu logo al campo 'ImagenLogo' (CanvasPresentacion)\n" +
            "Guarda la escena (Ctrl+S).",
            "Aceptar");
    }

    private static void CrearPantallaInicio()
    {
        // No duplicar
        if (GameObject.Find("CanvasPresentacion") != null)
        {
            Debug.LogWarning("[SCARA] CanvasPresentacion ya existe. Elimínalo primero si quieres recrearlo.");
            return;
        }

        // ── Canvas ───────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("CanvasPresentacion");
        Undo.RegisterCreatedObjectUndo(canvasGO, "CanvasPresentacion");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;  // siempre encima del canvas principal

        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── Panel fondo completo ──────────────────────────────────────────────
        GameObject fondo = new GameObject("PanelFondo");
        fondo.transform.SetParent(canvasGO.transform, false);
        Undo.RegisterCreatedObjectUndo(fondo, "PanelFondo");
        var fondoRT = fondo.AddComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero; fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = fondoRT.offsetMax = Vector2.zero;
        fondo.AddComponent<Image>().color = ColoresUI.FondoPrincipal;

        // ── Panel izquierdo (imagen del robot) ────────────────────────────────
        GameObject panelIzq = new GameObject("PanelIzquierdo");
        panelIzq.transform.SetParent(fondo.transform, false);
        Undo.RegisterCreatedObjectUndo(panelIzq, "PanelIzquierdo");
        var izqRT = panelIzq.AddComponent<RectTransform>();
        izqRT.anchorMin = new Vector2(0, 0); izqRT.anchorMax = new Vector2(0.5f, 1);
        izqRT.offsetMin = izqRT.offsetMax = Vector2.zero;
        panelIzq.AddComponent<Image>().color = ColoresUI.FondoPrincipal;

        // Imagen placeholder izquierda (el usuario asigna el sprite)
        GameObject imgIzqGO = new GameObject("ImagenPrincipal");
        imgIzqGO.transform.SetParent(panelIzq.transform, false);
        Undo.RegisterCreatedObjectUndo(imgIzqGO, "ImagenPrincipal");
        var imgIzqRT = imgIzqGO.AddComponent<RectTransform>();
        imgIzqRT.anchorMin = new Vector2(0.05f, 0.05f);
        imgIzqRT.anchorMax = new Vector2(0.95f, 0.95f);
        imgIzqRT.offsetMin = imgIzqRT.offsetMax = Vector2.zero;
        var imgIzq = imgIzqGO.AddComponent<Image>();
        imgIzq.color = new Color(1, 1, 1, 0.08f);  // placeholder translúcido

        // ── Panel derecho (blanco, textos) ────────────────────────────────────
        GameObject panelDer = new GameObject("PanelDerecho");
        panelDer.transform.SetParent(fondo.transform, false);
        Undo.RegisterCreatedObjectUndo(panelDer, "PanelDerecho");
        var derRT = panelDer.AddComponent<RectTransform>();
        derRT.anchorMin = new Vector2(0.5f, 0); derRT.anchorMax = new Vector2(1, 1);
        derRT.offsetMin = derRT.offsetMax = Vector2.zero;
        panelDer.AddComponent<Image>().color = Color.white;

        // Logo (arriba centrado)
        GameObject logoGO = new GameObject("ImagenLogo");
        logoGO.transform.SetParent(panelDer.transform, false);
        Undo.RegisterCreatedObjectUndo(logoGO, "ImagenLogo");
        var logoRT = logoGO.AddComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0.2f, 1); logoRT.anchorMax = new Vector2(0.8f, 1);
        logoRT.pivot     = new Vector2(0.5f, 1);
        logoRT.anchoredPosition = new Vector2(0, -60);
        logoRT.sizeDelta = new Vector2(0, 130);
        var logoImg = logoGO.AddComponent<Image>();
        logoImg.color = new Color(0.1f, 0.1f, 0.2f, 0.15f);  // placeholder

        // Texto título
        GameObject tituloGO = new GameObject("TextoTitulo");
        tituloGO.transform.SetParent(panelDer.transform, false);
        Undo.RegisterCreatedObjectUndo(tituloGO, "TextoTitulo");
        var tituloRT = tituloGO.AddComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0.05f, 0.5f); tituloRT.anchorMax = new Vector2(0.95f, 0.5f);
        tituloRT.pivot     = new Vector2(0.5f, 0.5f);
        tituloRT.anchoredPosition = new Vector2(0, 60);
        tituloRT.sizeDelta = new Vector2(0, 90);
        var tituloTMP = tituloGO.AddComponent<TextMeshProUGUI>();
        tituloTMP.text      = "ROBOT VIRTUAL SCARA";
        tituloTMP.fontSize  = 64f;
        tituloTMP.fontStyle = TMPro.FontStyles.Bold;
        tituloTMP.color     = ColoresUI.FondoPrincipal;
        tituloTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // Texto bienvenido
        GameObject bienvenidoGO = new GameObject("TextoBienvenido");
        bienvenidoGO.transform.SetParent(panelDer.transform, false);
        Undo.RegisterCreatedObjectUndo(bienvenidoGO, "TextoBienvenido");
        var bienvenidoRT = bienvenidoGO.AddComponent<RectTransform>();
        bienvenidoRT.anchorMin = new Vector2(0.05f, 0.5f); bienvenidoRT.anchorMax = new Vector2(0.95f, 0.5f);
        bienvenidoRT.pivot     = new Vector2(0.5f, 0.5f);
        bienvenidoRT.anchoredPosition = new Vector2(0, -30);
        bienvenidoRT.sizeDelta = new Vector2(0, 50);
        var bienvenidoTMP = bienvenidoGO.AddComponent<TextMeshProUGUI>();
        bienvenidoTMP.text      = "Bienvenido";
        bienvenidoTMP.fontSize  = 36f;
        bienvenidoTMP.color     = new Color(0.3f, 0.3f, 0.3f);
        bienvenidoTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // Texto "presiona tecla"
        GameObject continuarGO = new GameObject("TextoContinuar");
        continuarGO.transform.SetParent(panelDer.transform, false);
        Undo.RegisterCreatedObjectUndo(continuarGO, "TextoContinuar");
        var continuarRT = continuarGO.AddComponent<RectTransform>();
        continuarRT.anchorMin = new Vector2(0.05f, 0); continuarRT.anchorMax = new Vector2(0.95f, 0);
        continuarRT.pivot     = new Vector2(0.5f, 0);
        continuarRT.anchoredPosition = new Vector2(0, 30);
        continuarRT.sizeDelta = new Vector2(0, 30);
        var continuarTMP = continuarGO.AddComponent<TextMeshProUGUI>();
        continuarTMP.text      = "Presiona cualquier tecla para continuar...";
        continuarTMP.fontSize  = 16f;
        continuarTMP.fontStyle = TMPro.FontStyles.Italic;
        continuarTMP.color     = new Color(0.5f, 0.5f, 0.5f);
        continuarTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // ── Componente PantallaInicio ─────────────────────────────────────────
        PantallaInicio comp = canvasGO.AddComponent<PantallaInicio>();
        var so = new SerializedObject(comp);
        so.FindProperty("canvasPresentacion").objectReferenceValue = canvas;
        so.ApplyModifiedProperties();

        Debug.Log("[SCARA] CanvasPresentacion creado. Asigna tus sprites en ImagenPrincipal e ImagenLogo.");
    }

    private static void AgregarBotonSalir()
    {
        GameObject barraSuperior = GameObject.Find("PanelSuperior");
        if (barraSuperior == null) barraSuperior = GameObject.Find("PanelBarraSuperior");
        if (barraSuperior == null)
        {
            Debug.LogWarning("[SCARA] No se encontró PanelSuperior ni PanelBarraSuperior. Agrega el botón Salir manualmente.");
            return;
        }

        // No duplicar
        if (barraSuperior.transform.Find("BotonSalir") != null)
        {
            Debug.LogWarning("[SCARA] BotonSalir ya existe en la barra superior.");
            return;
        }

        // Crear botón en la esquina superior derecha
        GameObject btnGO = new GameObject("BotonSalir");
        btnGO.transform.SetParent(barraSuperior.transform, false);
        Undo.RegisterCreatedObjectUndo(btnGO, "BotonSalir");

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot     = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-8, 0);
        rt.sizeDelta = new Vector2(80, 28);

        Image img = btnGO.AddComponent<Image>();
        img.color = ColoresUI.Error;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.9f, 0.2f, 0.2f);
        cb.pressedColor     = new Color(0.6f, 0.1f, 0.1f);
        btn.colors = cb;

        // Texto
        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "X Salir";
        tmp.fontSize  = 13f;
        tmp.color     = Color.white;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        // Cablear al componente PanelBarraSuperior
        var compBarra = barraSuperior.GetComponent<PanelBarraSuperior>();
        if (compBarra != null)
        {
            var soBarra = new SerializedObject(compBarra);
            soBarra.FindProperty("botonSalir").objectReferenceValue = btn;
            soBarra.ApplyModifiedProperties();
            AsignarClick(btn, compBarra, "AlPresionarSalir");
        }

        Debug.Log("[SCARA] Botón Salir creado en la barra superior.");
    }

    private static void RepararFlagsGestorSCARA(GameObject gestorGO)
    {
        var gestor = gestorGO.GetComponent<GestorSCARA>();
        if (gestor == null) { Debug.LogWarning("[Reparar] GestorPrincipal no tiene GestorSCARA."); return; }

        var so = new SerializedObject(gestor);
        so.FindProperty("invertirZ")         .boolValue = true;
        so.FindProperty("invertirConteoJ1")  .boolValue = true;
        so.FindProperty("invertirConteoJ2")  .boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log("[Reparar] GestorSCARA: invertirZ=true, invertirConteoJ1=true, invertirConteoJ2=true aplicados.");
    }

    private static void RepararPanelConsola(GameObject panel)
    {
        var comp = panel.GetComponent<PanelConsola>();
        if (comp == null) { Debug.LogWarning("[Reparar] PanelConsola no tiene componente PanelConsola."); return; }
        var so = new SerializedObject(comp);

        // ── Crear TextMeshProUGUI dentro de ConsolaLog si no existe ─────────────
        Transform consolaLog = panel.transform.Find("ConsolaLog");
        TextMeshProUGUI textoTMP = consolaLog?.GetComponentInChildren<TextMeshProUGUI>();

        if (textoTMP == null && consolaLog != null)
        {
            Transform viewport = consolaLog.Find("Viewport");
            if (viewport == null)
            {
                GameObject vpGO = new GameObject("Viewport");
                vpGO.transform.SetParent(consolaLog, false);
                Undo.RegisterCreatedObjectUndo(vpGO, "Viewport");
                RectTransform vpRT = vpGO.AddComponent<RectTransform>();
                vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
                vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
                vpGO.AddComponent<Image>().color = Color.clear;
                Mask vpMask = vpGO.AddComponent<Mask>();
                vpMask.showMaskGraphic = false;
                viewport = vpGO.transform;
            }

            // Eliminar contenido antiguo si existe
            Transform contenidoAntiguo = viewport.Find("Contenido");
            if (contenidoAntiguo != null)
                Undo.DestroyObjectImmediate(contenidoAntiguo.gameObject);

            // TextMeshProUGUI puro — no seleccionable, crece con el contenido
            GameObject txtGO = new GameObject("TextoConsola");
            txtGO.transform.SetParent(viewport, false);
            Undo.RegisterCreatedObjectUndo(txtGO, "TextoConsola");

            RectTransform txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0, 1);
            txtRT.anchorMax = new Vector2(1, 1);
            txtRT.pivot     = new Vector2(0.5f, 1);
            txtRT.offsetMin = new Vector2(4, 0);
            txtRT.offsetMax = new Vector2(-4, 0);

            textoTMP = txtGO.AddComponent<TextMeshProUGUI>();
            textoTMP.fontSize  = 11f;
            textoTMP.color     = ColoresUI.TextoPrincipal;
            textoTMP.richText  = true;
            textoTMP.alignment = TextAlignmentOptions.TopLeft;

            // ContentSizeFitter para que crezca verticalmente con el texto
            ContentSizeFitter csf = txtGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Configurar el ScrollRect
            ScrollRect sr = consolaLog.GetComponent<ScrollRect>();
            if (sr != null)
            {
                sr.content  = txtRT;
                sr.viewport = viewport.GetComponent<RectTransform>();
                sr.vertical   = true;
                sr.horizontal = false;
            }

            Debug.Log("[Reparar] TextMeshProUGUI TextoConsola creado en ConsolaLog.");
        }

        // ── Asignar referencias ───────────────────────────────────────────────
        so.FindProperty("textoConsola").objectReferenceValue  = textoTMP;
        so.FindProperty("scrollConsola").objectReferenceValue = consolaLog?.GetComponent<ScrollRect>();

        Button btnPausa    = panel.transform.Find("BarraBotones/BotonPausa")   ?.GetComponent<Button>();
        Button btnLimpiar  = panel.transform.Find("BarraBotones/BotonLimpiar") ?.GetComponent<Button>();
        Button btnExportar = panel.transform.Find("BarraBotones/BotonExportar")?.GetComponent<Button>();

        so.FindProperty("botonPausa")   .objectReferenceValue = btnPausa;
        so.FindProperty("botonLimpiar") .objectReferenceValue = btnLimpiar;
        so.FindProperty("botonExportar").objectReferenceValue = btnExportar;
        so.FindProperty("textoBotonPausa").objectReferenceValue =
            panel.transform.Find("BarraBotones/BotonPausa/Texto")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("filtroT").objectReferenceValue = panel.transform.Find("BarraBotones/FiltroT")?.GetComponent<Toggle>();
        so.FindProperty("filtroE").objectReferenceValue = panel.transform.Find("BarraBotones/FiltroE")?.GetComponent<Toggle>();
        so.FindProperty("filtroL").objectReferenceValue = panel.transform.Find("BarraBotones/FiltroL")?.GetComponent<Toggle>();
        so.FindProperty("filtroB").objectReferenceValue = panel.transform.Find("BarraBotones/FiltroB")?.GetComponent<Toggle>();
        so.FindProperty("textoContador").objectReferenceValue =
            panel.transform.Find("BarraBotones/TextoContador")?.GetComponent<TextMeshProUGUI>();

        so.ApplyModifiedProperties();

        // ── Cablear onClick ───────────────────────────────────────────────────
        AsignarClick(btnPausa,    comp, "AlPresionarPausa");
        AsignarClick(btnLimpiar,  comp, "AlPresionarLimpiar");
        AsignarClick(btnExportar, comp, "AlPresionarExportar");

        Debug.Log("[Reparar] PanelConsola: referencias y onClick cableados.");
    }

    private static void RepararPanelErrores(GameObject panel)
    {
        var comp = panel.GetComponent<PanelErrores>();
        if (comp == null) { Debug.LogWarning("[Reparar] PanelErroresActivos no tiene componente PanelErrores."); return; }
        var so = new SerializedObject(comp);

        Button btnLimpiar = panel.transform.Find("BotonLimpiarErrores")?.GetComponent<Button>();
        so.FindProperty("botonLimpiarErrores").objectReferenceValue = btnLimpiar;
        so.ApplyModifiedProperties();

        AsignarClick(btnLimpiar, comp, "AlPresionarLimpiar");

        Debug.Log("[Reparar] PanelErrores: botonLimpiarErrores cableado.");
    }
}
