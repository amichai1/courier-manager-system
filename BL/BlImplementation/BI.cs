namespace BL.BIImplementation;
using BlApi;
using DalApi;
/// <summary>
/// The main implementation class for the Business Logic layer.
/// Implements IBI and serves as the entry point to all BL services.
/// </summary>
internal class BI : IBI
{
    // --- Instance Properties (Initialized with the Implementation Classes) ---

    public IBIOrder Orders { get; } = new OrderImplementation();
    public IBIDelivery Deliveries { get; } = new DeliveryImplementation();
    public IAdmin Admin { get; } = new AdminImplementation();
    public IBICourier Couriers { get; } = new CourierImplementation();
}
