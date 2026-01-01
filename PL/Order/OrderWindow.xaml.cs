using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
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

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateComboBoxes();
            LoadOrder();
            // LoadDeliveryHistory is now called inside LoadOrder after CurrentOrder is set
            UpdateCourierInfoVisibility();
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

                    // No delivery history for new orders
                    DeliveryHistoryVisibility = Visibility.Collapsed;
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

                    // Load delivery history after CurrentOrder is set
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

                // Always show the delivery history section for existing orders
                DeliveryHistoryVisibility = Visibility.Visible;

                // Get delivery history from CurrentOrder (loaded by BL)
                var allHistory = CurrentOrder.DeliveryHistory ?? new List<BO.DeliveryPerOrderInList>();
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"[OrderWindow] Order {CurrentOrder.Id} has {allHistory.Count} delivery history records");

                // Take the most recent items
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
                    System.Diagnostics.Debug.WriteLine($"[OrderWindow] Showing {history.Count} delivery history items");
                }
                else
                {
                    NoDeliveryHistoryVisibility = Visibility.Visible;
                    HasDeliveryHistoryVisibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine($"[OrderWindow] No delivery history to show");
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

        #endregion

        #region Actions

        private void btnActionOrSave_Click(object sender, RoutedEventArgs e)
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

                if (_isAddMode)
                {
                    s_bl.Orders.Create(CurrentOrder);
                    MessageBox.Show("Order created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (CurrentOrder.OrderStatus != OrderStatus.Open)
                    {
                        MessageBox.Show($"Cannot update order in {CurrentOrder.OrderStatus} status.", "Operation Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    s_bl.Orders.Update(CurrentOrder);
                    MessageBox.Show("Order updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Close();
            }
            catch (BLAlreadyExistsException)
            {
                MessageBox.Show($"Order ID {CurrentOrder?.Id} already exists.", "Duplicate Order", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (BLException ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
