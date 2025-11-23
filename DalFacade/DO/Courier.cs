namespace DO;

public record Courier
(
    int Id,
    DateTime StartWorkingDate
)
{
    private string? v1;
    private string? v2;
    private string? v3;
    private string? v4;
    private DeliveryType type;
    private double latitude;
    private double longitude;

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
        string v1,
        string v2,
        string v3,
        string v4,
        bool isActive,
        double? maxDeliveryDistance,
        DeliveryType type,
        DateTime startWorkingDate,
        double latitude,
        double longitude)
        : this(id, startWorkingDate)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
        this.v4 = v4;
        IsActive = isActive;
        MaxDeliveryDistance = maxDeliveryDistance;
        this.type = type;
        this.latitude = latitude;
        this.longitude = longitude;
    }
}