namespace BO;

public class CourierInList
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; }
    public DeliveryType DeliveryType{ get; init; }
    public DateTime StartWorkingDate { get; init; }
    public int DeliveredOnTime { get; init; }
    public int DeliveredLate { get; init; }
    public int? CurrentIdOrder { get; init; }
}
