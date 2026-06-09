using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Componente principal del simulador SCARA.
/// Consume la cola del LectorSerial, parsea las 4 tramas del protocolo,
/// valida límites, mueve el modelo 3D y actualiza los paneles de UI.
/// </summary>
public class GestorSCARA : MonoBehaviour
{
    // ─── REFERENCIAS COMPONENTES ──────────────────────────────────────────────
    [Header("Componentes del sistema")]
    [SerializeField] private LectorSerial lectorSerial;

    [Header("Actuadores (arrastrar assets ConfiguracionMotor)")]
    [SerializeField] private ConfiguracionMotor motorM1_EjeZ;
    [SerializeField] private ConfiguracionMotor motorM2_Brazo1;
    [SerializeField] private ConfiguracionMotor motorM3_Brazo2;
    [SerializeField] private ConfiguracionMotor servoS1_GiroGarra;
    [SerializeField] private ConfiguracionMotor servoS2_Gripper;

    [Header("GameObjects del robot (arrastrar desde jerarquía)")]
    [SerializeField] private Transform articulacionVertical;   // ARTICULACION_VERTICAL
    [SerializeField] private Transform pivotArt1;              // pivot_art1
    [SerializeField] private Transform pivotArt2;              // pivot_art2
    [SerializeField] private Transform giroGarra;              // GiroGarra
    [SerializeField] private Transform garra1;                 // Garra_1
    [SerializeField] private Transform garra2;                 // Garra_2
    [SerializeField] private Transform transformTCP;           // TCP

    [Header("Paneles de UI")]
    [SerializeField] private PanelBarraSuperior  panelBarra;
    [SerializeField] private PanelEstadoRobot    panelEstado;
    [SerializeField] private PanelConsola        panelConsola;
    [SerializeField] private PanelErrores        panelErrores;
    [SerializeField] private PanelControlArticular panelControlArticular;
    [SerializeField] private PanelTrayectorias   panelTrayectorias;
    [SerializeField] private PanelVisionBT       panelVisionBT;

    // ─── CONFIGURACIÓN ────────────────────────────────────────────────────────
    [Header("Configuración de movimiento")]
    [SerializeField] private float velocidadRotacion = ConfiguracionRobot.VelocidadRotacion;
    [SerializeField] private float velocidadVertical  = ConfiguracionRobot.VelocidadVertical;

    [Header("Ejes de rotación (ajustar según modelo 3D)")]
    [Tooltip("Eje local de rotación de pivot_art1 (J1). Cambiar si el brazo gira mal.")]
    [SerializeField] private Vector3 ejeJ1 = Vector3.up;
    [Tooltip("Eje local de rotación de pivot_art2 (J2).")]
    [SerializeField] private Vector3 ejeJ2 = Vector3.up;
    [Tooltip("Eje local de rotación de GiroGarra (S1).")]
    [SerializeField] private Vector3 ejeS1 = Vector3.up;
    [Tooltip("Eje local del eje Z (ARTICULACION_VERTICAL). Normalmente Vector3.up.")]
    [SerializeField] private Vector3 ejeZ  = Vector3.up;
    [Tooltip("Invertir dirección de J1 si gira al revés.")]
    [SerializeField] private bool invertirJ1 = false;
    [Tooltip("Invertir dirección de J2 si gira al revés.")]
    [SerializeField] private bool invertirJ2 = false;
    [Tooltip("Invertir dirección de S1 si gira al revés.")]
    [SerializeField] private bool invertirS1 = false;
    [Tooltip("true=LOW sube Z (default este robot). false=HIGH sube Z.")]
    [SerializeField] private bool invertirZ  = true;
    [Tooltip("Unidades Unity por mm en el eje Z. 110 unidades / 200mm = 0.55")]
    [SerializeField] private float escalaZ_UnidadPorMm = 0.55f;
    [Tooltip("Posición Y mundial máxima de ARTICULACION_VERTICAL. El brazo no subirá de aquí.")]
    [SerializeField] private float limiteSuperiorWorldY =  30f;
    [Tooltip("Posición Y mundial mínima de ARTICULACION_VERTICAL. El brazo no bajará de aquí (evita atravesar el suelo).")]
    [SerializeField] private float limiteInferiorWorldY = -80f;

    [Header("Módulo activo")]
    [SerializeField] private int moduloActivo = 1;   // 1, 2 o 3

    [Header("Posición de home")]
    [Tooltip("Ángulo J1 en home. -90° = brazo 1 apunta hacia atrás.")]
    [SerializeField] private float homeJ1 = ConfiguracionRobot.HomeJ1;
    [Tooltip("Ángulo J2 en home. -90° = brazo 2 dobla hacia la izquierda.")]
    [SerializeField] private float homeJ2 = ConfiguracionRobot.HomeJ2;
    [Tooltip("Si es true, el contador de pasos aumenta al girar en sentido horario.")]
    [SerializeField] private bool conteoHorario = true;

    [Header("Inversión de dirección de conteo (independiente del visual)")]
    [Tooltip("true=LOW mueve J1 hacia +90° (default este robot). false=HIGH mueve hacia +90°.")]
    [SerializeField] private bool invertirConteoJ1 = true;
    [Tooltip("true=LOW mueve J2 hacia +90° (default este robot). false=HIGH mueve hacia +90°.")]
    [SerializeField] private bool invertirConteoJ2 = true;

    // ─── ESTADO INTERNO DEL ROBOT ─────────────────────────────────────────────
    private DatosRobot _estado;

    // ─── EVENTOS BLUETOOTH (hilo principal — seguros para suscripción de UI) ──
    /// <summary>Disparado cuando llega trama B:CON o B:DIS del ESP32.</summary>
    public event Action<bool>   OnBluetoothStateChanged;
    /// <summary>Disparado cuando llega trama L: con prefijo "BT cmd:" (comando de gestos).</summary>
    public event Action<string> OnBluetoothCommandReceived;

    // ─── ANALIZADOR DE SKETCH ─────────────────────────────────────────────────
    private Action<int, int, long> _callbackAnalizador;

    // ─── DETECCIÓN DE DESCONEXIÓN ─────────────────────────────────────────────
    private bool _estabaConectado = false;

    // ─── LÍMITE DE MENSAJES REPETIDOS ────────────────────────────────────────
    private readonly Dictionary<string, int> _contadorMensajes = new Dictionary<string, int>();
    private const int MaxRepeticionesMensaje = 3;

    // ─── RATE LIMITER PARA EVENTOS T: ────────────────────────────────────────
    private readonly Dictionary<int, float> _timerLogMotor = new Dictionary<int, float>();
    private const float IntervaloLogMotorS = 0.5f;

