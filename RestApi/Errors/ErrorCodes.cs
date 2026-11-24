namespace RestApi.Errors;

public static class ErrorCodes
{
    public const string InvalidNickname = "INVALID_NICKNAME";
    public const string InvalidPassword = "INVALID_PASSWORD";
    public const string NicknameTaken = "NICKNAME_TAKEN";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string WeaponNotFound = "WEAPON_NOT_FOUND";
    public const string WeaponAlreadyOwned = "WEAPON_ALREADY_OWNED";
    public const string WeaponNotOwned = "WEAPON_NOT_OWNED";
    public const string RankRequired = "RANK_REQUIRED";
    public const string InsufficientCoins = "INSUFFICIENT_COINS";
}