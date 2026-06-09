using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Crea todos los visualizadores 3D y los botones de cambio de módulo en la escena.
/// Ejecutar DESPUÉS de haber construido la escena principal.
/// USO: Menú Unity → SCARA → Crear Visualizadores
/// </summary>
public static class CrearVisualizadores
{
    [MenuItem("SCARA/Crear Visualizadores")]
    public static void CrearTodo()
    {
        GestorSCARA gestor = Object.FindObjectOfType<GestorSCARA>();
        if (gestor == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró GestorSCARA en la escena.\n" +
                "Ejecuta primero: SCARA → Construir Escena Completa",
                "Aceptar");
            return;
        }

        CrearVisualizadorTCP(gestor);
        CrearVisualizadorEspacioTrabajo();
        CrearVisualizadorAngulos(gestor);
        CrearVisualizadorTrayectoria(gestor);
        CrearBotonesModulo(gestor);
        CrearEjesXYZ();
        CrearCuadriculaSuelo();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SCARA] Visualizadores creados. Guarda la escena (Ctrl+S).");
        EditorUtility.DisplayDialog("Listo",
            "Visualizadores creados correctamente:\n\n" +
            "• Esfera TCP (amarilla)\n" +
            "• Anillo espacio de trabajo\n" +
            "• Arcos de ángulos\n" +
            "• Líneas de trayectoria\n" +
            "• Botones M1 / M2 / M3\n" +
            "• Ejes XYZ\n" +
            "• Cuadrícula milimétrica\n\n" +
            "Guarda la escena (Ctrl+S).",
            "Aceptar");
    }

    // ─── HELPER: obtener origen del robot ─────────────────────────────────────
    // Busca pivot_art1 primero (hombro del brazo 1) y cae en SCARA_ROBOT si no.
    private static Vector3 ObtenerOrigenRobot()
    {
        Transform pivot = BuscarEnJerarquia("pivot_art1");
        if (pivot != null) return pivot.position;

        Transform scara = BuscarEnJerarquia("SCARA_ROBOT");
        if (scara != null) return scara.position;

        return Vector3.zero;
    }

    // =========================================================================
    // ESFERA TCP
    // =========================================================================

    private static void CrearVisualizadorTCP(GestorSCARA gestor)
    {
        Transform tcp = BuscarEnJerarquia("TCP");
        if (tcp == null)
        {
            Debug.LogWarning("[SCARA] No se encontró 'TCP' en la jerarquía. " +
                             "Asigna la referencia manualmente en el Inspector.");
        }

        // Esfera: 10 unidades Unity (metros) — tamaño confirmado por el usuario como visible
        float tamEsfera = 10f;

        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "EsferaTCP";
        esfera.transform.localScale = Vector3.one * tamEsfera;

        // Posicionar en el TCP actual (o en el hombro si TCP no existe)
        esfera.transform.position = tcp != null ? tcp.position : ObtenerOrigenRobot();

        // Material amarillo emisivo
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.85f, 0f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.8f, 0.6f, 0f));
        AssetDatabase.CreateAsset(mat, "Assets/Datos/MaterialTCP.mat");
        esfera.GetComponent<Renderer>().material = mat;

        Object.DestroyImmediate(esfera.GetComponent<Collider>());

        VisualizadorTCP vis = esfera.AddComponent<VisualizadorTCP>();

        SerializedObject so = new SerializedObject(vis);
        if (tcp != null)
            so.FindProperty("transformTCP").objectReferenceValue = tcp;
        so.FindProperty("esferaTCP").objectReferenceValue   = esfera;
        so.FindProperty("materialTCP").objectReferenceValue = mat;
        so.FindProperty("escalaEsfera").floatValue          = tamEsfera;   // 10 unidades Unity
        so.ApplyModifiedProperties();

        // Forzar la escala en el transform por si la serialización no aplica en Edit mode
        esfera.transform.localScale = Vector3.one * tamEsfera;

        Undo.RegisterCreatedObjectUndo(esfera, "Crear EsferaTCP");
        Debug.Log("[SCARA] EsferaTCP creada" +
                  (tcp != null ? $" en TCP {tcp.position}" : " (TCP no encontrado)") + ".");
    }

    // =========================================================================
    // ANILLO ESPACIO DE TRABAJO
    // =========================================================================

    private static void CrearVisualizadorEspacioTrabajo()
    {
        // Centrar el anillo en pivot_art1 (origen real del espacio de trabajo)
        Vector3 origen = ObtenerOrigenRobot();

        GameObject go = new GameObject("VisualizadorEspacioTrabajo");
        // Y del anillo = Y del pivot_art1 (plano de trabajo del robot)
        go.transform.position = new Vector3(origen.x, origen.y, origen.z);

        VisualizadorEspacioTrabajo vis = go.AddComponent<VisualizadorEspacioTrabajo>();

        SerializedObject so = new SerializedObject(vis);
        so.FindProperty("colorAnillo").colorValue         = new Color(1f, 0.85f, 0f, 0.18f);
        so.FindProperty("colorBordeExterior").colorValue  = new Color(1f, 0.85f, 0f, 0.55f);
        so.FindProperty("mostrar").boolValue              = true;
        so.FindProperty("segmentos").intValue             = 64;
        so.FindProperty("multiplicadorEscala").floatValue = 10f;   // Escala en metros
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(go, "Crear VisualizadorEspacioTrabajo");
        Debug.Log($"[SCARA] VisualizadorEspacioTrabajo creado en {origen}.");
    }

    // =========================================================================
    // ARCOS DE ÁNGULOS
    // =========================================================================

    private static void CrearVisualizadorAngulos(GestorSCARA gestor)
    {
        Transform pivotArt1 = BuscarEnJerarquia("pivot_art1");
        Transform pivotArt2 = BuscarEnJerarquia("pivot_art2");
        Transform giroGarra = BuscarEnJerarquia("GiroGarra");

        GameObject go = new GameObject("VisualizadorAngulos");
        // Centrar el contenedor en el hombro del robot
        go.transform.position = ObtenerOrigenRobot();

        VisualizadorAngulos vis = go.AddComponent<VisualizadorAngulos>();

        SerializedObject so = new SerializedObject(vis);
        if (pivotArt1 != null) so.FindProperty("pivotArt1").objectReferenceValue = pivotArt1;
        if (pivotArt2 != null) so.FindProperty("pivotArt2").objectReferenceValue = pivotArt2;
        if (giroGarra != null) so.FindProperty("giroGarra").objectReferenceValue = giroGarra;
        so.FindProperty("gestorSCARA").objectReferenceValue = gestor;
        // Radio arco: 15 unidades Unity (metros) — proporcional a esfera=10
        so.FindProperty("radioArco").floatValue             = 15f;
        so.FindProperty("segmentosArco").intValue           = 48;
        so.FindProperty("mostrar").boolValue                = true;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(go, "Crear VisualizadorAngulos");
        Debug.Log("[SCARA] VisualizadorAngulos creado.");
    }

    // =========================================================================
    // TRAYECTORIA (línea roja real + línea azul referencia)
    // =========================================================================

    private static void CrearVisualizadorTrayectoria(GestorSCARA gestor)
    {
        Transform tcp = BuscarEnJerarquia("TCP");
        PanelTrayectorias panelTray = Object.FindObjectOfType<PanelTrayectorias>();

        GameObject goVis = new GameObject("VisualizadorTrayectoria");

        // LineRenderer — trayectoria real (roja)
        GameObject goReal = new GameObject("LineaReal");
        goReal.transform.SetParent(goVis.transform);
        LineRenderer lrReal = goReal.AddComponent<LineRenderer>();
        ConfigurarLineRenderer(lrReal,
            new Color(0.95f, 0.25f, 0.25f, 0.9f),
            5f * ConfiguracionRobot.MM_A_UNITY);   // 5mm de ancho

        // LineRenderer — trayectoria referencia (azul)
        GameObject goRef = new GameObject("LineaReferencia");
        goRef.transform.SetParent(goVis.transform);
        LineRenderer lrRef = goRef.AddComponent<LineRenderer>();
        ConfigurarLineRenderer(lrRef,
            new Color(0.20f, 0.55f, 1.00f, 0.7f),
            4f * ConfiguracionRobot.MM_A_UNITY);   // 4mm de ancho

        VisualizadorTrayectoria vis = goVis.AddComponent<VisualizadorTrayectoria>();

        SerializedObject so = new SerializedObject(vis);
        if (tcp != null)
            so.FindProperty("transformTCP").objectReferenceValue      = tcp;
        so.FindProperty("gestorSCARA").objectReferenceValue           = gestor;
        if (panelTray != null)
            so.FindProperty("panelTrayectorias").objectReferenceValue = panelTray;
        so.FindProperty("lineaTrayectoriaReal").objectReferenceValue  = lrReal;
        so.FindProperty("lineaTrayectoriaRef").objectReferenceValue   = lrRef;
        so.FindProperty("colorReal").colorValue       = new Color(0.95f, 0.25f, 0.25f, 0.9f);
        so.FindProperty("colorReferencia").colorValue = new Color(0.20f, 0.55f, 1.00f, 0.7f);
        so.FindProperty("anchoLinea").floatValue      = 5f * ConfiguracionRobot.MM_A_UNITY;
        so.FindProperty("maxPuntosReales").intValue   = 500;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(goVis, "Crear VisualizadorTrayectoria");
        Debug.Log("[SCARA] VisualizadorTrayectoria creado.");
    }

    // =========================================================================
    // BOTONES M1 / M2 / M3 EN LA BARRA SUPERIOR
    // =========================================================================

    private static void CrearBotonesModulo(GestorSCARA gestor)
    {
        GameObject barra = GameObject.Find("PanelBarraSuperior");
        if (barra == null)
        {
            Debug.LogWarning("[SCARA] No se encontró PanelBarraSuperior. " +
                             "Botones de módulo no creados.");
            return;
        }

        foreach (string nombre in new[] { "BotonM1", "BotonM2", "BotonM3" })
        {
            Transform existente = barra.transform.Find(nombre);
            if (existente != null) Object.DestroyImmediate(existente.gameObject);
        }

        var botones = new (string nombre, string texto, string metodo)[]
        {
            ("BotonM1", "M1", "CambiarAModulo1"),
            ("BotonM2", "M2", "CambiarAModulo2"),
            ("BotonM3", "M3", "CambiarAModulo3"),
        };

        float xBase = 730f;
        float ancho = 55f;
        float alto  = 36f;
        float sep   = 62f;

        for (int i = 0; i < botones.Length; i++)
        {
            var (nombre, texto, metodo) = botones[i];

            GameObject goBtn = new GameObject(nombre);
            goBtn.transform.SetParent(barra.transform, false);

            RectTransform rt = goBtn.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xBase + i * sep, 0f);
            rt.sizeDelta        = new Vector2(ancho, alto);

            Image img = goBtn.AddComponent<Image>();
            img.color = new Color(0.08f, 0.13f, 0.26f, 1f);

            Button btn = goBtn.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(0.08f, 0.13f, 0.26f);
            cb.highlightedColor = new Color(1f, 0.85f, 0f, 0.25f);
            cb.pressedColor     = new Color(1f, 0.85f, 0f, 0.45f);
            cb.selectedColor    = new Color(1f, 0.85f, 0f, 0.35f);
            btn.colors = cb;

            GameObject goTxt = new GameObject("Texto");
            goTxt.transform.SetParent(goBtn.transform, false);
            RectTransform rtTxt = goTxt.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = Vector2.zero;
            rtTxt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = goTxt.AddComponent<TextMeshProUGUI>();
            tmp.text      = texto;
            tmp.fontSize  = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = new Color(1f, 0.85f, 0f);
            tmp.alignment = TextAlignmentOptions.Center;

            Outline outline = goBtn.AddComponent<Outline>();
            outline.effectColor    = new Color(1f, 0.85f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                btn.onClick,
                System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction),
                    gestor,
                    typeof(GestorSCARA).GetMethod(metodo)) as UnityEngine.Events.UnityAction);

            Undo.RegisterCreatedObjectUndo(goBtn, $"Crear {nombre}");
        }

        Debug.Log("[SCARA] Botones M1/M2/M3 creados en PanelBarraSuperior.");
    }

    // =========================================================================
    // EJES XYZ
    // =========================================================================

    private static void CrearEjesXYZ()
    {
        // Origen en el hombro del robot (pivot_art1)
        Vector3 origen = ObtenerOrigenRobot();

        GameObject contenedor = new GameObject("EjesXYZ");
        contenedor.transform.position = origen;

        // Longitud: 300mm × MM_A_UNITY
        CrearEje("EjeX", contenedor.transform, Vector3.right,   Color.red,   new Color(1f, 0.2f, 0.2f));
        CrearEje("EjeY", contenedor.transform, Vector3.up,      Color.green, new Color(0.2f, 1f, 0.2f));
        CrearEje("EjeZ", contenedor.transform, Vector3.forward, Color.blue,  new Color(0.2f, 0.4f, 1f));

        Undo.RegisterCreatedObjectUndo(contenedor, "Crear EjesXYZ");
        Debug.Log($"[SCARA] Ejes XYZ creados en {origen}.");
    }

    private static void CrearEje(string nombre, Transform padre,
        Vector3 direccion, Color colorBase, Color colorEmisivo)
    {
        // 30 unidades Unity de largo, 1 unidad de radio — proporcional a esfera=10
        float largo = 30f;
        float radio =  1f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        Object.DestroyImmediate(go.GetComponent<Collider>());

        go.transform.localScale    = new Vector3(radio * 2f, largo / 2f, radio * 2f);
        go.transform.localPosition = direccion * (largo / 2f);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direccion);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = colorBase;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colorEmisivo * 0.7f);
        go.GetComponent<Renderer>().material = mat;
    }

    // =========================================================================
    // CUADRÍCULA MILIMÉTRICA EN EL SUELO
    // =========================================================================

    private static void CrearCuadriculaSuelo()
    {
        // Buscar suelo: primero "Plane", luego cualquier plano en la escena
        GameObject plane = GameObject.Find("Plane");
        if (plane == null)
        {
            Debug.LogWarning("[SCARA] No se encontró 'Plane'. " +
                             "Cuadrícula no aplicada al suelo.");
            return;
        }

        Material matCuad = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        matCuad.color = new Color(0.12f, 0.20f, 0.38f, 1f);
        AssetDatabase.CreateAsset(matCuad, "Assets/Datos/MaterialSuelo.mat");

        Renderer rend = plane.GetComponent<Renderer>();
        if (rend != null)
            rend.material = matCuad;

        GameObject cuadricula = new GameObject("CuadriculaSuelo");
        cuadricula.transform.position = plane.transform.position;

        // Cuadrícula en metros — proporcional al modelo a escala metros
        float tamano  = 50f;    // ±50 metros de radio
        float paso    = 10f;    // línea principal cada 10 metros
        float pasoMin =  5f;    // línea secundaria cada 5 metros
        Color colorPrincipal  = new Color(0.25f, 0.40f, 0.70f, 0.8f);
        Color colorSecundario = new Color(0.15f, 0.25f, 0.45f, 0.4f);
        float y = plane.transform.position.y + 0.1f;   // 10cm sobre el suelo

        int indice = 0;
        for (float x = -tamano; x <= tamano + 0.01f; x += pasoMin)
        {
            bool  esPrincipal = Mathf.Abs(x % paso) < 0.1f;
            Color color = esPrincipal ? colorPrincipal : colorSecundario;
            float ancho = esPrincipal ? 0.2f : 0.1f;   // grosor en metros
            AgregarLineaCuadricula(cuadricula.transform, $"Linea_{indice++}",
                new Vector3(x, y, -tamano), new Vector3(x, y, tamano), color, ancho);

            AgregarLineaCuadricula(cuadricula.transform, $"Linea_{indice++}",
                new Vector3(-tamano, y, x), new Vector3(tamano, y, x), color, ancho);
        }

        Undo.RegisterCreatedObjectUndo(cuadricula, "Crear CuadriculaSuelo");
        Debug.Log("[SCARA] Cuadrícula de suelo creada.");
    }

    private static void AgregarLineaCuadricula(Transform padre, string nombre,
        Vector3 inicio, Vector3 fin, Color color, float ancho)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.positionCount     = 2;
        lr.SetPosition(0, inicio);
        lr.SetPosition(1, fin);
        lr.startWidth  = ancho;
        lr.endWidth    = ancho;
        lr.material    = new Material(Shader.Find("Sprites/Default"));
        lr.startColor  = color;
        lr.endColor    = color;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static Transform BuscarEnJerarquia(string nombre)
    {
        GameObject go = GameObject.Find(nombre);
        return go != null ? go.transform : null;
    }

    private static void ConfigurarLineRenderer(LineRenderer lr, Color color, float ancho)
    {
        lr.useWorldSpace     = true;
        lr.positionCount     = 0;
        lr.startWidth        = ancho;
        lr.endWidth          = ancho;
        lr.material          = new Material(Shader.Find("Sprites/Default"));
        lr.startColor        = color;
        lr.endColor          = color;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
    }
}
