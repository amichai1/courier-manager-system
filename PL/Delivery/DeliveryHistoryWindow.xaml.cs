using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BlApi;
using BO;

namespace PL.Delivery
{
    public partial class DeliveryHistoryWindow : Window
    {
        private static readonly IBI s_bl = BL.Factory.Get();
        private int _courierId = 0;
        private List<BO.DeliveryInList> _allDeliveries = new();
        private SortOrder _currentSortOrder = SortOrder.Descending;

        public enum SortOrder { Ascending, Descending }

        public DeliveryHistoryWindow(int courierId = 0)
        {
            InitializeComponent();
            _courierId = courierId;
        }

        #region Dependency Properties

        public IEnumerable<BO.DeliveryInList>? DeliveryHistory
        {
            get => (IEnumerable<BO.DeliveryInList>?)GetValue(DeliveryHistoryProperty);
            set => SetValue(DeliveryHistoryProperty, value);
        }

        public static readonly DependencyProperty DeliveryHistoryProperty =
            DependencyProperty.Register("DeliveryHistory", typeof(IEnumerable<BO.DeliveryInList>), typeof(DeliveryHistoryWindow), new PropertyMetadata(null));

        #endregion

        #region Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbTitle.Text = $"Delivery History - Courier ID: {_courierId}";
            LoadDeliveryHistory();
            UpdateStats();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        #endregion

        #region Loading

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

                    _allDeliveries = deliveries;
                    DeliveryHistory = deliveries;
                    tbEmptyState.Visibility = deliveries.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    DeliveryHistory = new List<BO.DeliveryInList>();
                    tbEmptyState.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading delivery history: {ex.Message}");
                DeliveryHistory = new List<BO.DeliveryInList>();
                tbEmptyState.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Filtering & Sorting

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allDeliveries.AsEnumerable();

                // Status filter
                if (cbxStatusFilter.SelectedItem is ComboBoxItem statusItem)
                {
                    string selectedStatus = statusItem.Content.ToString() ?? "All";
                    if (selectedStatus != "All" && Enum.TryParse<OrderStatus>(selectedStatus, out var statusEnum))
                    {
                        filtered = filtered.Where(d => d.Status == statusEnum.ToString());
                    }
                }

                // Date filter
                if (dpFromDate.SelectedDate.HasValue)
                {
                    var fromDate = dpFromDate.SelectedDate.Value;
                    filtered = filtered.Where(d => d.PickupDate.HasValue && d.PickupDate.Value.Date >= fromDate.Date);
                }

                // Apply current sort order
                if (_currentSortOrder == SortOrder.Ascending)
                {
                    filtered = filtered.OrderBy(d => d.DeliveryDate ?? d.PickupDate ?? DateTime.MinValue);
                }
                else
                {
                    filtered = filtered.OrderByDescending(d => d.DeliveryDate ?? d.PickupDate ?? DateTime.MinValue);
                }

                DeliveryHistory = filtered.ToList();
                UpdateStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying filters: {ex.Message}");
            }
        }

        private void cbxStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        private void dpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        private void btnSortAscending_Click(object sender, RoutedEventArgs e)
        {
            _currentSortOrder = SortOrder.Ascending;
            ApplyFilters();
        }

        private void btnSortDescending_Click(object sender, RoutedEventArgs e)
        {
            _currentSortOrder = SortOrder.Descending;
            ApplyFilters();
        }

        #endregion

        #region Stats & UI

        private void UpdateStats()
        {
            try
            {
                var displayedDeliveries = DeliveryHistory?.ToList() ?? new List<BO.DeliveryInList>();
                tbTotalCount.Text = displayedDeliveries.Count.ToString();

                // Calculate average delivery time
                var completedDeliveries = displayedDeliveries
                    .Where(d => d.PickupDate.HasValue && d.DeliveryDate.HasValue)
                    .ToList();

                if (completedDeliveries.Any())
                {
                    var elapsedTimes = completedDeliveries
                        .Select(d => d.DeliveryDate!.Value - d.PickupDate!.Value)
                        .ToList();

                    TimeSpan totalElapsed = TimeSpan.Zero;
                    foreach (var elapsed in elapsedTimes)
                    {
                        totalElapsed = totalElapsed.Add(elapsed);
                    }

                    TimeSpan averageElapsed = TimeSpan.FromMilliseconds(
                        totalElapsed.TotalMilliseconds / elapsedTimes.Count
                    );

                    tbAverageTime.Text = averageElapsed.ToString(@"hh\:mm");
                }
                else
                {
                    tbAverageTime.Text = "—";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating stats: {ex.Message}");
                tbAverageTime.Text = "—";
            }
        }

        #endregion

        #region Actions

        private void dgDeliveryHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgDeliveryHistory.SelectedItem is BO.DeliveryInList delivery)
            {
                // Could open order details window here if needed
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
