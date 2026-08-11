using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    private BossAI boss;

    void Awake()
    {
        boss = GetComponentInParent<BossAI>();

        if (boss == null)
        {
            Debug.LogError("❌ Relay Gagal: BossAI tidak ditemukan!");
        }
    }

    // 🔥 EVENT DAMAGE
    public void RelayDealDamage()
    {
        // 🛑 STOP TOTAL SAAT GAME OVER
        if (UI.instance != null && UI.instance.isGameOver)
        {
            Debug.Log("🚫 Damage dibatalkan (Game Over)");
            return;
        }

        if (boss == null || !boss.isActiveAndEnabled)
            return;

        boss.DealDamage();
        Debug.Log("📡 Relay kirim damage");
    }

    // 🔥 EVENT AKHIR ATTACK
    public void RelayEndAttack()
    {
        if (UI.instance != null && UI.instance.isGameOver)
            return;

        if (boss == null || !boss.isActiveAndEnabled)
            return;

        boss.EndAttack();
    }

    // 🔥 AUTO MATI SAAT GAME OVER
    void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver)
        {
            Debug.Log("🛑 Relay dimatikan");
            enabled = false;
        }
    }
}