    // Ángulos y posición objetivo (se interpolan con Lerp)
    private float _objetivoJ1   = 0f;
    private float _objetivoJ2   = 0f;
    private float _objetivoZ_mm = 0f;
    private float _objetivoS1   = 0f;
    private float _objetivoS2   = 0f;

    // ─── ESTADO INTERNO DE LOS MOTORES ───────────────────────────────────────
    // Stepper: seguimiento de pasos y dirección
    private float _pasosJ1   = 0f;
    private float _pasosJ2   = 0f;
    private float _pasosZ    = 0f;
    private int   _dirM1     = 1;
    private int   _dirM2     = 1;
    private int   _dirM3     = 1;

    // Seguimiento suavizado de posición real (para EstaEnPosicion y estado UI)
    private float _j1Suave = 0f;
    private float _j2Suave = 0f;
    private float _zSuave  = 0f;

    // Posición Y en espacio MUNDO de ARTICULACION_VERTICAL cuando Z = 0mm.
    // Se calibra automáticamente en el primer frame de Play leyendo la posición real del modelo.
    // Usar world position evita depender de la rotación del padre (FBX con Z-up de Blender).
    private float _zPivotWorldY = float.NaN;

    // Pasos acumulados desde el último home (para display en UI)
    private int _pasosRelJ1 = 0;
    private int _pasosRelJ2 = 0;
    private int _pasosRelZ  = 0;

    // Timestamps de subida (HIGH) por pin, para calcular ancho de pulso servo
    private Dictionary<int, long> _timestampAlto = new Dictionary<int, long>();

    // Timestamps de subida de STEP (HIGH) por pin, para calcular velocidad
    private Dictionary<int, long> _timestampStep = new Dictionary<int, long>();

    // Mapa pin → ConfiguracionMotor (construido en Start)
    private Dictionary<int, ConfiguracionMotor> _mapaPines = new Dictionary<int, ConfiguracionMotor>();

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    private void Start()
    {
        ConfigurarPantalla();
        ConstruirMapaPines();
        _objetivoJ1   = homeJ1;
        _j1Suave      = homeJ1;
        _objetivoJ2   = homeJ2;
        _j2Suave      = homeJ2;
        _objetivoZ_mm = 0f;
        _zSuave       = 0f;
        InicializarPaneles();
        ConfiguracionRobot.OnReduccionCambiada += LogReduccionCambiada;
    }

    private void OnDestroy()
    {
        ConfiguracionRobot.OnReduccionCambiada -= LogReduccionCambiada;
    }

    private void LogReduccionCambiada(string motor, float pasos)
    {
        string relacion = pasos == 100f ? "1:1" : pasos == 200f ? "1:2" :
                          pasos == 300f ? "1:3" : "1:4";
        string unidad   = motor == "Z" ? $"vuelta ({ConfiguracionRobot.MmPorPaso_Z * pasos:F2}mm)"
                                       : "180°";
        string msg      = $"Reducción {motor}: {relacion} → {(int)pasos} pasos = {unidad}";
        if (panelConsola != null)
            panelConsola.AgregarMensaje(msg, TipoMensaje.Normal);
        Debug.Log($"[GestorSCARA] {msg}");
    }

    private void Update()
    {
        ProcesarCola();
        InterpolarMovimiento();
        ActualizarPaneles();
        DetectarDesconexion();
    }

    // ─── DETECCIÓN DE DESCONEXIÓN ─────────────────────────────────────────────

    private void DetectarDesconexion()
    {
        if (lectorSerial == null) return;
        bool conectadoAhora = lectorSerial.Conectado;
        if (_estabaConectado && !conectadoAhora)
            AlDesconectarCable();
        _estabaConectado = conectadoAhora;
    }

    private void AlDesconectarCable()
    {
        lectorSerial.Desconectar();
        if (panelBarra != null) panelBarra.ActualizarEstadoConexion(false, "");

        // Resetear estado completo del robot
        IrAHome();
        _pasosJ1 = 0f;  _pasosJ2 = 0f;  _pasosZ = 0f;
        _dirM1 = 1;     _dirM2 = 1;     _dirM3 = 1;
        _timestampAlto.Clear();
        _timestampStep.Clear();
        while (lectorSerial.ColaLineas.TryDequeue(out _)) { }
        _contadorMensajes.Clear();
        _timerLogMotor.Clear();

        if (panelConsola != null)
            panelConsola.AgregarMensaje("══════ USB DESCONECTADO — robot en home ══════", TipoMensaje.Advertencia);
    }

    // ─── CONFIGURACIÓN INICIAL ────────────────────────────────────────────────

    /// <summary>Fija la resolución a 1920×1080 pantalla completa sin bordes.</summary>
    private void ConfigurarPantalla()
    {
        Screen.SetResolution(
            ConfiguracionRobot.PantallaAncho,
            ConfiguracionRobot.PantallaAlto,
            FullScreenMode.FullScreenWindow);
    }

    /// <summary>
    /// Llena el diccionario pin → ConfiguracionMotor.
    /// Permite al GestorSCARA despachar cualquier trama T en O(1).
    /// </summary>
    private void ConstruirMapaPines()
    {
        _mapaPines.Clear();
        RegistrarMotor(motorM1_EjeZ);
        RegistrarMotor(motorM2_Brazo1);
        RegistrarMotor(motorM3_Brazo2);
        RegistrarMotor(servoS1_GiroGarra);
        RegistrarMotor(servoS2_Gripper);
    }

    private void RegistrarMotor(ConfiguracionMotor motor)
    {
        if (motor == null) return;

        if (motor.pinSenal != 0 && !_mapaPines.ContainsKey(motor.pinSenal))
            _mapaPines[motor.pinSenal] = motor;

        if (motor.tipo == TipoActuador.Stepper && motor.pinDir != 0
            && !_mapaPines.ContainsKey(motor.pinDir))
            _mapaPines[motor.pinDir] = motor;
    }

    private void InicializarPaneles()
    {
        if (panelBarra            != null) panelBarra.Inicializar(this);
        if (panelEstado           != null) panelEstado.Inicializar();
        if (panelConsola          != null) panelConsola.Inicializar();
        if (panelErrores          != null) panelErrores.Inicializar();
        if (panelTrayectorias     != null) panelTrayectorias.Inicializar();
        if (panelVisionBT         != null) panelVisionBT.Inicializar(this);

        // Activar solo el panel del módulo activo
        if (panelControlArticular != null)
            panelControlArticular.gameObject.SetActive(moduloActivo == 1);
        if (panelTrayectorias != null)
            panelTrayectorias.gameObject.SetActive(moduloActivo == 2);
        if (panelVisionBT != null)
            panelVisionBT.gameObject.SetActive(moduloActivo == 3);
    }

