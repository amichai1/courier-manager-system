namespace DalApi;

public interface IDal
{
    ICourier Courier { get; }
    IOrder Order { get; }
    IDelivery Delivery { get; }
    void ResetDB();
    IConfig Config { get; }
}
