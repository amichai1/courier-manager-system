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
    public override string ToString() => this.ToStringProperty();
}
