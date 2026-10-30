using UnityEditor;
using UnityEngine;

namespace LunarConcerto.Lyruis.Editor
{
    [CustomEditor(typeof(LyruisLayout)), CanEditMultipleObjects]
    public sealed class LyruisLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach (string field in new[] { "mode", "widthMode", "heightMode", "fixedSize", "m_Padding", "m_ChildAlignment", "spacing" })
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field), true);
            var mode = serializedObject.FindProperty("mode");
            if (mode.hasMultipleDifferentValues || mode.enumValueIndex >= (int)LayoutMode.Grid)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("flowAxis"));
            if (mode.hasMultipleDifferentValues || mode.enumValueIndex == (int)LayoutMode.Grid)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gridCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"));
            }
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Rebuild Layout")) foreach (var item in targets) ((LyruisLayout)item).RebuildNow();
        }
    }
}
