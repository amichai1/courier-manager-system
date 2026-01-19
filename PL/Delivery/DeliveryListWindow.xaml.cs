using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlApi;
using BO;
using System.Threading.Tasks;

namespace PL.Delivery
{
    public partial class DeliveryListWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();

        // --- Singleton Implementation Start ---
        private static DeliveryListWindow? _instance = null;

        // ✅ הוסף את ObserverMutex - משמר מ-thread safety
        private readonly PL.Helpers.ObserverMutex _deliveryListMutex = new();

        // Add this field to declare lblStatus
        private System.Windows.Controls.Label? lblStatus;

        private DeliveryListWindow()
        {
            InitializeComponent();

            // Initialize lblStatus by finding it in the XAML (assuming it exists)
            lblStatus = (System.Windows.Controls.Label?)FindName("lblStatus");
        }

        public static void ShowList()
        {
            if (_instance == null)
            {
                _instance = new DeliveryListWindow();
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

        public IEnumerable<BO.Delivery>? DeliveryList
        {
            get => (IEnumerable<BO.Delivery>?)GetValue(DeliveryListProperty);
            set => SetValue(DeliveryListProperty, value);
        }

        public static readonly DependencyProperty DeliveryListProperty =
            DependencyProperty.Register("DeliveryList", typeof(IEnumerable<BO.Delivery>), typeof(DeliveryListWindow), new PropertyMetadata(null));

        public BO.Delivery? SelectedDelivery { get; set; }

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QueryDeliveryList();
            try { s_bl.Deliveries.AddObserver(DeliveryListObserver); } catch { /* ignore */ }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Deliveries.RemoveObserver(DeliveryListObserver); } catch { /* ignore */ }
            _instance = null;
        }

        // ✅ שנה את DeliveryListObserver להשתמש ב-ObserverMutex (בדיוק כמו CourierListWindow)
        private void DeliveryListObserver()
        {
            #region Stage 7 - Thread-safe observer with non-blocking mutex
            if (_deliveryListMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;

            if (_instance == null || !_instance.IsLoaded)
            {
                _deliveryListMutex.UnsetLoadInProgressAndCheckRestartRequested().Wait();
                return;
            }

            Dispatcher.BeginInvoke(async () =>
            {
                if (_instance != null && _instance.IsLoaded)
                {
                    QueryDeliveryList();
                }

                if (await _deliveryListMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    DeliveryListObserver();
            });
            #endregion
        }

        #endregion

        #region Query & Actions

        private async void QueryDeliveryList()
        {
            try
            {
                var allDeliveries = await Task.Run(() => s_bl.Deliveries.ReadAll()
                    .Where(d => !d.Status.Equals(BO.OrderStatus.Delivered))
                    .ToList()).ConfigureAwait(false);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeliveryList = allDeliveries;

                    if (lblStatus != null)
                        lblStatus.Content = $"Total Active Deliveries: {DeliveryList?.Count() ?? 0}";
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error loading deliveries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
        }

        private void dgDeliveries_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedDelivery != null)
            {
                // Open the Order window with the associated order ID
                new PL.Order.OrderWindow(SelectedDelivery.OrderId).Show();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            QueryDeliveryList();
        }

        #endregion
    }
}
