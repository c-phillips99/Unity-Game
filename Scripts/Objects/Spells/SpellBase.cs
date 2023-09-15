using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpellBase : MonoBehaviour
{
    [SerializeField] private SpellData _spellData;

    public SpellData SpellData => _spellData;

    public abstract void Activate();
    public abstract void Deactivate();
}
