using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Acumula y analiza las tramas T: recibidas por GestorSCARA durante la sesión.
/// Va en GestorPrincipal (mismo GO que GestorSCARA). Se conecta via GetComponent.
/// No toca el hilo serial: todo llega desde el hilo principal de Unity.
/// </summary>
public class AnalizadorSketch : MonoBehaviour
{
    // ─── ESTRUCTURAS DE DATOS PÚBLICAS (leídas por PanelAnalisisSketch) ────────

    public class InfoPin
    {
        public int    pin;
        public string nombreActuador;
        public string tipo;             // "Stepper DIR", "Stepper STEP", "Servo", "Desconocido"
        public int    cantidadCambios;
        public bool   esValido;
    }

    public class InfoMotor
    {
        public string nombreMotor;
        public int    pasosTotales;
        public float  velocidadPromedio_pps;
        public float  velocidadMax_pps;
        public int    movimientoResultante;  // pasos netos (positivos - negativos)
        public string direccionPredominante; // "positiva", "negativa", "equilibrada"
    }

    public class InfoServo
    {
        public string nombreServo;
        public float  ultimoPulso_us;
        public float  anguloCalculado;
    }

    public class InfoPatrones
    {
        public bool   esBucle;
        public int    pasosBucle;           // pasos contados en el bucle detectado
        public bool   esSimultaneo;
        public string motoresSimultaneos;   // "M2 + M3"
        public bool   esSecuenciaCoordinada;
    }

    public class InfoAdvertencia
    {
        public string codigo;
        public string descripcion;
        public Color  color;
    }

    public class InfoResumen
    {
        public float  duracionSegundos;
        public int    totalEventos;
        public int    totalPasos;
        public string motorMasActivo;
        public float  velocidadMaxRegistrada;
        public List<InfoAdvertencia> advertencias = new List<InfoAdvertencia>();
    }

    // ─── ESTADO INTERNO DE SEGUIMIENTO ────────────────────────────────────────

    private class EstadoStepper
    {
        public string nombre;
        public int    pinStep;
        public int    pinDir;
        public bool   esLineal;
        public int    dirActual             = 1;
        public bool   dirRecibidaAntesPrimerStep = false;
        public bool   primerStepRecibido    = false;
        public long   tsHighStep            = -1;
        public int    pasosPositivos        = 0;
        public int    pasosNegativos        = 0;
        public List<float> intervalos_us   = new List<float>();
        public float  velMax_pps            = 0f;
        public long   ultimaActividad_us    = -1;
    }

    private class EstadoServo
    {
        public string nombre;
        public int    pin;
        public long   tsHigh           = -1;
        public float  ultimoPulso_us   = 0f;
        public float  anguloCalculado  = 0f;
        public long   ultimaActividad  = -1;
    }

    // ─── ESTADO PRIVADO ────────────────────────────────────────────────────────

    // Mapa pin → InfoPin (todos los pines vistos)
    private readonly Dictionary<int, InfoPin> _pinesVistos = new Dictionary<int, InfoPin>();

    // Steppers por pin STEP (no DIR)
    private readonly Dictionary<int, EstadoStepper> _steppers = new Dictionary<int, EstadoStepper>();

    // Servos por pin
    private readonly Dictionary<int, EstadoServo> _servos = new Dictionary<int, EstadoServo>();

    // Timestamps globales
    private long _tsInicio     = -1;
    private long _tsUltimo     = 0;
    private int  _totalEventos = 0;

    // Para detección de movimiento simultáneo (STEP HIGH events recientes)
    private readonly List<(long ts, int pin)> _stepRecientes = new List<(long, int)>();
    private const long VentanaSimultaneo_us = 500L;  // 500µs

    // Para detección de secuencia coordinada (última actividad por grupo)
    private long _ultimaActivZ    = -1;
    private long _ultimaActivBraz = -1;  // J1 o J2
    private long _ultimaActivServ = -1;
    private const long VentanaCoord_us = 5_000_000L;  // 5s

    // Advertencias acumuladas (sin duplicados por código)
    private readonly Dictionary<string, InfoAdvertencia> _advertencias =
        new Dictionary<string, InfoAdvertencia>();

    private GestorSCARA _gestor;

    // ─── CICLO DE VIDA ─────────────────────────────────────────────────────────

    private void Start()
    {
        _gestor = GetComponent<GestorSCARA>();
        if (_gestor != null)
            _gestor.SuscribirAnalizador(AlRecibirTramaT);

        InicializarMapaSteppers();
        InicializarMapaServos();
    }

    // ─── INICIALIZACIÓN ────────────────────────────────────────────────────────

