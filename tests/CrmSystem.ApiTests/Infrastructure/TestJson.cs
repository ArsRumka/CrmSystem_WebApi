using System.Text.Json;

namespace CrmSystem.ApiTests.Infrastructure;

public static class TestJson
{
    public static string GetString(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetString()
            ?? throw new InvalidOperationException($"Property '{propertyName}' is null.");
    }

    public static Guid GetGuid(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetGuid();
    }

    public static decimal GetDecimal(this JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.String
            ? decimal.Parse(property.GetString()!)
            : property.GetDecimal();
    }

    public static int GetInt32(this JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.String
            ? int.Parse(property.GetString()!)
            : property.GetInt32();
    }

    public static bool GetBool(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetBoolean();
    }

    public static JsonElement? FindByGuid(
        this JsonElement array,
        string propertyName,
        Guid value)
    {
        foreach (var item in array.EnumerateArray())
        {
            if (item.GetGuid(propertyName) == value)
            {
                return item;
            }
        }

        return null;
    }

    public static JsonElement? FindByString(
        this JsonElement array,
        string propertyName,
        string value)
    {
        foreach (var item in array.EnumerateArray())
        {
            if (string.Equals(item.GetString(propertyName), value, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }
}