    // ─── PROCESAMIENTO DE COLA ────────────────────────────────────────────────

    /// <summary>
    /// Consume todas las líneas disponibles en la cola del LectorSerial.
    /// Se llama en Update(), en el hilo principal de Unity.
    /// </summary>
    private void ProcesarCola()
    {
        if (lectorSerial == null) return;

        // Procesar hasta 50 tramas por frame para no bloquear el render
        int limite = 50;
        while (limite-- > 0 && lectorSerial.ColaLineas.TryDequeue(out string linea))
        {
            if (string.IsNullOrEmpty(linea)) continue;
            ProcesarLinea(linea);
        }
    }

    /// <summary>
    /// Determina el tipo de trama por su primer carácter y la despacha.
    /// Líneas que no comienzan con T, E, L o B se descartan silenciosamente.
    /// </summary>
    private void ProcesarLinea(string linea)
    {
        if (linea.Length == 0) return;

        switch (linea[0])
        {
            case 'T': ProcesarTramaT(linea); break;
            case 'E': ProcesarTramaE(linea); break;
            case 'L': ProcesarTramaL(linea); break;
            case 'B': ProcesarTramaB(linea); break;
            // Cualquier otro carácter: descartar sin advertencia
        }
    }

    // ─── TRAMA T: evento digital ──────────────────────────────────────────────

    /// <summary>
    /// Parsea: T:&lt;us&gt;,P:&lt;pin&gt;,V:&lt;0|1&gt;
    /// Despacha al motor correspondiente según el pin.
    /// </summary>
    private void ProcesarTramaT(string linea)
    {
        // Formato esperado: T:123456,P:26,V:1
        long us  = 0;
        int  pin = -1;
        int  val = -1;

        try
        {
            // Separar por comas: ["T:123456", "P:26", "V:1"]
            string[] partes = linea.Split(',');
            if (partes.Length < 3) return;

            us  = long.Parse(partes[0].Substring(2));
            pin = int.Parse(partes[1].Substring(2));
            val = int.Parse(partes[2].Substring(2));
        }
        catch
        {
            // Trama malformada, descartar
            return;
        }

        // Validar que el pin esté configurado
        // Notificar al analizador con el evento raw (incluye pines desconocidos)
        _callbackAnalizador?.Invoke(pin, val, us);

        if (!_mapaPines.TryGetValue(pin, out ConfiguracionMotor motor))
        {
            ReportarAdvertencia("A08", $"Pin {pin} no corresponde a ningún actuador. Señal descartada.");
            return;
        }

        // Despachar según tipo de actuador y rol del pin
        if (motor.tipo == TipoActuador.Stepper)
            ProcesarPinStepper(motor, pin, val, us);
        else
            ProcesarPinServo(motor, pin, val, us);

        // Loguear eventos T: con rate limiter por motor (máx 1 entrada cada 0.5s)
        // Solo en flanco bajada del pin STEP — el pin DIR no debe consumir el slot
        if (val == 0 && pin == motor.pinSenal && panelConsola != null)
        {
            int clave = motor.pinSenal;
            _timerLogMotor.TryGetValue(clave, out float tUltimo);
            if (Time.time - tUltimo >= IntervaloLogMotorS)
            {
                _timerLogMotor[clave] = Time.time;
                panelConsola.AgregarMensaje(FormatearEventoT(motor, pin, us), TipoMensaje.Normal);
            }
        }
    }

    private string FormatearEventoT(ConfiguracionMotor motor, int pin, long us)
    {
        if (motor.tipo == TipoActuador.Stepper)
        {
            if (pin == motor.pinDir)
            {
                string dir = ObtenerDireccion(motor) > 0 ? "▲" : "▼";
                return $"{motor.nombreActuador} | DIR {dir}";
            }

            string dirS = ObtenerDireccion(motor) > 0 ? "+" : "-";

            if (motor == motorM1_EjeZ)
            {
                float mm      = ConfiguracionRobot.PasosAMmZ(_pasosZ);
                int   ref_    = (int)ConfiguracionRobot.PasosRef_Z;
                return $"{motor.nombreActuador} | Z:{mm:F2}mm | paso {dirS}{_pasosRelZ} | ref:{ref_}p/vuelta";
            }
            else if (motor == motorM2_Brazo1)
            {
                float grados  = ConfiguracionRobot.PasosAGradosJ1(_pasosJ1);
                int   p180    = (int)ConfiguracionRobot.Pasos180_J1;
                return $"{motor.nombreActuador} | J1:{grados + 90f:F1}° | paso {dirS}{_pasosRelJ1}/{p180}";
            }
            else
            {
                float grados  = ConfiguracionRobot.PasosAGradosJ2(_pasosJ2);
                int   p180    = (int)ConfiguracionRobot.Pasos180_J2;
                return $"{motor.nombreActuador} | J2:{grados + 90f:F1}° | paso {dirS}{_pasosRelJ2}/{p180}";
            }
        }
        else
        {
            float angulo = motor == servoS1_GiroGarra ? _objetivoS1 : _objetivoS2;
            return $"{motor.nombreActuador} | {angulo:F1}°";
        }
    }

    /// <summary>
    /// Lógica stepper:
    /// - pinDir  HIGH/LOW → guarda dirección del motor.
    /// - pinSenal HIGH    → guarda timestamp (para calcular velocidad).
    /// - pinSenal LOW     → ejecuta 1 paso en la dirección guardada.
    /// </summary>
    private void ProcesarPinStepper(ConfiguracionMotor motor, int pin, int valor, long us)
    {
        if (pin == motor.pinDir)
        {
            // Actualizar dirección
            int dir = (valor == 1) ? 1 : -1;
            AsignarDireccion(motor, dir);
            return;
        }

        if (pin == motor.pinSenal)
        {
            if (valor == 1)
            {
                // Flanco de subida: guardar timestamp
                _timestampStep[pin] = us;
            }
            else
            {
                // Flanco de bajada: ejecutar un paso
                long periodoUs = 0;
                if (_timestampStep.TryGetValue(pin, out long tsAlto))
                    periodoUs = us - tsAlto;

                // Validar velocidad (A01) y guardar en estado
                if (periodoUs > 0)
                {
                    float velPasosPorSeg = 1_000_000f / periodoUs;
                    if (velPasosPorSeg > motor.velocidadMaxPasos && motor.velocidadMaxPasos > 0)
                        ReportarAdvertencia("A01",
                            $"{motor.nombreActuador}: vel {velPasosPorSeg:F0} pasos/s excede máx ({motor.velocidadMaxPasos}).");

                    if (motor == motorM1_EjeZ)    _estado.velPasosM1 = velPasosPorSeg;
                    else if (motor == motorM2_Brazo1) _estado.velPasosM2 = velPasosPorSeg;
                    else if (motor == motorM3_Brazo2) _estado.velPasosM3 = velPasosPorSeg;
                }

                EjecutarPaso(motor);
            }
        }
    }

