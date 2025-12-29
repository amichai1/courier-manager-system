using System;
using System.Windows;
using BlApi;
using BO;

namespace PL.Delivery
{
    public partial class DeliveryWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();
        private int _deliveryId = 0;

        public DeliveryWindow()
            : this(0) { }

        public DeliveryWindow(int id = 0)
        {
            InitializeComponent();
            _deliveryId = id;
            LoadDelivery();
        }

        #region Dependency Properties

        public BO.Delivery? CurrentDelivery
        {
            get => (BO.Delivery?)GetValue(CurrentDeliveryProperty);
            set => SetValue(CurrentDeliveryProperty, value);
        }

        public static readonly DependencyProperty CurrentDeliveryProperty =
            DependencyProperty.Register("CurrentDelivery", typeof(BO.Delivery), typeof(DeliveryWindow), new PropertyMetadata(null));

        #endregion

        #region Loading

        private void LoadDelivery()
        {
            try
            {
                if (_deliveryId > 0)
                {
                    CurrentDelivery = s_bl.Deliveries.Read(_deliveryId);
                    if (CurrentDelivery == null)
                    {
                        MessageBox.Show($"Delivery ID {_deliveryId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Register observer for real-time updates
                    try { s_bl.Deliveries.AddObserver(_deliveryId, DeliveryObserver); } catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load delivery: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        #endregion

        #region Actions

        private void btnCalculateETA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentDelivery != null && CurrentDelivery.Id > 0)
                {
                    DateTime estimatedTime = s_bl.Deliveries.CalculateEstimatedCompletionTime(CurrentDelivery.Id);
                    MessageBox.Show($"Estimated Completion Time: {estimatedTime:yyyy-MM-dd HH:mm:ss}", 
                                    "ETA Calculation", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating ETA: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Observers

        private void DeliveryObserver()
        {
            try
            {
                if (CurrentDelivery?.Id > 0)
                {
                    var updated = s_bl.Deliveries.Read(CurrentDelivery.Id);
                    Dispatcher.Invoke(() => CurrentDelivery = updated);
                }
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("The delivery has been deleted from the system. The window will close.",
                                    "Entity Deleted",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                    Close();
                });
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { s_bl.Deliveries.RemoveObserver(_deliveryId, DeliveryObserver); } catch { /* ignore */ }
        }

        #endregion
    }
}
