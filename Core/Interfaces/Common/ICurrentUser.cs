namespace Core.Interfaces.Common;

/// <summary>The signed-in user of the current request (read from the access token claims).</summary>
public interface ICurrentUser
{
    long? GetUserId();
    string GetUserName();
}