    private void AsignarDireccion(ConfiguracionMotor motor, int dir)
    {
        if (motor == motorM1_EjeZ)    _dirM1 = dir;
        else if (motor == motorM2_Brazo1) _dirM2 = dir;
        else if (motor == motorM3_Brazo2) _dirM3 = dir;
    }

    private int ObtenerDireccion(ConfiguracionMotor motor)
    {
        if (motor == motorM1_EjeZ)    return _dirM1;
        if (motor == motorM2_Brazo1)  return _dirM2;
        if (motor == motorM3_Brazo2)  return _dirM3;
        return 1;
    }

    /// <summary>Aplica un paso al motor y valida los límites resultantes.</summary>
    private void EjecutarPaso(ConfiguracionMotor motor)
    {
        int dir = ObtenerDireccion(motor);

        if (motor == motorM1_EjeZ)
        {
            _pasosZ += invertirZ ? -dir : dir;
            float nuevaZ = ConfiguracionRobot.PasosAMmZ(_pasosZ);

            if (nuevaZ > ConfiguracionRobot.Z_Max_mm)
            {
                _pasosZ -= invertirZ ? -dir : dir;
                ReportarError("E03",
                    $"Eje Z: posición {nuevaZ:F1}mm excede límite ({ConfiguracionRobot.Z_Max_mm}mm). Detenido.");
                return;
            }
            if (nuevaZ < ConfiguracionRobot.Z_Min_mm)
            {
                _pasosZ -= invertirZ ? -dir : dir;
                ReportarError("E03",
                    $"Eje Z: posición {nuevaZ:F1}mm excede límite mín ({ConfiguracionRobot.Z_Min_mm}mm). Detenido.");
                return;
            }
            _objetivoZ_mm = nuevaZ;
            _pasosRelZ += dir;
        }
        else if (motor == motorM2_Brazo1)
        {
            _pasosJ1 += invertirConteoJ1 ? -dir : dir;
            float nuevoJ1 = ConfiguracionRobot.PasosAGradosJ1(_pasosJ1);

            if (nuevoJ1 > ConfiguracionRobot.J1_Max)
            {
                _pasosJ1 -= invertirConteoJ1 ? -dir : dir;
                ReportarError("E01",
                    $"{motor.nombreActuador}: ángulo {nuevoJ1 + 90f:F1}° excede máx ({ConfiguracionRobot.J1_Max + 90f}°). Detenido.");
                return;
            }
            if (nuevoJ1 < ConfiguracionRobot.J1_Min)
            {
                _pasosJ1 -= invertirConteoJ1 ? -dir : dir;
                ReportarError("E02",
                    $"{motor.nombreActuador}: ángulo {nuevoJ1 + 90f:F1}° excede mín ({ConfiguracionRobot.J1_Min + 90f}°). Detenido.");
                return;
            }

            if (DetectarColisionBrazos(nuevoJ1, _objetivoJ2))
            {
                _pasosJ1 -= invertirConteoJ1 ? -dir : dir;
                ReportarError("E04",
                    $"Colisión: Brazo 2 intersecta Brazo 1 en θ1={nuevoJ1 + 90f:F1}°, θ2={_objetivoJ2 + 90f:F1}°.");
                return;
            }

            _objetivoJ1 = nuevoJ1;
            _pasosRelJ1 += conteoHorario ? -dir : dir;
        }
        else if (motor == motorM3_Brazo2)
        {
            _pasosJ2 += invertirConteoJ2 ? -dir : dir;
            float nuevoJ2 = ConfiguracionRobot.PasosAGradosJ2(_pasosJ2);

            if (nuevoJ2 > ConfiguracionRobot.J2_Max)
            {
                _pasosJ2 -= invertirConteoJ2 ? -dir : dir;
                ReportarError("E01",
                    $"{motor.nombreActuador}: ángulo {nuevoJ2 + 90f:F1}° excede máx ({ConfiguracionRobot.J2_Max + 90f}°). Detenido.");
                return;
            }
            if (nuevoJ2 < ConfiguracionRobot.J2_Min)
            {
                _pasosJ2 -= invertirConteoJ2 ? -dir : dir;
                ReportarError("E02",
                    $"{motor.nombreActuador}: ángulo {nuevoJ2 + 90f:F1}° excede mín ({ConfiguracionRobot.J2_Min + 90f}°). Detenido.");
                return;
            }

            if (DetectarColisionBrazos(_objetivoJ1, nuevoJ2))
            {
                _pasosJ2 -= invertirConteoJ2 ? -dir : dir;
                ReportarError("E04",
                    $"Colisión: Brazo 2 intersecta Brazo 1 en θ1={_objetivoJ1 + 90f:F1}°, θ2={nuevoJ2 + 90f:F1}°.");
                return;
            }

            _objetivoJ2 = nuevoJ2;
            _pasosRelJ2 += conteoHorario ? -dir : dir;
        }
    }

    /// <summary>
    /// Lógica servo:
    /// - HIGH → guarda timestamp.
    /// - LOW  → calcula ancho_pulso = ts_bajo - ts_alto → mapea a ángulo.
    /// </summary>
    private void ProcesarPinServo(ConfiguracionMotor motor, int pin, int valor, long us)
    {
        if (valor == 1)
        {
            _timestampAlto[pin] = us;
        }
        else
        {
            if (!_timestampAlto.TryGetValue(pin, out long tsAlto)) return;

            float pulsoUs = us - tsAlto;

            // Validar rango de pulso (A02)
            if (pulsoUs < motor.pulsoMin_us || pulsoUs > motor.pulsoMax_us)
            {
                ReportarAdvertencia("A02",
                    $"Servo {motor.nombreActuador}: pulso {pulsoUs:F0}µs fuera de rango.");
                // Clampar en lugar de rechazar
                pulsoUs = Mathf.Clamp(pulsoUs, motor.pulsoMin_us, motor.pulsoMax_us);
            }

            float angulo = motor.PulsoAAngulo(pulsoUs);

            if (motor == servoS1_GiroGarra)
                _objetivoS1 = angulo;
            else if (motor == servoS2_Gripper)
                _objetivoS2 = angulo;
        }
    }

