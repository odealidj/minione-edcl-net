using System.Collections.Generic;
namespace EDCL.IdcsSeeder;

public static class RouteData
{
    public static readonly List<(string RouteCode, string CycleCode, string LogisticPartnerCode)> Data = new()
    {
        ("RD03", "01", "AJL"),
        ("RD03", "02", "AJL"),
        
        ("RD04", "01", "TTL"),
        ("RD04", "02", "TTL"),
        
        ("RD23", "01", "SYN"),
        ("RD23", "02", "SYN"),
        
        ("RD10", "01", "SGL"),
        ("RD10", "02", "SGL"),
        
        ("RD11", "01", "NSI"),
        ("RD11", "02", "NSI"),
        
        ("RD12", "01", "HKR"),
        ("RD12", "02", "HKR"),
        
        ("RD13", "01", "PYL"),
        ("RD13", "02", "PYL"),
        
        ("RD14", "01", "NYK"),
        ("RD14", "02", "NYK"),
        
        ("RD15", "01", "ALS"),
        ("RD15", "02", "ALS"),
        
        ("RD16", "01", "DNX"),
        ("RD16", "02", "DNX"),
        
        ("RD17", "01", "TTN"),
        ("RD17", "02", "TTN"),
    };
}
