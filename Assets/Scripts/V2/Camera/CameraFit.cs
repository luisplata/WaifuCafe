using UnityEngine;
using Unity.Cinemachine; // CM3 (3.1.7)

namespace V2.Camera
{
    /// Fit-width ortho: keeps designWidth fully visible at any aspect; vertical adapts.
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private float designWidth = 26.9f; // scene bounds 26.88w x 15.36h (16:9)

        private CinemachineCamera _cam;

        private void Awake()
        {
            _cam = GetComponent<CinemachineCamera>();
        }

        private void LateUpdate()
        {
            float aspect = _cam.Lens.Aspect;
            if (aspect <= 0.01f) return;

            float target = (designWidth / 2f) / aspect;
            if (!Mathf.Approximately(_cam.Lens.OrthographicSize, target))
            {
                _cam.Lens.OrthographicSize = target;
            }
        }
    }
}