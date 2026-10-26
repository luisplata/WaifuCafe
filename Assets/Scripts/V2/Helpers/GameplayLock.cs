namespace V2.Helpers
{
    /// <summary>
    /// Candado unico de gameplay con contador. Invariante: si esta tomado, el jugador no
    /// puede actuar Y el mundo no avanza (CafeFloor congela sus relojes). Todo
    /// <see cref="Hold"/> debe tener exactamente un <see cref="Release"/> en el mismo camino
    /// de salida (fin natural, skip, error, destroy). No se toma dos veces por el mismo
    /// evento: el contador permite bloqueos superpuestos, pero un desbalance congela o
    /// libera el juego antes de tiempo.
    /// </summary>
    public static class GameplayLock
    {
        private static int _count;

        public static bool IsLocked => _count > 0;
        public static int Count => _count;

        public static void Hold() => _count++;

        public static void Release() => _count = _count > 0 ? _count - 1 : 0;
    }
}
