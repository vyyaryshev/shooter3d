using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter3D.AutoSave
{
    [InitializeOnLoad]
    internal static class ProjectAutoSave
    {
        private const string SettingsPath = "ProjectSettings/Shooter3DAutoSave.json";
        private const string DeadlineKey = "Shooter3D.AutoSave.Deadline";
        private static readonly HashSet<int> WarnedUntitledScenes = new HashSet<int>();
        private static readonly Configuration Settings = LoadSettings();
        private static bool saving;

        [Serializable]
        private sealed class Configuration
        {
            public bool enabled = true;
            public int intervalSeconds = 600;
            public bool saveScenes = true;
            public bool saveAssets = true;
            public bool logSaves = true;
        }

        static ProjectAutoSave()
        {
            // SessionState survives domain reloads, so editing scripts doesn't restart the timer.
            if (SessionState.GetFloat(DeadlineKey, -1f) < 0f)
                ScheduleNextSave();
            EditorApplication.update += Update;
        }

        private static Configuration LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    Configuration result = JsonUtility.FromJson<Configuration>(File.ReadAllText(SettingsPath));
                    if (result != null)
                    {
                        result.intervalSeconds = Mathf.Max(60, result.intervalSeconds);
                        return result;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Auto Save] Settings could not be loaded: " + exception.Message);
            }
            return new Configuration();
        }

        private static bool CanSave => !saving && !Application.isBatchMode
            && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused
            && !EditorApplication.isCompiling && !EditorApplication.isUpdating
            && !BuildPipeline.isBuildingPlayer && GUIUtility.hotControl == 0;

        private static void Update()
        {
            if (Settings.enabled && CanSave && EditorApplication.timeSinceStartup >=
                SessionState.GetFloat(DeadlineKey, float.MaxValue))
                SaveNow();
        }

        private static void ScheduleNextSave()
        {
            SessionState.SetFloat(DeadlineKey, (float)(EditorApplication.timeSinceStartup + Settings.intervalSeconds));
        }

        [MenuItem("Tools/Shooter3D/Auto Save/Save Now")]
        private static void SaveNow()
        {
            if (!CanSave)
                return;
            saving = true;
            int savedScenes = 0;
            bool failed = false;
            try
            {
                if (Settings.saveScenes)
                {
                    PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty
                            || EditorSceneManager.IsPreviewScene(scene)
                            || (stage != null && scene == stage.scene))
                            continue;
                        // An untitled scene needs a user-chosen path; don't open a timed save dialog.
                        if (string.IsNullOrEmpty(scene.path))
                        {
                            if (WarnedUntitledScenes.Add(scene.handle))
                                Debug.LogWarning("[Auto Save] Save the untitled scene once with File > Save As to enable auto save for it.");
                            continue;
                        }
                        try
                        {
                            if (EditorSceneManager.SaveScene(scene))
                                savedScenes++;
                            else
                            {
                                failed = true;
                                Debug.LogError("[Auto Save] Could not save scene: " + scene.path);
                            }
                        }
                        catch (Exception exception)
                        {
                            failed = true;
                            Debug.LogException(exception);
                        }
                    }
                }
                if (Settings.saveAssets)
                    AssetDatabase.SaveAssets();
                if (Settings.logSaves)
                    Debug.Log("[Auto Save] " + DateTime.Now.ToString("HH:mm:ss") + ": saved scenes: "
                        + savedScenes + "; assets: " + Settings.saveAssets + (failed ? "; some scenes failed." : "."));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                saving = false;
                ScheduleNextSave();
            }
        }

        [MenuItem("Tools/Shooter3D/Auto Save/Save Now", true)]
        private static bool ValidateSaveNow() => CanSave;

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Shooter3D/Auto Save", SettingsScope.Project)
            {
                label = "Auto Save",
                keywords = new HashSet<string> { "auto", "save", "autosave", "scene", "сохранение" },
                guiHandler = _ =>
                {
                    EditorGUI.BeginChangeCheck();
                    Settings.enabled = EditorGUILayout.Toggle("Enabled", Settings.enabled);
                    Settings.intervalSeconds = Mathf.Max(1, EditorGUILayout.IntField("Interval (minutes)",
                        Settings.intervalSeconds / 60)) * 60;
                    Settings.saveScenes = EditorGUILayout.Toggle("Save modified scenes", Settings.saveScenes);
                    Settings.saveAssets = EditorGUILayout.Toggle("Save modified assets", Settings.saveAssets);
                    Settings.logSaves = EditorGUILayout.Toggle("Log saves to Console", Settings.logSaves);
                    if (EditorGUI.EndChangeCheck())
                    {
                        File.WriteAllText(SettingsPath, JsonUtility.ToJson(Settings, true) + "\n");
                        ScheduleNextSave();
                    }
                    EditorGUILayout.HelpBox("Saves in Edit Mode. New scenes need an initial Save As. "
                        + "Prefab Mode uses Unity's own Auto Save toggle.", MessageType.Info);
                    using (new EditorGUI.DisabledScope(!CanSave))
                        if (GUILayout.Button("Save Now"))
                            SaveNow();
                }
            };
        }
    }
}
