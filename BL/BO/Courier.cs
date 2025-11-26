using DO;

namespace BO;

/// <summary>
/// Represents a courier with personal and operational details.
/// </summary>
/// <remarks>
/// This record stores information about a courier in the delivery system,
/// including their contact details, operational status, and delivery capabilities.
/// The courier's ID is their national ID number.
/// </remarks>
/// <param name="Id">National ID number - unique identifier with valid check digit</param>
/// <param name="Name">Full name of the courier (first and last name)</param>
/// <param name="Phone">Mobile phone number - exactly 10 digits starting with 0</param>
/// <param name="Email">Valid email address</param>
/// <param name="Password">Courier's password - initially set by manager, can be updated by courier</param>
/// <param name="IsActive">Whether the courier is currently active - inactive couriers retain delivery history but cannot handle new orders</param>
/// <param name="MaxDeliveryDistance">Maximum personal delivery distance in kilometers from company address - null means no distance limitation</param>
/// <param name="DeliveryType">Type of delivery method (Car/Motorcycle/Bicycle/OnFoot) - affects distance and speed calculations</param>
/// <param name="StartWorkingDate">Date and time when the courier started working at the company</param>
/// <param name="DeliveredOnTime">Number of orders delivered on time by the courier</param>
/// <param name="DeliverdLate">number of orders delivered late by the courier</param>
/// <param name="CurrentOrder">The order that the courier is currently handling, if any</param>"
public class Courier
{
    internal CourierStatus Status;
    public required Location Location { get; set; }

    public int Id { get; init; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool IsActive { get; set; }
    public double? MaxDeliveryDistance { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public DateTime StartWorkingDate { get; init; }
    public int DeliveredOnTime { get; set; }
    public int DeliveredLate { get; set; }
    public OrderInProgress? CurrentOrder { get; set; }
    public double TotalWeightInDelivery { get; internal set; }
    public int OrdersInDelivery { get; internal set; }

    public override string ToString()
    {
        return $"""
            ID:                    {Id}
            Name:                  {Name}
            Phone:                 {Phone}
            Email:                 {Email}
            Password:              {Password}
            Is Active:             {(IsActive ? "Yes" : "No")}
            Delivery Type:         {DeliveryType}
            Max Delivery Distance: {(MaxDeliveryDistance.HasValue ? $"{MaxDeliveryDistance} km" : "Unlimited")}
            Start Working Date:    {StartWorkingDate:yyyy-MM-dd HH:mm:ss}
            Delivered On Time:     {DeliveredOnTime}
            Delivered Late:        {DeliveredLate}
            Location:              Lat={Location.Latitude}, Lon={Location.Longitude}
            Current Order:         {(CurrentOrder?.OrderId ?? 0)}
            Total Weight:          {TotalWeightInDelivery} kg
            Orders In Delivery:    {OrdersInDelivery}
            """;
    }
}