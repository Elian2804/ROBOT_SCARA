using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel derecho exclusivo del Módulo 1 (Control Articular).
/// Muestra ÚNICAMENTE lo que manda el sketch del ESP32:
///   - Pasos acumulados por motor (sube y baja en tiempo real)
///   - Dirección actual (HIGH / LOW)
///   - Velocidad en pasos/segundo
/// No muestra ángulos, XYZ ni cinemática — eso pertenece al panel izquierdo.
/// Todas las referencias se asignan desde el Inspector.
/// </summary>
public class PanelControlArticular : MonoBehaviour
{
    // ─── M1 — EJE Z ──────────────────────────────────────────────────────────
    [Header("M1 — Eje Z")]
    [SerializeField] private TextMeshProUGUI textoM1_Pasos;
    [SerializeField] private TextMeshProUGUI textoM1_Direccion;
    [SerializeField] private TextMeshProUGUI textoM1_Velocidad;

    // ─── M2 — BRAZO 1 ─────────────────────────────────────────────────────────
    [Header("M2 — Brazo 1 (J1)")]
    [SerializeField] private TextMeshProUGUI textoM2_Pasos;
    [SerializeField] private TextMeshProUGUI textoM2_Direccion;
    [SerializeField] private TextMeshProUGUI textoM2_Velocidad;

    // ─── M3 — BRAZO 2 ─────────────────────────────────────────────────────────
    [Header("M3 — Brazo 2 (J2)")]
    [SerializeField] private TextMeshProUGUI textoM3_Pasos;
    [SerializeField] private TextMeshProUGUI textoM3_Direccion;
    [SerializeField] private TextMeshProUGUI textoM3_Velocidad;

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Inicializar valores por defecto. Llamado por GestorSCARA.</summary>
    public void Inicializar()
    {
        SetTexto(textoM1_Pasos,     "Pasos M1: 0");
        SetTexto(textoM2_Pasos,     "Pasos M2: 0");
        SetTexto(textoM3_Pasos,     "Pasos M3: 0");
        SetTexto(textoM1_Direccion, "DIR: —");
        SetTexto(textoM2_Direccion, "DIR: —");
        SetTexto(textoM3_Direccion, "DIR: —");
        SetTexto(textoM1_Velocidad, "Vel: 0 p/s");
        SetTexto(textoM2_Velocidad, "Vel: 0 p/s");
        SetTexto(textoM3_Velocidad, "Vel: 0 p/s");
    }

    /// <summary>
    /// Actualiza todos los campos con el estado actual del robot.
    /// Llamado por GestorSCARA en cada Update() cuando el módulo 1 está activo.
    /// </summary>
    public void ActualizarEstado(DatosRobot datos)
    {
        // Pasos acumulados
        SetTexto(textoM1_Pasos, $"Pasos M1: {datos.pasosZ:+0;-0;0}");
        SetTexto(textoM2_Pasos, $"Pasos M2: {datos.pasosJ1:+0;-0;0}");
        SetTexto(textoM3_Pasos, $"Pasos M3: {datos.pasosJ2:+0;-0;0}");

        // Dirección actual
        SetTexto(textoM1_Direccion, $"DIR: {FormatearDir(datos.dirM1)}");
        SetTexto(textoM2_Direccion, $"DIR: {FormatearDir(datos.dirM2)}");
        SetTexto(textoM3_Direccion, $"DIR: {FormatearDir(datos.dirM3)}");

        // Velocidad
        SetTexto(textoM1_Velocidad, $"Vel: {datos.velPasosM1:F0} p/s");
        SetTexto(textoM2_Velocidad, $"Vel: {datos.velPasosM2:F0} p/s");
        SetTexto(textoM3_Velocidad, $"Vel: {datos.velPasosM3:F0} p/s");
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private static string FormatearDir(int dir)
        => dir >= 0 ? "HIGH ▲" : "LOW ▼";

    private static void SetTexto(TextMeshProUGUI campo, string texto)
    {
        if (campo != null) campo.text = texto;
    }
}
