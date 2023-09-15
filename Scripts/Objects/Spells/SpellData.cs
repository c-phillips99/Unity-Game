using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellData", menuName = "Data/SpellData")]
public class SpellData : ScriptableObject
{
    [SerializeField] private string _spellName;
    [SerializeField] private string _spellDescription;
    [SerializeField] private int _spellPageCost;

    public string SpellName => _spellName;
    public string SpellDescription => _spellDescription;
    public int SpellPageCost => _spellPageCost;

}
