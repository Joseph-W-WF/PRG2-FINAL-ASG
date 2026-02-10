using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class OrderCsvStore
{
    public static void AppendOrder(string ordersPath, Order o)
    {
        bool exists = File.Exists(ordersPath);

        using var sw = new StreamWriter(ordersPath, append: true);

        if (!exists)
            sw.WriteLine("OrderId,CustomerEmail,RestaurantId,DeliveryDate,DeliveryTime,DeliveryAddress,CreatedDateTime,TotalAmount,Status,Items");

        sw.WriteLine(ToCsvLine(o));
    }

    public static void RewriteAllOrders(string ordersPath, List<Customer> customers)
    {
        var all = new List<Order>();
        var seen = new HashSet<int>();

        foreach (var c in customers)
            foreach (var o in c.Orders)
                if (seen.Add(o.OrderId))
                    all.Add(o);

        all.Sort((a, b) => a.OrderId.CompareTo(b.OrderId));

        using var sw = new StreamWriter(ordersPath, append: false);
        sw.WriteLine("OrderId,CustomerEmail,RestaurantId,DeliveryDate,DeliveryTime,DeliveryAddress,CreatedDateTime,TotalAmount,Status,Items");

        foreach (var o in all)
            sw.WriteLine(ToCsvLine(o));
    }

    private static string ToCsvLine(Order o)
    {
        string deliveryDate = o.DeliveryDateTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        string deliveryTime = o.DeliveryDateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        string created = o.OrderDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        string amount = o.OrderTotal.ToString("0.##", CultureInfo.InvariantCulture);

        string items = BuildItemsField(o); // contains commas => must be quoted

        return string.Join(",",
            o.OrderId,
            CsvUtils.EscapeField(o.CustomerEmail),
            CsvUtils.EscapeField(o.RestaurantId),
            deliveryDate,
            deliveryTime,
            CsvUtils.EscapeField(o.DeliveryAddress),
            created,
            amount,
            CsvUtils.EscapeField(o.OrderStatus),
            CsvUtils.EscapeField(items)
        );
    }

    private static string BuildItemsField(Order o)
    {
        var parts = new List<string>();
        foreach (var it in o.OrderedFoodItems)
            parts.Add($"{it.ItemName}, {it.QtyOrdered}");

        return string.Join("|", parts);
    }
}
