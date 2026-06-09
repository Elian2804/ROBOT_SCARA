"""
Genera las figuras de cinemática directa e inversa del robot SCARA.
Usa los parámetros reales: L1 = 150.9 mm, L2 = 149.0 mm

Ejecutar:
    python figuras_cinematica.py

Salida:
    cinematica_directa.png
    cinematica_inversa.png
"""

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import Arc, FancyArrowPatch

# ─── Parámetros del robot (ConfiguracionRobot.cs) ────────────────────────────
L1 = 150.9   # mm  eslabón 1 (pivot_art1 → pivot_art2)
L2 = 149.0   # mm  eslabón 2 (pivot_art2 → TCP)

# ─── Colores (paleta del proyecto) ───────────────────────────────────────────
AZUL_MARINO  = "#121C33"
AMARILLO     = "#FFD900"
BLANCO       = "#FFFFFF"
GRIS_CLARO   = "#B0B8CC"
ROJO         = "#E05050"
VERDE        = "#50C878"
AZUL_CLARO   = "#4A90D9"


# ═══════════════════════════════════════════════════════════════════════════════
#  FIGURA 1 — CINEMÁTICA DIRECTA
# ═══════════════════════════════════════════════════════════════════════════════

def dibujar_cinematica_directa(theta1_deg=40.0, theta2_deg=55.0):
    """
    Muestra cómo θ1, θ2, L1 y L2 determinan (X, Y) del TCP.
    """
    t1 = np.radians(theta1_deg)
    t2 = np.radians(theta1_deg + theta2_deg)   # ángulo absoluto del eslabón 2

    # Puntos del robot
    O  = np.array([0.0, 0.0])
    J1 = np.array([L1 * np.cos(t1), L1 * np.sin(t1)])
    TCP = np.array([
        L1 * np.cos(t1) + L2 * np.cos(t2),
        L1 * np.sin(t1) + L2 * np.sin(t2),
    ])

    fig, ax = plt.subplots(figsize=(7, 6), facecolor=AZUL_MARINO)
    ax.set_facecolor(AZUL_MARINO)

    # ── Ejes de referencia ────────────────────────────────────────────────────
    lim = 350
    ax.axhline(0, color=GRIS_CLARO, linewidth=0.6, linestyle="--", alpha=0.5)
    ax.axvline(0, color=GRIS_CLARO, linewidth=0.6, linestyle="--", alpha=0.5)

    # ── Proyecciones del TCP (líneas guía) ────────────────────────────────────
    ax.plot([TCP[0], TCP[0]], [0, TCP[1]],
            color=GRIS_CLARO, linewidth=0.8, linestyle=":", alpha=0.6)
    ax.plot([0, TCP[0]], [TCP[1], TCP[1]],
            color=GRIS_CLARO, linewidth=0.8, linestyle=":", alpha=0.6)
    ax.text(TCP[0] + 6, -22, f"X = {TCP[0]:.1f} mm",
            color=GRIS_CLARO, fontsize=8.5, ha="center")
    ax.text(-28, TCP[1], f"Y = {TCP[1]:.1f} mm",
            color=GRIS_CLARO, fontsize=8.5, va="center", ha="right")

    # ── Eslabones ─────────────────────────────────────────────────────────────
    ax.plot([O[0], J1[0]], [O[1], J1[1]],
            color=AMARILLO, linewidth=4, solid_capstyle="round", zorder=3)
    ax.plot([J1[0], TCP[0]], [J1[1], TCP[1]],
            color=AZUL_CLARO, linewidth=4, solid_capstyle="round", zorder=3)

    # ── Articulaciones ────────────────────────────────────────────────────────
    for punto, radio, color in [(O, 10, BLANCO), (J1, 7, AMARILLO), (TCP, 7, VERDE)]:
        ax.add_patch(plt.Circle(punto, radio, color=color, zorder=5))
        ax.add_patch(plt.Circle(punto, radio, fill=False,
                                edgecolor=AZUL_MARINO, linewidth=1.5, zorder=6))

    # ── Ángulo θ1 ─────────────────────────────────────────────────────────────
    arco_t1 = Arc(O, 70, 70, angle=0,
                  theta1=0, theta2=theta1_deg,
                  color=AMARILLO, linewidth=1.8)
    ax.add_patch(arco_t1)
    mid1 = np.radians(theta1_deg / 2)
    ax.text(42 * np.cos(mid1), 42 * np.sin(mid1),
            f"θ₁ = {theta1_deg:.0f}°",
            color=AMARILLO, fontsize=10, ha="center", va="center",
            fontweight="bold")

    # ── Ángulo θ2 (relativo al eslabón 1) ────────────────────────────────────
    arco_t2 = Arc(J1, 60, 60, angle=0,
                  theta1=theta1_deg, theta2=theta1_deg + theta2_deg,
                  color=AZUL_CLARO, linewidth=1.8)
    ax.add_patch(arco_t2)
    mid2 = np.radians(theta1_deg + theta2_deg / 2)
    ax.text(J1[0] + 38 * np.cos(mid2), J1[1] + 38 * np.sin(mid2),
            f"θ₂ = {theta2_deg:.0f}°",
            color=AZUL_CLARO, fontsize=10, ha="center", va="center",
            fontweight="bold")

    # ── Etiquetas de longitud ─────────────────────────────────────────────────
    mid_esl1 = (O + J1) / 2
    perp1 = np.array([-np.sin(t1), np.cos(t1)]) * 18
    ax.text(mid_esl1[0] + perp1[0], mid_esl1[1] + perp1[1],
            f"L₁ = {L1:.0f} mm",
            color=AMARILLO, fontsize=9.5, ha="center", va="center",
            fontweight="bold")

    mid_esl2 = (J1 + TCP) / 2
    perp2 = np.array([-np.sin(t2), np.cos(t2)]) * 18
    ax.text(mid_esl2[0] + perp2[0], mid_esl2[1] + perp2[1],
            f"L₂ = {L2:.0f} mm",
            color=AZUL_CLARO, fontsize=9.5, ha="center", va="center",
            fontweight="bold")

    # ── Etiquetas de puntos ───────────────────────────────────────────────────
    ax.text(O[0] - 14, O[1] - 18, "O", color=BLANCO, fontsize=11, fontweight="bold")
    ax.text(J1[0] + 12, J1[1] + 6, "J₂", color=AMARILLO, fontsize=11, fontweight="bold")
    ax.text(TCP[0] + 10, TCP[1] + 8, "TCP", color=VERDE, fontsize=11, fontweight="bold")

    # ── Fórmulas ──────────────────────────────────────────────────────────────
    formula = (
        r"$X = L_1 \cos\theta_1 + L_2 \cos(\theta_1+\theta_2)$"
        "\n"
        r"$Y = L_1 \sin\theta_1 + L_2 \sin(\theta_1+\theta_2)$"
    )
    ax.text(0.02, 0.04, formula,
            transform=ax.transAxes,
            color=BLANCO, fontsize=9.5,
            bbox=dict(facecolor="#1E2E50", edgecolor=GRIS_CLARO,
                      boxstyle="round,pad=0.5", alpha=0.85))

    # ── Eje X / Y labels ──────────────────────────────────────────────────────
    ax.text(lim - 10, -20, "X (mm)", color=GRIS_CLARO, fontsize=9, ha="right")
    ax.text(-15, lim - 10, "Y\n(mm)", color=GRIS_CLARO, fontsize=9, va="top")

    ax.set_xlim(-80, lim)
    ax.set_ylim(-60, lim)
    ax.set_aspect("equal")
    ax.axis("off")

    fig.tight_layout(pad=1.5)
    ruta = "cinematica_directa.png"
    fig.savefig(ruta, dpi=180, facecolor=AZUL_MARINO,
                bbox_inches="tight")
    print(f"  Guardado: {ruta}")
    plt.close(fig)


