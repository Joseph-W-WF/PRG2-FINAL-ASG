using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Gruberoo.Models;

// feature owner: aydan (features 1, 4, 6, 8)
public static class AydanFeatures
{
    // feature 1: load restaurants + food items
    // important: restaurants.csv = RestaurantId,Name,Email | fooditems.csv = RestaurantId,ItemName,Description,Price
    public static void Feature1_LoadRestaurantsAndFoodItems(
        string dataDir,
        Dictionary<string, Restaurant> restaurantsById,
        out int restaurantsLoaded,
        out int foodItemsLoaded)
    {
        restaurantsLoaded = 0;
        foodItemsLoaded = 0;

        if (restaurantsById == null)
            throw new ArgumentNullException(nameof(restaurantsById));

        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = ".";

        string restaurantsPath = Path.Combine(dataDir, "restaurants.csv");
        string foodItemsPath = FindFoodItemsFile(dataDir);

        if (!File.Exists(restaurantsPath))
            throw new FileNotFoundException("cannot find restaurants.csv at: " + restaurantsPath);

        if (foodItemsPath == null || !File.Exists(foodItemsPath))
            throw new FileNotFoundException("cannot find fooditems file inside: " + dataDir);

        restaurantsById.Clear();

        string[] restLines = File.ReadAllLines(restaurantsPath);
        for (int i = 1; i < restLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(restLines[i]))
                continue;

            List<string> parts = SplitCsvLine(restLines[i]);
            if (parts.Count < 3)
                continue;

            string id = parts[0].Trim();
            string name = parts[1].Trim();
            string email = parts[2].Trim();

            if (id.Length == 0)
                continue;

            Restaurant r = new Restaurant(id, name, email);
            EnsureMainMenuExists(r);

            restaurantsById[id] = r;
            restaurantsLoaded++;
        }

        string[] foodLines = File.ReadAllLines(foodItemsPath);
        for (int i = 1; i < foodLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(foodLines[i]))
                continue;

            List<string> parts = SplitCsvLine(foodLines[i]);
            if (parts.Count < 4)
                continue;

            string restId = parts[0].Trim();
            string itemName = parts[1].Trim();
            string desc = parts[2].Trim();
            string priceStr = parts[3].Trim();

            if (!restaurantsById.ContainsKey(restId))
                continue;

            double price = 0;
            double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

