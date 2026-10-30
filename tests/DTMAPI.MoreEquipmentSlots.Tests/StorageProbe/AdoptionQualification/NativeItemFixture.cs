// Handwritten reflection doubles, not game implementations. No Unity/game/Core dependency.
// Deliberately no MES identity field: the production adapter never writes one.
namespace MesAdoptionFixture { internal sealed class Api { } }

namespace DolocTown
{
    internal abstract class Item
    {
        public string name = "";
        public int count = 1;
    }
    internal sealed class ItemPassive : Item { }
    internal sealed class ItemHatShield : Item
    {
        public int shieldValue;
        public void SetShieldValue(int value) => shieldValue = value;
    }
    internal sealed class ItemHerbPackage : Item
    {
        public bool isFilled;
        public void SetFilled(bool value) => isFilled = value;
    }
    internal sealed class ItemCooldown : Item { public int cdDuration = 0; }
    internal static class ItemFactory
    {
        public static bool GenerateItem(string id, int count, out Item item)
        {
            item = id switch
            {
                "box_hat" => new ItemHatShield(),
                "herbal_pouch" => new ItemHerbPackage(),
                "vulture_badge" => new ItemCooldown(),
                _ => new ItemPassive()
            };
            item.name = id;
            item.count = count;
            return true;
        }
    }
}
