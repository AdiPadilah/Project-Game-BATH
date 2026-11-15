using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        FixMenuLayout();
    }

    void FixMenuLayout()
    {
        // ── Fix Main Menu container ───────────────────────────────────────
        // Pastikan container di kiri layar, vertikal center
        RectTransform me = GetComponent<RectTransform>();
        if (me != null)
        {
            me.anchorMin        = new Vector2(0f, 0.5f);
            me.anchorMax        = new Vector2(0f, 0.5f);
            me.pivot            = new Vector2(0f, 0.5f);
            me.anchoredPosition = new Vector2(80f, -40f);
            me.sizeDelta        = new Vector2(300f, 400f);
        }

        // ── Data tombol: (nama GO, nama text child, anchoredPos, size) ───
        var buttons = new (string btn, string txt, Vector2 pos, Vector2 size)[]
        {
            ("Play Button",      "Play Text",    new Vector2(0,  150), new Vector2(260, 52)),
            ("Options Button",   "Options Text", new Vector2(0,   80), new Vector2(260, 52)),
            ("Quit Button (1)",  "Quit Text",    new Vector2(0,   10), new Vector2(260, 52)),
            ("Quit Button",      "Quit Text",    new Vector2(0,  -60), new Vector2(260, 52)),
        };

        foreach (var (btnName, txtName, pos, size) in buttons)
        {
            // Cari tombol di dalam container ini
            Transform btnT = transform.Find(btnName);
            if (btnT == null) continue;

            // ── Fix RectTransform tombol ──
            RectTransform rt = btnT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta        = size;
            }

            // ── Fix Image background (transparan gelap, bukan kotak hitam) ──
            Image img = btnT.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.04f, 0.02f, 0.01f, 0f); // transparan total
            }

            // ── Fix TMP text word wrap ──
            // Cari semua TMP_Text di dalam tombol
            foreach (var tmp in btnT.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.enableWordWrapping  = false;
                tmp.overflowMode        = TextOverflowModes.Overflow;
                tmp.alignment           = TextAlignmentOptions.Center;
                tmp.fontSize            = 36f;
            }
        }

        Debug.Log("<color=lime>[MainMenu] Layout tombol berhasil difix saat runtime!</color>");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void kembali()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void Next()
    {
        SceneManager.LoadScene("Next");
    }

    public void About()
    {
        SceneManager.LoadScene("About");
    }
}
