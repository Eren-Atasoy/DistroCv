namespace DistroCv.Core.Enums;

/// <summary>
/// Turkey cities for geographic filtering
/// Task 20.1: Geographic filter support (Validates: Requirement 22.3)
/// </summary>
public enum TurkeyCity
{
    Adana = 1,
    Adiyaman = 2,
    Afyonkarahisar = 3,
    Agri = 4,
    Aksaray = 68,
    Amasya = 5,
    Ankara = 6,
    Antalya = 7,
    Ardahan = 75,
    Artvin = 8,
    Aydin = 9,
    Balikesir = 10,
    Bartin = 74,
    Batman = 72,
    Bayburt = 69,
    Bilecik = 11,
    Bingol = 12,
    Bitlis = 13,
    Bolu = 14,
    Burdur = 15,
    Bursa = 16,
    Canakkale = 17,
    Cankiri = 18,
    Corum = 19,
    Denizli = 20,
    Diyarbakir = 21,
    Duzce = 81,
    Edirne = 22,
    Elazig = 23,
    Erzincan = 24,
    Erzurum = 25,
    Eskisehir = 26,
    Gaziantep = 27,
    Giresun = 28,
    Gumushane = 29,
    Hakkari = 30,
    Hatay = 31,
    Igdir = 76,
    Isparta = 32,
    Istanbul = 34,
    Izmir = 35,
    Kahramanmaras = 46,
    Karabuk = 78,
    Karaman = 70,
    Kars = 36,
    Kastamonu = 37,
    Kayseri = 38,
    Kilis = 79,
    Kirikkale = 71,
    Kirklareli = 39,
    Kirsehir = 40,
    Kocaeli = 41,
    Konya = 42,
    Kutahya = 43,
    Malatya = 44,
    Manisa = 45,
    Mardin = 47,
    Mersin = 33,
    Mugla = 48,
    Mus = 49,
    Nevsehir = 50,
    Nigde = 51,
    Ordu = 52,
    Osmaniye = 80,
    Rize = 53,
    Sakarya = 54,
    Samsun = 55,
    Sanliurfa = 63,
    Siirt = 56,
    Sinop = 57,
    Sirnak = 73,
    Sivas = 58,
    Tekirdag = 59,
    Tokat = 60,
    Trabzon = 61,
    Tunceli = 62,
    Usak = 64,
    Van = 65,
    Yalova = 77,
    Yozgat = 66,
    Zonguldak = 67,
    
    /// <summary>Remote work / Location independent</summary>
    Remote = 100,
    
    /// <summary>All cities</summary>
    AllCities = 999
}

