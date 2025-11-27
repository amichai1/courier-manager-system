namespace DO;

public record Courier
(
    int Id,
    DateTime StartWorkingDate
)
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsActive { get; set; } = false;
    public double? MaxDeliveryDistance { get; set; } = null;
    public double AddressLatitude { get; init; }
    public double AddressLongitude { get; init; }
    public DeliveryType DeliveryType { get; set; } = DeliveryType.OnFoot;
    public Courier() : this(0, default) { }

    public Courier(
        int id,
        string name,
        string phone,
        string email,
        string pasword,
        bool isActive,
        double? maxDeliveryDistance,
        DeliveryType type,
        DateTime startWorkingDate,
        double latitude,
        double longitude)
        : this(id, startWorkingDate)
    {
        this.Name = name;
        this.Phone = phone;
        this.Email = email;
        this.Password = pasword;
        IsActive = isActive;
        MaxDeliveryDistance = maxDeliveryDistance;
        this.DeliveryType = type;
        this.AddressLatitude = latitude;
        this.AddressLongitude = longitude;
    }
}