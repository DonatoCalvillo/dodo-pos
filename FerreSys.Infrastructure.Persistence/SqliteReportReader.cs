using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public sealed class SqliteReportReader : IReportReader
{
    private readonly SqliteDbContext _context;

    public SqliteReportReader(SqliteDbContext context)
    {
        _context = context;
    }

    public SalesSummaryReport GetSalesSummary(DateTime start, DateTime end, ReportGrouping grouping)
    {
        var points = new Dictionary<string, MutablePoint>(StringComparer.Ordinal);
        int tickets = 0;
        decimal gross = 0, discounts = 0, cash = 0, card = 0, mixedAmount = 0;
        int mixedTickets = 0;

        using var connection = Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                    {BucketSql("CreatedAt")} AS Bucket,
                    MIN(date(CreatedAt)) AS BucketStart,
                    COUNT(*) AS Tickets,
                    COALESCE(SUM(Total + COALESCE(DiscountAmount, 0)), 0) AS Gross,
                    COALESCE(SUM(COALESCE(DiscountAmount, 0)), 0) AS Discounts,
                    COALESCE(SUM(CASE
                        WHEN PaymentMethod = 'EFECTIVO' THEN Total
                        WHEN PaymentMethod = 'MIXTO' THEN Total - COALESCE(CardAmount, 0)
                        ELSE 0 END), 0) AS Cash,
                    COALESCE(SUM(CASE
                        WHEN PaymentMethod = 'TARJETA' THEN CASE WHEN COALESCE(CardAmount, 0) > 0 THEN CardAmount ELSE Total END
                        WHEN PaymentMethod = 'MIXTO' THEN COALESCE(CardAmount, 0)
                        ELSE 0 END), 0) AS Card,
                    COALESCE(SUM(CASE WHEN PaymentMethod = 'MIXTO' THEN 1 ELSE 0 END), 0) AS MixedTickets,
                    COALESCE(SUM(CASE WHEN PaymentMethod = 'MIXTO' THEN Total ELSE 0 END), 0) AS MixedAmount
                FROM Sales
                WHERE date(CreatedAt) >= date(@Start) AND date(CreatedAt) <= date(@End)
                GROUP BY Bucket
                ORDER BY Bucket;
                """;
            BindRange(command, start, end, grouping);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var point = Point(points, reader.GetString(0), reader.GetString(1));
                point.Tickets += Convert.ToInt32(reader.GetValue(2));
                point.Gross += Money(reader, 3);
                point.Discounts += Money(reader, 4);
                point.Cash += Money(reader, 5);
                point.Card += Money(reader, 6);
                tickets += Convert.ToInt32(reader.GetValue(2));
                gross += Money(reader, 3);
                discounts += Money(reader, 4);
                cash += Money(reader, 5);
                card += Money(reader, 6);
                mixedTickets += Convert.ToInt32(reader.GetValue(7));
                mixedAmount += Money(reader, 8);
            }
        }

        decimal returns = ReadReturns(connection, start, end, grouping, points);
        var series = ToSeries(points);
        return new SalesSummaryReport
        {
            Start = start.Date,
            End = end.Date,
            Tickets = tickets,
            Gross = Round(gross),
            Discounts = Round(discounts),
            Returns = Round(returns),
            Net = Round(gross - discounts - returns),
            Cash = Round(cash),
            Card = Round(card),
            MixedTickets = mixedTickets,
            MixedAmount = Round(mixedAmount),
            Series = series
        };
    }

    public SalesDetailReport GetSalesDetail(DateTime start, DateTime end, ReportGrouping grouping)
    {
        SalesSummaryReport summary = GetSalesSummary(start, end, grouping);
        var lines = new List<SalesDetailLine>();
        using var connection = Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    s.Id,
                    MIN(s.CreatedAt),
                    MIN(d.Barcode),
                    MIN(d.ProductName),
                    SUM(d.Quantity),
                    CASE WHEN SUM(d.Quantity) = 0 THEN 0
                         ELSE SUM(d.Quantity * COALESCE(d.UnitPrice, d.PriceSold)) / SUM(d.Quantity) END,
                    SUM(d.Quantity * COALESCE(d.UnitPrice, d.PriceSold)),
                    COALESCE((
                        SELECT SUM(rl.Quantity)
                        FROM SaleReturnLines rl
                        INNER JOIN SaleReturns r ON r.Id = rl.ReturnId
                        WHERE r.SaleId = s.Id AND rl.ProductId = d.ProductId
                          AND date(r.CreatedAt) >= date(@Start) AND date(r.CreatedAt) <= date(@End)
                    ), 0),
                    COALESCE((
                        SELECT SUM(rl.LineAmount)
                        FROM SaleReturnLines rl
                        INNER JOIN SaleReturns r ON r.Id = rl.ReturnId
                        WHERE r.SaleId = s.Id AND rl.ProductId = d.ProductId
                          AND date(r.CreatedAt) >= date(@Start) AND date(r.CreatedAt) <= date(@End)
                    ), 0)
                FROM SaleDetails d
                INNER JOIN Sales s ON s.Id = d.SaleId
                WHERE date(s.CreatedAt) >= date(@Start) AND date(s.CreatedAt) <= date(@End)
                GROUP BY s.Id, d.ProductId
                ORDER BY MIN(s.CreatedAt), MIN(d.ProductName);
                """;
            BindRange(command, start, end, null);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                lines.Add(MapDetail(reader, outside: false));
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    r.SaleId,
                    MIN(r.CreatedAt),
                    MIN(rl.Barcode),
                    MIN(rl.ProductName),
                    0,
                    CASE WHEN SUM(rl.Quantity) = 0 THEN 0 ELSE SUM(rl.LineAmount) / SUM(rl.Quantity) END,
                    0,
                    SUM(rl.Quantity),
                    SUM(rl.LineAmount)
                FROM SaleReturnLines rl
                INNER JOIN SaleReturns r ON r.Id = rl.ReturnId
                INNER JOIN Sales s ON s.Id = r.SaleId
                WHERE date(r.CreatedAt) >= date(@Start) AND date(r.CreatedAt) <= date(@End)
                  AND (date(s.CreatedAt) < date(@Start) OR date(s.CreatedAt) > date(@End))
                GROUP BY r.SaleId, rl.ProductId
                ORDER BY MIN(r.CreatedAt);
                """;
            BindRange(command, start, end, null);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                lines.Add(MapDetail(reader, outside: true));
        }

        return new SalesDetailReport { Summary = summary, Lines = lines };
    }

    public IReadOnlyList<CashCloseReportDay> GetCashClose(DateTime start, DateTime end)
    {
        var days = new List<CashCloseReportDay>();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            WITH RECURSIVE dates(d) AS (
                SELECT date(@Start)
                UNION ALL
                SELECT date(d, '+1 day') FROM dates WHERE d < date(@End)
            )
            SELECT
                dates.d,
                COALESCE((SELECT OpeningCash FROM CashCloses c WHERE c.BusinessDate = dates.d), 0),
                COALESCE((SELECT SUM(CASE
                    WHEN PaymentMethod = 'EFECTIVO' THEN Total
                    WHEN PaymentMethod = 'MIXTO' THEN Total - COALESCE(CardAmount, 0)
                    ELSE 0 END) FROM Sales WHERE date(CreatedAt) = dates.d), 0),
                COALESCE((SELECT SUM(CASE
                    WHEN PaymentMethod = 'TARJETA' THEN CASE WHEN COALESCE(CardAmount, 0) > 0 THEN CardAmount ELSE Total END
                    WHEN PaymentMethod = 'MIXTO' THEN COALESCE(CardAmount, 0)
                    ELSE 0 END) FROM Sales WHERE date(CreatedAt) = dates.d), 0),
                COALESCE((SELECT SUM(Amount) FROM CashMovements WHERE BusinessDate = dates.d AND Kind = 'ENTRADA'), 0),
                COALESCE((SELECT SUM(Amount) FROM CashMovements WHERE BusinessDate = dates.d AND Kind = 'SALIDA'), 0),
                COALESCE((SELECT SUM(Amount) FROM CashMovements WHERE BusinessDate = dates.d AND Kind = 'SALIDA' AND Concept LIKE 'Devolución ticket %'), 0),
                (SELECT CountedCash FROM CashCloses c WHERE c.BusinessDate = dates.d),
                (SELECT ClosedAt FROM CashCloses c WHERE c.BusinessDate = dates.d)
            FROM dates
            ORDER BY dates.d;
            """;
        command.Parameters.AddWithValue("@Start", start.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@End", end.ToString("yyyy-MM-dd"));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            decimal opening = Money(reader, 1);
            decimal cashSales = Money(reader, 2);
            decimal entries = Money(reader, 4);
            decimal exits = Money(reader, 5);
            decimal expected = Round(opening + cashSales + entries - exits);
            decimal? counted = reader.IsDBNull(7) ? null : Money(reader, 7);
            bool closed = !reader.IsDBNull(8);
            days.Add(new CashCloseReportDay
            {
                BusinessDate = DateTime.Parse(reader.GetString(0)),
                OpeningCash = opening,
                CashSales = cashSales,
                CardSales = Money(reader, 3),
                Entries = entries,
                Exits = exits,
                ReturnExits = Money(reader, 6),
                ExpectedCash = expected,
                CountedCash = closed ? counted : counted,
                Difference = counted is decimal countedValue ? Round(countedValue - expected) : null,
                IsClosed = closed
            });
        }

        return days;
    }

    public InventoryReport GetInventory()
    {
        var lines = new List<InventoryReportLine>();
        int ok = 0, below = 0, zero = 0;
        decimal costValue = 0, retailValue = 0;
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Barcode, Name, Unit, Stock, MinStock, DesiredStock, CostPrice, RetailPrice
            FROM Products
            WHERE IsActive = 1
            ORDER BY Name COLLATE NOCASE;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            decimal stock = Convert.ToDecimal(reader.GetValue(3));
            decimal min = Convert.ToDecimal(reader.GetValue(4));
            decimal cost = Money(reader, 6);
            decimal retail = Money(reader, 7);
            decimal lineCost = Round(stock * cost);
            decimal lineRetail = Round(stock * retail);
            string status = stock <= 0m ? "En cero" : min > 0m && stock <= min ? "Bajo mínimo" : "OK";
            if (status == "En cero") zero++;
            else if (status == "Bajo mínimo") below++;
            else ok++;
            costValue += lineCost;
            retailValue += lineRetail;
            lines.Add(new InventoryReportLine
            {
                Barcode = reader.GetString(0),
                Name = reader.GetString(1),
                Unit = reader.IsDBNull(2) ? "Pieza" : reader.GetString(2),
                Stock = stock,
                MinStock = min,
                DesiredStock = Convert.ToDecimal(reader.GetValue(5)),
                CostPrice = cost,
                RetailPrice = retail,
                CostValue = lineCost,
                RetailValue = lineRetail,
                Status = status
            });
        }

        return new InventoryReport
        {
            OkCount = ok,
            BelowMinimumCount = below,
            OutOfStockCount = zero,
            CostValue = Round(costValue),
            RetailValue = Round(retailValue),
            Lines = lines
        };
    }

    public BestSellersReport GetBestSellers(
        DateTime start,
        DateTime end,
        int topN,
        BestSellerRankBy rankBy)
    {
        var map = new Dictionary<string, MutableSeller>(StringComparer.Ordinal);
        using var connection = Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT d.ProductId, MIN(d.Barcode), MIN(d.ProductName),
                       SUM(d.Quantity),
                       SUM(d.Quantity * COALESCE(d.UnitPrice, d.PriceSold)),
                       SUM(d.Quantity * COALESCE(d.CostPrice, 0))
                FROM SaleDetails d
                INNER JOIN Sales s ON s.Id = d.SaleId
                WHERE date(s.CreatedAt) >= date(@Start) AND date(s.CreatedAt) <= date(@End)
                GROUP BY d.ProductId;
                """;
            BindRange(command, start, end, null);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = Seller(map, reader.GetString(0), reader.GetString(1), reader.GetString(2));
                row.Quantity += Convert.ToDecimal(reader.GetValue(3));
                row.Amount += Money(reader, 4);
                row.Cost += Convert.ToDecimal(reader.GetValue(5));
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT rl.ProductId, MIN(rl.Barcode), MIN(rl.ProductName),
                       SUM(rl.Quantity),
                       SUM(rl.LineAmount),
                       SUM(rl.Quantity * COALESCE(rl.CostPrice, 0))
                FROM SaleReturnLines rl
                INNER JOIN SaleReturns r ON r.Id = rl.ReturnId
                WHERE date(r.CreatedAt) >= date(@Start) AND date(r.CreatedAt) <= date(@End)
                GROUP BY rl.ProductId;
                """;
            BindRange(command, start, end, null);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = Seller(map, reader.GetString(0), reader.GetString(1), reader.GetString(2));
                row.Quantity -= Convert.ToDecimal(reader.GetValue(3));
                row.Amount -= Money(reader, 4);
                row.Cost -= Convert.ToDecimal(reader.GetValue(5));
            }
        }

        IEnumerable<BestSellerLine> ranked = map.Values
            .Select(row => new BestSellerLine
            {
                Barcode = row.Barcode,
                Name = row.Name,
                Quantity = row.Quantity,
                Amount = Round(row.Amount),
                Profit = Round(row.Amount - row.Cost)
            })
            .OrderByDescending(line => rankBy switch
            {
                BestSellerRankBy.Quantity => line.Quantity,
                BestSellerRankBy.Profit => line.Profit,
                _ => line.Amount
            })
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(topN, 5, 50));

        return new BestSellersReport
        {
            Start = start.Date,
            End = end.Date,
            RankBy = rankBy,
            Lines = ranked.ToList()
        };
    }

    private decimal ReadReturns(
        SqliteConnection connection,
        DateTime start,
        DateTime end,
        ReportGrouping grouping,
        Dictionary<string, MutablePoint> points)
    {
        decimal total = 0;
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {BucketSql("CreatedAt")}, MIN(date(CreatedAt)), COALESCE(SUM(Amount), 0)
            FROM SaleReturns
            WHERE date(CreatedAt) >= date(@Start) AND date(CreatedAt) <= date(@End)
            GROUP BY 1;
            """;
        BindRange(command, start, end, grouping);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            decimal amount = Money(reader, 2);
            Point(points, reader.GetString(0), reader.GetString(1)).Returns += amount;
            total += amount;
        }

        return total;
    }

    private static List<SalesSeriesPoint> ToSeries(Dictionary<string, MutablePoint> points) =>
        points.Values
            .OrderBy(point => point.Bucket)
            .Select(point => new SalesSeriesPoint
            {
                Bucket = point.Bucket,
                BucketStart = point.Start,
                Tickets = point.Tickets,
                Gross = Round(point.Gross),
                Discounts = Round(point.Discounts),
                Returns = Round(point.Returns),
                Net = Round(point.Gross - point.Discounts - point.Returns),
                Cash = Round(point.Cash),
                Card = Round(point.Card)
            })
            .ToList();

    private static SalesDetailLine MapDetail(SqliteDataReader reader, bool outside)
    {
        string id = reader.GetString(0);
        decimal quantity = Convert.ToDecimal(reader.GetValue(4));
        decimal amount = Money(reader, 6);
        return new SalesDetailLine
        {
            Ticket = id.Length >= 8 ? id[..8].ToUpperInvariant() : id.ToUpperInvariant(),
            OccurredAt = DateTime.Parse(reader.GetString(1)),
            Barcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            ProductName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Quantity = outside ? 0m : quantity,
            UnitPrice = Round(Convert.ToDecimal(reader.GetValue(5))),
            Amount = outside ? 0m : Round(amount),
            ReturnedQuantity = Convert.ToDecimal(reader.GetValue(7)),
            ReturnedAmount = Money(reader, 8),
            SaleOutsidePeriod = outside
        };
    }

    private SqliteConnection Open()
    {
        var connection = _context.CreateConnection();
        connection.Open();
        return connection;
    }

    private static void BindRange(SqliteCommand command, DateTime start, DateTime end, ReportGrouping? grouping)
    {
        command.Parameters.AddWithValue("@Start", start.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@End", end.ToString("yyyy-MM-dd"));
        if (grouping.HasValue)
            command.Parameters.AddWithValue("@Grouping", grouping.Value.ToString());
    }

    private static string BucketSql(string column) => $"""
        CASE
            WHEN @Grouping = 'Month' THEN strftime('%Y-%m', {column})
            WHEN @Grouping = 'Week' THEN strftime('%Y', {column}) || '-W' || printf('%02d', CAST(strftime('%W', {column}) AS INTEGER))
            ELSE date({column})
        END
        """;

    private static MutablePoint Point(Dictionary<string, MutablePoint> points, string bucket, string start)
    {
        if (!points.TryGetValue(bucket, out MutablePoint? point))
        {
            point = new MutablePoint(bucket, DateTime.Parse(start));
            points[bucket] = point;
        }

        return point;
    }

    private static MutableSeller Seller(
        Dictionary<string, MutableSeller> map,
        string id,
        string barcode,
        string name)
    {
        if (!map.TryGetValue(id, out MutableSeller? row))
        {
            row = new MutableSeller(barcode, name);
            map[id] = row;
        }

        return row;
    }

    private static decimal Money(SqliteDataReader reader, int ordinal) =>
        Round(Convert.ToDecimal(reader.GetValue(ordinal)));

    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class MutablePoint
    {
        public MutablePoint(string bucket, DateTime start)
        {
            Bucket = bucket;
            Start = start;
        }

        public string Bucket { get; }
        public DateTime Start { get; }
        public int Tickets { get; set; }
        public decimal Gross { get; set; }
        public decimal Discounts { get; set; }
        public decimal Returns { get; set; }
        public decimal Cash { get; set; }
        public decimal Card { get; set; }
    }

    private sealed class MutableSeller
    {
        public MutableSeller(string barcode, string name)
        {
            Barcode = barcode;
            Name = name;
        }

        public string Barcode { get; }
        public string Name { get; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal Cost { get; set; }
    }
}
