using UnityEngine;

public class MenuManager : MonoBehaviour
{
  
    public GameObject aboutPanel;

   
    public void OpenAbout()
    {
        aboutPanel.SetActive(true);
    }

   
    public void CloseAbout()
    {
        aboutPanel.SetActive(false);
    }
}