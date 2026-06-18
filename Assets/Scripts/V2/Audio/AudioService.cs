using UnityEngine;

namespace V2.Audio
{
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        public static AudioService Instance;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void StartSfx(AudioClip clip)
        {
            if (audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}