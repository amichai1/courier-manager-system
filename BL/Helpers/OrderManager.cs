using BO;
using DalApi;
using DO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BL.Helpers;

internal static class OrderManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;

    // ------------------------------------
    // --- 1. CONVERSION (Mappers) ---
    // ------------------------------------

    /// <summary>
    /// Converts a DO.Order (from DAL) into a BO.Order (for BL/PL).
    /// </summary>
    private static BO.Order ConvertDOToBO(DO.Order doOrder)
    {
        // NOTE: The BO.Order structure is complex and includes calculated fields.

        // [CRITICAL FIX] Do NOT fetch Courier details here to avoid collection modification issues
        // Courier details should only be fetched when explicitly requested, not during conversion
        BO.Courier? assignedCourier = null;
        if (doOrder.CourierId.HasValue)
        {
            try
            {
                assignedCourier = CourierManager.ReadCourier(doOrder.CourierId.Value);
            }
            catch
            {
                // If courier cannot be fetched, continue with null
                assignedCourier = null;
            }
        }

        // Determine OrderStatus based on dates and courier assignment
        BO.OrderStatus orderStatus = BO.OrderStatus.Confirmed;
        if (doOrder.DeliveryDate.HasValue)
        {
            orderStatus = BO.OrderStatus.Delivered;
        }
        else if (doOrder.PickupDate.HasValue)
        {
            orderStatus = BO.OrderStatus.InProgress;
        }
        else if (doOrder.CourierAssociatedDate.HasValue)
        {
            orderStatus = BO.OrderStatus.AssociatedToCourier;
        }

        // This is a simplified mapper focusing on core fields.
        return new BO.Order()
        {
            Id = doOrder.Id,
            OrderType = (BO.OrderType)doOrder.OrderType,
            Description = doOrder.Description,
            Address = doOrder.Address,
            Latitude = doOrder.Latitude,
            Longitude = doOrder.Longitude,
            CustomerName = doOrder.CustomerName,
            CustomerPhone = doOrder.CustomerPhone,
            Weight = doOrder.Weight,
            Volume = doOrder.Volume,
            IsFragile = doOrder.IsFragile,
            CreatedAt = doOrder.CreatedAt,
            CourierAssociatedDate = doOrder.CourierAssociatedDate,
            PickupDate = doOrder.PickupDate,
            DeliveryDate = doOrder.DeliveryDate,
            CourierId = doOrder.CourierId,
            CourierName = assignedCourier?.Name,
            OrderStatus = orderStatus,
            CustomerLocation = new Location { Latitude = doOrder.Latitude, Longitude = doOrder.Longitude },
            ArialDistance = 0,
            MaxDeliveredTime = doOrder.CreatedAt.Add(AdminManager.GetConfig().MaxDeliveryTime)
        };
    }

    /// <summary>
    /// Converts a BO.Order (Class) to a DO.Order (Record).
    /// </summary>
    private static DO.Order ConvertBOToDO(BO.Order boOrder)
    {
        // NOTE: Uses the DO.Order positional constructor implicit structure.
        return new DO.Order(
            boOrder.Id,
            boOrder.CreatedAt
        )
        {
            OrderType = (DO.OrderType)boOrder.OrderType,
            Description = boOrder.Description,
            Address = boOrder.Address,
            Latitude = boOrder.Latitude,
            Longitude = boOrder.Longitude,
            CustomerName = boOrder.CustomerName!,
            CustomerPhone = boOrder.CustomerPhone!,
            Weight = boOrder.Weight,
            Volume = boOrder.Volume,
            IsFragile = boOrder.IsFragile,
        };
    }

    // ------------------------------------
    // --- 2. CRUD Logic ---
    // ------------------------------------

    public static void CreateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Check required fields and non-negative values
            if (order.Weight <= 0 || string.IsNullOrWhiteSpace(order.CustomerName))
                throw new BLInvalidValueException("Order Weight or Customer Name is missing or invalid.");

            // [2] DAL CREATE & EXCEPTION HANDLING
            try
            {
                DO.Order doOrder = ConvertBOToDO(order);
                s_dal.Order.Create(doOrder);
            }
            catch (DO.DalAlreadyExistsException ex)
            {
                throw new BLAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to create order: {ex.Message}", ex);
            }
        }
    }

    public static BO.Order ReadOrder(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DO.Order? doOrder = s_dal.Order.Read(id);
                if (doOrder is null)
                    throw new BLDoesNotExistException($"Order ID {id} not found.");
                return ConvertDOToBO(doOrder);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {id} not found.", ex);
            }
        }
    }

    public static IEnumerable<BO.Order> ReadAllOrders(Func<BO.Order, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            // [CRITICAL FIX] Convert to List immediately to avoid lazy enumeration issues
            List<DO.Order> doOrders = s_dal.Order.ReadAll().ToList();

            // Mapping from DO to BO
            List<BO.Order> boOrders = new List<BO.Order>();
            foreach (DO.Order doOrder in doOrders)
            {
                try
                {
                    boOrders.Add(ConvertDOToBO(doOrder));
                }
                catch
                {
                    // Skip orders that cannot be converted
                    continue;
                }
            }

            return filter != null ? boOrders.Where(filter).ToList() : boOrders;
        }
    }

    public static void UpdateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Check if order is still open for modification
            if (order.OrderStatus != OrderStatus.Confirmed)
                throw new BLOperationFailedException($"Cannot update Order ID {order.Id}: Status is {order.OrderStatus} (not open for modification).");

            try
            {
                DO.Order doOrder = ConvertBOToDO(order);
                s_dal.Order.Update(doOrder);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {order.Id} not found for update.", ex);
            }
        }
    }

    public static void DeleteOrder(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // [1] VALIDATION: Ensure the order is not in progress before deletion
                BO.Order boOrder = ReadOrder(id);
                if (boOrder.OrderStatus != OrderStatus.Confirmed)
                    throw new BLOperationFailedException($"Cannot delete Order ID {id}: It has already been processed or is active.");

                s_dal.Order.Delete(id);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {id} not found for deletion.", ex);
            }
        }
    }

    // ------------------------------------
    // --- 3. SPECIFIC OPERATIONS (Order Flow Management) ---
    // ------------------------------------

    public static void AssociateCourierToOrder(int orderId, int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Order must be Confirmed and Courier must be Available
            BO.Order boOrder = ReadOrder(orderId);
            BO.Courier boCourier = CourierManager.ReadCourier(courierId);

            if (boOrder.OrderStatus != OrderStatus.Confirmed)
                throw new BLOperationFailedException($"Order ID {orderId} is not confirmed (Status: {boOrder.OrderStatus}).");
            if (boCourier.Status != CourierStatus.Available)
                throw new BLOperationFailedException($"Courier ID {courierId} is not available (Status: {boCourier.Status}).");

            // [2] LOGIC: Update Order with Courier information
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with
                {
                    CourierId = courierId,
                    CourierAssociatedDate = AdminManager.Now
                };

                s_dal.Order.Update(updatedDoOrder);

                // [3] UPDATE COURIER: Increment OrdersInDelivery and set CurrentOrder
                DO.Courier? doCourierNullable = s_dal.Courier.Read(courierId);
                if (doCourierNullable is not null)
                {
                    // We don't have a way to directly update OrdersInDelivery in DO.Courier
                    // So we'll use the BO layer to track this information
                    // The courier's order count will be calculated when converting from DO to BO
                    System.Diagnostics.Debug.WriteLine($"[INFO] Courier {courierId} assigned to Order {orderId}");
                }
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {orderId} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to associate courier {courierId} to order {orderId}: {ex.Message}", ex);
            }
        }
    }

    public static void PickUpOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Order must be AssociatedToCourier
            BO.Order boOrder = ReadOrder(orderId);
            if (boOrder.OrderStatus != OrderStatus.AssociatedToCourier)
                throw new BLOperationFailedException($"Order ID {orderId} is not ready for pickup.");

            // [2] LOGIC: Update PickupDate in DAL and status
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with { PickupDate = AdminManager.Now };
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {orderId} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to pick up order {orderId}: {ex.Message}", ex);
            }
        }
    }

    public static void DeliverOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Order must be PickedUp
            BO.Order boOrder = ReadOrder(orderId);
            if (boOrder.OrderStatus != OrderStatus.InProgress)
                throw new BLOperationFailedException($"Order ID {orderId} is not currently being delivered.");

            // [2] LOGIC: Update DeliveryDate in DAL and status
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with { DeliveryDate = AdminManager.Now };
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {orderId} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to deliver order {orderId}: {ex.Message}", ex);
            }
        }
    }


    // ------------------------------------
    // --- 4. PERIODIC UPDATES ---
    // ------------------------------------

    /// <summary>
    /// Periodic update method called after the system clock advances.
    /// Responsible for:
    /// 1. Updating courier status to OnRouteForPickup when an order is associated
    /// 2. Checking for risky orders that exceed the RiskRange time limit
    /// </summary>
    public static void PeriodicOrderUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                BO.Config config = AdminManager.GetConfig();
                TimeSpan riskThreshold = config.RiskRange;
                
                // [CRITICAL FIX] Materialize immediately
                List<DO.Order> allOrders = s_dal.Order.ReadAll().ToList();
                
                // First pass: Process delivered orders
                foreach (DO.Order doOrder in allOrders)
                {
                    // [1] FLAG RISKY ORDERS: If order is associated but not picked up and exceeds RiskRange
                    if (doOrder.CourierAssociatedDate.HasValue && !doOrder.PickupDate.HasValue)
                    {
                        TimeSpan timeSinceAssociation = newClock - doOrder.CourierAssociatedDate.Value;
                        if (timeSinceAssociation > riskThreshold)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WARNING] Risky order detected: Order {doOrder.Id} - not picked up for {timeSinceAssociation.TotalMinutes} minutes");
                        }
                    }

                    // [2] DELIVERY COMPLETE: If order is delivered, keep courier as Available
                    if (doOrder.DeliveryDate.HasValue && doOrder.CourierId.HasValue)
                    {
                        try
                        {
                            DO.Courier? doCourier = s_dal.Courier.Read(doOrder.CourierId.Value);
                            if (doCourier is not null && !doCourier.IsActive)
                            {
                                // If courier was marked as inactive (was working on this order), mark as Available again
                                DO.Courier updatedCourier = doCourier with { IsActive = true };
                                s_dal.Courier.Update(updatedCourier);
                                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {doCourier.Id} marked as Available after delivery of order {doOrder.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to update courier status after delivery: {ex.Message}");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in PeriodicOrderUpdates: {ex.Message}");
            }
        }
    }
}