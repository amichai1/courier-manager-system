using DO;
using DalApi;
using Dal;

namespace DalTest;
internal class Program
{
    private static ICourier? s_dalCourier = new CourierImplementation();
    private static IOrder? s_dalOrder = new OrderImplementation();
    private static IDelivery? s_dalDelivery = new DeliveryImplementation();
    private static IConfig? s_dalConfig = new ConfigImplementation();

    private enum MainMenu { Exit, Couriers, Orders, Deliveries, Config, InitDb, ShowAll, Reset }
    private enum CrudMenu { Exit, Create, Read, ReadAll, Update, Delete, DeleteAll }
    private enum ConfigMenu { Exit, AddMinute, AddHour, ShowClock, SetMaxDistance, ShowAllConfigurations, ResetConfig }

    static void Main()
    {
        try
        {
            RunMainMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Main menu loop
    /// </summary>
    private static void RunMainMenu()
    {
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("Main menu \n");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. Manage couriers");
            Console.WriteLine("2. Manage orders");
            Console.WriteLine("3. Manage deliveries");
            Console.WriteLine("4. Manage config");
            Console.WriteLine("5. Init data base");
            Console.WriteLine("6. Show all data");
            Console.WriteLine("7. Reset");
            
            try
            {
                int choiceNum = ReadInt("Enter your choice: ");
                if (!Enum.IsDefined(typeof(MainMenu), choiceNum))
                {
                    Console.WriteLine("Invalid choice. Try again.");
                    continue;
                }
                var choice = (MainMenu)choiceNum;

                switch (choice)
                {
                    case MainMenu.Exit:
                        exit = true;
                        Console.WriteLine("Goodbye!");
                        break;
                    case MainMenu.Couriers:
                        RunCrudMenu("Courier");
                        break;
                    case MainMenu.Orders:
                        RunCrudMenu("Order");
                        break;
                    case MainMenu.Deliveries:
                        RunCrudMenu("Delivery");
                        break;
                    case MainMenu.Config:
                        RunConfigMenu();
                        break;
                    case MainMenu.InitDb:
                        Initialization.Do(s_dalCourier, s_dalOrder, s_dalDelivery, s_dalConfig);
                        Console.WriteLine("Initialization success!");
                        break;
                    case MainMenu.ShowAll:
                        ShowAllData();
                        break;
                    case MainMenu.Reset:
                        ResetDatabase();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    /// <summary>
    /// CRUD menu for entity management
    /// </summary>
    private static void RunCrudMenu(string entity)
    {
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine($"{entity} Menu:");
            Console.WriteLine("0. Return to main menu");
            Console.WriteLine("1. Create");
            Console.WriteLine("2. Read");
            Console.WriteLine("3. ReadAll");
            Console.WriteLine("4. Update");
            Console.WriteLine("5. Delete");
            Console.WriteLine("6. DeleteAll");
            try
            {
                int choiceNum = ReadInt("Enter your choice: ");
                if (!Enum.IsDefined(typeof(CrudMenu), choiceNum))
                {
                    Console.WriteLine("Invalid choice. Try again.");
                    continue;
                }
                var choice = (CrudMenu)choiceNum;

                switch (choice)
                {
                    case CrudMenu.Exit:
                        exit = true;
                        break;

                    case CrudMenu.Create:
                        if (entity == "Courier")
                        {
                            var courier = ReadCourierFromConsole();
                            s_dalCourier?.Create(courier);
                            Console.WriteLine("Courier created.");
                            Console.WriteLine(s_dalCourier?.Read(courier.Id));

                        }
                        else if (entity == "Order")
                        {
                            var order = ReadOrderFromConsole();
                            s_dalOrder?.Create(order);
                            Console.WriteLine("Order created.");
                            Console.WriteLine(s_dalOrder?.Read(order.Id));
                        }
                        else if (entity == "Delivery")
                        {
                            var delivery = ReadDeliveryFromConsole();
                            s_dalDelivery?.Create(delivery);
                            Console.WriteLine("Delivery created.");
                            Console.WriteLine(s_dalDelivery?.Read(delivery.Id));
                        }
                        break;

                    case CrudMenu.Read:
                        int readId = ReadInt("Enter ID: ");
                        if (entity == "Courier") Console.WriteLine(s_dalCourier?.Read(readId));
                        else if (entity == "Order") Console.WriteLine(s_dalOrder?.Read(readId));
                        else if (entity == "Delivery") Console.WriteLine(s_dalDelivery?.Read(readId));
                        break;

                    case CrudMenu.ReadAll:
                        if (entity == "Courier") foreach (var c in s_dalCourier.ReadAll()) Console.WriteLine(c);
                        else if (entity == "Order") foreach (var o in s_dalOrder.ReadAll()) Console.WriteLine(o);
                        else if (entity == "Delivery") foreach (var d in s_dalDelivery.ReadAll()) Console.WriteLine(d);
                        break;

                    case CrudMenu.Update:
                        int updateId = ReadInt("Enter ID to update: ");
                        if (entity == "Courier") UpdateCourierInteractive(updateId);
                        else if (entity == "Order") UpdateOrderInteractive(updateId);
                        else if (entity == "Delivery") UpdateDeliveryInteractive(updateId);
                        break;

                    case CrudMenu.Delete:
                        int deleteId = ReadInt("Enter ID to delete: ");
                        if (entity == "Courier") s_dalCourier?.Delete(deleteId);
                        else if (entity == "Order") s_dalOrder?.Delete(deleteId);
                        else if (entity == "Delivery") s_dalDelivery?.Delete(deleteId);
                        break;

                    case CrudMenu.DeleteAll:
                        if (entity == "Courier") s_dalCourier?.DeleteAll();
                        else if (entity == "Order") s_dalOrder?.DeleteAll();
                        else if (entity == "Delivery") s_dalDelivery?.DeleteAll();
                        Console.WriteLine($"{entity} list cleared.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {entity} action: {ex.Message}");
            }
        }
    }

    // Config Menu 
    private static void RunConfigMenu()
    {
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("Config Menu\n");
            Console.WriteLine("0. Return");
            Console.WriteLine("1. Advance clock by one minute");
            Console.WriteLine("2. Advance clock by one hour");
            Console.WriteLine("3. Show system clock");
            Console.WriteLine("4. Set a configuration value (MaxDeliveryDistance)");
            Console.WriteLine("5. Show current configuration value (MaxDeliveryDistance)");
            Console.WriteLine("6. Reset configuration");

            try
            {
                int choiceNum = ReadInt("Enter your choice: ");
                if (!Enum.IsDefined(typeof(ConfigMenu), choiceNum))
                {
                    Console.WriteLine("Invalid choice. Try again.");
                    continue;
                }
                var choice = (ConfigMenu)choiceNum;
                switch (choice)
                {
                    case ConfigMenu.Exit:
                        exit = true;
                        break;

                    case ConfigMenu.AddMinute:
                        if(s_dalConfig is not null)
                            s_dalConfig.Clock = s_dalConfig.Clock.AddMinutes(1);
                        Console.WriteLine("Clock advanced by 1 minute.");
                        break;

                    case ConfigMenu.AddHour:
                        if (s_dalConfig is not null)
                            s_dalConfig.Clock = s_dalConfig.Clock.AddHours(1);
                        Console.WriteLine("Clock advanced by 1 hour.");
                        break;

                    case ConfigMenu.ShowClock:
                        Console.WriteLine(s_dalConfig?.Clock);
                        break;

                    case ConfigMenu.SetMaxDistance:
                        {
                            Console.Write("Enter MaxDeliveryDistance (km) or 'null' for no limit: ");
                            string? maxDeliveryDistance = Console.ReadLine();

                            if (!string.IsNullOrWhiteSpace(maxDeliveryDistance) &&
                                maxDeliveryDistance.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                            {
                                if (s_dalConfig is not null)
                                    s_dalConfig.MaxDeliveryDistance = null;
                                Console.WriteLine("MaxDeliveryDistance cleared (no limit).");
                            }
                            else
                            {
                                double value = ReadDouble("MaxDeliveryDistance (km): ", initialInput: maxDeliveryDistance);
                                if (s_dalConfig is not null) s_dalConfig.MaxDeliveryDistance = value;
                                Console.WriteLine("MaxDeliveryDistance updated.");
                            }
                            break;
                        }

                    case ConfigMenu.ShowAllConfigurations:
                        Console.WriteLine("Current Configuration:");
                        Console.WriteLine($"Clock: { s_dalConfig?.Clock }");
                        Console.WriteLine($"Manager id: { s_dalConfig?.ManagerId }");
                        Console.WriteLine($"Manager passowrd is: { s_dalConfig?.ManagerPassword }");
                        Console.WriteLine($"Company address is: { s_dalConfig?.CompanyAddress }"); 
                        Console.WriteLine($"Max delivery distance is: { s_dalConfig?.MaxDeliveryDistance?.ToString() ?? "(no limit)" }");
                        Console.WriteLine($"Risk range is: { s_dalConfig?.RiskRange }");
                        break;

                    case ConfigMenu.ResetConfig:
                        s_dalConfig?.Reset();
                        Console.WriteLine("Configuration reset.");
                        break;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    // Utility Methods
    private static void ShowAllData()
    {
        Console.WriteLine("\nAll courier:\n ");
        foreach (var c in s_dalCourier.ReadAll()) Console.WriteLine(c);

        Console.WriteLine("\nAll order:\n");
        foreach (var o in s_dalOrder.ReadAll()) Console.WriteLine(o);

        Console.WriteLine("\nAll delivery:\n");
        foreach (var d in s_dalDelivery.ReadAll()) Console.WriteLine(d);
    }

    private static void ResetDatabase()
    {
        s_dalCourier?.DeleteAll();
        s_dalOrder?.DeleteAll();
        s_dalDelivery?.DeleteAll();
        s_dalConfig?.Reset();
        Console.WriteLine("DB reset successful.");
    }

    private static Courier ReadCourierFromConsole()
    {
        int id = ReadInt("Id: ");

        Console.Write("Name: ");
        string? name = Console.ReadLine();

        Console.Write("Phone: ");
        string? phone = Console.ReadLine();

        Console.Write("Email: ");
        string? email = Console.ReadLine();

        Console.Write("Password: ");
        string? password = Console.ReadLine();

        bool isActive = ReadBool("Is Active (true/false): ");

        Console.Write("Max Delivery Distance (or leave empty for no limit): ");
        string? maxDistanceInput = Console.ReadLine();
        double? maxDeliveryDistance = null;
        if (!string.IsNullOrWhiteSpace(maxDistanceInput))
            maxDeliveryDistance = ReadDouble("Max Delivery Distance: ", initialInput: maxDistanceInput);
     
        Console.Write("Delivery Type (Car, Motorcycle, Bicycle, OnFoot): ");
        DeliveryType deliveryType = ReadEnum<DeliveryType>("Delivery Type: ", allowEmpty: false);

        DateTime startDate = ReadDateTime("Start date (yyyy-MM-dd HH:mm): ");

        return new Courier
        {
            Id = id,
            Name = name ?? "",
            Phone = phone ?? "",
            Email = email ?? "",
            Password = password ?? "",
            IsActive = isActive,
            MaxDeliveryDistance = maxDeliveryDistance,
            DeliveryType = deliveryType,
            StartWorkingDate = startDate
        };
    }

    private static void UpdateCourierInteractive(int id)
    {
        var existing = s_dalCourier?.Read(id);

        Console.WriteLine("Current courier:");
        Console.WriteLine(existing);
        if (existing == null)
        {
            Console.WriteLine("Error, this id not exist!");
            return;
        }
        Console.Write("New Name (empty = keep): ");
        string? nameInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(nameInput))
            existing.Name = nameInput;

        Console.Write("New Phone (empty = keep): ");
        string? phoneInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(phoneInput))
            existing.Phone = phoneInput;

        Console.Write("New Email (empty = keep): ");
        string? emailInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(emailInput))
            existing.Email = emailInput;

        Console.Write("New Password (empty = keep): ");
        string? passwordInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(passwordInput))
            existing.Password = passwordInput;

        Console.Write("IsActive (true/false, empty = keep): ");
        string? isActiveInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(isActiveInput))
            existing.IsActive = ReadBool("IsActive: ", initialInput: isActiveInput);

        Console.Write("MaxDeliveryDistance (km) [empty = keep, 'null' = clear]: ");
        string? distInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(distInput))
        {
            if (distInput.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                existing.MaxDeliveryDistance = null;
            else
                existing.MaxDeliveryDistance = ReadDouble("MaxDeliveryDistance: ", initialInput: distInput);
        }

        Console.Write("DeliveryType (name/number, empty = keep): ");
        string? typeInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(typeInput))
            existing.DeliveryType = ReadEnum<DeliveryType>("DeliveryType: ", initialInput: typeInput);

        s_dalCourier.Update(existing);
        Console.WriteLine("Courier updated successfully.");
    }

    private static Order ReadOrderFromConsole()
    {
        OrderType orderType = ReadEnum<OrderType>("OrderType (name/number): ", allowEmpty: false);

        Console.Write("Description [empty = null]: ");
        string? desc = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(desc)) desc = null;

        Console.Write("Address: ");
        string? address = Console.ReadLine();

        Console.Write("CustomerName: ");
        string? customerName = Console.ReadLine();

        Console.Write("CustomerPhone: ");
        string? customerPhone = Console.ReadLine();

        double weight = ReadDouble("Weight (kg): ");
        double volume = ReadDouble("Volume (m^3): ");
        bool isFragile = ReadBool("IsFragile (true/false): ");
        DateTime createdAt = ReadDateTime("CreatedAt (yyyy-MM-dd HH:mm): ");

        return new Order
        {
            OrderType = orderType,
            Description = desc,
            Address = address ?? "",
            CustomerName = customerName ?? "",
            CustomerPhone = customerPhone ?? "",
            Weight = weight,
            Volume = volume,
            IsFragile = isFragile,
            CreatedAt = createdAt
        };
    }
    private static void UpdateOrderInteractive(int id)
    {
        var existing = s_dalOrder!.Read(id);

        Console.WriteLine("Current order:");
        Console.WriteLine(existing);
        if (existing == null)
        {
            Console.WriteLine("Error, this id not exist!");
            return;
        }
        Console.Write("OrderType (name/number, empty = keep): ");
        string? typeInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(typeInput))
            existing.OrderType = ReadEnum<OrderType>("OrderType: ", initialInput: typeInput);

        Console.Write("Description [empty = keep, 'null' = clear]: ");
        string? descInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(descInput))
            existing.Description = descInput.Trim().Equals("null", StringComparison.OrdinalIgnoreCase) ? null : descInput;

        Console.Write("Address [empty = keep]: ");
        string? addressInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(addressInput))
            existing.Address = addressInput;

