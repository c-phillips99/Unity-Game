using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogWarning("Attempted to create second instance of Combat Manager");
    }

    public void DealAttack(int damage, Vector2 knockback, Collider2D attackReceiver, GameObject attackDealer)
    {
        CombatBase attackReceiverCombat = attackReceiver.gameObject.GetComponent<CombatBase>();
        attackReceiverCombat.TakeAttack(damage, knockback, attackDealer);
    }

    public void DealDamage(int damage, Collider2D damageReceiver, GameObject damageDealer)
    {
        CombatBase damageReceiverCombat = damageReceiver.gameObject.GetComponent<CombatBase>();
        damageReceiverCombat.TakeDamage(damage, damageDealer);
    }

    public void DealKnockback(Vector2 knockback, Collider2D knockbackReceiver, GameObject knockbackDealer)
    {
        CombatBase knockbackReceiverCombat = knockbackReceiver.gameObject.GetComponent<CombatBase>();
        knockbackReceiverCombat.TakeKnockback(knockback, knockbackDealer);
    }
}
