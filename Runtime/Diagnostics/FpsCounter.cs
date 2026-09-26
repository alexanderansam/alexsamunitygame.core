using UnityEngine;
using TMPro;
namespace AlexSamGame.Core
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float updateInterval = 0.5f;

        private float timer;
        private int frames;

        void Update()
        {
            frames++;
            timer += Time.unscaledDeltaTime;

            if (timer >= updateInterval)
            {
                int fps = Mathf.RoundToInt(frames / timer);
                label.text = "FPS: " + fps;
                frames = 0;
                timer = 0f;
            }
        }
    }

}
