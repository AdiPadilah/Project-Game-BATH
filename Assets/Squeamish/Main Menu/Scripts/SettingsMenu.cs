using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
// CATATAN: Dihapus 'using Unity.VisualScripting.Antlr3.Runtime' (library internal, tidak ada di build)
// CATATAN: Dihapus 'using UnityEngine.UIElements' (konflik namespace dengan UnityEngine.UI)

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;

    public TMPro.TMP_Dropdown resolutionDropdown;

    Resolution[] resolutions;

    private void Start()
    {
        // Guard: jika dropdown tidak diassign, skip
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("SettingsMenu: resolutionDropdown belum di-assign di Inspector!");
            return;
        }

        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio + "hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void setResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex < 0 || resolutionIndex >= resolutions.Length) return;
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("SettingsMenu: AudioMixer belum di-assign di Inspector!");
            return;
        }
        audioMixer.SetFloat("Master Volume", volume);
    }

    public void SetQuality(float qualityIndex)
    {
        int qi = Mathf.RoundToInt(qualityIndex);
        QualitySettings.SetQualityLevel(qi);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
