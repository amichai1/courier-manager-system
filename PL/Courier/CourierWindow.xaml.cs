using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using BlApi;
using BO;
using PL.Converters;

namespace PL.Courier
{
    public partial class CourierWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();
        private int _courierId = 0;
        private BO.Courier? _originalCourier; // Store original for rollback

        public CourierWindow()
            : this(0) { }

        public CourierWindow(int id = 0, bool isReadOnly = false)
        {
            InitializeComponent();
            _courierId = id;

            // Set button text and visibility based on mode
            SaveButtonVisibility = isReadOnly ? Visibility.Collapsed : Visibility.Visible;
            ButtonText = id == 0 ? "Add" : "Update";
            DeleteVisibility = (id > 0 && !isReadOnly) ? Visibility.Visible : Visibility.Collapsed;
            IsFieldsEnabled = !isReadOnly;

            LoadCourier();
            LoadCurrentOrder();
            LoadDeliveryHistory();
        }

        #region Dependency Properties

        public BO.Courier? CurrentCourier
        {
            get => (BO.Courier?)GetValue(CurrentCourierProperty);
            set => SetValue(CurrentCourierProperty, value);
        }
        public static readonly DependencyProperty CurrentCourierProperty =
            DependencyProperty.Register("CurrentCourier", typeof(BO.Courier), typeof(CourierWindow), new PropertyMetadata(null));

        public BO.Order? CurrentOrder
        {
            get => (BO.Order?)GetValue(CurrentOrderProperty);
            set => SetValue(CurrentOrderProperty, value);
        }
        public static readonly DependencyProperty CurrentOrderProperty =
            DependencyProperty.Register("CurrentOrder", typeof(BO.Order), typeof(CourierWindow), new PropertyMetadata(null));

