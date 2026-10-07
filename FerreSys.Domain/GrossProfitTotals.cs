namespace FerreSys.Domain;

public class GrossProfitTotals
{
    public decimal TotalSold { get; }
    public decimal TotalCost { get; }

    public GrossProfitTotals(decimal totalSold, decimal totalCost)
    {
        TotalSold = totalSold;
        TotalCost = totalCost;
    }
}
