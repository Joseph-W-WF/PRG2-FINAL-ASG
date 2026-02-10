using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class OrderLoader
{
    public static void LoadOrders(string ordersPath, List<Customer> customers, List<Restaurant> restaurants)
    {
        if (!File.Exists(ordersPath)) return;

        // quick lookup
        var custByEmail = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in customers) custByEmail[c.EmailAddress] = c;

        var restById = new Dictionary<string, Restaurant>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in restaurants) restById[r.RestaurantId] = r;

        var lines = File.ReadAllLines(ordersPath);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var p = CsvUtils.SplitCsvLine(lines[i]);
            if (p.Count < 10) continue;

            int orderId = int.Parse(p[0].Trim());
            string customerEmail = p[1].Trim();
            string restaurantId = p[2].Trim();

            string deliveryDateStr = p[3].Trim(); // dd/MM/yyyy
            string deliveryTimeStr = p[4].Trim(); // HH:mm
            string address = p[5].Trim();

            string createdStr = p[6].Trim();      // dd/MM/yyyy HH:mm
            double totalAmount = double.Parse(p[7].Trim(), CultureInfo.InvariantCulture);

            string status = p[8].Trim();
            string itemsField = p[9].Trim();      // quoted, contains commas

            if (!custByEmail.ContainsKey(customerEmail)) continue;
            if (!restById.ContainsKey(restaurantId)) continue;

            DateTime createdDT = ParseDateTime(createdStr, new[]
            {
                "dd/MM/yyyy HH:mm", "d/M/yyyy HH:mm"
            });

            DateTime deliveryDT = ParseDateTime(deliveryDateStr + " " + deliveryTimeStr, new[]
            {
                "dd/MM/yyyy HH:mm", "d/M/yyyy HH:mm"
            });

            var order = new Order(orderId, status, address, "Loaded");
            order.CustomerEmail = customerEmail;
            order.RestaurantId = restaurantId;
            order.OrderDateTime = createdDT;
            order.DeliveryDateTime = deliveryDT;
            order.OrderTotal = totalAmount;
            order.OrderPaid = true;

            // load items: "Item, qty|Item, qty"
            LoadItems(order, restById[restaurantId], itemsField);

            custByEmail[customerEmail].AddOrder(order);
            restById[restaurantId].OrderQueue.Enqueue(order);
        }
    }

    private static void LoadItems(Order order, Restaurant restaurant, string itemsField)
    {
        if (string.IsNullOrWhiteSpace(itemsField)) return;

        string[] chunks = itemsField.Split('|');

        foreach (var raw in chunks)
        {
            string s = raw.Trim();
            if (s.Length == 0) continue;

            int idx = s.LastIndexOf(',');
            if (idx < 0) continue;

            string itemName = s.Substring(0, idx).Trim();
            string qtyStr = s.Substring(idx + 1).Trim();

            if (!int.TryParse(qtyStr, out int qty)) continue;

            FoodItem fi = FindFoodItem(restaurant, itemName);
            string desc = fi != null ? fi.Description : "";
            double price = fi != null ? fi.Price : 0;

            order.OrderedFoodItems.Add(new OrderedFoodItem(itemName, desc, price, qty));
        }
    }

    private static FoodItem FindFoodItem(Restaurant restaurant, string name)
    {
        foreach (var m in restaurant.Menus)
            foreach (var f in m.FoodItems)
                if (string.Equals(f.ItemName, name, StringComparison.OrdinalIgnoreCase))
                    return f;

        return null;
    }

    private static DateTime ParseDateTime(string input, string[] formats)
    {
        if (DateTime.TryParseExact(
            input,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dt))
            return dt;

        // fallback (should not happen for your dataset)
        return DateTime.Parse(input, CultureInfo.InvariantCulture);
    }
}
