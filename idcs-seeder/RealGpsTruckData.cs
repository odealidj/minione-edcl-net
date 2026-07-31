namespace EDCL.IdcsSeeder;

public static class RealGpsTruckData
{
    public static readonly (string LpCode, string PlateNumber, string GpsVehicleId, string DriverNik, string DriverName, string DriverPhone)[] Data = new[]
    {
        // Data from INOVATRACK JSON
        ("AJL", "B 049 RC", "357544371301389", "3201019988776655", "AJL DRIVER 1", "081299887766"),
        ("TTL", "B 050 RC", "357544371301390", "3201019988776656", "TTL DRIVER 1", "081299887767"),
        ("SYN", "B 051 RC", "357544371301391", "3201019988776657", "SYN DRIVER 1", "081299887768"),

        // Data from JITRA JSON
        ("SGL", "196", "350317178629519", "3201019988776658", "SGL DRIVER 1", "081299887769"),
        ("NSI", "Avanza Silver KZK", "350317178726307", "3201019988776659", "NSI DRIVER 1", "081299887770"),
        ("HKR", "186", "350424064005414", "3201019988776660", "HKR DRIVER 1", "081299887771"),

        // Data from PUNINAR JSON
        ("PYL", "B 9710 TXS", "B 9710 TXS", "8880000000000001", "Driver Puninar 1", "081100000001"),
        ("PYL", "B 9758 TYY", "B 9758 TYY", "8880000000000002", "Driver Puninar 2", "081100000002"),
        ("NYK", "B 9893 TXT", "B 9893 TXT", "8880000000000004", "Driver Puninar 4", "081100000004"),

        // Data from MULIATRACK JSON
        ("ALS", "B 9399 FXU", "61713", "3201019988776661", "ALS DRIVER 1", "081299887772"),
        ("DNX", "B 9491 UXT", "66488", "3201019988776662", "DNX DRIVER 1", "081299887773"),
        ("TTN", "B 9489 UXT", "66487", "3201019988776663", "TTN DRIVER 1", "081299887774")
    };
}
