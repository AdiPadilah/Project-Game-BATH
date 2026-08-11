using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnimationsEvent : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    public void DamageTargets() => entity.DamageTargets();
    private void DisableMovementAndJump() => entity.EnableMovement(false);
    private void EnableMovementAndJump() => entity.EnableMovement(true);
}
