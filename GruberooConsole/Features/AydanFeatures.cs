//==========================================================
// Student Number : S10273117G
// Student Name : Aydan Yeo
// Partner Name : Joseph Wong
//==========================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class AydanFeatures
{
    // feature 1: load files (restaurants + food items)
    public static void Feature1_LoadRestaurantsAndFoodItems(
        string dataDir,
        Dictionary<string, Restaurant> restaurantsById,
        out int restaurantsLoaded,
        out int foodItemsLoaded)
    {
        restaurantsLoaded = 0;
        foodItemsLoaded = 0;

        restaurantsById.Clear();

        string restaurantsPath = Path.Combine(dataDir, "restaurants.csv");
        string foodItemsPath = FindFoodItemsFile(dataDir);

        if (!File.Exists(restaurantsPath))
            throw new FileNotFoundException("Missing restaurants.csv at: " + restaurantsPath);

        if (!File.Exists(foodItemsPath))
            throw new FileNotFoundException("Missing fooditems.csv at: " + foodItemsPath);

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

            string restaurantId = p[0].Trim();
            string itemName = p[1].Trim();
            string desc = p[2].Trim();
            string priceStr = p[3].Trim();

            if (!restaurantsById.TryGetValue(restaurantId, out Restaurant restaurant))
                continue;

            if (!double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double price))
                price = 0;

            FoodItem fi = new FoodItem(itemName, desc, price);
            restaurant.GetOrCreateMainMenu().AddFoodItem(fi);

            foodItemsLoaded++;
        }
    }

    // feature 4: list all orders
    public static void Feature4_ListAllOrders(
        List<Order> allOrders,
        Dictionary<string, Customer> customersByEmail,
        Dictionary<string, Restaurant> restaurantsById)
    {
        Console.WriteLine("All Orders");
        Console.WriteLine("==========");

        Console.WriteLine("Order ID   Customer      Restaurant       Delivery Date/Time   Amount    Status");
        Console.WriteLine("--------   ----------    -------------    ------------------   ------    ---------");

        allOrders.Sort((a, b) => a.OrderId.CompareTo(b.OrderId));

        for (int i = 0; i < allOrders.Count; i++)
        {
            Order o = allOrders[i];

            string customerName = o.CustomerEmail;
            if (customersByEmail.TryGetValue(o.CustomerEmail, out Customer c))
                customerName = c.CustomerName;

            string restaurantName = o.RestaurantId;
            if (restaurantsById.TryGetValue(o.RestaurantId, out Restaurant r))
                restaurantName = r.Name;

            string dt = o.DeliveryDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            string amt = "$" + o.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);

            Console.WriteLine(
                $"{o.OrderId,-8}   {Trunc(customerName, 10),-10}    {Trunc(restaurantName, 13),-13}    {dt,-18}   {amt,-7}   {o.OrderStatus}"
            );
        }

        Console.WriteLine();
    }

    // feature 6: process an order
    public static void Feature6_ProcessOrder(
        Dictionary<string, Restaurant> restaurantsById,
        Dictionary<string, Customer> customersByEmail,
        Stack<Order> refundStack)
    {
        Console.WriteLine("Process Order");
        Console.WriteLine("=============");

        string rid = ReadNonEmpty("Enter Restaurant ID: ");

        if (!restaurantsById.TryGetValue(rid, out Restaurant restaurant))
        {
            Console.WriteLine("Restaurant not found.\n");
            return;
        }

        if (restaurant.OrderQueue.Count == 0)
        {
            Console.WriteLine("No orders in this restaurant queue.\n");
            return;
        }

        Order order = restaurant.OrderQueue.Dequeue();

        PrintOrderBlock(order, customersByEmail);

        Console.Write("[C]onfirm / [R]eject / [S]kip / [D]eliver: ");
        string action = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

        if (action == "C")
        {
            if (IsStatus(order, "Pending"))
            {
                order.OrderStatus = "Preparing";
                Console.WriteLine($"\nOrder {order.OrderId} confirmed. Status: {order.OrderStatus}\n");
                restaurant.OrderQueue.Enqueue(order);
            }
            else
            {
                Console.WriteLine("\nAction not allowed for this order status.\n");
                restaurant.OrderQueue.Enqueue(order);
            }
        }
        else if (action == "R")
        {
            if (IsStatus(order, "Pending"))
            {
                order.OrderStatus = "Rejected";
                PushRefundIfNotExists(refundStack, order);
                Console.WriteLine($"\nOrder {order.OrderId} rejected. Refund of ${order.OrderTotal:0.00} processed.\n");
            }
            else
            {
                Console.WriteLine("\nAction not allowed for this order status.\n");
                restaurant.OrderQueue.Enqueue(order);
            }
        }
        else if (action == "S")
        {
            if (IsStatus(order, "Cancelled"))
            {
                PushRefundIfNotExists(refundStack, order);
                Console.WriteLine($"\nOrder {order.OrderId} skipped.\n");
            }
            else
            {
                Console.WriteLine("\nAction not allowed for this order status.\n");
                restaurant.OrderQueue.Enqueue(order);
            }
        }
        else if (action == "D")
        {
            if (IsStatus(order, "Preparing"))
            {
                order.OrderStatus = "Delivered";
                Console.WriteLine($"\nOrder {order.OrderId} delivered. Status: {order.OrderStatus}\n");
            }
            else
            {
                Console.WriteLine("\nAction not allowed for this order status.\n");
                restaurant.OrderQueue.Enqueue(order);
            }
        }
        else
        {
            Console.WriteLine("\nInvalid option.\n");
            restaurant.OrderQueue.Enqueue(order);
        }
    }

    // feature 8: delete an existing order
    public static void Feature8_DeleteOrder(
        Dictionary<string, Customer> customersByEmail,
        Dictionary<string, Restaurant> restaurantsById,
        Stack<Order> refundStack)
    {
        Console.WriteLine("Delete Order");
        Console.WriteLine("============");

        string email = ReadNonEmpty("Enter Customer Email: ");

        if (!customersByEmail.TryGetValue(email, out Customer customer))
        {
            Console.WriteLine("Customer not found.\n");
            return;
        }

        List<Order> pending = new List<Order>();
        for (int i = 0; i < customer.Orders.Count; i++)
        {
            if (IsStatus(customer.Orders[i], "Pending"))
                pending.Add(customer.Orders[i]);
        }

        if (pending.Count == 0)
        {
            Console.WriteLine("No Pending orders.\n");
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
            Console.WriteLine("Invalid Order ID.\n");
            return;
        }

        PrintDeleteBlock(target, customer);

        Console.Write("Confirm deletion? [Y/N]: ");
        string confirm = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

        if (confirm != "Y")
        {
            Console.WriteLine("Deletion cancelled.\n");
            return;
        }

        target.OrderStatus = "Cancelled";
        PushRefundIfNotExists(refundStack, target);

        if (restaurantsById.TryGetValue(target.RestaurantId, out Restaurant r))
            RemoveFromQueueByOrderId(r, target.OrderId);

        Console.WriteLine($"\nOrder {target.OrderId} cancelled. Refund of ${target.OrderTotal:0.00} processed.\n");
    }

    private static void PrintOrderBlock(Order o, Dictionary<string, Customer> customersByEmail)
    {
        string custName = o.CustomerEmail;
        if (customersByEmail.TryGetValue(o.CustomerEmail, out Customer c))
            custName = c.CustomerName;

        Console.WriteLine();
        Console.WriteLine($"Order {o.OrderId}:");
        Console.WriteLine($"Customer: {custName}");
        Console.WriteLine("Ordered Items:");

        for (int i = 0; i < o.OrderedFoodItems.Count; i++)
            Console.WriteLine($"{i + 1}. {o.OrderedFoodItems[i].ItemName} - {o.OrderedFoodItems[i].QtyOrdered}");

        Console.WriteLine($"Delivery date/time: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${o.OrderTotal:0.00}");
        Console.WriteLine($"Order Status: {o.OrderStatus}");
    }

    private static void PrintDeleteBlock(Order o, Customer customer)
    {
        Console.WriteLine();
        Console.WriteLine($"Customer: {customer.CustomerName}");
        Console.WriteLine("Ordered Items:");

        for (int i = 0; i < o.OrderedFoodItems.Count; i++)
            Console.WriteLine($"{i + 1}. {o.OrderedFoodItems[i].ItemName} - {o.OrderedFoodItems[i].QtyOrdered}");

        Console.WriteLine($"Delivery date/time: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${o.OrderTotal:0.00}");
        Console.WriteLine($"Order Status: {o.OrderStatus}");
    }

    private static string FindFoodItemsFile(string dataDir)
    {
        string p1 = Path.Combine(dataDir, "fooditems.csv");
        if (File.Exists(p1)) return p1;

        string p2 = Path.Combine(dataDir, "fooditems - Copy.csv");
        if (File.Exists(p2)) return p2;

        return p1;
    }

    private static bool IsStatus(Order o, string expected)
    {
        return string.Equals(o.OrderStatus, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveFromQueueByOrderId(Restaurant r, int orderId)
    {
        int n = r.OrderQueue.Count;
        for (int i = 0; i < n; i++)
        {
            Order o = r.OrderQueue.Dequeue();
            if (o.OrderId != orderId)
                r.OrderQueue.Enqueue(o);
        }
    }

    private static void PushRefundIfNotExists(Stack<Order> stack, Order order)
    {
        foreach (var o in stack)
        {
            if (o.OrderId == order.OrderId)
                return;
        }
        stack.Push(order);
    }

    private static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim();
            if (s.Length > 0) return s;
            Console.WriteLine("Value cannot be empty.\n");
        }
    }

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim();

            if (int.TryParse(s, out int v))
                return v;

            Console.WriteLine("Invalid number. Try again.\n");
        }
    }

    private static string Trunc(string s, int maxLen)
    {
        if (s == null) return "";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen);
    }
}

