//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 
using System;
using System.Collections.Generic;

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDateTime { get; set; }
    public double OrderTotal { get; set; }
    public string OrderStatus { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public string DeliveryAddress { get; set; }
    public string OrderPaymentMethod { get; set; }
    public bool OrderPaid { get; set; }

    public List<OrderedFoodItem> OrderedFoodItems { get; set; } = new List<OrderedFoodItem>();

    public Order(int orderId, string status, string address, string paymentMethod)
    {
        OrderId = orderId;
        OrderStatus = status;
        DeliveryAddress = address;
        OrderPaymentMethod = paymentMethod;

        OrderDateTime = DateTime.Now;
        DeliveryDateTime = DateTime.Now;
        OrderTotal = 0;
        OrderPaid = false;
    }

    public double CalculateOrderTotal()
    {
        OrderTotal = 0;
        foreach (OrderedFoodItem i in OrderedFoodItems)
            OrderTotal += i.CalculateSubtotal();
        return OrderTotal;
    }

    public void AddOrderedFoodItem(OrderedFoodItem item)
    {
        OrderedFoodItems.Add(item);
        CalculateOrderTotal();
    }

    public bool RemoveOrderedFoodItem(OrderedFoodItem item)
    {
        bool removed = OrderedFoodItems.Remove(item);
        CalculateOrderTotal();
        return removed;
    }

    public void DisplayOrderedFoodItems()
    {
        foreach (OrderedFoodItem i in OrderedFoodItems)
            Console.WriteLine(i);
    }

    public override string ToString()
    {
        return "Order " + OrderId + " | " + OrderStatus + " | $" + OrderTotal;
    }
}
