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
            else
            {
                BringToFront();
            }
        }

        /// <summary>
        /// Forces the window to appear above other windows without affecting other windows
        /// </summary>
        private static void BringToFront()
        {
            if (_instance == null) return;

            if (_instance.WindowState == WindowState.Minimized)
            {
                _instance.WindowState = WindowState.Normal;
            }

            // Use Focus() instead of Activate() + Topmost trick
            _instance.Focus();
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

        public BO.CourierSortBy SelectedSortBy { get; set; } = BO.CourierSortBy.Id;

        public BO.SortOrder SelectedSortOrder { get; set; } = BO.SortOrder.Ascending;

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
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        #endregion

        #region Query & UI actions

        private void QueryCourierList()
        {
            try
            {
                IEnumerable<BO.CourierInList> couriers = s_bl.Couriers.GetCourierList();

                // Apply filter
                if (DeliveryTypeFilter is BO.DeliveryType selectedType)
                {
                    couriers = couriers.Where(c => c.DeliveryType == selectedType);
                }

                // Apply sorting
                couriers = ApplySorting(couriers);

                CourierList = couriers.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<BO.CourierInList> ApplySorting(IEnumerable<BO.CourierInList> couriers)
        {
            bool ascending = SelectedSortOrder == BO.SortOrder.Ascending;

            IOrderedEnumerable<BO.CourierInList> sortedCouriers = SelectedSortBy switch
            {
                BO.CourierSortBy.Availability => ascending
                    ? couriers.OrderBy(c => c.IsActive)
                    : couriers.OrderByDescending(c => c.IsActive),

                BO.CourierSortBy.Id => ascending
                    ? couriers.OrderBy(c => c.Id)
                    : couriers.OrderByDescending(c => c.Id),

                BO.CourierSortBy.Name => ascending
                    ? couriers.OrderBy(c => c.Name)
                    : couriers.OrderByDescending(c => c.Name),

                BO.CourierSortBy.StartDate => ascending
                    ? couriers.OrderBy(c => c.StartWorkingDate)
                    : couriers.OrderByDescending(c => c.StartWorkingDate),

                BO.CourierSortBy.HasOrder => ascending
                    ? couriers.OrderBy(c => c.CurrentIdOrder.HasValue)
                    : couriers.OrderByDescending(c => c.CurrentIdOrder.HasValue),

                BO.CourierSortBy.DeliveryType => ascending
                    ? couriers.OrderBy(c => c.DeliveryType)
                    : couriers.OrderByDescending(c => c.DeliveryType),

                BO.CourierSortBy.LateDeliveries => ascending
                    ? couriers.OrderBy(c => c.DeliveredLate)
                    : couriers.OrderByDescending(c => c.DeliveredLate),

                BO.CourierSortBy.OnTimeDeliveries => ascending
                    ? couriers.OrderBy(c => c.DeliveredOnTime)
                    : couriers.OrderByDescending(c => c.DeliveredOnTime),

                _ => couriers.OrderBy(c => c.Id)
            };

            return sortedCouriers;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                QueryCourierList();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                QueryCourierList();
            }
        }

        private void CourierCard_Click(object sender, MouseButtonEventArgs e)
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

            if (sender is FrameworkElement element && element.DataContext is BO.CourierInList courier)
            {
                var window = new CourierWindow(courier.Id);
                window.Show();
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var window = new CourierWindow();
            window.Show();
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
