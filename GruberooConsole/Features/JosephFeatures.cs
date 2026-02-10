//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class JosephFeatures
{
    private const double DELIVERY_FEE = 5.00;

    // ---------------------------
    // FEATURE 2: Load customers + orders
    // ---------------------------
    public static List<Customer> Feature2_LoadCustomersAndOrders(
        string customersPath,
        string ordersPath,
        List<Restaurant> restaurants)
    {
        var customers = FileLoader.LoadCustomers(customersPath);
        OrderLoader.LoadOrders(ordersPath, customers, restaurants);
        return customers;
    }

    // ---------------------------
    // FEATURE 3: List all restaurants and menu items
    // ---------------------------
    public static void Feature3_ListAllRestaurantsAndMenuItems(List<Restaurant> restaurants)
    {
        Console.WriteLine();
        Console.WriteLine("All Restaurants and Menu Items");
        Console.WriteLine("==============================");

        foreach (var r in restaurants)
        {
            Console.WriteLine($"Restaurant: {r.Name} ({r.RestaurantId})");

            foreach (var f in GetAllFoodItems(r))
                Console.WriteLine($"- {f.ItemName}: {f.Description} - ${f.Price:0.00}");

            Console.WriteLine("");
        }

        Console.WriteLine();
    }

    // ---------------------------
    // FEATURE 5: Create a new order
    // ---------------------------
    public static void Feature5_CreateNewOrder(
        List<Customer> customers,
        List<Restaurant> restaurants,
        string ordersPath)
    {
        Console.WriteLine("Create New Order");
        Console.WriteLine("================");

        var customer = PromptCustomer(customers);
        if (customer == null) return;

        var restaurant = PromptRestaurant(restaurants);
        if (restaurant == null) return;

        DateTime deliveryDT = PromptDeliveryDateTime();
        string address = ReadNonEmpty("Enter Delivery Address: ");

        var available = GetAllFoodItems(restaurant);
        if (available.Count == 0)
        {
            Console.WriteLine("No food items available.\n");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("Available Food Items:");
        for (int i = 0; i < available.Count; i++)
            Console.WriteLine($"{i + 1}. {available[i].ItemName} - ${available[i].Price:0.00}");

        int newOrderId = GetNextOrderId(customers);

        var order = new Order(newOrderId, "Draft", address, "")
        {
            CustomerEmail = customer.EmailAddress,
            RestaurantId = restaurant.RestaurantId,
            OrderDateTime = DateTime.Now,
            DeliveryDateTime = deliveryDT
        };

        while (true)
        {
            int itemNo = ReadInt("Enter item number (0 to finish): ", 0, available.Count);
            if (itemNo == 0) break;

            int qty = ReadInt("Enter quantity: ", 1, 99);

            var fi = available[itemNo - 1];
            order.OrderedFoodItems.Add(new OrderedFoodItem(fi.ItemName, fi.Description, fi.Price, qty));
        }

        if (order.OrderedFoodItems.Count == 0)
        {
            Console.WriteLine("No items selected. Exiting feature.\n");
            return;
        }

        Console.Write("Add special request? [Y/N]: ");
        Console.WriteLine();
        string sr = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
        if (sr == "Y")
            order.SpecialRequest = ReadNonEmpty("Enter special request: ");

        double itemsTotal = order.CalculateItemsTotal();
        double finalTotal = itemsTotal + DELIVERY_FEE;
        order.OrderTotal = finalTotal;

        Console.WriteLine($"Order Total: ${itemsTotal:0.00} + ${DELIVERY_FEE:0.00} (delivery) = ${finalTotal:0.00}");

        Console.Write("Proceed to payment? [Y/N]: ");
        string pay = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
        if (pay != "Y")
        {
            Console.WriteLine("Payment cancelled. Exiting feature.\n");
            return;
        }

        Console.Write("Payment method:");
        Console.Write("[CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery:");
        string method = ReadPaymentMethod();

        order.OrderPaymentMethod = method;
        order.OrderPaid = true;
        order.OrderStatus = "Pending";

        customer.AddOrder(order);
        restaurant.OrderQueue.Enqueue(order);

        OrderCsvStore.AppendOrder(ordersPath, order);

        Console.WriteLine();
        Console.WriteLine($"Order {order.OrderId} created successfully! Status: {order.OrderStatus}\n");
    }

    // ---------------------------
    // FEATURE 7: Modify an existing order (Pending only)
    // ---------------------------
    public static void Feature7_ModifyExistingOrder(
    List<Customer> customers,
    List<Restaurant> restaurants,
    string ordersPath)
    {
        Console.WriteLine();
        Console.WriteLine("Modify Order");
        Console.WriteLine("============");

        var customer = PromptCustomer(customers);
        if (customer == null) return;

        var pending = customer.Orders
            .Where(o => string.Equals(o.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pending.Count == 0)
        {
            Console.WriteLine("No Pending orders.\n");
            return;
        }

        Console.WriteLine("Pending Orders:");
        foreach (var o in pending) Console.WriteLine(o.OrderId);

        int orderId = ReadInt("Enter Order ID: ", 1, int.MaxValue);

        var order = pending.FirstOrDefault(o => o.OrderId == orderId);
        if (order == null)
        {
            Console.WriteLine("Invalid Order ID.\n");
            return;
        }

        var restaurant = restaurants.FirstOrDefault(r =>
            string.Equals(r.RestaurantId, order.RestaurantId, StringComparison.OrdinalIgnoreCase));

        if (restaurant == null)
        {
            Console.WriteLine("Restaurant not found for this order.\n");
            return;
        }

        // show current details (same as sample flow)
        PrintOrderDetails(order);

        Console.Write("Modify: [1] Items [2] Address [3] Delivery Time: ");
        string choice = (Console.ReadLine() ?? "").Trim();

        // snapshot for rollback + for “what changed” messages
        double oldTotal = order.OrderTotal;
        string oldAddr = order.DeliveryAddress;
        DateTime oldDelivery = order.DeliveryDateTime;
        var oldItems = CloneItems(order.OrderedFoodItems);

        // we’ll build the exact “updated…” message based on the choice
        string updatedMessage;

        if (choice == "1")
        {
            ModifyItems(order, restaurant);
            updatedMessage = $"Order {order.OrderId} updated. Items updated.";
        }
        else if (choice == "2")
        {
            order.DeliveryAddress = ReadNonEmpty("Enter new Address: ");
            updatedMessage = $"Order {order.OrderId} updated. New Address: {order.DeliveryAddress}";
        }
        else if (choice == "3")
        {
            order.DeliveryDateTime = PromptNewTimeSameDate(order.DeliveryDateTime);
            string newTime = order.DeliveryDateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            // matches the sample style: “New Delivery Time: 14:00”
            updatedMessage = $"Order {order.OrderId} updated. New Delivery Time: {newTime}";
        }
        else
        {
            Console.WriteLine("Invalid option.\n");
            return;
        }

        // recalc after changes
        order.RecalculateTotalWithDelivery(DELIVERY_FEE);

        // if total increased, prompt to pay (keep your existing behaviour)
        if (order.OrderTotal > oldTotal + 0.0001)
        {
            Console.WriteLine($"Order total increased: ${oldTotal:0.00} -> ${order.OrderTotal:0.00}");
            Console.Write("Pay the difference? [Y/N]: ");
            string pay = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (pay != "Y")
            {
                // rollback
                order.OrderTotal = oldTotal;
                order.DeliveryAddress = oldAddr;
                order.DeliveryDateTime = oldDelivery;
                order.OrderedFoodItems = oldItems;

                Console.WriteLine("Update cancelled.\n");
                return;
            }

            Console.WriteLine("Payment method:");
            Console.WriteLine("[CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery:");
            order.OrderPaymentMethod = ReadPaymentMethod();
            order.OrderPaid = true;
        }

        // persist
        OrderCsvStore.RewriteAllOrders(ordersPath, customers);

        // ✅ now prints “updated” + what exactly was updated (like the sample wants)
        Console.WriteLine(updatedMessage);

        // optional but matches the spec line “display updated order details”
        PrintOrderDetails(order);
    }


    // ---------------------------
    // ADVANCED FEATURE (a): Bulk processing of unprocessed orders for current day
    // ---------------------------
    public static void AdvancedA_BulkProcessPendingOrdersForToday(
        List<Restaurant> restaurants,
        string ordersPath,
        List<Customer> customers)
    {
        Console.WriteLine();
        Console.WriteLine("Bulk Process Pending Orders (Today)");
        Console.WriteLine("===================================");

        DateTime now = DateTime.Now;
        DateTime today = now.Date;

        // 1) identify all orders with status "Pending" (in order queues)
        int totalPendingInQueues = 0;
        int processed = 0;
        int preparing = 0;
        int rejected = 0;

        // Only process "current day" pending orders (delivery date = today)
        var toProcess = new List<Order>();

        foreach (var r in restaurants)
        {
            foreach (var o in r.OrderQueue) // Queue<T> can be enumerated
            {
                if (string.Equals(o.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    totalPendingInQueues++;

                    if (o.DeliveryDateTime.Date == today)
                        toProcess.Add(o);
                }
            }
        }

        // 2) display total number in the Order Queues with this status
        Console.WriteLine($"Total Pending orders in all restaurant queues: {totalPendingInQueues}");

        if (toProcess.Count == 0)
        {
            Console.WriteLine("No Pending orders to bulk process for today.\n");
            return;
        }

        // 3) for each order: attempt to process it
        // - Rejected if delivery time is less than 1 hour
        // - else Preparing
        foreach (var o in toProcess)
        {
            bool lessThanOneHour = (o.DeliveryDateTime - now) < TimeSpan.FromHours(1);

            if (lessThanOneHour)
            {
                o.OrderStatus = "Rejected";
                rejected++;
            }
            else
            {
                o.OrderStatus = "Preparing";
                preparing++;
            }

            processed++;
        }

        // Optional but useful: persist statuses back into orders.csv
        // (so restart of program keeps the updated statuses)
        OrderCsvStore.RewriteAllOrders(ordersPath, customers);

        // 4) display summary statistics
        double percent = (totalPendingInQueues == 0)
            ? 0.0
            : (processed * 100.0 / totalPendingInQueues);

        Console.WriteLine();
        Console.WriteLine("Summary");
        Console.WriteLine("-------");
        Console.WriteLine($"Orders processed: {processed}");
        Console.WriteLine($"Preparing: {preparing}");
        Console.WriteLine($"Rejected: {rejected}");
        Console.WriteLine($"% processed against all Pending orders: {percent:0.00}%");
        Console.WriteLine();
    }


    // =========================
    // Helpers
    // =========================
    private static Customer PromptCustomer(List<Customer> customers)
    {
        string email = ReadNonEmpty("Enter Customer Email: ");

        var customer = customers.FirstOrDefault(c =>
            string.Equals(c.EmailAddress, email, StringComparison.OrdinalIgnoreCase));

        if (customer != null) return customer;

        Console.WriteLine("Customer not found.\n");
        return null;
    }

    private static Restaurant PromptRestaurant(List<Restaurant> restaurants)
    {
        string rid = ReadNonEmpty("Enter Restaurant ID: ");

        var restaurant = restaurants.FirstOrDefault(r =>
            string.Equals(r.RestaurantId, rid, StringComparison.OrdinalIgnoreCase));

        if (restaurant != null) return restaurant;

        Console.WriteLine("Restaurant not found.\n");
        return null;
    }

    private static List<FoodItem> GetAllFoodItems(Restaurant restaurant) =>
        restaurant.Menus.SelectMany(m => m.FoodItems).ToList();

    private static void ModifyItems(Order order, Restaurant restaurant)
    {
        var available = GetAllFoodItems(restaurant);

        Console.WriteLine("Enter item number to change qty (0 to finish). Qty=0 removes item.");

        while (true)
        {
            for (int i = 0; i < available.Count; i++)
                Console.WriteLine($"{i + 1}. {available[i].ItemName} - ${available[i].Price:0.00}");

            int itemNo = ReadInt("Item number (0 to finish): ", 0, available.Count);
            if (itemNo == 0) break;

            int qty = ReadInt("New quantity (0 to remove): ", 0, 99);

            var fi = available[itemNo - 1];

            int idx = order.OrderedFoodItems.FindIndex(ofi =>
                string.Equals(ofi.ItemName, fi.ItemName, StringComparison.OrdinalIgnoreCase));

            if (qty == 0)
            {
                if (idx >= 0) order.OrderedFoodItems.RemoveAt(idx);
            }
            else
            {
                if (idx >= 0)
                    order.OrderedFoodItems[idx].QtyOrdered = qty;
                else
                    order.OrderedFoodItems.Add(new OrderedFoodItem(fi.ItemName, fi.Description, fi.Price, qty));
            }
        }
    }

    private static void PrintOrderDetails(Order o)
    {
        Console.WriteLine($"Order Items:");
        for (int i = 0; i < o.OrderedFoodItems.Count; i++)
            Console.WriteLine($"{i + 1}. {o.OrderedFoodItems[i].ItemName} - {o.OrderedFoodItems[i].QtyOrdered}");

        Console.WriteLine("Address:");
        Console.WriteLine(o.DeliveryAddress);

        Console.WriteLine("Delivery Date/Time:");
        Console.WriteLine($"{o.DeliveryDateTime:dd/M/yyyy, HH:mm}");

        Console.WriteLine($"Total Amount: ${o.OrderTotal:0.00}");
        Console.WriteLine($"Order Status: {o.OrderStatus}");
        Console.WriteLine();
    }

    private static DateTime PromptDeliveryDateTime()
    {
        while (true)
        {
            string d = ReadNonEmpty("Enter Delivery Date (dd/mm/yyyy): ");
            string t = ReadNonEmpty("Enter Delivery Time (hh:mm): ");

            if (DateTime.TryParseExact(
                d + " " + t,
                new[] { "dd/MM/yyyy HH:mm", "d/M/yyyy HH:mm" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dt))
                return dt;

            Console.WriteLine("Invalid delivery date/time. Try again.\n");
        }
    }

    private static DateTime PromptNewTimeSameDate(DateTime oldDT)
    {
        while (true)
        {
            string t = ReadNonEmpty("Enter new Delivery Time (hh:mm): ");
            if (TimeSpan.TryParseExact(t, "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan ts))
                return oldDT.Date + ts;

            Console.WriteLine("Invalid time. Try again.\n");
        }
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

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim();

            if (int.TryParse(s, out int v) && v >= min && v <= max)
                return v;

            Console.WriteLine("Invalid number. Try again.\n");
        }
    }

    private static string ReadPaymentMethod()
    {
        while (true)
        {
            string m = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            if (m == "CC" || m == "PP" || m == "CD") return m;
            Console.Write("Invalid. Enter CC / PP / CD: ");
        }
    }

    private static int GetNextOrderId(List<Customer> customers)
    {
        int max = 1000;
        foreach (var c in customers)
            foreach (var o in c.Orders)
                if (o.OrderId > max) max = o.OrderId;

        return max + 1;
    }

    private static List<OrderedFoodItem> CloneItems(List<OrderedFoodItem> items) =>
        items.Select(it => new OrderedFoodItem(it.ItemName, it.Description, it.Price, it.QtyOrdered)).ToList();
}
