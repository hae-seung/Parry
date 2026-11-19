using System;
using Parry.BossClass;
using UnityEditor;
using UnityEngine;

namespace Parry.EditorClass
{
    public static class BossAttackClipEvents
    {
        public const string HitFunction = "ApplyAttackHit";

        public static bool CanEdit(AnimationClip clip)
        {
            return clip != null && !EditorApplication.isPlayingOrWillChangePlaymode &&
                AssetDatabase.IsMainAsset(clip) &&
                AssetDatabase.GetAssetPath(clip).EndsWith(".anim", StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureHitData(AnimationClip clip)
        {
            if (!CanEdit(clip))
                return;

            var events = AnimationUtility.GetAnimationEvents(clip);
            bool changed = false;
            foreach (var animationEvent in events)
            {
                if (animationEvent.functionName != HitFunction || animationEvent.objectReferenceParameter != null)
                    continue;

                if (!changed)
                {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("Connect boss hit data");
                    Undo.RecordObject(clip, "Connect boss hit data");
                }
                var hit = ScriptableObject.CreateInstance<BossAttackHitData>();
                hit.name = "Hit_" + GUID.Generate().ToString().Substring(0, 8);
                AssetDatabase.AddObjectToAsset(hit, clip);
                Undo.RegisterCreatedObjectUndo(hit, "Connect boss hit data");
                animationEvent.objectReferenceParameter = hit;
                changed = true;
            }
            if (!changed)
                return;

            AnimationUtility.SetAnimationEvents(clip, events);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }
    }
}
