using System;

namespace Game3.Hunting
{
    [Serializable]
    public sealed class InventoryModel
    {
        public int Capacity { get; }
        public int Meat { get; private set; }
        public int Hide { get; private set; }
        public int Count => Meat + Hide;
        public int Remaining => Math.Max(0, Capacity - Count);

        public InventoryModel(int capacity)
        {
            Capacity = Math.Max(1, capacity);
        }

        public int Add(int meat, int hide)
        {
            meat = Math.Max(0, meat);
            hide = Math.Max(0, hide);
            var requested = meat + hide;
            var accepted = Math.Min(requested, Remaining);
            var acceptedMeat = Math.Min(meat, accepted);
            var acceptedHide = Math.Min(hide, accepted - acceptedMeat);
            Meat += acceptedMeat;
            Hide += acceptedHide;
            return acceptedMeat + acceptedHide;
        }

        public int SellAll(int meatPrice, int hidePrice)
        {
            var value = Meat * Math.Max(0, meatPrice) + Hide * Math.Max(0, hidePrice);
            Clear();
            return value;
        }

        public void Clear()
        {
            Meat = 0;
            Hide = 0;
        }
    }
}
