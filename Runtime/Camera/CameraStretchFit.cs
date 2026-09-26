using UnityEngine;

namespace AlexSamGame.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraStretchFit : MonoBehaviour
    {
        [SerializeField] private float virtualWidth = 432f;
        [SerializeField] private float virtualHeight = 243f;

        void Start()
        {
            Camera cam = GetComponent<Camera>();
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.projectionMatrix = Matrix4x4.Ortho(
                -virtualWidth / 2f, virtualWidth / 2f,
                -virtualHeight / 2f, virtualHeight / 2f,
                cam.nearClipPlane, cam.farClipPlane);
        }
    }
}