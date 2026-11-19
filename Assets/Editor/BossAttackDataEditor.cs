using Parry.BossClass;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Parry.EditorClass
{
    [CustomEditor(typeof(BossAttackData))]
    public sealed class BossAttackDataEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            foreach (string field in new[] { "animationClip", "distanceCondition", "range", "cooldown", "attackEndDelayDuration" })
                root.Add(new PropertyField(serializedObject.FindProperty(field)));
            root.Add(new Button(() => BossAttackClipWindow.Open(((BossAttackData)target).AnimationClip))
            {
                text = "클립의 타격 데이터 편집"
            });
            return root;
        }
    }
}
