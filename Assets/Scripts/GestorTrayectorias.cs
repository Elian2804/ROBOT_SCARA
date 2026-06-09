using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Módulo 2 — Trayectorias.
/// Flujo:
///   A) Estudiante mueve el robot con SimularPasoJ1/J2/Z desde PanelTrayectorias.
///   B) Unity convierte pasos → grados → XYZ (cinemática directa) en tiempo real.
///   C) "Guardar punto" registra {pasosJ1, pasosJ2, pasosZ} del momento actual.
///   D) Con ≥2 puntos, "Ejecutar trayectoria" reproduce la secuencia.
///   E) El panel muestra los pasos exactos de cada punto para replicar en el ESP32.
///   F) Las trayectorias se pueden nombrar ("PICK", "DROP") para el Módulo 3.
/// No hay entrada de coordenadas XYZ ni cinemática inversa.
/// </summary>
public class GestorTrayectorias : MonoBehaviour
{
    // ─── ESTRUCTURA DE PUNTO ──────────────────────────────────────────────────

    [System.Serializable]
    public struct PuntoTrayectoria
    {
        public int    pasosM1;   // Eje Z
        public int    pasosM2;   // J1
        public int    pasosM3;   // J2
        public string nombre;
    }

    // ─── REFERENCIAS ─────────────────────────────────────────────────────────
    [Header("Sistema")]
    [SerializeField] private GestorSCARA           gestorSCARA;
    [SerializeField] private PanelTrayectorias     panelTrayectorias;
    [SerializeField] private VisualizadorTrayectoria visualizador;

    [Header("Tiempos de ejecución")]
    [SerializeField] private float tiempoEntreMovimientos = 1.0f;
    [SerializeField] private float tiempoGrip             = 0.5f;

    [Header("Objetos de la escena")]
    [SerializeField] private ObjetoManipulable[] objetos;

    // ─── ESTADO INTERNO ───────────────────────────────────────────────────────
    private readonly List<PuntoTrayectoria>                       _puntosActuales   = new List<PuntoTrayectoria>();
    private readonly Dictionary<string, List<PuntoTrayectoria>>  _trayectorias     = new Dictionary<string, List<PuntoTrayectoria>>();