    // ─── TRAMA E: overflow buffer ─────────────────────────────────────────────

    private void ProcesarTramaE(string linea)
    {
        // Formato: E:OVF,C:<count>
        int count = 0;
        try
        {
            int idx = linea.LastIndexOf(':');
            if (idx >= 0) count = int.Parse(linea.Substring(idx + 1));
        }
        catch { }

        ReportarError("E06", $"ESP32: se perdieron {count} eventos por overflow.");
    }

    // ─── TRAMA L: log del sketch ──────────────────────────────────────────────

    private void ProcesarTramaL(string linea)
    {
        // Formato: L:<texto libre>
        string mensaje = linea.Length > 2 ? linea.Substring(2) : "(vacío)";
        if (panelConsola != null) panelConsola.AgregarMensaje(mensaje, TipoMensaje.Log);
        Debug.Log($"[ESP32] {mensaje}");

        // El ESP32 loguea cada comando BT recibido como "BT cmd: <cmd>".
        if (mensaje.StartsWith("BT cmd:"))
        {
            // Extraer el comando (todo lo que hay tras "BT cmd:")
            string cmd = mensaje.Length > 7 ? mensaje.Substring(7).Trim() : "";

            // Si Unity conectó serial después de que Python ya estaba en BT,
            // inferir aquí el estado conectado (el B:CON ya se perdió).
            if (!_estado.bluetoothConectado)
            {
                _estado.bluetoothConectado = true;
                OnBluetoothStateChanged?.Invoke(true);
            }

            // Notificar al panel M3 del comando recibido
            OnBluetoothCommandReceived?.Invoke(cmd);
        }
    }

    // ─── TRAMA B: estado Bluetooth ────────────────────────────────────────────

    private void ProcesarTramaB(string linea)
    {
        // Formato: B:CON o B:DIS
        bool conectado = linea.Contains("CON");
        _estado.bluetoothConectado = conectado;

        if (!conectado)
            ReportarAdvertencia("A06", "Conexión Bluetooth perdida.");

        // El panel se actualiza vía evento (sin acoplamiento directo)
        OnBluetoothStateChanged?.Invoke(conectado);
    }

    // ─── INTERPOLACIÓN DE MOVIMIENTO ─────────────────────────────────────────

    /// <summary>
    /// Interpola suavemente las transformaciones del modelo 3D hacia los objetivos.
    /// Se llama en cada Update() del hilo principal.
    /// </summary>
    private void InterpolarMovimiento()
    {
        float dt = Time.deltaTime;

        // Actualizar seguimiento suavizado (refleja posición visual real)
        _j1Suave = Mathf.LerpAngle(_j1Suave, invertirJ1 ? -_objetivoJ1 : _objetivoJ1, velocidadRotacion * dt);
        _j2Suave = Mathf.LerpAngle(_j2Suave, invertirJ2 ? -_objetivoJ2 : _objetivoJ2, velocidadRotacion * dt);
        _zSuave  = Mathf.Lerp(_zSuave, _objetivoZ_mm, velocidadVertical * dt);

        // Eje Z: traslación de ARTICULACION_VERTICAL en espacio MUNDO
        // (evita depender del eje local que puede ser distinto al eje Y global por la rotación del FBX)
        if (articulacionVertical != null)
        {
            // Primer frame: calcular worldY de referencia para Z=0mm
            // Se compensa el Z inicial (100mm) para que el pivote represente siempre Z=0mm
            if (float.IsNaN(_zPivotWorldY))
            {
                float yActual    = articulacionVertical.position.y;
                float mmInicial  = invertirZ
                    ? (ConfiguracionRobot.Z_Max_mm - _objetivoZ_mm)
                    : _objetivoZ_mm;
                _zPivotWorldY = yActual + (mmInicial * escalaZ_UnidadPorMm);
            }

            float mmObjetivo = invertirZ
                ? ConfiguracionRobot.Z_Max_mm - _objetivoZ_mm
                : _objetivoZ_mm;

            float yMundoObjetivo = Mathf.Clamp(
                _zPivotWorldY - (mmObjetivo * escalaZ_UnidadPorMm),
                limiteInferiorWorldY, limiteSuperiorWorldY);

            Vector3 posM = articulacionVertical.position;
            posM.y = Mathf.Lerp(posM.y, yMundoObjetivo, velocidadVertical * dt);
            articulacionVertical.position = posM;

            _estado.posicionZ_mm = _zSuave;
        }

        // J1: rotación configurable de pivot_art1
        if (pivotArt1 != null)
        {
            AplicarRotacionEje(pivotArt1, ejeJ1, _j1Suave, 1f);   // ya suavizado
            _estado.anguloJ1 = invertirJ1 ? -_j1Suave : _j1Suave;
        }

        // J2: rotación configurable de pivot_art2
        if (pivotArt2 != null)
        {
            AplicarRotacionEje(pivotArt2, ejeJ2, _j2Suave, 1f);   // ya suavizado
            _estado.anguloJ2 = invertirJ2 ? -_j2Suave : _j2Suave;
        }

        // S1: rotación configurable de GiroGarra
        if (giroGarra != null)
        {
            float objetivo = invertirS1 ? -_objetivoS1 : _objetivoS1;
            AplicarRotacionEje(giroGarra, ejeS1, objetivo, velocidadRotacion * dt);
            _estado.anguloS1 = objetivo;
        }

        // S2: traslación X simétrica de Garra_1 y Garra_2
        if (garra1 != null && garra2 != null)
        {
            // Mapear ángulo S2 (0-20°) a desplazamiento (GarraCerrada - GarraAbierta)
            float t   = Mathf.InverseLerp(ConfiguracionRobot.S2_Min, ConfiguracionRobot.S2_Max, _objetivoS2);
            float despl = Mathf.Lerp(ConfiguracionRobot.GarraCerrada, ConfiguracionRobot.GarraAbierta, t);
            float suave = Mathf.Lerp(garra1.localPosition.x, despl, velocidadRotacion * dt);

            Vector3 posG1 = garra1.localPosition;
            Vector3 posG2 = garra2.localPosition;
            posG1.x =  suave;
            posG2.x = -suave;
            garra1.localPosition = posG1;
            garra2.localPosition = posG2;
            _estado.anguloS2 = _objetivoS2;
        }

        // Pasos relativos y dirección (para display en UI)
        _estado.pasosJ1 = _pasosRelJ1;
        _estado.pasosJ2 = _pasosRelJ2;
        _estado.pasosZ  = _pasosRelZ;
        _estado.dirM1   = _dirM1;
        _estado.dirM2   = _dirM2;
        _estado.dirM3   = _dirM3;

        // Actualizar posición TCP por cinemática directa
        _estado.ActualizarTCP();
    }

