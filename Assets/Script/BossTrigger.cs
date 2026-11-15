using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossAI boss;
    [SerializeField] private float panSpeed = 6f;
    [SerializeField] private float roarDuration = 2.0f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            Player player = col.GetComponent<Player>();
            StartCoroutine(BossIntroCutscene(player));
        }
    }

    private IEnumerator BossIntroCutscene(Player player)
    {
        // 1. Kunci kontrol Player dan posisikan menghadap Boss
        if (player != null)
        {
            player.canMove = false;
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity = new Vector2(0f, prb.velocity.y);
            Animator panim = player.GetComponentInChildren<Animator>();
            if (panim != null)
            {
                panim.SetFloat("xVelocity", 0f);
            }

            // Hadapkan player ke arah boss
            if (boss != null)
            {
                float dirToBoss = boss.transform.position.x - player.transform.position.x;
                if ((dirToBoss > 0 && !player.facingRight) || (dirToBoss < 0 && player.facingRight))
                {
                    player.Flip();
                }
            }
        }

        // 2. Kamera menyorot halus ke arah Boss
        if (CameraFollow.instance != null && boss != null)
        {
            float origSpeed = CameraFollow.instance.smoothSpeed;
            CameraFollow.instance.smoothSpeed = panSpeed;
            CameraFollow.instance.overrideTarget = boss.transform;
            yield return new WaitForSeconds(0.7f);

            // 3. Boss memainkan animasi mengaum / bersiap siaga + efek auman & guncangan layar
            boss.PlayRoar();
            CameraFollow.instance.TriggerScreenShake(1.4f, 0.22f);

            yield return new WaitForSeconds(roarDuration);

            // 4. Kamera kembali ke Player
            CameraFollow.instance.overrideTarget = null;
            yield return new WaitForSeconds(0.6f);
            CameraFollow.instance.smoothSpeed = origSpeed;
        }
        else
        {
            if (boss != null) boss.PlayRoar();
            yield return new WaitForSeconds(1.5f);
        }

        // 5. Kembalikan kontrol Player & mulai pertarungan boss
        if (player != null)
        {
            player.canMove = true;
        }

        if (boss != null)
        {
            boss.ActivateBoss();
        }
    }
}