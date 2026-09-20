using Roguelike.Data.Traits;

namespace Roguelike.Combat
{
    public struct ActiveTrait
    {
        // 특성 정보를 누가 줬냐.
        public TraitDefinition definition;
        public object source; 
    }
}

