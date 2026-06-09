using UnityEngine;
using UnityEditor;

/// <summary>
/// Inspector personalizado para ConfiguracionMotor.
/// Muestra valores calculados usando la lógica de conversión por Pasos180
/// y oculta campos irrelevantes según el tipo de actuador.
/// </summary>
[CustomEditor(typeof(ConfiguracionMotor))]
public class InspectorConfiguracionMotor : Editor
{
    public override void OnInspectorGUI()
    {
        ConfiguracionMotor motor = (ConfiguracionMotor)target;
        serializedObject.Update();

        // ─── IDENTIFICACIÓN ───────────────────────────────────────────────────
        EditorGUILayout.LabelField("Identificación", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nombreActuador"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tipo"));

        EditorGUILayout.Space(8);

        // ─── PINES ────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Pines ESP32", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pinSenal"));

        if (motor.tipo == TipoActuador.Stepper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pinDir"));

        EditorGUILayout.Space(8);

        // ─── PARÁMETROS SEGÚN TIPO ────────────────────────────────────────────
        if (motor.tipo == TipoActuador.Stepper)
        {
            EditorGUILayout.LabelField("Parámetros Stepper", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocidadMaxPasos"));
        }
        else
        {
            EditorGUILayout.LabelField("Parámetros Servo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pulsoMin_us"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pulsoMax_us"));
        }

        EditorGUILayout.Space(8);

        // ─── LÍMITES ──────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Límites", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("limiteMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("limiteMax"));

        EditorGUILayout.Space(12);

        // ─── PANEL DE VALORES CALCULADOS (solo lectura) ───────────────────────
        EditorGUILayout.LabelField("Valores calculados (runtime)", EditorStyles.boldLabel);

        GUI.enabled = false;

        if (motor.tipo == TipoActuador.Stepper)
        {
            // Eje Z: usa MmPorPaso_Z fijo
            bool esEjeZ = motor.limiteMax > 50f;   // heurística: Z tiene rango en mm (0-200)

            if (esEjeZ)
            {
                float mmPorPaso  = ConfiguracionRobot.MmPorPaso_Z;
                float pasosPorMm = 1f / mmPorPaso;
                float pasosRango = (motor.limiteMax - motor.limiteMin) * pasosPorMm;

                EditorGUILayout.FloatField("mm por paso (Z)", mmPorPaso);
                EditorGUILayout.FloatField("Pasos por mm (Z)", pasosPorMm);
                EditorGUILayout.FloatField("Pasos en rango total", pasosRango);
            }
            else
            {
                // J1 o J2: usa Pasos180 configurable en runtime
                float pasos180J1 = ConfiguracionRobot.Pasos180_J1;
                float pasos180J2 = ConfiguracionRobot.Pasos180_J2;
                float gradosPorPasoJ1 = 180f / pasos180J1;
                float gradosPorPasoJ2 = 180f / pasos180J2;

                EditorGUILayout.FloatField("Pasos para 180° (J1 runtime)", pasos180J1);
                EditorGUILayout.FloatField("°/paso (J1)", gradosPorPasoJ1);
                EditorGUILayout.FloatField("Pasos para 180° (J2 runtime)", pasos180J2);
                EditorGUILayout.FloatField("°/paso (J2)", gradosPorPasoJ2);
                float pasosRangoJ1 = (motor.limiteMax - motor.limiteMin) / gradosPorPasoJ1;
                EditorGUILayout.FloatField("Pasos en rango total (J1)", pasosRangoJ1);
            }

            if (motor.velocidadMaxPasos > 0f)
            {
                float mmS = motor.velocidadMaxPasos * ConfiguracionRobot.MmPorPaso_Z;
                EditorGUILayout.FloatField("Vel. máx mm/s (si es Z)", mmS);
            }
        }
        else
        {
            float rangoAngular = motor.limiteMax - motor.limiteMin;
            float rangoPulso   = motor.pulsoMax_us - motor.pulsoMin_us;
            float usPorGrado   = rangoAngular > 0f ? rangoPulso / rangoAngular : 0f;

            EditorGUILayout.FloatField("Rango angular (°)", rangoAngular);
            EditorGUILayout.FloatField("µs por grado", usPorGrado);
        }

        GUI.enabled = true;

        // ─── AVISOS DE VALIDACIÓN ─────────────────────────────────────────────
        EditorGUILayout.Space(4);

        if (motor.limiteMin >= motor.limiteMax)
        {
            EditorGUILayout.HelpBox(
                "El límite mínimo debe ser menor que el máximo.",
                MessageType.Error);
        }

        if (motor.tipo == TipoActuador.Stepper && motor.pinSenal != 0
            && !ConfiguracionRobot.EsPinValido(motor.pinSenal))
        {
            EditorGUILayout.HelpBox(
                $"Pin señal {motor.pinSenal} no está en la tabla de pines válidos del robot.",
                MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
