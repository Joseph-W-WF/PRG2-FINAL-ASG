//Main program area

List<Customer> customers =
            FileLoader.LoadCustomers("customers.csv");

List<Restaurant> restaurants = new List<Restaurant>
        {
            //Aydans side
        };


OrderLoader.LoadOrders(
    "orders.csv",
    customers,
    restaurants
);

/* Simple proof (for demo/exam)
Console.WriteLine("Customers loaded: " + customers.Count);
Console.WriteLine("Orders for first customer: " + customers[0].Orders.Count);
Console.WriteLine("Orders in first restaurant queue: " + restaurants[0].OrderQueue.Count);
*/