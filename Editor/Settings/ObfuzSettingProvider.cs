using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obfuz
{
    public class ObfuzSettingsProvider : SettingsProvider
    {

        private static ObfuzSettingsProvider s_provider;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (s_provider == null)
            {
                s_provider = new ObfuzSettingsProvider();
                using (var so = new SerializedObject(ObfuzSettings.Instance))
                {
                    s_provider.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }
            return s_provider;
        }


        private SerializedObject _serializedObject;
        private SerializedProperty _enable;
        private SerializedProperty _mappingFile;
        private SerializedProperty _ruleFiles;
        private SerializedProperty _extraAssemblySearchDirs;

        public ObfuzSettingsProvider() : base("Project/Obfuz", SettingsScope.Project)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EditorStatusWatcher.OnEditorFocused += OnEditorFocused;
            InitGUI();
        }

        private void InitGUI()
        {
            var setting = ObfuzSettings.Instance;
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(setting);
            _enable = _serializedObject.FindProperty("enable");
            _mappingFile = _serializedObject.FindProperty("mappingFile");
            _ruleFiles = _serializedObject.FindProperty("ruleFiles");
            _extraAssemblySearchDirs = _serializedObject.FindProperty("extraAssemblySearchDirs");
        }

        private void OnEditorFocused()
        {
            InitGUI();
            Repaint();
        }

        public override void OnGUI(string searchContext)
        {
            using (CreateSettingsWindowGUIScope())
            {
                if (_serializedObject == null||!_serializedObject.targetObject)
                {
                    InitGUI();
                }
                _serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(_enable);
                EditorGUILayout.PropertyField(_mappingFile);
                EditorGUILayout.PropertyField(_ruleFiles);
                EditorGUILayout.PropertyField(_extraAssemblySearchDirs);

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    ObfuzSettings.Save();
                }
            }
        }

        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            EditorStatusWatcher.OnEditorFocused -= OnEditorFocused;
            ObfuzSettings.Save();
        }
    }
}