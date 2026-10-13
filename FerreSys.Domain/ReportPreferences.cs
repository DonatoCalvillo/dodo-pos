using System.Text.Json.Serialization;

namespace FerreSys.Domain;

public sealed class ReportPreferences
{
    public string ReportId { get; set; } = "sales-summary";
    public ReportGrouping Grouping { get; set; } = ReportGrouping.Day;
    public int TopN { get; set; } = 10;
    public string RankBy { get; set; } = nameof(BestSellerRankBy.Amount);
    public string InventoryMetric { get; set; } = nameof(InventoryValueMetric.Retail);

    [JsonIgnore]
    public int NormalizedTopN => TopN is < 5 or > 50 ? 10 : TopN;

    public Dictionary<string, List<string>> Series { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
