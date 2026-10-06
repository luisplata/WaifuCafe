using UnityEngine;

/// <summary>
/// Prototipo: oleadas de clientes. Ciclo pico (rápido) / valle (lento) con rampa
/// progresiva durante la partida. Escribe WaveRate en el spawner cada frame.
/// Valores propuestos por el dueño: 10s pico + 5s valle.
/// </summary>
public class PrototypeWaves : MonoBehaviour
{
    [SerializeField] private float peakDuration = 10f;
    [SerializeField] private float valleyDuration = 5f;
    [SerializeField] private float peakRate = 1.7f;
    [SerializeField] private float valleyRate = 0.65f;
    [SerializeField] private float rampPerMinute = 0.12f;
    [SerializeField] private float maxRate = 2.5f;

    public bool IsPeak { get; private set; }

    private CustomerSpawnerCoroutine spawner;
    private float elapsed;

    private void Start()
    {
        spawner = FindFirstObjectByType<CustomerSpawnerCoroutine>();
        if (spawner == null)
        {
            Debug.LogError("PrototypeWaves: no hay CustomerSpawnerCoroutine en escena.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (spawner == null) return;
        elapsed += Time.deltaTime;
        float cycle = peakDuration + valleyDuration;
        IsPeak = (elapsed % cycle) < peakDuration;
        float ramp = Mathf.Min(1f + elapsed * (rampPerMinute / 60f), maxRate / peakRate);
        spawner.WaveRate = Mathf.Min((IsPeak ? peakRate : valleyRate) * ramp, maxRate);
    }
}
