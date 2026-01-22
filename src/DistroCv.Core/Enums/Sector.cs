namespace DistroCv.Core.Enums;

/// <summary>
/// Industry sectors for job categorization and user preferences
/// Task 20.1: Sector taxonomy with 14+ categories (Validates: Requirement 22.1)
/// </summary>
public enum Sector
{
    /// <summary>Technology & Software</summary>
    Technology = 1,
    
    /// <summary>Finance & Banking</summary>
    Finance = 2,
    
    /// <summary>Healthcare & Medical</summary>
    Healthcare = 3,
    
    /// <summary>E-Commerce & Retail</summary>
    ECommerce = 4,
    
    /// <summary>Manufacturing & Production</summary>
    Manufacturing = 5,
    
    /// <summary>Education & Training</summary>
    Education = 6,
    
    /// <summary>Consulting & Professional Services</summary>
    Consulting = 7,
    
    /// <summary>Marketing & Advertising</summary>
    Marketing = 8,
    
    /// <summary>Telecommunications</summary>
    Telecommunications = 9,
    
    /// <summary>Energy & Utilities</summary>
    Energy = 10,
    
    /// <summary>Automotive & Transportation</summary>
    Automotive = 11,
    
    /// <summary>Construction & Real Estate</summary>
    Construction = 12,
    
    /// <summary>Tourism & Hospitality</summary>
    Tourism = 13,
    
    /// <summary>Media & Entertainment</summary>
    Media = 14,
    
    /// <summary>Agriculture & Food</summary>
    Agriculture = 15,
    
    /// <summary>Legal Services</summary>
    Legal = 16,
    
    /// <summary>Human Resources</summary>
    HumanResources = 17,
    
    /// <summary>Logistics & Supply Chain</summary>
    Logistics = 18,
    
    /// <summary>Insurance</summary>
    Insurance = 19,
    
    /// <summary>Government & Public Sector</summary>
    Government = 20,
    
    /// <summary>Pharmaceuticals & Biotechnology</summary>
    Pharmaceuticals = 21,
    
    /// <summary>Aerospace & Defense</summary>
    Aerospace = 22,
    
    /// <summary>Non-Profit & NGO</summary>
    NonProfit = 23,
    
    /// <summary>Other Industries</summary>
    Other = 99
}

/// <summary>
/// Extension methods for Sector enum
/// </summary>
public static class SectorExtensions
{
    /// <summary>
    /// Gets the Turkish display name for a sector
    /// </summary>
    public static string GetDisplayNameTr(this Sector sector)
    {
        return sector switch
        {
            Sector.Technology => "Teknoloji & Yazılım",
            Sector.Finance => "Finans & Bankacılık",
            Sector.Healthcare => "Sağlık & Medikal",
            Sector.ECommerce => "E-Ticaret & Perakende",
            Sector.Manufacturing => "Üretim & İmalat",
            Sector.Education => "Eğitim & Öğretim",
            Sector.Consulting => "Danışmanlık & Profesyonel Hizmetler",
            Sector.Marketing => "Pazarlama & Reklamcılık",
            Sector.Telecommunications => "Telekomünikasyon",
            Sector.Energy => "Enerji & Altyapı",
            Sector.Automotive => "Otomotiv & Ulaşım",
            Sector.Construction => "İnşaat & Gayrimenkul",
            Sector.Tourism => "Turizm & Otelcilik",
            Sector.Media => "Medya & Eğlence",
            Sector.Agriculture => "Tarım & Gıda",
            Sector.Legal => "Hukuk Hizmetleri",
            Sector.HumanResources => "İnsan Kaynakları",
            Sector.Logistics => "Lojistik & Tedarik Zinciri",
            Sector.Insurance => "Sigortacılık",
            Sector.Government => "Kamu & Devlet",
            Sector.Pharmaceuticals => "İlaç & Biyoteknoloji",
            Sector.Aerospace => "Havacılık & Savunma",
            Sector.NonProfit => "Sivil Toplum Kuruluşları",
            Sector.Other => "Diğer",
            _ => "Bilinmiyor"
        };
    }

    /// <summary>
    /// Gets the English display name for a sector
    /// </summary>
    public static string GetDisplayNameEn(this Sector sector)
    {
        return sector switch
        {
            Sector.Technology => "Technology & Software",
            Sector.Finance => "Finance & Banking",
            Sector.Healthcare => "Healthcare & Medical",
            Sector.ECommerce => "E-Commerce & Retail",
            Sector.Manufacturing => "Manufacturing & Production",
            Sector.Education => "Education & Training",
            Sector.Consulting => "Consulting & Professional Services",
            Sector.Marketing => "Marketing & Advertising",
            Sector.Telecommunications => "Telecommunications",
            Sector.Energy => "Energy & Utilities",
            Sector.Automotive => "Automotive & Transportation",
            Sector.Construction => "Construction & Real Estate",
            Sector.Tourism => "Tourism & Hospitality",
            Sector.Media => "Media & Entertainment",
            Sector.Agriculture => "Agriculture & Food",
            Sector.Legal => "Legal Services",
            Sector.HumanResources => "Human Resources",
            Sector.Logistics => "Logistics & Supply Chain",
            Sector.Insurance => "Insurance",
            Sector.Government => "Government & Public Sector",
            Sector.Pharmaceuticals => "Pharmaceuticals & Biotechnology",
            Sector.Aerospace => "Aerospace & Defense",
            Sector.NonProfit => "Non-Profit & NGO",
            Sector.Other => "Other",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the display name based on language preference
    /// </summary>
    public static string GetDisplayName(this Sector sector, string language = "tr")
    {
        return language?.ToLower() == "en" ? sector.GetDisplayNameEn() : sector.GetDisplayNameTr();
    }
}

