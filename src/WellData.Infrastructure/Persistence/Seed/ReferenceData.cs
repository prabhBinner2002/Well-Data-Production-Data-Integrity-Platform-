using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Seed;

/// <summary>Petrinex activity and product code lists used to seed the reference tables.</summary>
public static class ReferenceData
{
    public static readonly ActivityCode[] ActivityCodes =
    [
        Activity("DIFF", "Metering difference", canBeNegative: true),
        Activity("DISP", "Disposition"),
        Activity("EMIS", "Emissions"),
        Activity("FLARE", "Flared"),
        Activity("FRAC", "Fractionation yield", canBeNegative: true),
        Activity("FUEL", "Fuel"),
        Activity("IMBAL", "Imbalance"),
        Activity("INJ", "Injection"),
        Activity("INVADJ", "Inventory adjustment", canBeNegative: true),
        Activity("INVCL", "Closing inventory"),
        Activity("INVOP", "Opening inventory"),
        Activity("LDINJ", "Load fluid injection"),
        Activity("LDINVADJ", "Load fluid inventory adjustment", canBeNegative: true),
        Activity("LDINVCL", "Load fluid closing inventory"),
        Activity("LDINVOP", "Load fluid opening inventory"),
        Activity("LDREC", "Load fluid receipt"),
        Activity("PLTUSE", "Plant use"),
        Activity("PROC", "Processing gain or loss", canBeNegative: true),
        Activity("PROD", "Production"),
        Activity("PURDISP", "Purchase disposition"),
        Activity("PURREC", "Purchase receipt"),
        Activity("REC", "Receipt"),
        Activity("ROYALTY", "Royalty"),
        Activity("SHR", "Shrinkage", canBeNegative: true),
        Activity("SHUTIN", "Shut in", reportsVolume: false),
        Activity("UTIL", "Utilities"),
        Activity("VENT", "Vented"),
    ];

    public static readonly ProductCode[] ProductCodes =
    [
        Product("ACGAS", "Acid gas"),
        Product("AIR", "Air"),
        Product("BRKWTR", "Brackish water"),
        Product("C1-MX", "Methane mix"),
        Product("C2-MX", "Ethane mix"),
        Product("C2-SP", "Ethane spec"),
        Product("C3-MX", "Propane mix"),
        Product("C3-SP", "Propane spec"),
        Product("C4-MX", "Butanes mix"),
        Product("C4-SP", "Butanes spec"),
        Product("C5-MX", "Pentanes plus mix"),
        Product("C5-SP", "Pentanes plus spec"),
        Product("C6-MX", "Hexanes plus mix"),
        Product("C6-SP", "Hexanes plus spec"),
        Product("CO2", "Carbon dioxide"),
        Product("CO2-MX", "Carbon dioxide mix"),
        Product("COND", "Condensate"),
        Product("DIESEL", "Diesel oil"),
        Product("ENTGAS", "Entrained gas"),
        Product("FSHWTR", "Fresh water"),
        Product("GAS", "Gas"),
        Product("HELIUM", "Helium"),
        Product("IC4-MX", "Iso-butane mix"),
        Product("IC4-SP", "Iso-butane spec"),
        Product("IC5-MX", "Iso-pentane mix"),
        Product("IC5-SP", "Iso-pentane spec"),
        Product("LITEMX", "Lite mix"),
        Product("N2", "Nitrogen"),
        Product("NC4-MX", "Normal butane mix"),
        Product("NC4-SP", "Normal butane spec"),
        Product("NC5-MX", "Normal pentane mix"),
        Product("NC5-SP", "Normal pentane spec"),
        Product("O2", "Oxygen"),
        Product("OIL", "Crude oil"),
        Product("SAND", "Sand"),
        Product("SBASE", "Sulphur basepad"),
        Product("SBLOC", "Sulphur block"),
        Product("SOLV", "Solvent"),
        Product("SPRILL", "Sulphur prilled"),
        Product("STEAM", "Steam"),
        Product("SUL", "Sulphur"),
        Product("SYNCRD", "Synthetic crude oil"),
        Product("WASTE", "Waste"),
        Product("WATER", "Water"),
    ];

    private static ActivityCode Activity(string code, string description, bool canBeNegative = false, bool reportsVolume = true) =>
        new() { Code = code, Description = description, CanBeNegative = canBeNegative, ReportsVolume = reportsVolume };

    private static ProductCode Product(string code, string description) =>
        new() { Code = code, Description = description };
}