        Console.Write("CustomerName [empty = keep]: ");
        string? customerNameInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(customerNameInput))
            existing.CustomerName = customerNameInput;

        Console.Write("CustomerPhone [empty = keep]: ");
        string? customerPhoneInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(customerPhoneInput))
            existing.CustomerPhone = customerPhoneInput;

        Console.Write("Weight (kg) [empty = keep]: ");
        string? weightInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(weightInput))
            existing.Weight = ReadDouble("Weight: ", initialInput: weightInput);

        Console.Write("Volume (m^3) [empty = keep]: ");
        string? volumeInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(volumeInput))
            existing.Volume = ReadDouble("Volume: ", initialInput: volumeInput);

        Console.Write("IsFragile (true/false) [empty = keep]: ");
        string? fragInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(fragInput))
            existing.IsFragile = ReadBool("IsFragile: ", initialInput: fragInput);

        s_dalOrder.Update(existing);
        Console.WriteLine("Order updated successfully.");
    }
    private static Delivery ReadDeliveryFromConsole()
    {
        int orderId = ReadInt("OrderId: ");
        int courierId = ReadInt("CourierId (0 for dummy): ");
        DeliveryType deliveryType = ReadEnum<DeliveryType>("DeliveryType (name/number): ", allowEmpty: false);
        DateTime startTime = ReadDateTime("StartTime (yyyy-MM-dd HH:mm): ");

        Console.Write("ActualDistance (km) [empty = null]: ");
        string? distStr = Console.ReadLine();
        double? actualDistance = null;
        if (!string.IsNullOrWhiteSpace(distStr))
            actualDistance = ReadDouble("ActualDistance: ", initialInput: distStr);

        Console.Write("CompletionStatus (Completed/Failed/Cancelled) [empty = null]: ");
        string? compStr = Console.ReadLine();
        DeliveryStatus? status = null;
        if (!string.IsNullOrWhiteSpace(compStr))
            status = ReadEnum<DeliveryStatus>("CompletionStatus: ", initialInput: compStr, allowEmpty: false);

        Console.Write("EndTime (yyyy-MM-dd HH:mm) [empty = null]: ");
        string? endStr = Console.ReadLine();
        DateTime? endTime = null;
        if (!string.IsNullOrWhiteSpace(endStr))
            endTime = ReadDateTime("EndTime: ", initialInput: endStr);

        return new Delivery
        {
            OrderId = orderId,
            CourierId = courierId,
            DeliveryType = deliveryType,
            StartTime = startTime,
            ActualDistance = actualDistance,
            CompletionStatus = status,
            EndTime = endTime
        };
    }
    private static void UpdateDeliveryInteractive(int id)
    {
        var existing = s_dalDelivery?.Read(id);

        Console.WriteLine("Current delivery:");
        Console.WriteLine(existing);
        if (existing == null)
        {
            Console.WriteLine("Error, this id not exist!");
            return;
        }
        Console.Write("CompletionStatus (Completed/Failed/Cancelled) [empty = keep, 'null' = clear]: ");
        string? statusInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(statusInput))
        {
            if (statusInput.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                existing.CompletionStatus = null;
            else
                existing.CompletionStatus = ReadEnum<DeliveryStatus>("CompletionStatus: ", initialInput: statusInput);
        }

        Console.Write("EndTime (yyyy-MM-dd HH:mm) [empty = keep, 'null' = clear]: ");
        string? endInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(endInput))
        {
            if (endInput.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                existing.EndTime = null;
            else
                existing.EndTime = ReadDateTime("EndTime: ", initialInput: endInput);
        }

        s_dalDelivery.Update(existing);
        Console.WriteLine("Delivery updated successfully.");
    }

    private static int ReadInt(string prompt, string? initialInput = null)
    {
        while (true)
        {

            if (initialInput is null) Console.Write(prompt);
            string? s = initialInput ?? Console.ReadLine();
            initialInput = null;
            if (int.TryParse(s, out int val)) return val;
            Console.WriteLine("Invalid integer. Try again.");
        }
    }

    private static double ReadDouble(string prompt, string? initialInput = null)
    {
        while (true)
        {
            if (initialInput is null) Console.Write(prompt);
            string? s = initialInput ?? Console.ReadLine();
            initialInput = null;
            if (double.TryParse(s, out double val)) return val;
            Console.WriteLine("Invalid number. Try again.");
        }
    }

    private static bool ReadBool(string prompt, string? initialInput = null)
    {
        while (true)
        {
            if (initialInput is null) Console.Write(prompt);
            string? s = initialInput ?? Console.ReadLine();
            initialInput = null;
            if (bool.TryParse(s, out bool val)) return val;
            Console.WriteLine("Invalid boolean (true/false). Try again.");
        }
    }

    private static DateTime ReadDateTime(string prompt, string? initialInput = null)
    {
        while (true)
        {
            if (initialInput is null) Console.Write(prompt);
            string? s = initialInput ?? Console.ReadLine();
            initialInput = null;
            if (DateTime.TryParse(s, out DateTime dt)) return dt;
            Console.WriteLine("Invalid date/time. Try again (e.g., yyyy-MM-dd HH:mm).");
        }
    }

    private static TEnum ReadEnum<TEnum>(string prompt, string? initialInput = null, bool allowEmpty = false)
        where TEnum : struct, Enum
    {
        while (true)
        {
            if (initialInput is null) Console.Write(prompt);
            string? s = initialInput ?? Console.ReadLine();
            initialInput = null;

            if (allowEmpty && string.IsNullOrWhiteSpace(s))
                throw new FormatException("Empty value not allowed here.");

            // Try by numeric value
            if (int.TryParse(s, out int num) && Enum.IsDefined(typeof(TEnum), num))
                return (TEnum)Enum.ToObject(typeof(TEnum), num);

            // Try by name
            if (Enum.TryParse<TEnum>(s, true, out var val) && Enum.IsDefined(typeof(TEnum), val))
                return val;

            Console.WriteLine("Invalid option. Try again.");
        }
    }
}