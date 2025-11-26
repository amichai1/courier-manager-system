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

            // TO_DO: יש להשלים חישובים מורכבים עבור BO.Order, כגון:
            // ExpectedDeliverdTime = CalculationManager.CalculateETA(doOrder.Id), 
            // OrderStatus = StatusDerivationLogic(doOrder),
            // DeliveryHistory (דורש קריאה ל-DeliveryManager)

            // Simplified calculated fields for compilation:
            OrderStatus = BO.OrderStatus.Confirmed,
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
            // Mapping from DO to BO
            IEnumerable<BO.Order> boOrders = s_dal.Order.ReadAll().Select(ConvertDOToBO);
            return filter != null ? boOrders.Where(filter) : boOrders;
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

            // [2] LOGIC: Update Order status and association date
            DO.Order doOrder = ConvertBOToDO(boOrder);
            DO.Order updatedDoOrder = doOrder with
            {
                // Assuming Order DO has fields for CourierId and CourierAssociatedDate
                // and a method/logic to handle status update.
            };

            // s_dal.Orders.Update(updatedDoOrder);
            // TO_DO: Update Courier status in DAL if necessary
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
            DO.Order doOrder = ConvertBOToDO(boOrder);
            DO.Order updatedDoOrder = doOrder with { PickupDate = AdminManager.Now };
            // s_dal.Orders.Update(updatedDoOrder);
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
            DO.Order doOrder = ConvertBOToDO(boOrder);
            DO.Order updatedDoOrder = doOrder with { PickupDate = AdminManager.Now };
            // s_dal.Orders.Update(updatedDoOrder);
        }
    }

    // ------------------------------------
    // --- 4. PERIODIC UPDATES ---
    // ------------------------------------

    public static void PeriodicOrderUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            // Logic: Check orders that are associated but not picked up and exceed MaxDeliveryTime/RiskRange
            // Example:
            // TimeSpan riskThreshold = AdminManager.GetConfig().RiskRange;
            // var riskyOrders = s_dal.Orders.ReadAll(o => o.Status == Associated && (AdminManager.Now - o.CourierAssociatedDate) > riskThreshold);
            // Log/flag risky orders.
        }
    }
}