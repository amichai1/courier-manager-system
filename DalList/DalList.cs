namespace Dal;
using DalApi;
using System;
sealed internal class DalList : IDal
{
    private static readonly Lazy<DalList> s_instance = new(() => new DalList());
    public static IDal Instance => s_instance.Value;
    private DalList() { }
    public ICourier Courier { get; } = new CourierImplementation();
    public IOrder Order { get; } = new OrderImplementation();
    public IDelivery Delivery { get; } = new DeliveryImplementation();
    public IConfig Config { get; } = new ConfigImplementation();

    public void ResetDB()
    {
        Delivery.DeleteAll();
        Order.DeleteAll();
        Courier.DeleteAll();
        Config.Reset();
    }
}
