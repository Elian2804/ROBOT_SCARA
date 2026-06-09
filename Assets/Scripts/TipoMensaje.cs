/// <summary>
/// Tipos de mensajes que puede mostrar la consola de log.
/// Determina el color de la línea en PanelConsola.
/// </summary>
public enum TipoMensaje
{
    /// <summary>Evento normal (trama T:) — blanco.</summary>
    Normal,

    /// <summary>Advertencia (A01-A08) — amarillo.</summary>
    Advertencia,

    /// <summary>Error (E01-E06) — rojo.</summary>
    Error,

    /// <summary>Éxito / conexión — verde.</summary>
    Exito,

    /// <summary>Log del sketch (trama L:) — cyan.</summary>
    Log
}
