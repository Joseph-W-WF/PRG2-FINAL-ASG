//==========================================================
// Student Number : S10273117G
// Student Name : Aydan Yeo
// Partner Name : Joseph Wong
//==========================================================

using System;
using System.Collections.Generic;
using System.IO;

public class Program
{
    public static void Main()
    {
        string dataDir = ResolveDataDir();

        string customersPath = Path.Combine(dataDir, "customers.csv");
        string ordersPath = Path.Combine(dataDir, "orders.csv");
        string restaurantsPath = Path.Combine(dataDir, "restaurants.csv");
        string foodItemsPath = Path.Combine(dataDir, "fooditems.csv");
        string foodItemsAltPath = Path.Combine(dataDir, "fooditems - Copy.csv");

        if (!File.Exists(foodItemsPath) && File.Exists(foodItemsAltPath))
            foodItemsPath = foodItemsAltPath;

        RequireFile(customersPath);
        RequireFile(ordersPath);
        RequireFile(restaurantsPath);
        RequireFile(foodItemsPath);

        var restaurantsById = new Dictionary<string, Restaurant>(StringComparer.OrdinalIgnoreCase);
        var refundStack = new Stack<Order>();

        int restaurantsLoaded, foodItemsLoaded;
        AydanFeatures.Feature1_LoadRestaurantsAndFoodItems(
            dataDir,
            restaurantsById,
            out restaurantsLoaded,
            out foodItemsLoaded
        );

        var restaurants = new List<Restaurant>(restaurantsById.Values);

        var customers = JosephFeatures.Feature2_LoadCustomersAndOrders(
            customersPath,
            ordersPath,
            restaurants
        );

        var customersByEmail = BuildCustomersByEmail(customers);
        int ordersLoaded = CountUniqueOrders(customers);

        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurantsLoaded} restaurants loaded!");
        Console.WriteLine($"{foodItemsLoaded} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{ordersLoaded} orders loaded!");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("===== Gruberoo Food Delivery System =====");
            Console.WriteLine("1. List all restaurants and menu items");
            Console.WriteLine("2. List all orders");
            Console.WriteLine("3. Create a new order");
            Console.WriteLine("4. Process an order");
            Console.WriteLine("5. Modify an existing order");
            Console.WriteLine("6. Delete an existing order");
            Console.WriteLine("7. Bulk process pending orders (today)");
            Console.WriteLine("8. Display total order amount");
            Console.WriteLine("0. Exit");
            Console.Write("Enter your choice: ");

            string choice = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();

            if (choice == "0")
                break;

            switch (choice)
            {
                case "1":
                    JosephFeatures.Feature3_ListAllRestaurantsAndMenuItems(restaurants);
                    break;

                case "2":
                    var allOrders = BuildAllOrders(customers);
                    AydanFeatures.Feature4_ListAllOrders(allOrders, customersByEmail, restaurantsById);
                    break;

                case "3":
                    JosephFeatures.Feature5_CreateNewOrder(customers, restaurants, ordersPath);
                    customersByEmail = BuildCustomersByEmail(customers);
                    break;

                case "4":
                    AydanFeatures.Feature6_ProcessOrder(restaurantsById, customersByEmail, refundStack);
                    break;

                case "5":
                    JosephFeatures.Feature7_ModifyExistingOrder(customers, restaurants, ordersPath);
                    customersByEmail = BuildCustomersByEmail(customers);
                    break;

                case "6":
                    AydanFeatures.Feature8_DeleteOrder(customersByEmail, restaurantsById, refundStack);
                    break;

                case "7":
                    JosephFeatures.AdvancedA_BulkProcessPendingOrdersForToday(restaurants, ordersPath, customers);
                    break;

                case "8":
                    AydanFeatures.AdvancedB_DisplayTotalOrderAmount(restaurants, customers);
                    break;

                default:
                    Console.WriteLine("Invalid option.\n");
                    break;
            }
        }

        QueueStackStore.SaveQueueAndStack(dataDir, restaurants, refundStack);
    }

    private static string ResolveDataDir()
    {
        string dir = Directory.GetCurrentDirectory();

        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "Data-Files");
            if (Directory.Exists(candidate))
                return candidate;

            DirectoryInfo? parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        return Directory.GetCurrentDirectory();
    }

    private static void RequireFile(string filePath)
    {
        if (!File.Exists(filePath))
            Console.WriteLine($"[WARNING] Missing file: {filePath}");
    }

    private static Dictionary<string, Customer> BuildCustomersByEmail(List<Customer> customers)
    {
        var dict = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in customers)
        {
            if (!dict.ContainsKey(c.EmailAddress))
                dict[c.EmailAddress] = c;
        }

        return dict;
    }

    private static List<Order> BuildAllOrders(List<Customer> customers)
    {
        var list = new List<Order>();
        foreach (var c in customers)
            foreach (var o in c.Orders)
                list.Add(o);

        return list;
    }

    private static int CountUniqueOrders(List<Customer> customers)
    {
        var seen = new HashSet<int>();
        foreach (var c in customers)
            foreach (var o in c.Orders)
                seen.Add(o.OrderId);

        return seen.Count;
    }
}
