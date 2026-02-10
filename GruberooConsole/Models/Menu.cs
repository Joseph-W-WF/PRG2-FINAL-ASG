//==========================================================
// Student Number : S10273117G
// Student Name : Aydan Yeo
// Partner Name : Joseph Wong
//==========================================================
using System.Collections.Generic;

public class Menu
{
    public string MenuName { get; }
    public List<FoodItem> FoodItems { get; }

    public Menu(string menuName)
    {
        MenuName = menuName ?? "Main Menu";
        FoodItems = new List<FoodItem>();
    }

    public void AddFoodItem(FoodItem item)
    {
        if (item != null)
            FoodItems.Add(item);
    }
}