    private bool              _secuenciaActiva = false;
    private ObjetoManipulable _objetoActual    = null;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        if (panelTrayectorias != null)
        {
            panelTrayectorias.OnGuardarPunto        += GuardarPunto;
            panelTrayectorias.OnEjecutarTrayectoria += IniciarSecuencia;
            panelTrayectorias.OnLimpiarPuntos       += LimpiarPuntos;
            panelTrayectorias.OnGuardarComoNombre   += GuardarComoNombre;
            panelTrayectorias.OnReiniciarObjetos    += ReiniciarObjetos;
        }
    }

    private void OnDestroy()
    {
        if (panelTrayectorias != null)
        {
            panelTrayectorias.OnGuardarPunto        -= GuardarPunto;
            panelTrayectorias.OnEjecutarTrayectoria -= IniciarSecuencia;
            panelTrayectorias.OnLimpiarPuntos       -= LimpiarPuntos;
            panelTrayectorias.OnGuardarComoNombre   -= GuardarComoNombre;
            panelTrayectorias.OnReiniciarObjetos    -= ReiniciarObjetos;
        }
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>Registra la posición actual del robot como un nuevo punto.</summary>
    public void GuardarPunto()
    {
        if (gestorSCARA == null) return;

        PuntoTrayectoria nuevo = new PuntoTrayectoria
        {
            pasosM1 = gestorSCARA.PasosActualesZ,
            pasosM2 = gestorSCARA.PasosActualesJ1,
            pasosM3 = gestorSCARA.PasosActualesJ2,
            nombre  = $"P{_puntosActuales.Count + 1:D2}"
        };

        _puntosActuales.Add(nuevo);

        if (panelTrayectorias != null)
            panelTrayectorias.ActualizarListaPuntos(_puntosActuales);

        ActualizarVisualizador();
        Debug.Log($"[GestorTrayectorias] Punto guardado: {nuevo.nombre} M1={nuevo.pasosM1} M2={nuevo.pasosM2} M3={nuevo.pasosM3}");
    }

    /// <summary>Inicia la reproducción de los puntos guardados en orden.</summary>
    public void IniciarSecuencia()
    {
        if (_secuenciaActiva || gestorSCARA == null) return;
        if (_puntosActuales.Count < 2)
        {
            Debug.LogWarning("[GestorTrayectorias] Se necesitan al menos 2 puntos para ejecutar.");
            return;
        }
        StartCoroutine(CorrutinaSecuencia(_puntosActuales));
    }

    /// <summary>Elimina todos los puntos de la secuencia actual.</summary>
    public void LimpiarPuntos()
    {
        _puntosActuales.Clear();
        if (panelTrayectorias != null)
            panelTrayectorias.ActualizarListaPuntos(_puntosActuales);
        if (visualizador != null)
            visualizador.LimpiarLinea();
    }

    /// <summary>
    /// Guarda la secuencia actual con un nombre de trayectoria (ej: "PICK", "DROP").
    /// Usada por el Módulo 3 para ejecutar trayectorias por comando.
    /// </summary>
    public void GuardarComoNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || _puntosActuales.Count == 0) return;
        _trayectorias[nombre.ToUpper()] = new List<PuntoTrayectoria>(_puntosActuales);
        Debug.Log($"[GestorTrayectorias] Trayectoria '{nombre}' guardada con {_puntosActuales.Count} puntos.");
    }

    /// <summary>
    /// Ejecuta una trayectoria predefinida por nombre (ej: "PICK", "DROP").
    /// Retorna false si no existe esa trayectoria.
    /// </summary>
    public bool EjecutarTrayectoriaNombrada(string nombre)
    {
        if (_secuenciaActiva) return false;
        if (!_trayectorias.TryGetValue(nombre.ToUpper(), out var puntos))
        {
            Debug.LogWarning($"[GestorTrayectorias] Trayectoria '{nombre}' no existe.");
            return false;
        }
        StartCoroutine(CorrutinaSecuencia(puntos));
        return true;
    }

    /// <summary>Reinicia todos los objetos manipulables a su posición original.</summary>
    public void ReiniciarObjetos()
    {
        if (objetos == null) return;
        foreach (var obj in objetos)
            if (obj != null) obj.Reiniciar();
        _objetoActual = null;
    }

    // ─── CORRUTINA DE REPRODUCCIÓN ────────────────────────────────────────────

    private IEnumerator CorrutinaSecuencia(List<PuntoTrayectoria> puntos)
    {
        _secuenciaActiva = true;

        for (int i = 0; i < puntos.Count; i++)
        {
            PuntoTrayectoria pt = puntos[i];
            ActualizarFase($"Moviendo a {pt.nombre} ({i + 1}/{puntos.Count})");

            // Secuencia segura: subir Z → mover brazos → bajar Z al destino
            yield return StartCoroutine(MoverPuntoSeguro(pt));

            yield return new WaitForSeconds(tiempoEntreMovimientos);

            // Lógica de gripper basada en posición en la secuencia
            if (panelTrayectorias != null)
            {
                bool cerrarGripper = panelTrayectorias.DebeAgarrarEnPunto(i);
                bool abrirGripper  = panelTrayectorias.DebebSoltarEnPunto(i);

                if (cerrarGripper)
                {
                    gestorSCARA.CerrarGripper();
                    yield return new WaitForSeconds(tiempoGrip);
                    AgarrarObjetoCercano();
                }
                else if (abrirGripper)
                {
                    gestorSCARA.AbrirGripper();
                    yield return new WaitForSeconds(tiempoGrip);
                    SoltarObjeto();
                }
            }
        }

        ActualizarFase("Secuencia completada");
        if (panelTrayectorias != null) panelTrayectorias.RegistrarExito();
        _secuenciaActiva = false;
    }

    // ─── MOVIMIENTO SEGURO (sin colisión) ────────────────────────────────────

    private IEnumerator MoverPuntoSeguro(PuntoTrayectoria pt)
    {
        // Paso 1: subir Z al tope (Z=0 → brazo arriba, sin obstáculos)
        gestorSCARA.IrAPasos(gestorSCARA.PasosActualesJ1, gestorSCARA.PasosActualesJ2, 0);
        yield return new WaitUntil(() => gestorSCARA.EstaEnPosicion(toleranciaZ: 3f));

        // Paso 2: rotar brazos a la posición destino (con Z en posición segura)
        gestorSCARA.IrAPasos(pt.pasosM2, pt.pasosM3, 0);
        yield return new WaitUntil(() => gestorSCARA.EstaEnPosicion());

        // Paso 3: bajar Z al destino
        gestorSCARA.IrAPasos(pt.pasosM2, pt.pasosM3, pt.pasosM1);
        yield return new WaitUntil(() => gestorSCARA.EstaEnPosicion(toleranciaZ: 3f));
    }

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private void AgarrarObjetoCercano()
    {
        if (objetos == null || gestorSCARA == null) return;
        Transform tcp = gestorSCARA.ObtenerTransformTCP();
        if (tcp == null) return;

        float distMin = float.MaxValue;
        ObjetoManipulable masC = null;

        foreach (var obj in objetos)
        {
            if (obj == null || obj.Estado != ObjetoManipulable.EstadoObjeto.Libre) continue;
            float d = Vector3.Distance(obj.transform.position, tcp.position);
            if (d < distMin && obj.EstaCercaDe(tcp.position)) { distMin = d; masC = obj; }
        }

        if (masC != null)
        {
            masC.Agarrar(tcp);
            _objetoActual = masC;
        }
    }

    private void SoltarObjeto()
    {
        if (_objetoActual != null) { _objetoActual.Soltar(); _objetoActual = null; }
    }

    private void ActualizarFase(string fase)
    {
        if (panelTrayectorias != null) panelTrayectorias.ActualizarFase(fase);
        Debug.Log($"[GestorTrayectorias] {fase}");
    }

    private void ActualizarVisualizador()
    {
        if (visualizador == null || _puntosActuales.Count < 2) return;
        // Pasar los ángulos calculados para que el LineRenderer dibuje la ruta
        float[] j1s = new float[_puntosActuales.Count];
        float[] j2s = new float[_puntosActuales.Count];
        for (int i = 0; i < _puntosActuales.Count; i++)
        {
            j1s[i] = ConfiguracionRobot.PasosAGradosJ1(_puntosActuales[i].pasosM2);
            j2s[i] = ConfiguracionRobot.PasosAGradosJ2(_puntosActuales[i].pasosM3);
        }
        visualizador.ActualizarSecuencia(j1s, j2s);
    }
}
