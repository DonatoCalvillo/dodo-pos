namespace FerreSys.Domain;

public enum UnitOfMeasure
{
    Pieza,
    Metro,
    Kilogramo,
    Litro,
    Juego,
    Paquete
}

public static class UnitOfMeasureExtensions
{
    public static bool AllowsDecimals(this UnitOfMeasure unit) =>
        unit is UnitOfMeasure.Metro or UnitOfMeasure.Kilogramo or UnitOfMeasure.Litro;

    public static int MaxDecimalPlaces(this UnitOfMeasure unit) =>
        unit.AllowsDecimals() ? 3 : 0;

    public static string DisplaySuffix(this UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Pieza => "pza",
        UnitOfMeasure.Metro => "m",
        UnitOfMeasure.Kilogramo => "kg",
        UnitOfMeasure.Litro => "L",
        UnitOfMeasure.Juego => "jg",
        UnitOfMeasure.Paquete => "paq",
        _ => "pza"
    };

    public static string DisplayName(this UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Pieza => "Pieza",
        UnitOfMeasure.Metro => "Metro",
        UnitOfMeasure.Kilogramo => "Kilogramo",
        UnitOfMeasure.Litro => "Litro",
        UnitOfMeasure.Juego => "Juego",
        UnitOfMeasure.Paquete => "Paquete",
        _ => "Pieza"
    };

    public static string FormatQuantity(this UnitOfMeasure unit, decimal quantity)
    {
        string qtyText = unit.AllowsDecimals()
            ? quantity.ToString("0.###")
            : ((int)quantity).ToString();
        return $"{qtyText} {unit.DisplaySuffix()}";
    }

    public static bool TryParse(string? value, out UnitOfMeasure unit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            unit = UnitOfMeasure.Pieza;
            return false;
        }

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out unit) && Enum.IsDefined(unit))
            return true;

        unit = UnitOfMeasure.Pieza;
        return false;
    }

    public static UnitOfMeasure ParseOrDefault(string? value) =>
        TryParse(value, out var unit) ? unit : UnitOfMeasure.Pieza;
}
