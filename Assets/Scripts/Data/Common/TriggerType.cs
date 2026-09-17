namespace Roguelike.Data
{
    /// <summary>Battle-wide events that traits (and, via status effects, buffs/debuffs) can react to.</summary>
    public enum TriggerType
    {
        OnBattleStart,
        OnBattleEnd,
        OnTurnStart,
        OnTurnEnd,
        OnCardPlayed,
        OnDamageTaken,
        OnDamageDealt,
        OnBlockGained,
        OnKill,
        OnDeath,
        OnHeal
    }
}