    // ─── ACTUALIZACIÓN DE PANELES ─────────────────────────────────────────────

    /// <summary>
    /// Cambia el módulo activo (1, 2 o 3) y actualiza la visibilidad de los paneles.
    /// Asignar este método a botones de cambio de módulo desde el Inspector.
    /// </summary>
    public void CambiarModulo(int nuevoModulo)
    {
        if (nuevoModulo < 1 || nuevoModulo > 3) return;
        moduloActivo = nuevoModulo;

        if (panelControlArticular != null)
            panelControlArticular.gameObject.SetActive(moduloActivo == 1);
        if (panelTrayectorias != null)
            panelTrayectorias.gameObject.SetActive(moduloActivo == 2);
        if (panelVisionBT != null)
            panelVisionBT.gameObject.SetActive(moduloActivo == 3);
        string[] nombres = { "", "M1 — Control Articular", "M2 — Trayectorias", "M3 — Visión BT" };
        if (panelBarra != null && moduloActivo <= 3)
            panelBarra.SetNombreModulo(nombres[moduloActivo]);
    }

    public void CambiarAModulo1() => CambiarModulo(1);
    public void CambiarAModulo2() => CambiarModulo(2);
    public void CambiarAModulo3() => CambiarModulo(3);

    private void ActualizarPaneles()
    {
        if (panelEstado != null) panelEstado.ActualizarEstado(_estado);

        if (moduloActivo == 1 && panelControlArticular != null)
            panelControlArticular.ActualizarEstado(_estado);
        else if (moduloActivo == 2 && panelTrayectorias != null)
            panelTrayectorias.ActualizarEstado(_estado);
        else if (moduloActivo == 3 && panelVisionBT != null)
            panelVisionBT.ActualizarEstado(_estado);
    }

    // ─── VALIDACIONES ─────────────────────────────────────────────────────────

    /// <summary>
    /// Detecta colisión brazo-brazo: ocurre cuando el ángulo absoluto
    /// entre los brazos supera el rango permitido de solapamiento.
    /// </summary>
    private bool DetectarColisionBrazos(float j1Grados, float j2Grados)
    {
        // Ángulo relativo entre brazo 2 y brazo 1
        float anguloAbsoluto = Mathf.Abs(j2Grados);
        // Colisión si J2 supera 160° en valor absoluto relativo al brazo 1
        return anguloAbsoluto > 160f;
    }

    // ─── SISTEMA DE ERRORES Y ADVERTENCIAS ───────────────────────────────────

    private void ReportarError(string codigo, string mensaje)
    {
        _contadorMensajes.TryGetValue(codigo, out int n);
        _contadorMensajes[codigo] = ++n;
        if (n > MaxRepeticionesMensaje) return;

        if (panelErrores != null) panelErrores.AgregarError(codigo, mensaje);

        string txt = n == MaxRepeticionesMensaje
            ? $"[{codigo}] {mensaje}  ← suprimido a partir de aquí"
            : $"[{codigo}] {mensaje}";

        if (panelConsola != null) panelConsola.AgregarMensaje(txt, TipoMensaje.Error);
        Debug.LogError($"[GestorSCARA][{codigo}] {mensaje}");
    }

    private void ReportarAdvertencia(string codigo, string mensaje)
    {
        string clave = "W_" + codigo;
        _contadorMensajes.TryGetValue(clave, out int n);
        _contadorMensajes[clave] = ++n;
        if (n > MaxRepeticionesMensaje) return;

        string txt = n == MaxRepeticionesMensaje
            ? $"[{codigo}] {mensaje}  ← suprimido a partir de aquí"
            : $"[{codigo}] {mensaje}";

        if (panelConsola != null) panelConsola.AgregarMensaje(txt, TipoMensaje.Advertencia);
        if (panelErrores != null) panelErrores.AgregarError(codigo, mensaje);
        Debug.LogWarning($"[GestorSCARA][{codigo}] {mensaje}");
    }

    // ─── MÉTODOS PÚBLICOS ─────────────────────────────────────────────────────

    /// <summary>Abre el puerto serial. Llamado desde PanelBarraSuperior.</summary>
    public bool ConectarSerial(string puerto)
    {
        if (lectorSerial == null) return false;
        bool resultado = lectorSerial.Conectar(puerto);
        _estabaConectado = resultado;
        if (panelBarra != null) panelBarra.ActualizarEstadoConexion(resultado, puerto);
        return resultado;
    }

    /// <summary>Cierra el puerto serial. Llamado desde PanelBarraSuperior.</summary>
    public void DesconectarSerial()
    {
        _estabaConectado = false;
        if (lectorSerial != null) lectorSerial.Desconectar();
        if (panelBarra   != null) panelBarra.ActualizarEstadoConexion(false, "");
    }

    /// <summary>
    /// Registra un callback que recibirá (pin, valor, timestampUs) por cada trama T: válida.
    /// AnalizadorSketch llama esto en Start() para recibir el feed raw sin modificar el flujo.
    /// </summary>
    public void SuscribirAnalizador(Action<int, int, long> callback)
        => _callbackAnalizador = callback;

    /// <summary>Expone el estado actual del robot (solo lectura).</summary>
    public DatosRobot ObtenerEstado() => _estado;

    /// <summary>True si el lector serial está conectado y activo.</summary>
    public bool EstaConectado => lectorSerial != null && lectorSerial.Conectado;

    // ─── API PÚBLICA PARA MÓDULOS 2 Y 3 ──────────────────────────────────────

    /// <summary>
    /// Mueve el robot a la posición indicada por pasos acumulados.
    /// Usado por GestorTrayectorias para reproducir puntos grabados.
    /// </summary>
    public void IrAPasos(int j1, int j2, int z)
    {
        _pasosJ1     = j1;
        _pasosJ2     = j2;
        _pasosZ      = z;
        _pasosRelJ1  = j1;
        _pasosRelJ2  = j2;
        _pasosRelZ   = z;
        _objetivoJ1   = Mathf.Clamp(ConfiguracionRobot.PasosAGradosJ1(j1),
                            ConfiguracionRobot.J1_Min, ConfiguracionRobot.J1_Max);
        _objetivoJ2   = Mathf.Clamp(ConfiguracionRobot.PasosAGradosJ2(j2),
                            ConfiguracionRobot.J2_Min, ConfiguracionRobot.J2_Max);
        _objetivoZ_mm = Mathf.Clamp(ConfiguracionRobot.PasosAMmZ(z),
                            ConfiguracionRobot.Z_Min_mm, ConfiguracionRobot.Z_Max_mm);
    }

