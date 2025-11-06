using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystemWpf.Models
{
    public class OrderLine
    {
        public Item Item { get; init; }
        public decimal Quantity { get; init; } // count for UnitItem; kg/m/etc. for BulkItem

        public OrderLine(Item item, decimal quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public decimal LineTotal() => Item.PricePerUnit * Quantity;

        public override string ToString() => $"{Item.Name} x {Quantity} = {LineTotal():C}";
    }

    public class Order
    {
        public DateTime Time { get; init; } = DateTime.Now;
        public List<OrderLine> OrderLines { get; init; } = new();

        public decimal TotalPrice() => OrderLines.Sum(ol => ol.LineTotal());

        public override string ToString() => $"{Time:G} — {TotalPrice():C}";
    }

    public class Customer
    {
        public string Name { get; init; }
        public List<Order> Orders { get; } = new();

        public Customer(string name) => Name = name;

        public Order CreateOrder(IEnumerable<OrderLine> lines)
        {
            var order = new Order { Time = DateTime.Now, OrderLines = lines.ToList() };
            Orders.Add(order);
            return order;
        }

        public override string ToString() => Name;
    }
}