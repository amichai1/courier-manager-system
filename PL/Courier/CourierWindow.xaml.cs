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
    /// Interaction logic for CourierWindow.xaml
    /// </summary>
    public partial class CourierWindow : Window
    {
        static readonly BlApi.IBI s_bl = BL.Factory.Get();

        public CourierWindow()
            : this(0) { }

        public CourierWindow(int id = 0, bool isReadOnly = false)
        {
            InitializeComponent();
            // 1. לוגיקה לכפתור השמירה/עדכון (Add/Update)
            // נראה אותו רק אם אנחנו לא במצב קריאה בלבד
            // set ButtonText depending on mode
            SaveButtonVisibility = isReadOnly ? Visibility.Collapsed : Visibility.Visible;
            ButtonText = id == 0 ? "Add" : "Update";

            DeleteVisibility = (id > 0 && !isReadOnly) ? Visibility.Visible : Visibility.Collapsed;
            DeleteVisibility = id == 0 ? Visibility.Collapsed : Visibility.Visible;

            IsFieldsEnabled = !isReadOnly;

            if (id == 0)
            {
                // New courier: create empty BO object with sensible defaults
                CurrentCourier = new BO.Courier()
                {
                    Id = 0,
                    Name = string.Empty,
                    Phone = string.Empty,
                    Email = string.Empty,
                    Password = string.Empty,
                    IsActive = true,
                    MaxDeliveryDistance = null,
                    DeliveryType = (BO.DeliveryType)0,
                    StartWorkingDate = s_bl.Admin.GetClock(),
                    Location = new BO.Location() { Latitude = 32.098799, Longitude = 34.8979087 }
                };
            }
            else
            {
                try
                {
                    CurrentCourier = s_bl.Couriers.Read(id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load courier: {ex.Message}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    Close(); // close window if load fails
                    return; // exit constructor
                }

                // register observer (best-effort; signature may vary)
                try { s_bl.Couriers.AddObserver(CourierObserver); } catch { }
            }
        }

        #region Dependency Properties

        public BO.Courier? CurrentCourier
        {
            get => (BO.Courier?)GetValue(CurrentCourierProperty);
            set => SetValue(CurrentCourierProperty, value);
        }
        public static readonly DependencyProperty CurrentCourierProperty =
            DependencyProperty.Register("CurrentCourier", typeof(BO.Courier), typeof(CourierWindow), new PropertyMetadata(null));

        public Visibility SaveButtonVisibility
        {
            get => (Visibility)GetValue(SaveButtonVisibilityProperty);
            set => SetValue(SaveButtonVisibilityProperty, value);
        }
        public static readonly DependencyProperty SaveButtonVisibilityProperty =
            DependencyProperty.Register("SaveButtonVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Visible));

        public bool IsFieldsEnabled
        {
            get => (bool)GetValue(IsFieldsEnabledProperty);
            set => SetValue(IsFieldsEnabledProperty, value);
        }
        public static readonly DependencyProperty IsFieldsEnabledProperty =
            DependencyProperty.Register("IsFieldsEnabled", typeof(bool), typeof(CourierWindow), new PropertyMetadata(true));
        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(CourierWindow), new PropertyMetadata("Add"));

        public Visibility DeleteVisibility
        {
            get => (Visibility)GetValue(DeleteVisibilityProperty);
            set => SetValue(DeleteVisibilityProperty, value);
        }

        public static readonly DependencyProperty DeleteVisibilityProperty =
            DependencyProperty.Register("DeleteVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

        #endregion

        private void btnAddUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Basic UI-side validation: ensure name not empty and MaxDeliveryDistance format ok
            try
            {
                if (CurrentCourier == null)
                    throw new Exception("Courier is null.");

                if (string.IsNullOrWhiteSpace(CurrentCourier.Name))
                {
                    MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // MaxDeliveryDistance can be null or double — binding already sets value or null
                // Call BL for logical validation / persistence
                if (ButtonText == "Add")
                {
                    s_bl.Couriers.Create(CurrentCourier);
                    MessageBox.Show("Courier added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    s_bl.Couriers.Update(CurrentCourier);
                    MessageBox.Show("Courier updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Close();
            }
            catch (BO.BLException boex)
            {
                MessageBox.Show($"Business error: {boex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show($"Are you sure you want to delete this courier?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;            
            try
            {
                if (CurrentCourier != null && CurrentCourier.Id > 0)
                {
                    s_bl.Couriers.Delete(CurrentCourier.Id);
                    MessageBox.Show("Courier deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (BO.BLException boex)
            {
                MessageBox.Show($"Cannot delete: {boex.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CourierObserver()
        {
            // best-effort: refresh the courier from BL if it still exists
            try
            {
                if (CurrentCourier?.Id > 0)
                {
                    int id = CurrentCourier.Id;
                    var updated = s_bl.Couriers.Read(id);
                    Dispatcher.Invoke(() => CurrentCourier = updated);
                }
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("The courier has been deleted from the system. The window will close.",
                                    "Entity Deleted",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                    Close();
                });
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Couriers.RemoveObserver(CourierObserver); } catch { }
        }
    }
}
