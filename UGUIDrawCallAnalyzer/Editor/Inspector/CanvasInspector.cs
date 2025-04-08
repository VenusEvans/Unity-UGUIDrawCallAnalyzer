using System;
using UnityEditor;
using UnityEngine;

namespace Venus.UGUIDrawCallAnalyzer
{
    [CustomEditor(typeof(Canvas))]
    public class CanvasInspector : Editor
    {
        
        private Editor _originalEditor;
        public override void OnInspectorGUI()
        {
            if (_originalEditor == null)
            {
                _originalEditor = CreateEditor(target, Type.GetType("UnityEditor.CanvasEditor, UnityEditor"));
            }
            _originalEditor.OnInspectorGUI();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open UGUI DrawCall Analyzer Window"))
            {
                UGUIDrawCallAnalyzerWindow.ShowWindow(target as Canvas);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}


