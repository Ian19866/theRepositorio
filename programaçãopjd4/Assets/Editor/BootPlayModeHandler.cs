using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BootPlayModeHandler
{
    static BootPlayModeHandler()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // When leaving Edit mode to enter Play mode
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Ask user to save scenes if needed. If they cancel, cancel play.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid()) return;

            // If already on _Boot, don't change scenes
            if (active.name == "_Boot") return;

            // Try to find the _Boot scene in the project
            string[] guids = AssetDatabase.FindAssets("_Boot t:scene");
            string bootScenePath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == "_Boot")
                {
                    bootScenePath = p;
                    break;
                }
            }

            if (string.IsNullOrEmpty(bootScenePath))
            {
                Debug.LogWarning("Boot scene named '_Boot' not found. Play will continue with the current scene.");
                return;
            }

            // Open _Boot as the single scene so it will be the first loaded in Play
            Scene bootScene = EditorSceneManager.OpenScene(bootScenePath, OpenSceneMode.Single);

            // Ensure a temporary marker GameObject exists in the boot scene so runtime code can detect and act.
            string targetSceneName = System.IO.Path.GetFileNameWithoutExtension(active.path);
            var existingRoots = bootScene.GetRootGameObjects();
            // Remove any existing markers (startsWith) to avoid duplicates, then create a fresh marker with the target name
            foreach (var r in existingRoots)
            {
                if (r.name != null && r.name.StartsWith("_BootRuntimeTemp"))
                {
                    // destroy ephemeral marker
                    Object.DestroyImmediate(r);
                }
            }

            var goName = "_BootRuntimeTemp|" + targetSceneName;
            var go = new GameObject(goName);
            // Move the temp object into the boot scene
            SceneManager.MoveGameObjectToScene(go, bootScene);
            // Do NOT save the scene here - leave it unsaved so the project asset isn't changed.

            // Open the previously active scene additively so at runtime it will be loaded after _Boot
            var opened = EditorSceneManager.OpenScene(active.path, OpenSceneMode.Additive);
            // Make sure the previously active scene is set as the active scene in the editor
            EditorSceneManager.SetActiveScene(opened);
        }
    }
}





