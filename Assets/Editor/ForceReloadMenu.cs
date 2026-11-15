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
        string targetPath = "Assets/Scenes/Main Menu.unity";
        if (activeScene.name == "About")
        {
            targetPath = "Assets/Scenes/About.unity";
        }
        else if (activeScene.name == "GamePlay")
        {
            targetPath = "Assets/Scenes/GamePlay.unity";
        }

        try
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
            Debug.Log($"<color=cyan>[ForceReloadMenu] ✓ {System.IO.Path.GetFileName(targetPath)} berhasil di-reload dari disk!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ForceReloadMenu] GAGAL reload: " + ex.Message);
        }
    }

    [MenuItem("Tools/RELOAD SCENE AKTIF DARI DISK")]
    public static void ReloadActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ForceReloadMenu] Stop Play Mode dulu!");
            return;
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path)) return;

        try
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            Debug.Log($"<color=cyan>[ForceReloadMenu] ✓ {activeScene.name}.unity berhasil di-reload dari disk!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ForceReloadMenu] GAGAL reload: " + ex.Message);
        }
    }
}
