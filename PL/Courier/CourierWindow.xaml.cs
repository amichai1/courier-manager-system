using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BlApi;
using Helpers;
using PL.Converters;

namespace PL.Courier;

public partial class CourierWindow : Window
{
    private static readonly IBI s_bl = BL.Factory.Get();
    private int _courierId = 0;
    private BO.Courier? _originalCourier;
    private bool _isNewCourier = false;
    private bool _isClosed = false;
    private bool _isCourierMode = false; // Track if this window was opened by a courier

    public bool IsClosed => _isClosed;

    public CourierWindow()
        : this(0) { }

    public CourierWindow(int id = 0, bool isReadOnly = false)
    {
        InitializeComponent();
        _courierId = id;
        _isNewCourier = (id == 0);

        // Set button text and visibility based on mode
        SaveButtonVisibility = isReadOnly ? Visibility.Collapsed : Visibility.Visible;
        ButtonText = _isNewCourier ? "Add" : "Update";
        DeleteVisibility = (id > 0 && !isReadOnly) ? Visibility.Visible : Visibility.Collapsed;
        IsFieldsEnabled = !isReadOnly;
        
        // Hide password field in ADD mode
        PasswordFieldVisibility = _isNewCourier ? Visibility.Collapsed : Visibility.Visible;

        // Initialize action buttons as hidden by default (will be shown for courier users)
        ActionButtonsVisibility = Visibility.Collapsed;
        SelectOrderButtonEnabled = true;

        // Initialize promote status button as hidden
        PromoteStatusButtonVisibility = Visibility.Collapsed;
        PromoteStatusButtonText = "";

        LoadCourier();
        LoadCurrentOrder();
    }

    #region Dependency Properties

    public BO.Courier? CurrentCourier
    {
        get => (BO.Courier?)GetValue(CurrentCourierProperty);
        set => SetValue(CurrentCourierProperty, value);
    }
    public static readonly DependencyProperty CurrentCourierProperty =
        DependencyProperty.Register("CurrentCourier", typeof(BO.Courier), typeof(CourierWindow), new PropertyMetadata(null));

    public string CurrentPassword
    {
        get => (string)GetValue(CurrentPasswordProperty);
        set => SetValue(CurrentPasswordProperty, value);
    }
    public static readonly DependencyProperty CurrentPasswordProperty =
        DependencyProperty.Register("CurrentPassword", typeof(string), typeof(CourierWindow), new PropertyMetadata(string.Empty));

    public BO.Order? CurrentOrder
    {
        get => (BO.Order?)GetValue(CurrentOrderProperty);
        set => SetValue(CurrentOrderProperty, value);
    }
    public static readonly DependencyProperty CurrentOrderProperty =
        DependencyProperty.Register("CurrentOrder", typeof(BO.Order), typeof(CourierWindow), new PropertyMetadata(null, OnCurrentOrderChanged));

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

