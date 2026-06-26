using EDCL.Module.Auth.Domain.Entities;

namespace EDCL.Module.Auth.Application.Ports;

/// <summary>Repository port for Driver domain queries.</summary>
public interface IDriverRepository
{
    Task<Driver?> FindActiveByPhoneAsync(string phoneNumber, CancellationToken ct = default);
    Task<Driver?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Driver?> FindByNikAsync(string nik, CancellationToken ct = default);
    Task AddAsync(Driver driver, CancellationToken ct = default);
    Task UpdateAsync(Driver driver, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Repository port for RefreshToken operations.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashedTokenAsync(string hashedToken, CancellationToken ct = default);
    Task<IList<RefreshToken>> GetActiveTokensByDriverAsync(long driverId, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Port for JWT token generation and validation.</summary>
public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(Driver driver);
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(AppUser user);
    (string RawToken, string HashedToken)    GenerateRefreshToken();
    bool TryValidateRefreshToken(string hashedToken, string rawToken);
    int RefreshTokenExpiryDays { get; }
}

/// <summary>Repository port for AppUser domain operations.</summary>
public interface IAppUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Repository port for Role domain operations.</summary>
public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken ct = default);
}
