using System.IO;
using System.Text;
using InventoryApp.Models;

namespace InventoryApp.Helpers;

public static class CsvExporter
{
    private const string Header = "SKU,Name,Category,Price,Quantity,Status";

    /// <summary>Writes parts as RFC 4180 CSV (UTF-8 with BOM so Excel detects the encoding).</summary>
    public static async Task ExportAsync(IEnumerable<Part> parts, string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n");

        foreach (var p in parts)
        {
            sb.Append(Escape(p.Sku)).Append(',')
              .Append(Escape(p.Name)).Append(',')
              .Append(Escape(p.CategoryName ?? string.Empty)).Append(',')
              .Append(p.UnitPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(p.QuantityInStock.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(Escape(p.Status))
              .Append("\r\n");
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true), cancellationToken);
    }

    /// <summary>Quotes fields as needed and neutralizes spreadsheet formula injection.</summary>
    internal static string Escape(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '@' or '\t' or '\r')
            value = "'" + value;

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
