using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystemWpf.Models
{
    // Inventory stores current stock as a decimal quantity for each Item.
    // For UnitItem this is the count; for BulkItem this is kilograms/meters (etc.).
    public class Inventory
    {
        private readonly Dictionary<Item, decimal> _stock = new();

        public void AddStock(Item item, decimal quantity)
        {
            if (_stock.ContainsKey(item))
                _stock[item] += quantity;
            else
                _stock[item] = quantity;
        }

        public decimal GetQuantity(Item item) => _stock.TryGetValue(item, out var q) ? q : 0m;

        public IReadOnlyDictionary<Item, decimal> Snapshot() => new Dictionary<Item, decimal>(_stock);

        public IEnumerable<(Item item, decimal quantity)> LowStockItems(decimal threshold = 5m) =>
            _stock.Select(kv => (kv.Key, kv.Value)).Where(t => t.Value < threshold);

        public bool TryConsume(Item item, decimal quantity)
        {
            var available = GetQuantity(item);
            if (available < quantity) return false;
            _stock[item] = available - quantity;
            return true;
        }

        public bool TryConsumeOrder(Order order, out string? failure)
        {
            // Check availability first
            foreach (var line in order.OrderLines)
            {
                if (GetQuantity(line.Item) < line.Quantity)
                {
                    failure = $"Insufficient stock for {line.Item.Name}. Needed {line.Quantity}, have {GetQuantity(line.Item)}.";
                    return false;
                }
            }

            // Deduct
            foreach (var line in order.OrderLines)
            {
                TryConsume(line.Item, line.Quantity);
            }

            failure = null;
            return true;
        }
    }
}