//==========================================================
// Student Number : S10272886
// Student Name : Joseph Wong
// Partner Name : Aydan Yeo
//========================================================== 
public static class FileLoader
{
    public static List<Customer> LoadCustomers(string filePath)
    {
        List<Customer> customers = new List<Customer>();

        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "") continue;

            string[] parts = lines[i].Split(',');

            string name = parts[0].Trim();
            string email = parts[1].Trim();

            Customer c = new Customer(email, name);
            customers.Add(c);
        }

        return customers;
    }
}

public static class OrderLoader
{
    public static void LoadOrders(
        string filePath,
        List<Customer> customers,
        List<Restaurant> restaurants)
    {
        string[] lines = File.ReadAllLines(filePath);


        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "") continue;

            string[] parts = lines[i].Split(',');

            int orderId = int.Parse(parts[0]);
            string customerEmail = parts[1];
            string restaurantId = parts[2];

            string deliveryDate = parts[3];
            string deliveryTime = parts[4];
            string deliveryAddress = parts[5];

            DateTime createdDateTime = DateTime.Parse(parts[6]);
            double totalAmount = double.Parse(parts[7]);
            string status = parts[8];

            
            DateTime deliveryDateTime =
                DateTime.Parse(deliveryDate + " " + deliveryTime);

            
            Customer customer = null;
            foreach (Customer c in customers)
            {
                if (c.EmailAddress == customerEmail)
                {
                    customer = c;
                    break;
                }
            }

            
            Restaurant restaurant = null;
            foreach (Restaurant r in restaurants)
            {
                if (r.RestaurantId == restaurantId)
                {
                    restaurant = r;
                    break;
                }
            }

            
            if (customer == null || restaurant == null)
                continue;

            
            Order order = new Order(orderId, status, deliveryAddress, "Not Specified");
            order.OrderDateTime = createdDateTime;
            order.DeliveryDateTime = deliveryDateTime;
            order.OrderTotal = totalAmount;

            
            customer.AddOrder(order);
            restaurant.OrderQueue.Enqueue(order);
        }
    }
}
OrderLoader 
