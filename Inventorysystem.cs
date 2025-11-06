// InventorySystem.cs
// Single-file, code-only WPF app for the "Inventory system" assignment.
// - Object-oriented model: Item/BulkItem/UnitItem, Inventory, OrderLine, Order, Customer, OrderBook
// - Two DataGrid controls: QueuedOrders and ProcessedOrders
// - "Process Next" button moves next queued order to processed, deducts stock, and updates Total Revenue
// - Low-stock preview (threshold 5)
// Screencast tip: record <10s showing Process Next → revenue increases & orders move.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace InventorySystemAssignment
{
    // ===== Domain model =====
    public abstract class Item
    {
        public string Name { get; init; }
        public decimal PricePerUnit { get; init; }
        protected Item(string name, decimal pricePerUnit) { Name = name; PricePerUnit = pricePerUnit; }
        public override string ToString() => $"{Name} ({PricePerUnit:C}/unit)";
    }
    public sealed class BulkItem : Item
    {
        public string MeasurementUnit { get; init; } // e.g. "kg"
        public BulkItem(string name, decimal pricePerUnit, string unit) : base(name, pricePerUnit) { MeasurementUnit = unit; }
        public override string ToString() => $"{Name} ({PricePerUnit:C}/{MeasurementUnit})";
    }
    public sealed class UnitItem : Item
    {
        public decimal Weight { get; init; } // kg per item
        public UnitItem(string name, decimal pricePerUnit, decimal weight) : base(name, pricePerUnit) { Weight = weight; }
        public override string ToString() => $"{Name} ({PricePerUnit:C}/item, {Weight} kg each)";
    }

    public class OrderLine
    {
        public Item Item { get; init; }
        public decimal Quantity { get; init; } // count for UnitItem; kg/etc. for BulkItem
        public OrderLine(Item item, decimal quantity) { Item = item; Quantity = quantity; }
        public decimal LineTotal => Item.PricePerUnit * Quantity;
        public override string ToString() => $"{Item.Name} x {Quantity} = {LineTotal:C}";
    }

    public class Order
    {
        public DateTime Time { get; init; } = DateTime.Now;
        public List<OrderLine> OrderLines { get; init; } = new();
        public decimal TotalPrice => OrderLines.Sum(ol => ol.LineTotal);
        public override string ToString() => $"{Time:G} — {TotalPrice:C}";
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

    public class Inventory
    {
        private readonly Dictionary<Item, decimal> _stock = new();
        public void AddStock(Item item, decimal quantity)
        {
            if (_stock.ContainsKey(item)) _stock[item] += quantity; else _stock[item] = quantity;
        }
        public decimal GetQuantity(Item item) => _stock.TryGetValue(item, out var q) ? q : 0m;
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
            foreach (var line in order.OrderLines)
            {
                if (GetQuantity(line.Item) < line.Quantity)
                {
                    failure = $"Insufficient stock for {line.Item.Name}. Needed {line.Quantity}, have {GetQuantity(line.Item)}.";
                    return false;
                }
            }
            foreach (var line in order.OrderLines) TryConsume(line.Item, line.Quantity);
            failure = null;
            return true;
        }
    }

    public class OrderBook : INotifyPropertyChanged
    {
        public ObservableCollection<Order> QueuedOrders { get; } = new();
        public ObservableCollection<Order> ProcessedOrders { get; } = new();
        private readonly Inventory _inventory;
        public event PropertyChangedEventHandler? PropertyChanged;

        public OrderBook(Inventory inventory) { _inventory = inventory; }
        public void QueueOrder(Order order) => QueuedOrders.Add(order);

        public bool ProcessNextOrder(out string? message)
        {
            if (QueuedOrders.Count == 0) { message = "No queued orders."; return false; }
            var next = QueuedOrders[0];
            if (!_inventory.TryConsumeOrder(next, out var failure)) { message = failure; return false; }
            QueuedOrders.RemoveAt(0);
            ProcessedOrders.Add(next);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRevenue)));
            message = $"Processed order for {next.TotalPrice:C}";
            return true;
        }

        public decimal TotalRevenue => ProcessedOrders.Sum(o => o.TotalPrice);
    }

    // ===== UI helpers =====
    public class OrderTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Order o ? o.TotalPrice.ToString("C") : "";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class LowStockInfo
    {
        public Item Item { get; init; }
        public decimal Quantity { get; init; }
        public LowStockInfo(Item item, decimal quantity) { Item = item; Quantity = quantity; }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public Inventory Inventory { get; }
        public OrderBook OrderBook { get; }
        public ObservableCollection<LowStockInfo> LowStockPreview { get; } = new();

        public MainViewModel()
        {
            Inventory = new Inventory();
            Seed(out var customers, out var items);

            // Initial stock
            Inventory.AddStock(items.pen, 12);
            Inventory.AddStock(items.paper, 50);
            Inventory.AddStock(items.cable, 30);
            Inventory.AddStock(items.gravel, 20); // kg

            // Sample orders
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
                LowStockPreview.Add(new LowStockInfo(t.item, t.quantity));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LowStockPreview)));
        }

        private void Seed(out (Customer alex, Customer bella, Customer chen) customers,
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

    // ===== Code-only WPF Window =====
    public class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly ResourceDictionary _resources = new ResourceDictionary();

        public MainWindow()
        {
            Title = "Inventory System — Queued vs. Processed Orders";
            Width = 960; Height = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _vm = new MainViewModel();
            DataContext = new { OrderBook = _vm.OrderBook, LowStockPreview = _vm.LowStockPreview };
            _resources.Add("OrderTotalConverter", new OrderTotalConverter());
            Resources = _resources;

            // Root grid
            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "Inventory System — Queued vs. Processed Orders",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            // Middle: two DataGrids and a button
            var mid = new Grid();
            mid.ColumnDefinitions.Add(new ColumnDefinition());
            mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            mid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetRow(mid, 1);
            root.Children.Add(mid);

            // Queued Orders GroupBox + DataGrid
            var queuedGroup = new GroupBox { Header = "Queued Orders", Margin = new Thickness(0) };
            var queuedGrid = BuildOrdersGrid(bindingPath: "OrderBook.QueuedOrders");
            queuedGroup.Content = queuedGrid;
            Grid.SetColumn(queuedGroup, 0);
            mid.Children.Add(queuedGroup);

            // Process Next button
            var centerPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var processBtn = new Button { Content = "Process Next →", Padding = new Thickness(12, 8, 12, 8) };
            processBtn.Click += OnProcessNext;
            centerPanel.Children.Add(processBtn);
            Grid.SetColumn(centerPanel, 1);
            mid.Children.Add(centerPanel);

            // Processed Orders GroupBox + DataGrid
            var processedGroup = new GroupBox { Header = "Processed Orders" };
            var processedGrid = BuildOrdersGrid(bindingPath: "OrderBook.ProcessedOrders");
            processedGroup.Content = processedGrid;
            Grid.SetColumn(processedGroup, 2);
            mid.Children.Add(processedGroup);

            // Bottom: revenue + low stock
            var bottom = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var revLabel = new TextBlock { Text = "Total Revenue:", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0) };
            bottom.Children.Add(revLabel);
            var revValue = new TextBlock { FontSize = 16 };
            var revBinding = new Binding("OrderBook.TotalRevenue") { StringFormat = "C" };
            revValue.SetBinding(TextBlock.TextProperty, revBinding);
            bottom.Children.Add(revValue);

            var spacer = new Border { Width = 24, Margin = new Thickness(16, 0, 16, 0) };
            bottom.Children.Add(spacer);

            var lowLabel = new TextBlock { Text = "Low stock (below 5):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0) };
            bottom.Children.Add(lowLabel);

            var itemsControl = new ItemsControl();
            itemsControl.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)) { SetValue(StackPanel.OrientationProperty, Orientation.Horizontal) });
            itemsControl.ItemTemplate = BuildLowStockTemplate();
            itemsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("LowStockPreview"));
            bottom.Children.Add(itemsControl);

            Grid.SetRow(bottom, 2);
            root.Children.Add(bottom);

            Content = root;
        }

        private static DataTemplate BuildLowStockTemplate()
        {
            // Template: [ItemName: Quantity] in a rounded border
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(4, 0, 0, 0));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.LightGray);

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            var run1 = new FrameworkElementFactory(typeof(Run));
            run1.SetBinding(Run.TextProperty, new Binding("Item.Name"));
            var run2 = new FrameworkElementFactory(typeof(Run));
            run2.SetValue(Run.TextProperty, ": ");
            var run3 = new FrameworkElementFactory(typeof(Run));
            run3.SetBinding(Run.TextProperty, new Binding("Quantity"));

            textFactory.AppendChild(run1);
            textFactory.AppendChild(run2);
            textFactory.AppendChild(run3);

            borderFactory.AppendChild(textFactory);

            var template = new DataTemplate(typeof(LowStockInfo)) { VisualTree = borderFactory };
            return template;
        }

        private DataGrid BuildOrdersGrid(string bindingPath)
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Margin = new Thickness(6),
                CanUserAddRows = false
            };

            // Time
            var c1 = new DataGridTextColumn { Header = "Time", Binding = new Binding("Time") };
            // Lines count
            var c2 = new DataGridTextColumn { Header = "Lines", Binding = new Binding("OrderLines.Count") };
            // Total (uses converter on the Order object)
            var c3 = new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding(".") { Converter = (IValueConverter)Resources["OrderTotalConverter"] }
            };

            dg.Columns.Add(c1);
            dg.Columns.Add(c2);
            dg.Columns.Add(c3);

            dg.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(bindingPath));
            return dg;
        }

        private void OnProcessNext(object sender, RoutedEventArgs e)
        {
            if (((MainViewModel)((dynamic)DataContext).GetType().GetProperty("OrderBook") == null)) return;
            if (_vm.OrderBook.ProcessNextOrder(out var message))
                MessageBox.Show(message, "Order Processed", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(message ?? "Unable to process order.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public class AppEntry : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new AppEntry();
            app.Startup += (_, __) =>
            {
                var win = new MainWindow();
                app.MainWindow = win;
                win.Show();
            };
            app.Run();
        }
    }
}
