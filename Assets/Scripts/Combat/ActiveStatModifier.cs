using Roguelike.Data;

namespace Roguelike.Combat
{
    /// <summary>Runtime-only: a StatModifier plus the object that granted it, so it can be revoked later.</summary>
    public class ActiveStatModifier
    {
        public StatModifier data;
        public object source;
    }
}
