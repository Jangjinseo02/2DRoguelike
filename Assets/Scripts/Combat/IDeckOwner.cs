namespace Roguelike.Combat
{
    /// <summary>Combatants that play from a deck (currently just the player character).</summary>
    public interface IDeckOwner
    {
        int Energy { get; }
        void DrawCards(int count);
        void GainEnergy(int amount);
    }
}
