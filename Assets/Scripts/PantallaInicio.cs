using UnityEngine;

/// <summary>
/// Pantalla de presentación al inicio de la aplicación.
/// Se cierra al presionar cualquier tecla y muestra la interfaz principal.
/// Asignar al GameObject CanvasPresentacion desde el Inspector.
/// </summary>
public class PantallaInicio : MonoBehaviour
{
    [SerializeField] private Canvas canvasPresentacion;

    private void Start()
    {
        if (canvasPresentacion == null)
            canvasPresentacion = GetComponent<Canvas>();
    }

    private void Update()
    {
        if (Input.anyKeyDown)
            Cerrar();
    }

    private void Cerrar()
    {
        if (canvasPresentacion != null)
            canvasPresentacion.gameObject.SetActive(false);
    }
}
