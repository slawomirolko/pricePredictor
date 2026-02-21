namespace PricePredicator.App.Weather;

internal static class CityCoordinates
{
    public static (double Lat, double Lon) GetCoordinates(City city) => city switch
    {
        City.NewYork => (40.7128, -74.0060),
        City.Moscow => (55.7558, 37.6173),
        City.Berlin => (52.5200, 13.4050),
        City.Paris => (48.8566, 2.3522),
        City.London => (51.5074, -0.1278),
        City.Beijing => (39.9042, 116.4074),
        City.Tokyo => (35.6895, 139.6917),
        City.Ottawa => (45.4215, -75.6972),
        City.Warsaw => (52.2297, 21.0122),
        City.LosAngeles => (34.0522, -118.2437),
        _ => throw new ArgumentOutOfRangeException(nameof(city), $"Coordinates not defined for {city}")
    };
}