using System.Collections.Generic;
using System.Text;

public static class CsvUtils
{
    // Splits CSV lines safely even if some fields are quoted and contain commas.
    public static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // escaped quote "" inside quoted field
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    public static string EscapeField(string value)
    {
        if (value == null) return "";
        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
        if (!mustQuote) return value;

        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
