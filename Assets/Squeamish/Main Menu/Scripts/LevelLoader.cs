using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] int sceneIndex;

    [Header("UI")]
    [SerializeField] GameObject loadingUI;
    [SerializeField] Slider slider;
    [SerializeField] TMP_Text progressText;

    public void LoadLevel()
    {
        if (loadingUI != null)
            loadingUI.SetActive(true);

        StartCoroutine(LoadScene());
    }

    public void LoadLevelByIndex(int index)
    {
        sceneIndex = index;
        LoadLevel();
    }

    IEnumerator LoadScene()
    {
        // Mulai load scene (ditahan dulu)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        float duration = 2f; // waktu loading biar keliatan (bisa ubah)
        float timer = 0f;

        // 🔥 Loading palsu biar smooth (0 → 100%)
        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;

            if (slider != null)
                slider.value = progress;

            if (progressText != null)
                progressText.text = "LOADING " + Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        // 🔥 Tunggu Unity bener-bener siap
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // Pastikan 100%
        if (slider != null)
            slider.value = 1f;

        if (progressText != null)
            progressText.text = "LOADING COMPLETE";

        yield return new WaitForSeconds(0.2f);

        // 🔥 Baru masuk scene
        operation.allowSceneActivation = true;
    }
}