using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine;

[InitializeOnLoad]
public static class ReloadMainMenuScene
{
    static ReloadMainMenuScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += CheckAndReload;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += CheckAndReload;
        }
    }

    private static void CheckAndReload()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            Debug.Log("<color=green>[ReloadMainMenuScene] Main Menu.unity berhasil direload bersih dari disk!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ReloadMainMenuScene] " + ex.Message);
        }

        // Hapus script ini sendiri setelah reload selesai
        EditorApplication.delayCall += () =>
        {
            try
            {
                string scriptPath = "Assets/Editor/ReloadMainMenuScene.cs";
                string metaPath = scriptPath + ".meta";
                if (File.Exists(scriptPath)) File.Delete(scriptPath);
                if (File.Exists(metaPath)) File.Delete(metaPath);
                AssetDatabase.Refresh();
            }
            catch {}
        };
    }
}
