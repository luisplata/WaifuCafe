using System;

namespace StateMachines
{
    // Temporizador simple para fases. No depende de UnityEngine para ser reutilizable
    public class PhaseTimer
    {
        public float Duration { get; private set; }
        public float Elapsed { get; private set; }

        public PhaseTimer()
        {
            Duration = 0f;
            Elapsed = 0f;
        }

        // Inicia el temporizador con una duración en segundos. Si duration <= 0 significa "no timer".
        public void Start(float duration)
        {
            Duration = Math.Max(0f, duration);
            Elapsed = 0f;
        }

        public void Stop()
        {
            Duration = 0f;
            Elapsed = 0f;
        }

        // Actualiza el temporizador; devuelve true si el timer ha terminado justo en esta actualización
        public bool Update(float delta)
        {
            if (Duration <= 0f) return false;
            Elapsed += delta;
            if (Elapsed >= Duration)
            {
                Stop();
                return true;
            }
            return false;
        }

        public float Remaining => Math.Max(0f, Duration - Elapsed);
        public bool IsRunning => Duration > 0f && Elapsed < Duration;
    }
}

