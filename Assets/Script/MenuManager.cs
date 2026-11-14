using UnityEngine;

public class MenuManager : MonoBehaviour
{
  
    public GameObject aboutPanel;

   
    public void OpenAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(true);
        else
            Debug.LogWarning("AboutPanel belum di-assign di Inspector!");
    }

   
    public void CloseAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
    }
}