    private void InicializarMapaSteppers()
    {
        AgregarStepper("M1 - Eje Z",       ConfiguracionRobot.M1_PinSenal, ConfiguracionRobot.M1_PinDir, esLineal: true);
        AgregarStepper("M2 - Brazo 1 (J1)",ConfiguracionRobot.M2_PinSenal, ConfiguracionRobot.M2_PinDir, esLineal: false);
        AgregarStepper("M3 - Brazo 2 (J2)",ConfiguracionRobot.M3_PinSenal, ConfiguracionRobot.M3_PinDir, esLineal: false);
    }

    private void AgregarStepper(string nombre, int pinStep, int pinDir, bool esLineal)
    {
        _steppers[pinStep] = new EstadoStepper
        {
            nombre   = nombre,
            pinStep  = pinStep,
            pinDir   = pinDir,
            esLineal = esLineal
        };
    }

    private void InicializarMapaServos()
    {
        _servos[ConfiguracionRobot.S1_Pin] = new EstadoServo
            { nombre = "S1 - GiroGarra", pin = ConfiguracionRobot.S1_Pin };
        _servos[ConfiguracionRobot.S2_Pin] = new EstadoServo
            { nombre = "S2 - Gripper",   pin = ConfiguracionRobot.S2_Pin };
    }

    // ─── CALLBACK DE TRAMAS T: ─────────────────────────────────────────────────

    private void AlRecibirTramaT(int pin, int valor, long timestampUs)
    {
        if (_tsInicio < 0) _tsInicio = timestampUs;
        _tsUltimo = timestampUs;
        _totalEventos++;

        // Registrar el pin en el mapa de pines vistos
        if (!_pinesVistos.ContainsKey(pin))
            _pinesVistos[pin] = CrearInfoPin(pin);
        _pinesVistos[pin].cantidadCambios++;

        // Despachar según tipo
        if (_steppers.TryGetValue(pin, out EstadoStepper stepper))
        {
            ProcesarStep(stepper, pin, valor, timestampUs);
        }
        else if (EsPinDir(pin, out EstadoStepper stepperDelDir))
        {
            ProcesarDir(stepperDelDir, valor);
        }
        else if (_servos.TryGetValue(pin, out EstadoServo servo))
        {
            ProcesarServo(servo, valor, timestampUs);
        }
        else
        {
            // Pin desconocido
            AgregarAdvertencia("A08",
                $"Pin {pin} no corresponde a ningún actuador configurado.",
                ColoresUI.Advertencia);
        }
    }

    // ─── PROCESAMIENTO STEPPER ─────────────────────────────────────────────────

    private void ProcesarDir(EstadoStepper st, int valor)
    {
        st.dirActual = (valor == 1) ? 1 : -1;
        if (!st.primerStepRecibido)
            st.dirRecibidaAntesPrimerStep = true;
    }

    private void ProcesarStep(EstadoStepper st, int pin, int valor, long ts)
    {
        if (valor == 1)
        {
            // Flanco de subida: guardar timestamp y registrar para detección simultánea
            st.tsHighStep = ts;

            // Guardar en lista de steps recientes para detección de simultaneidad
            _stepRecientes.Add((ts, pin));
            // Limpiar entradas viejas (fuera de la ventana)
            _stepRecientes.RemoveAll(e => ts - e.ts > VentanaSimultaneo_us * 10);
        }
        else
        {
            // Flanco de bajada: ejecutar paso
            if (!st.primerStepRecibido)
            {
                st.primerStepRecibido = true;
                if (!st.dirRecibidaAntesPrimerStep)
                    AgregarAdvertencia("A03",
                        $"{st.nombre}: primer STEP recibido antes del primer DIR.",
                        ColoresUI.Advertencia);
            }

            // Calcular velocidad (pulso HIGH→LOW)
            if (st.tsHighStep >= 0)
            {
                float pulso_us = ts - st.tsHighStep;
                if (pulso_us > 0f)
                {
                    float vel_pps = 1_000_000f / pulso_us;
                    st.intervalos_us.Add(pulso_us);
                    if (vel_pps > st.velMax_pps) st.velMax_pps = vel_pps;

                    // Verificar velocidad máxima (A01)
                    // Umbral aproximado: a partir de 10000 p/s es excesivo para este hardware
                    if (vel_pps > 10000f)
                        AgregarAdvertencia("A01",
                            $"{st.nombre}: velocidad {vel_pps:F0} p/s excede límite recomendado.",
                            ColoresUI.Advertencia);
                }
            }

            // Acumular pasos según dirección
            if (st.dirActual > 0) st.pasosPositivos++;
            else                  st.pasosNegativos++;

            // Actualizar timestamp de actividad por grupo
            st.ultimaActividad_us = ts;
            if (pin == ConfiguracionRobot.M1_PinSenal)
                _ultimaActivZ = ts;
            else
                _ultimaActivBraz = ts;
        }
    }

    // ─── PROCESAMIENTO SERVO ───────────────────────────────────────────────────

