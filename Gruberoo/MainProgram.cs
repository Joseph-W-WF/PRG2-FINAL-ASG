//Main program area
//2nd feature
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
//
//3rd feature
console.WriteLine("All Restaurants and Menu Items\n==============================");
foreach (var res in restaurants) //replace with correct list
{
    console.WriteLine("Restaurant: "+ "restaurant name"+ " (Restaurant id)"); //Replace restaurant name and id with proper variable ( call from aydans side )
    foreach (var menuitem in restaurants) //replace restaurants with correct list
    {
        console.writeline("\t - "+  menuitem);
    }
        }
}
