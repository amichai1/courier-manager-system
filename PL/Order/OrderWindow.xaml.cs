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
        private BO.Order? _originalOrder; // Store original for rollback

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

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateComboBoxes();
            LoadOrder();
            LoadCouriersList();
            LoadDeliveryHistory();
            UpdateCourierAssignmentVisibility();
        }

        #endregion

        #region Loading & Data Binding

        private void PopulateComboBoxes()
        {
            // LINQ Method Syntax - demonstrates: Cast, Select, ToList with lambda
            cbxOrderType.ItemsSource = Enum.GetValues(typeof(OrderType))
                .Cast<OrderType>()
                .Select(e => e.ToString())
                .ToList();

            cbxOrderStatus.ItemsSource = Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
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
                        OrderStatus = OrderStatus.Confirmed,
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
                }

                _originalOrder = CloneOrder(CurrentOrder);

                cbxOrderType.SelectedItem = CurrentOrder.OrderType.ToString();
                cbxOrderStatus.SelectedItem = CurrentOrder.OrderStatus.ToString();

                if (CurrentOrder.CourierId.HasValue)
                {
                    cbxCourier.SelectedValue = CurrentOrder.CourierId.Value;
                }
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

        private void LoadCouriersList()
        {
            try
            {
                // LINQ Method Syntax - demonstrates: Where, ToList with lambda
                var couriers = s_bl.Couriers.ReadAll()
                    .Where(c => c.IsActive)
                    .ToList();

                cbxCourier.ItemsSource = couriers;

                // Set selected courier after ItemsSource is set
                if (CurrentOrder?.CourierId.HasValue == true)
                {
                    cbxCourier.SelectedValue = CurrentOrder.CourierId.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading couriers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDeliveryHistory()
        {
            try
            {
                if (!_isAddMode && CurrentOrder?.Id > 0)
                {
                    // LINQ Query Syntax - demonstrates: where, orderby, select
                    DeliveryHistory = CurrentOrder.DeliveryHistory?
                        .OrderByDescending(d => d.EndTime)
                        .ToList() ?? new List<BO.DeliveryPerOrderInList>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading delivery history: {ex.Message}");
                DeliveryHistory = new List<BO.DeliveryPerOrderInList>();
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

            // Validate Customer Name
            if (!CustomerNameConverter.IsValid(CurrentOrder.CustomerName))
            {
                errors.AppendLine($"• {CustomerNameConverter.GetErrorMessage()}");
            }

            // Validate Phone
            if (!PhoneNumberConverter.IsValid(CurrentOrder.CustomerPhone))
            {
                errors.AppendLine($"• {PhoneNumberConverter.GetErrorMessage()}");
            }

            // Validate Address
            if (string.IsNullOrWhiteSpace(CurrentOrder.Address))
            {
                errors.AppendLine("• Address is required.");
            }

            // Validate Weight
            if (!WeightConverter.IsValid(CurrentOrder.Weight))
            {
                errors.AppendLine($"• {WeightConverter.GetErrorMessage()}");
            }

            // Validate Volume
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

        private void cbxOrderStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateCourierAssignmentVisibility();
        }

        private void UpdateCourierAssignmentVisibility()
        {
            if (CurrentOrder != null &&
                (CurrentOrder.OrderStatus == OrderStatus.Confirmed ||
                 CurrentOrder.OrderStatus == OrderStatus.AssociatedToCourier))
            {
                courierAssignmentPanel.Visibility = Visibility.Visible;
            }
            else
            {
                courierAssignmentPanel.Visibility = Visibility.Collapsed;
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

                // Update CurrentOrder from ComboBox selections
                if (cbxOrderType.SelectedItem != null && Enum.TryParse<OrderType>(cbxOrderType.SelectedItem.ToString(), out var orderType))
                {
                    CurrentOrder.OrderType = orderType;
                }

                if (cbxOrderStatus.SelectedItem != null && Enum.TryParse<OrderStatus>(cbxOrderStatus.SelectedItem.ToString(), out var orderStatus))
                {
                    CurrentOrder.OrderStatus = orderStatus;
                }

                // Validate all fields
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
                    if (CurrentOrder.OrderStatus != OrderStatus.Confirmed)
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
