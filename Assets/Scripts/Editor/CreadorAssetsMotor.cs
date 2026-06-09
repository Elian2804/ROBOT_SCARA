using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Herramienta de editor para crear los 5 assets ConfiguracionMotor
/// del robot SCARA con todos sus valores precargados.
///
/// USO: Menú Unity → SCARA → Crear Assets de Motores
/// Los assets se guardan en Assets/Datos/Motores/
/// </summary>
public static class CreadorAssetsMotor
{
    private const string CarpetaDestino = "Assets/Datos/Motores";

    [MenuItem("SCARA/Crear Assets de Motores")]
    public static void CrearTodosLosMotores()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Datos"))
            AssetDatabase.CreateFolder("Assets", "Datos");
        if (!AssetDatabase.IsValidFolder(CarpetaDestino))
            AssetDatabase.CreateFolder("Assets/Datos", "Motores");

        CrearMotorM1_EjeZ();
        CrearMotorM2_Brazo1();
        CrearMotorM3_Brazo2();
        CrearServoS1_GiroGarra();
        CrearServoS2_Gripper();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SCARA] Assets de motores creados en: " + CarpetaDestino);
        EditorUtility.DisplayDialog(
            "Assets creados",
            "Se crearon 5 assets ConfiguracionMotor en:\n" + CarpetaDestino +
            "\n\nArrástralos al Inspector de GestorSCARA.",
            "Aceptar");
    }

    // ─── M1 — EJE Z (Stepper lineal) ─────────────────────────────────────────

    private static void CrearMotorM1_EjeZ()
    {
        ConfiguracionMotor asset = ScriptableObject.CreateInstance<ConfiguracionMotor>();
        asset.nombreActuador    = "M1 - Eje Z";
        asset.tipo              = TipoActuador.Stepper;
        asset.pinSenal          = ConfiguracionRobot.M1_PinSenal;   // 15
        asset.pinDir            = ConfiguracionRobot.M1_PinDir;     // 2
        asset.velocidadMaxPasos = 10000f;
        asset.limiteMin         = ConfiguracionRobot.Z_Min_mm;       // 0 mm
        asset.limiteMax         = ConfiguracionRobot.Z_Max_mm;       // 200 mm
        asset.pulsoMin_us       = ConfiguracionRobot.ServoPulsoMin_us;
        asset.pulsoMax_us       = ConfiguracionRobot.ServoPulsoMax_us;
        GuardarAsset(asset, "M1_EjeZ");
    }

    // ─── M2 — BRAZO 1 / J1 (Stepper rotacional) ──────────────────────────────

    private static void CrearMotorM2_Brazo1()
    {
        ConfiguracionMotor asset = ScriptableObject.CreateInstance<ConfiguracionMotor>();
        asset.nombreActuador    = "M2 - Brazo 1 (J1)";
        asset.tipo              = TipoActuador.Stepper;
        asset.pinSenal          = ConfiguracionRobot.M2_PinSenal;   // 19
        asset.pinDir            = ConfiguracionRobot.M2_PinDir;     // 18
        asset.velocidadMaxPasos = 10000f;
        asset.limiteMin         = ConfiguracionRobot.J1_Min;         // -90°
        asset.limiteMax         = ConfiguracionRobot.J1_Max;         //  90°
        asset.pulsoMin_us       = ConfiguracionRobot.ServoPulsoMin_us;
        asset.pulsoMax_us       = ConfiguracionRobot.ServoPulsoMax_us;
        GuardarAsset(asset, "M2_Brazo1");
    }

    // ─── M3 — BRAZO 2 / J2 (Stepper rotacional) ──────────────────────────────

    private static void CrearMotorM3_Brazo2()
    {
        ConfiguracionMotor asset = ScriptableObject.CreateInstance<ConfiguracionMotor>();
        asset.nombreActuador    = "M3 - Brazo 2 (J2)";
        asset.tipo              = TipoActuador.Stepper;
        asset.pinSenal          = ConfiguracionRobot.M3_PinSenal;   // 16
        asset.pinDir            = ConfiguracionRobot.M3_PinDir;     // 4
        asset.velocidadMaxPasos = 10000f;
        asset.limiteMin         = ConfiguracionRobot.J2_Min;         // -90°
        asset.limiteMax         = ConfiguracionRobot.J2_Max;         //  90°
        asset.pulsoMin_us       = ConfiguracionRobot.ServoPulsoMin_us;
        asset.pulsoMax_us       = ConfiguracionRobot.ServoPulsoMax_us;
        GuardarAsset(asset, "M3_Brazo2");
    }

    // ─── S1 — GIROGARRA (Servo) ───────────────────────────────────────────────

    private static void CrearServoS1_GiroGarra()
    {
        ConfiguracionMotor asset = ScriptableObject.CreateInstance<ConfiguracionMotor>();
        asset.nombreActuador    = "S1 - GiroGarra";
        asset.tipo              = TipoActuador.Servo;
        asset.pinSenal          = ConfiguracionRobot.S1_Pin;   // 35
        asset.pinDir            = 0;
        asset.velocidadMaxPasos = 0f;
        asset.limiteMin         = ConfiguracionRobot.S1_Min;
        asset.limiteMax         = ConfiguracionRobot.S1_Max;
        asset.pulsoMin_us       = ConfiguracionRobot.ServoPulsoMin_us;
        asset.pulsoMax_us       = ConfiguracionRobot.ServoPulsoMax_us;
        GuardarAsset(asset, "S1_GiroGarra");
    }

    // ─── S2 — GRIPPER (Servo) ─────────────────────────────────────────────────

    private static void CrearServoS2_Gripper()
    {
        ConfiguracionMotor asset = ScriptableObject.CreateInstance<ConfiguracionMotor>();
        asset.nombreActuador    = "S2 - Gripper";
        asset.tipo              = TipoActuador.Servo;
        asset.pinSenal          = ConfiguracionRobot.S2_Pin;   // 34
        asset.pinDir            = 0;
        asset.velocidadMaxPasos = 0f;
        asset.limiteMin         = ConfiguracionRobot.S2_Min;
        asset.limiteMax         = ConfiguracionRobot.S2_Max;
        asset.pulsoMin_us       = ConfiguracionRobot.ServoPulsoMin_us;
        asset.pulsoMax_us       = ConfiguracionRobot.ServoPulsoMax_us;
        GuardarAsset(asset, "S2_Gripper");
    }

    // ─── HELPER ───────────────────────────────────────────────────────────────

    private static void GuardarAsset(ConfiguracionMotor asset, string nombreArchivo)
    {
        string rutaCompleta = $"{CarpetaDestino}/{nombreArchivo}.asset";

        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), rutaCompleta)))
        {
            bool sobreescribir = EditorUtility.DisplayDialog(
                "Asset ya existe",
                $"El asset '{nombreArchivo}.asset' ya existe.\n¿Sobreescribir?",
                "Sí", "No");

            if (!sobreescribir) { Object.DestroyImmediate(asset); return; }
            AssetDatabase.DeleteAsset(rutaCompleta);
        }

        AssetDatabase.CreateAsset(asset, rutaCompleta);
        Debug.Log($"[SCARA] Asset creado: {rutaCompleta}");
    }
}