/// <summary>
/// Extension methods for TurkeyCity enum
/// </summary>
public static class TurkeyCityExtensions
{
    /// <summary>
    /// Gets the proper Turkish display name for a city
    /// </summary>
    public static string GetDisplayName(this TurkeyCity city)
    {
        return city switch
        {
            TurkeyCity.Adana => "Adana",
            TurkeyCity.Adiyaman => "Adıyaman",
            TurkeyCity.Afyonkarahisar => "Afyonkarahisar",
            TurkeyCity.Agri => "Ağrı",
            TurkeyCity.Aksaray => "Aksaray",
            TurkeyCity.Amasya => "Amasya",
            TurkeyCity.Ankara => "Ankara",
            TurkeyCity.Antalya => "Antalya",
            TurkeyCity.Ardahan => "Ardahan",
            TurkeyCity.Artvin => "Artvin",
            TurkeyCity.Aydin => "Aydın",
            TurkeyCity.Balikesir => "Balıkesir",
            TurkeyCity.Bartin => "Bartın",
            TurkeyCity.Batman => "Batman",
            TurkeyCity.Bayburt => "Bayburt",
            TurkeyCity.Bilecik => "Bilecik",
            TurkeyCity.Bingol => "Bingöl",
            TurkeyCity.Bitlis => "Bitlis",
            TurkeyCity.Bolu => "Bolu",
            TurkeyCity.Burdur => "Burdur",
            TurkeyCity.Bursa => "Bursa",
            TurkeyCity.Canakkale => "Çanakkale",
            TurkeyCity.Cankiri => "Çankırı",
            TurkeyCity.Corum => "Çorum",
            TurkeyCity.Denizli => "Denizli",
            TurkeyCity.Diyarbakir => "Diyarbakır",
            TurkeyCity.Duzce => "Düzce",
            TurkeyCity.Edirne => "Edirne",
            TurkeyCity.Elazig => "Elazığ",
            TurkeyCity.Erzincan => "Erzincan",
            TurkeyCity.Erzurum => "Erzurum",
            TurkeyCity.Eskisehir => "Eskişehir",
            TurkeyCity.Gaziantep => "Gaziantep",
            TurkeyCity.Giresun => "Giresun",
            TurkeyCity.Gumushane => "Gümüşhane",
            TurkeyCity.Hakkari => "Hakkari",
            TurkeyCity.Hatay => "Hatay",
            TurkeyCity.Igdir => "Iğdır",
            TurkeyCity.Isparta => "Isparta",
            TurkeyCity.Istanbul => "İstanbul",
            TurkeyCity.Izmir => "İzmir",
            TurkeyCity.Kahramanmaras => "Kahramanmaraş",
            TurkeyCity.Karabuk => "Karabük",
            TurkeyCity.Karaman => "Karaman",
            TurkeyCity.Kars => "Kars",
            TurkeyCity.Kastamonu => "Kastamonu",
            TurkeyCity.Kayseri => "Kayseri",
            TurkeyCity.Kilis => "Kilis",
            TurkeyCity.Kirikkale => "Kırıkkale",
            TurkeyCity.Kirklareli => "Kırklareli",
            TurkeyCity.Kirsehir => "Kırşehir",
            TurkeyCity.Kocaeli => "Kocaeli",
            TurkeyCity.Konya => "Konya",
            TurkeyCity.Kutahya => "Kütahya",
            TurkeyCity.Malatya => "Malatya",
            TurkeyCity.Manisa => "Manisa",
            TurkeyCity.Mardin => "Mardin",
            TurkeyCity.Mersin => "Mersin",
            TurkeyCity.Mugla => "Muğla",
            TurkeyCity.Mus => "Muş",
            TurkeyCity.Nevsehir => "Nevşehir",
            TurkeyCity.Nigde => "Niğde",
            TurkeyCity.Ordu => "Ordu",
            TurkeyCity.Osmaniye => "Osmaniye",
            TurkeyCity.Rize => "Rize",
            TurkeyCity.Sakarya => "Sakarya",
            TurkeyCity.Samsun => "Samsun",
            TurkeyCity.Sanliurfa => "Şanlıurfa",
            TurkeyCity.Siirt => "Siirt",
            TurkeyCity.Sinop => "Sinop",
            TurkeyCity.Sirnak => "Şırnak",
            TurkeyCity.Sivas => "Sivas",
            TurkeyCity.Tekirdag => "Tekirdağ",
            TurkeyCity.Tokat => "Tokat",
            TurkeyCity.Trabzon => "Trabzon",
            TurkeyCity.Tunceli => "Tunceli",
            TurkeyCity.Usak => "Uşak",
            TurkeyCity.Van => "Van",
            TurkeyCity.Yalova => "Yalova",
            TurkeyCity.Yozgat => "Yozgat",
            TurkeyCity.Zonguldak => "Zonguldak",
            TurkeyCity.Remote => "Uzaktan Çalışma",
            TurkeyCity.AllCities => "Tüm Şehirler",
            _ => city.ToString()
        };
    }

    /// <summary>
    /// Gets the English display name for a city
    /// </summary>
    public static string GetDisplayNameEn(this TurkeyCity city)
    {
        return city switch
        {
            TurkeyCity.Remote => "Remote Work",
            TurkeyCity.AllCities => "All Cities",
            _ => city.GetDisplayName() // Turkish city names are same in English
        };
    }

    /// <summary>
    /// Gets major cities (most common for job seekers)
    /// </summary>
    public static TurkeyCity[] GetMajorCities()
    {
        return new[]
        {
            TurkeyCity.Istanbul,
            TurkeyCity.Ankara,
            TurkeyCity.Izmir,
            TurkeyCity.Bursa,
            TurkeyCity.Antalya,
            TurkeyCity.Kocaeli,
            TurkeyCity.Konya,
            TurkeyCity.Gaziantep,
            TurkeyCity.Adana,
            TurkeyCity.Eskisehir,
            TurkeyCity.Remote
        };
    }
}

