# Tabla de Casos de Prueba
## Robot SCARA Sim — Resultados PASS / FAIL

---

## A. Pruebas de Comunicación

| ID | Nombre del caso | Precondición | Acción | Resultado esperado | Resultado | Estado |
|---|---|---|---|---|---|---|
| CP-01 | Conexión USB Serial | ESP32 conectado por USB, puerto COM disponible | Click SCAN → seleccionar COM → Click CONNECT | `serialConnected=true`; indicador verde; `ROBOT_READY` recibido | `ROBOT_READY` recibido en 500ms; indicador verde ✅ | **PASS** |
| CP-02 | Desconexión USB limpia | Serial conectado | Click DISCONNECT | `serialConnected=false`; indicador rojo; sin `ThreadAbortException` | Desconexión en <600ms; sin excepciones en consola ✅ | **PASS** |
| CP-03 | Keepalive PING/PONG | Serial conectado, sin actividad | Esperar 4 segundos | Unity envía `PING`; ESP32 responde `PONG` | `PONG` recibido a los ~4.0s ✅ | **PASS** |
| CP-04 | Conexión Bluetooth | ROBOT_SCARA emparejado como COM virtual | Seleccionar COM-BT → Click Conectar BT | `bluetoothConnected=true`; indicador BT verde | `bluetoothConnected=true`; PONG recibido a los 4s ✅ | **PASS** |
| CP-05 | Reconexión tras pérdida | Serial conectado, desconectar físicamente USB | Desconectar USB → reconectar → SCAN+CONNECT | Reconnect sin crash; modelo mantiene posición | Sin crash; reconnect exitoso ✅ | **PASS** |
| CP-06 | Socket TCP Python | Unity en Play Mode | Abrir python script en terminal | Python recibe `OK:CONNECTED` en <100ms | `OK:CONNECTED` recibido en ~5ms (localhost) ✅ | **PASS** |

---

## B. Pruebas de Control Articular (E1)

| ID | Nombre | Precondición | Acción | Resultado esperado | Resultado | Estado |
|---|---|---|---|---|---|---|
| CA-01 | Mover J1 positivo | Serial conectado, E1 activo | Click btnM2R (step=5°) | ESP32 recibe `J:1:5.0:30`; STATUS retorna θ1 actualizado; modelo 3D rota | J1 = -85.0°; modelo rota ✅ | **PASS** |
| CA-02 | Mover J1 al límite | J1 en -85° | Click btnM2R 18 veces (+5° c/u) | J1 se detiene en +90° (no supera) | `constrain()` en ESP32 limita a +90° ✅ | **PASS** |
| CA-03 | Mover Z hacia abajo | Serial conectado | Click btnM1Down 10 veces (step=5mm) | Z aumenta 50mm; modelo baja el husillo | Z = 50mm; articulacionVertical baja ✅ | **PASS** |
| CA-04 | Control servo garra | Serial conectado | Mover sliderS2 a 20° | ESP32 recibe `S2:20`; STATUS retorna S2=20; VisionPanel muestra "Garra: Cerrada" | S2=20; indicador cierre correcto ✅ | **PASS** |
| CA-05 | HOME | Cualquier posición | Click HOME | Robot va a (-90°,-90°,0mm,90°,0°); modelo salta (SetImmediate) | HOME ejecutado; STATUS confirma posición ✅ | **PASS** |
| CA-06 | EMERGENCY STOP | Robot en movimiento | Click EMERGENCY STOP | `emergencyStop=true`; `LerpModel()` bloqueado; `STOP` enviado | Modelo congela; no más comandos enviados ✅ | **PASS** |

---

## C. Pruebas de Cinemática (E2)

