using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Visualiza en tiempo real las trayectorias del TCP (módulos 2 y 3).
/// - Línea roja: trayectoria REAL grabada frame a frame desde el TCP.
/// - Línea azul: trayectoria de REFERENCIA.
///     Modo geométrico: círculo/arco/recta/rectángulo configurado en Inspector.
///     Modo waypoints:  ruta calculada por IK entre puntos configurados en Play.
///                      Se activa con ActualizarReferenciaPorAngulos().
/// Auto-calibración: al generar la referencia por ángulos, calcula la escala y
/// rotación del sistema robot→mundo comparando la posición actual del TCP.
/// </summary>
public class VisualizadorTrayectoria : MonoBehaviour
{
    public enum TipoTrayectoria { Recta, Arco, Circulo, Rectangulo }

    // ─── REFERENCIAS ─────────────────────────────────────────────────────────
    [Header("Referencia al TCP del robot")]
    [SerializeField] private Transform transformTCP;

    [Header("GestorSCARA")]
    [SerializeField] private GestorSCARA gestorSCARA;

    [Header("Panel de Trayectorias (módulo 2)")]
    [SerializeField] private PanelTrayectorias panelTrayectorias;

    // ─── LINE RENDERERS ───────────────────────────────────────────────────────
    [Header("LineRenderers (crear en la escena y arrastrar)")]
    [SerializeField] private LineRenderer lineaTrayectoriaReal;
    [SerializeField] private LineRenderer lineaTrayectoriaRef;

    // ─── APARIENCIA ───────────────────────────────────────────────────────────
    [Header("Apariencia")]
    [SerializeField] private Color colorReal       = new Color(0.95f, 0.25f, 0.25f, 0.9f);
    [SerializeField] private Color colorReferencia = new Color(0.20f, 0.55f, 1.00f, 0.7f);
    [SerializeField] private float anchoLinea      = 10f;

    // ─── GRABACIÓN REAL ───────────────────────────────────────────────────────
    [Header("Grabación de trayectoria real")]
    [SerializeField] private bool  grabarAlInicio       = true;
    [SerializeField] private float distanciaMinRegistro = 0.04f;
    [SerializeField] private int   maxPuntosReales      = 500;

    // ─── REFERENCIA GEOMÉTRICA ────────────────────────────────────────────────
    [Header("Trayectoria de referencia (modo geométrico)")]
    [SerializeField] private TipoTrayectoria tipoReferencia   = TipoTrayectoria.Circulo;
    [SerializeField] private Vector3         centroReferencia = Vector3.zero;
    [SerializeField] private float           radioReferencia  = 100f;
    [SerializeField] private float           anchoRectangulo  = 100f;
    [SerializeField] private float           altoRectangulo   = 100f;
    [SerializeField] private Vector3         puntoA           = Vector3.zero;
    [SerializeField] private Vector3         puntoB           = new Vector3(0.3f, 0f, 0f);
    [SerializeField] private float           anguloInicioArco = 0f;
    [SerializeField] private float           anguloFinArco    = 180f;
    [SerializeField] private int             segmentosRef     = 64;
    [SerializeField] private bool            mostrarReferencia = true;
    [SerializeField] private bool            mostrarReal       = true;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private readonly List<Vector3> _puntosReales = new List<Vector3>();
    private Vector3 _ultimoPunto = Vector3.positiveInfinity;
    private bool    _grabando   = false;
    private bool    _modoWaypoints = false;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Awake()
    {
        ConfigurarLineaReal();
        ConfigurarLineaRef();
    }

    private void Start()
    {
        GenerarTrayectoriaReferencia();
        if (grabarAlInicio) IniciarGrabacion();
    }

