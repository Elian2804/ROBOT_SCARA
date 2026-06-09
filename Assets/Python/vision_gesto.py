"""
Módulo 3 — Control por gestos de mano para robot SCARA.
Usa MediaPipe para detectar dedos y enviar comandos al ESP32 por Bluetooth.

Instalación:
  pip install mediapipe opencv-python pyserial

Uso:
  python vision_gesto.py --puerto COM5

Gestos:
  1 dedo     → SUBE   (Z sube)
  2 dedos    → BAJA   (Z baja)
  3 dedos    → J1A    (Brazo 1 avanza)
  4 dedos    → J1R    (Brazo 1 retrocede)
  5 dedos    → J2A    (Brazo 2 avanza)
  Puño       → J2R    (Brazo 2 retrocede)
  Sin mano   → quieto (no se envía nada)
"""

import cv2
import mediapipe as mp
import serial
import argparse
import time
import sys

# Mapa gesto (nº dedos) → comando BT  formato: "motor,pasos,dir"
# dir 0 = LOW  |  dir 1 = HIGH
# En este robot: LOW sube Z y avanza brazos
GESTOS = {
    1: "z,15,0",   # Z  sube    (1 dedo)
    2: "z,15,1",   # Z  baja    (2 dedos)
    3: "a,10,0",   # J1 avanza  (3 dedos)
    4: "a,10,1",   # J1 retrocede (4 dedos)
    5: "f,10,0",   # J2 avanza  (5 dedos)
    0: "f,10,1",   # J2 retrocede (puño)
}

DESCRIPCION = {
    "z,15,0": "Z  sube",
    "z,15,1": "Z  baja",
    "a,10,0": "Brazo 1 avanza",
    "a,10,1": "Brazo 1 retrocede",
    "f,10,0": "Brazo 2 avanza",
    "f,10,1": "Brazo 2 retrocede",
}

INTERVALO_ENVIO_S = 0.10   # 100 ms entre comandos → movimiento fluido

mp_hands = mp.solutions.hands
mp_draw  = mp.solutions.drawing_utils


# ── Bluetooth ────────────────────────────────────────────────────────────────

def conectar_bluetooth(puerto: str, velocidad: int = 115200):
    try:
        bt = serial.Serial(puerto, velocidad, timeout=0.1)
        time.sleep(2)
        print(f"[BT] Conectado a {puerto} a {velocidad} bps")
        return bt
    except serial.SerialException as e:
        print(f"[BT] Error al conectar: {e}")
        sys.exit(1)


# ── Detección de dedos ───────────────────────────────────────────────────────

def contar_dedos(landmarks, etiqueta_mano: str) -> int:
    """
    Retorna el número de dedos levantados (0 = puño).
    etiqueta_mano: 'Right' o 'Left' según MediaPipe.
    """
    dedos = 0

    # Pulgar: comparar eje X (invertido según mano)
    if etiqueta_mano == "Right":
        if landmarks[4].x < landmarks[3].x:
            dedos += 1
    else:
        if landmarks[4].x > landmarks[3].x:
            dedos += 1

    # Índice, medio, anular, meñique: punta más arriba que la articulación media
    for punta_id in [8, 12, 16, 20]:
        if landmarks[punta_id].y < landmarks[punta_id - 2].y:
            dedos += 1

    return dedos


# ── HUD ──────────────────────────────────────────────────────────────────────

def dibujar_hud(frame, dedos: int, comando: str | None, enviando: bool):
    h, w = frame.shape[:2]

    if comando is None:
        texto  = "Sin mano — quieto"
        color  = (120, 120, 120)
    else:
        desc   = DESCRIPCION.get(comando, comando)
        texto  = f"{dedos} dedo{'s' if dedos != 1 else ''}  →  {desc}"
        color  = (0, 220, 80) if enviando else (0, 140, 50)

    # Fondo semitransparente
    overlay = frame.copy()
    cv2.rectangle(overlay, (0, 0), (w, 70), (20, 20, 20), -1)
    cv2.addWeighted(overlay, 0.55, frame, 0.45, 0, frame)

    cv2.putText(frame, texto, (14, 44),
                cv2.FONT_HERSHEY_SIMPLEX, 1.1, color, 2, cv2.LINE_AA)
    cv2.putText(frame, "q = salir", (14, h - 12),
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (160, 160, 160), 1)


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Control SCARA por gestos")
    parser.add_argument("--puerto", required=True,
                        help="Puerto COM del Bluetooth (ej: COM5)")
    parser.add_argument("--camara", type=int, default=0,
                        help="Índice de cámara (default: 0)")
    args = parser.parse_args()

    bt     = conectar_bluetooth(args.puerto)
    camara = cv2.VideoCapture(args.camara)
    camara.set(cv2.CAP_PROP_FRAME_WIDTH,  640)
    camara.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

    if not camara.isOpened():
        print("[CAM] No se pudo abrir la cámara")
        sys.exit(1)

    # model_complexity=0 → modelo ligero, ~50% menos CPU que el default (1)
    hands = mp_hands.Hands(
        model_complexity=0,
        max_num_hands=1,
        min_detection_confidence=0.75,
        min_tracking_confidence=0.75,
    )

    ultimo_envio     = 0.0
    frame_count      = 0
    dedos_detectados = -1   # -1 = sin mano, se mantiene entre frames
    res              = None

    print("[OK] Control por gestos activo.")
    print("     1=SUBE  2=BAJA  3=J1+  4=J1-  5=J2+  puño=J2-  sin mano=quieto")

    while True:
        ret, frame = camara.read()
        if not ret:
            break

        frame_count += 1
        frame = cv2.flip(frame, 1)

        # Inferencia MediaPipe solo en frames pares → mitad de carga CPU
        if frame_count % 2 == 0:
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            res = hands.process(rgb)

            if res and res.multi_hand_landmarks and res.multi_handedness:
                lm       = res.multi_hand_landmarks[0]
                etiqueta = res.multi_handedness[0].classification[0].label
                dedos_detectados = contar_dedos(lm.landmark, etiqueta)
            else:
                dedos_detectados = -1

        # Dibujar landmarks si hay resultado válido
        if res and res.multi_hand_landmarks:
            mp_draw.draw_landmarks(frame, res.multi_hand_landmarks[0],
                                   mp_hands.HAND_CONNECTIONS)

        ahora    = time.time()
        enviando = False

        if dedos_detectados == -1:
            dibujar_hud(frame, 0, None, False)
        else:
            comando = GESTOS.get(dedos_detectados)
            if comando and ahora - ultimo_envio >= INTERVALO_ENVIO_S:
                bt.write((comando + "\n").encode())
                ultimo_envio = ahora
                enviando = True
                print(f"[TX] {comando}")

                # Leer respuesta del ESP32 (debería llegar "OK")
                time.sleep(0.05)
                respuesta = bt.read_all()
                if respuesta:
                    print(f"[RX] {respuesta.decode(errors='ignore').strip()}")
                else:
                    print("[RX] (sin respuesta del ESP32)")

            dibujar_hud(frame, dedos_detectados, comando, enviando)

        cv2.imshow("Gestos SCARA", frame)
        if cv2.waitKey(16) & 0xFF == ord('q'):   # ~60 fps tope de UI, inferencia a 30
            break

    camara.release()
    cv2.destroyAllWindows()
    bt.close()
    print("[OK] Sistema cerrado.")


if __name__ == "__main__":
    main()