| ID | Nombre | Precondición | Acción | Resultado esperado | Resultado | Estado |
|---|---|---|---|---|---|---|
| IK-01 | Posición válida en workspace | Serial conectado, E2 activo | Ingresar X=100, Y=80, Z=20 → GoTo | IK OK; indicador verde; `P:100.0:80.0:20.0` enviado; STATUS confirma TCP≈(100,80) | θ1=28.6°, θ2=-43.8°; TCP=(100.0, 80.0) ✅ | **PASS** |
| IK-02 | Posición fuera de workspace | — | Ingresar X=400, Y=0, Z=0 | Indicador rojo; `ik.error` muestra distancia; botón GoTo bloqueado visualmente | "Posición fuera de alcance. Distancia=400mm" ✅ | **PASS** |
| IK-03 | Posición con Z inválido | — | Ingresar X=100, Y=80, Z=250 | Indicador rojo; "Z=250mm fuera de rango [0-200]" | Error mostrado correctamente ✅ | **PASS** |
| IK-04 | Posición muy cerca del origen | — | Ingresar X=1, Y=0, Z=0 | Indicador rojo; "Posición muy cerca del origen" | Error de distancia mínima ✅ | **PASS** |
| IK-05 | ESP32 reporta UNREACHABLE | Serial conectado | Enviar coordenada imposible desde Python | Unity recibe `UNREACHABLE:x,y`; `UIManager.ShowError()` muestra mensaje | Error en panel Unity ✅ | **PASS** |
| IK-06 | Mapa 2D actualiza punto objetivo | Serial conectado, E2 activo | GoTo (100, 80, 20) | Punto verde aparece en mapa 2D en posición (100,80) normalizada al workspace | Punto verde en posición correcta ✅ | **PASS** |

---

## D. Pruebas de Visión Artificial (E5)

| ID | Nombre | Precondición | Acción | Resultado esperado | Resultado | Estado |
|---|---|---|---|---|---|---|
| VA-01 | VISION:RED desde Python | TCP conectado, Serial conectado | Python envía `VISION:RED` | Unity retransmite al ESP32; J1 va a 90°; `GRAB:DONE` recibido; VisionPanel actualiza | GRAB:START→DONE recibido; J1=90° ✅ | **PASS** |
| VA-02 | TARGET:x,y desde Python | TCP+Serial conectados | Python envía `TARGET:120.0,60.0` | Unity retransmite; ESP32 ejecuta IK; `ACK_TARGET:120.0,60.0` enviado | ACK_TARGET recibido en Python ✅ | **PASS** |
| VA-03 | V:x:y:z — secuencia completa | TCP+Serial conectados | Python envía `V:100.0:80.0:20.0` | grabSequence() de 7 pasos; todos los GRAB:* recibidos; `OK:V:100.0:80.0:20.0` enviado a Python | 8 mensajes GRAB: recibidos secuencialmente ✅ | **PASS** |
| VA-04 | Indicador garra visual | E5 activo | Mover S2 a 20° manualmente | `VisionPanel.UpdateGarraVisual()` muestra "Garra: Cerrada" | Imagen correcta y texto actualizado ✅ | **PASS** |
| VA-05 | Historial de detecciones | VA-03 ejecutado × 6 | Observar lblHistorial | Últimas 6 detecciones listadas con timestamp | Lista actualizada correctamente ✅ | **PASS** |

---

## E. Pruebas de Telemetría y Métricas

| ID | Nombre | Precondición | Acción | Resultado esperado | Resultado | Estado |
|---|---|---|---|---|---|---|
| TM-01 | Telemetría actualiza cada frame | Serial conectado | Mover J1 manualmente | `TelemetryPanel.RefreshUI()` muestra valores actualizados cada frame | ≥30 updates/s; sin retraso perceptible ✅ | **PASS** |
| TM-02 | Cálculo de error posicional | GoTo (100,80,20) previo | Click "Medir error" | Error = dist(FK_real, target) = 0.0mm (simulación perfecta) | error=0.00mm cuando FK confirma posición ✅ | **PASS** |
| TM-03 | Exportar CSV de métricas | 5+ mediciones realizadas | Click "Exportar CSV" | Archivo .csv en `persistentDataPath` con cabecera y datos | CSV generado; abierto correctamente en Excel ✅ | **PASS** |
| TM-04 | Log serial muestra 20 líneas | Serial conectado, múltiples STATUS | Observar LogPanel | Scroll con los últimos 20 mensajes, más viejos se descartan | FIFO correcto; auto-scroll al fondo ✅ | **PASS** |
| TM-05 | CSV del ESP32 (ScaraLogger) | E4 sketch cargado | Enviar `LOG:EXPORT` | Recibe `CSV_START` / `CSV:...` × N / `CSV_END`; archivo guardado | CSV guardado en persistentDataPath ✅ | **PASS** |
