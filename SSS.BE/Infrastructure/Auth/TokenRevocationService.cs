namespace SSS.BE.Infrastructure.Auth;

public interface ITokenRevocationService
{
    void RevokeToken(string jti);
    bool IsTokenRevoked(string jti);
    Task RevokeAllUserTokensAsync(string userId);
}

public class TokenRevocationService : ITokenRevocationService
{
    private readonly HashSet<string> _revokedTokens = new();
    private readonly Dictionary<string, HashSet<string>> _userTokens = new();
    private readonly object _lock = new();

    public void RevokeToken(string jti)
    {
        lock (_lock)
        {
            _revokedTokens.Add(jti);
        }
    }

    public bool IsTokenRevoked(string jti)
    {
        lock (_lock)
        {
            return _revokedTokens.Contains(jti);
        }
    }

    public Task RevokeAllUserTokensAsync(string userId)
    {
        lock (_lock)
        {
            if (_userTokens.TryGetValue(userId, out var userTokens))
            {
                foreach (var token in userTokens)
                {
                    _revokedTokens.Add(token);
                }
                _userTokens.Remove(userId);
            }
        }
        return Task.CompletedTask;
    }

    public void TrackUserToken(string userId, string jti)
    {
        lock (_lock)
        {
            if (!_userTokens.ContainsKey(userId))
            {
                _userTokens[userId] = new HashSet<string>();
            }
            _userTokens[userId].Add(jti);
        }
    }
}