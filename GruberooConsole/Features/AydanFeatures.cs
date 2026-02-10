//==========================================================
// Student Number : S10273196
// Student Name : Aydan Yeo
// Partner Name : Joseph Wong
//==========================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class AydanFeatures
{
    // feature 1 (aydan): load restaurants + food items
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

        if (string.IsNullOrWhiteSpace(foodItemsPath) || !File.Exists(foodItemsPath))
            throw new FileNotFoundException("cannot find fooditems.csv in folder: " + dataDir);

        restaurantsById.Clear();

        string[] restLines = File.ReadAllLines(restaurantsPath);
        for (int i = 1; i < restLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(restLines[i])) continue;

            List<string> p = CsvUtils.SplitCsvLine(restLines[i]);
            if (p.Count < 3) continue;

            string id = p[0].Trim();
            string name = p[1].Trim();
            string email = p[2].Trim();

            if (id.Length == 0) continue;

            Restaurant r = new Restaurant(id, name, email);
            r.GetOrCreateMainMenu();

            restaurantsById[id] = r;
            restaurantsLoaded++;
        }

        string[] foodLines = File.ReadAllLines(foodItemsPath);
        for (int i = 1; i < foodLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(foodLines[i])) continue;

            List<string> p = CsvUtils.SplitCsvLine(foodLines[i]);
            if (p.Count < 4) continue;

            string restId = p[0].Trim();
            string itemName = p[1].Trim();
            string desc = p[2].Trim();
            string priceStr = p[3].Trim();

            if (!restaurantsById.ContainsKey(restId)) continue;

            double price = 0;
            double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

            FoodItem fi = new FoodItem(itemName, desc, price);

            Menu main = restaurantsById[restId].GetOrCreateMainMenu();
            main.AddFoodItem(fi);

            foodItemsLoaded++;
        }
    }

    // feature 4 (aydan): list all orders (basic info)
    // note: your Program.cs currently passes only List<Order>, so we display email + restaurant id.
    // assignment wants customer name + restaurant name (you can add an overload later). :contentReference[oaicite:2]{index=2}
    public static void Feature4_ListAllOrders(List<Order> allOrders)
    {
        Console.WriteLine("All Orders");
        Console.WriteLine("==========");

        if (allOrders == null || allOrders.Count == 0)
        {
            Console.WriteLine("(no orders)\n");
            return;
        }

        Console.WriteLine("Order ID  Customer Email              Restaurant ID  Delivery Date/Time     Amount     Status");
        Console.WriteLine("--------  --------------------------  -----------    ------------------     ------     ---------");

        for (int i = 0; i < allOrders.Count; i++)
        {
            Order o = allOrders[i];

            string email = o.CustomerEmail ?? "-";
            string rid = o.RestaurantId ?? "-";
            string status = o.OrderStatus ?? "-";

            Console.WriteLine(
                $"{o.OrderId,-8}  {TrimTo(email, 26),-26}  {rid,-11}  {o.DeliveryDateTime:dd/MM/yyyy HH:mm,-18}  ${o.OrderTotal,8:0.00}  {status}"
            );
        }

        Console.WriteLine();
    }

    // feature 6 (aydan): process an order (restaurant queue)
    // rules: confirm/reject only if Pending, skip only if Cancelled, deliver only if Preparing. :contentReference[oaicite:3]{index=3}
    public static void Feature6_ProcessOrder(
        Dictionary<string, Restaurant> restaurantsById,
        Stack<Order> refundStack)
    {
        Console.WriteLine("Process Order");
        Console.WriteLine("=============");

        if (restaurantsById == null || refundStack == null)
        {
            Console.WriteLine("internal error.\n");
            return;
        }

        string rid = ReadNonEmpty("Enter Restaurant ID: ");

        if (!restaurantsById.ContainsKey(rid))
        {
            Console.WriteLine("invalid restaurant id.\n");
            return;
        }

        Restaurant r = restaurantsById[rid];
        Queue<Order> q = r.OrderQueue;

        if (q == null || q.Count == 0)
        {
            Console.WriteLine("no orders in this restaurant queue.\n");
            return;
        }

        int count = q.Count;

        for (int i = 0; i < count; i++)
        {
            Order o = q.Dequeue();

            PrintOrderForProcessing(o);

            Console.Write("[C]onfirm / [R]eject / [S]kip / [D]eliver: ");
            string action = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            string st = NormalizeStatus(o.OrderStatus);

            if (action == "C")
            {
                if (st == "PENDING")
                {
                    o.OrderStatus = "Preparing";
                    Console.WriteLine($"Order {o.OrderId} confirmed. Status: Preparing\n");
                }
                else
                {
                    Console.WriteLine("cannot confirm (order not pending).\n");
                }
                q.Enqueue(o);
            }
            else if (action == "R")
            {
                if (st == "PENDING")
                {
                    o.OrderStatus = "Rejected";
                    refundStack.Push(o);
                    Console.WriteLine($"Order {o.OrderId} rejected. Refund of ${o.OrderTotal:0.00} processed.\n");
                }
                else
                {
                    Console.WriteLine("cannot reject (order not pending).\n");
                }
                q.Enqueue(o);
            }
            else if (action == "S")
            {
                if (st == "CANCELLED")
                {
                    Console.WriteLine($"Order {o.OrderId} skipped (cancelled).\n");
                }
                else
                {
                    Console.WriteLine("cannot skip (only cancelled orders can be skipped).\n");
                }
                q.Enqueue(o);
            }
            else if (action == "D")
            {
                // your CSV contains "Confirmed" sometimes, treat it as preparing for delivery
                if (st == "PREPARING" || st == "CONFIRMED")
                {
                    o.OrderStatus = "Delivered";
                    Console.WriteLine($"Order {o.OrderId} delivered. Status: Delivered\n");
                }
                else
                {
                    Console.WriteLine("cannot deliver (order must be preparing).\n");
                }
                q.Enqueue(o);
            }
            else
            {
                Console.WriteLine("invalid option.\n");
                q.Enqueue(o);
            }
        }
    }

    // feature 8 (aydan): delete order (cancel + refund)
    // only pending orders can be deleted. :contentReference[oaicite:4]{index=4}
    public static void Feature8_DeleteOrder(
        Dictionary<string, Customer> customersByEmail,
        Stack<Order> refundStack)
    {
        Console.WriteLine("Delete Order");
        Console.WriteLine("===========");

        if (customersByEmail == null || refundStack == null)
        {
            Console.WriteLine("internal error.\n");
            return;
        }

        string email = ReadNonEmpty("Enter Customer Email: ");

        if (!customersByEmail.ContainsKey(email))
        {
            Console.WriteLine("customer not found.\n");
            return;
        }

        Customer c = customersByEmail[email];

        List<Order> pending = new List<Order>();
        for (int i = 0; i < c.Orders.Count; i++)
        {
            if (NormalizeStatus(c.Orders[i].OrderStatus) == "PENDING")
                pending.Add(c.Orders[i]);
        }

        if (pending.Count == 0)
        {
            Console.WriteLine("no pending orders.\n");
            return;
        }

        Console.WriteLine("Pending Orders:");
        for (int i = 0; i < pending.Count; i++)
            Console.WriteLine(pending[i].OrderId);

        int orderId = ReadInt("Enter Order ID: ");

        Order target = null;
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].OrderId == orderId)
            {
                target = pending[i];
                break;
            }
        }

        if (target == null)
        {
            Console.WriteLine("invalid order id.\n");
            return;
        }

        Console.WriteLine($"Customer: {c.CustomerName}");
        Console.WriteLine("Ordered Items:");
        PrintOrderedItems(target);

        Console.WriteLine($"Delivery date/time: {target.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${target.OrderTotal:0.00}");
        Console.WriteLine($"Order Status: {target.OrderStatus}");

        bool yes = ReadYesNo("Confirm deletion? [Y/N]: ");
        if (!yes)
        {
            Console.WriteLine("deletion cancelled.\n");
            return;
        }

        target.OrderStatus = "Cancelled";
        refundStack.Push(target);

        Console.WriteLine($"Order {target.OrderId} cancelled. Refund of ${target.OrderTotal:0.00} processed.\n");
    }

    private static void PrintOrderForProcessing(Order o)
    {
        Console.WriteLine($"Order {o.OrderId}:");
        Console.WriteLine($"Customer Email: {o.CustomerEmail}");
        Console.WriteLine("Ordered Items:");
        PrintOrderedItems(o);
        Console.WriteLine($"Delivery date/time: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${o.OrderTotal:0.00}");
        Console.WriteLine($"Order Status: {o.OrderStatus}");
    }

    private static void PrintOrderedItems(Order o)
    {
        if (o.OrderedFoodItems == null || o.OrderedFoodItems.Count == 0)
        {
            Console.WriteLine("(no items)");
            return;
        }

        for (int i = 0; i < o.OrderedFoodItems.Count; i++)
        {
            OrderedFoodItem it = o.OrderedFoodItems[i];
            Console.WriteLine($"{i + 1}. {it.ItemName} - {it.QtyOrdered}");
        }
    }

    private static string FindFoodItemsFile(string dataDir)
    {
        string exact = Path.Combine(dataDir, "fooditems.csv");
        if (File.Exists(exact)) return exact;

        string alt = Path.Combine(dataDir, "fooditems - Copy.csv");
        if (File.Exists(alt)) return alt;

        string[] matches = Directory.GetFiles(dataDir, "fooditems*.csv");
        if (matches.Length > 0) return matches[0];

        return "";
    }

    private static string NormalizeStatus(string status)
    {
        return (status ?? "").Trim().ToUpperInvariant();
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

            int n;
            if (int.TryParse(s, out n)) return n;

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
            Console.WriteLine("please enter Y or N.");
        }
    }

    private static string TrimTo(string s, int maxLen)
    {
        if (s == null) s = "";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen);
    }
}
