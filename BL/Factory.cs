using BL.BIImplementation;
using BlApi;
using DalApi;

namespace BL;
public class Factory : IBI
{
    //Singleton pattern
    private static readonly IBI s_instance = new BIImplementation.BI();
    private Factory() { }
    public static IBI Get() => s_instance;
    public IBICourier Couriers => new CourierImplementation();
    public IBIOrder Orders => new OrderImplementation();
    public IBIDelivery Deliveries => new DeliveryImplementation();
    public IAdmin Admin => new AdminImplementation();
}
