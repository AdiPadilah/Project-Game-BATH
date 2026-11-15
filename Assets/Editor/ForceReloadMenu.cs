using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auto-reload Main Menu scene dari disk setiap kali keluar Play Mode.
/// Juga ada menu manual: Tools > RELOAD MAIN MENU
/// </summary>
[InitializeOnLoad]
public static class ForceReloadMenu
{
    static ForceReloadMenu()
    {
        EditorApplication.playModeStateChanged += OnModeChanged;
        EditorApplication.delayCall += TryReload;
    }

    static void OnModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Tunggu 2 frame agar Unity selesai restore dari backup
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += TryReload;
        }
    }

    [MenuItem("Tools/RELOAD MAIN MENU DARI DISK")]
    public static void TryReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ForceReloadMenu] Stop Play Mode dulu!");
            return;
        }

        // Tandai scene tidak perlu disave (buang perubahan dari 0.backup)
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            Debug.Log("<color=cyan>[ForceReloadMenu] ✓ Main Menu.unity berhasil di-reload dari disk!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ForceReloadMenu] GAGAL reload: " + ex.Message);
        }
    }
}