    private void ProcesarServo(EstadoServo sv, int valor, long ts)
    {
        if (valor == 1)
        {
            sv.tsHigh = ts;
        }
        else if (sv.tsHigh >= 0)
        {
            float pulso_us = ts - sv.tsHigh;
            float pulsoMin = ConfiguracionRobot.ServoPulsoMin_us;
            float pulsoMax = ConfiguracionRobot.ServoPulsoMax_us;

            if (pulso_us < pulsoMin || pulso_us > pulsoMax)
                AgregarAdvertencia("A02",
                    $"{sv.nombre}: pulso {pulso_us:F0}µs fuera de rango [{pulsoMin},{pulsoMax}].",
                    ColoresUI.Advertencia);

            pulso_us = Mathf.Clamp(pulso_us, pulsoMin, pulsoMax);
            float rangoAngulo = sv.pin == ConfiguracionRobot.S1_Pin
                ? ConfiguracionRobot.S1_Max
                : ConfiguracionRobot.S2_Max;

            sv.ultimoPulso_us  = pulso_us;
            sv.anguloCalculado = Mathf.InverseLerp(pulsoMin, pulsoMax, pulso_us) * rangoAngulo;
            sv.ultimaActividad = ts;
            _ultimaActivServ   = ts;
        }
    }

    // ─── PROPIEDADES PÚBLICAS (snapshot calculado al leer) ────────────────────

    public List<InfoPin> PinesDetectados
    {
        get
        {
            var lista = new List<InfoPin>(_pinesVistos.Values);
            lista.Sort((a, b) => a.pin.CompareTo(b.pin));
            return lista;
        }
    }

    public List<InfoMotor> PorMotor
    {
        get
        {
            var lista = new List<InfoMotor>();
            foreach (var st in _steppers.Values)
            {
                int total = st.pasosPositivos + st.pasosNegativos;
                if (total == 0) continue;

                float promVel = 0f;
                if (st.intervalos_us.Count > 0)
                {
                    float suma = 0f;
                    foreach (float iv in st.intervalos_us) suma += iv;
                    promVel = 1_000_000f / (suma / st.intervalos_us.Count);
                }

                string dir = st.pasosPositivos > st.pasosNegativos * 1.2f ? "positiva"
                           : st.pasosNegativos > st.pasosPositivos * 1.2f ? "negativa"
                           : "equilibrada";

                lista.Add(new InfoMotor
                {
                    nombreMotor            = st.nombre,
                    pasosTotales          = total,
                    velocidadPromedio_pps = promVel,
                    velocidadMax_pps      = st.velMax_pps,
                    movimientoResultante  = st.pasosPositivos - st.pasosNegativos,
                    direccionPredominante = dir
                });
            }
            return lista;
        }
    }

    public List<InfoServo> PorServo
    {
        get
        {
            var lista = new List<InfoServo>();
            foreach (var sv in _servos.Values)
            {
                if (sv.ultimaActividad < 0) continue;
                lista.Add(new InfoServo
                {
                    nombreServo      = sv.nombre,
                    ultimoPulso_us   = sv.ultimoPulso_us,
                    anguloCalculado  = sv.anguloCalculado
                });
            }
            return lista;
        }
    }

    public InfoPatrones PatronesDetectados
    {
        get
        {
            var p = new InfoPatrones();

            // EsBucle: algún motor stepper con >10 pulsos y variación de intervalo <20%
            foreach (var st in _steppers.Values)
            {
                if (st.intervalos_us.Count <= 10) continue;
                float suma = 0f, maxI = 0f, minI = float.MaxValue;
                foreach (float iv in st.intervalos_us)
                {
                    suma += iv;
                    if (iv > maxI) maxI = iv;
                    if (iv < minI) minI = iv;
                }
                float prom = suma / st.intervalos_us.Count;
                if (prom > 0f && (maxI - minI) / prom < 0.20f)
                {
                    p.esBucle    = true;
                    p.pasosBucle = st.intervalos_us.Count;
                    break;
                }
            }

            // EsSimultaneo: dos pines STEP distintos con pulsos dentro de 500µs
            for (int i = 0; i < _stepRecientes.Count && !p.esSimultaneo; i++)
            for (int j = i + 1; j < _stepRecientes.Count && !p.esSimultaneo; j++)
            {
                if (_stepRecientes[i].pin == _stepRecientes[j].pin) continue;
                long diff = Math.Abs(_stepRecientes[i].ts - _stepRecientes[j].ts);
                if (diff <= VentanaSimultaneo_us)
                {
                    p.esSimultaneo = true;
                    p.motoresSimultaneos = NombreDePin(_stepRecientes[i].pin)
                        + " + " + NombreDePin(_stepRecientes[j].pin);
                }
            }

            // EsSecuenciaCoordinada: Z + brazos + servos con actividad en <5s entre sí
            if (_ultimaActivZ >= 0 && _ultimaActivBraz >= 0 && _ultimaActivServ >= 0)
            {
                long difZB  = Math.Abs(_ultimaActivZ    - _ultimaActivBraz);
                long difZS  = Math.Abs(_ultimaActivZ    - _ultimaActivServ);
                long difBS  = Math.Abs(_ultimaActivBraz - _ultimaActivServ);
                if (difZB < VentanaCoord_us && difZS < VentanaCoord_us && difBS < VentanaCoord_us)
                    p.esSecuenciaCoordinada = true;
            }

            return p;
        }
    }

