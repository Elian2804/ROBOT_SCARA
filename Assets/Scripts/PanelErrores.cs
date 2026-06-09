using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel flotante de errores activos en la esquina superior derecha.
/// Lista los errores y advertencias actuales (E01-E06, A01-A08)
/// con su código, mensaje y contador de ocurrencias en la sesión.
/// Todas las referencias se asignan desde el Inspector.
/// </summary>
public class PanelErrores : MonoBehaviour
{
    // ─── REFERENCIAS (asignar desde Inspector) ────────────────────────────────
    [Header("Contenedor de errores")]
    [SerializeField] private Transform       contenedorErrores;     // VerticalLayoutGroup
    [SerializeField] private GameObject      prefabItemError;       // Prefab: código + mensaje + contador
    [SerializeField] private TextMeshProUGUI textoContadorSesion;   // "X errores en sesión"
    [SerializeField] private Button          botonLimpiarErrores;

    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────
    [Header("Configuración")]
    [SerializeField] private int maxErroresVisibles = 10;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    // Diccionario: código → (itemUI, contador)
    private Dictionary<string, ErrorItem> _erroresActivos = new Dictionary<string, ErrorItem>();
    private int _contadorSesion = 0;

    private class ErrorItem
    {
        public GameObject      objeto;
        public TextMeshProUGUI textoCodigo;
        public TextMeshProUGUI textoMensaje;
        public TextMeshProUGUI textoContador;
        public Image           imagenFondo;
        public int             ocurrencias;
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Inicializar el panel. Llamado por GestorSCARA.</summary>
    public void Inicializar()
    {
        _contadorSesion = 0;
        LimpiarErrores();
    }

    /// <summary>
    /// Agrega o actualiza un error/advertencia en el panel.
    /// Si el código ya existe, incrementa el contador. Si no, crea una entrada nueva.
    /// </summary>
    public void AgregarError(string codigo, string mensaje)
    {
        _contadorSesion++;

        if (_erroresActivos.TryGetValue(codigo, out ErrorItem existente))
        {
            existente.ocurrencias++;
            if (existente.textoContador != null)
                existente.textoContador.text = $"×{existente.ocurrencias}";
        }
        else
        {
            if (contenedorErrores == null || prefabItemError == null) return;

            // Limitar número de items visibles
            if (_erroresActivos.Count >= maxErroresVisibles)
                EliminarErrorMasAntiguo();

            GameObject nuevoItem = Instantiate(prefabItemError, contenedorErrores);
            ErrorItem item = new ErrorItem
            {
                objeto       = nuevoItem,
                ocurrencias  = 1
            };

            item.textoCodigo   = nuevoItem.transform.Find("TextoCodigo")?.GetComponent<TextMeshProUGUI>();
            item.textoMensaje  = nuevoItem.transform.Find("TextoMensaje")?.GetComponent<TextMeshProUGUI>();
            item.textoContador = nuevoItem.transform.Find("TextoContador")?.GetComponent<TextMeshProUGUI>();
            item.imagenFondo   = nuevoItem.GetComponent<Image>();

            if (item.textoCodigo  != null) item.textoCodigo.text  = codigo;
            if (item.textoMensaje != null) item.textoMensaje.text  = mensaje;
            if (item.textoContador != null) item.textoContador.text = "×1";

            // Color según si es error o advertencia
            bool esError = codigo.StartsWith("E");
            Color colorFondo = esError ? ColoresUI.Error : ColoresUI.Advertencia;
            colorFondo.a = 0.25f;

            if (item.imagenFondo != null) item.imagenFondo.color = colorFondo;
            if (item.textoCodigo != null)
                item.textoCodigo.color = esError ? ColoresUI.Error : ColoresUI.Advertencia;

            _erroresActivos[codigo] = item;
        }

        ActualizarContadorSesion();
    }

    /// <summary>Elimina un error específico del panel. Llamar al resolver el error.</summary>
    public void LimpiarError(string codigo)
    {
        if (_erroresActivos.TryGetValue(codigo, out ErrorItem item))
        {
            if (item.objeto != null) Destroy(item.objeto);
            _erroresActivos.Remove(codigo);
        }
    }

    // ─── MÉTODO PARA ASIGNAR DESDE INSPECTOR ─────────────────────────────────

    /// <summary>Limpia todos los errores activos. Asignar al botonLimpiarErrores.</summary>
    public void AlPresionarLimpiar()
    {
        LimpiarErrores();
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void LimpiarErrores()
    {
        if (contenedorErrores != null)
        {
            foreach (Transform hijo in contenedorErrores)
                Destroy(hijo.gameObject);
        }
        _erroresActivos.Clear();
        _contadorSesion = 0;
        ActualizarContadorSesion();
    }

    private void EliminarErrorMasAntiguo()
    {
        // Eliminar el primero del diccionario (más antiguo)
        foreach (var par in _erroresActivos)
        {
            if (par.Value.objeto != null) Destroy(par.Value.objeto);
            _erroresActivos.Remove(par.Key);
            break;
        }
    }

    private void ActualizarContadorSesion()
    {
        if (textoContadorSesion != null)
            textoContadorSesion.text = $"{_contadorSesion} errores en sesión";
    }
}
