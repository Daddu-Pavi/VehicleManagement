namespace VehicleManagement.Core.Constants;

public static class VehicleCategoryIcons
{
    public static readonly IReadOnlyDictionary<string, string> Icons =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        ["car"] = "🚗",
        ["suv"] = "🚙",
        ["van"] = "🚐",
        ["truck"] = "🚚",
        ["heavy-truck"] = "🚛",
        ["bus"] = "🚌",
        ["motorcycle"] = "🏍️",
        ["other"] = "◆"
    };

    public static string GetSymbol(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return Icons["other"];

        return Icons.TryGetValue(icon, out var symbol)
            ? symbol
            : Icons["other"];
    }

    public static bool IsValid(string? icon)
    {
        return !string.IsNullOrWhiteSpace(icon)
               && Icons.ContainsKey(icon);
    }
}