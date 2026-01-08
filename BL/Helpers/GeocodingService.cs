using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Helpers;
namespace BL.Helpers;

/// <summary>
/// Async service for geocoding and distance calculations using external APIs.
/// Implements caching (ConcurrentDictionary) to avoid duplicate API calls.
/// Stage 7 - Async network requests with cache.
/// </summary>
public static class GeocodingService
{
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    // Cache for distance calculations - ConcurrentDictionary for thread safety
    private static readonly ConcurrentDictionary<string, double> s_distanceCache = new();

    // Cache for geocoding results
    private static readonly ConcurrentDictionary<string, (double lat, double lon)?> s_geocodeCache = new();

    // Status tracking for UI display
    public enum GeocodingStatus
    {
        Success,
        Pending,
        NetworkError,
        InvalidAddress,
        NotAttempted
    }

    static GeocodingService()
    {
        // Set User-Agent for Nominatim (required by their usage policy)
        s_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WoltDeliverySystem/1.0 (Educational Project)");
    }

    // ─────────────────────────────────────────────────────────────
    // GEOCODING - Convert address to coordinates
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Geocodes an address to coordinates using Nominatim API (OpenStreetMap).
    /// Async all the way - keeps UI responsive.
    /// </summary>
    /// <param name="address">Full address string</param>
    /// <returns>Tuple of (latitude, longitude) or null if failed</returns>
    public static async Task<(double lat, double lon, GeocodingStatus status)> GeocodeAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (0, 0, GeocodingStatus.InvalidAddress);
        }

        // Check cache first
        if (s_geocodeCache.TryGetValue(address, out var cachedResult))
        {
            if (cachedResult.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[GEOCODING] Cache hit for: {address}");
                return (cachedResult.Value.lat, cachedResult.Value.lon, GeocodingStatus.Success);
            }
            return (0, 0, GeocodingStatus.InvalidAddress);
        }

        try
        {
            string encodedAddress = Uri.EscapeDataString(address);
            string url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=1";

            System.Diagnostics.Debug.WriteLine($"[GEOCODING] API call for: {address}");

            HttpResponseMessage response = await s_httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(json);

            var results = doc.RootElement;
            if (results.GetArrayLength() == 0)
            {
                s_geocodeCache.TryAdd(address, null);
                System.Diagnostics.Debug.WriteLine($"[GEOCODING] No results for: {address}");
                return (0, 0, GeocodingStatus.InvalidAddress);
            }

            double lat = double.Parse(results[0].GetProperty("lat").GetString()!);
            double lon = double.Parse(results[0].GetProperty("lon").GetString()!);

            // Store in cache
            s_geocodeCache.TryAdd(address, (lat, lon));
            System.Diagnostics.Debug.WriteLine($"[GEOCODING] Success for: {address} -> ({lat}, {lon})");

            return (lat, lon, GeocodingStatus.Success);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GEOCODING] Network error for {address}: {ex.Message}");
            return (0, 0, GeocodingStatus.NetworkError);
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[GEOCODING] Timeout for: {address}");
            return (0, 0, GeocodingStatus.NetworkError);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GEOCODING] Error for {address}: {ex.Message}");
            s_geocodeCache.TryAdd(address, null);
            return (0, 0, GeocodingStatus.InvalidAddress);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // DISTANCE CALCULATION - Using OSRM API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates driving/walking distance between two points using OSRM API.
    /// Uses caching (ConcurrentDictionary) to avoid duplicate API calls.
    /// </summary>
    /// <param name="fromLat">Source latitude</param>
    /// <param name="fromLon">Source longitude</param>
    /// <param name="toLat">Destination latitude</param>
    /// <param name="toLon">Destination longitude</param>
    /// <param name="isDriving">True for driving, false for walking</param>
    /// <returns>Distance in kilometers, or fallback air distance on error</returns>
    public static async Task<(double distance, bool isActualRoute)> GetRouteDistanceAsync(
        double fromLat, double fromLon,
        double toLat, double toLon,
        bool isDriving = true)
    {
        // Create cache key with profile and coordinates
        string profile = isDriving ? "driving" : "foot";
        string cacheKey = $"{profile}:{fromLat:F5},{fromLon:F5}->{toLat:F5},{toLon:F5}";

        // Check cache first
        if (s_distanceCache.TryGetValue(cacheKey, out double cachedDistance))
        {
            System.Diagnostics.Debug.WriteLine($"[DISTANCE] Cache hit: {cacheKey} = {cachedDistance:F2} km");
            return (cachedDistance, true);
        }

        try
        {
            // Using OSRM demo server (for educational purposes)
            // For production, use your own OSRM server or a paid service
            string url = $"https://router.project-osrm.org/route/v1/{profile}/{fromLon},{fromLat};{toLon},{toLat}?overview=false";

            System.Diagnostics.Debug.WriteLine($"[DISTANCE] API call: {profile} from ({fromLat:F4},{fromLon:F4}) to ({toLat:F4},{toLon:F4})");

            HttpResponseMessage response = await s_httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(json);

            // Check if route was found
            string code = doc.RootElement.GetProperty("code").GetString() ?? "";
            if (code != "Ok")
            {
                throw new Exception($"OSRM returned: {code}");
            }

            double distanceMeters = doc.RootElement
                .GetProperty("routes")[0]
                .GetProperty("distance")
                .GetDouble();

            double distanceKm = distanceMeters / 1000.0;

            // Store in cache
            s_distanceCache.TryAdd(cacheKey, distanceKm);
            System.Diagnostics.Debug.WriteLine($"[DISTANCE] Success: {distanceKm:F2} km");

            return (distanceKm, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DISTANCE] API error, using fallback: {ex.Message}");

            // Fallback to air distance * 1.4 (typical road factor)
            double airDistance = CalculateAirDistanceFallback(fromLat, fromLon, toLat, toLon);
            double estimatedRoadDistance = airDistance * 1.4;

            return (estimatedRoadDistance, false);
        }
    }

    /// <summary>
    /// Gets estimated travel time based on distance and delivery type.
    /// </summary>
    public static async Task<(TimeSpan duration, double distanceKm)> GetTravelTimeAsync(
        double fromLat, double fromLon,
        double toLat, double toLon,
        BO.DeliveryType deliveryType)
    {
        bool isDriving = deliveryType == BO.DeliveryType.Car || deliveryType == BO.DeliveryType.Motorcycle;
        var (distance, _) = await GetRouteDistanceAsync(fromLat, fromLon, toLat, toLon, isDriving).ConfigureAwait(false);

        // Get speed based on delivery type from config
        var config = AdminManager.GetConfig();
        double speed = deliveryType switch
        {
            BO.DeliveryType.Car => config.CarSpeed,
            BO.DeliveryType.Motorcycle => config.MotorcycleSpeed,
            BO.DeliveryType.Bicycle => config.BicycleSpeed,
            BO.DeliveryType.OnFoot => config.OnFootSpeed,
            _ => config.CarSpeed
        };

        if (speed <= 0) speed = 30; // Default fallback

        double hours = distance / speed;
        return (TimeSpan.FromHours(hours), distance);
    }

    // ─────────────────────────────────────────────────────────────
    // FALLBACK - Local air distance calculation (Haversine)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fallback air distance calculation using Haversine formula.
    /// Used when API calls fail.
    /// </summary>
    public static double CalculateAirDistanceFallback(double lat1, double lon1, double lat2, double lon2)
    {
        const double EARTH_RADIUS_KM = 6371;

        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EARTH_RADIUS_KM * c;
    }

    // ─────────────────────────────────────────────────────────────
    // CACHE MANAGEMENT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public static void ClearCache()
    {
        s_distanceCache.Clear();
        s_geocodeCache.Clear();
        System.Diagnostics.Debug.WriteLine("[GEOCODING] Cache cleared");
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public static (int geocodeCount, int distanceCount) GetCacheStats()
    {
        return (s_geocodeCache.Count, s_distanceCache.Count);
    }
}
