using Parry.BossClass;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Parry.EditorClass
{
    public sealed class BossAttackClipWindow : EditorWindow
    {
        [SerializeField] private AnimationClip clip;
        private ObjectField clipField;
        private ScrollView eventList;
        private Label status;

        [MenuItem("Tools/Parry/Boss Attack Clip")]
        public static void Open()
        {
            var window = GetWindow<BossAttackClipWindow>("보스 타격 편집");
            window.minSize = new Vector2(420f, 420f);
            window.OnSelectionChange();
        }

        public static void Open(AnimationClip selectedClip)
        {
            Open();
            var window = GetWindow<BossAttackClipWindow>();
            window.SetClip(selectedClip);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state) => Refresh();

        private void OnUndoRedo()
        {
            if (clip != null)
                AssetDatabase.SaveAssetIfDirty(clip);
            Refresh(false);
        }

        private void OnSelectionChange()
        {
            AnimationClip selected = Selection.activeObject as AnimationClip;
            if (Selection.activeObject is BossAttackData attack)
                selected = attack.AnimationClip;
            else if (Selection.activeObject is BossAttackHitData hit)
                selected = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GetAssetPath(hit));
            if (selected != null && selected != clip)
                SetClip(selected);
        }

        private void SetClip(AnimationClip value)
        {
            clip = value;
            clipField?.SetValueWithoutNotify(value);
            Refresh();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            clipField = new ObjectField("공격 클립") { objectType = typeof(AnimationClip), allowSceneObjects = false };
            clipField.SetValueWithoutNotify(clip);
            clipField.RegisterValueChangedCallback(e => SetClip(e.newValue as AnimationClip));
            rootVisualElement.Add(clipField);
            status = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            rootVisualElement.Add(status);
            rootVisualElement.Add(new Button(Refresh) { text = "새로고침" });
            eventList = new ScrollView { style = { flexGrow = 1f } };
            rootVisualElement.Add(eventList);
            Refresh();
        }

        private void OnFocus() => Refresh();

        private void Refresh() => Refresh(true);

        private void Refresh(bool initializeHitData)
        {
            if (eventList == null)
                return;
            eventList.Unbind();
            eventList.Clear();
            bool editable = BossAttackClipEvents.CanEdit(clip);
            if (!editable)
            {
                status.text = "Edit 모드에서 편집할 .anim 클립을 선택하세요.";
                return;
            }

            if (initializeHitData)
                BossAttackClipEvents.EnsureHitData(clip);
            var events = AnimationUtility.GetAnimationEvents(clip);
            int hitNumber = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].functionName != BossAttackClipEvents.HitFunction)
                    continue;
                hitNumber++;
                var section = new Foldout { text = $"타격 {hitNumber} · {events[i].time:0.###}초", value = true };
                section.style.marginBottom = 12;
                eventList.Add(section);
                var hit = events[i].objectReferenceParameter as BossAttackHitData;
                if (hit != null)
                {
                    var serialized = new SerializedObject(hit);
                    foreach (string field in new[] { "damage", "attackType", "canKnockback", "knockbackDistance",
                        "causesFaint", "hitboxOffset", "hitboxSize" })
                        section.Add(new PropertyField(serialized.FindProperty(field)));
                    section.Bind(serialized);
                    section.RegisterCallback<SerializedPropertyChangeEvent>(_ => AssetDatabase.SaveAssetIfDirty(hit));
                }
                else
                {
                    section.Add(new HelpBox("Animation 창의 이벤트 Object 인자를 비우고 새로고침하면 타격 데이터가 연결됩니다.", HelpBoxMessageType.Warning));
                }
            }
            status.text = $"{clip.length:0.###}초 · 타격 {hitNumber}개. 이벤트 추가·삭제·시간 변경은 Animation 창에서 합니다.";
        }
    }
}
