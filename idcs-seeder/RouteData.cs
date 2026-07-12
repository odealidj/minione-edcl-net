using System.Collections.Generic;
namespace EDCL.IdcsSeeder;

public static class RouteData
{
    public static readonly List<(string RouteCode, string CycleCode)> Data = new()
    {
        ("RD03", "01"),
        ("RD03", "03"),
        ("RD03", "05"),
        ("RD03", "07"),
        ("RD04", "01"),
        ("RD04", "02"),
        ("RD23", "01"),
        ("RD23", "02"),
        ("RD23", "03"),
        ("RD23", "04"),
    };
}
