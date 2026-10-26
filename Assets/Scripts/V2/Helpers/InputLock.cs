namespace V2.Helpers
{
    /// <summary>
    /// Candado global de input con contador. Cualquier sistema que necesite secuestrar
    /// el control (cinematica, fin de dia) lo adquiere y lo libera al terminar. Con
    /// contador, dos bloqueos superpuestos no se pisan al liberar.
    /// </summary>
    public static class InputLock
    {
        private static int _count;

        public static bool IsLocked => _count > 0;
        public static int Count => _count;

        public static void Acquire() => _count++;

        public static void Release() => _count = _count > 0 ? _count - 1 : 0;
    }
}
