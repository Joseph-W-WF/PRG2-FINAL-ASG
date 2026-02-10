//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 


using System.Collections.Generic;

public class Customer
{
    public string EmailAddress { get; set; }
    public string CustomerName { get; set; }
    public List<Order> Orders { get; set; }

    public Customer(string email, string name)
    {
        EmailAddress = email ?? "";
        CustomerName = name ?? "";
        Orders = new List<Order>();
    }

    public void AddOrder(Order order)
    {
        if (order != null) Orders.Add(order);
    }

    public bool RemoveOrder(Order order) => Orders.Remove(order);
}


