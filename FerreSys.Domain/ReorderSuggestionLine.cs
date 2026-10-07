namespace FerreSys.Domain;

public sealed class ReorderSuggestionLine
{
    public ReorderSuggestionLine(
        string barcode,
        string name,
        UnitOfMeasure unit,
        decimal stock,
        decimal minStock,
        decimal desiredStock)
    {
        Barcode = barcode;
        Name = name;
        Unit = unit;
        Stock = stock;
        MinStock = minStock;
        DesiredStock = desiredStock;
    }

    public string Barcode { get; }
    public string Name { get; }
    public UnitOfMeasure Unit { get; }
    public decimal Stock { get; }
    public decimal MinStock { get; }
    public decimal DesiredStock { get; }

    public decimal SuggestedOrderQty =>
        Math.Max(0m, DesiredStock - Stock);

    public bool IsOutOfStock => Stock <= 0m;

    public StockAlertLevel AlertLevel =>
        IsOutOfStock
            ? StockAlertLevel.Critical
            : MinStock > 0m && Stock <= MinStock
                ? StockAlertLevel.Warning
                : StockAlertLevel.None;
}