        public Visibility CurrentOrderVisibility
        {
            get => (Visibility)GetValue(CurrentOrderVisibilityProperty);
            set => SetValue(CurrentOrderVisibilityProperty, value);
        }
        public static readonly DependencyProperty CurrentOrderVisibilityProperty =
            DependencyProperty.Register("CurrentOrderVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

        public Visibility NoCurrentOrderVisibility
        {
            get => (Visibility)GetValue(NoCurrentOrderVisibilityProperty);
            set => SetValue(NoCurrentOrderVisibilityProperty, value);
        }
        public static readonly DependencyProperty NoCurrentOrderVisibilityProperty =
            DependencyProperty.Register("NoCurrentOrderVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Visible));

        public IEnumerable<BO.DeliveryInList>? DeliveryHistory
        {
            get => (IEnumerable<BO.DeliveryInList>?)GetValue(DeliveryHistoryProperty);
            set => SetValue(DeliveryHistoryProperty, value);
        }
        public static readonly DependencyProperty DeliveryHistoryProperty =
            DependencyProperty.Register("DeliveryHistory", typeof(IEnumerable<BO.DeliveryInList>), typeof(CourierWindow), new PropertyMetadata(null));

        public Visibility NoHistoryVisibility
        {
            get => (Visibility)GetValue(NoHistoryVisibilityProperty);
            set => SetValue(NoHistoryVisibilityProperty, value);
        }
        public static readonly DependencyProperty NoHistoryVisibilityProperty =
            DependencyProperty.Register("NoHistoryVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

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

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Window lifecycle: called when window is fully loaded
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Couriers.RemoveObserver(CourierObserver); } catch { /* ignore */ }
            try { s_bl.Orders.RemoveObserver(OrderObserver); } catch { /* ignore */ }
        }

        #endregion

        #region Loading

        private void LoadCourier()
        {
            try
            {
                if (_courierId == 0)
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
                        DeliveryType = BO.DeliveryType.Car,
                        StartWorkingDate = s_bl.Admin.GetClock(),
                        Location = new BO.Location() { Latitude = 32.098799, Longitude = 34.8979087 }
                    };
                    _originalCourier = CloneCourier(CurrentCourier);
                }
                else
                {
                    // Load existing courier
                    CurrentCourier = s_bl.Couriers.Read(_courierId);
                    if (CurrentCourier == null)
                    {
                        MessageBox.Show($"Courier ID {_courierId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }
                    _originalCourier = CloneCourier(CurrentCourier);

                    // Register observer for real-time updates
                    try { s_bl.Couriers.AddObserver(CourierObserver); } catch { /* ignore if not available */ }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load courier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private BO.Courier CloneCourier(BO.Courier courier)
        {
            return new BO.Courier
            {
                Id = courier.Id,
                Name = courier.Name,
                Phone = courier.Phone,
                Email = courier.Email,
                Password = courier.Password,
                IsActive = courier.IsActive,
                MaxDeliveryDistance = courier.MaxDeliveryDistance,
                DeliveryType = courier.DeliveryType,
                StartWorkingDate = courier.StartWorkingDate,
                Location = courier.Location
            };
        }

        private void LoadCurrentOrder()
        {
            try
            {
                if (_courierId > 0)
                {
                    // LINQ Method Syntax - Get current active order for this courier
                    var currentOrder = s_bl.Orders.ReadAll(o => o.CourierId == _courierId)
                        .Where(o => o.OrderStatus == BO.OrderStatus.InProgress || o.OrderStatus == BO.OrderStatus.AssociatedToCourier)
                        .OrderByDescending(o => o.CourierAssociatedDate)
                        .FirstOrDefault();

                    if (currentOrder != null)
                    {
                        CurrentOrder = currentOrder;
                        CurrentOrderVisibility = Visibility.Visible;
                        NoCurrentOrderVisibility = Visibility.Collapsed;

                        // Register order observer
                        try { s_bl.Orders.AddObserver(OrderObserver); } catch { /* ignore */ }
                    }
                    else
                    {
                        CurrentOrder = null;
                        CurrentOrderVisibility = Visibility.Collapsed;
                        NoCurrentOrderVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    CurrentOrder = null;
                    CurrentOrderVisibility = Visibility.Collapsed;
                    NoCurrentOrderVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading current order: {ex.Message}");
                CurrentOrder = null;
                CurrentOrderVisibility = Visibility.Collapsed;
                NoCurrentOrderVisibility = Visibility.Visible;
            }
        }

        private void LoadDeliveryHistory()
        {
            try
            {
                if (_courierId > 0)
                {
                    // LINQ Method Syntax - Get completed orders for this courier as delivery history
                    var deliveries = s_bl.Orders.ReadAll(o => o.CourierId == _courierId)
                        .Where(o => o.OrderStatus == BO.OrderStatus.Delivered)
                        .OrderByDescending(o => o.DeliveryDate ?? o.PickupDate ?? o.CourierAssociatedDate)
                        .Select(o => new BO.DeliveryInList
                        {
                            OrderId = o.Id,
                            CustomerName = o.CustomerName ?? string.Empty,
                            Status = o.OrderStatus.ToString(),
                            PickupDate = o.PickupDate,
                            DeliveryDate = o.DeliveryDate
                        })
                        .ToList();

                    DeliveryHistory = deliveries;
                    NoHistoryVisibility = deliveries.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    DeliveryHistory = new List<BO.DeliveryInList>();
                    NoHistoryVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading delivery history: {ex.Message}");
                DeliveryHistory = new List<BO.DeliveryInList>();
                NoHistoryVisibility = Visibility.Visible;
            }
        }

        #endregion

        #region Validation

        private bool ValidateAndShowErrors()
        {
            if (CurrentCourier == null)
            {
                return false;
            }

            var errors = new StringBuilder();

            // Validate ID (must be exactly 9 digits)
            if (!IsraeliIdConverter.IsValid(CurrentCourier.Id))
            {
                errors.AppendLine($"• {IsraeliIdConverter.GetErrorMessage()}");
            }

            // Validate Name
            if (string.IsNullOrWhiteSpace(CurrentCourier.Name))
            {
                errors.AppendLine("• Name is required.");
            }

            // Validate Phone
            if (!PhoneNumberConverter.IsValid(CurrentCourier.Phone))
            {
                errors.AppendLine($"• {PhoneNumberConverter.GetErrorMessage()}");
            }

            // Validate Email
            if (!EmailConverter.IsValid(CurrentCourier.Email))
            {
                errors.AppendLine($"• {EmailConverter.GetErrorMessage()}");
            }

            // Validate MaxDeliveryDistance (if provided)
            if (CurrentCourier.MaxDeliveryDistance.HasValue && 
                !MaxDistanceConverter.IsValid(CurrentCourier.MaxDeliveryDistance))
            {
                errors.AppendLine($"• {MaxDistanceConverter.GetErrorMessage()}");
            }

            // Validate: Cannot deactivate courier with active order
            if (!CurrentCourier.IsActive && CurrentOrder != null)
            {
                errors.AppendLine("• Cannot deactivate courier with an active order. Please complete or reassign the order first.");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(
                    $"Please correct the following errors:\n\n{errors}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void RevertToOriginal()
        {
            if (_originalCourier != null)
            {
                CurrentCourier = CloneCourier(_originalCourier);
            }
        }

        #endregion

        #region Actions

        private void btnAddUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentCourier == null)
                {
                    throw new Exception("Courier is null.");
                }

                // Validate all fields
                if (!ValidateAndShowErrors())
                {
                    return;
                }

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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Are you sure you want to delete this courier?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
            {
                return;
            }

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

        #endregion

        #region Observers

        private void CourierObserver()
        {
            try
            {
                if (CurrentCourier?.Id > 0)
                {
                    var updated = s_bl.Couriers.Read(CurrentCourier.Id);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CurrentCourier = updated;
                        _originalCourier = CloneCourier(updated);
                        LoadCurrentOrder();
                        LoadDeliveryHistory();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("The courier has been deleted from the system. The window will close.",
                                    "Entity Deleted",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                    Close();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void OrderObserver()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadCurrentOrder();
                    LoadDeliveryHistory();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Order observer error: {ex.Message}");
            }
        }

        #endregion
    }
}
