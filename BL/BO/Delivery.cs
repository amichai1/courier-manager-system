namespace BO;

using Helpers;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a Delivery entity (the actual physical shipment in progress).
/// This entity combines information from the Order and the assigned Courier.
/// </summary>
public class Delivery
{
    // --- Basic Identifiers ---
    public int Id { get; set; } 

    // --- Courier Details (Summary) ---
    public int CourierId { get; set; }
    public string? CourierName { get; set; }
    public VehicleType CourierVehicleType { get; set; }
    public Location? CourierLocation { get; set; }

    // --- Order Details (Summary) ---
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
    public Location? CustomerLocation { get; set; }
    public double Weight { get; set; }

    // --- Status and Dates (Copied from Order) ---
    public OrderStatus Status { get; set; }
    public DateTime? CourierAssociatedDate { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    // --- Calculated / Logical Fields for PL ---
    public double DistanceFromCourierToPickup { get; set; }
    public double DistanceFromPickupToTarget { get; set; } 
    public TimeSpan EstimatedTime { get; set; } 

    // Override ToString
    public override string ToString()
    {
        return $"""
            ── DELIVERY IDENTIFIERS ──
            Delivery ID:             {Id}
            Order ID:                {OrderId}
            Status:                  {Status}
            
            ── COURIER INFORMATION ──
            Courier ID:              {CourierId}
            Courier Name:            {CourierName ?? "[Not assigned]"}
            Vehicle Type:            {CourierVehicleType}
            Courier Location:        {(CourierLocation != null ? $"Lat={CourierLocation.Latitude}, Lon={CourierLocation.Longitude}" : "[Not available]")}
            
            ── CUSTOMER INFORMATION ──
            Customer Name:           {CustomerName ?? "[Unknown]"}
            Customer Location:       {(CustomerLocation != null ? $"Lat={CustomerLocation.Latitude}, Lon={CustomerLocation.Longitude}" : "[Not available]")}
            Package Weight:          {Weight} kg
            
            ── DELIVERY TIMELINE ──
            Associated Date:         {(CourierAssociatedDate.HasValue ? CourierAssociatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not associated]")}
            Pickup Date:             {(PickupDate.HasValue ? PickupDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not picked up]")}
            Delivery Date:           {(DeliveryDate.HasValue ? DeliveryDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not delivered]")}
            
            ── DISTANCE CALCULATIONS ──
            Distance (Courier→Pickup): {DistanceFromCourierToPickup:F2} km
            Distance (Pickup→Target):  {DistanceFromPickupToTarget:F2} km
            Total Distance:            {(DistanceFromCourierToPickup + DistanceFromPickupToTarget):F2} km
            
            ── ESTIMATED TIME ──
            Estimated Completion:    {(EstimatedTime.TotalHours > 0 ? $"{EstimatedTime.TotalHours:F2} hours ({EstimatedTime.Hours}h {EstimatedTime.Minutes}m)" : "[Calculating...]")}
            """;
    }
}
