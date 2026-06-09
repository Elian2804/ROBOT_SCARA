using UnityEngine;

/// <summary>
/// Creado en runtime por ConfiguradorFisicaGripper.
/// No añadir manualmente — se instancia automáticamente.
/// </summary>
public class ReceptorTriggerGarra : MonoBehaviour
{
    private GestorSCARA _gestorSCARA;

    public void Inicializar(GestorSCARA gestor)
    {
        _gestorSCARA = gestor;
    }

    private void OnTriggerEnter(Collider other)
    {
        var obj = other.GetComponent<ObjetoManipulable>();
        if (obj != null) _gestorSCARA?.NotificarContactoGarra(obj);
    }

    private void OnTriggerExit(Collider other)
    {
        var obj = other.GetComponent<ObjetoManipulable>();
        if (obj != null) _gestorSCARA?.NotificarSalidaGarra(obj);
    }
}
