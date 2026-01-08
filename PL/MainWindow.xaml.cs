using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BlApi;
using PL.Converters;

namespace PL
{
    public partial class MainWindow : Window
    {
        static readonly IBI s_bl = BL.Factory.Get();
        private double _originalMaxDistance;

        private static MainWindow? s_instance = null;
        public static bool IsAdminWindowOpen => s_instance != null;

        // Flag to prevent window activation during observer updates
        private bool _suppressActivation = false;

        public MainWindow()
        {
            InitializeComponent();
            s_instance = this;
        }

        #region Dependency Properties

        public DateTime CurrentTime
        {
            get { return (DateTime)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(DateTime), typeof(MainWindow));

        public BO.Config Configuration
        {
            get { return (BO.Config)GetValue(ConfigurationProperty); }
            set { SetValue(ConfigurationProperty, value); }
        }

        public static readonly DependencyProperty ConfigurationProperty =
            DependencyProperty.Register("Configuration", typeof(BO.Config), typeof(MainWindow));

        public BO.OrderStatusSummary OrderSummary
        {
            get { return (BO.OrderStatusSummary)GetValue(OrderSummaryProperty); }
            set { SetValue(OrderSummaryProperty, value); }
        }

        public static readonly DependencyProperty OrderSummaryProperty =
            DependencyProperty.Register("OrderSummary", typeof(BO.OrderStatusSummary), typeof(MainWindow));

        #endregion

        #region Clock Buttons

        private void btnAddOneMinute_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Minute);
        }

        private void btnAddOneHour_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Hour);
        }

        private void btnAddOneDay_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Day);
        }

        private void btnAddOneMonth_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Month);
        }

        private void btnAddOneYear_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Year);
        }

        #endregion

        #region Configuration Buttons

        private void btnUpdateConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!MaxDistanceConverter.IsValid(Configuration.MaxDeliveryDistance))
                {
                    MessageBox.Show(
                        MaxDistanceConverter.GetErrorMessage() + "\nReverting to original value.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Configuration.MaxDeliveryDistance = _originalMaxDistance;

                    var config = Configuration;
                    Configuration = null!;
                    Configuration = config;
                    return;
                }

                s_bl.Admin.SetConfig(Configuration);
                _originalMaxDistance = Configuration.MaxDeliveryDistance ?? MaxDistanceConverter.DefaultDistance;
                MessageBox.Show("Configuration updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Configuration.MaxDeliveryDistance = _originalMaxDistance;
                var config = Configuration;
                Configuration = null!;
                Configuration = config;
            }
        }

        #endregion

        #region Order Summary Click Handlers

        private void OpenOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.Open);
        }

        private void InProgressOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.InProgress);
        }

        private void DeliveredOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.Delivered);
        }

        private void RefusedOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.OrderRefused);
        }

        private void CanceledOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.Canceled);
        }

        #endregion

        #region List Window Buttons

        private void btnDeliveries_Click(object sender, RoutedEventArgs e)
        {
            PL.Delivery.DeliveryListWindow.ShowList();
        }

        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            PL.Order.OrderListWindow.ShowList();
        }

        private void btnCouriers_Click(object sender, RoutedEventArgs e)
        {
            PL.Courier.CourierListWindow.ShowList();
        }

        #endregion

        #region Database Buttons

        private void btnInitializeDB_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to initialize the database? This will create initial data.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    CloseAllWindowsExceptMainAndLogin();
                    
                    // Suppress activation during initialization
                    _suppressActivation = true;
                    s_bl.Admin.InitializeDB();
                    RefreshAllData();
                    _suppressActivation = false;
                    
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Database initialized successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _suppressActivation = false;
                    Mouse.OverrideCursor = null;
                    MessageBox.Show($"Error initializing database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnResetDB_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reset the database? This will delete all data.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    CloseAllWindowsExceptMainAndLogin();
                    
                    // Suppress activation during reset
                    _suppressActivation = true;
                    s_bl.Admin.ResetDB();
                    RefreshAllData();
                    _suppressActivation = false;
                    
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Database reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _suppressActivation = false;
                    Mouse.OverrideCursor = null;
                    MessageBox.Show($"Error resetting database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshAllData()
        {
            CurrentTime = s_bl.Admin.GetClock();
            Configuration = s_bl.Admin.GetConfig();
            OrderSummary = s_bl.Orders.GetOrderStatusSummary();
            _originalMaxDistance = Configuration.MaxDeliveryDistance ?? MaxDistanceConverter.DefaultDistance;
        }

        private void CloseAllWindowsExceptMainAndLogin()
        {
            for (int i = Application.Current.Windows.Count - 1; i >= 0; i--)
            {
                var window = Application.Current.Windows[i];
                if (window != this && !(window is LoginWindow))
                {
                    window.Close();
                }
            }
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentTime = s_bl.Admin.GetClock();
            Configuration = s_bl.Admin.GetConfig();
            OrderSummary = s_bl.Orders.GetOrderStatusSummary();
            _originalMaxDistance = Configuration.MaxDeliveryDistance ?? MaxDistanceConverter.DefaultDistance;
            s_bl.Admin.AddClockObserver(clockObserver);
            s_bl.Admin.AddConfigObserver(configObserver);
            s_bl.Orders.AddObserver(orderObserver);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Stop simulator if running
            if (s_bl.Admin.IsSimulatorRunning)
            {
                s_bl.Admin.StopSimulator();
            }

            s_instance = null;
            s_bl.Admin.RemoveClockObserver(clockObserver);
            s_bl.Admin.RemoveConfigObserver(configObserver);
            s_bl.Orders.RemoveObserver(orderObserver);
            Application.Current.Shutdown();
        }

        // Override to prevent activation when suppressed
        protected override void OnActivated(EventArgs e)
        {
            if (_suppressActivation)
            {
                return; // Don't call base - prevents window from coming to front
            }
            base.OnActivated(e);
        }

        #endregion

        #region Observers

        private void clockObserver()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && !_suppressActivation)
                {
                    CurrentTime = s_bl.Admin.GetClock();
                }
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void configObserver()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && !_suppressActivation)
                {
                    Configuration = s_bl.Admin.GetConfig();
                    _originalMaxDistance = Configuration.MaxDeliveryDistance ?? MaxDistanceConverter.DefaultDistance;
                }
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void orderObserver()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && !_suppressActivation)
                {
                    OrderSummary = s_bl.Orders.GetOrderStatusSummary();
                }
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        #endregion

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnToggleSimulator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (s_bl.Admin.IsSimulatorRunning)
                {
                    s_bl.Admin.StopSimulator();
                    btnToggleSimulator.Content = "▶ Start";
                    txtSimulatorInterval.IsEnabled = true;
                }
                else
                {
                    if (!int.TryParse(txtSimulatorInterval.Text, out int interval) || interval <= 0)
                    {
                        MessageBox.Show("Please enter a valid positive interval in minutes.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    s_bl.Admin.StartSimulator(interval);
                    btnToggleSimulator.Content = "⏹ Stop";
                    txtSimulatorInterval.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulator error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
