using System;
using DO;
using DalApi;
using Dal;
using System.Linq;

namespace DalTest
{
    internal class Program
    {
        private static ICourier? s_dalCourier = new CourierImplementation();
        private static IOrder? s_dalOrder = new OrderImplementation();
        private static IDelivery? s_dalDelivery = new DeliveryImplementation();
        private static IConfig? s_dalConfig = new ConfigImplementation();

        private enum MainMenu { Exit, Couriers, Orders, Deliveries, Config, InitDb, ShowAll, Reset }
        private enum CrudMenu { Exit, Create, Read, ReadAll, Update, Delete, DeleteAll }

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

        private static void RunMainMenu()
        {
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("Main menu \n");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Manege courier");
                Console.WriteLine("2. Manege order");
                Console.WriteLine("3. Manege delivery");
                Console.WriteLine("4. Manege config");
                Console.WriteLine("5. Init db");
                Console.WriteLine("6. Show all details");
                Console.WriteLine("7. Reset");
                Console.Write("Enter your choice: ");

                if (!Enum.TryParse(Console.ReadLine(), out MainMenu choice))
                {
                    Console.WriteLine("Error, Invalid choice!.");
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case MainMenu.Exit:
                            exit = true;
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
                            Console.WriteLine("Initialization seccess!");
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
                    Console.WriteLine($"Erorr: {ex.Message}!");
                }
            }
        }

        // CRUD menu
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
                Console.Write("Enter your choice: ");

                if (!Enum.TryParse(Console.ReadLine(), out CrudMenu choice))
                {
                    Console.WriteLine("Error, Invalid choice!");
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case CrudMenu.Exit:
                            exit = true;
                            break;
                        case CrudMenu.Create:
                            break;
                        case CrudMenu.Read:
                            break;
                        case CrudMenu.ReadAll:
                            break;
                        case CrudMenu.Update:
                            break;
                        case CrudMenu.Delete:
                            break;
                        case CrudMenu.DeleteAll:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in {entity} action: {ex.Message}!");
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
                Console.WriteLine("1. Show system clock");
                Console.WriteLine("2. Advance clock by one hour");
                Console.WriteLine("3. Reset configuration");
                Console.Write("Choice: ");

                string? input = Console.ReadLine();
                switch (input)
                {
                    case "0": exit = true; break;
                    case "1": Console.WriteLine(s_dalConfig?.Clock); break;
                    case "2": s_dalConfig?.Clock.AddHours(1); break;
                    case "3": s_dalConfig?.Reset(); break;
                    default: Console.WriteLine("Invalid choice."); break;
                }
            }
        }

        // Utility Methods
        private static void ShowAllData()
        {
            Console.WriteLine("All courier:\n ");
            foreach (var c in s_dalCourier.ReadAll()) Console.WriteLine(c);

            Console.WriteLine("All order:\n");
            foreach (var o in s_dalOrder.ReadAll()) Console.WriteLine(o);

            Console.WriteLine("All delivery:\n");
            foreach (var d in s_dalDelivery.ReadAll()) Console.WriteLine(d);
        }

        private static void ResetDatabase()
        {
            s_dalCourier?.DeleteAll();
            s_dalOrder?.DeleteAll();
            s_dalDelivery?.DeleteAll();
            s_dalConfig?.Reset();
            Console.WriteLine("Db reset seccessful.");
        }
    }
}
