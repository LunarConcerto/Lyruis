using UnityEditor;
using UnityEngine;

namespace LunarConcerto.Lyruis.Editor
{
    internal static class LyruisInspectorGUI
    {
        internal static GUIContent Content(string text, string tooltip = "") => new GUIContent(text, tooltip);
        internal static void Heading(string title)
        { EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }
        internal static void Hint(string text)
        { EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel); }
        internal static void Popup(SerializedProperty property, GUIContent label, string[] choices)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, label, property);
            bool previous = EditorGUI.showMixedValue; EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var options = new GUIContent[choices.Length];
            for (int i = 0; i < choices.Length; i++) options[i] = Content(choices[i]);
            int value = EditorGUI.Popup(rect, label, property.intValue, options);
            if (EditorGUI.EndChangeCheck()) property.intValue = value;
            EditorGUI.showMixedValue = previous; EditorGUI.EndProperty();
        }
        internal static bool Float(Rect rect, SerializedProperty property, GUIContent label, bool nonnegative = false)
        {
            EditorGUI.BeginProperty(rect, label, property);
            bool previous = EditorGUI.showMixedValue; EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck(); float value = EditorGUI.FloatField(rect, label, property.floatValue);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed) property.floatValue = nonnegative ? (float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value)) : value;
            EditorGUI.showMixedValue = previous; EditorGUI.EndProperty(); return changed;
        }
        internal static bool Integer(Rect rect, SerializedProperty property, GUIContent label, bool nonnegative = false)
        {
            EditorGUI.BeginProperty(rect, label, property);
            bool previous = EditorGUI.showMixedValue; EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck(); int value = EditorGUI.IntField(rect, label, property.intValue);
            bool changed = EditorGUI.EndChangeCheck(); if (changed) property.intValue = nonnegative ? Mathf.Max(0, value) : value;
            EditorGUI.showMixedValue = previous; EditorGUI.EndProperty(); return changed;
        }
        internal static void Pair(SerializedProperty vector, GUIContent label, bool nonnegative = false)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            Rect body = EditorGUI.PrefixLabel(rect, label);
            float width = (body.width - 6) / 2;
            float previous = EditorGUIUtility.labelWidth; EditorGUIUtility.labelWidth = 14;
            Float(new Rect(body.x, body.y, width, body.height), vector.FindPropertyRelative("x"), Content("X"), nonnegative);
            Float(new Rect(body.x + width + 6, body.y, width, body.height), vector.FindPropertyRelative("y"), Content("Y"), nonnegative);
            EditorGUIUtility.labelWidth = previous;
        }
        internal static void Alignment(SerializedProperty property, bool chinese)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 65);
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            const float cellWidth = 28, cellHeight = 20;
            int selected = property.hasMultipleDifferentValues ? -1 : property.intValue;
            string[] names = chinese ? new[] { "左上", "上中", "右上", "左中", "居中", "右中", "左下", "下中", "右下" } :
                new[] { "Top left", "Top center", "Top right", "Middle left", "Center", "Middle right", "Bottom left", "Bottom center", "Bottom right" };
            for (int i = 0; i < 9; i++)
            {
                int column = i % 3, row = i / 3;
                Rect button = new Rect(rect.x + column * (cellWidth + 2), rect.y + row * (cellHeight + 2), cellWidth, cellHeight);
                if (GUI.Toggle(button, selected == i, Content("", names[i]), EditorStyles.miniButton) && selected != i) property.intValue = i;
                if (Event.current.type != EventType.Repaint) continue;
                Color ink = selected == i ? new Color(.25f, .72f, 1f) : (EditorGUIUtility.isProSkin ? new Color(.72f, .74f, .77f) : new Color(.25f, .27f, .3f));
                float y = button.y + 4 + row * 3;
                for (int line = 0; line < 3; line++)
                {
                    float length = line == 1 ? 7 : 11;
                    float x = column == 0 ? button.x + 5 : column == 2 ? button.xMax - 5 - length : button.center.x - length / 2;
                    EditorGUI.DrawRect(new Rect(x, y + line * 3, length, 1), ink);
                }
            }
            Rect caption = new Rect(rect.x + 99, rect.y + 2, Mathf.Max(0, rect.width - 99), 20);
            EditorGUI.LabelField(caption, selected < 0 ? (chinese ? "混合对齐" : "Mixed alignment") : names[selected], EditorStyles.boldLabel);
            caption.y += 23; caption.height = 38;
            EditorGUI.LabelField(caption, chinese ? "对齐内容块；Flow 同时对齐每一行和行内子项。" : "Aligns the content block. Flow also aligns each line and its items.", EditorStyles.wordWrappedMiniLabel);
            EditorGUI.EndProperty();
        }
    }
}
