using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class VictoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject victoryPanel;
    public Image[] stars;
    public Sprite yellowStar;
    public Sprite defaultStar;

    [Header("Animation Settings")]
    public float delayInSeconds = 0.5f;

    private bool sudahMenang = false;

    void Start()
    {
        // Pastikan panel mati saat game dimulai
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    public void MunculkanMenang(float waktu)
    {
        if (sudahMenang) return;
        sudahMenang = true;

        // 1. Hentikan pergerakan/logika di script Player
        Player p = FindFirstObjectByType<Player>();
        if (p != null)
        {
            p.sudahMenang = true;
        }

        // 2. Validasi Panel
        if (victoryPanel == null)
        {
            Debug.LogError("❌ VictoryPanel belum diisi di Inspector!");
            return;
        }

        // 3. Munculkan Panel & Pause Game
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;

        // 4. Hitung Bintang berdasarkan durasi
        int jumlahBintang = HitungBintang(waktu);

        Debug.Log("🏆 Menang! Waktu Akhir: " + waktu + "s | Bintang didapat: " + jumlahBintang);

        // 5. Jalankan animasi bintang
        StartCoroutine(AnimasiBintang(jumlahBintang));
    }

    int HitungBintang(float waktu)
    {
        if (waktu <= 60f)
        {
            return 3;
        }
        else if (waktu <= 100f)
        {
            return 2;
        }
        else if (waktu <= 150f)
        {
            return 1;
        }
        else
        {
            return 0; // Lebih dari 150 detik tidak dapat bintang
        }
    }

    IEnumerator AnimasiBintang(int jumlah)
    {
        if (stars == null || stars.Length == 0)
        {
            Debug.LogError("❌ Array Stars kosong! Masukkan Image bintang ke Inspector.");
            yield break;
        }

        // --- RESET TAMPILAN AWAL (Bintang Mati) ---
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                if (defaultStar != null)
                    stars[i].sprite = defaultStar;

                stars[i].color = Color.gray; // Warna redup
                stars[i].transform.localScale = Vector3.one;
            }
        }

        // Tunggu sebentar sebelum animasi mulai (Gunakan Realtime karena timescale = 0)
        yield return new WaitForSecondsRealtime(0.5f);

        // --- PROSES MENGISI BINTANG ---
        int jumlahFix = Mathf.Min(jumlah, stars.Length);

        for (int i = 0; i < jumlahFix; i++)
        {
            if (stars[i] == null) continue;

            // Efek suara bisa ditaruh di sini jika ada (misal: AudioSource.PlayClipAtPoint)

            // Ganti ke sprite bintang kuning
            if (yellowStar != null)
                stars[i].sprite = yellowStar;

            stars[i].color = Color.white;

            // Animasi Pop (Membesar lalu mengecil)
            float t = 0;
            float speed = 5f;
            Vector3 scaleBesar = Vector3.one * 1.5f;

            while (t < 1.0f)
            {
                t += Time.unscaledDeltaTime * speed;
                stars[i].transform.localScale = Vector3.Lerp(scaleBesar, Vector3.one, t);
                yield return null;
            }

            // Jeda antar bintang
            yield return new WaitForSecondsRealtime(delayInSeconds);
        }
    }

    // Fungsi untuk Button UI
    public void KeHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }

    public void KeNext()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}