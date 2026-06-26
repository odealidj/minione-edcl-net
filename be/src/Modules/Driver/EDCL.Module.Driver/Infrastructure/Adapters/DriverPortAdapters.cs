using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Infrastructure.Adapters;

/// <summary>
/// Real implementation of ISupplierPort — reads from [driver].[suppliers] table.
/// Registered in DriverModuleRegistration and injected into Job module via DI.
/// </summary>
public sealed class SupplierPortAdapter(DriverDbContext db) : ISupplierPort
{
    public async Task<SupplierInfo?> GetSupplierByIdAsync(long supplierId, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, ct);

        if (supplier is null) return null;

        return new SupplierInfo(
            Id: supplier.Id,
            SupplierCode: supplier.SupplierCode,
            Name: supplier.Name,
            Address: supplier.Address,
            Latitude: supplier.Latitude,
            Longitude: supplier.Longitude,
            GeofenceRadiusMeters: supplier.GeofenceRadiusMeters);
    }

    public async Task<SupplierInfo?> GetSupplierByCodeAsync(string supplierCode, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode, ct);

        if (supplier is null) return null;

        return new SupplierInfo(
            Id: supplier.Id,
            SupplierCode: supplier.SupplierCode,
            Name: supplier.Name,
            Address: supplier.Address,
            Latitude: supplier.Latitude,
            Longitude: supplier.Longitude,
            GeofenceRadiusMeters: supplier.GeofenceRadiusMeters);
    }
}

/// <summary>
/// Real implementation of ITruckPort — reads from [driver].[trucks] table.
/// </summary>
public sealed class TruckPortAdapter(DriverDbContext db) : ITruckPort
{
    public async Task<TruckInfo?> GetTruckByIdAsync(long truckId, CancellationToken ct = default)
    {
        var truck = await db.Trucks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == truckId, ct);

        if (truck is null) return null;

        return new TruckInfo(
            Id: truck.Id,
            PlateNumber: truck.PlateNumber,
            VehicleType: truck.VehicleType);
    }
}
