using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using BlApi;
using BO;
using PL.Converters;

namespace PL.Order
{
    public partial class OrderWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();
        private int _orderId = 0;
        private bool _isAddMode = true;
        private BO.Order? _originalOrder;
        private string? _originalAddress;

        private const int MaxDeliveryHistoryItems = 5;

        public OrderWindow()
            : this(0) { }

        public OrderWindow(int id = 0)
        {
            InitializeComponent();
            _orderId = id;
            _isAddMode = id == 0;

            btnActionOrSave.Content = _isAddMode ? "Add" : "Update";
            lblTitle.Text = _isAddMode ? "Create New Order" : "Update Order";
        }

        #region Dependency Properties

        public BO.Order? CurrentOrder
        {
            get => (BO.Order?)GetValue(CurrentOrderProperty);
            set => SetValue(CurrentOrderProperty, value);
        }

        public static readonly DependencyProperty CurrentOrderProperty =
            DependencyProperty.Register("CurrentOrder", typeof(BO.Order), typeof(OrderWindow), new PropertyMetadata(null));

        public IEnumerable<BO.DeliveryPerOrderInList>? DeliveryHistory
        {
            get => (IEnumerable<BO.DeliveryPerOrderInList>?)GetValue(DeliveryHistoryProperty);
            set => SetValue(DeliveryHistoryProperty, value);
        }

        public static readonly DependencyProperty DeliveryHistoryProperty =
            DependencyProperty.Register("DeliveryHistory", typeof(IEnumerable<BO.DeliveryPerOrderInList>), typeof(OrderWindow), new PropertyMetadata(null));

        public int DeliveryHistoryCount
        {
            get => (int)GetValue(DeliveryHistoryCountProperty);
            set => SetValue(DeliveryHistoryCountProperty, value);
        }

        public static readonly DependencyProperty DeliveryHistoryCountProperty =
            DependencyProperty.Register("DeliveryHistoryCount", typeof(int), typeof(OrderWindow), new PropertyMetadata(0));

        public Visibility DeliveryHistoryVisibility
        {
            get => (Visibility)GetValue(DeliveryHistoryVisibilityProperty);
            set => SetValue(DeliveryHistoryVisibilityProperty, value);
        }

        public static readonly DependencyProperty DeliveryHistoryVisibilityProperty =
            DependencyProperty.Register("DeliveryHistoryVisibility", typeof(Visibility), typeof(OrderWindow), new PropertyMetadata(Visibility.Collapsed));

        public Visibility NoDeliveryHistoryVisibility
        {
            get => (Visibility)GetValue(NoDeliveryHistoryVisibilityProperty);
            set => SetValue(NoDeliveryHistoryVisibilityProperty, value);
        }

        public static readonly DependencyProperty NoDeliveryHistoryVisibilityProperty =
            DependencyProperty.Register("NoDeliveryHistoryVisibility", typeof(Visibility), typeof(OrderWindow), new PropertyMetadata(Visibility.Visible));

        public Visibility HasDeliveryHistoryVisibility
        {
            get => (Visibility)GetValue(HasDeliveryHistoryVisibilityProperty);
            set => SetValue(HasDeliveryHistoryVisibilityProperty, value);
        }

        public static readonly DependencyProperty HasDeliveryHistoryVisibilityProperty =
            DependencyProperty.Register("HasDeliveryHistoryVisibility", typeof(Visibility), typeof(OrderWindow), new PropertyMetadata(Visibility.Collapsed));

        public Visibility CourierInfoVisibility
        {
            get => (Visibility)GetValue(CourierInfoVisibilityProperty);
            set => SetValue(CourierInfoVisibilityProperty, value);
        }

        public static readonly DependencyProperty CourierInfoVisibilityProperty =
            DependencyProperty.Register("CourierInfoVisibility", typeof(Visibility), typeof(OrderWindow), new PropertyMetadata(Visibility.Collapsed));

        public Visibility CancelOrderVisibility
        {
            get => (Visibility)GetValue(CancelOrderVisibilityProperty);
            set => SetValue(CancelOrderVisibilityProperty, value);
        }

        public static readonly DependencyProperty CancelOrderVisibilityProperty =
            DependencyProperty.Register("CancelOrderVisibility", typeof(Visibility), typeof(OrderWindow), new PropertyMetadata(Visibility.Collapsed));

