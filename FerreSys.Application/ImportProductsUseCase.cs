using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class ImportProductsResult
{
    public ImportProductsResult(List<Product> products, int skippedByLimit)
    {
        Products = products;
        SkippedByLimit = skippedByLimit;
    }

    public List<Product> Products { get; }

    public int SkippedByLimit { get; }
}

public class ImportProductsUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISubscriptionService _subscriptionService;

    public ImportProductsUseCase(IProductRepository productRepository, ISubscriptionService subscriptionService)
    {
        _productRepository = productRepository;
        _subscriptionService = subscriptionService;
    }

    public ImportProductsResult Execute(string csvPath)
    {
        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("El archivo CSV está vacío o solo contiene el encabezado.");
        }

        // Unit, MinStock y DesiredStock son opcionales (en ese orden después de Stock)
        string actualHeader = lines[0].Trim();
        var headerCols = actualHeader.Split(',').Select(h => h.Trim()).ToArray();

        string[] required = { "Barcode", "Name", "CostPrice", "RetailPrice", "Stock" };
        bool requiredOk = headerCols.Length >= 5
            && required.Select((name, i) => string.Equals(headerCols[i], name, StringComparison.OrdinalIgnoreCase)).All(x => x);

        int unitIndex = -1;
        int minStockIndex = -1;
        int desiredStockIndex = -1;

        for (int i = 5; i < headerCols.Length; i++)
        {
            if (string.Equals(headerCols[i], "Unit", StringComparison.OrdinalIgnoreCase))
                unitIndex = i;
            else if (string.Equals(headerCols[i], "MinStock", StringComparison.OrdinalIgnoreCase))
                minStockIndex = i;
            else if (string.Equals(headerCols[i], "DesiredStock", StringComparison.OrdinalIgnoreCase))
                desiredStockIndex = i;
            else
            {
                requiredOk = false;
                break;
            }
        }

        if (!requiredOk)
        {
            throw new FormatException(
                "La estructura del archivo CSV es incorrecta.\n\n" +
                "Debe tener este encabezado (Unit, MinStock y DesiredStock son opcionales):\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock\n" +
                "o\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock,Unit\n" +
                "o\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock,Unit,MinStock,DesiredStock"
            );
        }

        int remainingSlots = int.MaxValue;
        if (_subscriptionService.GetCurrentTier() == SubscriptionTier.Free)
        {
            int currentTotal = _productRepository.GetTotalCount();
            remainingSlots = Math.Max(0, FreeTierLimits.ProductLimit - currentTotal);
        }

        var productsToImport = new List<Product>();
        int skippedByLimit = 0;
        int minCols = headerCols.Length;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (productsToImport.Count >= remainingSlots)
            {
                skippedByLimit++;
                continue;
            }

            var columns = line.Split(',');
            if (columns.Length < minCols)
            {
                throw new DataMisalignedException(
                    $"Error en la línea {i + 1}: La fila no contiene las {minCols} columnas requeridas.");
            }

            try
            {
                string barcode = columns[0].Trim();
                string name = columns[1].Trim();

                if (!decimal.TryParse(columns[2].Trim(), out decimal cost))
                    throw new FormatException($"El precio de costo '{columns[2]}' no es un número válido.");

                if (!decimal.TryParse(columns[3].Trim(), out decimal retail))
                    throw new FormatException($"El precio de venta '{columns[3]}' no es un número válido.");

                if (!decimal.TryParse(columns[4].Trim(), out decimal stock))
                    throw new FormatException($"El stock '{columns[4]}' no es un número válido.");

                UnitOfMeasure unit = UnitOfMeasure.Pieza;
                if (unitIndex >= 0)
                {
                    string unitRaw = columns[unitIndex].Trim();
                    if (!UnitOfMeasureExtensions.TryParse(unitRaw, out unit))
                    {
                        throw new FormatException(
                            $"La unidad '{unitRaw}' no es válida. Use: Pieza, Metro, Kilogramo, Litro, Juego, Paquete.");
                    }
                }

                decimal minStock = 0m;
                if (minStockIndex >= 0)
                {
                    if (!decimal.TryParse(columns[minStockIndex].Trim(), out minStock))
                        throw new FormatException($"El stock mínimo '{columns[minStockIndex]}' no es un número válido.");
                }

                decimal desiredStock = 0m;
                if (desiredStockIndex >= 0)
                {
                    if (!decimal.TryParse(columns[desiredStockIndex].Trim(), out desiredStock))
                        throw new FormatException($"El stock deseado '{columns[desiredStockIndex]}' no es un número válido.");
                }

                if (_productRepository.GetByBarcode(barcode) != null)
                {
                    throw new InvalidOperationException($"El código de barras '{barcode}' ya existe registrado en el sistema.");
                }

                if (productsToImport.Any(p => p.Barcode == barcode))
                {
                    throw new InvalidOperationException($"El código de barras '{barcode}' está duplicado dentro del mismo archivo CSV.");
                }

                var product = new Product(
                    Guid.NewGuid(), barcode, name, cost, retail, stock, unit, minStock, desiredStock);
                productsToImport.Add(product);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error de validación en la línea {i + 1} ({columns[1]}):\n{ex.Message}\n\nNo se importó ningún producto.");
            }
        }

        return new ImportProductsResult(productsToImport, skippedByLimit);
    }
}
