using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Parry.EditorClass
{
    // UUM-150797: Unity 6000.6 resets the shared registry while Prefab Mode
    // Selectables can remain enabled. Remove when upgrading to a fixed Editor.
    [InitializeOnLoad]
    internal static class SelectableRegistryWorkaround
    {
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo Selectables = typeof(Selectable).GetField("s_Selectables", StaticFields);
        private static readonly FieldInfo Count = typeof(Selectable).GetField("s_SelectableCount", StaticFields);
        private static readonly FieldInfo Index = typeof(Selectable).GetField("m_CurrentIndex", InstanceFields);
        private static readonly FieldInfo EnableCalled = typeof(Selectable).GetField("m_EnableCalled", InstanceFields);

        static SelectableRegistryWorkaround()
        {
            if (!Application.unityVersion.StartsWith("6000.6."))
                return;

            AssemblyReloadEvents.beforeAssemblyReload += Repair;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.hierarchyChanged += QueueRepair;
            EditorApplication.projectChanged += QueueRepair;
            EditorApplication.delayCall += Repair;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            Repair();
            QueueRepair();
        }

        private static void QueueRepair()
        {
            EditorApplication.delayCall -= Repair;
            EditorApplication.delayCall += Repair;
        }

        internal static void Repair()
        {
            var registered = new List<Selectable>();
            foreach (var selectable in Resources.FindObjectsOfTypeAll<Selectable>())
            {
                if (selectable.gameObject.scene.IsValid() && (bool)EnableCalled.GetValue(selectable))
                    registered.Add(selectable);
            }

            var existing = (Selectable[])Selectables.GetValue(null);
            int count = (int)Count.GetValue(null);
            bool valid = count == registered.Count && count <= existing.Length && count >= 0;
            foreach (var selectable in registered)
            {
                int index = (int)Index.GetValue(selectable);
                if (index < 0 || index >= count || index >= existing.Length || existing[index] != selectable)
                    valid = false;
            }
            if (valid)
                return;

            var repaired = new Selectable[Mathf.Max(10, registered.Count)];
            for (int i = 0; i < registered.Count; i++)
            {
                repaired[i] = registered[i];
                Index.SetValue(registered[i], i);
            }
            Selectables.SetValue(null, repaired);
            Count.SetValue(null, registered.Count);
        }
    }
}