    private void LateUpdate()
    {
        if (_grabando && transformTCP != null)
            RegistrarPunto(transformTCP.position);

        ActualizarLineaReal();
        ActualizarDesviacion();
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    public void IniciarGrabacion()  { _grabando = true; }
    public void DetenerGrabacion()  { _grabando = false; }

    /// <summary>Alias de LimpiarTrayectoriaReal. Llamado por GestorTrayectorias.</summary>
    public void LimpiarLinea() => LimpiarTrayectoriaReal();

    /// <summary>
    /// Genera la línea de referencia a partir de arrays de ángulos J1/J2 (puntos grabados).
    /// Llamado por GestorTrayectorias al actualizar la lista de puntos.
    /// </summary>
    public void ActualizarSecuencia(float[] j1s, float[] j2s)
    {
        if (lineaTrayectoriaRef == null || j1s == null || j1s.Length < 2) return;

        Transform pivot = gestorSCARA != null ? gestorSCARA.ObtenerTransformPivotArt1() : null;
        float yMundo = transformTCP != null ? transformTCP.position.y : 0f;

        var pts = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < j1s.Length; i++)
        {
            float j1r = j1s[i] * Mathf.Deg2Rad;
            float j2r = (j1s[i] + j2s[i]) * Mathf.Deg2Rad;
            float xMm = ConfiguracionRobot.Eslablon1 * Mathf.Cos(j1r)
                       + ConfiguracionRobot.Eslablon2 * Mathf.Cos(j2r);
            float yMm = ConfiguracionRobot.Eslablon1 * Mathf.Sin(j1r)
                       + ConfiguracionRobot.Eslablon2 * Mathf.Sin(j2r);

            if (pivot != null && gestorSCARA != null)
            {
                DatosRobot est = gestorSCARA.ObtenerEstado();
                CalibracionMundo cal = Calibrar(pivot, est.tcpX_mm, est.tcpY_mm);
                pts.Add(MMaWorld(pivot.position, xMm, yMm, yMundo, cal));
            }
            else
            {
                // Sin calibración: escala directa mm → Unity
                pts.Add(new Vector3(xMm * ConfiguracionRobot.MM_A_UNITY, yMundo,
                                    yMm * ConfiguracionRobot.MM_A_UNITY));
            }
        }

        lineaTrayectoriaRef.positionCount = pts.Count;
        lineaTrayectoriaRef.SetPositions(pts.ToArray());
        lineaTrayectoriaRef.enabled = true;
        _modoWaypoints = true;
    }

    public void LimpiarTrayectoriaReal()
    {
        _puntosReales.Clear();
        _ultimoPunto = Vector3.positiveInfinity;
        if (lineaTrayectoriaReal != null) lineaTrayectoriaReal.positionCount = 0;
    }

    /// <summary>
    /// Genera la línea azul en modo geométrico con los parámetros del Inspector.
    /// </summary>
    public void GenerarTrayectoriaReferencia()
    {
        _modoWaypoints = false;
        if (lineaTrayectoriaRef == null) return;

        var pts = new List<Vector3>();

        switch (tipoReferencia)
        {
            case TipoTrayectoria.Recta:
                pts.Add(puntoA); pts.Add(puntoB);
                break;

            case TipoTrayectoria.Arco:
                for (int i = 0; i <= segmentosRef; i++)
                {
                    float a = Mathf.Lerp(anguloInicioArco, anguloFinArco,
                                         (float)i / segmentosRef) * Mathf.Deg2Rad;
                    pts.Add(centroReferencia + new Vector3(
                        Mathf.Cos(a) * radioReferencia, 0f,
                        Mathf.Sin(a) * radioReferencia));
                }
                break;

            case TipoTrayectoria.Circulo:
                for (int i = 0; i <= segmentosRef; i++)
                {
                    float a = (float)i / segmentosRef * Mathf.PI * 2f;
                    pts.Add(centroReferencia + new Vector3(
                        Mathf.Cos(a) * radioReferencia, 0f,
                        Mathf.Sin(a) * radioReferencia));
                }
                break;

            case TipoTrayectoria.Rectangulo:
                Vector3 h = new Vector3(anchoRectangulo * .5f, 0f, altoRectangulo * .5f);
                pts.Add(centroReferencia + new Vector3(-h.x, 0, -h.z));
                pts.Add(centroReferencia + new Vector3( h.x, 0, -h.z));
                pts.Add(centroReferencia + new Vector3( h.x, 0,  h.z));
                pts.Add(centroReferencia + new Vector3(-h.x, 0,  h.z));
                pts.Add(centroReferencia + new Vector3(-h.x, 0, -h.z));
                break;
        }

        lineaTrayectoriaRef.positionCount = pts.Count;
        lineaTrayectoriaRef.SetPositions(pts.ToArray());
        lineaTrayectoriaRef.enabled = mostrarReferencia;
    }

