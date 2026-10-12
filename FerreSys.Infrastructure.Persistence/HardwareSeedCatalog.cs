using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.Persistence;

public static class HardwareSeedCatalog
{
    private readonly record struct SeedItem(string Name, decimal Cost, decimal Retail, decimal Stock);

    public static int ApplyIfEmpty(IProductRepository repository)
    {
        if (repository.GetTotalCount() > 0)
            return 0;

        var products = CreateProducts();
        var movements = products
            .Select(product => StockMovementFactory.Create(
                product,
                StockMovementKind.CargaInicial,
                quantityBefore: 0m,
                quantityDelta: product.Stock,
                quantityAfter: product.Stock,
                notes: "Catálogo inicial"))
            .ToList();

        repository.SaveBulkWithStockMovements(products, movements);
        return products.Count;
    }

    public static List<Product> CreateProducts()
    {
        var items = Items();
        if (items.Count != 150)
            throw new InvalidOperationException($"El catálogo inicial debe tener 150 productos y tiene {items.Count}.");

        var products = new List<Product>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            products.Add(new Product(
                Guid.NewGuid(),
                $"75012345{(i + 1):D5}",
                item.Name,
                item.Cost,
                item.Retail,
                item.Stock));
        }

        return products;
    }

    private static List<SeedItem> Items()
    {
        var items = new List<SeedItem>(150);
        items.AddRange(Tornilleria);
        items.AddRange(Plomeria);
        items.AddRange(Electrico);
        items.AddRange(Herramienta);
        items.AddRange(Pintura);
        return items;
    }

    private static readonly SeedItem[] Tornilleria =
    [
        new("Tornillo pija 6 x 1 pulg", 0.35m, 0.80m, 200),
        new("Tornillo pija 8 x 1 pulg", 0.45m, 1.00m, 200),
        new("Tornillo pija 8 x 1.5 pulg", 0.55m, 1.20m, 180),
        new("Tornillo pija 10 x 2 pulg", 0.80m, 1.80m, 150),
        new("Tornillo pija 12 x 2 pulg", 1.10m, 2.40m, 120),
        new("Tornillo pija 14 x 3 pulg", 1.80m, 3.50m, 80),
        new("Tornillo hexagonal 1/4 x 1 pulg", 1.20m, 2.50m, 100),
        new("Tornillo hexagonal 5/16 x 2 pulg", 2.40m, 4.80m, 80),
        new("Tornillo hexagonal 3/8 x 2 pulg", 3.50m, 7.00m, 60),
        new("Tornillo hexagonal 1/2 x 3 pulg", 6.50m, 12.00m, 40),
        new("Tuerca hexagonal 1/4", 0.40m, 1.00m, 200),
        new("Tuerca hexagonal 5/16", 0.55m, 1.30m, 180),
        new("Tuerca hexagonal 3/8", 0.80m, 1.80m, 150),
        new("Tuerca hexagonal 1/2", 1.40m, 3.00m, 100),
        new("Rondana plana 1/4", 0.20m, 0.50m, 300),
        new("Rondana plana 5/16", 0.25m, 0.60m, 250),
        new("Rondana plana 3/8", 0.35m, 0.80m, 200),
        new("Rondana de presión 1/4", 0.30m, 0.70m, 200),
        new("Rondana de presión 3/8", 0.45m, 1.00m, 150),
        new("Taquet expansivo 1/4", 0.60m, 1.50m, 200),
        new("Taquet expansivo 5/16", 0.90m, 2.00m, 150),
        new("Taquet expansivo 3/8", 1.40m, 3.00m, 120),
        new("Pija para lámina 1 pulg", 0.50m, 1.20m, 200),
        new("Pija para lámina 1.5 pulg", 0.70m, 1.60m, 160),
        new("Pija autotaladrante 1 pulg", 0.90m, 2.00m, 140),
        new("Clavo 2 pulg caja 1 kg", 28.00m, 48.00m, 25),
        new("Clavo 2.5 pulg caja 1 kg", 28.00m, 48.00m, 20),
        new("Clavo 3 pulg caja 1 kg", 30.00m, 52.00m, 20),
        new("Clavo para concreto 2 pulg", 1.20m, 2.80m, 100),
        new("Armella cerrada No. 10", 2.50m, 5.50m, 40),
        new("Armella abierta No. 12", 3.00m, 6.50m, 40),
        new("Bisagra de acero 2 pulg", 12.00m, 24.00m, 30),
        new("Bisagra de acero 3 pulg", 18.00m, 35.00m, 25),
        new("Candado 40 mm", 45.00m, 85.00m, 15),
        new("Candado 50 mm", 65.00m, 120.00m, 12),
        new("Cadena galvanizada 3/16 metro", 18.00m, 32.00m, 30),
        new("Alambre recocido 1 kg", 32.00m, 55.00m, 20),
        new("Alambre de púas metro", 8.50m, 16.00m, 40),
        new("Remache pop 1/8", 0.40m, 1.00m, 200),
        new("Remache pop 3/16", 0.70m, 1.60m, 150)
    ];

    private static readonly SeedItem[] Plomeria =
    [
        new("Tubo PVC sanitario 2 pulg 3 m", 85.00m, 140.00m, 20),
        new("Tubo PVC sanitario 4 pulg 3 m", 165.00m, 260.00m, 12),
        new("Tubo PVC hidráulico 1/2 pulg 6 m", 42.00m, 75.00m, 25),
        new("Tubo PVC hidráulico 3/4 pulg 6 m", 68.00m, 115.00m, 20),
        new("Codo PVC sanitario 2 pulg 90", 18.00m, 35.00m, 30),
        new("Codo PVC sanitario 4 pulg 90", 42.00m, 75.00m, 16),
        new("Codo PVC hidráulico 1/2 90", 6.50m, 14.00m, 40),
        new("Codo PVC hidráulico 3/4 90", 9.00m, 18.00m, 30),
        new("Tee PVC sanitario 2 pulg", 22.00m, 42.00m, 20),
        new("Tee PVC hidráulico 1/2", 8.00m, 16.00m, 30),
        new("Cople PVC 1/2", 4.50m, 10.00m, 40),
        new("Cople PVC 3/4", 6.50m, 13.00m, 30),
        new("Cemento para PVC 240 ml", 38.00m, 68.00m, 18),
        new("Limpiador PVC 240 ml", 32.00m, 58.00m, 15),
        new("Tuboplus 1/2 pulg 6 m", 95.00m, 160.00m, 16),
        new("Tuboplus 3/4 pulg 6 m", 145.00m, 240.00m, 12),
        new("Codo Tuboplus 1/2 90", 12.00m, 24.00m, 25),
        new("Codo Tuboplus 3/4 90", 18.00m, 34.00m, 20),
        new("Tee Tuboplus 1/2", 16.00m, 32.00m, 20),
        new("Conector Tuboplus macho 1/2", 14.00m, 28.00m, 20),
        new("Llave de nariz 1/2", 55.00m, 95.00m, 12),
        new("Llave angular 1/2", 48.00m, 85.00m, 12),
        new("Mezcladora para lavabo", 220.00m, 390.00m, 6),
        new("Regadera 4 pulg", 85.00m, 150.00m, 10),
        new("Cinta teflón 1/2 pulg", 8.00m, 18.00m, 40),
        new("Cinta teflón 3/4 pulg", 12.00m, 25.00m, 30),
        new("Válvula de globo 1/2", 75.00m, 135.00m, 8),
        new("Válvula de compuerta 3/4", 145.00m, 250.00m, 6),
        new("Flotador para tinaco", 65.00m, 120.00m, 10),
        new("Empaque para WC", 18.00m, 38.00m, 15),
        new("Manguera flexible lavabo 40 cm", 35.00m, 65.00m, 12),
        new("Manguera flexible fregadero 50 cm", 42.00m, 78.00m, 10),
        new("Sifón de lavabo", 55.00m, 98.00m, 8),
        new("Coladera de piso 4 pulg", 48.00m, 90.00m, 10),
        new("Niple galvanizado 1/2 x 4 pulg", 22.00m, 42.00m, 15)
    ];

    private static readonly SeedItem[] Electrico =
    [
        new("Cinta aislar negra 18 m", 12.50m, 25.00m, 50),
        new("Cinta aislar blanca 18 m", 12.50m, 25.00m, 30),
        new("Cable THW calibre 12 metro", 12.00m, 22.00m, 100),
        new("Cable THW calibre 14 metro", 8.50m, 16.00m, 100),
        new("Cable THW calibre 10 metro", 18.00m, 32.00m, 80),
        new("Cable dúplex calibre 12 metro", 22.00m, 38.00m, 60),
        new("Apagador sencillo", 18.00m, 35.00m, 25),
        new("Apagador doble", 28.00m, 52.00m, 20),
        new("Contacto dúplex", 22.00m, 42.00m, 25),
        new("Contacto con tierra", 32.00m, 58.00m, 20),
        new("Placa de 1 ventana", 8.00m, 18.00m, 30),
        new("Placa de 2 ventanas", 12.00m, 24.00m, 25),
        new("Foco LED 9 W", 22.00m, 45.00m, 30),
        new("Foco LED 12 W", 28.00m, 55.00m, 25),
        new("Socket de porcelana", 18.00m, 35.00m, 20),
        new("Socket de baquelita", 12.00m, 25.00m, 20),
        new("Breaker 1 polo 15 A", 85.00m, 150.00m, 10),
        new("Breaker 1 polo 20 A", 95.00m, 165.00m, 10),
        new("Breaker 2 polos 30 A", 185.00m, 320.00m, 6),
        new("Centro de carga 4 polos", 280.00m, 480.00m, 4),
        new("Caja chalupa", 8.00m, 16.00m, 40),
        new("Caja cuadrada 4x4", 18.00m, 34.00m, 20),
        new("Tubo conduit 1/2 3 m", 42.00m, 75.00m, 15),
        new("Conector conduit 1/2", 6.00m, 14.00m, 30),
        new("Cinta aislar verde 18 m", 14.00m, 28.00m, 20),
        new("Clavija uso rudo", 28.00m, 52.00m, 15),
        new("Extensión uso rudo 5 m", 95.00m, 165.00m, 8),
        new("Foco espiral 20 W", 35.00m, 65.00m, 20),
        new("Timbre ding-dong", 75.00m, 135.00m, 6),
        new("Portalámpara de baquelita", 15.00m, 32.00m, 15)
    ];

    private static readonly SeedItem[] Herramienta =
    [
        new("Martillo uña curva 16 oz mango madera", 95.00m, 160.00m, 10),
        new("Martillo de bola 16 oz", 110.00m, 185.00m, 8),
        new("Pinza de presión 10 pulg", 110.00m, 195.00m, 15),
        new("Pinza de electricista 8 pulg", 75.00m, 135.00m, 12),
        new("Pinza de corte diagonal 6 pulg", 65.00m, 120.00m, 12),
        new("Desarmador plano 1/4 x 4", 28.00m, 55.00m, 15),
        new("Desarmador plano 3/16 x 6", 32.00m, 62.00m, 15),
        new("Desarmador de cruz 1/4 x 4", 28.00m, 55.00m, 15),
        new("Desarmador de cruz No. 2 x 6", 35.00m, 68.00m, 12),
        new("Llave ajustable 8 pulg", 85.00m, 150.00m, 10),
        new("Llave ajustable 10 pulg", 120.00m, 210.00m, 8),
        new("Llave stilson 10 pulg", 145.00m, 250.00m, 6),
        new("Flexómetro 5 m", 55.00m, 98.00m, 12),
        new("Flexómetro 8 m", 85.00m, 150.00m, 8),
        new("Nivel de burbuja 12 pulg", 65.00m, 120.00m, 8),
        new("Serrucho 20 pulg", 95.00m, 170.00m, 6),
        new("Arco de segueta", 55.00m, 98.00m, 10),
        new("Segueta 12 pulg 24 dientes", 18.00m, 35.00m, 20),
        new("Cincel 3/4 pulg", 45.00m, 85.00m, 8),
        new("Cinta métrica 3 m", 35.00m, 65.00m, 12),
        new("Espátula 3 pulg", 22.00m, 42.00m, 15),
        new("Espátula 5 pulg", 32.00m, 58.00m, 12),
        new("Cutter 18 mm", 25.00m, 48.00m, 15),
        new("Navaja multiusos", 45.00m, 85.00m, 10),
        new("Juego de llaves allen", 65.00m, 120.00m, 8)
    ];

    private static readonly SeedItem[] Pintura =
    [
        new("Pintura vinílica blanca 4 L", 185.00m, 320.00m, 12),
        new("Pintura vinílica blanca 19 L", 620.00m, 980.00m, 6),
        new("Pintura esmalte blanco 1 L", 95.00m, 165.00m, 10),
        new("Pintura esmalte negro 1 L", 95.00m, 165.00m, 8),
        new("Thinner estándar 1 L", 42.00m, 75.00m, 15),
        new("Sellador acrílico 4 L", 165.00m, 290.00m, 8),
        new("Sellador vinílico 19 L", 480.00m, 780.00m, 4),
        new("Impermeabilizante blanco 19 L", 890.00m, 1450.00m, 4),
        new("Silicón transparente 300 ml", 55.00m, 98.00m, 20),
        new("Silicón blanco 300 ml", 55.00m, 98.00m, 20),
        new("Resanador de muros 1 kg", 28.00m, 52.00m, 15),
        new("Yeso 1 kg", 18.00m, 35.00m, 20),
        new("Brocha 2 pulg", 22.00m, 42.00m, 15),
        new("Brocha 4 pulg", 38.00m, 70.00m, 12),
        new("Rodillo 9 pulg", 35.00m, 65.00m, 12),
        new("Charola para rodillo", 28.00m, 52.00m, 8),
        new("Lija de agua No. 120", 8.00m, 18.00m, 30),
        new("Lija de agua No. 220", 8.00m, 18.00m, 30),
        new("Masking tape 1 pulg", 18.00m, 35.00m, 25),
        new("Masking tape 2 pulg", 28.00m, 52.00m, 20)
    ];
}
