namespace Dal;
using DalApi;
using DO;
using System;
sealed internal class DalXml : IDal
{
    private static readonly Lazy<DalXml> s_instance = new(() => new DalXml());
    public static IDal Instance => s_instance.Value;
    private DalXml() { }
    public ICourier Courier { get; } = new CourierImplementation();
    public IOrder Order { get; } = new OrderImplementation();
    public IDelivery Delivery { get; } = new DeliveryImplementation();
    public IConfig Config { get; } = new ConfigImplementation();

    public void ResetDB()
    {
        Courier.DeleteAll();
        Order.DeleteAll();
        Delivery.DeleteAll();
        Config.Reset();
    }
}
