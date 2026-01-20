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

    private const double WOLT_LATITUDE = 32.098799;
    private const double WOLT_LONGITUDE = 34.8979087;
    private const string WOLT_ADDRESS = "Yona Green 7, Petah Tikva, Israel";

    private static readonly string[] validPhonePrefixes = { "050", "051", "052", "053", "054", "055", "058" };
    private static List<int> s_courierIds = new();

    // Track the next order ID manually
    private static int s_nextOrderId = 1;

    public static void Do()
    {
        s_dal = DalApi.Factory.Get;

        Console.WriteLine("Resetting configuration values and clearing data lists...");
        s_dal.ResetDB();

        // Reset order ID counter
        s_nextOrderId = 1;

        Console.WriteLine("Initializing company configuration...");
        CreateConfig();

        Console.WriteLine("Initializing couriers list...");
        CreateCouriers();

        Console.WriteLine("Initializing orders (Open/InProgress only)...");
        CreateOrders();

        Console.WriteLine("Creating courier delivery history (until yesterday)...");
        CreateCourierDeliveryHistory();

        // Debug: Check how many deliveries exist after initialization
        var allDeliveries = s_dal.Delivery.ReadAll().ToList();
        Console.WriteLine($"[DEBUG] Total deliveries in DAL after initialization: {allDeliveries.Count}");
        System.Diagnostics.Debug.WriteLine($"[Initialization] Total deliveries created: {allDeliveries.Count}");

        Console.WriteLine("Data initialization completed successfully!");
    }

    private static void CreateConfig()
    {
        s_dal.Config.CompanyAddress = WOLT_ADDRESS;
        s_dal.Config.CompanyLatitude = WOLT_LATITUDE;
        s_dal.Config.CompanyLongitude = WOLT_LONGITUDE;
        s_dal.Config.MaxDeliveryDistance = 50.0;
        s_dal.Config.CarSpeed = 30.0;
        s_dal.Config.MotorcycleSpeed = 35.0;
        s_dal.Config.BicycleSpeed = 15.0;
        s_dal.Config.OnFootSpeed = 4.0;
        s_dal.Config.MaxDeliveryTime = TimeSpan.FromHours(4);
        s_dal.Config.RiskRange = TimeSpan.FromMinutes(120);
        s_dal.Config.InactivityRange = TimeSpan.FromDays(30);
    }

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
        DeliveryType[] deliveryTypes = { DeliveryType.Car, DeliveryType.Motorcycle, DeliveryType.Bicycle, DeliveryType.OnFoot };

        for (int i = 0; i < courierNames.Length; i++)
        {
            int id = GenerateValidIsraeliId();
            string phone = GeneratePhoneNumber();
            string emailName = courierNames[i].Replace(" ", ".").ToLower();
            string email = emailName + emailDomains[s_rand.Next(emailDomains.Length)];
            string plainPassword = GenerateStrongPassword();
            double? maxDistance = s_rand.Next(10, 51);
            DeliveryType deliveryType = deliveryTypes[i % deliveryTypes.Length];

            // ✅ ALL couriers start working at the simulator's current clock time
            DateTime startDate = s_dal!.Config.Clock;

            Courier courier = new Courier(id, startDate)
            {
                Name = courierNames[i],
                Phone = phone,
                Email = email,
                Password = plainPassword,
                IsActive = true,
                MaxDeliveryDistance = maxDistance,
                DeliveryType = deliveryType,
                AddressLatitude = WOLT_LATITUDE,
                AddressLongitude = WOLT_LONGITUDE
            };

            s_dal.Courier.Create(courier);
            s_courierIds.Add(id);
            Console.WriteLine($"  Created: {courierNames[i]}, ID: {id}, Password: {plainPassword}, Start Date: {startDate:yyyy-MM-dd HH:mm:ss}");
        }
    }

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

        OrderType[] orderTypes = { OrderType.RestaurantFood, OrderType.Groceries, OrderType.Retail };
        var allCouriers = s_dal!.Courier.ReadAll().ToList();
        var clock = s_dal.Config.Clock;

        int openCount = 0, inProgressCount = 0;
        int onTimeCount = 0, inRiskCount = 0, lateCount = 0;
        int totalDeliveryHistory = 0;
        int ordersWithHistory = 0;
        int currentDeliveryCount = 0;

        for (int i = 0; i < 30; i++)
        {
            var (address, lat, lon) = addressData[i % addressData.Length];
            OrderType orderType = orderTypes[s_rand.Next(orderTypes.Length)];

            double weight = orderType switch
            {
                OrderType.RestaurantFood => s_rand.NextDouble() * 3 + 1.0,
                OrderType.Groceries => s_rand.NextDouble() * 15 + 2,
                _ => s_rand.NextDouble() * 8 + 1.0
            };

            double volume = orderType switch
            {
                OrderType.RestaurantFood => s_rand.NextDouble() * 0.05 + 1.0,
                OrderType.Groceries => s_rand.NextDouble() * 0.15 + 1.0,
                _ => s_rand.NextDouble() * 0.1 + 1.0
            };

            bool isInProgress = s_rand.Next(2) == 1;
            int scheduleStatusRoll = s_rand.Next(3);

            int minutesAgo = scheduleStatusRoll switch
            {
                0 => s_rand.Next(0, 119),
                1 => s_rand.Next(121, 239),
                _ => s_rand.Next(241, 400)
            };

            DateTime createdAt = clock.AddMinutes(-minutesAgo);
            int? courierId = null;
            DateTime? courierAssociatedDate = null;
            DateTime? pickupDate = null;

            if (isInProgress)
            {
                var courier = allCouriers[s_rand.Next(allCouriers.Count)];
                courierId = courier.Id;
                courierAssociatedDate = createdAt.AddMinutes(s_rand.Next(5, 15));
                pickupDate = courierAssociatedDate.Value.AddMinutes(s_rand.Next(5, 10));
                inProgressCount++;
            }
            else
            {
                openCount++;
            }

            switch (scheduleStatusRoll)
            {
                case 0:
                    onTimeCount++;
                    break;
                case 1:
                    inRiskCount++;
                    break;
                default:
                    lateCount++;
                    break;
            }

            Order order = new Order(Id: 0, CreatedAt: createdAt)
            {
                OrderType = orderType,
                Description = $"Order #{i + 1} - {orderType}",
                Address = address,
                Latitude = lat,
                Longitude = lon,
                CustomerName = customerNames[i],
                CustomerPhone = GeneratePhoneNumber(),
                Weight = weight,
                Volume = volume,
                IsFragile = s_rand.Next(0, 4) == 0,
                CourierId = courierId,
                CourierAssociatedDate = courierAssociatedDate,
                PickupDate = pickupDate,
                DeliveryDate = null
            };

            s_dal.Order.Create(order);
            int createdOrderId = s_nextOrderId++;

            // Create current delivery record for InProgress orders
            if (courierId.HasValue)
            {
                var courier = allCouriers.First(c => c.Id == courierId.Value);
                double distance = CalculateAirDistance(WOLT_LATITUDE, WOLT_LONGITUDE, lat, lon);

                Delivery delivery = new Delivery(
                    Id: 0,
                    OrderId: createdOrderId,
                    CourierId: courier.Id,
                    DeliveryType: courier.DeliveryType,
                    StartTime: pickupDate ?? courierAssociatedDate ?? createdAt,
                    ActualDistance: distance * 1.5
                )
                {
                    CompletionStatus = null,
                    EndTime = null
                };

                s_dal.Delivery.Create(delivery);
                currentDeliveryCount++;
            }

            // ✅ LOGIC CHANGE: We want ~10% total CustomerRefused.
            // Active orders will contribute a small part to this.
            // ~15% chance for active orders to have a "Refused" history
            bool shouldHaveHistory = s_rand.NextDouble() < 0.15;

            if (shouldHaveHistory)
            {
                int historyCount = s_rand.Next(1, 4);
                int created = CreateDeliveryHistoryForOrder(createdOrderId, createdAt, allCouriers, historyCount);
                totalDeliveryHistory += created;
                ordersWithHistory++;
            }
        }

        Console.WriteLine($"  Created 30 orders (Open/InProgress only):");
        Console.WriteLine($"    OrderStatus: {openCount} Open, {inProgressCount} InProgress");
        Console.WriteLine($"    ScheduleStatus: {onTimeCount} OnTime, {inRiskCount} InRisk, {lateCount} Late");
        Console.WriteLine($"  Created {currentDeliveryCount} current delivery records");
        Console.WriteLine($"  Created {totalDeliveryHistory} delivery history records for {ordersWithHistory} orders");

        // Verify deliveries were created
        var verifyDeliveries = s_dal.Delivery.ReadAll().ToList();
        Console.WriteLine($"  [VERIFY] Deliveries in DAL after CreateOrders: {verifyDeliveries.Count}");
        System.Diagnostics.Debug.WriteLine($"[Initialization] Deliveries in DAL after CreateOrders: {verifyDeliveries.Count}");
    }

    private static int CreateDeliveryHistoryForOrder(int orderId, DateTime orderCreatedAt, List<Courier> allCouriers, int historyCount)
    {
        int created = 0;

        for (int i = 0; i < historyCount; i++)
        {
            var courier = allCouriers[s_rand.Next(allCouriers.Count)];
            DateTime startTime = orderCreatedAt.AddMinutes(s_rand.Next(5, 30) + (i * 60));

            // All history deliveries generated here are "Bad History" for active orders
            DeliveryStatus status = DeliveryStatus.CustomerRefused;
            DateTime endTime = startTime.AddMinutes(s_rand.Next(10, 45));

            Delivery delivery = new Delivery(
                Id: 0,
                OrderId: orderId,
                CourierId: courier.Id,
                DeliveryType: courier.DeliveryType,
                StartTime: startTime,
                ActualDistance: s_rand.NextDouble() * 10 + 1
            )
            {
                CompletionStatus = status,
                EndTime = endTime
            };

            s_dal!.Delivery.Create(delivery);
            System.Diagnostics.Debug.WriteLine($"[Initialization] Created history Delivery for Order: {orderId}, Status: {status}");
            created++;
        }

        return created;
    }

    private static void CreateCourierDeliveryHistory()
    {
        var allCouriers = s_dal!.Courier.ReadAll().ToList();
        var couriersWithHistory = allCouriers.Take(18).ToList(); // 18 couriers with history

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
        int completedCount = 0, canceledCount = 0, refusedCount = 0;

        foreach (var courier in couriersWithHistory)
        {
            int deliveryCount = s_rand.Next(2, 5); // 2-4 deliveries per courier

            for (int i = 0; i < deliveryCount; i++)
            {
                var (address, lat, lon) = addressData[s_rand.Next(addressData.Length)];

                // ✅ TIMING FIX: 1 to 60 days BACK. Ensures it is strictly "until yesterday".
                DateTime historicalDate = s_dal.Config.Clock.AddDays(-s_rand.Next(1, 60));

                int scheduleRoll = s_rand.Next(3);
                int deliveryMinutes = scheduleRoll switch
                {
                    0 => s_rand.Next(60, 119),
                    1 => s_rand.Next(121, 239),
                    _ => s_rand.Next(241, 360)
                };

                // ✅ STATS CALCULATION:
                // Global Target: ~15% Canceled, ~10% Refused.
                // Assuming ~85 total orders in DB (30 active + ~55 historical).
                // 15% of 85 = ~13 Canceled orders.
                // 10% of 85 = ~9 Refused deliveries.

                // We generated some Refused in CreateOrders (~4-5). Need ~4-5 more here.
                // All Canceled must come from here (since active orders aren't canceled).

                double r = s_rand.NextDouble();
                DeliveryStatus deliveryStatus;

                if (r < 0.25) // 25% of history = Cancelled (approx 13 orders)
                {
                    deliveryStatus = DeliveryStatus.Cancelled;
                    canceledCount++;
                }
                else if (r < 0.35) // 10% of history = Refused (approx 5-6 deliveries)
                {
                    deliveryStatus = DeliveryStatus.CustomerRefused;
                    refusedCount++;
                }
                else // Remainder (65%) = Completed
                {
                    deliveryStatus = DeliveryStatus.Completed;
                    completedCount++;
                }

                bool isCanceled = (deliveryStatus == DeliveryStatus.Cancelled);

                Order historicalOrder = new Order(Id: 0, CreatedAt: historicalDate)
                {
                    OrderType = (OrderType)s_rand.Next(0, 3),
                    Description = $"Historical Order - {deliveryStatus} by {courier.Name}",
                    Address = address,
                    Latitude = lat,
                    Longitude = lon,
                    CustomerName = customerNames[s_rand.Next(customerNames.Length)],
                    CustomerPhone = GeneratePhoneNumber(),
                    Weight = s_rand.NextDouble() * 5 + 1.0,
                    Volume = s_rand.NextDouble() * 0.5 + 1.0,
                    IsFragile = false,
                    CourierId = isCanceled ? null : courier.Id,
                    CourierAssociatedDate = isCanceled ? null : historicalDate.AddMinutes(10),
                    PickupDate = isCanceled ? null : historicalDate.AddMinutes(20),
                    DeliveryDate = isCanceled ? null : historicalDate.AddMinutes(deliveryMinutes)
                };

                s_dal.Order.Create(historicalOrder);
                int createdOrderId = s_nextOrderId++;

                double distance = CalculateAirDistance(WOLT_LATITUDE, WOLT_LONGITUDE, lat, lon);

                Delivery historicalDelivery = new Delivery(
                    Id: 0,
                    OrderId: createdOrderId,
                    CourierId: courier.Id,
                    DeliveryType: courier.DeliveryType,
                    StartTime: historicalDate.AddMinutes(20),
                    ActualDistance: distance * 1.5
                )
                {
                    CompletionStatus = deliveryStatus,
                    EndTime = historicalDate.AddMinutes(deliveryMinutes)
                };

                s_dal.Delivery.Create(historicalDelivery);
                totalHistoryDeliveries++;
            }
        }

        Console.WriteLine($"  Created {totalHistoryDeliveries} historical orders for {couriersWithHistory.Count} couriers");
        Console.WriteLine($"    Status: {completedCount} Completed, {canceledCount} Canceled, {refusedCount} Refused");
    }

    private static int GenerateValidIsraeliId()
    {
        int id;
        bool isValid;

        do
        {
            string idStr = "";
            for (int i = 0; i < 8; i++)
                idStr += s_rand.Next(0, 10);

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
            isValid = s_dal?.Courier.Read(id) == null;
        } while (!isValid);

        return id;
    }

    private static string GeneratePhoneNumber()
    {
        string prefix = validPhonePrefixes[s_rand.Next(validPhonePrefixes.Length)];
        string rest = s_rand.Next(0, 10000000).ToString("D7");
        return s_rand.Next(0, 2) == 0 ? $"{prefix}-{rest}" : $"{prefix}{rest}";
    }

    private static string GenerateStrongPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var password = new StringBuilder();
        password.Append(uppercase[s_rand.Next(uppercase.Length)]);
        password.Append(lowercase[s_rand.Next(lowercase.Length)]);
        password.Append(digits[s_rand.Next(digits.Length)]);
        password.Append(special[s_rand.Next(special.Length)]);

        string allChars = uppercase + lowercase + digits + special;
        int additionalLength = s_rand.Next(4, 9);
        for (int i = 0; i < additionalLength; i++)
            password.Append(allChars[s_rand.Next(allChars.Length)]);

        return new string(password.ToString().OrderBy(c => s_rand.Next()).ToArray());
    }

    private static double CalculateAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
