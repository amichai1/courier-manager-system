using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PL.Order
{
    public partial class OrderListWindow : Window
    {
        static readonly BlApi.IBI s_bl = BL.Factory.Get();

        private static OrderListWindow? _instance = null;
        private BO.OrderStatus? _initialFilter = null;

        private OrderListWindow(BO.OrderStatus? filterStatus = null)
        {
            InitializeComponent();
            _initialFilter = filterStatus;
        }

        public static void ShowList()
        {
            if (_instance == null)
            {
                _instance = new OrderListWindow();
                _instance.Show();
            }

            BringToFront();
        }

        public static void ShowListFiltered(BO.OrderStatus status)
        {
            if (_instance == null)
            {
                _instance = new OrderListWindow(status);
                _instance.Show();
            }
            else
            {
                _instance._initialFilter = status;
                _instance.ApplyFilter(status);
            }

            BringToFront();
        }

        /// <summary>
        /// Forces the window to appear above all other windows including Login
        /// </summary>
        private static void BringToFront()
        {
            if (_instance == null) return;

            if (_instance.WindowState == WindowState.Minimized)
            {
                _instance.WindowState = WindowState.Normal;
            }

            _instance.Activate();
            _instance.Topmost = true;
            _instance.Focus();

            // Delay reset of Topmost to ensure it stays on top
            _instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_instance != null)
                {
                    _instance.Topmost = false;
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        #region Dependency Properties

        public IEnumerable<BO.Order>? OrderList
        {
            get => (IEnumerable<BO.Order>?)GetValue(OrderListProperty);
            set => SetValue(OrderListProperty, value);
        }

        public static readonly DependencyProperty OrderListProperty =
            DependencyProperty.Register("OrderList", typeof(IEnumerable<BO.Order>), typeof(OrderListWindow), new PropertyMetadata(null));

        public BO.Order? SelectedOrder
        {
            get => (BO.Order?)GetValue(SelectedOrderProperty);
            set => SetValue(SelectedOrderProperty, value);
        }

        public static readonly DependencyProperty SelectedOrderProperty =
            DependencyProperty.Register("SelectedOrder", typeof(BO.Order), typeof(OrderListWindow), new PropertyMetadata(null));

        #endregion

        #region Lifecycle & Observers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialFilter.HasValue)
            {
                ApplyFilter(_initialFilter.Value);
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
            if (_instance == null || !_instance.IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_instance != null && _instance.IsLoaded)
                {
                    QueryOrderList();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        #endregion

        #region Query & Actions

        private void ApplyFilter(BO.OrderStatus status)
        {
            for (int i = 0; i < cbxStatusFilter.Items.Count; i++)
            {
                if (cbxStatusFilter.Items[i] is ComboBoxItem item)
                {
                    string content = item.Content?.ToString() ?? "";
                    if (content == status.ToString())
                    {
                        cbxStatusFilter.SelectedIndex = i;
                        break;
                    }
                }
            }

            QueryOrderList();
        }

        private void QueryOrderList()
        {
            try
            {
                var allOrders = s_bl.Orders.ReadAll();

                if (cbxStatusFilter.SelectedItem is ComboBoxItem selectedItem)
                {
                    string filterText = selectedItem.Content?.ToString() ?? "All Statuses";

                    if (filterText != "All Statuses" && Enum.TryParse<BO.OrderStatus>(filterText, out var status))
                    {
                        OrderList = allOrders.Where(o => o.OrderStatus == status).ToList();
                    }
                    else
                    {
                        OrderList = allOrders.ToList();
                    }
                }
                else
                {
                    OrderList = allOrders.ToList();
                }
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

        private void OrderCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BO.Order order)
            {
                new OrderWindow(order.Id).Show();
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

        private void dgOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOrder != null)
            {
                new OrderWindow(SelectedOrder.Id).Show();
            }
        }

        #endregion
    }
}
