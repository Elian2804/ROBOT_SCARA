using UnityEngine;
using UnityEditor;

/// <summary>
/// Elimina todos los componentes con script faltante de la escena activa.
/// Ejecutar UNA VEZ desde SCARA → Limpiar Scripts Faltantes, luego borrar este archivo.
/// </summary>
public static class LimpiarScriptsFaltantes
{
    [MenuItem("SCARA/Limpiar Scripts Faltantes")]
    public static void Limpiar()
    {
        int eliminados = 0;
        GameObject[] todos = Object.FindObjectsOfType<GameObject>();

        foreach (GameObject go in todos)
        {
            SerializedObject so = new SerializedObject(go);
            SerializedProperty componentes =
                so.FindProperty("m_Component");

            for (int i = componentes.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty comp =
                    componentes.GetArrayElementAtIndex(i)
                               .FindPropertyRelative("component");

                if (comp.objectReferenceValue == null)
                {
                    componentes.DeleteArrayElementAtIndex(i);
                    eliminados++;
                }
            }
            so.ApplyModifiedProperties();
        }

        Debug.Log($"[SCARA] Scripts faltantes eliminados: {eliminados}");
        EditorUtility.DisplayDialog("Limpieza completada",
            $"Se eliminaron {eliminados} componente(s) con script faltante.",
            "Aceptar");
    }
}
