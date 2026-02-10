using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class Program
{
    public static void Main()
    {
        
        string basePath = Directory.Exists("Data-Files") ? "Data-Files" : ".";

        string restaurantsPath = Path.Combine(basePath, "restaurants.csv");
        string foodItemsPath = Path.Combine(basePath, "fooditems.csv");

        var restaurants = LoadRestaurants(restaurantsPath);
        LoadFoodItemsIntoRestaurants(foodItemsPath, restaurants);

        
        Console.WriteLine("=== Feature: List All Restaurants and Menu Items ===\n");

        if (restaurants.Count == 0)
        {
            Console.WriteLine("No restaurants loaded. Check your restaurants.csv path.");
        }
        else
        {
            foreach (var r in restaurants.Values)
            {
                r.DisplayRestaurantAndMenuItems();
            }
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static Dictionary<string, Restaurant> LoadRestaurants(string filePath)
    {
        var map = new Dictionary<string, Restaurant>();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: Cannot find file: {filePath}");
            return map;
        }

        string[] lines = File.ReadAllLines(filePath);

        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 3) continue;

            string id = parts[0].Trim();
            string name = parts[1].Trim();
            string email = parts[2].Trim();

            if (!string.IsNullOrEmpty(id))
                map[id] = new Restaurant(id, name, email);
        }

        return map;
    }

    private static void LoadFoodItemsIntoRestaurants(string filePath, Dictionary<string, Restaurant> restaurants)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: Cannot find file: {filePath}");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 4) continue;

            string restId = parts[0].Trim();
            string itemName = parts[1].Trim();
            string desc = parts[2].Trim();
            string priceStr = parts[3].Trim();

            if (!restaurants.ContainsKey(restId)) continue;

            if (!double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double price))
                price = 0;

            var item = new FoodItem(itemName, desc, price);
            restaurants[restId].GetOrCreateMainMenu().AddFoodItem(item);
        }
    }

    
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
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
