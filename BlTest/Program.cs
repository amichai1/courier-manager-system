using BlApi;
using BO;
using DalApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BITest;

/// <summary>
/// Static class for handling console input operations safely.
/// </summary>
internal static class InputHelper
{
    public static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int result))
        {
            return result;
        }
        throw new FormatException("Invalid input format. Must be an integer.");
    }

    public static double ReadDouble(string prompt)
    {
        Console.Write(prompt);
        if (double.TryParse(Console.ReadLine(), out double result))
        {
            return result;
        }
        throw new FormatException("Invalid input format. Must be a number.");
    }

    public static string ReadString(string prompt)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("Input cannot be empty.");
        return input;
    }
}

internal class Program
{
    // Field for accessing the BL layer via the Factory (Singleton)
    private static readonly IBI s_bl = BL.Factory.Get();

    static void Main(string[] args)
    {
        Console.WriteLine("--- BL Test Program Initialized ---");

        try
        {
            // Reset and initialize the database (DO/DAL layer)
            s_bl.Admin.ResetDB();
            s_bl.Admin.InitializeDB();
            MainMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Startup Error: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
        }
    }

    private static void MainMenu()
    {
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n--- Main Menu ---");
            Console.WriteLine("1. Courier Management");
            Console.WriteLine("2. Order Management");
            Console.WriteLine("3. Delivery Management");
            Console.WriteLine("4. Admin / Config Management");
            Console.WriteLine("0. Exit");
            Console.Write("Enter your choice: ");

            if (int.TryParse(Console.ReadLine(), out int choice))
            {
                try
                {
                    switch (choice)
                    {
                        case 1: HandleCourierMenu(); break;
                        case 2: HandleOrderMenu(); break;
                        case 3: HandleDeliveryMenu(); break;
                        case 4: HandleAdminMenu(); break;
                        case 0: exit = true; break;
                        default: Console.WriteLine("Invalid choice."); break;
                    }
                }
                // Catching specific BL exceptions
                catch (BLException ex)
                {
                    Console.WriteLine($"BL Error: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                }
                // Catching format errors from InputHelper
                catch (FormatException ex)
                {
                    Console.WriteLine($"Input Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal Error: {ex.Message}");
                }
            }
        }
    }

    // ====================================================
    // *** COURIER MANAGEMENT IMPLEMENTATION ***
    // ====================================================

    private static BO.Courier GetCourierFromUser(int id)
    {
        return new BO.Courier
        {
            Id = id,
            Name = InputHelper.ReadString("Enter Name: "),
            Phone = InputHelper.ReadString("Enter Phone: "),
            Email = InputHelper.ReadString("Enter Email: "),
            Password = InputHelper.ReadString("Enter Password: "),
            IsActive = true,
            DeliveryType = (DeliveryType)InputHelper.ReadInt("Enter Vehicle Type (0=Car, 1=Motorcycle, 2=Bicycle, 3=Foot): "),
            Location = new BO.Location { Latitude = 32.0, Longitude = 34.8 },
            StartWorkingDate = s_bl.Admin.GetClock()
        };
    }

    private static void HandleCourierMenu()
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine("\n--- Courier Menu ---");
            Console.WriteLine("1. Add Courier (Create)");
            Console.WriteLine("2. View Courier Details (Read)");
            Console.WriteLine("3. View All Couriers (ReadAll)");
            Console.WriteLine("4. Update Courier Info");
            Console.WriteLine("5. Delete Courier");
            Console.WriteLine("0. Back");
            Console.Write("Enter your choice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            try
            {
                int id;
                switch (choice)
                {
                    case 1:
                        id = InputHelper.ReadInt("Enter New Courier ID: ");
                        s_bl.Couriers.Create(GetCourierFromUser(id));
                        Console.WriteLine($"Courier {id} created successfully.");
                        break;
                    case 2:
                        id = InputHelper.ReadInt("Enter Courier ID to Read: ");
                        BO.Courier courier = s_bl.Couriers.Read(id);
                        Console.WriteLine(courier);
                        break;
                    case 3:
                        s_bl.Couriers.ReadAll().ToList().ForEach(c => Console.WriteLine(c));
                        break;
                    case 4:
                        id = InputHelper.ReadInt("Enter Courier ID to Update: ");
                        BO.Courier oldCourier = s_bl.Couriers.Read(id);
                        Console.WriteLine($"Current Details:\n{oldCourier}");

                        // Read new values (empty input means keep current value)
                        Console.Write("Enter New Name (leave blank to keep current): ");
                        string? newName = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(newName)) newName = oldCourier.Name;

                        Console.Write("Enter New Email (leave blank to keep current): ");
                        string? newEmail = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(newEmail)) newEmail = oldCourier.Email;

                        Console.Write("Enter New Phone (leave blank to keep current): ");
                        string? newPhone = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(newPhone)) newPhone = oldCourier.Phone;

                        Console.Write("Enter New Password (leave blank to keep current): ");
                        string? newPassword = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(newPassword)) newPassword = oldCourier.Password;

