using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public SpellManager Instance { get; private set; }
    private List<SpellBase> _activeSpells = new List<SpellBase>();
    private int _maxSpellPages = 3;
    private int _currentSpellPagesUsed = 0;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogWarning("Attempted to create second instance of Spell Manager");
    }

    public void AddSpell(SpellBase spell)
    {
        if (_currentSpellPagesUsed >= _maxSpellPages) return;

        if (_maxSpellPages >= _currentSpellPagesUsed + spell.SpellData.SpellPageCost)
        {
            _activeSpells.Add(spell);
            _currentSpellPagesUsed += spell.SpellData.SpellPageCost;
            spell.Activate();
        }
    }
    public void RemoveSpell(SpellBase spell)
    {
        _activeSpells.Remove(spell);
        _currentSpellPagesUsed -= spell.SpellData.SpellPageCost;
        spell.Deactivate();
    }
    public List<SpellBase> GetActiveSpells()
    {
        return _activeSpells;
    }
}
