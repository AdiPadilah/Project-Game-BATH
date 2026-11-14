using UnityEngine;

public class ObjectToProtect : Entity
{
    private Transform player;

    protected override void Awake()
    {
        base.Awake();

        Player foundPlayer = FindFirstObjectByType<Player>();
        if (foundPlayer != null)
            player = foundPlayer.transform;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void HandleAnimations()
    {
        // Object to protect tidak memiliki parameter animator
    }

    protected override void HandleFlip()
    {
        if (player == null)
            return;

        if (player.position.x > transform.position.x && !facingRight)
            Flip();
        else if (player.position.x < transform.position.x && facingRight)
            Flip();
    }

    protected override void Die()
    {
        base.Die();

        // ?? GAME OVER kalau object ini hancur
        if (UI.instance != null)
            UI.instance.ShowGameOver();
    }
}