    /// <summary>Mueve solo el eje Z a la posición dada en mm.</summary>
    public void MoverZ(float zMm)
    {
        _objetivoZ_mm = Mathf.Clamp(zMm, ConfiguracionRobot.Z_Min_mm, ConfiguracionRobot.Z_Max_mm);
    }

    /// <summary>Abre el gripper (S2 al máximo).</summary>
    public void AbrirGripper() => _objetivoS2 = ConfiguracionRobot.S2_Max;

    /// <summary>Cierra el gripper (S2 a cero).</summary>
    public void CerrarGripper() => _objetivoS2 = ConfiguracionRobot.S2_Min;

    /// <summary>Rota la garra (S1) en delta grados. Rango 0°–180°.</summary>
    public void SimularS1(float deltaDeg)
    {
        _objetivoS1 = Mathf.Clamp(_objetivoS1 + deltaDeg,
            ConfiguracionRobot.S1_Min, ConfiguracionRobot.S1_Max);
    }

    /// <summary>
    /// Mueve a home en secuencia segura: sube Z primero, luego rota brazos, evita colisiones.
    /// Usa la corrutina para esperar entre fases. Llamar desde MonoBehaviour vía StartCoroutine.
    /// Para llamada instantánea (sin espera) usar IrAHomeInmediato().
    /// </summary>
    public System.Collections.IEnumerator CorrutinaIrAHome()
    {
        // Fase 1: subir Z al tope (posición segura antes de rotar brazos)
        _objetivoZ_mm = 0f;
        _zSuave       = 0f;
        _pasosZ       = 0f;
        _pasosRelZ    = 0;
        yield return new WaitUntil(() => EstaEnPosicion(toleranciaZ: 3f));

        // Fase 2: rotar brazos a home
        _objetivoJ1 = homeJ1;
        _objetivoJ2 = homeJ2;
        _objetivoS1 = 0f;
        _objetivoS2 = ConfiguracionRobot.S2_Min;
        _pasosJ1    = 0f;
        _pasosJ2    = 0f;
        _pasosRelJ1 = 0;
        _pasosRelJ2 = 0;
        yield return new WaitUntil(() => EstaEnPosicion());
    }

    /// <summary>Home instantáneo sin secuencia segura (solo para reset de estado en desconexión).</summary>
    public void IrAHome()
    {
        _objetivoJ1   = homeJ1;
        _objetivoJ2   = homeJ2;
        _objetivoZ_mm = 0f;
        _objetivoS1   = 0f;
        _objetivoS2   = ConfiguracionRobot.S2_Min;
        _pasosJ1      = 0f;
        _pasosJ2      = 0f;
        _pasosZ       = 0f;
        _pasosRelJ1   = 0;
        _pasosRelJ2   = 0;
        _pasosRelZ    = 0;
    }

    /// <summary>
    /// True cuando todos los ejes están dentro de la tolerancia de sus objetivos.
    /// Usa los valores suavizados que reflejan la posición visual real.
    /// </summary>
    public bool EstaEnPosicion(float toleranciaDeg = 2f, float toleranciaZ = 2f)
    {
        return Mathf.Abs(Mathf.DeltaAngle(_j1Suave, invertirJ1 ? -_objetivoJ1 : _objetivoJ1)) < toleranciaDeg &&
               Mathf.Abs(Mathf.DeltaAngle(_j2Suave, invertirJ2 ? -_objetivoJ2 : _objetivoJ2)) < toleranciaDeg &&
               Mathf.Abs(_zSuave - _objetivoZ_mm) < toleranciaZ;
    }

    // ─── API DETECCIÓN DE CONTACTO GARRA ─────────────────────────────────────

    /// <summary>Llamado por DetectorContactoGarra cuando el gripper toca un objeto.</summary>
    public void NotificarContactoGarra(ObjetoManipulable obj)
    {
        if (obj == null || obj.Estado != ObjetoManipulable.EstadoObjeto.Libre) return;

        // Solo agarrar si el gripper está suficientemente cerrado (S2 < 30% apertura)
        float t = Mathf.InverseLerp(ConfiguracionRobot.S2_Min, ConfiguracionRobot.S2_Max, _objetivoS2);
        if (t > 0.3f) return;

        Transform tcp = ObtenerTransformTCP();
        if (tcp == null) return;

        obj.Agarrar(tcp);
        _estado.objetoAgarrado = true;
        if (panelConsola != null)
            panelConsola.AgregarMensaje($"Gripper: agarrado '{obj.nombreObjeto}'", TipoMensaje.Exito);
    }

    /// <summary>Llamado por DetectorContactoGarra cuando el objeto sale del radio de la garra.</summary>
    public void NotificarSalidaGarra(ObjetoManipulable obj)
    {
        if (obj == null || obj.Estado != ObjetoManipulable.EstadoObjeto.Transportado) return;

        // Solo soltar si el gripper está abierto (S2 > 70% apertura)
        float t = Mathf.InverseLerp(ConfiguracionRobot.S2_Min, ConfiguracionRobot.S2_Max, _objetivoS2);
        if (t < 0.7f) return;

        obj.Soltar();
        _estado.objetoAgarrado = false;
        if (panelConsola != null)
            panelConsola.AgregarMensaje($"Gripper: soltado '{obj.nombreObjeto}'", TipoMensaje.Log);
    }

    // ─── API SIMULACIÓN (sin ESP32) ───────────────────────────────────────────

    /// <summary>Desplaza J1 en delta grados (compatibilidad). Mantiene pasos sincronizados con el home.</summary>
    public void SimularJ1(float deltaDeg)
    {
        float nuevo = Mathf.Clamp(_objetivoJ1 + deltaDeg,
            ConfiguracionRobot.J1_Min, ConfiguracionRobot.J1_Max);
        if (DetectarColisionBrazos(nuevo, _objetivoJ2)) return;
        _objetivoJ1 = nuevo;
        _j1Suave    = nuevo;
        _pasosJ1    = (nuevo - ConfiguracionRobot.HomeJ1) / 180f * ConfiguracionRobot.Pasos180_J1;
        _pasosRelJ1 = (int)_pasosJ1;
    }

