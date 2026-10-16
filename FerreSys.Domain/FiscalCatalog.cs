namespace FerreSys.Domain;

public static class FiscalCatalog
{
    public static readonly IReadOnlyList<(string Code, string Name)> TaxRegimes =
    [
        ("601", "General de Ley Personas Morales"),
        ("603", "Personas Morales con Fines no Lucrativos"),
        ("605", "Sueldos y Salarios e Ingresos Asimilados a Salarios"),
        ("606", "Arrendamiento"),
        ("612", "Personas Físicas con Actividades Empresariales y Profesionales"),
        ("616", "Sin obligaciones fiscales"),
        ("621", "Incorporación Fiscal"),
        ("625", "Actividades Empresariales con ingresos a través de Plataformas Tecnológicas"),
        ("626", "Régimen Simplificado de Confianza")
    ];

    public static readonly IReadOnlyList<(string Code, string Name)> CfdiUses =
    [
        ("G01", "Adquisición de mercancías"),
        ("G02", "Devoluciones, descuentos o bonificaciones"),
        ("G03", "Gastos en general"),
        ("I01", "Construcciones"),
        ("I02", "Mobiliario y equipo de oficina por inversiones"),
        ("I08", "Otra maquinaria y equipo"),
        ("S01", "Sin efectos fiscales"),
        ("CP01", "Pagos")
    ];

    public static bool IsTaxRegime(string code) =>
        TaxRegimes.Any(item => item.Code == code);

    public static bool IsCfdiUse(string code) =>
        CfdiUses.Any(item => item.Code == code);
}
