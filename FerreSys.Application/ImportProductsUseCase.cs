using FerreSys.Domain;

namespace FerreSys.Application;

public class ImportProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public ImportProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<Product> Execute(string csvPath)
    {
        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("El archivo CSV está vacío o solo contiene el encabezado.");
        }

        // 1. VALIDACIÓN DE ESTRUCTURA (Fail-Fast)
        string expectedHeader = "Barcode,Name,CostPrice,RetailPrice,Stock";
        string actualHeader = lines[0].Trim();

        if (!string.Equals(expectedHeader, actualHeader, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                "La estructura del archivo CSV es incorrecta.\n\n" +
                "Debe tener exactamente este encabezado de 5 columnas separadas por comas:\n" +
                "Barcode,Name,CostPrice,RetailPrice,Stock"
            );
        }

        var productsToImport = new List<Product>();

        // 2. VALIDACIÓN DE DATOS (Fila por fila en memoria)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = line.Split(',');
            if (columns.Length < 5)
            {
                throw new DataMisalignedException($"Error en la línea {i + 1}: La fila no contiene las 5 columnas requeridas.");
            }

            try
            {
                string barcode = columns[0].Trim();
                string name = columns[1].Trim();

                if (!decimal.TryParse(columns[2].Trim(), out decimal cost))
                    throw new FormatException($"El precio de costo '{columns[2]}' no es un número válido.");

                if (!decimal.TryParse(columns[3].Trim(), out decimal retail))
                    throw new FormatException($"El precio de venta '{columns[3]}' no es un número válido.");

                if (!int.TryParse(columns[4].Trim(), out int stock))
                    throw new FormatException($"El stock '{columns[4]}' no es un número entero válido.");

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
                var product = new Product(Guid.NewGuid(), barcode, name, cost, retail, stock);
                productsToImport.Add(product);
            }
            catch (Exception ex)
            {
                // Si CUALQUIER fila falla, lanzamos el error deteniendo el proceso completo
                throw new InvalidOperationException($"Error de validación en la línea {i + 1} ({columns[1]}):\n{ex.Message}\n\nNo se importó ningún producto.");
            }
        }

        // Si todo el archivo es válido, regresamos la lista completa para ser guardada
        return productsToImport;
    }
}