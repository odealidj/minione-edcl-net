using EDCL.Module.Auth.Domain.Entities;

namespace EDCL.Module.Auth.Application.Ports;

/// <summary>Repository port for Driver domain queries.</summary>
public interface IDriverRepository
{
    Task<Driver?> FindActiveByPhoneAsync(string phoneNumber, CancellationToken ct = default);
    Task<Driver?> FindByIdAsync(long id, CancellationToken ct = default);
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
    (string RawToken, string HashedToken)    GenerateRefreshToken();
    bool TryValidateRefreshToken(string hashedToken, string rawToken);
    int RefreshTokenExpiryDays { get; }
}
