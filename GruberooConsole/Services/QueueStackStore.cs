//==========================================================
// Student Number : S10273117G
// Student Name : Aydan Yeo
// Partner Name : Joseph Wong
//==========================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class QueueStackStore
{
	// important: saves restaurant queues and refund stack when exiting
	public static void SaveQueueAndStack(string dataDir, List<Restaurant> restaurants, Stack<Order> refundStack)
	{
		string queuePath = Path.Combine(dataDir, "queue.csv");
		string stackPath = Path.Combine(dataDir, "stack.csv");

		WriteOrders(queuePath, EnumerateQueueOrders(restaurants));
		WriteOrders(stackPath, refundStack);

		Console.WriteLine("Queue and stack saved. Goodbye!");
	}

	private static IEnumerable<Order> EnumerateQueueOrders(List<Restaurant> restaurants)
	{
		foreach (var r in restaurants)
		{
			foreach (var o in r.OrderQueue)
				yield return o;
		}
	}

	private static void WriteOrders(string path, IEnumerable<Order> orders)
	{
		using var sw = new StreamWriter(path, append: false);
		sw.WriteLine("OrderId,CustomerEmail,RestaurantId,DeliveryDate,DeliveryTime,DeliveryAddress,CreatedDateTime,TotalAmount,Status,Items");

		foreach (var o in orders)
			sw.WriteLine(ToCsvLine(o));
	}

	private static string ToCsvLine(Order o)
	{
		string deliveryDate = o.DeliveryDateTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
		string deliveryTime = o.DeliveryDateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
		string created = o.OrderDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
		string amount = o.OrderTotal.ToString("0.##", CultureInfo.InvariantCulture);

		string items = BuildItemsField(o);

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
