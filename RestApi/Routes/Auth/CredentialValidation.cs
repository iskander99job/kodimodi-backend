using System.Diagnostics.CodeAnalysis;

namespace RestApi.Routes.Auth;

internal static class CredentialValidation
{
    public static bool IsNicknameValid([NotNullWhen(true)] string? nickname) =>
        nickname is { Length: >= 4 and <= 15 } &&
        nickname.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    public static bool IsPasswordValid([NotNullWhen(true)] string? password) =>
        password is { Length: >= 6 and <= 30 } &&
        password.All(c => c is >= '!' and <= '~');
}
