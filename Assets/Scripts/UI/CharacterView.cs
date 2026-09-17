using UnityEngine;
using TMPro;
using Roguelike.Combat;

/// <summary>The player character on screen: everything CombatantView shows, plus energy.</summary>
public class CharacterView : CombatantView
{
    [Header("Character")]
    [SerializeField] private TextMeshProUGUI energyText;

    private CharacterInstance character;

    public void Bind(CharacterInstance model)
    {
        character = model;
        base.Bind(model);
    }

    protected override void Refresh(bool animate)
    {
        base.Refresh(animate);
        if (energyText != null && character != null)
            energyText.text = $"{character.Energy} / {character.MaxEnergyPerTurn}";
    }
}
