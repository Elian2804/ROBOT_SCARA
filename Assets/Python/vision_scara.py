"""
Programa de vision artificial para el robot SCARA.
Detecta objetos por color usando la camara de la laptop
y envia comandos al ESP32 por Bluetooth serial.

Uso:
  1. Emparejar el ESP32 por Bluetooth en la PC.
  2. Identificar el puerto COM del Bluetooth (ej: COM5).
  3. Ejecutar: python vision_scara.py --puerto COM5
"""

import cv2
import serial
import argparse
import time
import sys

# Rangos de color HSV para deteccion
COLORES = {
    "rojo":  {"bajo": (0, 120, 70),   "alto": (10, 255, 255)},
    "verde": {"bajo": (36, 120, 70),  "alto": (86, 255, 255)},
    "azul":  {"bajo": (94, 120, 70),  "alto": (130, 255, 255)},
}

# Comandos que se envian al ESP32
COMANDOS = {
    "rojo":  "PICK\n",
    "verde": "PLACE\n",
    "azul":  "HOME\n",
}

AREA_MINIMA = 500  # Pixeles minimos para considerar una deteccion valida


def conectar_bluetooth(puerto, velocidad=115200):
    """Intenta conectar al ESP32 por Bluetooth serial."""
    try:
        bt = serial.Serial(puerto, velocidad, timeout=1)
        time.sleep(2)  # Esperar a que se estabilice la conexion
        print(f"Conectado a {puerto}")
        return bt
    except serial.SerialException as e:
        print(f"Error al conectar a {puerto}: {e}")
        sys.exit(1)


def detectar_color(frame, nombre_color, rango):
    """Detecta un color en el frame y retorna True si encuentra area suficiente."""
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    mascara = cv2.inRange(hsv, rango["bajo"], rango["alto"])
    contornos, _ = cv2.findContours(mascara, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    for contorno in contornos:
        area = cv2.contourArea(contorno)
        if area > AREA_MINIMA:
            x, y, w, h = cv2.boundingRect(contorno)
            cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
            cv2.putText(frame, nombre_color.upper(), (x, y - 10),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
            return True
    return False


def main():
    parser = argparse.ArgumentParser(description="Vision artificial para robot SCARA")
    parser.add_argument("--puerto", required=True, help="Puerto COM del Bluetooth (ej: COM5)")
    parser.add_argument("--camara", type=int, default=0, help="Indice de la camara (default: 0)")
    args = parser.parse_args()

    bt = conectar_bluetooth(args.puerto)
    camara = cv2.VideoCapture(args.camara)

    if not camara.isOpened():
        print("Error: no se pudo abrir la camara")
        sys.exit(1)

    print("Sistema de vision activo. Presionar 'q' para salir.")
    print("Colores detectables: ROJO (PICK), VERDE (PLACE), AZUL (HOME)")

    ultimo_comando = ""
    tiempo_ultimo = 0
    ESPERA_ENTRE_COMANDOS = 3  # Segundos minimos entre comandos

    while True:
        ret, frame = camara.read()
        if not ret:
            break

        ahora = time.time()

        for nombre, rango in COLORES.items():
            if detectar_color(frame, nombre, rango):
                if ahora - tiempo_ultimo > ESPERA_ENTRE_COMANDOS:
                    comando = COMANDOS[nombre]
                    bt.write(comando.encode())
                    print(f"Enviado: {comando.strip()} (color {nombre})")
                    ultimo_comando = comando.strip()
                    tiempo_ultimo = ahora

        # Mostrar estado en la ventana
        cv2.putText(frame, f"Ultimo: {ultimo_comando}", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        cv2.imshow("Vision SCARA", frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    camara.release()
    cv2.destroyAllWindows()
    bt.close()
    print("Sistema de vision cerrado.")


if __name__ == "__main__":
    main()
