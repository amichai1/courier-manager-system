namespace DO;

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
public record Courier
(
    int Id,
    DateTime StartWorkingDate
)
{
    /// <summary>
    /// Default parameterless constructor for future use in stage 3 of the project.
    /// </summary>
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsActive { get; set; } = false;
    public double? MaxDeliveryDistance { get; set; } = null;
    public DeliveryType DeliveryType { get; set; } = DeliveryType.OnFoot;
    public Courier() :this(0, default) { }
}