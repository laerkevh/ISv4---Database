using System;

namespace InventorySystemWpf.Models
{
    public abstract class Item
    {
        public string Name { get; init; }
        public decimal PricePerUnit { get; init; }

        protected Item(string name, decimal pricePerUnit)
        {
            Name = name;
            PricePerUnit = pricePerUnit;
        }

        public override string ToString() => $"{Name} ({PricePerUnit:C}/unit)";
    }

    // BulkItem: sold by continuous measurement (e.g., kilograms, meters).
    public sealed class BulkItem : Item
    {
        public string MeasurementUnit { get; init; } // e.g., "kg", "m"

        public BulkItem(string name, decimal pricePerUnit, string measurementUnit)
            : base(name, pricePerUnit)
        {
            MeasurementUnit = measurementUnit;
        }

        public override string ToString() => $"{Name} ({PricePerUnit:C}/{MeasurementUnit})";
    }

    // UnitItem: sold in discrete counts; includes per-item weight.
    public sealed class UnitItem : Item
    {
        public decimal Weight { get; init; } // weight per item, e.g., in kg

        public UnitItem(string name, decimal pricePerUnit, decimal weight)
            : base(name, pricePerUnit)
        {
            Weight = weight;
        }

        public override string ToString() => $"{Name} ({PricePerUnit:C}/item, {Weight} kg each)";
    }
}