        // Stage 7 - Loading indicator
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(OrderWindow), new PropertyMetadata(false));

        public string LoadingMessage
        {
            get => (string)GetValue(LoadingMessageProperty);
            set => SetValue(LoadingMessageProperty, value);
        }

        public static readonly DependencyProperty LoadingMessageProperty =
            DependencyProperty.Register("LoadingMessage", typeof(string), typeof(OrderWindow), new PropertyMetadata("Processing..."));

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateComboBoxes();
            LoadOrder();
            UpdateCourierInfoVisibility();
            UpdateCancelOrderVisibility();

            // Register for order-specific updates (Stage 7 - Observer)
            if (!_isAddMode && _orderId > 0)
            {
                try
                {
                    s_bl.Orders.AddObserver(_orderId, OrderUpdatedObserver);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrderWindow] Failed to register observer: {ex.Message}");
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Unregister from order updates
            if (!_isAddMode && _orderId > 0)
            {
                try
                {
                    s_bl.Orders.RemoveObserver(_orderId, OrderUpdatedObserver);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrderWindow] Failed to unregister observer: {ex.Message}");
                }
            }
        }

        #endregion

        #region Loading & Data Binding

        private void PopulateComboBoxes()
        {
            cbxOrderType.ItemsSource = Enum.GetValues(typeof(OrderType))
                .Cast<OrderType>()
                .Select(e => e.ToString())
                .ToList();
        }

        private void LoadOrder()
        {
            try
            {
                if (_isAddMode)
                {
                    CurrentOrder = new BO.Order
                    {
                        Id = 0,
                        CustomerName = string.Empty,
                        CustomerPhone = string.Empty,
                        Address = string.Empty,
                        Weight = 0,
                        Volume = 0,
                        OrderType = OrderType.Retail,
                        OrderStatus = OrderStatus.Open,
                        ScheduleStatus = ScheduleStatus.OnTime,
                        CreatedAt = s_bl.Admin.GetClock(),
                        ExpectedDeliverdTime = s_bl.Admin.GetClock().AddHours(2),
                        MaxDeliveredTime = s_bl.Admin.GetClock().AddHours(24),
                        IsFragile = false,
                        Description = null,
                        Latitude = 0,
                        Longitude = 0,
                        ArialDistance = 0
                    };

                    DeliveryHistoryVisibility = Visibility.Collapsed;
                    _originalAddress = null;
                }
                else
                {
                    CurrentOrder = s_bl.Orders.Read(_orderId);
                    if (CurrentOrder == null)
                    {
                        MessageBox.Show($"Order ID {_orderId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    _originalAddress = CurrentOrder.Address;
                    LoadDeliveryHistory();
                }

                _originalOrder = CloneOrder(CurrentOrder);
                cbxOrderType.SelectedItem = CurrentOrder.OrderType.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private BO.Order CloneOrder(BO.Order order)
        {
            return new BO.Order
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                Address = order.Address,
                Weight = order.Weight,
                Volume = order.Volume,
                OrderType = order.OrderType,
                OrderStatus = order.OrderStatus,
                ScheduleStatus = order.ScheduleStatus,
                CreatedAt = order.CreatedAt,
                ExpectedDeliverdTime = order.ExpectedDeliverdTime,
                MaxDeliveredTime = order.MaxDeliveredTime,
                IsFragile = order.IsFragile,
                Description = order.Description,
                Latitude = order.Latitude,
                Longitude = order.Longitude,
                ArialDistance = order.ArialDistance,
                CourierId = order.CourierId
            };
        }

        private void LoadDeliveryHistory()
        {
            try
            {
                if (_isAddMode || CurrentOrder == null || CurrentOrder.Id <= 0)
                {
                    DeliveryHistoryVisibility = Visibility.Collapsed;
                    return;
                }

                DeliveryHistoryVisibility = Visibility.Visible;

                var allHistory = CurrentOrder.DeliveryHistory ?? new List<BO.DeliveryPerOrderInList>();

                var history = allHistory
                    .OrderByDescending(d => d.EndTime ?? d.StartTimeDelivery)
                    .Take(MaxDeliveryHistoryItems)
                    .ToList();

                DeliveryHistory = history;
                DeliveryHistoryCount = allHistory.Count;

                if (history.Any())
                {
                    NoDeliveryHistoryVisibility = Visibility.Collapsed;
                    HasDeliveryHistoryVisibility = Visibility.Visible;
                }
                else
                {
                    NoDeliveryHistoryVisibility = Visibility.Visible;
                    HasDeliveryHistoryVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderWindow] Error loading delivery history: {ex.Message}");
                DeliveryHistory = new List<BO.DeliveryPerOrderInList>();
                NoDeliveryHistoryVisibility = Visibility.Visible;
                HasDeliveryHistoryVisibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Validation

        private bool ValidateAndShowErrors()
        {
            if (CurrentOrder == null)
            {
                return false;
            }

            var errors = new StringBuilder();

            if (!CustomerNameConverter.IsValid(CurrentOrder.CustomerName))
            {
                errors.AppendLine($"• {CustomerNameConverter.GetErrorMessage()}");
            }

            if (!PhoneNumberConverter.IsValid(CurrentOrder.CustomerPhone))
            {
                errors.AppendLine($"• {PhoneNumberConverter.GetErrorMessage()}");
            }

            if (string.IsNullOrWhiteSpace(CurrentOrder.Address))
            {
                errors.AppendLine("• Address is required.");
            }

            if (!WeightConverter.IsValid(CurrentOrder.Weight))
            {
                errors.AppendLine($"• {WeightConverter.GetErrorMessage()}");
            }

            if (!VolumeConverter.IsValid(CurrentOrder.Volume))
            {
                errors.AppendLine($"• {VolumeConverter.GetErrorMessage()}");
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

        #endregion

        #region UI Logic

        private void UpdateCourierInfoVisibility()
        {
            if (CurrentOrder != null && CurrentOrder.CourierId.HasValue && !string.IsNullOrEmpty(CurrentOrder.CourierName))
            {
                CourierInfoVisibility = Visibility.Visible;
            }
            else
            {
                CourierInfoVisibility = Visibility.Collapsed;
            }
        }

        private void UpdateCancelOrderVisibility()
        {
            if (!_isAddMode && CurrentOrder != null &&
                CurrentOrder.OrderStatus != OrderStatus.Delivered &&
                CurrentOrder.OrderStatus != OrderStatus.Canceled &&
                CurrentOrder.OrderStatus != OrderStatus.OrderRefused)
            {
                CancelOrderVisibility = Visibility.Visible;
            }
            else
            {
                CancelOrderVisibility = Visibility.Collapsed;
            }
        }

        private string GetGeocodingStatusMessage(int status)
        {
            return status switch
            {
                0 => "✓ Address verified",           // Success
                2 => "⚠️ Network error - using estimated location", // NetworkError
                3 => "❌ Address not found - please verify",        // InvalidAddress
                _ => ""
            };
        }

        #endregion

        #region Actions - Stage 7 Async

        private async void btnActionOrSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentOrder == null)
                {
                    MessageBox.Show("Order data is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cbxOrderType.SelectedItem != null && Enum.TryParse<OrderType>(cbxOrderType.SelectedItem.ToString(), out var orderType))
                {
                    CurrentOrder.OrderType = orderType;
                }

                if (!ValidateAndShowErrors())
                {
                    return;
                }

                // Show loading indicator
                IsLoading = true;
                Mouse.OverrideCursor = Cursors.Wait;
                btnActionOrSave.IsEnabled = false;

                try
                {
                    if (_isAddMode)
                    {
                        BO.Order orderToSend = CloneOrder(CurrentOrder);


                        var (success, error, geocodeStatus) = await s_bl.Orders.CreateOrderAsync(orderToSend);

                        if (!success)
                        {
                            MessageBox.Show($"Failed to create order: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string message = "Order created successfully.";
                        if (geocodeStatus == 2) // NetworkError
                        {
                            message += "\n\n⚠️ Note: Could not verify address due to network issues. Location is estimated.";
                        }
                        else if (geocodeStatus == 3) // InvalidAddress
                        {
                            message += "\n\n⚠️ Note: Address could not be found. Please verify the address is correct.";
                        }

                        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    }
                    else
                    {
                        if (CurrentOrder.OrderStatus != OrderStatus.Open)
                        {
                            MessageBox.Show($"Cannot update order in {CurrentOrder.OrderStatus} status.", "Operation Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }


                        // Use async method with geocoding if address changed
                        BO.Order orderToUpdate = CloneOrder(CurrentOrder);
                        var (success, error, geocodeStatus) = await s_bl.Orders.UpdateOrderAsync(orderToUpdate, _originalAddress);

                        if (!success)
                        {
                            MessageBox.Show($"Failed to update order: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string message = "Order updated successfully.";
                        if (geocodeStatus == 2)
                        {
                            message += "\n\n⚠️ Note: Could not verify new address due to network issues.";
                        }
                        else if (geocodeStatus == 3)
                        {
                            message += "\n\n⚠️ Note: New address could not be found. Please verify.";
                        }

                        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    }
                }
                finally
                {
                    IsLoading = false;
                    Mouse.OverrideCursor = null;
                    btnActionOrSave.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentOrder == null || CurrentOrder.Id <= 0)
                {
                    MessageBox.Show("No order to cancel.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string message = CurrentOrder.CourierId.HasValue
                    ? $"Are you sure you want to cancel Order #{CurrentOrder.Id}?\n\nThe assigned courier will be released and notified by email."
                    : $"Are you sure you want to cancel Order #{CurrentOrder.Id}?";
                
                var result = MessageBox.Show(
                    message,
                    "Confirm Cancel Order",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                s_bl.Orders.CancelOrder(CurrentOrder.Id);

                MessageBox.Show(
                    $"Order #{CurrentOrder.Id} has been canceled successfully.",
                    "Order Canceled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (BLException ex)
            {
                MessageBox.Show($"Cannot cancel order: {ex.Message}", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error canceling order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Observers - Stage 7

        /// <summary>
        /// Called when the order is updated externally (status changes, etc.)
        /// Refreshes the order data and UI accordingly.
        /// </summary>
        private void OrderUpdatedObserver()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_isAddMode && _orderId > 0 && IsLoaded)
                    {
                        try
                        {
                            // Reload the updated order from database
                            CurrentOrder = s_bl.Orders.Read(_orderId);
                            
                            // Refresh delivery history
                            LoadDeliveryHistory();
                            
                            // Update UI state based on new status
                            UpdateCourierInfoVisibility();
                            UpdateCancelOrderVisibility();
                            
                            System.Diagnostics.Debug.WriteLine($"[OrderWindow] Order #{_orderId} updated via observer");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OrderWindow] Error refreshing order: {ex.Message}");
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.DataBind);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderWindow] Observer error: {ex.Message}");
            }
        }

        #endregion
    }
}
