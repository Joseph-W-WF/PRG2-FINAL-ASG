//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 

using system;

namespace FoodOrderingApp
{
    public class OrderedFoodItem : FoodItem
    {
        public int QtyOrdered { get; set; }
        public double SubTotal { get; set; }

        public OrderedFoodItem(string name, string desc, double price, string customise, int qty)
            : base(name, desc, price, customise)
        {
            QtyOrdered = qty;
            SubTotal = CalculateSubtotal();
        }

        public double CalculateSubtotal()
        {
            SubTotal = QtyOrdered * ItemPrice;
            return SubTotal;
        }

        public override string ToString()
        {
            return ItemName + " x" + QtyOrdered + " = $" + CalculateSubtotal();
        }
    }
}
