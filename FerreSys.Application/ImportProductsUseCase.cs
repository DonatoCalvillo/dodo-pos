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

        // 1. VALIDACIÓN DE ESTRUCTURA (Fail-Fast)
        // Unit es opcional: Barcode,Name,CostPrice,RetailPrice,Stock[,Unit]
        string actualHeader = lines[0].Trim();
        var headerCols = actualHeader.Split(',').Select(h => h.Trim()).ToArray();
        bool hasUnitColumn = headerCols.Length >= 6
            && string.Equals(headerCols[5], "Unit", StringComparison.OrdinalIgnoreCase);

        string[] required = { "Barcode", "Name", "CostPrice", "RetailPrice", "Stock" };
        bool headerOk = headerCols.Length >= 5
            && required.Select((name, i) => string.Equals(headerCols[i], name, StringComparison.OrdinalIgnoreCase)).All(x => x)
            && (headerCols.Length == 5 || hasUnitColumn);

        if (!headerOk)
        {
            throw new FormatException(
                "La estructura del archivo CSV es incorrecta.\n\n" +
                "Debe tener este encabezado (Unit es opcional):\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock\n" +
                "o\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock,Unit"
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

        // 2. VALIDACIÓN DE DATOS (Fila por fila en memoria, hasta el cupo gratuito)
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
            int minCols = hasUnitColumn ? 6 : 5;
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
                if (hasUnitColumn)
                {
                    string unitRaw = columns[5].Trim();
                    if (!UnitOfMeasureExtensions.TryParse(unitRaw, out unit))
                    {
                        throw new FormatException(
                            $"La unidad '{unitRaw}' no es válida. Use: Pieza, Metro, Kilogramo, Litro, Juego, Paquete.");
                    }
                }

                // Validar duplicados contra la base de datos
                if (_productRepository.GetByBarcode(barcode) != null)
                {
                    throw new InvalidOperationException($"El código de barras '{barcode}' ya existe registrado en el sistema.");
                }

                // Validar duplicados dentro del mismo archivo CSV que se está subiendo
                if (productsToImport.Any(p => p.Barcode == barcode))
                {
                    throw new InvalidOperationException($"El código de barras '{barcode}' está duplicado dentro del mismo archivo CSV.");
                }

                // Creamos la entidad (esto dispara las reglas de negocio del Dominio automáticamente)
                var product = new Product(Guid.NewGuid(), barcode, name, cost, retail, stock, unit);
                productsToImport.Add(product);
            }
            catch (Exception ex)
            {
                // Si CUALQUIER fila que sí entra falla, lanzamos el error deteniendo el proceso completo
                throw new InvalidOperationException($"Error de validación en la línea {i + 1} ({columns[1]}):\n{ex.Message}\n\nNo se importó ningún producto.");
            }
        }

        return new ImportProductsResult(productsToImport, skippedByLimit);
    }
}
