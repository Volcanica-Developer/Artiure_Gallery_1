/// <summary>
/// Global application configuration constants.
/// Change the BaseUrl here to switch between environments (staging, production, etc.)
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// Base URL for all API endpoints.
    /// Change this value to switch between environments:
    /// - Staging: "https://stg.artiure.com"
    /// - Production: "https://artiure.com" (example)
    /// </summary>
    public const string BaseUrl = "https://stg.artiure.com";
    //public const string BaseUrl = "https://artiure.com"; // PRODUCTION
    //public const string BaseUrl = "http://localhost:3000"; // LOCAL

    // API endpoint paths (can be combined with BaseUrl)
    public const string CartEndpoint = "/api/user/cart";
    public const string FavouriteEndpoint = "/api/user/favourite";
    public const string ExhibitionEndpoint = "/api/artist/exhibition/getExhibitionFromId";
    public const string SendPriceOnRequestEndpoint = "/api/artist/exhibition/sendPriceOnRequest";
    public const string AutheticationEndpoint = "/api/auth/verifyToken";

    // Full URLs (for convenience)
    public static string CartUrl => BaseUrl + CartEndpoint;
    public static string FavouriteUrl => BaseUrl + FavouriteEndpoint;
    public static string ExhibitionUrl => BaseUrl + ExhibitionEndpoint;
    public static string SendPriceOnRequestUrl => BaseUrl + SendPriceOnRequestEndpoint;
    public static string AuthenticationURL => BaseUrl + AutheticationEndpoint;
}
