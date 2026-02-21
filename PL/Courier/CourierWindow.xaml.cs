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
    private bool _isCourierMode = false;

    public bool IsClosed => _isClosed;

    public CourierWindow()
        : this(0) { }

    public CourierWindow(int id = 0, bool isReadOnly = false)
    {
        InitializeComponent();
        _courierId = id;
        _isNewCourier = (id == 0);

        SaveButtonVisibility = isReadOnly ? Visibility.Collapsed : Visibility.Visible;
        ButtonText = _isNewCourier ? "Add" : "Update";
        DeleteVisibility = (id > 0 && !isReadOnly) ? Visibility.Visible : Visibility.Collapsed;
        IsFieldsEnabled = !isReadOnly;

        PasswordFieldVisibility = _isNewCourier ? Visibility.Collapsed : Visibility.Visible;
        ActionButtonsVisibility = Visibility.Collapsed;
        SelectOrderButtonEnabled = true;
        PromoteStatusButtonVisibility = Visibility.Collapsed;
        PromoteStatusButtonText = "";

        LoadCourier();
        LoadCurrentOrder();
        UpdatePasswordButtonVisibility();
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

    public bool IsDeliveryTypeEnabled
    {
        get => (bool)GetValue(IsDeliveryTypeEnabledProperty);
        set => SetValue(IsDeliveryTypeEnabledProperty, value);
    }
    public static readonly DependencyProperty IsDeliveryTypeEnabledProperty =
        DependencyProperty.Register("IsDeliveryTypeEnabled", typeof(bool), typeof(CourierWindow), new PropertyMetadata(true));

    public double SalaryAmount
    {
        get => (double)GetValue(SalaryAmountProperty);
        set => SetValue(SalaryAmountProperty, value);
    }
    public static readonly DependencyProperty SalaryAmountProperty =
        DependencyProperty.Register("SalaryAmount", typeof(double), typeof(CourierWindow), new PropertyMetadata(0.0));

    public int DeliveredCount
    {
        get => (int)GetValue(DeliveredCountProperty);
        set => SetValue(DeliveredCountProperty, value);
    }
    public static readonly DependencyProperty DeliveredCountProperty =
        DependencyProperty.Register("DeliveredCount", typeof(int), typeof(CourierWindow), new PropertyMetadata(0));

    public Visibility SalaryVisibility
    {
        get => (Visibility)GetValue(SalaryVisibilityProperty);
        set => SetValue(SalaryVisibilityProperty, value);
    }
    public static readonly DependencyProperty SalaryVisibilityProperty =
        DependencyProperty.Register("SalaryVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

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

        if (CurrentCourier != null && !_isNewCourier)
        {
            CurrentPassword = "";
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _isClosed = true;
        try { s_bl.Couriers.RemoveObserver(CourierObserver); } catch { /* Observer may already be removed */ }
        try { s_bl.Orders.RemoveObserver(OrderObserver); } catch { /* Observer may already be removed */ }
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
                SalaryVisibility = Visibility.Collapsed;
            }
            else
            {
                CurrentCourier = s_bl.Couriers.Read(_courierId);
                if (CurrentCourier == null)
                {
                    MessageBox.Show($"Courier ID {_courierId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                CurrentCourier.AverageDeliveryTime = s_bl.Couriers.CalculateAverageDeliveryTime(_courierId);
                CurrentPassword = "";

                // ✅ Calculate and display salary
                UpdateSalaryDisplay();

                _originalCourier = CloneCourier(CurrentCourier);
                try { s_bl.Couriers.AddObserver(CourierObserver); } catch { /* Observer registration is best-effort */ }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load courier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    /// <summary>
    /// Updates the salary display (net salary and delivery count).
    /// </summary>
    private void UpdateSalaryDisplay()
    {
        try
        {
            if (_courierId > 0)
            {
                var startOfMonth = new DateTime(s_bl.Admin.GetClock().Year, s_bl.Admin.GetClock().Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
                var salary = s_bl.Couriers.CalculateSalary(_courierId, startOfMonth, endOfMonth);
                
                SalaryAmount = salary.NetSalary;
                DeliveredCount = salary.TotalDeliveries;
                SalaryVisibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            SalaryVisibility = Visibility.Collapsed;
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
                // Get current active order - InProgress means both associated and picked up
                var currentOrder = s_bl.Orders.ReadAll(o => o.CourierId == _courierId)
                    .Where(o => o.OrderStatus == BO.OrderStatus.InProgress)
                    .OrderByDescending(o => o.CourierAssociatedDate)
                    .FirstOrDefault();

                if (currentOrder != null)
                {
                    CurrentOrder = currentOrder;
                    CurrentOrderVisibility = Visibility.Visible;
                    NoCurrentOrderVisibility = Visibility.Collapsed;
                    SelectOrderButtonEnabled = false;
                    IsDeliveryTypeEnabled = false;

                    try { s_bl.Orders.AddObserver(OrderObserver); } catch { /* Observer registration is best-effort */ }
                }
                else
                {
                    CurrentOrder = null;
                    CurrentOrderVisibility = Visibility.Collapsed;
                    NoCurrentOrderVisibility = Visibility.Visible;
                    SelectOrderButtonEnabled = true;
                    IsDeliveryTypeEnabled = true;
                }

                // Update buttons visibility based on courier mode and order state
                UpdatePromoteStatusButton();
            }
        }
        catch (Exception ex)
        {
            CurrentOrder = null;
            CurrentOrderVisibility = Visibility.Collapsed;
            NoCurrentOrderVisibility = Visibility.Visible;
            IsDeliveryTypeEnabled = true;
            PromoteStatusButtonVisibility = Visibility.Collapsed;
        }
    }

    #endregion

    #region Status Promotion

    // Handler for "Customer Refuse"
    private void btnRefuse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentOrder == null || CurrentOrder.Id <= 0) return;

            // UI Interaction only
            var result = MessageBox.Show(
                $"Customer refused Order #{CurrentOrder.Id}.\n\nProceed?",
                "Customer Refused",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            int orderId = CurrentOrder.Id;
            int courierId = _courierId;

            // Delegation to Business Logic
            s_bl.Orders.RefuseOrder(orderId);
            
            MessageBox.Show("Order processed as Refused.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Reload current order after refusal
            LoadCurrentOrder();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Updated Handler for "Delivered" (previously Complete)
    private void btnPromoteStatus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentOrder == null || CurrentOrder.Id <= 0) return;

            var result = MessageBox.Show(
                $"Mark Order #{CurrentOrder.Id} as Delivered?",
                "Confirm Delivery",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int orderId = CurrentOrder.Id;

            // Delegation to BL
            if (!CurrentOrder.PickupDate.HasValue) 
                s_bl.Orders.PickUpOrder(orderId);
            
            s_bl.Orders.DeliverOrder(orderId);
            
            MessageBox.Show("Order delivered successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Reload current order after delivery
            LoadCurrentOrder();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Logic to show/hide the button panel based on state.
    /// Shows buttons ONLY if:
    /// 1. In courier mode (not admin viewing)
    /// 2. There is an active order (InProgress)
    /// </summary>
    private void UpdatePromoteStatusButton()
    {
        // Hide buttons if NOT in courier mode
        if (!_isCourierMode)
        {
            PromoteStatusButtonVisibility = Visibility.Collapsed;
            return;
        }

        // Hide buttons if no current order
        if (CurrentOrder == null || CurrentOrder.Id <= 0)
        {
            PromoteStatusButtonVisibility = Visibility.Collapsed;
            return;
        }

        // Show buttons if order is active (In Progress)
        if (CurrentOrder.OrderStatus == BO.OrderStatus.InProgress)
        {
            PromoteStatusButtonVisibility = Visibility.Visible;
        }
        else
        {
            PromoteStatusButtonVisibility = Visibility.Collapsed;
        }
    }

    #endregion

    #region Validation

    private bool ValidateAndShowErrors()
    {
        if (CurrentCourier == null)
            return false;

        var errors = new StringBuilder();

        if (!IsraeliIdConverter.IsValid(CurrentCourier.Id))
            errors.AppendLine($"• {IsraeliIdConverter.GetErrorMessage()}");

        if (string.IsNullOrWhiteSpace(CurrentCourier.Name))
            errors.AppendLine("• Name is required.");

        if (!PhoneNumberConverter.IsValid(CurrentCourier.Phone))
            errors.AppendLine($"• {PhoneNumberConverter.GetErrorMessage()}");

        if (!EmailConverter.IsValid(CurrentCourier.Email))
            errors.AppendLine($"• {EmailConverter.GetErrorMessage()}");

        if (!_isNewCourier && !string.IsNullOrEmpty(CurrentPassword) && !PasswordHelper.IsPasswordStrong(CurrentPassword))
        {
            errors.AppendLine("• Password must be at least 8 characters " +
                              "with uppercase, lowercase, digit, and special character.");
        }

        if (CurrentCourier.MaxDeliveryDistance.HasValue &&
            !MaxDistanceConverter.IsValid(CurrentCourier.MaxDeliveryDistance))
            errors.AppendLine($"• {MaxDistanceConverter.GetErrorMessage()}");

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

            if (!ValidateAndShowErrors())
            {
                return;
            }

            if (ButtonText == "Add")
            {
                string plainPassword = PasswordHelper.GenerateStrongPassword();
                CurrentCourier.Password = plainPassword;
                s_bl.Couriers.Create(CurrentCourier);

                ShowCopyablePasswordDialog(
                    "Courier Added",
                    $"Courier '{CurrentCourier.Name}' (ID: {CurrentCourier.Id}) added successfully!\nPlease save this password securely.",
                    plainPassword);

                _courierId = CurrentCourier.Id;
                _isNewCourier = false;
                ButtonText = "Update";
                DeleteVisibility = Visibility.Visible;           // Show delete button
                PasswordFieldVisibility = Visibility.Visible;    // Show the password field
                CurrentPassword = "";
                courierIdTextBox.IsReadOnly = true;
                _originalCourier = CloneCourier(CurrentCourier);
                UpdatePasswordButtonVisibility();

                UpdateSalaryDisplay();

                try
                { s_bl.Couriers.AddObserver(CourierObserver); }
                catch { /* Observer registration is best-effort */ }
            }
            else
            {
                CurrentCourier.Password = CurrentPassword;
                s_bl.Couriers.Update(CurrentCourier);
                MessageBox.Show("Courier updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                UpdateSalaryDisplay();
            }
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
                    CurrentPassword = "";
                    _originalCourier = CloneCourier(updated);
                    LoadCurrentOrder();
                    UpdateSalaryDisplay();
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
        }
    }

    #endregion

    public void SetCourierMode(bool isCourierMode)
    {
        _isCourierMode = isCourierMode;
        ActionButtonsVisibility = isCourierMode ? Visibility.Visible : Visibility.Collapsed;
        UpdatePasswordButtonVisibility();
        UpdatePromoteStatusButton();
    }

    #region Password Management

    public Visibility ResetPasswordVisibility
    {
        get => (Visibility)GetValue(ResetPasswordVisibilityProperty);
        set => SetValue(ResetPasswordVisibilityProperty, value);
    }
    public static readonly DependencyProperty ResetPasswordVisibilityProperty =
        DependencyProperty.Register("ResetPasswordVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

    public Visibility ChangePasswordVisibility
    {
        get => (Visibility)GetValue(ChangePasswordVisibilityProperty);
        set => SetValue(ChangePasswordVisibilityProperty, value);
    }
    public static readonly DependencyProperty ChangePasswordVisibilityProperty =
        DependencyProperty.Register("ChangePasswordVisibility", typeof(Visibility), typeof(CourierWindow), new PropertyMetadata(Visibility.Collapsed));

    private void UpdatePasswordButtonVisibility()
    {
        ResetPasswordVisibility = (!_isCourierMode && !_isNewCourier) ? Visibility.Visible : Visibility.Collapsed;
        ChangePasswordVisibility = _isCourierMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void btnResetPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentCourier == null || CurrentCourier.Id <= 0) return;

            var result = MessageBox.Show(
                $"Reset password for courier '{CurrentCourier.Name}'?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            string newPlainPassword = PasswordHelper.GenerateStrongPassword();
            CurrentCourier.Password = newPlainPassword;
            s_bl.Couriers.Update(CurrentCourier);
            CurrentPassword = "";

            ShowCopyablePasswordDialog(
                "Password Reset",
                $"Password for '{CurrentCourier.Name}' has been reset.\nPlease save this password securely.",
                newPlainPassword);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to reset password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentCourier == null || CurrentCourier.Id <= 0) return;

            var dialog = new Window
            {
                Title = "Change Password",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(15) };

            stack.Children.Add(new TextBlock { Text = "Current Password:", Margin = new Thickness(0, 0, 0, 3) });
            var txtCurrent = new PasswordBox { Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(txtCurrent);

            stack.Children.Add(new TextBlock { Text = "New Password:", Margin = new Thickness(0, 0, 0, 3) });
            var txtNew = new PasswordBox { Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(txtNew);

            stack.Children.Add(new TextBlock { Text = "Confirm New Password:", Margin = new Thickness(0, 0, 0, 3) });
            var txtConfirm = new PasswordBox { Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(txtConfirm);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "Change", Width = 80, Margin = new Thickness(0, 0, 5, 0) };
            var btnCancelDlg = new Button { Content = "Cancel", Width = 80 };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancelDlg);
            stack.Children.Add(btnPanel);

            btnCancelDlg.Click += (s, args) => dialog.DialogResult = false;
            btnOk.Click += (s, args) =>
            {
                var courier = s_bl.Couriers.Read(CurrentCourier.Id);
                if (!PasswordHelper.VerifyPassword(txtCurrent.Password, courier.Password))
                {
                    MessageBox.Show("Current password is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtNew.Password != txtConfirm.Password)
                {
                    MessageBox.Show("New passwords do not match.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!PasswordHelper.IsPasswordStrong(txtNew.Password))
                {
                    MessageBox.Show("Password must be at least 8 characters with uppercase, lowercase, digit, and special character.",
                        "Weak Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dialog.DialogResult = true;
            };

            dialog.Content = stack;

            if (dialog.ShowDialog() == true)
            {
                CurrentCourier.Password = txtNew.Password;
                s_bl.Couriers.Update(CurrentCourier);
                CurrentPassword = "";
                MessageBox.Show("Password changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to change password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void ShowCopyablePasswordDialog(string title, string message, string password)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize
        };

        var stack = new StackPanel { Margin = new Thickness(15) };

        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var passwordPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        var txtPassword = new TextBox
        {
            Text = password,
            IsReadOnly = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 14,
            Width = 250,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        var btnCopy = new Button { Content = "Copy", Width = 60 };
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(password);
            btnCopy.Content = "Copied!";
        };
        passwordPanel.Children.Add(txtPassword);
        passwordPanel.Children.Add(btnCopy);
        stack.Children.Add(passwordPanel);

        var btnClose = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        btnClose.Click += (s, e) => dialog.Close();
        stack.Children.Add(btnClose);

        dialog.Content = stack;
        dialog.ShowDialog();
    }

    #endregion
}