            FoodItem fi = new FoodItem(itemName, desc, price);
            AddFoodItemToMainMenu(restaurantsById[restId], fi);
            foodItemsLoaded++;
        }
    }

    // feature 4: list all orders (basic info)
    public static void Feature4_ListAllOrders(List<Order> allOrders)
    {
        Console.WriteLine();
        Console.WriteLine("all orders");
        Console.WriteLine("==========");

        if (allOrders == null || allOrders.Count == 0)
        {
            Console.WriteLine("(no orders)");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("order id  customer              restaurant            delivery            amount    status");
        Console.WriteLine("--------  --------------------  --------------------  ------------------  --------  --------");

        for (int i = 0; i < allOrders.Count; i++)
        {
            Order o = allOrders[i];

            int orderId = o.OrderId;
            string customerName = (o.Customer == null) ? "-" : o.Customer.CustomerName;
            string restaurantName = (o.Restaurant == null) ? "-" : o.Restaurant.RestaurantName;
            DateTime deliveryDt = o.DeliveryDateTime;
            double amount = o.OrderTotal;
            string status = o.OrderStatus ?? "-";

            Console.WriteLine(
                $"{orderId,-8}  {TrimTo(customerName, 20),-20}  {TrimTo(restaurantName, 20),-20}  {deliveryDt:dd/MM HH:mm,-18}  {amount,8:0.00}  {status}"
            );
        }

        Console.WriteLine();
    }

    // feature 6: process an order (restaurant queue)
    // important: C confirm (pending->preparing), R reject (pending->rejected + refund), S skip (only if cancelled), D deliver (preparing->delivered)
    public static void Feature6_ProcessOrder(
        Dictionary<string, Restaurant> restaurantsById,
        Stack<Order> refundStack)
    {
        Console.WriteLine();
        Console.WriteLine("process order");
        Console.WriteLine("=============");

        if (restaurantsById == null || refundStack == null)
        {
            Console.WriteLine("internal error: missing data structures.");
            return;
        }

        string rid = ReadNonEmpty("enter restaurant id: ");

        if (!restaurantsById.ContainsKey(rid))
        {
            Console.WriteLine("restaurant not found.\n");
            return;
        }

        Restaurant restaurant = restaurantsById[rid];
        Queue<Order> q = restaurant.OrderQueue;

        if (q == null || q.Count == 0)
        {
            Console.WriteLine("no orders in this restaurant queue.\n");
            return;
        }

        int toProcess = q.Count;

        for (int i = 0; i < toProcess; i++)
        {
            Order order = q.Dequeue();

            PrintOrderForProcessing(order);

            Console.Write("[c]onfirm / [r]eject / [s]kip / [d]eliver: ");
            string action = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            string status = NormalizeStatus(order.OrderStatus);

            if (action == "C")
            {
                if (status == "PENDING")
                {
                    order.OrderStatus = "Preparing";
                    Console.WriteLine("confirmed! status is now preparing.\n");
                }
                else
                {
                    Console.WriteLine("cannot confirm because order is not pending.\n");
                }

                q.Enqueue(order);
            }
            else if (action == "R")
            {
                if (status == "PENDING")
                {
                    order.OrderStatus = "Rejected";
                    refundStack.Push(order);
                    Console.WriteLine($"rejected. refund: ${order.OrderTotal:0.00}\n");
                }
                else
                {
                    Console.WriteLine("cannot reject because order is not pending.\n");
                    q.Enqueue(order);
                }
            }
            else if (action == "S")
            {
                if (status == "CANCELLED")
                {
                    Console.WriteLine("skipped (cancelled).\n");
                    q.Enqueue(order);
                }
                else
                {
                    Console.WriteLine("skip is only allowed if order is cancelled.\n");
                    q.Enqueue(order);
                }
            }
            else if (action == "D")
            {
                // important: dataset may contain "Confirmed" in orders.csv; treat it like preparing for delivery
                if (status == "PREPARING" || status == "CONFIRMED")
                {
                    order.OrderStatus = "Delivered";
                    Console.WriteLine("delivered. status is now delivered.\n");
                }
                else
                {
                    Console.WriteLine("cannot deliver because order must be preparing.\n");
                    q.Enqueue(order);
                }
            }
            else
            {
                Console.WriteLine("invalid action. order unchanged.\n");
                q.Enqueue(order);
            }
        }
    }

    // feature 8: delete order (cancel + refund)
    // important: only pending orders can be deleted -> status becomes cancelled + push to refund stack
    public static void Feature8_DeleteOrder(
        Dictionary<string, Customer> customersByEmail,
        Stack<Order> refundStack)
    {
        Console.WriteLine();
        Console.WriteLine("delete order");
        Console.WriteLine("===========");

        if (customersByEmail == null || refundStack == null)
        {
            Console.WriteLine("internal error: missing data structures.");
            return;
        }

        string email = ReadNonEmpty("enter customer email: ");

        if (!customersByEmail.ContainsKey(email))
        {
            Console.WriteLine("customer not found.\n");
            return;
        }

        Customer customer = customersByEmail[email];

        List<Order> pendingOrders = new List<Order>();
        for (int i = 0; i < customer.Orders.Count; i++)
        {
            Order o = customer.Orders[i];
            if (NormalizeStatus(o.OrderStatus) == "PENDING")
                pendingOrders.Add(o);
        }

        if (pendingOrders.Count == 0)
        {
            Console.WriteLine("no pending orders to delete.\n");
            return;
        }

        Console.WriteLine("pending orders:");
        for (int i = 0; i < pendingOrders.Count; i++)
        {
            Console.WriteLine("- " + pendingOrders[i].OrderId);
        }

        int orderId = ReadInt("enter order id: ");

        Order target = null;
        for (int i = 0; i < pendingOrders.Count; i++)
        {
            if (pendingOrders[i].OrderId == orderId)
            {
                target = pendingOrders[i];
                break;
            }
        }

        if (target == null)
        {
            Console.WriteLine("invalid order id.\n");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("order summary");
        Console.WriteLine($"customer: {customer.CustomerName}");
        Console.WriteLine($"restaurant: {(target.Restaurant == null ? "-" : target.Restaurant.RestaurantName)}");
        Console.WriteLine($"delivery: {target.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"total: ${target.OrderTotal:0.00}");
        Console.WriteLine($"status: {target.OrderStatus}");

        bool confirm = ReadYesNo("confirm deletion? [y/n]: ");
        if (!confirm)
        {
            Console.WriteLine("deletion cancelled.\n");
            return;
        }

        target.OrderStatus = "Cancelled";
        refundStack.Push(target);

        Console.WriteLine($"order {target.OrderId} cancelled. refund: ${target.OrderTotal:0.00}\n");
    }

    private static void EnsureMainMenuExists(Restaurant r)
    {
        if (r.Menus == null)
            r.Menus = new List<Menu>();

        if (r.Menus.Count == 0)
        {
            r.Menus.Add(new Menu("Main Menu"));
            return;
        }

        bool hasMain = false;
        for (int i = 0; i < r.Menus.Count; i++)
        {
            string name = r.Menus[i].MenuName ?? "";
            if (name.Equals("Main Menu", StringComparison.OrdinalIgnoreCase))
            {
                hasMain = true;
                break;
            }
        }

        if (!hasMain)
            r.Menus.Insert(0, new Menu("Main Menu"));
    }

    private static void AddFoodItemToMainMenu(Restaurant r, FoodItem fi)
    {
        EnsureMainMenuExists(r);

        Menu main = r.Menus[0];

        if (main.FoodItems == null)
            main.FoodItems = new List<FoodItem>();

        main.FoodItems.Add(fi);
    }

    private static string FindFoodItemsFile(string dataDir)
    {
        string exact = Path.Combine(dataDir, "fooditems.csv");
        if (File.Exists(exact))
            return exact;

        string[] matches = Directory.GetFiles(dataDir, "fooditems*.csv");
        if (matches.Length > 0)
            return matches[0];

        return null;
    }

    private static string TrimTo(string s, int maxLen)
    {
        if (s == null) s = "";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen);
    }

    private static string NormalizeStatus(string status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();

        if (s == "PENDING") return "PENDING";
        if (s == "PREPARING") return "PREPARING";
        if (s == "DELIVERED") return "DELIVERED";
        if (s == "REJECTED") return "REJECTED";
        if (s == "CANCELLED") return "CANCELLED";
        if (s == "CONFIRMED") return "CONFIRMED";

        return s;
    }

    private static void PrintOrderForProcessing(Order o)
    {
        Console.WriteLine();
        Console.WriteLine($"order {o.OrderId}");
        Console.WriteLine($"customer: {(o.Customer == null ? "-" : o.Customer.CustomerName)}");
        Console.WriteLine($"delivery: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"total: ${o.OrderTotal:0.00}");
        Console.WriteLine($"status: {o.OrderStatus}");

        if (o.OrderedItems != null && o.OrderedItems.Count > 0)
        {
            Console.WriteLine("items:");
            for (int i = 0; i < o.OrderedItems.Count; i++)
            {
                var it = o.OrderedItems[i];
                Console.WriteLine($"  - {it.FoodItem.ItemName} x{it.Quantity}");
            }
        }
    }

    private static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim();
            if (s.Length > 0) return s;
            Console.WriteLine("cannot be empty.");
        }
    }

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim();

            int value;
            if (int.TryParse(s, out value))
                return value;

            Console.WriteLine("please enter a valid number.");
        }
    }

    private static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (s == "Y") return true;
            if (s == "N") return false;

            Console.WriteLine("please enter y or n.");
        }
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }
}
