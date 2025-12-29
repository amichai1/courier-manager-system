using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BlApi;
using BO;

namespace PL.Order
{
    public partial class OrderListWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();

        // --- Singleton Implementation Start ---
        private static OrderListWindow? _instance = null;

        private OrderListWindow()
        {
            InitializeComponent();
        }

        public static void ShowList()
        {
            if (_instance == null)
            {
                _instance = new OrderListWindow();
                _instance.Show();
            }
            else
            {
                if (_instance.WindowState == WindowState.Minimized)
                    _instance.WindowState = WindowState.Normal;
                _instance.Activate();
            }
        }
        // --- Singleton Implementation End ---

        #region Dependency Properties

        public IEnumerable<BO.Order>? OrderList
        {
            get => (IEnumerable<BO.Order>?)GetValue(OrderListProperty);
            set => SetValue(OrderListProperty, value);
        }

        public static readonly DependencyProperty OrderListProperty =
            DependencyProperty.Register("OrderList", typeof(IEnumerable<BO.Order>), typeof(OrderListWindow), new PropertyMetadata(null));

        public BO.Order? SelectedOrder { get; set; }

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QueryOrderList();
            try { s_bl.Orders.AddObserver(OrderListObserver); } catch { /* ignore */ }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Orders.RemoveObserver(OrderListObserver); } catch { /* ignore */ }
            _instance = null;
        }

        private void OrderListObserver() => Dispatcher.Invoke(QueryOrderList);

        #endregion

        #region Query & Actions

        private void QueryOrderList()
        {
            try
            {
                // LINQ Query Syntax - demonstrates: from, select
                var allOrders = from order in s_bl.Orders.ReadAll()
                               select order;

                if (cbxStatusFilter?.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedStatus = selectedItem.Content.ToString() ?? "All Statuses";

                    // LINQ Method Syntax - demonstrates: Where, ToList with lambda
                    OrderList = selectedStatus != "All Statuses" && Enum.TryParse<OrderStatus>(selectedStatus, out var statusEnum)
                        ? allOrders.Where(o => o.OrderStatus == statusEnum).ToList()
                        : allOrders.ToList();
                }
                else
                {
                    OrderList = allOrders.ToList();
                }

                lblStatus.Text = $"Total Orders: {OrderList?.Count() ?? 0}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbxStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                QueryOrderList();
            }
        }

        private void dgOrders_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedOrder != null)
            {
                new OrderWindow(SelectedOrder.Id).Show();
            }
        }

        private void btnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            new OrderWindow().Show();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            QueryOrderList();
        }

        #endregion
    }
}