    /// <summary>Desplaza J2 en delta grados (compatibilidad). Mantiene pasos sincronizados con el home.</summary>
    public void SimularJ2(float deltaDeg)
    {
        float nuevo = Mathf.Clamp(_objetivoJ2 + deltaDeg,
            ConfiguracionRobot.J2_Min, ConfiguracionRobot.J2_Max);
        if (DetectarColisionBrazos(_objetivoJ1, nuevo)) return;
        _objetivoJ2 = nuevo;
        _j2Suave    = nuevo;
        _pasosJ2    = (nuevo - ConfiguracionRobot.HomeJ2) / 180f * ConfiguracionRobot.Pasos180_J2;
        _pasosRelJ2 = (int)_pasosJ2;
    }

    /// <summary>Desplaza el eje Z en delta mm (compatibilidad SimuladorSerial).</summary>
    public void SimularZ(float deltaMm)
    {
        _objetivoZ_mm = Mathf.Clamp(_objetivoZ_mm + deltaMm,
            ConfiguracionRobot.Z_Min_mm, ConfiguracionRobot.Z_Max_mm);
        _zSuave    = _objetivoZ_mm;
        _pasosZ    = _objetivoZ_mm / ConfiguracionRobot.MmPorPaso_Z;
        _pasosRelZ = (int)_pasosZ;

        if (articulacionVertical != null && !float.IsNaN(_zPivotWorldY))
        {
            float mm = invertirZ ? ConfiguracionRobot.Z_Max_mm - _objetivoZ_mm : _objetivoZ_mm;
            Vector3 posM = articulacionVertical.position;
            posM.y = Mathf.Clamp(_zPivotWorldY - (mm * escalaZ_UnidadPorMm), limiteInferiorWorldY, limiteSuperiorWorldY);
            articulacionVertical.position = posM;
        }
    }

    /// <summary>Expone el Transform del TCP. Asignar desde Inspector (sin Find en runtime).</summary>
    public Transform ObtenerTransformTCP() => transformTCP;

    /// <summary>Expone pivot_art1 para que VisualizadorTrayectoria calcule la calibración mm→mundo.</summary>
    public Transform ObtenerTransformPivotArt1() => pivotArt1;

    /// <summary>
    /// Mueve J1 en delta grados (simulación sin ESP32). Actualiza pasos también.
    /// </summary>
    public void SimularPasoJ1(int deltaPasos)
    {
        int nuevoPasos = (int)_pasosJ1 + deltaPasos;
        float nuevoJ1  = ConfiguracionRobot.PasosAGradosJ1(nuevoPasos);
        nuevoJ1        = Mathf.Clamp(nuevoJ1, ConfiguracionRobot.J1_Min, ConfiguracionRobot.J1_Max);
        if (DetectarColisionBrazos(nuevoJ1, _objetivoJ2)) return;
        _pasosJ1    = nuevoPasos;
        _pasosRelJ1 = nuevoPasos;
        _objetivoJ1 = nuevoJ1;
        _j1Suave    = nuevoJ1;
    }

    /// <summary>Mueve J2 en delta pasos (simulación sin ESP32).</summary>
    public void SimularPasoJ2(int deltaPasos)
    {
        int nuevoPasos = (int)_pasosJ2 + deltaPasos;
        float nuevoJ2  = ConfiguracionRobot.PasosAGradosJ2(nuevoPasos);
        nuevoJ2        = Mathf.Clamp(nuevoJ2, ConfiguracionRobot.J2_Min, ConfiguracionRobot.J2_Max);
        if (DetectarColisionBrazos(_objetivoJ1, nuevoJ2)) return;
        _pasosJ2    = nuevoPasos;
        _pasosRelJ2 = nuevoPasos;
        _objetivoJ2 = nuevoJ2;
        _j2Suave    = nuevoJ2;
    }

    /// <summary>Mueve Z en delta pasos (simulación sin ESP32).</summary>
    public void SimularPasoZ(int deltaPasos)
    {
        int nuevoPasos  = (int)_pasosZ + deltaPasos;
        float nuevaZ    = ConfiguracionRobot.PasosAMmZ(nuevoPasos);
        nuevaZ          = Mathf.Clamp(nuevaZ, ConfiguracionRobot.Z_Min_mm, ConfiguracionRobot.Z_Max_mm);
        _pasosZ         = nuevoPasos;
        _pasosRelZ      = nuevoPasos;
        _objetivoZ_mm   = nuevaZ;
        _zSuave         = nuevaZ;
    }

    /// <summary>Getters de pasos actuales (para que GestorTrayectorias lea la posición).</summary>
    public int PasosActualesJ1 => _pasosRelJ1;
    public int PasosActualesJ2 => _pasosRelJ2;
    public int PasosActualesZ  => _pasosRelZ;

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica rotación suave sobre un eje local arbitrario.
    /// Permite configurar desde Inspector qué eje (X, Y o Z) usa cada articulación.
    /// </summary>
    private void AplicarRotacionEje(Transform t, Vector3 eje, float anguloObjetivo, float velocidad)
    {
        Vector3 ejeNorm = eje.normalized;

        // Extraer el ángulo actual sobre el eje dado usando la rotación local actual
        float angActual = 0f;
        if      (ejeNorm == Vector3.up)      angActual = NormalizarAngulo(t.localEulerAngles.y);
        else if (ejeNorm == Vector3.right)   angActual = NormalizarAngulo(t.localEulerAngles.x);
        else if (ejeNorm == Vector3.forward) angActual = NormalizarAngulo(t.localEulerAngles.z);
        else if (ejeNorm == Vector3.down)    angActual = NormalizarAngulo(-t.localEulerAngles.y);
        else if (ejeNorm == Vector3.left)    angActual = NormalizarAngulo(-t.localEulerAngles.x);
        else if (ejeNorm == Vector3.back)    angActual = NormalizarAngulo(-t.localEulerAngles.z);
        else                                 angActual = NormalizarAngulo(t.localEulerAngles.y);

        float angSuave = Mathf.LerpAngle(angActual, anguloObjetivo, velocidad);
        Vector3 euler  = t.localEulerAngles;

        if      (ejeNorm == Vector3.up      || ejeNorm == Vector3.down)    euler.y = angSuave;
        else if (ejeNorm == Vector3.right   || ejeNorm == Vector3.left)    euler.x = angSuave;
        else if (ejeNorm == Vector3.forward || ejeNorm == Vector3.back)    euler.z = angSuave;
        else    euler.y = angSuave;

        t.localEulerAngles = euler;
    }

    /// <summary>Normaliza un ángulo de eulerAngles (0-360) al rango -180 a 180.</summary>
    private float NormalizarAngulo(float angulo)
    {
        if (angulo > 180f) angulo -= 360f;
        return angulo;
    }
}
