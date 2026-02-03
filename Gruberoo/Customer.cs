//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 


using System;
using System.Collections.Generic;

namespace FoodOrderingApp
{
    public class Customer
    {
        public string EmailAddress { get; set; }
        public string CustomerName { get; set; }

        public List<Order> Orders { get; set; }

        public Customer(string email, string name)
        {
            EmailAddress = email;
            CustomerName = name;
            Orders = new List<Order>();
        }

        public void AddOrder(Order order)
        {
            Orders.Add(order);
        }

        public bool RemoveOrder(Order order)
        {
            return Orders.Remove(order);
        }
    }
}

