namespace DalTest;
using DalApi;
using DO;
using System.Text;


/// <summary>
/// Static class responsible for initializing the data source with sample data.
/// </summary>
public static class Initialization
{
    private static IDal? s_dal;
    private static readonly Random s_rand = new();

    // Valid Israeli phone prefixes
    private static readonly string[] validPhonePrefixes = { "050", "051", "052", "053", "054", "055", "058" };

    /// <summary>
    /// Main initialization method - resets and populates all data lists.
    /// </summary>
    public static void Do(IDal dal)
    {
        // Initialize interface references with null checking
        s_dal = dal ?? throw new NullReferenceException("DAL object cannot be null!");

        // Reset configuration and clear all lists
        Console.WriteLine("Resetting configuration values and clearing data lists...");
        s_dal.ResetDB();

        // Initialize company settings
        Console.WriteLine("Initializing company configuration...");
        CreateConfig();

        // Populate data lists in logical order
        Console.WriteLine("Initializing couriers list...");
        CreateCouriers();

        Console.WriteLine("Initializing orders list...");
        CreateOrders();

        Console.WriteLine("Initializing deliveries list...");
        CreateDeliveries();

        Console.WriteLine("Data initialization completed successfully!");
    }

    /// <summary>
    /// Initializes company configuration settings.
    /// </summary>
    private static void CreateConfig()
    {
        s_dal!.Config.Clock = DateTime.Now;
        s_dal.Config.ManagerId = 123456789;
        s_dal.Config.ManagerPassword = "Admin123!";
        s_dal.Config.CompanyAddress = "Yona Green 7, Petah Tikva, Israel";
        s_dal.Config.CompanyLatitude = 32.098799;
        s_dal.Config.CompanyLongitude = 34.8979087;
        s_dal.Config.MaxDeliveryDistance = 50.0;
        s_dal.Config.CarSpeed = 30.0;
        s_dal.Config.MotorcycleSpeed = 35.0;
        s_dal.Config.BicycleSpeed = 15.0;
        s_dal.Config.OnFootSpeed = 4.0;
        s_dal.Config.MaxDeliveryTime = TimeSpan.FromHours(2);
        s_dal.Config.RiskRange = TimeSpan.FromMinutes(90);
        s_dal.Config.InactivityRange = TimeSpan.FromDays(30);
    }

    private static void CreateCouriers()
    {
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

        // Create at least 20 couriers
        for (int i = 0; i < courierNames.Length; i++)
        {
            int id = GenerateValidIsraeliId();
            string phone = GeneratePhoneNumber();

            string emailName = courierNames[i].Replace(" ", ".").ToLower();
            string email = emailName + emailDomains[s_rand.Next(emailDomains.Length)];

            string plainPassword = GenerateStrongPassword();

            bool isActive = s_rand.Next(0, 10) != 0;

            double maxDeliveryDistance = s_dal?.Config.MaxDeliveryDistance ?? 50.0;
            double? maxDistance = s_rand.Next(0, 10) < 3
                ? null
                : s_rand.Next(5, (int)maxDeliveryDistance + 1);

            DeliveryType deliveryType = deliveryTypes[i % deliveryTypes.Length];

            DateTime startDate = s_dal?.Config.Clock.AddDays(-s_rand.Next(30, 365)) ?? DateTime.Now.AddDays(-15);

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
            };

            s_dal!.Courier.Create(courier);
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
            // Generate 8 random digits
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
    /// Format: 05X-XXXXXXX or 05XXXXXXXXX
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

        // Shuffle the password
        return new string(password.ToString().OrderBy(c => s_rand.Next()).ToArray());
    }

