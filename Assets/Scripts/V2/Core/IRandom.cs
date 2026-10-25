using System;

namespace WaifuCafe.Core
{
    /// <summary>Azar inyectable: el modelo no depende de UnityEngine.Random.</summary>
    public interface IRandom
    {
        /// <summary>Entero en [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    /// <summary>Implementacion con semilla para que los tests sean reproducibles.</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom(int seed) => _random = new Random(seed);

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
