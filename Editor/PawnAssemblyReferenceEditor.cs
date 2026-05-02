#if UNITY_EDITOR
using SackranyPawnAssembly.Components;

using UnityEditor;

using UnityEngine;

namespace SackranyPawnAssembly.Editor
{
    [CustomEditor(typeof(PawnAssemblyReference))]
    public class PawnAssemblyReferenceEditor : UnityEditor.Editor
    {
        bool _confirmPending;

        public override void OnInspectorGUI()
        {
            var reference = (PawnAssemblyReference)target;

            EditorGUILayout.LabelField("GUID", reference.Guid, EditorStyles.textField);

            EditorGUILayout.Space();

            if (!_confirmPending)
            {
                if (GUILayout.Button("Regenerate GUID"))
                    _confirmPending = true;
            }
            else
            {
                EditorGUILayout.HelpBox("All saves with this GUID will become invalid. Are you sure?", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();

                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Yes, regenerate"))
                {
                    var prop = serializedObject.FindProperty("_guid");
                    prop.stringValue = System.Guid.NewGuid().ToString();
                    serializedObject.ApplyModifiedProperties();
                    _confirmPending = false;
                }
                GUI.backgroundColor = prevColor;
                
                if (GUILayout.Button("Cancel"))
                    _confirmPending = false;

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif