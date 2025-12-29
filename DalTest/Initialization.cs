using System.Text;
using DalApi;
using DO;

namespace DalTest;

/// <summary>
/// Static class responsible for initializing the data source with sample data.
/// </summary>
public static class Initialization
{
    private static IDal? s_dal;
    private static readonly Random s_rand = new();

    // Wolt office in Israel - headquarters (Petah Tikva)
    private const double WOLT_LATITUDE = 32.098799;
    private const double WOLT_LONGITUDE = 34.8979087;
    private const string WOLT_ADDRESS = "Yona Green 7, Petah Tikva, Israel";

    // Valid Israeli phone prefixes
    private static readonly string[] validPhonePrefixes = { "050", "051", "052", "053", "054", "055", "058" };

    // Store courier IDs for delivery history assignment
    private static List<int> s_courierIds = new();

    /// <summary>
    /// Main initialization method - resets and populates all data lists.
    /// </summary>
    public static void Do()
    {
        s_dal = DalApi.Factory.Get;

        Console.WriteLine("Resetting configuration values and clearing data lists...");
        s_dal.ResetDB();

        Console.WriteLine("Initializing company configuration...");
        CreateConfig();

        Console.WriteLine("Initializing couriers list...");
        CreateCouriers();

        Console.WriteLine("Initializing orders list...");
        CreateOrders();

        Console.WriteLine("Initializing deliveries list...");
        CreateDeliveries();

        Console.WriteLine("Creating delivery history for couriers...");
        CreateDeliveryHistory();

        Console.WriteLine("Data initialization completed successfully!");
    }

