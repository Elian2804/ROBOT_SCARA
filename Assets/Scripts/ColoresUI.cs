using UnityEngine;

/// <summary>
/// Paleta de colores oficial de la interfaz del simulador SCARA.
/// Colores predominantes: blanco, amarillo y azul marino.
/// Referenciar siempre desde aquí. NUNCA hardcodear colores en otros scripts.
/// </summary>
public static class ColoresUI
{
    // ─── FONDOS ───────────────────────────────────────────────────────────────
    /// <summary>Fondo principal de paneles: azul marino oscuro.</summary>
    public static readonly Color FondoPrincipal   = new Color(0.07f, 0.11f, 0.20f, 1f);  // #121C33

    /// <summary>Fondo secundario (sub-paneles, filas alternas): azul marino medio.</summary>
    public static readonly Color FondoSecundario  = new Color(0.10f, 0.16f, 0.28f, 1f);  // #1A2948

    /// <summary>Fondo barra superior e inferior: azul marino oscuro profundo.</summary>
    public static readonly Color FondoBarra       = new Color(0.05f, 0.08f, 0.15f, 1f);  // #0D1426

    /// <summary>Fondo de botones en estado normal.</summary>
    public static readonly Color FondoBoton       = new Color(0.13f, 0.22f, 0.40f, 1f);  // #213866

    /// <summary>Fondo de botones al pasar el cursor (hover).</summary>
    public static readonly Color FondoBotonHover  = new Color(0.18f, 0.30f, 0.55f, 1f);  // #2E4D8C

    /// <summary>Fondo de botones al presionar (pressed).</summary>
    public static readonly Color FondoBotonPress  = new Color(0.25f, 0.42f, 0.75f, 1f);  // #406BBF

    // ─── TEXTO ────────────────────────────────────────────────────────────────
    /// <summary>Texto principal: blanco puro.</summary>
    public static readonly Color TextoPrincipal   = Color.white;                          // #FFFFFF

    /// <summary>Texto secundario: blanco semi-transparente para etiquetas.</summary>
    public static readonly Color TextoSecundario  = new Color(0.75f, 0.80f, 0.90f, 1f);  // #C0CCE6

    /// <summary>Texto deshabilitado: gris azulado.</summary>
    public static readonly Color TextoDeshabilitado = new Color(0.45f, 0.50f, 0.60f, 1f);

    // ─── ACENTOS ─────────────────────────────────────────────────────────────
    /// <summary>Acento principal: amarillo brillante para resaltar valores activos.</summary>
    public static readonly Color AcentoAmarillo   = new Color(1.00f, 0.85f, 0.00f, 1f);  // #FFD900

    /// <summary>Acento suave: amarillo tenue para bordes y separadores.</summary>
    public static readonly Color AcentoAmarilloSuave = new Color(1.00f, 0.85f, 0.00f, 0.35f);

    // ─── ESTADOS ─────────────────────────────────────────────────────────────
    /// <summary>Éxito / conexión activa: verde.</summary>
    public static readonly Color Exito            = new Color(0.20f, 0.85f, 0.45f, 1f);  // #33D973

    /// <summary>Advertencia (A01-A08): amarillo.</summary>
    public static readonly Color Advertencia      = new Color(1.00f, 0.85f, 0.00f, 1f);  // #FFD900

    /// <summary>Error (E01-E06): rojo.</summary>
    public static readonly Color Error            = new Color(0.95f, 0.25f, 0.25f, 1f);  // #F24040

    /// <summary>Log del sketch (trama L:): cyan.</summary>
    public static readonly Color LogSketch        = new Color(0.30f, 0.90f, 1.00f, 1f);  // #4DE6FF

    /// <summary>Evento normal (trama T:): blanco.</summary>
    public static readonly Color EventoNormal     = Color.white;

    /// <summary>Bluetooth conectado: verde azulado.</summary>
    public static readonly Color BTConectado      = new Color(0.20f, 0.85f, 0.45f, 1f);

    /// <summary>Bluetooth desconectado: rojo.</summary>
    public static readonly Color BTDesconectado   = new Color(0.95f, 0.25f, 0.25f, 1f);

    // ─── BARRAS DE PROGRESO ───────────────────────────────────────────────────
    /// <summary>Relleno de barra de progreso activa.</summary>
    public static readonly Color BarraRelleno     = new Color(1.00f, 0.85f, 0.00f, 1f);  // Amarillo

    /// <summary>Fondo de barra de progreso.</summary>
    public static readonly Color BarraFondo       = new Color(0.05f, 0.08f, 0.15f, 1f);  // Azul muy oscuro

    // ─── BORDES ───────────────────────────────────────────────────────────────
    /// <summary>Borde de panel activo: amarillo tenue.</summary>
    public static readonly Color BordePanelActivo = new Color(1.00f, 0.85f, 0.00f, 0.60f);

    /// <summary>Borde de panel inactivo: azul marino con opacidad.</summary>
    public static readonly Color BordePanelInactivo = new Color(0.30f, 0.40f, 0.60f, 0.50f);
}
