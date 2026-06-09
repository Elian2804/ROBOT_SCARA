using UnityEngine;

/// <summary>
/// Añade y configura automáticamente los BoxColliders de las garras en runtime.
/// También detecta contacto con ObjetoManipulable y notifica a GestorSCARA.
/// Agregar al mismo GameObject que GestorSCARA y asignar Garra_1 y Garra_2.
/// </summary>
public class ConfiguradorFisicaGripper : MonoBehaviour
{
    [Header("Garras (arrastrar desde jerarquía)")]
    [SerializeField] private Transform garra1;
    [SerializeField] private Transform garra2;

    [Header("Tamaño del collider de cada dedo")]
    [SerializeField] private Vector3 tamanoCollider = new Vector3(0.02f, 0.05f, 0.03f);
    [SerializeField] private Vector3 centroCollider  = Vector3.zero;

    [Header("Sistema")]
    [SerializeField] private GestorSCARA gestorSCARA;

    // Colliders creados en runtime
    private BoxCollider _colGarra1;
    private BoxCollider _colGarra2;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        ConfigurarGarra(garra1, ref _colGarra1, "Detector_Garra1");
        ConfigurarGarra(garra2, ref _colGarra2, "Detector_Garra2");
    }

    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────

    private void ConfigurarGarra(Transform garra, ref BoxCollider colRef, string nombreDetector)
    {
        if (garra == null) return;

        // Crear un hijo para el detector (no modificar el transform de la garra)
        GameObject detector = new GameObject(nombreDetector);
        detector.transform.SetParent(garra, false);
        detector.transform.localPosition = Vector3.zero;
        detector.transform.localRotation = Quaternion.identity;
        detector.layer = garra.gameObject.layer;

        // Collider trigger — solo para detectar agarre/suelta
        BoxCollider col = detector.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size   = tamanoCollider;
        col.center = centroCollider;
        colRef = col;

        // Collider sólido — para empujar físicamente objetos con Rigidbody
        BoxCollider colFisico = detector.AddComponent<BoxCollider>();
        colFisico.isTrigger = false;
        colFisico.size      = tamanoCollider;
        colFisico.center    = centroCollider;

        // Rigidbody isKinematic: necesario para trigger Y para que Unity procese colisiones
        var rb = detector.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Añadir el receptor de eventos
        var receptor = detector.AddComponent<ReceptorTriggerGarra>();
        receptor.Inicializar(gestorSCARA);
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>Actualiza el tamaño de ambos colliders en runtime (útil desde Inspector en Play).</summary>
    public void ActualizarTamano(Vector3 nuevoTamano)
    {
        tamanoCollider = nuevoTamano;
        if (_colGarra1 != null) _colGarra1.size = nuevoTamano;
        if (_colGarra2 != null) _colGarra2.size = nuevoTamano;
    }

    /// <summary>Muestra los colliders en el editor como gizmos de color cyan.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        DibujarGizmoGarra(garra1);
        DibujarGizmoGarra(garra2);
    }

    private void DibujarGizmoGarra(Transform garra)
    {
        if (garra == null) return;
        Vector3 centro = garra.TransformPoint(centroCollider);
        // Usar lossyScale con valor absoluto para evitar escala negativa (espejo FBX)
        Vector3 escala = garra.lossyScale;
        Vector3 tamano = new Vector3(
            Mathf.Abs(escala.x) * tamanoCollider.x,
            Mathf.Abs(escala.y) * tamanoCollider.y,
            Mathf.Abs(escala.z) * tamanoCollider.z);
        Gizmos.matrix = Matrix4x4.TRS(centro, garra.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamano);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
