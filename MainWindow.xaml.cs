using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using InventorySystemWpf.Models;

namespace InventorySystemWpf
{
    public class OrderTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Order o) return o.TotalPrice().ToString("C");
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Inventory Inventory { get; }
        public OrderBook OrderBook { get; }

        public ObservableCollection<(Item Item, decimal Quantity)> LowStockPreview { get; } = new();

        public MainViewModel()
        {
            Inventory = new Inventory();
            SeedData(out var customers, out var items);

            // Add some initial stock
            Inventory.AddStock(items.pen, 12);
            Inventory.AddStock(items.paper, 50);
            Inventory.AddStock(items.cable, 30);
            Inventory.AddStock(items.gravel, 20); // kg

            // Queue some sample orders
            OrderBook = new OrderBook(Inventory);
            OrderBook.QueueOrder(customers.alex.CreateOrder(new[] {
                new OrderLine(items.pen, 3),
                new OrderLine(items.paper, 10)
            }));

            OrderBook.QueueOrder(customers.bella.CreateOrder(new[] {
                new OrderLine(items.cable, 5),
                new OrderLine(items.gravel, 8) // kg
            }));

            OrderBook.QueueOrder(customers.chen.CreateOrder(new[] {
                new OrderLine(items.pen, 6),
                new OrderLine(items.gravel, 12) // kg
            }));

            RefreshLowStock();
            OrderBook.ProcessedOrders.CollectionChanged += (_, __) => RefreshLowStock();
            OrderBook.QueuedOrders.CollectionChanged += (_, __) => RefreshLowStock();
        }

        private void RefreshLowStock()
        {
            LowStockPreview.Clear();
            foreach (var t in Inventory.LowStockItems(5m))
                LowStockPreview.Add((t.item, t.quantity));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LowStockPreview)));
        }

        private void SeedData(out (Customer alex, Customer bella, Customer chen) customers,
                              out (UnitItem pen, UnitItem cable, BulkItem gravel, UnitItem paper) items)
        {
            customers = (new Customer("Alex Smith"), new Customer("Bella Cruz"), new Customer("Chen Li"));

            var pen = new UnitItem("Blue Pen", 1.50m, 0.02m);
            var cable = new UnitItem("USB-C Cable", 9.99m, 0.08m);
            var paper = new UnitItem("A4 Paper (100)", 5.49m, 0.6m);
            var gravel = new BulkItem("Construction Gravel", 20.00m, "kg");

            items = (pen, cable, gravel, paper);
        }
    }

    public partial class MainWindow : Window
    {
        public MainViewModel VM { get; }

        public MainWindow()
        {
            InitializeComponent();
            Resources.Add("OrderTotalConverter", new OrderTotalConverter());
            VM = new MainViewModel();
            DataContext = new { OrderBook = VM.OrderBook, LowStockPreview = VM.LowStockPreview };
        }

        private void OnProcessNext(object sender, RoutedEventArgs e)
        {
            if (VM.OrderBook.ProcessNextOrder(out var message))
                MessageBox.Show(message, "Order Processed", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(message ?? "Unable to process order.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}