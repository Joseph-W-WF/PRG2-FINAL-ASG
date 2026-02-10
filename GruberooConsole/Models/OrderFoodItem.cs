//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 

public class OrderedFoodItem : FoodItem
{
    public int QtyOrdered { get; set; }

    public OrderedFoodItem(string itemName, string description, double price, int qty)
        : base(itemName, description, price)
    {
        QtyOrdered = qty;
    }

    public double CalculateSubtotal()
    {
        return Price * QtyOrdered;
    }
}
