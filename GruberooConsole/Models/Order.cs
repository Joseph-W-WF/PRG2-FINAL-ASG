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

    // Needed so we can write to orders.csv correctly
    public string CustomerEmail { get; set; } = "";
    public string RestaurantId { get; set; } = "";

    public DateTime OrderDateTime { get; set; }
    public double OrderTotal { get; set; }
    public string OrderStatus { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public string DeliveryAddress { get; set; }
    public string OrderPaymentMethod { get; set; }
    public bool OrderPaid { get; set; }

    public string SpecialRequest { get; set; } = "";

    public List<OrderedFoodItem> OrderedFoodItems { get; set; } = new List<OrderedFoodItem>();

    public Order(int orderId, string status, string address, string paymentMethod)
    {
        OrderId = orderId;
        OrderStatus = status ?? "Pending";
        DeliveryAddress = address ?? "";
        OrderPaymentMethod = paymentMethod ?? "";

        OrderDateTime = DateTime.Now;
        DeliveryDateTime = DateTime.Now;
        OrderTotal = 0;
        OrderPaid = false;
    }

    public double CalculateItemsTotal()
    {
        double sum = 0;
        foreach (var i in OrderedFoodItems)
            sum += i.CalculateSubtotal();
        return sum;
    }

    public void RecalculateTotalWithDelivery(double deliveryFee)
    {
        OrderTotal = CalculateItemsTotal() + deliveryFee;
    }
}
