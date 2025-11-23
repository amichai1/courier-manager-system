using DalApi;

namespace BlApi;

/// <summary>
/// The main contract interface for the Business Logic (BL) layer.
/// Provides access to all entity-specific managers.
/// </summary>
public interface IBI
{
    // Access properties for all entity-specific managers
    IBICourier Couriers { get; }
    IBIOrder Orders { get; }
    IBIDelivery Deliveries { get; }
    IAdmin Admin { get; }
}
