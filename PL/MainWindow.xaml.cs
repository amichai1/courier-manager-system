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
namespace PL
{
    public partial class MainWindow : Window
    {
        static readonly IBI s_bl = BL.Factory.Get();

        public MainWindow()
        {
            InitializeComponent();
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

        #endregion

        #region Clock Buttons

        // קידום בדקה
        private void btnAddOneMinute_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Minute);
        }

        // קידום בשעה
        private void btnAddOneHour_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Hour);
        }

        // קידום ביום
        private void btnAddOneDay_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.TimeUnit.Day);
        }

        // קידום בחודש (התיקון שביקשת)
        private void btnAddOneMonth_Click(object sender, RoutedEventArgs e)
        {
            // עכשיו זה יעבוד מושלם גם בפברואר
            s_bl.Admin.ForwardClock(BO.TimeUnit.Month);
        }

        // קידום בשנה
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
                s_bl.Admin.SetConfig(Configuration);
                MessageBox.Show("Configuration updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region List Window Buttons

        private void btnDeliveries_Click(object sender, RoutedEventArgs e)
        {
            new Delivery.DeliveryListWindow().Show();
        }

        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            new Order.OrderListWindow().Show();
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
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    CloseAllWindowsExceptMain();
                    s_bl.Admin.InitializeDB();
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Database initialized successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
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
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    CloseAllWindowsExceptMain();
                    s_bl.Admin.ResetDB();
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Database reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show($"Error resetting database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseAllWindowsExceptMain()
        {
            for (int i = Application.Current.Windows.Count - 1; i >= 0; i--)
            {
                if (Application.Current.Windows[i] != this)
                {
                    Application.Current.Windows[i].Close();
                }
            }
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentTime = s_bl.Admin.GetClock();
            Configuration = s_bl.Admin.GetConfig();
            s_bl.Admin.AddClockObserver(clockObserver);
            s_bl.Admin.AddConfigObserver(configObserver);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            s_bl.Admin.RemoveClockObserver(clockObserver);
            s_bl.Admin.RemoveConfigObserver(configObserver);
        }

        #endregion

        #region Observers

        private void clockObserver()
        {
            Dispatcher.Invoke(() =>
            {
                CurrentTime = s_bl.Admin.GetClock();
            });
        }

        private void configObserver()
        {
            Dispatcher.Invoke(() =>
            {
                Configuration = s_bl.Admin.GetConfig();
            });
        }
        #endregion
    }
}