# ═══════════════════════════════════════════════════════════════════════════════
#  FIGURA 2 — CINEMÁTICA INVERSA  (dos soluciones)
# ═══════════════════════════════════════════════════════════════════════════════

def cinematica_inversa(X, Y):
    """
    Devuelve (theta1, theta2) para las dos configuraciones posibles.
    Retorna None si el punto está fuera del espacio de trabajo.
    """
    D = (X**2 + Y**2 - L1**2 - L2**2) / (2 * L1 * L2)
    if abs(D) > 1.0:
        return None, None

    # Solución 1: codo arriba (theta2 positivo)
    t2_up  = np.arctan2( np.sqrt(1 - D**2),  D)
    t1_up  = np.arctan2(Y, X) - np.arctan2(L2 * np.sin(t2_up),
                                             L1 + L2 * np.cos(t2_up))

    # Solución 2: codo abajo (theta2 negativo)
    t2_dn  = np.arctan2(-np.sqrt(1 - D**2),  D)
    t1_dn  = np.arctan2(Y, X) - np.arctan2(L2 * np.sin(t2_dn),
                                             L1 + L2 * np.cos(t2_dn))

    sol1 = (np.degrees(t1_up), np.degrees(t2_up))
    sol2 = (np.degrees(t1_dn), np.degrees(t2_dn))
    return sol1, sol2


