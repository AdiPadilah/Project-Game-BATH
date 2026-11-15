using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auto-reload Main Menu scene dari disk setiap kali keluar Play Mode.
/// Pasang script ini sekali di Assets/Editor/ — tidak perlu hapus.
/// </summary>
[InitializeOnLoad]
public static class ForceReloadMenu
{
    static ForceReloadMenu()
    {
        EditorApplication.playModeStateChanged += OnModeChanged;
        // Juga reload sekali saat Unity startup / compile ulang
        EditorApplication.delayCall += TryReload;
    }

    static void OnModeChanged(PlayModeStateChange state)
    {
        // Saat baru keluar dari Play Mode → reload scene dari disk
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryReload;
        }
    }

    static void TryReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            Debug.Log("<color=cyan>[ForceReloadMenu] ✓ Main Menu.unity berhasil di-reload dari disk!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ForceReloadMenu] Gagal reload: " + ex.Message);
        }
    }
}
