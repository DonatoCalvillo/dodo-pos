using FerreSys.Domain;
using Microsoft.Data.Sqlite;

namespace FerreSys.Infrastructure.Persistence;

public sealed class SqlitePurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly SqliteDbContext _context;

    public SqlitePurchaseOrderRepository(SqliteDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<PurchaseOrder> List(
        Guid? supplierId,
        PurchaseOrderStatus? status,
        DateTime? fromInclusive,
        DateTime? toInclusive)
    {
        var sql = """
            SELECT o.Id, o.Number, o.SupplierId, s.Name, o.Status, o.CreatedAt, o.SentAt, o.DecidedAt, o.Notes, o.CreatedBy
            FROM PurchaseOrders o
            INNER JOIN Suppliers s ON s.Id = o.SupplierId
            WHERE 1 = 1
            """;

        if (supplierId.HasValue)
            sql += " AND o.SupplierId = @SupplierId";
        if (status.HasValue)
            sql += " AND o.Status = @Status";
        if (fromInclusive.HasValue)
            sql += " AND o.CreatedAt >= @From";
        if (toInclusive.HasValue)
            sql += " AND o.CreatedAt < @To";
        sql += " ORDER BY o.CreatedAt DESC, o.Number DESC;";

        var headers = new List<OrderHeader>();
        using var connection = _context.CreateConnection();
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            if (supplierId.HasValue)
                command.Parameters.AddWithValue("@SupplierId", supplierId.Value.ToString());
            if (status.HasValue)
                command.Parameters.AddWithValue("@Status", status.Value.ToCode());
            if (fromInclusive.HasValue)
                command.Parameters.AddWithValue("@From", fromInclusive.Value.Date.ToString("yyyy-MM-dd HH:mm:ss"));
            if (toInclusive.HasValue)
                command.Parameters.AddWithValue("@To", toInclusive.Value.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
                headers.Add(ReadHeader(reader));
        }

        var orders = new List<PurchaseOrder>(headers.Count);
        foreach (var header in headers)
            orders.Add(Materialize(connection, header));
        return orders;
    }

    public PurchaseOrder? Get(Guid id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT o.Id, o.Number, o.SupplierId, s.Name, o.Status, o.CreatedAt, o.SentAt, o.DecidedAt, o.Notes, o.CreatedBy
            FROM PurchaseOrders o
            INNER JOIN Suppliers s ON s.Id = o.SupplierId
            WHERE o.Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var header = ReadHeader(reader);
        reader.Close();
        return Materialize(connection, header);
    }

    public PurchaseOrder SaveDraft(PurchaseOrder order)
    {
        if (!order.IsDraft)
            throw new InvalidOperationException("Solo se puede guardar una orden en borrador.");

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            string? existingStatus = ReadStatus(connection, transaction, order.Id);
            if (existingStatus != null && existingStatus != PurchaseOrderStatus.Draft.ToCode())
                throw new InvalidOperationException("La orden ya fue cerrada y no se puede editar.");

            string number = existingStatus == null
                ? NextNumber(connection, transaction)
                : order.Number;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO PurchaseOrders (Id, Number, SupplierId, Status, CreatedAt, SentAt, DecidedAt, Notes, CreatedBy)
                    VALUES (@Id, @Number, @SupplierId, @Status, @CreatedAt, @SentAt, NULL, @Notes, @CreatedBy)
                    ON CONFLICT(Id) DO UPDATE SET
                        SupplierId = excluded.SupplierId,
                        Notes = excluded.Notes;
                    """;
                command.Parameters.AddWithValue("@Id", order.Id.ToString());
                command.Parameters.AddWithValue("@Number", number);
                command.Parameters.AddWithValue("@SupplierId", order.SupplierId.ToString());
                command.Parameters.AddWithValue("@Status", PurchaseOrderStatus.Draft.ToCode());
                command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@SentAt", order.SentAt.HasValue
                    ? order.SentAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value);
                command.Parameters.AddWithValue("@Notes", order.Notes);
                command.Parameters.AddWithValue("@CreatedBy", order.CreatedBy);
                command.ExecuteNonQuery();
            }

            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM PurchaseOrderLines WHERE PurchaseOrderId = @Id;";
                delete.Parameters.AddWithValue("@Id", order.Id.ToString());
                delete.ExecuteNonQuery();
            }

            const string insertLine = """
                INSERT INTO PurchaseOrderLines (
                    Id, PurchaseOrderId, ProductId, Barcode, ProductName, Unit,
                    OrderedQty, ReceivedQty, UnitCost, SortOrder)
                VALUES (
                    @Id, @OrderId, @ProductId, @Barcode, @Name, @Unit,
                    @OrderedQty, 0, @UnitCost, @SortOrder);
                """;

            for (int i = 0; i < order.Lines.Count; i++)
            {
                PurchaseOrderLine line = order.Lines[i];
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = insertLine;
                command.Parameters.AddWithValue("@Id", line.Id.ToString());
                command.Parameters.AddWithValue("@OrderId", order.Id.ToString());
                command.Parameters.AddWithValue("@ProductId", line.ProductId.ToString());
                command.Parameters.AddWithValue("@Barcode", line.Barcode);
                command.Parameters.AddWithValue("@Name", line.Name);
                command.Parameters.AddWithValue("@Unit", line.Unit.ToString());
                command.Parameters.AddWithValue("@OrderedQty", line.OrderedQty);
                command.Parameters.AddWithValue("@UnitCost", line.UnitCost);
                command.Parameters.AddWithValue("@SortOrder", i);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return Get(order.Id) ?? throw new InvalidOperationException("No se pudo leer la orden guardada.");
    }

    public void MarkSent(Guid id, DateTime sentAt)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE PurchaseOrders
            SET SentAt = @SentAt
            WHERE Id = @Id AND Status = @Status;
            """;
        command.Parameters.AddWithValue("@SentAt", sentAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@Status", PurchaseOrderStatus.Draft.ToCode());
        if (command.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("Solo una orden en borrador puede marcarse como enviada.");
    }

    public PurchaseOrder ApplyDecision(
        Guid orderId,
        PurchaseOrderStatus status,
        DateTime decidedAt,
        string actor,
        IReadOnlyList<PurchaseDecisionLine> lines)
    {
        if (status is not (PurchaseOrderStatus.Accepted or PurchaseOrderStatus.Partial or PurchaseOrderStatus.Rejected))
            throw new InvalidOperationException("La respuesta de la orden no es válida.");

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            OrderHeader? header = ReadHeader(connection, transaction, orderId);
            if (header == null)
                throw new InvalidOperationException("La orden ya no existe.");
            if (header.Status != PurchaseOrderStatus.Draft)
                throw new InvalidOperationException("La orden ya fue cerrada.");

            List<PurchaseOrderLine> current = ReadLines(connection, transaction, orderId);
            if (lines.Count != current.Count || lines.Select(line => line.LineId).Distinct().Count() != current.Count)
                throw new InvalidOperationException("La recepción no coincide con las líneas de la orden.");

            var receivedByLine = lines.ToDictionary(line => line.LineId, line => line.ReceivedQty);
            bool allFull = true;
            bool anyReceived = false;
            foreach (PurchaseOrderLine line in current)
            {
                if (!receivedByLine.TryGetValue(line.Id, out decimal received))
                    throw new InvalidOperationException("Falta una línea en la recepción.");
                if (received < 0 || received > line.OrderedQty)
                    throw new InvalidOperationException($"La cantidad recibida de '{line.Name}' no es válida.");

                if (received > 0)
                    anyReceived = true;
                if (received != line.OrderedQty)
                    allFull = false;
            }

            if (status == PurchaseOrderStatus.Rejected && anyReceived)
                throw new InvalidOperationException("Una orden rechazada no puede recibir mercancía.");
            if (status == PurchaseOrderStatus.Accepted && !allFull)
                throw new InvalidOperationException("Aceptar la orden exige recibir todo lo pedido.");
            if (status == PurchaseOrderStatus.Partial && (allFull || !anyReceived))
                throw new InvalidOperationException("La recepción parcial debe incluir solo una parte de lo pedido.");
            if (status == PurchaseOrderStatus.Rejected && !current.TrueForAll(line => receivedByLine[line.Id] == 0))
                throw new InvalidOperationException("Una orden rechazada no puede recibir mercancía.");

            string who = string.IsNullOrWhiteSpace(actor) ? "Sistema" : actor.Trim();
            foreach (PurchaseOrderLine line in current)
            {
                decimal received = receivedByLine[line.Id];
                if (received <= 0)
                    continue;

                decimal before = ReadStock(connection, transaction, line.ProductId, line.Name);
                decimal after = before + received;
                using (var update = connection.CreateCommand())
                {
                    update.Transaction = transaction;
                    update.CommandText = "UPDATE Products SET Stock = @Stock WHERE Id = @Id;";
                    update.Parameters.AddWithValue("@Stock", after);
                    update.Parameters.AddWithValue("@Id", line.ProductId.ToString());
                    if (update.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException($"El producto '{line.Name}' ya no existe.");
                }

                var movement = new StockMovement(
                    Guid.NewGuid(),
                    line.ProductId,
                    line.Barcode,
                    line.Name,
                    decidedAt,
                    StockMovementKind.Compra,
                    before,
                    received,
                    after,
                    who,
                    orderId,
                    $"Orden {header.Number}");

                InsertMovement(connection, transaction, movement);
            }

            foreach (PurchaseOrderLine line in current)
            {
                using var updateLine = connection.CreateCommand();
                updateLine.Transaction = transaction;
                updateLine.CommandText = """
                    UPDATE PurchaseOrderLines
                    SET ReceivedQty = @Received
                    WHERE Id = @Id AND PurchaseOrderId = @OrderId;
                    """;
                updateLine.Parameters.AddWithValue("@Received", receivedByLine[line.Id]);
                updateLine.Parameters.AddWithValue("@Id", line.Id.ToString());
                updateLine.Parameters.AddWithValue("@OrderId", orderId.ToString());
                updateLine.ExecuteNonQuery();
            }

            using (var updateOrder = connection.CreateCommand())
            {
                updateOrder.Transaction = transaction;
                updateOrder.CommandText = """
                    UPDATE PurchaseOrders
                    SET Status = @Status, DecidedAt = @DecidedAt
                    WHERE Id = @Id AND Status = @Draft;
                    """;
                updateOrder.Parameters.AddWithValue("@Status", status.ToCode());
                updateOrder.Parameters.AddWithValue("@DecidedAt", decidedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                updateOrder.Parameters.AddWithValue("@Id", orderId.ToString());
                updateOrder.Parameters.AddWithValue("@Draft", PurchaseOrderStatus.Draft.ToCode());
                if (updateOrder.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("La orden ya fue cerrada.");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return Get(orderId) ?? throw new InvalidOperationException("No se pudo leer la orden actualizada.");
    }

    private static PurchaseOrder Materialize(SqliteConnection connection, OrderHeader header)
    {
        return new PurchaseOrder(
            header.Id,
            header.Number,
            header.SupplierId,
            header.SupplierName,
            header.Status,
            header.CreatedAt,
            header.SentAt,
            header.DecidedAt,
            header.Notes,
            header.CreatedBy,
            ReadLines(connection, null, header.Id));
    }

    private static List<PurchaseOrderLine> ReadLines(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid orderId)
    {
        var lines = new List<PurchaseOrderLine>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, ProductId, Barcode, ProductName, Unit, OrderedQty, ReceivedQty, UnitCost
            FROM PurchaseOrderLines
            WHERE PurchaseOrderId = @Id
            ORDER BY SortOrder, ProductName COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("@Id", orderId.ToString());
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lines.Add(new PurchaseOrderLine(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                UnitOfMeasureExtensions.ParseOrDefault(reader.GetString(4)),
                Convert.ToDecimal(reader.GetDouble(5)),
                Convert.ToDecimal(reader.GetDouble(6)),
                Convert.ToDecimal(reader.GetDouble(7))));
        }

        return lines;
    }

    private static string? ReadStatus(SqliteConnection connection, SqliteTransaction transaction, Guid id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Status FROM PurchaseOrders WHERE Id = @Id LIMIT 1;";
        command.Parameters.AddWithValue("@Id", id.ToString());
        object? value = command.ExecuteScalar();
        return value == null || value is DBNull ? null : Convert.ToString(value);
    }

    private static OrderHeader? ReadHeader(SqliteConnection connection, SqliteTransaction transaction, Guid id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT o.Id, o.Number, o.SupplierId, s.Name, o.Status, o.CreatedAt, o.SentAt, o.DecidedAt, o.Notes, o.CreatedBy
            FROM PurchaseOrders o
            INNER JOIN Suppliers s ON s.Id = o.SupplierId
            WHERE o.Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadHeader(reader) : null;
    }

    private static OrderHeader ReadHeader(SqliteDataReader reader)
    {
        return new OrderHeader(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            PurchaseOrderStatusExtensions.Parse(reader.GetString(4)),
            DateTime.Parse(reader.GetString(5)),
            reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
            reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
            reader.GetString(8),
            reader.GetString(9));
    }

    private static string NextNumber(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(Number, 4) AS INTEGER)), 0)
            FROM PurchaseOrders
            WHERE Number LIKE 'OC-%';
            """;
        int current = Convert.ToInt32(command.ExecuteScalar());
        return $"OC-{(current + 1):D5}";
    }

    private static decimal ReadStock(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid productId,
        string productName)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Stock FROM Products WHERE Id = @Id LIMIT 1;";
        command.Parameters.AddWithValue("@Id", productId.ToString());
        object? value = command.ExecuteScalar();
        if (value == null || value is DBNull)
            throw new InvalidOperationException($"El producto '{productName}' ya no existe.");
        return Convert.ToDecimal(value);
    }

    private static void InsertMovement(SqliteConnection connection, SqliteTransaction transaction, StockMovement movement)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO StockMovements (
                Id, ProductId, Barcode, ProductName, OccurredAt, Kind,
                QuantityBefore, QuantityDelta, QuantityAfter, Actor, ReferenceId, Notes)
            VALUES (
                @Id, @ProductId, @Barcode, @ProductName, @OccurredAt, @Kind,
                @QuantityBefore, @QuantityDelta, @QuantityAfter, @Actor, @ReferenceId, @Notes);
            """;
        command.Parameters.AddWithValue("@Id", movement.Id.ToString());
        command.Parameters.AddWithValue("@ProductId", movement.ProductId.ToString());
        command.Parameters.AddWithValue("@Barcode", movement.Barcode);
        command.Parameters.AddWithValue("@ProductName", movement.ProductName);
        command.Parameters.AddWithValue("@OccurredAt", movement.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@Kind", movement.KindCode);
        command.Parameters.AddWithValue("@QuantityBefore", movement.QuantityBefore);
        command.Parameters.AddWithValue("@QuantityDelta", movement.QuantityDelta);
        command.Parameters.AddWithValue("@QuantityAfter", movement.QuantityAfter);
        command.Parameters.AddWithValue("@Actor", movement.Actor);
        command.Parameters.AddWithValue("@ReferenceId", movement.ReferenceId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", movement.Notes);
        command.ExecuteNonQuery();
    }

    private sealed class OrderHeader
    {
        public OrderHeader(
            Guid id,
            string number,
            Guid supplierId,
            string supplierName,
            PurchaseOrderStatus status,
            DateTime createdAt,
            DateTime? sentAt,
            DateTime? decidedAt,
            string notes,
            string createdBy)
        {
            Id = id;
            Number = number;
            SupplierId = supplierId;
            SupplierName = supplierName;
            Status = status;
            CreatedAt = createdAt;
            SentAt = sentAt;
            DecidedAt = decidedAt;
            Notes = notes;
            CreatedBy = createdBy;
        }

        public Guid Id { get; }
        public string Number { get; }
        public Guid SupplierId { get; }
        public string SupplierName { get; }
        public PurchaseOrderStatus Status { get; }
        public DateTime CreatedAt { get; }
        public DateTime? SentAt { get; }
        public DateTime? DecidedAt { get; }
        public string Notes { get; }
        public string CreatedBy { get; }
    }
}
