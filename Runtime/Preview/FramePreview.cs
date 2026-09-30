using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlexSamGame.Core
{
    // превью кадра из атласа в редакторе, без Play. В игре не делает ничего
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FramePreview : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum Source { Script, File }

        [SerializeField] private Texture2D atlas;
        [SerializeField] private Source source = Source.Script;
        [SerializeField] private int frame;

        private Sprite[] frames;              // кэш нарезки этого объекта
        private Texture2D framesAtlas;
        private Source framesSource;

        // сколько кадров в текущей нарезке — нужно ползунку в инспекторе
        public int FrameCount => frames?.Length ?? 0;

        void OnEnable() => Refresh();
        void OnValidate() => Refresh();

        private void Refresh()
        {
            if (Application.isPlaying || atlas == null) return;

            if (frames == null || framesAtlas != atlas || framesSource != source)
            {
                frames = source == Source.Script ? FromScript() : FromFile();
                framesAtlas = atlas;
                framesSource = source;
            }

            if (frames == null || frames.Length == 0) return;

            frame = Mathf.Clamp(frame, 0, frames.Length - 1);
            GetComponent<SpriteRenderer>().sprite = frames[frame];
        }

        // нарезка кодом: её отдаёт скрипт этого же объекта
        private Sprite[] FromScript()
        {
            IFrameSource script = GetComponent<IFrameSource>();
            if (script == null)
            {
                Debug.LogWarning($"{name}: нет скрипта с IFrameSource — выбери Source = File", this);
                return null;
            }

            Sprite[] result = script.GetFrames(atlas);
            foreach (Sprite f in result) f.hideFlags = HideFlags.DontSave;
            return result;
        }

        // нарезка из Sprite Editor: все слайсы внутри png, по имени
        private Sprite[] FromFile()
        {
            string path = AssetDatabase.GetAssetPath(atlas);
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }
#endif
    }

#if UNITY_EDITOR
    // свой вид компонента в инспекторе: Frame — ползунок от 0 до (кадров − 1)
    [CustomEditor(typeof(FramePreview))]
    public class FramePreviewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("atlas"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("source"));

            SerializedProperty frameProp = serializedObject.FindProperty("frame");
            int count = ((FramePreview)target).FrameCount;

            if (count > 0)
            {
                frameProp.intValue = EditorGUILayout.IntSlider("Frame", frameProp.intValue, 0, count - 1);
            }
            else
            {
                EditorGUILayout.HelpBox("Нет кадров: укажи Atlas, проверь Source и включи объект", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}