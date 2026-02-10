using System.Collections.Generic;
using System.IO;

public static class FileLoader
{
    public static List<Customer> LoadCustomers(string customersPath)
    {
        var customers = new List<Customer>();
        if (!File.Exists(customersPath)) return customers;

        var lines = File.ReadAllLines(customersPath);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = CsvUtils.SplitCsvLine(lines[i]);
            if (parts.Count < 2) continue;

            string name = parts[0].Trim();
            string email = parts[1].Trim();

            customers.Add(new Customer(email, name));
        }

        return customers;
    }
}
