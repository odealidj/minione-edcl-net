using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Ports;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Infrastructure.Persistence.Repositories;

public sealed class DriverRepository(AuthDbContext db) : IDriverRepository
{
    public Task<Driver?> FindActiveByPhoneAsync(string phoneNumber, CancellationToken ct)
        => db.Drivers
            .FirstOrDefaultAsync(d => d.PhoneNumber == phoneNumber && d.IsActive, ct);

    public Task<Driver?> FindByIdAsync(long id, CancellationToken ct)
        => db.Drivers
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Driver?> FindByNikAsync(string nik, CancellationToken ct)
        => db.Drivers
            .FirstOrDefaultAsync(d => d.Nik == nik && !d.IsDeleted, ct);

    public async Task AddAsync(Driver driver, CancellationToken ct)
        => await db.Drivers.AddAsync(driver, ct);

    public Task UpdateAsync(Driver driver, CancellationToken ct)
    {
        db.Drivers.Update(driver);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}

public sealed class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashedTokenAsync(string hashedToken, CancellationToken ct)
        => db.RefreshTokens
            .Include(rt => rt.Driver)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, ct);

    public Task<IList<RefreshToken>> GetActiveTokensByDriverAsync(long driverId, CancellationToken ct)
        => db.RefreshTokens
            .Where(rt => rt.DriverId == driverId && !rt.IsRevoked)
            .ToListAsync(ct)
            .ContinueWith(t => (IList<RefreshToken>)t.Result, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
        => await db.RefreshTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}

/// <summary>
/// Implements IDriverPort (Shared.Kernel contract) so other modules
/// can get driver info without directly referencing Auth's DbContext.
/// </summary>
public sealed class DriverPortAdapter(AuthDbContext db) : IDriverPort
{
    public async Task<DriverInfo?> GetActiveDriverByIdAsync(long driverId, CancellationToken ct)
    {
        var driver = await db.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == driverId && d.IsActive, ct);

        if (driver is null) return null;

        return new DriverInfo(
            Id: driver.Id,
            Nik: driver.Nik,
            Name: driver.Name,
            PhoneNumber: driver.PhoneNumber,
            PhotoUrl: driver.PhotoUrl,
            LogisticPartnerId: driver.LogisticPartnerId,
            LogisticPartnerName: null, // LogisticPartner has been moved to Driver module
            IsActive: driver.IsActive);
    }

    public async Task<IReadOnlyList<DriverInfo>> GetDriversByIdsAsync(IEnumerable<long> driverIds, CancellationToken ct)
    {
        var drivers = await db.Drivers
            .AsNoTracking()
            .Where(d => driverIds.Contains(d.Id))
            .ToListAsync(ct);

        return drivers.Select(driver => new DriverInfo(
            Id: driver.Id,
            Nik: driver.Nik,
            Name: driver.Name,
            PhoneNumber: driver.PhoneNumber,
            PhotoUrl: driver.PhotoUrl,
            LogisticPartnerId: driver.LogisticPartnerId,
            LogisticPartnerName: null,
            IsActive: driver.IsActive)).ToList();
    }

    public async Task<IReadOnlyList<DriverInfo>> GetActiveDriversByLogisticPartnerIdAsync(long logisticPartnerId, CancellationToken ct = default)
    {
        return await db.Drivers
            .AsNoTracking()
            .Where(d => d.LogisticPartnerId == logisticPartnerId && d.IsActive)
            .Select(d => new DriverInfo(
                d.Id, 
                d.Nik, 
                d.Name, 
                d.PhoneNumber, 
                d.PhotoUrl, 
                d.LogisticPartnerId, 
                null, 
                d.IsActive))
            .ToListAsync(ct);
    }
}

public sealed class AppUserRepository(AuthDbContext db) : IAppUserRepository
{
    public Task<AppUser?> FindByIdAsync(long id, CancellationToken ct)
        => db.AppUsers
             .Include(u => u.Role)
             .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct)
        => db.AppUsers
             .Include(u => u.Role)
             .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct)
        => await db.AppUsers.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}

public sealed class RoleRepository(AuthDbContext db) : IRoleRepository
{
    public Task<Role?> FindByCodeAsync(string code, CancellationToken ct)
        => db.Roles.FirstOrDefaultAsync(r => r.Code == code && r.IsActive && !r.IsDeleted, ct);
}