    /// <summary>
    /// Initializes company configuration settings.
    /// </summary>
    private static void CreateConfig()
    {
        s_dal!.Config.Clock = new DateTime(2025, 1, 1, 8, 0, 0);
        s_dal.Config.ManagerId = 123456789;
        s_dal.Config.ManagerPassword = "Admin123!";
        s_dal.Config.CompanyAddress = WOLT_ADDRESS;
        s_dal.Config.CompanyLatitude = WOLT_LATITUDE;
        s_dal.Config.CompanyLongitude = WOLT_LONGITUDE;
        s_dal.Config.MaxDeliveryDistance = 50.0;
        s_dal.Config.CarSpeed = 30.0;
        s_dal.Config.MotorcycleSpeed = 35.0;
        s_dal.Config.BicycleSpeed = 15.0;
        s_dal.Config.OnFootSpeed = 4.0;
        s_dal.Config.MaxDeliveryTime = TimeSpan.FromHours(2);
        s_dal.Config.RiskRange = TimeSpan.FromMinutes(90);
        s_dal.Config.InactivityRange = TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Creates and initializes 22 couriers.
    /// All couriers start from Wolt office in Petah Tikva.
    /// Couriers have realistic max delivery distances (10-50 km).
    /// </summary>
    private static void CreateCouriers()
    {
        s_courierIds.Clear();

        string[] courierNames =
        {
            "David Cohen", "Sarah Levi", "Michael Amir", "Rachel Israeli",
            "Yossi Mizrahi", "Tal Shapira", "Noa Goldstein", "Avi Ben-David",
            "Shira Katz", "Eitan Levy", "Maya Friedman", "Ori Peretz",
            "Gal Sharon", "Lior Ben-Ami", "Dana Rosenberg", "Alon Har-Even",
            "Roni Avraham", "Chen Segal", "Yael Mor", "Amit Cohen",
            "Tamar Israeli", "Noam Klein"
        };

        string[] emailDomains = { "@delivery.com", "@fastship.co.il", "@express.net" };

        DeliveryType[] deliveryTypes =
        {
            DeliveryType.Car,
            DeliveryType.Motorcycle,
            DeliveryType.Bicycle,
            DeliveryType.OnFoot
        };

        // Create exactly 22 couriers
        for (int i = 0; i < courierNames.Length; i++)
        {
            int id = GenerateValidIsraeliId();
            string phone = GeneratePhoneNumber();

            string emailName = courierNames[i].Replace(" ", ".").ToLower();
            string email = emailName + emailDomains[s_rand.Next(emailDomains.Length)];

            string plainPassword = GenerateStrongPassword();

            // All couriers are active
            bool isActive = true;

            // Realistic max distance: 8-15 km
            double? maxDistance = s_rand.Next(8, 16);

            DeliveryType deliveryType = deliveryTypes[i % deliveryTypes.Length];

            DateTime startDate = s_dal!.Config.Clock.AddDays(-s_rand.Next(0, 5));

            Courier courier = new Courier(
                id,
                startDate
            )
            {
                Name = courierNames[i],
                Phone = phone,
                Email = email,
                Password = plainPassword,
                IsActive = isActive,
                MaxDeliveryDistance = maxDistance,
                DeliveryType = deliveryType,
                AddressLatitude = WOLT_LATITUDE,
                AddressLongitude = WOLT_LONGITUDE
            };

            s_dal.Courier.Create(courier);
            Console.WriteLine($"  Created: {courierNames[i]}, ID: {id}, Password: {plainPassword}");
        }
    }

    /// <summary>
    /// Generates a valid Israeli ID number with check digit using Luhn algorithm.
    /// </summary>
    private static int GenerateValidIsraeliId()
    {
        int id;
        bool isValid;

        do
        {
            string idStr = "";
            for (int i = 0; i < 8; i++)
            {
                idStr += s_rand.Next(0, 10);
            }

            // Calculate check digit using Luhn algorithm
            int sum = 0;
            for (int i = 0; i < 8; i++)
            {
                int digit = int.Parse(idStr[i].ToString());
                int multiplier = (i % 2 == 0) ? 1 : 2;
                int result = digit * multiplier;
                sum += (result > 9) ? (result - 9) : result;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            idStr += checkDigit;

            id = int.Parse(idStr);

            // Verify ID doesn't already exist
            isValid = s_dal?.Courier.Read(id) == null;

        } while (!isValid);

        return id;
    }

    /// <summary>
    /// Generates a valid Israeli mobile phone number.
    /// Format: 05X-XXXXXXX (10 digits with prefix 050-058)
    /// </summary>
    private static string GeneratePhoneNumber()
    {
        string prefix = validPhonePrefixes[s_rand.Next(validPhonePrefixes.Length)];
        string rest = s_rand.Next(0, 10000000).ToString("D7");

        // 50% with dash, 50% without
        return s_rand.Next(0, 2) == 0
            ? $"{prefix}-{rest}"
            : $"{prefix}{rest}";
    }

    /// <summary>
    /// Generates a strong password with uppercase, lowercase, digit and special character.
    /// </summary>
    private static string GenerateStrongPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        // Ensure at least one of each required type
        var password = new StringBuilder();
        password.Append(uppercase[s_rand.Next(uppercase.Length)]);
        password.Append(lowercase[s_rand.Next(lowercase.Length)]);
        password.Append(digits[s_rand.Next(digits.Length)]);
        password.Append(special[s_rand.Next(special.Length)]);

        // Add 4-8 more random characters
        string allChars = uppercase + lowercase + digits + special;
        int additionalLength = s_rand.Next(4, 9);
        for (int i = 0; i < additionalLength; i++)
        {
            password.Append(allChars[s_rand.Next(allChars.Length)]);
        }

        return new string(password.ToString().OrderBy(c => s_rand.Next()).ToArray());
    }

    /// <summary>
    /// Creates and initializes 30 order entities.
    /// Order IDs are limited to 1-99 as per validation requirements.
    /// </summary>
    private static void CreateOrders()
    {
        var addressData = new[]
        {
            ("Herzl 45, Tel Aviv, Israel", 32.0853, 34.7818),
            ("Ben Gurion 12, Ramat Gan, Israel", 32.0800, 34.8130),
            ("Rothschild 88, Tel Aviv, Israel", 32.0668, 34.7748),
            ("Dizengoff 120, Tel Aviv, Israel", 32.0827, 34.7753),
            ("Sokolov 22, Ramat Gan, Israel", 32.0891, 34.8237),
            ("Arlozorov 50, Tel Aviv, Israel", 32.0809, 34.7806),
            ("Begin 18, Petah Tikva, Israel", 32.0879, 34.8825),
            ("HaShalom 25, Tel Aviv, Israel", 32.0668, 34.7821),
            ("Allenby 88, Tel Aviv, Israel", 32.0644, 34.7716),
            ("Ibn Gabirol 71, Tel Aviv, Israel", 32.0850, 34.7825),
            ("Weizmann 30, Rehovot, Israel", 31.8969, 34.8186),
            ("Nordau 33, Netanya, Israel", 32.3215, 34.8532),
            ("King George 88, Tel Aviv, Israel", 32.0710, 34.7727),
            ("Frishman 38, Tel Aviv, Israel", 32.0776, 34.7691),
            ("Yehuda HaMaccabi 45, Tel Aviv, Israel", 32.0978, 34.7870),
            ("Moshe Sneh 8, Petah Tikva, Israel", 32.0845, 34.8715),
            ("Simtat Natan 12, Petah Tikva, Israel", 32.0921, 34.8654),
            ("Yitzhak Hanasi 17, Petah Tikva, Israel", 32.0897, 34.8782),
            ("Sirkin 45, Petah Tikva, Israel", 32.0812, 34.8598),
            ("HaShikma 22, Petah Tikva, Israel", 32.0956, 34.8704),
            ("Bialik 115, Ramat Gan, Israel", 32.0856, 34.8142),
            ("Jabotinsky 85, Ramat Gan, Israel", 32.0723, 34.8235),
            ("Henrietta Szold 8, Ramat Gan, Israel", 32.0801, 34.8089),
            ("Arlozorov 11, Ramat Gan, Israel", 32.0889, 34.8195),
            ("Einstein 7, Ramat Gan, Israel", 32.0865, 34.8167),
            ("Rabbi Akiva 88, Bnei Brak, Israel", 32.0850, 34.8384),
            ("Jabotinsky 144, Bnei Brak, Israel", 32.0879, 34.8296),
            ("Rashi 12, Bnei Brak, Israel", 32.0911, 34.8321),
            ("Katznelson 41, Givatayim, Israel", 32.0737, 34.8114),
            ("Weizmann 77, Givatayim, Israel", 32.0685, 34.8156)
        };

        string[] customerNames =
        {
            "Dan Israeli", "Maya Katz", "Ori Levy", "Shani Cohen", "Eyal Mizrahi",
            "Gal Sharabi", "Liora Amar", "Ron Goldberg", "Yael Shapiro", "Amit Ben-Zvi",
            "Tamar Avraham", "Noam Klein", "Shir Malka", "Ido Peretz", "Noa Biton",
            "Uri Dahan", "Hila Golan", "Rotem Azulay", "Adi Berkovich", "Yoni Hadad",
            "Michal Gabay", "Shai Ohayon", "Liat Vaknin", "Elad Mizrachi", "Roni Caspi",
            "Moran Eliyahu", "Itay Ben-David", "Osnat Shavit", "Yuval Saar", "Tal Kaplan"
        };

        OrderType[] orderTypes =
        {
            OrderType.RestaurantFood,
            OrderType.Groceries,
            OrderType.Retail
        };

        // Create exactly 30 orders
        for (int i = 0; i < 30; i++)
        {
            OrderType orderType = orderTypes[s_rand.Next(orderTypes.Length)];
            string? description = $"Order #{i + 1} - {orderType}";
            var (address, lat, lon) = addressData[i % addressData.Length];

            string customerName = customerNames[i];
            string customerPhone = GeneratePhoneNumber();

            double weight;
            double volume;
            bool isFragile;

            switch (orderType)
            {
                case OrderType.RestaurantFood:
                    weight = s_rand.NextDouble() * 3 + 1.0; // 1-4 kg (minimum 1 kg)
                    volume = s_rand.NextDouble() * 0.05 + 1.0; // 1-1.05 m³ (minimum 1)
                    isFragile = s_rand.Next(0, 5) == 0;
                    break;
                case OrderType.Groceries:
                    weight = s_rand.NextDouble() * 15 + 2; // 2-17 kg
                    volume = s_rand.NextDouble() * 0.15 + 1.0; // 1-1.15 m³
                    isFragile = s_rand.Next(0, 4) == 0;
                    break;
                case OrderType.Retail:
                default:
                    weight = s_rand.NextDouble() * 8 + 1.0; // 1-9 kg
                    volume = s_rand.NextDouble() * 0.1 + 1.0; // 1-1.1 m³
                    isFragile = s_rand.Next(0, 3) == 0;
                    break;
            }

            DateTime createdAt = s_dal!.Config.Clock.AddDays(-s_rand.Next(0, 2));

            Order order = new Order(
                Id: 0,
                CreatedAt: createdAt
            )
            {
                OrderType = orderType,
                Description = description,
                Address = address,
                Latitude = lat,
                Longitude = lon,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Weight = weight,
                Volume = volume,
                IsFragile = isFragile,
            };

            s_dal.Order.Create(order);
        }

        Console.WriteLine($"  Created 30 orders");
    }

    /// <summary>
    /// Creates and initializes delivery entities with realistic logic.
    /// </summary>
    private static void CreateDeliveries()
    {
        var allCouriers = s_dal!.Courier.ReadAll().Where(c => c.IsActive).ToList();
        var allOrders = s_dal.Order.ReadAll().OrderBy(o => o.CreatedAt).ToList();

        var courierAvailability = new Dictionary<int, DateTime>();
        var courierDeliveryCount = new Dictionary<int, int>();

        foreach (var courier in allCouriers)
        {
            courierAvailability[courier.Id] = courier.StartWorkingDate;
            courierDeliveryCount[courier.Id] = 0;
        }

        int deliveriesCreated = 0;
        int maxDeliveries = 8;

        for (int orderIdx = 0; orderIdx < allOrders.Count && deliveriesCreated < maxDeliveries; orderIdx++)
        {
            var order = allOrders[orderIdx];

            if (s_dal.Config.CompanyLatitude is null || s_dal.Config.CompanyLongitude is null)
            {
                throw new InvalidOperationException("Company location is not configured.");
            }

            double orderDistance = CalculateAirDistance(
                s_dal.Config.CompanyLatitude.Value,
                s_dal.Config.CompanyLongitude.Value,
                order.Latitude,
                order.Longitude
            );

            var eligibleCouriers = allCouriers
                .Where(c => c.MaxDeliveryDistance == null || orderDistance <= c.MaxDeliveryDistance)
                .Where(c => c.IsActive)
                .ToList();

            if (eligibleCouriers.Count == 0)
            {
                continue;
            }

            var courier = eligibleCouriers.OrderBy(c => courierDeliveryCount[c.Id]).First();
            courierDeliveryCount[courier.Id]++;

            DateTime courierAvailableTime = courierAvailability[courier.Id];
            DateTime startTime = s_dal.Config.Clock.AddMinutes(-s_rand.Next(10, 120));
            startTime = startTime > courierAvailableTime ? startTime : courierAvailableTime;

            // Delivery duration: 15-90 minutes (max 2 hours)
            int durationMinutes = s_rand.Next(15, 91);
            DateTime endTime = startTime.AddMinutes(durationMinutes);

            // Actual distance based on delivery type
            double actualDistance = orderDistance * (courier.DeliveryType switch
            {
                DeliveryType.Car => 1.5,
                DeliveryType.Motorcycle => 1.4,
                DeliveryType.Bicycle => 1.7,
                DeliveryType.OnFoot => 2.0,
                _ => 1.5
            });

            // Create delivery (in progress - Collected but not completed)
            Delivery delivery = new Delivery(
                Id: 0,
                OrderId: order.Id,
                CourierId: courier.Id,
                DeliveryType: courier.DeliveryType,
                StartTime: startTime,
                ActualDistance: actualDistance
            )
            {
                CompletionStatus = null, // In progress - Collected
                EndTime = null           // Still being delivered
            };

            s_dal.Delivery.Create(delivery);

            // ===== UPDATE DO ORDER =====
            // Update the DO order with pickup information
            order.CourierId = courier.Id;
            order.CourierAssociatedDate = startTime;
            order.PickupDate = startTime;
            s_dal.Order.Update(order);

            courierAvailability[courier.Id] = endTime;
            deliveriesCreated++;
        }

        Console.WriteLine($"  Created {deliveriesCreated} deliveries (in progress)");
    }

    /// <summary>
    /// Creates delivery history for 18 couriers with up to 3 completed deliveries each.
    /// This gives a more realistic view of the system with past deliveries.
    /// </summary>
    private static void CreateDeliveryHistory()
    {
        var allCouriers = s_dal!.Courier.ReadAll().ToList();
        var existingOrders = s_dal.Order.ReadAll().ToList();

        // Select 18 couriers to have delivery history
        var couriersWithHistory = allCouriers.Take(18).ToList();
        int historyOrderId = 100; // Start from 100 to avoid conflicts

        var addressData = new[]
        {
            ("Herzl 45, Tel Aviv, Israel", 32.0853, 34.7818),
            ("Ben Gurion 12, Ramat Gan, Israel", 32.0800, 34.8130),
            ("Rothschild 88, Tel Aviv, Israel", 32.0668, 34.7748),
            ("Dizengoff 120, Tel Aviv, Israel", 32.0827, 34.7753),
            ("Sokolov 22, Ramat Gan, Israel", 32.0891, 34.8237)
        };

        string[] customerNames = { "History Customer 1", "History Customer 2", "History Customer 3" };

        int totalHistoryDeliveries = 0;

        foreach (var courier in couriersWithHistory)
        {
            // Each courier gets 1-3 completed deliveries
            int deliveryCount = s_rand.Next(1, 4);

            for (int i = 0; i < deliveryCount; i++)
            {
                // Create a historical order (already delivered)
                var (address, lat, lon) = addressData[s_rand.Next(addressData.Length)];
                DateTime historicalDate = s_dal.Config.Clock.AddDays(-s_rand.Next(7, 60));

                Order historicalOrder = new Order(
                    Id: 0,
                    CreatedAt: historicalDate
                )
                {
                    OrderType = (OrderType)s_rand.Next(0, 3),
                    Description = $"Historical Order - Delivered",
                    Address = address,
                    Latitude = lat,
                    Longitude = lon,
                    CustomerName = customerNames[s_rand.Next(customerNames.Length)],
                    CustomerPhone = GeneratePhoneNumber(),
                    Weight = s_rand.NextDouble() * 5 + 1.0,
                    Volume = s_rand.NextDouble() * 0.5 + 1.0,
                    IsFragile = false,
                    CourierId = courier.Id,
                    CourierAssociatedDate = historicalDate.AddMinutes(10),
                    PickupDate = historicalDate.AddMinutes(20),
                    DeliveryDate = historicalDate.AddMinutes(s_rand.Next(30, 90))
                };

                s_dal.Order.Create(historicalOrder);
                int orderId = historicalOrder.Id;

                // Create completed delivery
                double distance = CalculateAirDistance(WOLT_LATITUDE, WOLT_LONGITUDE, lat, lon);

                Delivery completedDelivery = new Delivery(
                    Id: 0,
                    OrderId: orderId,
                    CourierId: courier.Id,
                    DeliveryType: courier.DeliveryType,
                    StartTime: historicalDate.AddMinutes(20),
                    ActualDistance: distance * 1.5
                )
                {
                    CompletionStatus = DeliveryStatus.Completed,
                    EndTime = historicalDate.AddMinutes(s_rand.Next(30, 90))
                };

                s_dal.Delivery.Create(completedDelivery);
                totalHistoryDeliveries++;
            }
        }

        Console.WriteLine($"  Created {totalHistoryDeliveries} historical deliveries for {couriersWithHistory.Count} couriers");
    }

    /// <summary>
    /// Calculates air distance between two coordinates using Haversine formula.
    /// </summary>
    private static double CalculateAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in kilometers

        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
