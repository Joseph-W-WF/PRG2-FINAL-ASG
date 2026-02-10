using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// If your project uses namespace Gruberoo.Models, uncomment this:
// using Gruberoo.Models;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== GRUBEROO ===\n");

        // -----------------------------
        // Locate Data-Files folder safely
        // -----------------------------
        string dataDir = ResolveDataDir();
        string customersPath = Path.Combine(dataDir, "customers.csv");
        string ordersPath = Path.Combine(dataDir, "orders.csv");
        string restaurantsPath = Path.Combine(dataDir, "restaurants.csv");
        string foodItemsPath = Path.Combine(dataDir, "fooditems.csv");
        string foodItemsAltPath = Path.Combine(dataDir, "fooditems - Copy.csv");

        if (!File.Exists(foodItemsPath) && File.Exists(foodItemsAltPath))
            foodItemsPath = foodItemsAltPath;

        // Basic file checks (helps during demo)
        RequireFile(customersPath);
        RequireFile(ordersPath);
        RequireFile(restaurantsPath);
        RequireFile(foodItemsPath);

        // =========================================================
        // ===== A Y D A N   (Features 1,4,6,8) - DO NOT REMOVE =====
        // =========================================================
        // Shared structures Aydan uses
        var restaurantsById = new Dictionary<string, Restaurant>(StringComparer.OrdinalIgnoreCase);
        var refundStack = new Stack<Order>();

        // Feature 1 (Aydan): load restaurants + food items
        int restaurantsLoaded = 0, foodItemsLoaded = 0;

        // NOTE: Aydan’s Feature1 expects "dataDir" (folder), not file path.
        AydanFeatures.Feature1_LoadRestaurantsAndFoodItems(
            dataDir,
            restaurantsById,
            out restaurantsLoaded,
            out foodItemsLoaded
        );

        Console.WriteLine($"[Aydan F1] Restaurants loaded: {restaurantsLoaded}");
        Console.WriteLine($"[Aydan F1] Food items loaded: {foodItemsLoaded}\n");

        // Convert dictionary -> list for Joseph’s features
        var restaurants = restaurantsById.Values.ToList();

        // =========================================================
        // ===== J O S E P H   (Features 2,3,5,7) - ADD HERE ========
        // =========================================================

        // Feature 2 (Joseph): load customers + orders (orders queued into restaurants)
        var customers = JosephFeatures.Feature2_LoadCustomersAndOrders(
            customersPath,
            ordersPath,
            restaurants
        );

        // Helpful shared structures for Aydan Feature 8 + Feature 4
        var customersByEmail = BuildCustomersByEmail(customers);
        var allOrders = BuildAllOrders(customers);

        // =========================================================
        // ====================== MAIN MENU =========================
        // =========================================================
        while (true)
        {
            Console.WriteLine("============== MENU ==============");
            Console.WriteLine("Aydan (1,4,6,8):");
            Console.WriteLine("  1) Feature 1 - Load restaurants + food items (already done at startup)");
            Console.WriteLine("  4) Feature 4 - List all orders");
            Console.WriteLine("  6) Feature 6 - Process orders (confirm/reject/deliver)");
            Console.WriteLine("  8) Feature 8 - Delete order (cancel + refund)");

            Console.WriteLine("\nJoseph (2,3,5,7):");
            Console.WriteLine("  2) Feature 2 - Load customers + orders (re-load)");
            Console.WriteLine("  3) Feature 3 - List restaurants + menu items");
            Console.WriteLine("  5) Feature 5 - Create new order");
            Console.WriteLine("  7) Feature 7 - Modify pending order");

            Console.WriteLine("\n0) Exit");
            Console.Write("Choose option: ");

            string choice = (Console.ReadLine() ?? "").Trim();

            Console.WriteLine();

            if (choice == "0") break;

            switch (choice)
            {
                // ==========================
                // ===== Aydan Features ======
                // ==========================

                case "1":
                    Console.WriteLine("Feature 1 already executed at startup.");
                    Console.WriteLine("If you want to re-run it, restart the program.\n");
                    break;

                case "4":
                    // Feature 4 (Aydan): list all orders
                    allOrders = BuildAllOrders(customers);
                    AydanFeatures.Feature4_ListAllOrders(allOrders);
                    Console.WriteLine();
                    break;

                case "6":
                    // Feature 6 (Aydan): process orders in a restaurant queue
                    AydanFeatures.Feature6_ProcessOrder(restaurantsById, refundStack);

                    // OPTIONAL persistence (recommended so status changes stay after restart)
                    SafePersistOrders(ordersPath, customers);

                    Console.WriteLine();
                    break;

                case "8":
                    // Feature 8 (Aydan): delete order (cancel + refund)
                    customersByEmail = BuildCustomersByEmail(customers);
                    AydanFeatures.Feature8_DeleteOrder(customersByEmail, refundStack);

                    // refresh shared lists after deletion
                    allOrders = BuildAllOrders(customers);

                    // OPTIONAL persistence (recommended so deletion stays after restart)
                    SafePersistOrders(ordersPath, customers);

                    Console.WriteLine();
                    break;

                // ===========================
                // ===== Joseph Features ======
                // ===========================

                case "2":
                    // Feature 2 (Joseph): re-load customers + orders
                    customers = JosephFeatures.Feature2_LoadCustomersAndOrders(customersPath, ordersPath, restaurants);
                    customersByEmail = BuildCustomersByEmail(customers);
                    allOrders = BuildAllOrders(customers);
                    Console.WriteLine();
                    break;

                case "3":
                    // Feature 3 (Joseph)
                    JosephFeatures.Feature3_ListAllRestaurantsAndMenuItems(restaurants);
                    break;

                case "5":
                    // Feature 5 (Joseph): create order + append orders.csv (your code already appends)
                    JosephFeatures.Feature5_CreateNewOrder(customers, restaurants, ordersPath);

                    // refresh shared lists after creating order
                    customersByEmail = BuildCustomersByEmail(customers);
                    allOrders = BuildAllOrders(customers);

                    Console.WriteLine();
                    break;

                case "7":
                    // Feature 7 (Joseph): modify order + rewrite orders.csv (your code already rewrites)
                    JosephFeatures.Feature7_ModifyExistingOrder(customers, restaurants, ordersPath);

                    // refresh shared lists after modification
                    customersByEmail = BuildCustomersByEmail(customers);
                    allOrders = BuildAllOrders(customers);

                    Console.WriteLine();
                    break;

                default:
                    Console.WriteLine("Invalid option.\n");
                    break;
            }
        }

        Console.WriteLine("\nGoodbye!");
    }

    // =========================
    // Helpers (Shared)
    // =========================

    private static string ResolveDataDir()
    {
        // Start from current execution directory and walk up to find Data-Files
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

        // fallback: current directory (if user runs from project root and already inside Data-Files)
        return Directory.GetCurrentDirectory();
    }

    private static void RequireFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[WARNING] Missing file: {filePath}");
        }
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
        {
            foreach (var o in c.Orders)
                list.Add(o);
        }
        return list;
    }

    private static void SafePersistOrders(string ordersPath, List<Customer> customers)
    {
        // Only works if you have OrderCsvStore from Joseph loaders.
        // If you don't want persistence, you can delete this call.
        try
        {
            OrderCsvStore.RewriteAllOrders(ordersPath, customers);
            Console.WriteLine("[Saved] orders.csv updated.\n");
        }
        catch
        {
            Console.WriteLine("[Warning] Could not save orders.csv (but program can continue).\n");
        }
    }
}
