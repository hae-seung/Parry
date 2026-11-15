using Parry.MonsterClass;
using UnityEditor;

namespace Parry.EditorClass
{
    [CustomEditor(typeof(MonsterAttackData)), CanEditMultipleObjects]
    public sealed class MonsterAttackDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            SerializedProperty canKnockback = serializedObject.FindProperty("canKnockback");

            // 넉백을 사용하는 공격에서만 밀림 거리를 설정한다.
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "knockbackDistance" &&
                    !canKnockback.hasMultipleDifferentValues && !canKnockback.boolValue)
                    continue;

                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);

                if (property.name == "attackType" && !property.hasMultipleDifferentValues &&
                    property.enumValueIndex == (int)MonsterAttackType.Heavy)
                    EditorGUILayout.HelpBox("강공격은 적중 시 항상 기절을 적용합니다.", MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
