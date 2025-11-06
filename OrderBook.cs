using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace InventorySystemWpf.Models
{
    public class OrderBook : INotifyPropertyChanged
    {
        public ObservableCollection<Order> QueuedOrders { get; } = new();
        public ObservableCollection<Order> ProcessedOrders { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Inventory _inventory;

        public OrderBook(Inventory inventory)
        {
            _inventory = inventory;
        }

        public void QueueOrder(Order order) => QueuedOrders.Add(order);

        public bool ProcessNextOrder(out string? message)
        {
            if (QueuedOrders.Count == 0)
            {
                message = "No queued orders.";
                return false;
            }

            var next = QueuedOrders[0];

            if (!_inventory.TryConsumeOrder(next, out var failure))
            {
                message = failure;
                return false;
            }

            QueuedOrders.RemoveAt(0);
            ProcessedOrders.Add(next);
            OnPropertyChanged(nameof(TotalRevenue));
            message = $"Processed order for {next.TotalPrice():C}";
            return true;
        }

        public decimal TotalRevenue => ProcessedOrders.Sum(o => o.TotalPrice());

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}