    public InfoResumen ResumenSesion
    {
        get
        {
            float duracion = (_tsInicio >= 0 && _tsUltimo > _tsInicio)
                ? (_tsUltimo - _tsInicio) / 1_000_000f
                : 0f;

            int totalPasos = 0;
            string motorMasActivo = "—";
            int maxPasos = 0;
            float velMax = 0f;

            foreach (var st in _steppers.Values)
            {
                int total = st.pasosPositivos + st.pasosNegativos;
                totalPasos += total;
                if (total > maxPasos)
                {
                    maxPasos        = total;
                    motorMasActivo  = $"{st.nombre} ({total} pasos)";
                }
                if (st.velMax_pps > velMax) velMax = st.velMax_pps;
            }

            return new InfoResumen
            {
                duracionSegundos     = duracion,
                totalEventos         = _totalEventos,
                totalPasos           = totalPasos,
                motorMasActivo       = motorMasActivo,
                velocidadMaxRegistrada = velMax,
                advertencias         = new List<InfoAdvertencia>(_advertencias.Values)
            };
        }
    }

    // ─── MÉTODO PÚBLICO Limpiar ────────────────────────────────────────────────

    public void Limpiar()
    {
        _pinesVistos.Clear();
        _stepRecientes.Clear();
        _advertencias.Clear();
        _tsInicio      = -1;
        _tsUltimo      = 0;
        _totalEventos  = 0;
        _ultimaActivZ    = -1;
        _ultimaActivBraz = -1;
        _ultimaActivServ = -1;

        foreach (var st in _steppers.Values)
        {
            st.dirActual                  = 1;
            st.dirRecibidaAntesPrimerStep = false;
            st.primerStepRecibido         = false;
            st.tsHighStep                 = -1;
            st.pasosPositivos             = 0;
            st.pasosNegativos             = 0;
            st.intervalos_us.Clear();
            st.velMax_pps                 = 0f;
            st.ultimaActividad_us         = -1;
        }

        foreach (var sv in _servos.Values)
        {
            sv.tsHigh          = -1;
            sv.ultimoPulso_us  = 0f;
            sv.anguloCalculado = 0f;
            sv.ultimaActividad = -1;
        }

        // Re-registrar los pines válidos (InfoPin vacíos) para que PinesDetectados no quede vacío
        // Los pines se re-llenan cuando llegan nuevas tramas.
    }

    // ─── HELPERS PRIVADOS ──────────────────────────────────────────────────────

    private InfoPin CrearInfoPin(int pin)
    {
        // Determinar si es un pin válido conocido
        if (_steppers.ContainsKey(pin))
            return new InfoPin { pin = pin, nombreActuador = _steppers[pin].nombre,
                tipo = "Stepper STEP", esValido = true };

        // ¿Es pin DIR de algún stepper?
        foreach (var st in _steppers.Values)
            if (st.pinDir == pin)
                return new InfoPin { pin = pin, nombreActuador = st.nombre,
                    tipo = "Stepper DIR", esValido = true };

        if (_servos.ContainsKey(pin))
            return new InfoPin { pin = pin, nombreActuador = _servos[pin].nombre,
                tipo = "Servo PWM", esValido = true };

        if (pin == ConfiguracionRobot.EN_Pin)
            return new InfoPin { pin = pin, nombreActuador = "EN_PIN (Enable drivers)",
                tipo = "Control", esValido = true };

        return new InfoPin { pin = pin, nombreActuador = "Desconocido",
            tipo = "Desconocido", esValido = false };
    }

    private bool EsPinDir(int pin, out EstadoStepper resultado)
    {
        foreach (var st in _steppers.Values)
        {
            if (st.pinDir == pin) { resultado = st; return true; }
        }
        resultado = null;
        return false;
    }

    private string NombreDePin(int pin)
    {
        if (_steppers.TryGetValue(pin, out var st)) return st.nombre;
        return $"Pin {pin}";
    }

    private void AgregarAdvertencia(string codigo, string descripcion, Color color)
    {
        if (!_advertencias.ContainsKey(codigo))
            _advertencias[codigo] = new InfoAdvertencia
                { codigo = codigo, descripcion = descripcion, color = color };
    }
}
