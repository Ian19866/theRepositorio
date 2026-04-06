using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootRuntimeController : MonoBehaviour
{
    // This class is intentionally lightweight. The actual startup hook is below.
}

public static class BootRuntimeInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        // Create a runner object to perform the unload logic in a coroutine
        var runnerGo = new GameObject("_BootRuntimeRunner");
        Object.DontDestroyOnLoad(runnerGo);
        runnerGo.AddComponent<BootRuntimeRunner>();
    }

    private class BootRuntimeRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Wait one frame so all additive scenes opened by the editor are available
            yield return null;

            Debug.Log("BootRuntimeRunner: scanning loaded scenes...");

            // Find the boot scene (prefer scene that contains the marker) and detect a target scene name from the marker
            Scene bootScene = default;
            string targetSceneNameFromMarker = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid()) continue;
                var roots = s.GetRootGameObjects();
                foreach (var r in roots)
                {
                    if (r.name.StartsWith("_BootRuntimeTemp"))
                    {
                        bootScene = s;
                        var parts = r.name.Split('|');
                        if (parts.Length > 1) targetSceneNameFromMarker = parts[1];
                        break;
                    }
                }

                if (bootScene.IsValid())
                {
                    Debug.Log($"BootRuntimeRunner: boot scene detected '{s.name}' via marker.");
                    break;
                }
            }

            // If boot scene not found by marker, fallback to name
            if (!bootScene.IsValid())
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (!s.IsValid()) continue;
                    if (s.name == "_Boot") { bootScene = s; Debug.Log("BootRuntimeRunner: boot scene found by name."); break; }
                }
            }


            // Decide target scene: prefer explicit marker name, then active scene, then first non-boot
            Scene targetScene = default;
            if (!string.IsNullOrEmpty(targetSceneNameFromMarker))
            {
                // try to find if already loaded
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.IsValid() && s.name == targetSceneNameFromMarker) { targetScene = s; break; }
                }
            }

            var runtimeActive = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() && runtimeActive.IsValid() && runtimeActive.name != "_Boot")
                targetScene = runtimeActive;

            if (!targetScene.IsValid())
            {
                // pick first non-boot scene
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.IsValid() && s.name != "_Boot") { targetScene = s; break; }
                }
            }

            if (!bootScene.IsValid())
            {
                Debug.LogWarning("BootRuntimeRunner: _Boot scene not found among loaded scenes. Aborting unload.");
                Destroy(gameObject);
                yield break;
            }

            if (!targetScene.IsValid())
            {
                Debug.LogWarning("BootRuntimeRunner: no other scene found to switch to. Aborting unload.");
                Destroy(gameObject);
                yield break;
            }

            // If the target scene is not loaded, but we have a marker with the scene name, try to load it additively
            if (!targetScene.isLoaded)
            {
                if (!string.IsNullOrEmpty(targetSceneNameFromMarker))
                {
                    Debug.Log($"BootRuntimeRunner: target scene '{targetSceneNameFromMarker}' not loaded; attempting additive load...");
                    var loadOp = SceneManager.LoadSceneAsync(targetSceneNameFromMarker, LoadSceneMode.Additive);
                    if (loadOp != null)
                    {
                        while (!loadOp.isDone) yield return null;
                        // refresh reference
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            var s = SceneManager.GetSceneAt(i);
                            if (s.IsValid() && s.name == targetSceneNameFromMarker) { targetScene = s; break; }
                        }
                    }
                }
            }

            if (!targetScene.isLoaded)
            {
                Debug.LogWarning($"BootRuntimeRunner: target scene '{targetScene.name}' is not loaded at runtime. Aborting unload.");
                Destroy(gameObject);
                yield break;
            }

            Debug.Log("BootRuntimeRunner: loaded scenes at unload time:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                Debug.Log($" - {s.name} (loaded={s.isLoaded}, roots={s.rootCount})");
            }

            // Set the target scene as active so runtime context moves there
            bool setActive = SceneManager.SetActiveScene(targetScene);
            Debug.Log($"BootRuntimeRunner: SetActiveScene('{targetScene.name}') returned {setActive}");

            // Small delay to ensure activation has propagated
            yield return null;

            // Unload boot scene
            Debug.Log($"BootRuntimeRunner: unloading boot scene '{bootScene.name}'...");
            var unloadOp = SceneManager.UnloadSceneAsync(bootScene);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone) yield return null;
                Debug.Log("BootRuntimeRunner: boot scene unloaded.");
            }
            else
            {
                Debug.LogWarning("BootRuntimeRunner: UnloadSceneAsync returned null.");
            }

            // Done
            Destroy(gameObject);
        }
    }

    private static Camera FindCameraInScene(Scene scene)
    {
        if (!scene.IsValid()) return null;
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            var cam = r.GetComponentInChildren<Camera>(includeInactive: true);
            if (cam != null) return cam;
        }
        return null;
    }
}