    /// <summary>
    /// Creates and initializes order entities (at least 50).
    /// </summary>
    private static void CreateOrders()
    {
        // Real addresses with manually calculated coordinates
        // Format: (address, lat, lon, airDistance, walkDistance, driveDistance)
        var addressData = new[]
        {
            ("Herzl 45, Tel Aviv, Israel", 32.0853, 34.7818, 3.5, 4.2, 5.8),
            ("Ben Gurion 12, Ramat Gan, Israel", 32.0800, 34.8130, 2.1, 2.8, 3.5),
            ("Rothschild 88, Tel Aviv, Israel", 32.0668, 34.7748, 4.2, 5.1, 6.9),
            ("Dizengoff 120, Tel Aviv, Israel", 32.0827, 34.7753, 3.8, 4.6, 6.2),
            ("Sokolov 22, Ramat Gan, Israel", 32.0891, 34.8237, 1.8, 2.3, 2.9),
            ("Arlozorov 50, Tel Aviv, Israel", 32.0809, 34.7806, 3.2, 3.9, 5.1),
            ("Begin 18, Petah Tikva, Israel", 32.0879, 34.8825, 5.2, 6.8, 8.1),
            ("HaShalom 25, Tel Aviv, Israel", 32.0668, 34.7821, 4.1, 4.9, 6.5),
            ("Allenby 88, Tel Aviv, Israel", 32.0644, 34.7716, 5.1, 6.2, 7.8),
            ("Ibn Gabirol 71, Tel Aviv, Israel", 32.0850, 34.7825, 3.4, 4.1, 5.6),
            ("Weizmann 30, Rehovot, Israel", 31.8969, 34.8186, 18.5, 22.3, 25.1),
            ("Nordau 33, Netanya, Israel", 32.3215, 34.8532, 28.3, 35.2, 39.8),
            ("King George 88, Tel Aviv, Israel", 32.0710, 34.7727, 4.8, 5.7, 7.2),
            ("Frishman 38, Tel Aviv, Israel", 32.0776, 34.7691, 4.6, 5.5, 7.1),
            ("Yehuda HaMaccabi 45, Tel Aviv, Israel", 32.0978, 34.7870, 4.9, 5.9, 7.5),
            ("Moshe Sneh 8, Petah Tikva, Israel", 32.0845, 34.8715, 4.1, 5.2, 6.5),
            ("Simtat Natan 12, Petah Tikva, Israel", 32.0921, 34.8654, 3.2, 4.0, 5.1),
            ("Yitzhak Hanasi 17, Petah Tikva, Israel", 32.0897, 34.8782, 4.8, 6.1, 7.4),
            ("Sirkin 45, Petah Tikva, Israel", 32.0812, 34.8598, 3.5, 4.4, 5.6),
            ("HaShikma 22, Petah Tikva, Israel", 32.0956, 34.8704, 3.8, 4.8, 6.0),
            ("Bialik 115, Ramat Gan, Israel", 32.0856, 34.8142, 2.3, 3.0, 3.8),
            ("Jabotinsky 85, Ramat Gan, Israel", 32.0723, 34.8235, 3.1, 3.9, 4.9),
            ("Henrietta Szold 8, Ramat Gan, Israel", 32.0801, 34.8089, 2.5, 3.2, 4.1),
            ("Arlozorov 11, Ramat Gan, Israel", 32.0889, 34.8195, 2.0, 2.6, 3.3),
            ("Einstein 7, Ramat Gan, Israel", 32.0865, 34.8167, 2.2, 2.8, 3.6),
            ("Rabbi Akiva 88, Bnei Brak, Israel", 32.0850, 34.8384, 1.5, 2.0, 2.6),
            ("Jabotinsky 144, Bnei Brak, Israel", 32.0879, 34.8296, 1.2, 1.6, 2.1),
            ("Rashi 12, Bnei Brak, Israel", 32.0911, 34.8321, 1.4, 1.8, 2.3),
            ("Katznelson 41, Givatayim, Israel", 32.0737, 34.8114, 2.8, 3.5, 4.5),
            ("Weizmann 77, Givatayim, Israel", 32.0685, 34.8156, 3.3, 4.1, 5.2),
            ("Eilat 45, Holon, Israel", 32.0186, 34.7751, 9.8, 12.1, 14.8),
            ("Moshe Dayan 88, Holon, Israel", 32.0234, 34.7698, 9.2, 11.3, 13.9),
            ("Balfour 65, Bat Yam, Israel", 32.0182, 34.7451, 11.5, 14.2, 17.1),
            ("Ben Gurion 128, Bat Yam, Israel", 32.0241, 34.7523, 10.8, 13.3, 16.0),
            ("Medinat HaYehudim 85, Herzliya, Israel", 32.1656, 34.8434, 9.5, 11.8, 13.9),
            ("Hanasi 12, Herzliya Pituach, Israel", 32.1789, 34.8067, 11.2, 13.9, 16.5),
            ("Weizmann 125, Kfar Saba, Israel", 32.1845, 34.9078, 12.8, 15.9, 18.7),
            ("Rothschild 45, Kfar Saba, Israel", 32.1789, 34.9156, 13.5, 16.8, 19.8),
            ("Ahuza 145, Raanana, Israel", 32.1867, 34.8723, 11.9, 14.8, 17.4),
            ("Achuza 88, Raanana, Israel", 32.1911, 34.8645, 12.4, 15.4, 18.1),
            ("Sokolov 77, Hod Hasharon, Israel", 32.1456, 34.8889, 8.2, 10.2, 12.1),
            ("HaNasich 45, Hod Hasharon, Israel", 32.1523, 34.8812, 8.9, 11.1, 13.1),
            ("Maccabim 12, Rosh HaAyin, Israel", 32.0934, 34.9567, 9.8, 12.2, 14.3),
            ("Pinhas Sapir 88, Rosh HaAyin, Israel", 32.0867, 34.9645, 10.5, 13.1, 15.4),
            ("HaAtzmaut 45, Yehud, Israel", 32.0334, 34.8912, 7.2, 8.9, 10.6),
            ("Jabotinsky 77, Yehud, Israel", 32.0389, 34.8867, 6.8, 8.4, 10.0),
            ("Haim Bar Lev 33, Or Yehuda, Israel", 32.0289, 34.8578, 5.8, 7.2, 8.7),
            ("HaYarden 88, Or Yehuda, Israel", 32.0245, 34.8623, 6.3, 7.8, 9.4),
            ("Derech Menachem Begin 132, Tel Aviv, Israel", 32.0589, 34.7897, 5.8, 7.1, 9.2),
            ("Derech Hashalom 53, Tel Aviv, Israel", 32.0647, 34.7868, 4.5, 5.4, 7.0)
        };

        string[] customerNames =
        {
            "Dan Israeli", "Maya Katz", "Ori Levy", "Shani Cohen", "Eyal Mizrahi",
            "Gal Sharabi", "Liora Amar", "Ron Goldberg", "Yael Shapiro", "Amit Ben-Zvi",
            "Tamar Avraham", "Noam Klein", "Shir Malka", "Ido Peretz", "Noa Biton",
            "Uri Dahan", "Hila Golan", "Rotem Azulay", "Adi Berkovich", "Yoni Hadad",
            "Michal Gabay", "Shai Ohayon", "Liat Vaknin", "Elad Mizrachi", "Roni Caspi",
            "Moran Eliyahu", "Itay Ben-David", "Osnat Shavit", "Yuval Saar", "Tal Kaplan",
            "Dana Friedman", "Amir Rosenberg", "Sigal Dror", "Kobi Tzur", "Merav Koren",
            "Eran Navon", "Vered Shmueli", "Doron Levi", "Keren Alon", "Oren Carmi",
            "Hadas Ben-Ami", "Raz Aloni", "Liron Sadeh", "Guy Rabin", "Limor Harari",
            "Yair Shefer", "Ronit Baruch", "Boaz Nagar", "Efrat Manor", "Asaf Porat"
        };

        OrderType[] orderTypes =
        {
            OrderType.RestaurantFood,
            OrderType.Groceries,
            OrderType.Retail
        };

        // Create at least 50 orders
        for (int i = 0; i < Math.Min(addressData.Length, customerNames.Length); i++)
        {
            OrderType orderType = orderTypes[s_rand.Next(orderTypes.Length)];
            string? description = $"Order #{i + 1} - {orderType}";
            var (address, lat, lon, airDist, walkDist, driveDist) = addressData[i];

            string customerName = customerNames[i];
            string customerPhone = GeneratePhoneNumber();

            // Package details based on order type
            double weight;
            double volume;
            bool isFragile;

            switch (orderType)
            {
                case OrderType.RestaurantFood:
                    weight = s_rand.NextDouble() * 3 + 0.5; // 0.5-3.5 kg
                    volume = s_rand.NextDouble() * 0.05 + 0.01; // in voilume 0.01-0.06 
                    isFragile = s_rand.Next(0, 5) == 0; // 20% fragile
                    break;
                case OrderType.Groceries:
                    weight = s_rand.NextDouble() * 15 + 2; // 2-17 kg
                    volume = s_rand.NextDouble() * 0.15 + 0.05; // 0.05-0.2 m³
                    isFragile = s_rand.Next(0, 4) == 0; // 25% fragile
                    break;
                case OrderType.Retail:
                default:
                    weight = s_rand.NextDouble() * 8 + 0.2; // 0.2-8.2 kg
                    volume = s_rand.NextDouble() * 0.1 + 0.01; // 0.01-0.11 m³
                    isFragile = s_rand.Next(0, 3) == 0; // 33% fragile
                    break;
            }

            // Order created in the past (last 45 days)
            DateTime createdAt = s_dal!.Config.Clock.AddDays(-s_rand.Next(0, 45));

            // Create order (ID will be auto-generated)
            Order order = new Order(
                Id: 0, // Will be auto-generated
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
            }
        ;
            s_dal.Order.Create(order);
        }
    }

