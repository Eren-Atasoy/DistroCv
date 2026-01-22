using DistroCv.Core.Enums;

namespace DistroCv.Core.DTOs;

/// <summary>
/// DTO for updating user's sector and location preferences
/// Task 20.4: Sector and city selection preferences
/// </summary>
public record UpdateFilterPreferencesRequest(
    List<int>? PreferredSectors,
    List<int>? PreferredCities,
    decimal? MinSalary,
    decimal? MaxSalary,
    bool? IsRemotePreferred
);

/// <summary>
/// DTO for returning user's filter preferences
/// </summary>
public record FilterPreferencesResponse(
    List<SectorDto> PreferredSectors,
    List<CityDto> PreferredCities,
    decimal? MinSalary,
    decimal? MaxSalary,
    bool IsRemotePreferred
);

/// <summary>
/// DTO for sector information
/// </summary>
public record SectorDto(
    int Id,
    string NameTr,
    string NameEn
);

/// <summary>
/// DTO for city information
/// </summary>
public record CityDto(
    int Id,
    string Name,
    bool IsMajorCity
);

/// <summary>
/// DTO for listing all available sectors
/// </summary>
public record SectorListResponse(
    List<SectorDto> Sectors
);

/// <summary>
/// DTO for listing all available cities
/// </summary>
public record CityListResponse(
    List<CityDto> Cities,
    List<CityDto> MajorCities
);

/// <summary>
/// Helper class for building sector and city DTOs
/// </summary>
public static class FilterDtoHelper
{
    /// <summary>
    /// Gets all available sectors as DTOs
    /// </summary>
    public static List<SectorDto> GetAllSectors()
    {
        return Enum.GetValues<Sector>()
            .Select(s => new SectorDto(
                (int)s,
                s.GetDisplayNameTr(),
                s.GetDisplayNameEn()
            ))
            .OrderBy(s => s.NameTr)
            .ToList();
    }

    /// <summary>
    /// Gets all available cities as DTOs
    /// </summary>
    public static List<CityDto> GetAllCities()
    {
        var majorCities = TurkeyCityExtensions.GetMajorCities();
        
        return Enum.GetValues<TurkeyCity>()
            .Where(c => c != TurkeyCity.AllCities) // Exclude "All Cities" from list
            .Select(c => new CityDto(
                (int)c,
                c.GetDisplayName(),
                majorCities.Contains(c)
            ))
            .OrderBy(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// Gets major cities only
    /// </summary>
    public static List<CityDto> GetMajorCities()
    {
        var majorCities = TurkeyCityExtensions.GetMajorCities();
        
        return majorCities
            .Select(c => new CityDto(
                (int)c,
                c.GetDisplayName(),
                true
            ))
            .ToList();
    }

    /// <summary>
    /// Converts sector IDs to SectorDto list
    /// </summary>
    public static List<SectorDto> SectorIdsToDto(IEnumerable<int>? sectorIds)
    {
        if (sectorIds == null) return new List<SectorDto>();
        
        return sectorIds
            .Where(id => Enum.IsDefined(typeof(Sector), id))
            .Select(id => 
            {
                var sector = (Sector)id;
                return new SectorDto(id, sector.GetDisplayNameTr(), sector.GetDisplayNameEn());
            })
            .ToList();
    }

    /// <summary>
    /// Converts city IDs to CityDto list
    /// </summary>
    public static List<CityDto> CityIdsToDto(IEnumerable<int>? cityIds)
    {
        if (cityIds == null) return new List<CityDto>();
        
        var majorCities = TurkeyCityExtensions.GetMajorCities();
        
        return cityIds
            .Where(id => Enum.IsDefined(typeof(TurkeyCity), id))
            .Select(id =>
            {
                var city = (TurkeyCity)id;
                return new CityDto(id, city.GetDisplayName(), majorCities.Contains(city));
            })
            .ToList();
    }
}

