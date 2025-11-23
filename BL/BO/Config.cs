namespace BO;

public class Config
{
    public DateTime Clock { get; set; }
    public int ManagerId { get; set; }
    public string? ManagerPassword { get; set; }
    public string? CompanyAddress { get; set; }
    public double? CompanyLatitude { get; set; }
    public double? CompanyLongitude { get; set; }
    public int MaxRange { get; set; }
    public double CarSpeed { get; set; }
    public double MotorcycleSpeed { get; set; }
    public double BicycleSpeed { get; set; }
    public double OnFootSpeed { get; set; }
    public double? MaxDeliveryDistance { get; set; }
    public TimeSpan MaxDeliveryTime { get; set; }
    public TimeSpan RiskRange { get; set; }
    public TimeSpan InactivityRange { get; set; }
}