    public Visibility ActionButtonsVisibility
    {
        get => (Visibility)GetValue(ActionButtonsVisibilityProperty);
        set => SetValue(ActionButtonsVisibilityProperty, value);
    }
    public static readonly DependencyProperty ActionButtonsVisibilityProperty =
        DependencyProperty.Register("ActionButtonsVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

    public bool SelectOrderButtonEnabled
    {
        get => (bool)GetValue(SelectOrderButtonEnabledProperty);
        set => SetValue(SelectOrderButtonEnabledProperty, value);
    }
    public static readonly DependencyProperty SelectOrderButtonEnabledProperty =
        DependencyProperty.Register("SelectOrderButtonEnabled", typeof(bool), typeof(CourierWindow), new PropertyMetadata(true));

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

    public Visibility PasswordFieldVisibility
    {
        get => (Visibility)GetValue(PasswordFieldVisibilityProperty);
        set => SetValue(PasswordFieldVisibilityProperty, value);
    }
    public static readonly DependencyProperty PasswordFieldVisibilityProperty =
        DependencyProperty.Register("PasswordFieldVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

    public Visibility PromoteStatusButtonVisibility
    {
        get => (Visibility)GetValue(PromoteStatusButtonVisibilityProperty);
        set => SetValue(PromoteStatusButtonVisibilityProperty, value);
    }
    public static readonly DependencyProperty PromoteStatusButtonVisibilityProperty =
        DependencyProperty.Register("PromoteStatusButtonVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

    public string PromoteStatusButtonText
    {
        get => (string)GetValue(PromoteStatusButtonTextProperty);
        set => SetValue(PromoteStatusButtonTextProperty, value);
    }
    public static readonly DependencyProperty PromoteStatusButtonTextProperty =
        DependencyProperty.Register("PromoteStatusButtonText", typeof(string), typeof(CourierWindow), new PropertyMetadata(""));

    #endregion

    #region Lifecycle

    private static void OnCurrentOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CourierWindow window)
        {
            window.UpdatePromoteStatusButton();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isNewCourier)
        {
            courierIdTextBox.IsReadOnly = true;
        }
        
        if (CurrentCourier != null && !_isNewCourier && !string.IsNullOrEmpty(CurrentCourier.Password))
        {
            CurrentPassword = CurrentCourier.Password;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _isClosed = true;
        try { s_bl.Couriers.RemoveObserver(CourierObserver); } catch { }
        try { s_bl.Orders.RemoveObserver(OrderObserver); } catch { }
    }

    #endregion

    #region Loading

    private void LoadCourier()
    {
        try
        {
            if (_courierId == 0)
            {
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
                    Location = new BO.Location() { Latitude = 32.098799, Longitude = 34.8979087 },
                    AverageDeliveryTime = "—"
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
                
                CurrentCourier.AverageDeliveryTime = s_bl.Couriers.CalculateAverageDeliveryTime(_courierId);
                CurrentPassword = CurrentCourier.Password;
                
                _originalCourier = CloneCourier(CurrentCourier);
                try { s_bl.Couriers.AddObserver(CourierObserver); } catch { }
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
            Location = courier.Location,
            AverageDeliveryTime = courier.AverageDeliveryTime
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
                    SelectOrderButtonEnabled = false; // Disable "Select Order" when there's a current order

                    // Register order observer
                    try
                    { s_bl.Orders.AddObserver(OrderObserver); }
                    catch { /* ignore */ }
                }
                else
                {
                    CurrentOrder = null;
                    CurrentOrderVisibility = Visibility.Collapsed;
                    NoCurrentOrderVisibility = Visibility.Visible;
                    SelectOrderButtonEnabled = true; // Enable "Select Order" when no current order
                }
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

    #endregion

    #region Status Promotion

    private void UpdatePromoteStatusButton()
    {
        if (CurrentOrder == null || !_isCourierMode)
        {
            PromoteStatusButtonVisibility = Visibility.Collapsed;
            PromoteStatusButtonText = "";
            return;
        }

        // Determine button text and visibility based on order status
        switch (CurrentOrder.OrderStatus)
        {
            case BO.OrderStatus.AssociatedToCourier:
                PromoteStatusButtonVisibility = Visibility.Visible;
                PromoteStatusButtonText = "🔼 Pick Up";
                break;

            case BO.OrderStatus.InProgress:
                PromoteStatusButtonVisibility = Visibility.Visible;
                PromoteStatusButtonText = "✓ Complete";
                break;

            default:
                PromoteStatusButtonVisibility = Visibility.Collapsed;
                PromoteStatusButtonText = "";
                break;
        }
    }

    private void btnPromoteStatus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentOrder == null || CurrentOrder.Id <= 0)
            {
                MessageBox.Show("No current order to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Store action type for display before clearing order
            BO.OrderStatus originalStatus = CurrentOrder.OrderStatus;
            int orderId = CurrentOrder.Id;

            try
            {
                switch (originalStatus)
                {
                    case BO.OrderStatus.AssociatedToCourier:
                        // Promote to InProgress (Pick up)
                        s_bl.Orders.PickUpOrder(orderId);
                        MessageBox.Show(
                            $"Order #{orderId} picked up successfully!",
                            "Order Picked Up",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;

                    case BO.OrderStatus.InProgress:
                        // Promote to Delivered (Complete)
                        s_bl.Orders.DeliverOrder(orderId);
                        MessageBox.Show(
                            $"Order #{orderId} completed successfully!",
                            "Order Completed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;

                    default:
                        MessageBox.Show("Order cannot be promoted from current status.", "Operation Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                }

                // After BL operations, the order observer will automatically trigger LoadCurrentOrder()
                // This ensures that:
                // 1. CurrentCourierWindow's order display refreshes
                // 2. OrderListWindow's list refreshes (via list observer)
                // 3. CourierListWindow's list refreshes (via list observer)
                // All via the observer pattern - no need for manual refresh here
            }
            catch (BO.BLException ex)
            {
                MessageBox.Show($"Cannot process order: {ex.Message}", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Validation

    private bool ValidateAndShowErrors()
    {
        if (CurrentCourier == null)
            return false;

        var errors = new StringBuilder();

        // Validate ID (must be exactly 9 digits)
        if (!IsraeliIdConverter.IsValid(CurrentCourier.Id))
            errors.AppendLine($"• {IsraeliIdConverter.GetErrorMessage()}");

        // Validate Name
        if (string.IsNullOrWhiteSpace(CurrentCourier.Name))
            errors.AppendLine("• Name is required.");

        // Validate Phone
        if (!PhoneNumberConverter.IsValid(CurrentCourier.Phone))
            errors.AppendLine($"• {PhoneNumberConverter.GetErrorMessage()}");

        // Validate Email
        if (!EmailConverter.IsValid(CurrentCourier.Email))
            errors.AppendLine($"• {EmailConverter.GetErrorMessage()}");

        // Validate Password strength ONLY in UPDATE mode
        if (!_isNewCourier && !PasswordHelper.IsPasswordStrong(CurrentPassword))
        {
            errors.AppendLine("• Password must be at least 8 characters " +
                              "with uppercase, lowercase, digit, and special character.");
        }

        // Validate MaxDeliveryDistance (if provided)
        if (CurrentCourier.MaxDeliveryDistance.HasValue && 
            !MaxDistanceConverter.IsValid(CurrentCourier.MaxDeliveryDistance))
            errors.AppendLine($"• {MaxDistanceConverter.GetErrorMessage()}");

        // Cannot deactivate with active order
        if (!CurrentCourier.IsActive && CurrentOrder != null)
            errors.AppendLine("• Cannot deactivate courier with an active order.");

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
                // Generate password if empty
                CurrentCourier.Password = PasswordHelper.GenerateStrongPassword();

                s_bl.Couriers.Create(CurrentCourier);

                // Show confirmation dialog with generated password
                MessageBox.Show(
                    $"Courier '{CurrentCourier.Name}' (ID: {CurrentCourier.Id}) added successfully!\n\n" +
                    $"Password: {CurrentCourier.Password}\n\n" +
                    $"Please save this password securely.",
                    "Courier Added",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                CurrentCourier.Password = CurrentPassword;
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

    private void btnDeliveryHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_courierId > 0)
            {
                new PL.Delivery.DeliveryHistoryWindow(_courierId).Show();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening delivery history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnSelectOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_courierId > 0)
            {
                new PL.Delivery.AvailableOrdersWindow(_courierId).Show();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening available orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                updated.AverageDeliveryTime = s_bl.Couriers.CalculateAverageDeliveryTime(CurrentCourier.Id);
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentCourier = updated;
                    CurrentPassword = updated.Password;
                    _originalCourier = CloneCourier(updated);
                    LoadCurrentOrder();
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
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Order observer error: {ex.Message}");
        }
    }

    #endregion

    // Method to set courier mode (called from CourierListWindow when opening for a courier user)
    public void SetCourierMode(bool isCourierMode)
    {
        _isCourierMode = isCourierMode;
        ActionButtonsVisibility = isCourierMode ? Visibility.Visible : Visibility.Collapsed;
        UpdatePromoteStatusButton(); // Update button visibility when courier mode is set
    }
}
