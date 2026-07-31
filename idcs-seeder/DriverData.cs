using System.Collections.Generic;

namespace EDCL.IdcsSeeder;

public static class DriverData
{
    public static readonly List<(string Name, string Nik, string Phone, string LogisticPartnerCode)> Data = new()
    {
        ("AJL DRIVER 1", "3201019988776655", "081299887766", "AJL"),
        ("TTL DRIVER 1", "3201019988776656", "081299887767", "TTL"),
        ("SYN DRIVER 1", "3201019988776657", "081299887768", "SYN"),
        ("SGL DRIVER 1", "3201019988776658", "081299887769", "SGL"),
        ("NSI DRIVER 1", "3201019988776659", "081299887770", "NSI"),
        ("HKR DRIVER 1", "3201019988776660", "081299887771", "HKR"),
        ("Driver Puninar 1", "8880000000000001", "081100000001", "PYL"),
        ("Driver Puninar 2", "8880000000000002", "081100000002", "PYL"),
        ("Driver Puninar 4", "8880000000000004", "081100000004", "NYK"),
        ("ALS DRIVER 1", "3201019988776661", "081299887772", "ALS"),
        ("DNX DRIVER 1", "3201019988776662", "081299887773", "DNX"),
        ("TTN DRIVER 1", "3201019988776663", "081299887774", "TTN")
    };
}
