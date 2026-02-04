//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 

using System;
using System.Collections.Generic;

namespace FoodOrderingApp
{
    public class Order
    {
        public int OrderNo { get; set; }
        public DateTime OrderDateTime { get; set; }
        public double OrderTotal { get; set; }
        public string OrderStatus { get; set; }
        public DateTime DeliveryDateTime { get; set; }
        public string DeliveryAddress { get; set; }
        public string OrderPaymentMethod { get; set; }
        public bool OrderPaid { get; set; }

        public List<OrderedFoodItem> OrderedFoodItems { get; set; }

        public Order(int orderNo, string status, string address, string paymentMethod)
        {
            OrderNo = orderNo;
            OrderStatus = status;
            DeliveryAddress = address;
            OrderPaymentMethod = paymentMethod;

            OrderDateTime = DateTime.Now;
            DeliveryDateTime = DateTime.Now;
            OrderPaid = false;

            OrderedFoodItems = new List<OrderedFoodItem>();
            OrderTotal = 0;
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

        public double CalculateOrderTotal()
        {
            OrderTotal = 0;

            foreach (OrderedFoodItem item in OrderedFoodItems)
            {
                OrderTotal = OrderTotal + item.CalculateSubtotal();
            }

            return OrderTotal;
        }

        public override string ToString()
        {
            return "Order " + OrderNo + " | Total: $" + CalculateOrderTotal();
        }
    }
}

