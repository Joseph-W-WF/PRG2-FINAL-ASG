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
    // constants used by advanced features
    private const double DELIVERY_FEE = 5.00;
    private const double GRUBEROO_FEE_RATE = 0.30; // 30%

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

        
        if (!File.Exists(restaurantsPath) || !File.Exists(foodItemsPath))
        {
            Console.WriteLine("[ERROR] Missing required CSV files in Data-Files folder.");
            Console.WriteLine($"- restaurants.csv: {restaurantsPath} {(File.Exists(restaurantsPath) ? "(found)" : "(missing)")}");
            Console.WriteLine($"- fooditems.csv:   {foodItemsPath} {(File.Exists(foodItemsPath) ? "(found)" : "(missing)")}");
            restaurantsLoaded = 0;
            foodItemsLoaded = 0;
            return;
        }

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
            Console.WriteLine("Restaurant not found.");
            Console.WriteLine();
            return;
        }

        if (restaurant.OrderQueue.Count == 0)
        {
            Console.WriteLine("No orders in this restaurant queue.");
            Console.WriteLine();
            return;
        }

        // Loop through the queue ONCE (FIFO), so each current order is shown once.
        int ordersToProcess = restaurant.OrderQueue.Count;

        for (int i = 0; i < ordersToProcess; i++)
        {
            Order order = restaurant.OrderQueue.Dequeue();

            PrintOrderBlock(order, customersByEmail);

            // Only these states are meant for processing in the writeup:
            // Pending -> Confirm/Reject
            // Cancelled -> Skip
            // Preparing -> Deliver
            bool isPending = IsStatus(order, "Pending");
            bool isCancelled = IsStatus(order, "Cancelled");
            bool isPreparing = IsStatus(order, "Preparing");

            // If this order is already completed (e.g., Delivered / Rejected), just move on.
            if (!isPending && !isCancelled && !isPreparing)
            {
                Console.WriteLine();
                Console.WriteLine($"Order {order.OrderId} is already {order.OrderStatus}. Skipping.");
                Console.WriteLine();
                continue; // do not put completed orders back into the queue
            }

            string action;
            while (true)
            {
                Console.Write("[C]onfirm / [R]eject / [S]kip / [D]eliver: ");
                action = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

                bool ok =
                    (isPending && (action == "C" || action == "R")) ||
                    (isCancelled && action == "S") ||
                    (isPreparing && action == "D");

                if (ok) break;

                Console.WriteLine("Invalid option for this order status. Try again.");
                Console.WriteLine();
            }

            // Apply the action
            if (action == "C") // Pending -> Preparing (stays in queue)
            {
                order.OrderStatus = "Preparing";
                Console.WriteLine();
                Console.WriteLine($"Order {order.OrderId} confirmed. Status: {order.OrderStatus}");
                Console.WriteLine();

                restaurant.OrderQueue.Enqueue(order);
            }
            else if (action == "R") // Pending -> Rejected (refund stack)
            {
                order.OrderStatus = "Rejected";
                PushRefundIfNotExists(refundStack, order);

                Console.WriteLine();
                Console.WriteLine($"Order {order.OrderId} rejected. Refund of ${order.OrderTotal:0.00} processed.");
                Console.WriteLine();
            }
            else if (action == "S") // Cancelled -> skip (move on)
            {
                // (Cancelled orders should already be refunded when cancelled; stack is for record.)
                PushRefundIfNotExists(refundStack, order);

                Console.WriteLine();
                Console.WriteLine($"Order {order.OrderId} skipped.");
                Console.WriteLine();
            }
            else if (action == "D") // Preparing -> Delivered (done)
            {
                order.OrderStatus = "Delivered";

                Console.WriteLine();
                Console.WriteLine($"Order {order.OrderId} delivered. Status: {order.OrderStatus}");
                Console.WriteLine();
            }
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


        Console.WriteLine($"\nOrder {target.OrderId} cancelled. Refund of ${target.OrderTotal:0.00} processed.\n");
    }

    // ==========================================
    // ADVANCED FEATURE (B): Display total order amount
    // ==========================================
    public static void AdvancedB_DisplayTotalOrderAmount(List<Restaurant> restaurants, List<Customer> customers)
    {
        Console.WriteLine();
        Console.WriteLine("Display Total Order Amount");
        Console.WriteLine("==========================");

        
        Dictionary<int, Order> uniqueOrders = new Dictionary<int, Order>();
        foreach (var c in customers)
        {
            foreach (var o in c.Orders)
            {
                if (!uniqueOrders.ContainsKey(o.OrderId))
                    uniqueOrders[o.OrderId] = o;
            }
        }

        double grandDeliveredNet = 0.0; 
        double grandRefunds = 0.0;      

        Console.WriteLine("Restaurant                         Delivered (excl. delivery)        Refunds");
        Console.WriteLine("--------------------------------  -------------------------------   ----------------");

        foreach (var rest in restaurants)
        {
            int deliveredCount = 0;
            double deliveredNet = 0.0;

            int refundCount = 0;
            double refunds = 0.0;

            foreach (var kv in uniqueOrders)
            {
                Order o = kv.Value;

                if (!string.Equals(o.RestaurantId, rest.RestaurantId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(o.OrderStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    deliveredCount++;
                    double net = o.OrderTotal - DELIVERY_FEE;
                    if (net < 0) net = 0;
                    deliveredNet += net;
                }
                else if (IsRefundStatus(o))
                {
                    refundCount++;
                    refunds += o.OrderTotal;
                }
            }

            grandDeliveredNet += deliveredNet;
            grandRefunds += refunds;

            string left = $"{Trunc(rest.Name, 28)} ({rest.RestaurantId})";
            string deliveredCell = $"{deliveredCount,3} order(s)  ${deliveredNet,10:0.00}";
            string refundCell = $"{refundCount,3} order(s)  ${refunds,10:0.00}";

            Console.WriteLine($"{left,-32}  {deliveredCell,-31}   {refundCell}");
        }

        Console.WriteLine();
        Console.WriteLine("Overall");
        Console.WriteLine("-------");
        Console.WriteLine($"Total order amount (Delivered, excl. delivery): ${grandDeliveredNet:0.00}");
        Console.WriteLine($"Total refunds (Rejected/Cancelled):            ${grandRefunds:0.00}");


        double finalEarned = grandDeliveredNet * GRUBEROO_FEE_RATE;
        Console.WriteLine($"Final amount Gruberoo earns:                   ${finalEarned:0.00}");
        Console.WriteLine();
    }

    // ==========================================
    // ADVANCED FEATURE (C): Favourite orders (quick reorder)
    // ==========================================
    public static void AdvancedC_FavouriteOrders(
        string dataDir,
        List<Customer> customers,
        List<Restaurant> restaurants,
        string ordersPath)
    {
        string favPath = Path.Combine(dataDir, "favourites.csv");
        EnsureFavouritesCsv(favPath);

        while (true)
        {
            Console.WriteLine("Favourite Orders");
            Console.WriteLine("===============");
            Console.WriteLine("1. Save favourite from an existing order");
            Console.WriteLine("2. View favourites");
            Console.WriteLine("3. Reorder favourite");
            Console.WriteLine("4. Delete favourite");
            Console.WriteLine("0. Back");
            Console.Write("Enter choice: ");

            string choice = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();

            if (choice == "0") break;

            if (choice == "1")
            {
                var customer = PromptCustomerByEmail(customers);
                if (customer == null) continue;

                if (customer.Orders.Count == 0)
                {
                    Console.WriteLine("This customer has no orders to save.\n");
                    continue;
                }

                Console.WriteLine("Orders:");
                for (int i = 0; i < customer.Orders.Count; i++)
                {
                    Order o = customer.Orders[i];
                    Console.WriteLine($"- {o.OrderId} | {o.OrderStatus} | {o.RestaurantId} | ${o.OrderTotal:0.00}");
                }

                int orderId = ReadInt("Enter Order ID to save as favourite: ");
                Order target = FindOrderById(customer, orderId);
                if (target == null)
                {
                    Console.WriteLine("Invalid Order ID.\n");
                    continue;
                }

                if (target.OrderedFoodItems.Count == 0)
                {
                    Console.WriteLine("That order has no items, cannot save as favourite.\n");
                    continue;
                }

                string favName = ReadNonEmpty("Enter favourite name (e.g. my usual lunch): ");

                List<FavouriteRecord> all = LoadFavourites(favPath);
                int newId = NextFavouriteId(all);

                var fav = new FavouriteRecord
                {
                    FavouriteId = newId,
                    CustomerEmail = customer.EmailAddress,
                    FavouriteName = favName,
                    RestaurantId = target.RestaurantId,
                    DefaultAddress = target.DeliveryAddress,
                    DefaultPaymentMethod = target.OrderPaymentMethod,
                    SpecialRequest = target.SpecialRequest
                };

                for (int i = 0; i < target.OrderedFoodItems.Count; i++)
                {
                    var it = target.OrderedFoodItems[i];
                    fav.Items.Add(new FavouriteItem(it.ItemName, it.QtyOrdered));
                }

                all.Add(fav);
                SaveAllFavourites(favPath, all);

                Console.WriteLine($"Saved favourite #{fav.FavouriteId}: {fav.FavouriteName}\n");
            }
            else if (choice == "2")
            {
                var customer = PromptCustomerByEmail(customers);
                if (customer == null) continue;

                List<FavouriteRecord> favs = LoadFavouritesForCustomer(favPath, customer.EmailAddress);
                if (favs.Count == 0)
                {
                    Console.WriteLine("No favourites found.\n");
                    continue;
                }

                PrintFavouriteList(favs, restaurants);
                Console.WriteLine();
            }
            else if (choice == "3")
            {
                var customer = PromptCustomerByEmail(customers);
                if (customer == null) continue;

                List<FavouriteRecord> favs = LoadFavouritesForCustomer(favPath, customer.EmailAddress);
                if (favs.Count == 0)
                {
                    Console.WriteLine("No favourites found.\n");
                    continue;
                }

                PrintFavouriteList(favs, restaurants);
                int favId = ReadInt("Enter Favourite ID to reorder: ");

                FavouriteRecord chosen = null;
                for (int i = 0; i < favs.Count; i++)
                {
                    if (favs[i].FavouriteId == favId)
                    {
                        chosen = favs[i];
                        break;
                    }
                }

                if (chosen == null)
                {
                    Console.WriteLine("Invalid Favourite ID.\n");
                    continue;
                }

                Restaurant rest = FindRestaurantById(restaurants, chosen.RestaurantId);
                if (rest == null)
                {
                    Console.WriteLine("Restaurant not found for this favourite (maybe removed).\n");
                    continue;
                }

                DateTime deliveryDT = PromptDeliveryDateTime();

                string address = chosen.DefaultAddress;
                if (!string.IsNullOrWhiteSpace(address))
                {
                    Console.Write($"Use saved address? ({address}) [Y/N]: ");
                    string ans = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                    if (ans != "Y")
                        address = ReadNonEmpty("Enter Delivery Address: ");
                }
                else
                {
                    address = ReadNonEmpty("Enter Delivery Address: ");
                }

                int newOrderId = GetNextOrderId(customers);
                var order = new Order(newOrderId, "Draft", address, "")
                {
                    CustomerEmail = customer.EmailAddress,
                    RestaurantId = rest.RestaurantId,
                    OrderDateTime = DateTime.Now,
                    DeliveryDateTime = deliveryDT
                };

                // rebuild items from menu (so price stays current)
                List<FoodItem> menuItems = GetAllFoodItems(rest);
                for (int i = 0; i < chosen.Items.Count; i++)
                {
                    var favItem = chosen.Items[i];
                    var menuItem = FindFoodItemByName(menuItems, favItem.ItemName);
                    if (menuItem == null)
                    {
                        Console.WriteLine($"[WARN] Item not found in menu anymore: {favItem.ItemName} (skipped)");
                        continue;
                    }

                    order.OrderedFoodItems.Add(new OrderedFoodItem(
                        menuItem.ItemName,
                        menuItem.Description,
                        menuItem.Price,
                        favItem.Qty
                    ));
                }

                if (order.OrderedFoodItems.Count == 0)
                {
                    Console.WriteLine("Cannot reorder because none of the items exist in the menu now.\n");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(chosen.SpecialRequest))
                {
                    Console.Write($"Use saved special request? ({chosen.SpecialRequest}) [Y/N]: ");
                    string ans = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                    if (ans == "Y")
                        order.SpecialRequest = chosen.SpecialRequest;
                    else
                    {
                        Console.Write("Add new special request? [Y/N]: ");
                        string sr = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                        if (sr == "Y")
                            order.SpecialRequest = ReadNonEmpty("Enter special request: ");
                    }
                }
                else
                {
                    Console.Write("Add special request? [Y/N]: ");
                    string sr = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                    if (sr == "Y")
                        order.SpecialRequest = ReadNonEmpty("Enter special request: ");
                }

                double itemsTotal = order.CalculateItemsTotal();
                order.OrderTotal = itemsTotal + DELIVERY_FEE;

                Console.WriteLine();
                Console.WriteLine($"New Order Total: ${itemsTotal:0.00} + ${DELIVERY_FEE:0.00} (delivery) = ${order.OrderTotal:0.00}");
                Console.Write("Proceed to payment? [Y/N]: ");
                string pay = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                if (pay != "Y")
                {
                    Console.WriteLine("Payment cancelled. Reorder aborted.\n");
                    continue;
                }

                string method = chosen.DefaultPaymentMethod;
                if (!string.IsNullOrWhiteSpace(method))
                {
                    Console.Write($"Use saved payment method ({method})? [Y/N]: ");
                    string ans = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                    if (ans != "Y")
                    {
                        Console.Write("Payment method: [CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery: ");
                        method = ReadPaymentMethod();
                    }
                }
                else
                {
                    Console.Write("Payment method: [CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery: ");
                    method = ReadPaymentMethod();
                }

                order.OrderPaymentMethod = method;
                order.OrderPaid = true;
                order.OrderStatus = "Pending";

                customer.AddOrder(order);
                rest.OrderQueue.Enqueue(order);
                OrderCsvStore.AppendOrder(ordersPath, order);

                Console.WriteLine();
                Console.WriteLine($"Reorder successful! New Order ID: {order.OrderId} (Pending)\n");
            }
            else if (choice == "4")
            {
                var customer = PromptCustomerByEmail(customers);
                if (customer == null) continue;

                List<FavouriteRecord> all = LoadFavourites(favPath);
                List<FavouriteRecord> favs = new List<FavouriteRecord>();
                for (int i = 0; i < all.Count; i++)
                    if (string.Equals(all[i].CustomerEmail, customer.EmailAddress, StringComparison.OrdinalIgnoreCase))
                        favs.Add(all[i]);

                if (favs.Count == 0)
                {
                    Console.WriteLine("No favourites found.\n");
                    continue;
                }

                PrintFavouriteList(favs, restaurants);
                int favId = ReadInt("Enter Favourite ID to delete: ");

                int idxToRemove = -1;
                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i].FavouriteId == favId &&
                        string.Equals(all[i].CustomerEmail, customer.EmailAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        idxToRemove = i;
                        break;
                    }
                }

                if (idxToRemove == -1)
                {
                    Console.WriteLine("Invalid Favourite ID.\n");
                    continue;
                }

                Console.Write("Confirm delete? [Y/N]: ");
                string confirm = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
                if (confirm != "Y")
                {
                    Console.WriteLine("Delete cancelled.\n");
                    continue;
                }

                all.RemoveAt(idxToRemove);
                SaveAllFavourites(favPath, all);
                Console.WriteLine("Favourite deleted.\n");
            }
            else
            {
                Console.WriteLine("Invalid option.\n");
            }
        }
    }

    
    private class FavouriteRecord
    {
        public int FavouriteId;
        public string CustomerEmail = "";
        public string FavouriteName = "";
        public string RestaurantId = "";
        public List<FavouriteItem> Items = new List<FavouriteItem>();
        public string DefaultAddress = "";
        public string DefaultPaymentMethod = "";
        public string SpecialRequest = "";
    }

    private struct FavouriteItem
    {
        public string ItemName;
        public int Qty;

        public FavouriteItem(string itemName, int qty)
        {
            ItemName = itemName ?? "";
            Qty = qty;
        }
    }

    private static void EnsureFavouritesCsv(string favPath)
    {
        if (File.Exists(favPath)) return;

        using var sw = new StreamWriter(favPath, append: false);
        sw.WriteLine("FavouriteId,CustomerEmail,FavouriteName,RestaurantId,Items,DefaultAddress,DefaultPaymentMethod,SpecialRequest");
    }

    private static List<FavouriteRecord> LoadFavourites(string favPath)
    {
        var list = new List<FavouriteRecord>();
        if (!File.Exists(favPath)) return list;

        string[] lines = File.ReadAllLines(favPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            List<string> p = CsvUtils.SplitCsvLine(lines[i]);
            if (p.Count < 5) continue;

            if (!int.TryParse(p[0].Trim(), out int favId)) continue;

            var fav = new FavouriteRecord
            {
                FavouriteId = favId,
                CustomerEmail = p.Count > 1 ? p[1].Trim() : "",
                FavouriteName = p.Count > 2 ? p[2].Trim() : "",
                RestaurantId = p.Count > 3 ? p[3].Trim() : "",
                DefaultAddress = p.Count > 5 ? p[5].Trim() : "",
                DefaultPaymentMethod = p.Count > 6 ? p[6].Trim() : "",
                SpecialRequest = p.Count > 7 ? p[7].Trim() : ""
            };

            string itemsField = p.Count > 4 ? p[4].Trim() : "";
            fav.Items = ParseItemsField(itemsField);

            list.Add(fav);
        }

        return list;
    }

    private static void SaveAllFavourites(string favPath, List<FavouriteRecord> favs)
    {
        using var sw = new StreamWriter(favPath, append: false);
        sw.WriteLine("FavouriteId,CustomerEmail,FavouriteName,RestaurantId,Items,DefaultAddress,DefaultPaymentMethod,SpecialRequest");

        favs.Sort((a, b) => a.FavouriteId.CompareTo(b.FavouriteId));
        for (int i = 0; i < favs.Count; i++)
            sw.WriteLine(ToCsvLine(favs[i]));
    }

    private static string ToCsvLine(FavouriteRecord f)
    {
        string itemsField = BuildItemsField(f.Items);
        return string.Join(",",
            f.FavouriteId,
            CsvUtils.EscapeField(f.CustomerEmail),
            CsvUtils.EscapeField(f.FavouriteName),
            CsvUtils.EscapeField(f.RestaurantId),
            CsvUtils.EscapeField(itemsField),
            CsvUtils.EscapeField(f.DefaultAddress),
            CsvUtils.EscapeField(f.DefaultPaymentMethod),
            CsvUtils.EscapeField(f.SpecialRequest)
        );
    }

    private static string BuildItemsField(List<FavouriteItem> items)
    {
        var parts = new List<string>();
        for (int i = 0; i < items.Count; i++)
            parts.Add($"{items[i].ItemName}, {items[i].Qty}");

        return string.Join("|", parts);
    }

    private static List<FavouriteItem> ParseItemsField(string itemsField)
    {
        var list = new List<FavouriteItem>();
        if (string.IsNullOrWhiteSpace(itemsField)) return list;

        string[] parts = itemsField.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = (parts[i] ?? "").Trim();
            if (part.Length == 0) continue;

            int comma = part.LastIndexOf(',');
            if (comma < 0)
            {
                list.Add(new FavouriteItem(part, 1));
                continue;
            }

            string name = part.Substring(0, comma).Trim();
            string qtyStr = part.Substring(comma + 1).Trim();

            if (!int.TryParse(qtyStr, out int qty)) qty = 1;
            if (qty < 1) qty = 1;

            list.Add(new FavouriteItem(name, qty));
        }

        return list;
    }

    private static int NextFavouriteId(List<FavouriteRecord> favs)
    {
        int max = 0;
        for (int i = 0; i < favs.Count; i++)
            if (favs[i].FavouriteId > max)
                max = favs[i].FavouriteId;
        return max + 1;
    }

    private static List<FavouriteRecord> LoadFavouritesForCustomer(string favPath, string email)
    {
        var all = LoadFavourites(favPath);
        var result = new List<FavouriteRecord>();

        for (int i = 0; i < all.Count; i++)
            if (string.Equals(all[i].CustomerEmail, email, StringComparison.OrdinalIgnoreCase))
                result.Add(all[i]);

        return result;
    }

    private static void PrintFavouriteList(List<FavouriteRecord> favs, List<Restaurant> restaurants)
    {
        Console.WriteLine();
        Console.WriteLine("Favourites:");
        Console.WriteLine("-----------");

        for (int i = 0; i < favs.Count; i++)
        {
            string restName = favs[i].RestaurantId;
            Restaurant rest = FindRestaurantById(restaurants, favs[i].RestaurantId);
            if (rest != null) restName = rest.Name;

            Console.WriteLine($"[{favs[i].FavouriteId}] {favs[i].FavouriteName} - {restName} ({favs[i].RestaurantId})");
            for (int j = 0; j < favs[i].Items.Count; j++)
                Console.WriteLine($"   - {favs[i].Items[j].ItemName} x {favs[i].Items[j].Qty}");
        }
    }

    private static Customer PromptCustomerByEmail(List<Customer> customers)
    {
        string email = ReadNonEmpty("Enter Customer Email: ");
        for (int i = 0; i < customers.Count; i++)
        {
            if (string.Equals(customers[i].EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                return customers[i];
        }
        Console.WriteLine("Customer not found.\n");
        return null;
    }

    private static Order FindOrderById(Customer customer, int orderId)
    {
        for (int i = 0; i < customer.Orders.Count; i++)
            if (customer.Orders[i].OrderId == orderId)
                return customer.Orders[i];
        return null;
    }

    private static Restaurant FindRestaurantById(List<Restaurant> restaurants, string restaurantId)
    {
        for (int i = 0; i < restaurants.Count; i++)
            if (string.Equals(restaurants[i].RestaurantId, restaurantId, StringComparison.OrdinalIgnoreCase))
                return restaurants[i];
        return null;
    }

    private static List<FoodItem> GetAllFoodItems(Restaurant restaurant)
    {
        var list = new List<FoodItem>();
        for (int i = 0; i < restaurant.Menus.Count; i++)
            for (int j = 0; j < restaurant.Menus[i].FoodItems.Count; j++)
                list.Add(restaurant.Menus[i].FoodItems[j]);
        return list;
    }

    private static FoodItem FindFoodItemByName(List<FoodItem> items, string name)
    {
        for (int i = 0; i < items.Count; i++)
            if (string.Equals(items[i].ItemName, name, StringComparison.OrdinalIgnoreCase))
                return items[i];
        return null;
    }

    private static int GetNextOrderId(List<Customer> customers)
    {
        int max = 1000;
        for (int i = 0; i < customers.Count; i++)
            for (int j = 0; j < customers[i].Orders.Count; j++)
                if (customers[i].Orders[j].OrderId > max)
                    max = customers[i].Orders[j].OrderId;
        return max + 1;
    }

    private static DateTime PromptDeliveryDateTime()
    {
        while (true)
        {
            string dateStr = ReadNonEmpty("Enter Delivery Date (dd/MM/yyyy): ");
            string timeStr = ReadNonEmpty("Enter Delivery Time (HH:mm): ");

            if (DateTime.TryParseExact(
                dateStr + " " + timeStr,
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dt))
            {
                return dt;
            }

            Console.WriteLine("Invalid delivery date/time. Try again.\n");
        }
    }

    private static string ReadPaymentMethod()
    {
        while (true)
        {
            string s = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            if (s == "CC") return "Credit Card";
            if (s == "PP") return "PayPal";
            if (s == "CD") return "Cash on Delivery";
            Console.Write("Invalid method. Enter [CC]/[PP]/[CD]: ");
        }
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

    private static bool IsRefundStatus(Order o)
    {
        return IsStatus(o, "Rejected") || IsStatus(o, "Cancelled");
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