    /// <summary>
    /// Creates and initializes delivery entities.
    /// At least 20 open orders, 10 in progress, 20 closed.
    /// </summary>
    private static void CreateDeliveries()
    {
        var allCouriers = s_dal!.Courier.ReadAll().Where(c => c.IsActive).ToList();
        var allOrders = s_dal.Order.ReadAll().OrderBy(o => o.CreatedAt).ToList();

        // Track courier availability (when they finish their last delivery)
        var courierAvailability = allCouriers.ToDictionary(c => c.Id, c => c.StartWorkingDate);

        // Targets
        int targetOpenOrders = 20;
        int targetInProgress = 10;
        int targetClosed = 20;
        int openOrdersCreated = 0;
        int inProgressCreated = 0;
        int closedCreated = 0;

        var availableOrders = new List<Order>(allOrders);

        // Create deliveries
        while (availableOrders.Count > 0 &&
               (inProgressCreated < targetInProgress || closedCreated < targetClosed))
        {
            var order = availableOrders[0];
            availableOrders.RemoveAt(0);

            // Calculate order distance from company
            double orderDistance = CalculateAirDistance(
                s_dal.Config.CompanyLatitude.Value,
                s_dal.Config.CompanyLongitude.Value,
                order.Latitude,
                order.Longitude
            );

            // Find eligible couriers
            var eligibleCouriers = allCouriers
                .Where(c => c.MaxDeliveryDistance == null || orderDistance <= c.MaxDeliveryDistance)
                .Where(c => c.IsActive is true)
                .ToList();

            if (eligibleCouriers.Count() == 0)
            {
                // No eligible courier, leave order open
                openOrdersCreated++;
                continue;
            }

            // Select random eligible courier
            var courier = eligibleCouriers[s_rand.Next(eligibleCouriers.Count)];

            // Determine delivery status based on targets
            DeliveryStatus? status;
            if (inProgressCreated < targetInProgress)
            {
                status = null; // In progress
                inProgressCreated++;
            }
            else
            {
                // Select from closed statuses with weights
                int random = s_rand.Next(100);
                if (random < 65) status = DeliveryStatus.Completed; // 65%
                else if (random < 75) status = DeliveryStatus.CustomerNotFound; // 10%
                else if (random < 85) status = DeliveryStatus.Failed; // 10%
                else if (random < 92) status = DeliveryStatus.Cancelled; // 7%
                else status = DeliveryStatus.CustomerRefused; // 8%

                closedCreated++;
            }

            // Start time: between order creation and now
            DateTime earliestStart = order.CreatedAt > courierAvailability[courier.Id]
                ? order.CreatedAt
                : courierAvailability[courier.Id];

            DateTime latestStart = s_dal.Config.Clock.AddHours(-2);
            if (earliestStart >= latestStart)
                latestStart = earliestStart.AddHours(2);

            int hoursDiff = Math.Max(1, (int)(latestStart - earliestStart).TotalHours);
            DateTime startTime = earliestStart.AddHours(s_rand.Next(0, hoursDiff));

            // Actual distance based on delivery type
            double baseDistance = orderDistance;
            double actualDistance = courier.DeliveryType switch
            {
                DeliveryType.Car => baseDistance * 1.5,
                DeliveryType.Motorcycle => baseDistance * 1.4,
                DeliveryType.Bicycle => baseDistance * 1.7,
                DeliveryType.OnFoot => baseDistance * 2.0,
                _ => baseDistance * 1.5
            };

            // End time and completion status
            DateTime? endTime = null;
            if (status.HasValue)
            {
                // Completed delivery
                int durationHours = s_rand.Next(1, 6);
                endTime = startTime.AddHours(durationHours);

                // Update courier availability
                courierAvailability[courier.Id] = endTime.Value;
            }
            else
            {
                // In progress
                courierAvailability[courier.Id] = s_dal.Config.Clock;
            }

            // Create delivery
            Delivery delivery = new Delivery(
                Id: 0, // Will be auto-generated
                OrderId: order.Id,
                CourierId: courier.Id,
                DeliveryType: courier.DeliveryType,
                StartTime: startTime,
                ActualDistance: status.HasValue || s_rand.Next(0, 2) == 0 ? actualDistance : null
            )
            {
                CompletionStatus = status,
                EndTime = endTime
            };
            s_dal.Delivery.Create(delivery);
        }

        // Remaining orders stay open
        openOrdersCreated += availableOrders.Count;

        Console.WriteLine($"  Created deliveries: {openOrdersCreated} open, {inProgressCreated} in progress, {closedCreated} closed");
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