using UnityEngine;

/// <summary>
/// Añadir a Garra_1 O Garra_2 (una sola basta).
/// Requiere BoxCollider con IsTrigger = true en el mismo GameObject.
/// Detecta cuándo el gripper toca un ObjetoManipulable y lo reporta al GestorSCARA.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DetectorContactoGarra : MonoBehaviour
{
    [SerializeField] private GestorSCARA gestorSCARA;

    private void Awake()
    {
        // Asegurarse de que el collider es trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var obj = other.GetComponent<ObjetoManipulable>();
        if (obj == null) return;

        // Notificar al gestor para que intente agarrar
        gestorSCARA?.NotificarContactoGarra(obj);
    }

    private void OnTriggerExit(Collider other)
    {
        var obj = other.GetComponent<ObjetoManipulable>();
        if (obj == null) return;

        gestorSCARA?.NotificarSalidaGarra(obj);
    }
}
