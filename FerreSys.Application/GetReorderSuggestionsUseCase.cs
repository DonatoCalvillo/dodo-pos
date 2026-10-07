using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class ReorderSuggestionsSummary
{
    public ReorderSuggestionsSummary(IReadOnlyList<ReorderSuggestionLine> lines)
    {
        Lines = lines;
        OutOfStockCount = lines.Count(l => l.IsOutOfStock);
        BelowMinimumCount = lines.Count(l => !l.IsOutOfStock && l.AlertLevel == StockAlertLevel.Warning);
        TotalSuggestedUnits = lines.Sum(l => l.SuggestedOrderQty);
    }

    public IReadOnlyList<ReorderSuggestionLine> Lines { get; }
    public int OutOfStockCount { get; }
    public int BelowMinimumCount { get; }
    public int TotalItems => Lines.Count;
    public decimal TotalSuggestedUnits { get; }
}

public class GetReorderSuggestionsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetReorderSuggestionsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public ReorderSuggestionsSummary Execute()
    {
        var lines = _productRepository.GetReorderSuggestions();
        return new ReorderSuggestionsSummary(lines);
    }
}
