using BlApi;
using BO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PL.Delivery;

public partial class AvailableOrdersWindow : Window
{
    private static readonly IBI s_bl = BL.Factory.Get();
    private int _courierId = 0;

    public AvailableOrdersWindow(int courierId = 0)
    {
        InitializeComponent();
        _courierId = courierId;
    }

    #region Dependency Properties

    public IEnumerable<BO.Order>? AvailableOrders
    {
        get => (IEnumerable<BO.Order>?)GetValue(AvailableOrdersProperty);
        set => SetValue(AvailableOrdersProperty, value);
    }

    public static readonly DependencyProperty AvailableOrdersProperty =
        DependencyProperty.Register("AvailableOrders", typeof(IEnumerable<BO.Order>), typeof(AvailableOrdersWindow), new PropertyMetadata(null));

    #endregion

    #region Lifecycle

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        tbTitle.Text = $"Available Orders - Courier ID: {_courierId}";
        LoadAvailableOrders();
        try
        { s_bl.Orders.AddObserver(OrderObserver); }
        catch { /* ignore */ }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        try
        { s_bl.Orders.RemoveObserver(OrderObserver); }
        catch { /* ignore */ }
    }

    #endregion

    #region Loading

    private void LoadAvailableOrders()
    {
        try
        {
            if (_courierId > 0)
            {
                var availableOrders = s_bl.Orders.GetAvailableOrdersForCourier(_courierId);
                AvailableOrders = availableOrders.ToList();
                tbEmptyState.Visibility = availableOrders.Any() ? Visibility.Collapsed : Visibility.Visible;
                tbOrderCount.Text = $"Total Orders: {availableOrders.Count()}";
            }
            else
            {
                AvailableOrders = new List<BO.Order>();
                tbEmptyState.Visibility = Visibility.Visible;
                tbOrderCount.Text = "Total Orders: 0";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading available orders: {ex.Message}");
            AvailableOrders = new List<BO.Order>();
            tbEmptyState.Visibility = Visibility.Visible;
        }
    }

    #endregion

    #region Actions

    private void btnTakeOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.DataContext is BO.Order order)
            {
                if (_courierId <= 0)
                {
                    MessageBox.Show("Courier is not properly loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Step 1: Check if courier is active
                var courier = s_bl.Couriers.Read(_courierId);
                if (courier == null)
                {
                    MessageBox.Show("Courier not found in the system.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!courier.IsActive)
                {
                    MessageBox.Show("This courier account is inactive and cannot accept orders.", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Step 2: Check if courier is already associated with another active order
                var courierCurrentOrder = s_bl.Orders.ReadAll(o => o.CourierId == _courierId)
                    .Where(o => o.OrderStatus == OrderStatus.InProgress)
                    .FirstOrDefault();

                if (courierCurrentOrder != null)
                {
                    MessageBox.Show(
                        $"Courier is already associated with order #{courierCurrentOrder.Id}. " +
                        $"Complete or cancel that order first.",
                        "Operation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Step 3: Associate the order to this courier
                // (BL layer handles all validations and updates, including observer notifications)
                s_bl.Orders.AssociateCourierToOrder(order.Id, _courierId);

                MessageBox.Show(
                    $"Order #{order.Id} assigned successfully!",
                    "Order Assigned",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Step 4: Observers will automatically notify all affected windows
                // - OrderObserver will refresh the available orders list in this window
                // - CourierListWindow will be notified via CourierObserver to update courier status
                // - CourierWindow will be notified via CourierObserver to show the current order
                // (See BL AssociateCourierToOrder method for observer notifications)

                // Step 5: Close the window
                Close();
            }
        }
        catch (BO.BLException ex)
        {
            MessageBox.Show($"Cannot assign order: {ex.Message}", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error assigning order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Observers

    private void OrderObserver()
    {
        try
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadAvailableOrders();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Order observer error: {ex.Message}");
        }
    }

    #endregion
}