                        Console.Write("Enter New Vehicle Type (0=Car, 1=Motorcycle, 2=Bicycle, 3=Foot, or leave blank to keep current): ");
                        string? vehicleTypeInput = Console.ReadLine();
                        DeliveryType newDeliveryType = oldCourier.DeliveryType;
                        if (!string.IsNullOrWhiteSpace(vehicleTypeInput) && int.TryParse(vehicleTypeInput, out int vehicleTypeInt))
                        {
                            newDeliveryType = (DeliveryType)vehicleTypeInt;
                        }

                        Console.Write("Enter New Status (0=Available, 1=Inactive, or leave blank to keep current): ");
                        string? statusInput = Console.ReadLine();
                        bool newIsActive = oldCourier.IsActive;
                        if (!string.IsNullOrWhiteSpace(statusInput) && int.TryParse(statusInput, out int statusInt))
                        {
                            newIsActive = (statusInt != 1); // 1 = Inactive, so != 1 means Active
                        }

                        Console.Write("Enter New Max Delivery Distance in KM (or leave blank to keep current): ");
                        string? maxDistanceInput = Console.ReadLine();
                        double? newMaxDistance = oldCourier.MaxDeliveryDistance;
                        if (!string.IsNullOrWhiteSpace(maxDistanceInput) && double.TryParse(maxDistanceInput, out double maxDist))
                        {
                            newMaxDistance = maxDist;
                        }

                        // Create updated courier object
                        BO.Courier updatedCourier = new BO.Courier
                        {
                            Id = oldCourier.Id,
                            Name = newName,
                            Phone = newPhone,
                            Email = newEmail,
                            Password = newPassword,
                            IsActive = newIsActive,
                            DeliveryType = newDeliveryType,
                            Location = oldCourier.Location,
                            StartWorkingDate = oldCourier.StartWorkingDate,
                            MaxDeliveryDistance = newMaxDistance,
                            DeliveredOnTime = oldCourier.DeliveredOnTime,
                            DeliveredLate = oldCourier.DeliveredLate,
                            CurrentOrder = oldCourier.CurrentOrder,
                        };

