public static class DolocAPI
{
}

namespace DolocTown
{
    public sealed class Item
    {
        public Item(string itemId, int itemCount)
        {
            Id = itemId;
            count = itemCount;
        }

        public string Id { get; }
        public int count;
    }

    public static class ItemFactory
    {
        public static int GenerateCount { get; private set; }
        public static bool FailGeneration { get; set; }

        public static bool GenerateItem(
            string itemId,
            int count,
            out Item item)
        {
            GenerateCount++;
            item = new Item(itemId, count);
            return !FailGeneration;
        }

        public static void Reset()
        {
            GenerateCount = 0;
            FailGeneration = false;
        }
    }

    public sealed class EquipmentRenderer
    {
        public void OnReuse()
        {
        }
    }

    public sealed class EquipmentBuilder
    {
        public void CreateIndicator()
        {
        }

        public void TurnIndicator()
        {
        }
    }
}
