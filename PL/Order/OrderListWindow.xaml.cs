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

        private static OrderListWindow? _instance = null;
        private static OrderStatus? _pendingFilter = null;

        private OrderListWindow()
        {
            InitializeComponent();
        }

        public static void ShowList()
        {
            _pendingFilter = null;
            ShowListInternal();
        }

        public static void ShowListFiltered(OrderStatus status)
        {
            _pendingFilter = status;
            ShowListInternal();
        }

        private static void ShowListInternal()
        {
            if (_instance == null)
            {
                _instance = new OrderListWindow();
                _instance.Show();
            }
            else
            {
                if (_instance.WindowState == WindowState.Minimized)
                {
                    _instance.WindowState = WindowState.Normal;
                }

                _instance.Activate();

                if (_pendingFilter.HasValue)
                {
                    _instance.ApplyStatusFilter(_pendingFilter.Value);
                }
            }
        }

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
            if (_pendingFilter.HasValue)
            {
                ApplyStatusFilter(_pendingFilter.Value);
                _pendingFilter = null;
            }
            else
            {
                QueryOrderList();
            }

            try { s_bl.Orders.AddObserver(OrderListObserver); } catch { }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Orders.RemoveObserver(OrderListObserver); } catch { }
            _instance = null;
        }

        private void OrderListObserver()
        {
            // Check if window is still valid before invoking
            if (_instance == null || !_instance.IsLoaded)
            {
                return;
            }

            // Use BeginInvoke with Background priority to avoid bringing window to front
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Double-check instance is still valid
                if (_instance != null && _instance.IsLoaded)
                {
                    QueryOrderList();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        #endregion

        #region Query & Actions

        private void ApplyStatusFilter(OrderStatus status)
        {
            foreach (ComboBoxItem item in cbxStatusFilter.Items)
            {
                if (item.Content.ToString() == status.ToString())
                {
                    cbxStatusFilter.SelectedItem = item;
                    break;
                }
            }

            QueryOrderList();
        }

        private void QueryOrderList()
        {
            try
            {
                var allOrders = from order in s_bl.Orders.ReadAll()
                                select order;

                if (cbxStatusFilter?.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedStatus = selectedItem.Content.ToString() ?? "All Statuses";

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
