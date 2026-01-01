using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PL.Courier
{
    public partial class CourierListWindow : Window
    {
        static readonly BlApi.IBI s_bl = BL.Factory.Get();

        private static CourierListWindow? _instance = null;

        private CourierListWindow()
        {
            InitializeComponent();
        }

        public static void ShowList()
        {
            if (_instance == null)
            {
                _instance = new CourierListWindow();
                _instance.Show();
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

        public IEnumerable<BO.CourierInList>? CourierList
        {
            get => (IEnumerable<BO.CourierInList>?)GetValue(CourierListProperty);
            set => SetValue(CourierListProperty, value);
        }

        public static readonly DependencyProperty CourierListProperty =
            DependencyProperty.Register("CourierList", typeof(IEnumerable<BO.CourierInList>), typeof(CourierListWindow), new PropertyMetadata(null));

        #endregion

        #region Bound normal properties

        public Object DeliveryTypeFilter { get; set; } = "All";

        public BO.CourierInList? SelectedCourier { get; set; }

        #endregion

        #region Lifecycle & observers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QueryCourierList();
            try { s_bl.Couriers.AddObserver(CourierListObserver); } catch { }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Couriers.RemoveObserver(CourierListObserver); } catch { }
            _instance = null;
        }

        private void CourierListObserver()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_instance != null && _instance.IsLoaded)
                {
                    QueryCourierList();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        #endregion

        #region Query & UI actions

        private void QueryCourierList()
        {
            try
            {
                var allCouriers = s_bl.Couriers.GetCourierList();

                if (DeliveryTypeFilter is BO.DeliveryType selectedType)
                {
                    CourierList = allCouriers.Where(c => c.DeliveryType == selectedType).ToList();
                }
                else
                {
                    CourierList = allCouriers;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            QueryCourierList();
        }

        private void CourierCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BO.CourierInList courier)
            {
                new CourierWindow(courier.Id).Show();
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            new CourierWindow().Show();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            QueryCourierList();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (sender is Button btn && btn.DataContext is BO.CourierInList item)
            {
                var res = MessageBox.Show($"Are you sure you want to delete this courier?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    s_bl.Couriers.Delete(item.Id);
                    MessageBox.Show("Courier deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    QueryCourierList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (sender is CheckBox cb && cb.DataContext is BO.CourierInList courierInList)
            {
                try
                {
                    var newStatus = cb.IsChecked == true ? BO.CourierStatus.Available : BO.CourierStatus.Inactive;
                    s_bl.Couriers.SetCourierStatus(courierInList.Id, newStatus);
                }
                catch (Exception ex)
                {
                    cb.IsChecked = !cb.IsChecked;
                    MessageBox.Show($"Status update failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