def brazo_puntos(theta1_deg, theta2_deg):
    t1 = np.radians(theta1_deg)
    t2 = np.radians(theta1_deg + theta2_deg)
    O   = np.array([0.0, 0.0])
    J1  = np.array([L1 * np.cos(t1), L1 * np.sin(t1)])
    TCP = np.array([L1 * np.cos(t1) + L2 * np.cos(t2),
                    L1 * np.sin(t1) + L2 * np.sin(t2)])
    return O, J1, TCP


def dibujar_cinematica_inversa(X=200.0, Y=160.0):
    """
    Muestra las dos configuraciones posibles (codo arriba / codo abajo)
    para un mismo punto objetivo (X, Y).
    """
    sol1, sol2 = cinematica_inversa(X, Y)
    if sol1 is None:
        print(f"  Punto ({X}, {Y}) fuera del espacio de trabajo.")
        return

    fig, axes = plt.subplots(1, 2, figsize=(12, 5.5), facecolor=AZUL_MARINO)
    fig.subplots_adjust(wspace=0.05)

    configs = [
        (sol1, "Solución 1 — Codo arriba", AMARILLO, AZUL_CLARO),
        (sol2, "Solución 2 — Codo abajo",  "#FF8C42",  "#A78BFA"),
    ]

    for ax, (sol, titulo, c_esl1, c_esl2) in zip(axes, configs):
        ax.set_facecolor(AZUL_MARINO)
        t1_deg, t2_deg = sol
        O, J1, TCP_pt = brazo_puntos(t1_deg, t2_deg)

        lim = 340
        ax.axhline(0, color=GRIS_CLARO, linewidth=0.6, linestyle="--", alpha=0.4)
        ax.axvline(0, color=GRIS_CLARO, linewidth=0.6, linestyle="--", alpha=0.4)

        # Espacio de trabajo (anillos)
        for radio, ls in [(L1 + L2, "--"), (abs(L1 - L2), ":")]:
            circulo = plt.Circle((0, 0), radio, fill=False,
                                 edgecolor=GRIS_CLARO, linewidth=0.8,
                                 linestyle=ls, alpha=0.3)
            ax.add_patch(circulo)

        # Eslabones
        ax.plot([O[0], J1[0]], [O[1], J1[1]],
                color=c_esl1, linewidth=4.5, solid_capstyle="round", zorder=3)
        ax.plot([J1[0], TCP_pt[0]], [J1[1], TCP_pt[1]],
                color=c_esl2, linewidth=4.5, solid_capstyle="round", zorder=3)

        # Articulaciones
        for punto, r, col in [(O, 10, BLANCO), (J1, 7, c_esl1), (TCP_pt, 8, VERDE)]:
            ax.add_patch(plt.Circle(punto, r, color=col, zorder=5))
            ax.add_patch(plt.Circle(punto, r, fill=False,
                                    edgecolor=AZUL_MARINO, linewidth=1.5, zorder=6))

        # Punto objetivo (cruz)
        ax.plot(X, Y, marker="+", color=ROJO, markersize=18,
                markeredgewidth=2.2, zorder=7)
        ax.plot(X, Y, "o", color=ROJO, markersize=6, zorder=8)
        ax.text(X + 10, Y + 8, f"P({X:.0f}, {Y:.0f})",
                color=ROJO, fontsize=9.5, fontweight="bold")

        # Ángulos
        t1 = np.radians(t1_deg)
        t2_abs = np.radians(t1_deg + t2_deg)
        arco1 = Arc(O, 80, 80, angle=0,
                    theta1=min(0, t1_deg), theta2=max(0, t1_deg),
                    color=c_esl1, linewidth=1.8)
        ax.add_patch(arco1)
        mid1 = np.radians(t1_deg / 2)
        ax.text(52 * np.cos(mid1), 52 * np.sin(mid1),
                f"θ₁={t1_deg:.1f}°",
                color=c_esl1, fontsize=9, ha="center", va="center",
                fontweight="bold")

        arco2 = Arc(J1, 65, 65, angle=0,
                    theta1=min(t1_deg, t1_deg + t2_deg),
                    theta2=max(t1_deg, t1_deg + t2_deg),
                    color=c_esl2, linewidth=1.8)
        ax.add_patch(arco2)
        mid2 = np.radians(t1_deg + t2_deg / 2)
        ax.text(J1[0] + 42 * np.cos(mid2), J1[1] + 42 * np.sin(mid2),
                f"θ₂={t2_deg:.1f}°",
                color=c_esl2, fontsize=9, ha="center", va="center",
                fontweight="bold")

        # Etiquetas longitud
        mid_e1 = (O + J1) / 2
        ax.text(mid_e1[0] - 18, mid_e1[1] + 10,
                f"L₁={L1:.0f}", color=c_esl1, fontsize=8.5,
                ha="center", fontweight="bold")
        mid_e2 = (J1 + TCP_pt) / 2
        ax.text(mid_e2[0] + 14, mid_e2[1] - 14,
                f"L₂={L2:.0f}", color=c_esl2, fontsize=8.5,
                ha="center", fontweight="bold")

        # Etiquetas puntos
        ax.text(O[0] - 14, O[1] - 20, "O", color=BLANCO,
                fontsize=11, fontweight="bold")
        ax.text(J1[0] + 10, J1[1] + 8, "J₂", color=c_esl1,
                fontsize=11, fontweight="bold")
        ax.text(TCP_pt[0] + 10, TCP_pt[1] + 8, "TCP", color=VERDE,
                fontsize=10, fontweight="bold")

        ax.set_xlim(-80, lim)
        ax.set_ylim(-60, lim)
        ax.set_aspect("equal")
        ax.axis("off")
        ax.set_title(titulo, color=BLANCO, fontsize=11,
                     fontweight="bold", pad=10)

    # Fórmulas IK centradas bajo la figura
    formula_ik = (
        r"$\cos\theta_2 = \dfrac{X^2 + Y^2 - L_1^2 - L_2^2}{2\,L_1\,L_2}$"
        "          "
        r"$\theta_1 = \mathrm{atan2}(Y,\,X) - \mathrm{atan2}(L_2\sin\theta_2,\;L_1+L_2\cos\theta_2)$"
    )
    fig.text(0.5, 0.01, formula_ik,
             ha="center", color=BLANCO, fontsize=9.5,
             bbox=dict(facecolor="#1E2E50", edgecolor=GRIS_CLARO,
                       boxstyle="round,pad=0.5", alpha=0.85))

    ruta = "cinematica_inversa.png"
    fig.savefig(ruta, dpi=180, facecolor=AZUL_MARINO,
                bbox_inches="tight")
    print(f"  Guardado: {ruta}")
    plt.close(fig)


# ─── Main ─────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    print("Generando figuras...")
    dibujar_cinematica_directa(theta1_deg=40.0, theta2_deg=55.0)
    dibujar_cinematica_inversa(X=200.0, Y=160.0)
    print("Listo.")
