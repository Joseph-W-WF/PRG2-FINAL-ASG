//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 
using System;
using System.Collections.Generic;
using System.Globalization;

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

        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine("orders loaded into Customer Order List and Restaurant Order Queue.\n");

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

            foreach (var menu in r.Menus)
            {
                foreach (var f in menu.FoodItems)
                {
                    Console.WriteLine($"- {f.ItemName}: {f.Description} - ${f.Price:0.00}");
                }
            }
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
        Console.WriteLine();
        Console.WriteLine("Create New Order");
        Console.WriteLine("================");

        Customer customer = PromptCustomer(customers);
        if (customer == null) return;

        Restaurant restaurant = PromptRestaurant(restaurants);
        if (restaurant == null) return;

        DateTime deliveryDT = PromptDeliveryDateTime();
        string address = ReadNonEmpty("Enter Delivery Address: ");

        // collect menu items
        var available = GetAllFoodItems(restaurant);
        if (available.Count == 0)
        {
            Console.WriteLine("No food items available.\n");
            return;
        }

        Console.WriteLine("Available Food Items:");
        for (int i = 0; i < available.Count; i++)
            Console.WriteLine($"{i + 1}. {available[i].ItemName} - ${available[i].Price:0.00}");

        int newOrderId = GetNextOrderId(customers);

        var order = new Order(newOrderId, "Draft", address, "");
        order.CustomerEmail = customer.EmailAddress;
        order.RestaurantId = restaurant.RestaurantId;
        order.OrderDateTime = DateTime.Now;
        order.DeliveryDateTime = deliveryDT;

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

        Console.WriteLine("Payment method:");
        Console.WriteLine("[CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery:");
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

        Customer customer = PromptCustomer(customers);
        if (customer == null) return;

        var pending = new List<Order>();
        foreach (var o in customer.Orders)
            if (string.Equals(o.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                pending.Add(o);

        if (pending.Count == 0)
        {
            Console.WriteLine("No Pending orders.\n");
            return;
        }

        Console.WriteLine("Pending Orders:");
        foreach (var o in pending) Console.WriteLine(o.OrderId);

        int orderId = ReadInt("Enter Order ID: ", 1, int.MaxValue);

        Order order = null;
        foreach (var o in pending)
            if (o.OrderId == orderId) { order = o; break; }

        if (order == null)
        {
            Console.WriteLine("Invalid Order ID.\n");
            return;
        }

        Restaurant restaurant = null;
        foreach (var r in restaurants)
            if (string.Equals(r.RestaurantId, order.RestaurantId, StringComparison.OrdinalIgnoreCase))
            { restaurant = r; break; }

        if (restaurant == null)
        {
            Console.WriteLine("Restaurant not found for this order.\n");
            return;
        }

        PrintOrderDetails(order);

        Console.Write("Modify: [1] Items [2] Address [3] Delivery Time: ");
        string choice = (Console.ReadLine() ?? "").Trim();

        double oldTotal = order.OrderTotal;
        string oldAddr = order.DeliveryAddress;
        DateTime oldDelivery = order.DeliveryDateTime;
        var oldItems = CloneItems(order.OrderedFoodItems);

        if (choice == "1")
        {
            ModifyItems(order, restaurant);
        }
        else if (choice == "2")
        {
            order.DeliveryAddress = ReadNonEmpty("Enter new Address: ");
        }
        else if (choice == "3")
        {
            order.DeliveryDateTime = PromptNewTimeSameDate(order.DeliveryDateTime);
        }
        else
        {
            Console.WriteLine("Invalid option.\n");
            return;
        }

        order.RecalculateTotalWithDelivery(DELIVERY_FEE);

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

        // optional but useful: persist modifications back to orders.csv
        OrderCsvStore.RewriteAllOrders(ordersPath, customers);

        Console.WriteLine("Order updated.\n");
        PrintOrderDetails(order);
    }

    // =========================
    // Helpers
    // =========================
    private static Customer PromptCustomer(List<Customer> customers)
    {
        string email = ReadNonEmpty("Enter Customer Email: ");

        foreach (var c in customers)
        {
            if (string.Equals(c.EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        Console.WriteLine("Customer not found.\n");
        return null;
    }

    private static Restaurant PromptRestaurant(List<Restaurant> restaurants)
    {
        string rid = ReadNonEmpty("Enter Restaurant ID: ");

        foreach (var r in restaurants)
        {
            if (string.Equals(r.RestaurantId, rid, StringComparison.OrdinalIgnoreCase))
                return r;
        }

        Console.WriteLine("Restaurant not found.\n");
        return null;
    }

    private static List<FoodItem> GetAllFoodItems(Restaurant restaurant)
    {
        var list = new List<FoodItem>();
        foreach (var m in restaurant.Menus)
            list.AddRange(m.FoodItems);
        return list;
    }

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

            int idx = -1;
            for (int i = 0; i < order.OrderedFoodItems.Count; i++)
            {
                if (string.Equals(order.OrderedFoodItems[i].ItemName, fi.ItemName, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

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

    private static List<OrderedFoodItem> CloneItems(List<OrderedFoodItem> items)
    {
        var copy = new List<OrderedFoodItem>();
        foreach (var it in items)
            copy.Add(new OrderedFoodItem(it.ItemName, it.Description, it.Price, it.QtyOrdered));
        return copy;
    }
}
