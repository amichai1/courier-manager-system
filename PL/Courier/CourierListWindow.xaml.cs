using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PL.Courier
{
    /// <summary>
    /// Interaction logic for CourierListWindow.xaml
    /// </summary>
    public partial class CourierListWindow : Window
    {
        static readonly BlApi.IBI s_bl = BL.Factory.Get();

        public CourierListWindow()
        {
            InitializeComponent();
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

        #region Bound normal properties (used for selection/filter)

        // Filter enum property (bound to ComboBox). Use a 'None' value in the enum for no-filter if available.
        public Object DeliveryTypeFilter { get; set; } = "All";

        public BO.CourierInList? SelectedCourier { get; set; }

        #endregion

        #region Lifecycle & observers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QueryCourierList();
            // register observer on couriers in BL (assumes BL exposes AddObserver on Courier service)
            try { s_bl.Couriers.AddObserver(CourierListObserver); } catch { /* ignore if signature differs */ }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Couriers.RemoveObserver(CourierListObserver); } catch { /* ignore */ }
        }

        private void CourierListObserver()
            => Dispatcher.Invoke(QueryCourierList);

        #endregion

        #region Query & UI actions

        private void QueryCourierList()
        {
            try
            {
                var allCouriers = s_bl.Couriers.ReadAll();

                // if delivery type is  Enum - filter by it
                if (DeliveryTypeFilter is BO.DeliveryType selectedType)
                {
                    CourierList = (IEnumerable<BO.CourierInList>?)allCouriers.Where(c => c.DeliveryType == selectedType).ToList();
                }
                else
                {
                    // If DeliveryTypeFilter is a special 'All' show all
                    CourierList = (IEnumerable<BO.CourierInList>?)allCouriers;
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

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedCourier != null)
            {
                new CourierWindow(SelectedCourier.Id).Show();
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
            if (sender is Button btn && btn.DataContext is BO.CourierInList item)
            {
                var res = MessageBox.Show($"Are you sure you want to delete this courier?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;

                try
                {
                    s_bl.Couriers.Delete(item.Id);
                    MessageBox.Show("Courier deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // BL should notify observers; still refresh just in case
                    QueryCourierList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