                        s_bl.Couriers.Update(updatedCourier);
                        Console.WriteLine($"Courier {id} updated successfully.");
                        Console.WriteLine($"Updated Details:\n{s_bl.Couriers.Read(id)}");
                        break;
                    case 5:
                        id = InputHelper.ReadInt("Enter Courier ID to Delete: ");
                        s_bl.Couriers.Delete(id);
                        Console.WriteLine($"Courier {id} deleted successfully.");
                        break;
                    case 0: back = true; break;
                }
            }
            catch (BLException ex)
            {
                Console.WriteLine($"BL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    // ====================================================
    // *** ORDER MANAGEMENT IMPLEMENTATION ***
    // ====================================================

    private static List<BO.OrderItem> GetOrderItemsFromUser()
    {
        List<BO.OrderItem> items = new List<BO.OrderItem>();
        Console.WriteLine("\n--- Enter Order Items ---");

        int itemId = 1;
        bool done = false;
        while (!done)
        {
            try
            {
                Console.WriteLine($"\nItem #{itemId}:");
                int productId = InputHelper.ReadInt("Enter Product ID (e.g., 101): ");
                int quantity = InputHelper.ReadInt("Enter Quantity: ");
                double price = InputHelper.ReadDouble("Enter Price per Unit: ");

                string productName = "Product_" + productId;

                items.Add(new BO.OrderItem
                {
                    Id = itemId,
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = quantity,
                    TotalPrice = price * quantity
                });

                itemId++;
                Console.Write("Add another item? (y/n): ");
                if (Console.ReadLine()?.ToLower() != "y")
                {
                    done = true;
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error reading item details: {ex.Message}. Try again.");
            }
        }
        return items;
    }

    private static BO.Order GetOrderFromUser(int id)
    {
        List<BO.OrderItem> items = GetOrderItemsFromUser();
        double totalWeight = items.Sum(i => i.Quantity * 0.5);

        double latitude = InputHelper.ReadDouble("Enter Latitude (Destination): ");
        double longitude = InputHelper.ReadDouble("Enter Longitude (Destination): ");
        string customerName = InputHelper.ReadString("Enter Customer Name: ");
        string customerPhone = InputHelper.ReadString("Enter Customer Phone: ");
        int orderTypeInt = InputHelper.ReadInt("Enter Order Type (0=Retail, 1=Food, 2=Grocery): ");

        OrderType orderType = (OrderType)orderTypeInt;

        return new BO.Order
        {
            Id = id,
            OrderType = orderType,
            Description = InputHelper.ReadString("Enter Description: "),
            Address = InputHelper.ReadString("Enter Address: "),
            Latitude = latitude,
            Longitude = longitude,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Weight = totalWeight,
            Volume = totalWeight / 10,
            IsFragile = false,
            CreatedAt = s_bl.Admin.GetClock(),
        };
    }

    private static void HandleOrderMenu()
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine("\n--- Order Menu ---");
            Console.WriteLine("1. Create Order");
            Console.WriteLine("2. View Order Details (Read)");
            Console.WriteLine("3. View All Orders (ReadAll)");
            Console.WriteLine("4. Associate Courier to Order");
            Console.WriteLine("5. Pick Up Order");
            Console.WriteLine("6. Deliver Order");
            Console.WriteLine("7. Delete Order");
            Console.WriteLine("0. Back");
            Console.Write("Enter your choice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            try
            {
                int id;
                switch (choice)
                {
                    case 1:
                        id = InputHelper.ReadInt("Enter New Order ID: ");
                        s_bl.Orders.Create(GetOrderFromUser(id));
                        Console.WriteLine($"Order {id} created successfully.");
                        break;
                    case 2:
                        id = InputHelper.ReadInt("Enter Order ID to Read: ");
                        Console.WriteLine(s_bl.Orders.Read(id));
                        break;
                    case 3:
                        s_bl.Orders.ReadAll().ToList().ForEach(o => Console.WriteLine(o));
                        break;
                    case 4:
                        int orderId = InputHelper.ReadInt("Enter Order ID to associate: ");
                        int courierId = InputHelper.ReadInt("Enter Courier ID: ");
                        s_bl.Orders.AssociateCourierToOrder(orderId, courierId);
                        Console.WriteLine($"Order {orderId} successfully associated with Courier {courierId}.");
                        break;
                    case 5:
                        id = InputHelper.ReadInt("Enter Order ID for Pick Up: ");
                        s_bl.Orders.PickUpOrder(id);
                        Console.WriteLine($"Order {id} picked up successfully.");
                        break;
                    case 6:
                        id = InputHelper.ReadInt("Enter Order ID for Delivery: ");
                        s_bl.Orders.DeliverOrder(id);
                        Console.WriteLine($"Order {id} delivered successfully.");
                        break;
                    case 7:
                        id = InputHelper.ReadInt("Enter Order ID to Delete: ");
                        s_bl.Orders.Delete(id);
                        Console.WriteLine($"Order {id} deleted successfully.");
                        break;
                    case 0: back = true; break;
                }
            }
            catch (BLException ex)
            {
                Console.WriteLine($"BL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
            }
        }
    }

    // ====================================================
    // *** DELIVERY MANAGEMENT IMPLEMENTATION ***
    // ====================================================

    private static void HandleDeliveryMenu()
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine("\n--- Delivery / Calculation Menu ---");
            Console.WriteLine("1. View Delivery Details (Read)");
            Console.WriteLine("2. View All Active Deliveries (ReadAll)");
            Console.WriteLine("3. Calculate Estimated Completion Time (ETA)");
            Console.WriteLine("0. Back");
            Console.Write("Enter your choice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            try
            {
                int id;
                switch (choice)
                {
                    case 1:
                        id = InputHelper.ReadInt("Enter Delivery ID to Read (usually Order ID): ");
                        BO.Delivery delivery = s_bl.Deliveries.Read(id);
                        Console.WriteLine(delivery);
                        break;
                    case 2:
                        s_bl.Deliveries.ReadAll().ToList().ForEach(d => Console.WriteLine(d));
                        break;
                    case 3:
                        id = InputHelper.ReadInt("Enter Delivery ID to calculate ETA: ");
                        DateTime estimatedTime = s_bl.Deliveries.CalculateEstimatedCompletionTime(id);

                        Console.WriteLine($"\n--- ETA Calculation for Delivery {id} ---");
                        Console.WriteLine($"Current System Time: {s_bl.Admin.GetClock()}");
                        Console.WriteLine($"Estimated Completion Time (ETA): {estimatedTime}");
                        Console.WriteLine("------------------------------------------");
                        break;
                    case 0: back = true; break;
                }
            }
            catch (BLException ex)
            {
                Console.WriteLine($"BL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
            }
        }
    }

    // ====================================================
    // *** ADMIN MANAGEMENT IMPLEMENTATION ***
    // ====================================================

    private static void ForwardClockMenu()
    {
        Console.WriteLine("Select time unit to advance:");
        Console.WriteLine("1. Minute (+1 Minute)");
        Console.WriteLine("2. Hour (+1 Hour)");
        Console.WriteLine("3. Day (+1 Day)");
        Console.WriteLine("4. Month (+1 Month)");
        Console.WriteLine("5. Year (+1 Year)");
        Console.Write("Enter your choice (1-5): ");

        if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 5)
        {
            BO.TimeUnit unit = (BO.TimeUnit)(choice - 1);

            Console.Write($"Enter number of {unit}s to advance: ");
            if (int.TryParse(Console.ReadLine(), out int amount) && amount > 0)
            {
                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        s_bl.Admin.ForwardClock(unit);
                    }
                    Console.WriteLine($"Clock forwarded by {amount} {unit}s. New time: {s_bl.Admin.GetClock()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error forwarding clock: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid amount value.");
            }
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
    }

    private static void DisplayConfig()
    {
        BO.Config config = s_bl.Admin.GetConfig();
        Console.WriteLine("\n--- Current System Configuration ---");
        Console.WriteLine(config);
    }

    private static void HandleAdminMenu()
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine("\n--- Admin / Config Menu ---");
            Console.WriteLine("1. Get Current Clock");
            Console.WriteLine("2. Forward Clock (Hours)");
            Console.WriteLine("3. View Config Variables");
            Console.WriteLine("4. Reset DB");
            Console.WriteLine("0. Back");
            Console.Write("Enter your choice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            try
            {
                switch (choice)
                {
                    case 1: Console.WriteLine($"Current System Time: {s_bl.Admin.GetClock()}"); break;
                    case 2: ForwardClockMenu(); break;
                    case 3: DisplayConfig(); break;
                    case 4: s_bl.Admin.ResetDB(); Console.WriteLine("Database and Config reset successfully."); break;
                    case 0: back = true; break;
                }
            }
            catch (BLException ex)
            {
                Console.WriteLine($"Admin Logic Error: {ex.Message}");
            }
        }
    }
}