    /// <summary>
    /// Genera la línea azul interpolando la ruta IK entre los ángulos de recogida y depósito.
    /// Se auto-calibra leyendo la posición actual del TCP para mapear mm→mundo.
    /// Llamado por GestorTrayectorias cuando el estudiante presiona Aplicar.
    /// </summary>
    public void ActualizarReferenciaPorAngulos(float j1Rec, float j2Rec,
                                               float j1Dep, float j2Dep,
                                               int segmentos = 48)
    {
        if (lineaTrayectoriaRef == null || gestorSCARA == null || transformTCP == null)
            return;

        Transform pivot = gestorSCARA.ObtenerTransformPivotArt1();
        if (pivot == null) return;

        // Auto-calibración: escala y rotación robot-frame → mundo
        DatosRobot est = gestorSCARA.ObtenerEstado();
        CalibracionMundo cal = Calibrar(pivot, est.tcpX_mm, est.tcpY_mm);

        float yMundo = transformTCP.position.y;
        var pts = new List<Vector3>();

        for (int i = 0; i <= segmentos; i++)
        {
            float t  = (float)i / segmentos;
            float j1 = Mathf.Lerp(j1Rec, j1Dep, t);
            float j2 = Mathf.Lerp(j2Rec, j2Dep, t);

            float j1r = j1 * Mathf.Deg2Rad;
            float j2r = (j1 + j2) * Mathf.Deg2Rad;
            float xMm = ConfiguracionRobot.Eslablon1 * Mathf.Cos(j1r)
                       + ConfiguracionRobot.Eslablon2 * Mathf.Cos(j2r);
            float yMm = ConfiguracionRobot.Eslablon1 * Mathf.Sin(j1r)
                       + ConfiguracionRobot.Eslablon2 * Mathf.Sin(j2r);

            pts.Add(MMaWorld(pivot.position, xMm, yMm, yMundo, cal));
        }

        lineaTrayectoriaRef.positionCount = pts.Count;
        lineaTrayectoriaRef.SetPositions(pts.ToArray());
        lineaTrayectoriaRef.enabled = true;
        _modoWaypoints = true;
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private struct CalibracionMundo
    {
        public float escala;
        public float offsetRotacion;
    }

    private CalibracionMundo Calibrar(Transform pivot, float tcpXmm, float tcpYmm)
    {
        Vector3 delta    = transformTCP.position - pivot.position;
        float distMm     = Mathf.Sqrt(tcpXmm * tcpXmm + tcpYmm * tcpYmm);
        float distWorld  = new Vector2(delta.x, delta.z).magnitude;

        float escala     = distMm > 0.5f ? distWorld / distMm : 1f;
        float mmAngle    = Mathf.Atan2(tcpYmm, tcpXmm) * Mathf.Rad2Deg;
        float worldAngle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;

        return new CalibracionMundo
        {
            escala         = escala,
            offsetRotacion = worldAngle - mmAngle
        };
    }

    private Vector3 MMaWorld(Vector3 pivotPos, float xMm, float yMm,
                              float yMundo, CalibracionMundo cal)
    {
        float dist  = Mathf.Sqrt(xMm * xMm + yMm * yMm) * cal.escala;
        float angle = (Mathf.Atan2(yMm, xMm) * Mathf.Rad2Deg + cal.offsetRotacion)
                      * Mathf.Deg2Rad;
        return new Vector3(
            pivotPos.x + dist * Mathf.Cos(angle),
            yMundo,
            pivotPos.z + dist * Mathf.Sin(angle));
    }

    private void RegistrarPunto(Vector3 pos)
    {
        if (!float.IsPositiveInfinity(_ultimoPunto.x) &&
            Vector3.Distance(pos, _ultimoPunto) < distanciaMinRegistro)
            return;

        _puntosReales.Add(pos);
        _ultimoPunto = pos;

        if (_puntosReales.Count > maxPuntosReales)
            _puntosReales.RemoveAt(0);
    }

    private void ActualizarLineaReal()
    {
        if (lineaTrayectoriaReal == null) return;
        lineaTrayectoriaReal.positionCount = _puntosReales.Count;
        if (_puntosReales.Count > 0)
            lineaTrayectoriaReal.SetPositions(_puntosReales.ToArray());
        lineaTrayectoriaReal.enabled = mostrarReal;
    }

    private void ActualizarDesviacion()
    {
        if (panelTrayectorias == null || transformTCP == null) return;
        if (lineaTrayectoriaRef == null || lineaTrayectoriaRef.positionCount < 2) return;

        float desv   = DesviacionAlSegmentoMasCercano(transformTCP.position);
        float desvMm = _modoWaypoints
            ? desv / (gestorSCARA != null ? ObtenerEscalaActual() : 1f)
            : desv / ConfiguracionRobot.MM_A_UNITY;

        panelTrayectorias.ActualizarDesviacion(desvMm);
    }

    private float ObtenerEscalaActual()
    {
        Transform pivot = gestorSCARA.ObtenerTransformPivotArt1();
        if (pivot == null) return 1f;
        DatosRobot est = gestorSCARA.ObtenerEstado();
        var cal = Calibrar(pivot, est.tcpX_mm, est.tcpY_mm);
        return cal.escala;
    }

    private float DesviacionAlSegmentoMasCercano(Vector3 p)
    {
        int n = lineaTrayectoriaRef.positionCount;
        float min = float.MaxValue;
        for (int i = 0; i < n - 1; i++)
        {
            float d = DistPuntoSegmento(p,
                lineaTrayectoriaRef.GetPosition(i),
                lineaTrayectoriaRef.GetPosition(i + 1));
            if (d < min) min = d;
        }
        return min;
    }

    private float DistPuntoSegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float   sq = ab.sqrMagnitude;
        if (sq < 0.00001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sq);
        return Vector3.Distance(p, a + t * ab);
    }

    private void ConfigurarLineaReal()
    {
        if (lineaTrayectoriaReal == null)
        {
            lineaTrayectoriaReal = new GameObject("LineaTrayectoriaReal")
                .AddComponent<LineRenderer>();
        }
        AplicarEstiloLinea(lineaTrayectoriaReal, colorReal);
    }

    private void ConfigurarLineaRef()
    {
        if (lineaTrayectoriaRef == null)
        {
            lineaTrayectoriaRef = new GameObject("LineaTrayectoriaReferencia")
                .AddComponent<LineRenderer>();
        }
        AplicarEstiloLinea(lineaTrayectoriaRef, colorReferencia);
    }

    private void AplicarEstiloLinea(LineRenderer lr, Color color)
    {
        lr.material        = new Material(Shader.Find("Sprites/Default"));
        lr.startColor      = color;
        lr.endColor        = color;
        lr.startWidth      = anchoLinea;
        lr.endWidth        = anchoLinea;
        lr.useWorldSpace   = true;
        lr.positionCount   = 0;
    }
}
