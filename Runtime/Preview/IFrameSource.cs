using UnityEngine;

namespace AlexSamGame.Core
{
    // скрипт объекта, который умеет нарезать атлас кодом (аналог GenerateQuads… лектора)
    public interface IFrameSource
    {
        Sprite[] GetFrames(Texture2D atlas);
    }
}
