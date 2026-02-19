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
using System.Threading.Tasks;

namespace PL
{
    public partial class MainWindow : Window
    {
        static readonly IBI s_bl = BL.Factory.Get();
        private double _originalMaxDistance;

        private static MainWindow? s_instance = null;
        public static bool IsAdminWindowOpen => s_instance != null;

        // Flag to prevent window activation during operations
        private bool _suppressActivation = false;
        private bool _isInitializing = false;

        // Observer Mutexes for thread-safe updates
        private readonly PL.Helpers.ObserverMutex _clockMutex = new();
        private readonly PL.Helpers.ObserverMutex _configMutex = new();
        private readonly PL.Helpers.ObserverMutex _simulatorMutex = new();

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

        // Simulator Interval
        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register("Interval", typeof(int), typeof(MainWindow), new PropertyMetadata(1));

        // Is Simulator Running
        public bool IsSimulatorRunning
        {
            get { return (bool)GetValue(IsSimulatorRunningProperty); }
            set { SetValue(IsSimulatorRunningProperty, value); }
        }

        public static readonly DependencyProperty IsSimulatorRunningProperty =
            DependencyProperty.Register("IsSimulatorRunning", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        #endregion

        #region Clock Buttons

        private void btnAddOneMinute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.ForwardClock(BO.TimeUnit.Minute);
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneHour_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.ForwardClock(BO.TimeUnit.Hour);
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneDay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.ForwardClock(BO.TimeUnit.Day);
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneMonth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.ForwardClock(BO.TimeUnit.Month);
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneYear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.ForwardClock(BO.TimeUnit.Year);
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            e.Handled = true; 
        }

        private void InProgressOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.InProgress);
            e.Handled = true; 
        }

        private void DeliveredOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.Delivered);
            e.Handled = true; 
        }

        private void RefusedOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.OrderRefused);
            e.Handled = true; 
        }

        private void CanceledOrders_Click(object sender, MouseButtonEventArgs e)
        {
            PL.Order.OrderListWindow.ShowListFiltered(BO.OrderStatus.Canceled);
            e.Handled = true; 
        }

        #endregion

        #region List Window Buttons

        private void btnDeliveries_Click(object sender, RoutedEventArgs e)
        {
            PL.Delivery.DeliveryListWindow.ShowList();
            e.Handled = true;
        }

        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            PL.Order.OrderListWindow.ShowList();
            e.Handled = true;
        }

        private void btnCouriers_Click(object sender, RoutedEventArgs e)
        {
            PL.Courier.CourierListWindow.ShowList();
            e.Handled = true; 
        }

        #endregion

        #region Database Buttons

        private void btnInitializeDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to initialize the database? This will create initial data.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    CloseAllWindowsExceptMainAndLogin();
                    
                    _suppressActivation = true;
                    _isInitializing = true;

                    try
                    {
                        s_bl.Admin.InitializeDB();

                        RefreshAllData();
                        MessageBox.Show("Database initialized successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        _suppressActivation = false;
                        _isInitializing = false;
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                _suppressActivation = false;
                _isInitializing = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _suppressActivation = false;
                _isInitializing = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Error initializing database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to reset the database? This will delete all data.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    CloseAllWindowsExceptMainAndLogin();
                    
                    _suppressActivation = true;
                    _isInitializing = true;

                    try
                    {
                        s_bl.Admin.ResetDB();

                        RefreshAllData();
                        MessageBox.Show("Database reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        _suppressActivation = false;
                        _isInitializing = false;
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                _suppressActivation = false;
                _isInitializing = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Cannot perform operation: {ex.Message}", "Simulator Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _suppressActivation = false;
                _isInitializing = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Error resetting database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Stop simulator if running before closing
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
            if (_suppressActivation || _isInitializing)
            {
                return;
            }
            base.OnActivated(e);
        }

        #endregion

        #region Observers

        private void clockObserver()
        {
            if (_clockMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;
            
            Dispatcher.BeginInvoke(async () =>
            {
                if (IsLoaded && !_suppressActivation && !_isInitializing)
                {
                    CurrentTime = s_bl.Admin.GetClock();
                }
                
                if (await _clockMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    clockObserver();
            });
        }

        private void configObserver()
        {
            if (_configMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;
            
            Dispatcher.BeginInvoke(async () =>
            {
                if (IsLoaded && !_suppressActivation && !_isInitializing)
                {
                    var cfg = s_bl.Admin.GetConfig();
                    Configuration = cfg;
                    _originalMaxDistance = Configuration.MaxDeliveryDistance ?? MaxDistanceConverter.DefaultDistance;
                }
                
                if (await _configMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    configObserver();
            });
        }

        private void orderObserver()
        {
            if (_simulatorMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;
            
            Dispatcher.BeginInvoke(async () =>
            {
                if (IsLoaded && !_suppressActivation && !_isInitializing)
                {
                    var summary = s_bl.Orders.GetOrderStatusSummary();
                    OrderSummary = summary;
                }
                
                if (await _simulatorMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    orderObserver();
            });
        }

        #endregion

        #region Simulator Control

        private void btnToggleSimulator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (s_bl.Admin.IsSimulatorRunning)
                {
                    s_bl.Admin.StopSimulator();
                    IsSimulatorRunning = false;
                }
                else
                {
                    if (!int.TryParse(Interval.ToString(), out int interval) || interval <= 0)
                    {
                        MessageBox.Show("Please enter a valid positive interval in minutes.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    s_bl.Admin.StartSimulator(interval);
                    IsSimulatorRunning = true;
                }
            }
            catch (BO.BLTemporaryNotAvailableException ex)
            {
                MessageBox.Show($"Simulator error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulator error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
