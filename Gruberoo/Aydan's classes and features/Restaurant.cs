using System;
using System.Collections.Generic;

public class Restaurant
{
	public string RestaurantId { get; }
	public string Name { get; }
	public string Email { get; }

	public List<Menu> Menus { get; }
	public List<SpecialOffer> SpecialOffers { get; }

	public Restaurant(string restaurantId, string name, string email)
	{
		RestaurantId = restaurantId ?? "";
		Name = name ?? "";
		Email = email ?? "";

		Menus = new List<Menu>();
		SpecialOffers = new List<SpecialOffer>();
	}

	// For checkpoint 1, we keep everything in one menu.
	public Menu GetOrCreateMainMenu()
	{
		foreach (var m in Menus)
		{
			if (m.MenuName.Equals("Main Menu", StringComparison.OrdinalIgnoreCase))
				return m;
		}

		var main = new Menu("Main Menu");
		Menus.Add(main);
		return main;
	}

	public void DisplayRestaurantAndMenuItems()
	{
		Console.WriteLine($"[{RestaurantId}] {Name} ({Email})");

		foreach (var menu in Menus)
		{
			Console.WriteLine($"  Menu: {menu.MenuName}");

			if (menu.FoodItems.Count == 0)
			{
				Console.WriteLine("  (No items)");
				continue;
			}

			for (int i = 0; i < menu.FoodItems.Count; i++)
			{
				Console.WriteLine($"  {i + 1}. {menu.FoodItems[i]}");
			}
		}

		Console.WriteLine();
	}
}
