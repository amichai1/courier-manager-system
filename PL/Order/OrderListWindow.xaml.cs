using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;

namespace PL.Order
{
    public partial class OrderListWindow : Window
    {
        static readonly BlApi.IBI s_bl = BL.Factory.Get();

        private static OrderListWindow? _instance = null;
        private BO.OrderStatus? _initialFilter = null;

        // Stage 7 - Observer Mutex for thread-safe updates
        private readonly PL.Helpers.ObserverMutex _orderListMutex = new();

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
                BringToFront();
            }
            else
            {
                BringToFront();
            }
        }

        public static void ShowListFiltered(BO.OrderStatus status)
        {
            if (_instance == null)
            {
                _instance = new OrderListWindow(status);
                _instance.Show();
                BringToFront();
            }
            else
            {
                _instance._initialFilter = status;
                _instance.ApplyFilter(status);
                BringToFront();
            }
        }

        /// <summary>
        /// Forces the window to appear above other windows without affecting other windows
        /// Uses Activate() to bring to front at OS level
        /// </summary>
        private static void BringToFront()
        {
            if (_instance == null) return;

            if (_instance.WindowState == WindowState.Minimized)
            {
                _instance.WindowState = WindowState.Normal;
            }

            // Defer activation to ensure window is rendered first
            _instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                _instance?.Activate();
                _instance?.Focus();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        #region Dependency Properties

        public IEnumerable<BO.OrderInList>? OrderList
        {
            get => (IEnumerable<BO.OrderInList>?)GetValue(OrderListProperty);
            set => SetValue(OrderListProperty, value);
        }

        public static readonly DependencyProperty OrderListProperty =
            DependencyProperty.Register("OrderList", typeof(IEnumerable<BO.OrderInList>), typeof(OrderListWindow), new PropertyMetadata(null));

        public BO.OrderInList? SelectedOrder
        {
            get => (BO.OrderInList?)GetValue(SelectedOrderProperty);
            set => SetValue(SelectedOrderProperty, value);
        }

        public static readonly DependencyProperty SelectedOrderProperty =
            DependencyProperty.Register("SelectedOrder", typeof(BO.OrderInList), typeof(OrderListWindow), new PropertyMetadata(null));

        #endregion

        #region Bound Properties for Sorting

        public BO.OrderSortBy SelectedSortBy { get; set; } = BO.OrderSortBy.OrderId;

        public BO.SortOrder SelectedSortOrder { get; set; } = BO.SortOrder.Ascending;

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
            #region Stage 7 - Thread-safe observer with non-blocking mutex
            // 1. בדיקה אם כבר רץ עדכון - אם כן, יוצאים (בדיקה מהירה)
            if (_orderListMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;

            // 2. כל הלוגיקה עוברת ל-Dispatcher כדי למנוע שימוש ב-.Wait()
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    // בדיקה האם החלון עדיין רלוונטי
                    if (_instance != null && _instance.IsLoaded)
                    {
                        QueryOrderList();
                    }
                }
                catch (Exception ex)
                {
                    // תפיסת שגיאות כדי למנוע קריסה, אבל לאפשר שחרור נעילה
                    System.Diagnostics.Debug.WriteLine($"Error in OrderListObserver: {ex.Message}");
                }
                finally
                {
                    // 3. הבלוק הזה ירוץ *תמיד*, גם אם הייתה שגיאה למעלה!
                    // זה מבטיח שהמסך לא ייתקע.
                    bool shouldRestart = await _orderListMutex.UnsetLoadInProgressAndCheckRestartRequested();

                    if (shouldRestart)
                    {
                        // אם הגיעה בקשה חדשה בזמן שעבדנו, נריץ שוב
                        OrderListObserver();
                    }
                }
            }));
        }
            #endregion
        
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

        // Fetches orders off the UI thread to avoid freezing; marshals results back for UI updates
        private async void QueryOrderList()
        {
            try
            {
                var orders = await Task.Run(() => s_bl.Orders.GetOrderList()).ConfigureAwait(false);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        IEnumerable<BO.OrderInList> filtered = orders;

                        // Apply filter
                        if (cbxStatusFilter.SelectedItem is ComboBoxItem selectedItem)
                        {
                            string filterText = selectedItem.Content?.ToString() ?? "All Statuses";

                            if (filterText != "All Statuses" && Enum.TryParse<BO.OrderStatus>(filterText, out var status))
                            {
                                filtered = filtered.Where(o => o.OrderStatus == status);
                            }
                        }

                        // Apply sorting
                        filtered = ApplySorting(filtered);

                        OrderList = filtered.ToList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }), System.Windows.Threading.DispatcherPriority.DataBind);
            }
            catch (Exception ex)
            {
                // If fetching in background failed, show message on UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
        }

        private IEnumerable<BO.OrderInList> ApplySorting(IEnumerable<BO.OrderInList> orders)
        {
            bool ascending = SelectedSortOrder == BO.SortOrder.Ascending;

            IOrderedEnumerable<BO.OrderInList> sortedOrders = SelectedSortBy switch
            {
                BO.OrderSortBy.OrderId => ascending
                    ? orders.OrderBy(o => o.OrderId)
                    : orders.OrderByDescending(o => o.OrderId),

                BO.OrderSortBy.DeliveryId => ascending
                    ? orders.OrderBy(o => o.DeliveryId ?? int.MaxValue)
                    : orders.OrderByDescending(o => o.DeliveryId ?? int.MinValue),

                BO.OrderSortBy.OrderType => ascending
                    ? orders.OrderBy(o => o.OrderType)
                    : orders.OrderByDescending(o => o.OrderType),

                BO.OrderSortBy.Distance => ascending
                    ? orders.OrderBy(o => o.Distance)
                    : orders.OrderByDescending(o => o.Distance),

                BO.OrderSortBy.OrderStatus => ascending
                    ? orders.OrderBy(o => o.OrderStatus)
                    : orders.OrderByDescending(o => o.OrderStatus),

                BO.OrderSortBy.ScheduleStatus => ascending
                    ? orders.OrderBy(o => o.ScheduleStatus)
                    : orders.OrderByDescending(o => o.ScheduleStatus),

                BO.OrderSortBy.CompletionTime => ascending
                    ? orders.OrderBy(o => o.OrderCompletionTime)
                    : orders.OrderByDescending(o => o.OrderCompletionTime),

                BO.OrderSortBy.TotalDeliveries => ascending
                    ? orders.OrderBy(o => o.TotalDeliveries)
                    : orders.OrderByDescending(o => o.TotalDeliveries),

                _ => orders.OrderBy(o => o.OrderId)
            };

            return sortedOrders;
        }

        private void cbxStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                QueryOrderList();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                QueryOrderList();
            }
        }

        private void OrderCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                DependencyObject? current = source;
                while (current != null && current != sender)
                {
                    if (current is Button || current is CheckBox)
                    {
                        return;
                    }
                    current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                }
            }

            if (sender is FrameworkElement element && element.DataContext is BO.OrderInList order)
            {
                var window = new OrderWindow(order.OrderId);
                window.Show();
            }
        }

        private void btnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderWindow();
            window.Show();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            QueryOrderList();
        }

        private void dgOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOrder != null)
            {
                var window = new OrderWindow(SelectedOrder.OrderId);
                window.Show();
            }
        }

        #endregion
    }
}
