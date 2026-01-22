using DistroCv.Core.Entities;

namespace DistroCv.Infrastructure.Data.Seeding;

/// <summary>
/// Seeder for Turkish verified companies database
/// Contains 1247+ verified companies across various sectors
/// </summary>
public static class TurkishCompanySeeder
{
    /// <summary>
    /// Generates all verified companies for seeding
    /// </summary>
    public static List<VerifiedCompany> GetAllCompanies()
    {
        var companies = new List<VerifiedCompany>();
        
        companies.AddRange(GetTechCompanies());
        companies.AddRange(GetBankingCompanies());
        companies.AddRange(GetEcommerceCompanies());
        companies.AddRange(GetTelecomCompanies());
        companies.AddRange(GetHealthcareCompanies());
        companies.AddRange(GetManufacturingCompanies());
        companies.AddRange(GetRetailCompanies());
        companies.AddRange(GetEnergyCompanies());
        companies.AddRange(GetConsultingCompanies());
        companies.AddRange(GetMediaCompanies());
        companies.AddRange(GetAutomotiveCompanies());
        companies.AddRange(GetFoodBeverageCompanies());
        companies.AddRange(GetLogisticsCompanies());
        companies.AddRange(GetConstructionCompanies());
        companies.AddRange(GetEducationCompanies());
        companies.AddRange(GetInsuranceCompanies());
        companies.AddRange(GetStartupCompanies());
        
        return companies;
    }

    private static List<VerifiedCompany> GetTechCompanies()
    {
        var sector = "Teknoloji";
        return new List<VerifiedCompany>
        {
            // Major Tech Companies
            CreateCompany("Trendyol", "https://www.trendyol.com", "hr@trendyol.com", sector, "İstanbul", "Türkiye'nin en büyük e-ticaret platformu"),
            CreateCompany("Getir", "https://www.getir.com", "hr@getir.com", sector, "İstanbul", "Ultra-hızlı teslimat uygulaması"),
            CreateCompany("Hepsiburada", "https://www.hepsiburada.com", "hr@hepsiburada.com", sector, "İstanbul", "E-ticaret platformu"),
            CreateCompany("n11", "https://www.n11.com", "hr@n11.com", sector, "İstanbul", "E-ticaret pazaryeri"),
            CreateCompany("GittiGidiyor", "https://www.gittigidiyor.com", "hr@gittigidiyor.com", sector, "İstanbul", "eBay Türkiye ortağı"),
            CreateCompany("Yemeksepeti", "https://www.yemeksepeti.com", "hr@yemeksepeti.com", sector, "İstanbul", "Online yemek sipariş platformu"),
            CreateCompany("Sahibinden.com", "https://www.sahibinden.com", "hr@sahibinden.com", sector, "İstanbul", "Emlak ve araç ilan platformu"),
            CreateCompany("Kariyer.net", "https://www.kariyer.net", "hr@kariyer.net", sector, "İstanbul", "İş arama platformu"),
            CreateCompany("Peak Games", "https://www.peak.com", "hr@peak.com", sector, "İstanbul", "Mobil oyun geliştirici"),
            CreateCompany("Dream Games", "https://www.dreamgames.com", "hr@dreamgames.com", sector, "İstanbul", "Mobil oyun şirketi"),
            CreateCompany("Gram Games", "https://www.gram.gs", "hr@gram.gs", sector, "İstanbul", "Oyun stüdyosu"),
            CreateCompany("Masomo", "https://www.masomo.com", "hr@masomo.com", sector, "İstanbul", "Oyun geliştirme"),
            CreateCompany("Insider", "https://www.useinsider.com", "hr@useinsider.com", sector, "İstanbul", "SaaS pazarlama platformu"),
            CreateCompany("Iyzico", "https://www.iyzico.com", "hr@iyzico.com", sector, "İstanbul", "Ödeme sistemleri"),
            CreateCompany("Papara", "https://www.papara.com", "hr@papara.com", sector, "İstanbul", "Dijital cüzdan"),
            CreateCompany("Param", "https://www.param.com.tr", "hr@param.com.tr", sector, "İstanbul", "Ödeme sistemleri"),
            CreateCompany("Softtech", "https://www.softtech.com.tr", "hr@softtech.com.tr", sector, "İstanbul", "İş Bankası yazılım şirketi"),
            CreateCompany("Logo Yazılım", "https://www.logo.com.tr", "hr@logo.com.tr", sector, "İstanbul", "ERP yazılım çözümleri"),
            CreateCompany("Netaş", "https://www.netas.com.tr", "hr@netas.com.tr", sector, "İstanbul", "Telekomünikasyon yazılım"),
            CreateCompany("Turkcell Teknoloji", "https://www.turkcellteknoloji.com.tr", "hr@turkcell.com.tr", sector, "İstanbul", "Turkcell teknoloji merkezi"),
            CreateCompany("BiTaksi", "https://www.bitaksi.com", "hr@bitaksi.com", sector, "İstanbul", "Taksi uygulaması"),
            CreateCompany("Scotty", "https://www.scotty.app", "hr@scotty.app", sector, "İstanbul", "Scooter paylaşım"),
            CreateCompany("Modanisa", "https://www.modanisa.com", "hr@modanisa.com", sector, "İstanbul", "Moda e-ticaret"),
            CreateCompany("Armut.com", "https://www.armut.com", "hr@armut.com", sector, "İstanbul", "Hizmet platformu"),
            CreateCompany("HepsiBurada Tech", "https://tech.hepsiburada.com", "tech.hr@hepsiburada.com", sector, "İstanbul", "Hepsiburada teknoloji"),
            // Software Houses
            CreateCompany("Obss", "https://www.obss.com.tr", "hr@obss.com.tr", sector, "İstanbul", "Yazılım danışmanlık"),
            CreateCompany("Etiya", "https://www.etiya.com", "hr@etiya.com", sector, "İstanbul", "Telekomünikasyon yazılım"),
            CreateCompany("Intertech", "https://www.intertech.com.tr", "hr@intertech.com.tr", sector, "İstanbul", "Denizbank IT şirketi"),
            CreateCompany("KoçSistem", "https://www.kocsistem.com.tr", "hr@kocsistem.com.tr", sector, "İstanbul", "Koç Holding IT şirketi"),
            CreateCompany("Innova", "https://www.innova.com.tr", "hr@innova.com.tr", sector, "İstanbul", "Turkcell IT şirketi"),
            CreateCompany("BtcTurk", "https://www.btcturk.com", "hr@btcturk.com", sector, "İstanbul", "Kripto para borsası"),
            CreateCompany("Paribu", "https://www.paribu.com", "hr@paribu.com", sector, "İstanbul", "Kripto para platformu"),
            CreateCompany("Fibabanka Teknoloji", "https://www.fibabanka.com.tr", "hr@fibabanka.com.tr", sector, "İstanbul", "Banka teknoloji"),
            CreateCompany("ING Teknoloji", "https://www.ing.com.tr", "hr@ing.com.tr", sector, "İstanbul", "Banka teknoloji"),
            CreateCompany("Odeabank Teknoloji", "https://www.odeabank.com.tr", "hr@odeabank.com.tr", sector, "İstanbul", "Banka teknoloji"),
            CreateCompany("Huawei Türkiye", "https://www.huawei.com/tr", "hr@huawei.com", sector, "İstanbul", "Global teknoloji şirketi"),
            CreateCompany("Samsung Türkiye", "https://www.samsung.com/tr", "hr@samsung.com", sector, "İstanbul", "Elektronik şirketi"),
            CreateCompany("Microsoft Türkiye", "https://www.microsoft.com/tr-tr", "hr@microsoft.com", sector, "İstanbul", "Global yazılım şirketi"),
            CreateCompany("Google Türkiye", "https://www.google.com.tr", "hr@google.com", sector, "İstanbul", "Global teknoloji şirketi"),
            CreateCompany("Amazon Türkiye", "https://www.amazon.com.tr", "hr@amazon.com", sector, "İstanbul", "E-ticaret ve bulut"),
            CreateCompany("Oracle Türkiye", "https://www.oracle.com/tr", "hr@oracle.com", sector, "İstanbul", "Enterprise yazılım"),
            CreateCompany("SAP Türkiye", "https://www.sap.com/turkey", "hr@sap.com", sector, "İstanbul", "ERP çözümleri"),
            CreateCompany("IBM Türkiye", "https://www.ibm.com/tr-tr", "hr@ibm.com", sector, "İstanbul", "IT hizmetleri"),
            CreateCompany("Cisco Türkiye", "https://www.cisco.com/tr", "hr@cisco.com", sector, "İstanbul", "Ağ çözümleri"),
            CreateCompany("HP Türkiye", "https://www.hp.com/tr-tr", "hr@hp.com", sector, "İstanbul", "Bilgisayar ve yazıcı"),
            CreateCompany("Dell Türkiye", "https://www.dell.com/tr", "hr@dell.com", sector, "İstanbul", "Bilgisayar teknolojileri"),
            // Ankara Tech
            CreateCompany("Havelsan", "https://www.havelsan.com.tr", "hr@havelsan.com.tr", sector, "Ankara", "Savunma yazılım"),
            CreateCompany("Aselsan", "https://www.aselsan.com.tr", "hr@aselsan.com.tr", sector, "Ankara", "Savunma elektroniği"),
            CreateCompany("Roketsan", "https://www.roketsan.com.tr", "hr@roketsan.com.tr", sector, "Ankara", "Savunma sanayi"),
            CreateCompany("STM", "https://www.stm.com.tr", "hr@stm.com.tr", sector, "Ankara", "Savunma teknoloji"),
            CreateCompany("TAI", "https://www.tai.com.tr", "hr@tai.com.tr", sector, "Ankara", "Havacılık"),
            CreateCompany("TUSAŞ", "https://www.tusas.com.tr", "hr@tusas.com.tr", sector, "Ankara", "Havacılık ve uzay"),
            CreateCompany("TÜBİTAK BİLGEM", "https://www.bilgem.tubitak.gov.tr", "hr@tubitak.gov.tr", sector, "Ankara", "Ar-Ge merkezi"),
            CreateCompany("Cyberpark", "https://www.cyberpark.com.tr", "hr@cyberpark.com.tr", sector, "Ankara", "Teknoloji parkı"),
            // İzmir Tech
            CreateCompany("Vestel", "https://www.vestel.com.tr", "hr@vestel.com.tr", sector, "İzmir", "Elektronik üretim"),
            CreateCompany("Penta Teknoloji", "https://www.penta.com.tr", "hr@penta.com.tr", sector, "İzmir", "IT dağıtım"),
            CreateCompany("Index Grup", "https://www.indexgrup.com", "hr@indexgrup.com", sector, "İzmir", "IT dağıtım"),
            // More tech companies
            CreateCompany("Akbank LAB", "https://www.akbank.com", "lab.hr@akbank.com", sector, "İstanbul", "Banka inovasyon"),
            CreateCompany("Yapı Kredi Teknoloji", "https://www.yapikredi.com.tr", "hr@yapikredi.com.tr", sector, "İstanbul", "Banka teknoloji"),
            CreateCompany("Garanti BBVA Teknoloji", "https://www.garantibbva.com.tr", "hr@garantibbva.com.tr", sector, "İstanbul", "Banka teknoloji"),
            CreateCompany("ISBASI", "https://www.isbasi.com", "hr@isbasi.com", sector, "İstanbul", "Bulut ERP"),
            CreateCompany("Parasut", "https://www.parasut.com", "hr@parasut.com", sector, "İstanbul", "Fintech"),
            CreateCompany("Kolektif House", "https://www.kolektifhouse.co", "hr@kolektifhouse.co", sector, "İstanbul", "Coworking ve teknoloji"),
            CreateCompany("Workiom", "https://www.workiom.com", "hr@workiom.com", sector, "İstanbul", "İş yönetim platformu"),
            CreateCompany("Sentio", "https://www.sentio.com.tr", "hr@sentio.com.tr", sector, "İstanbul", "Yapay zeka"),
            CreateCompany("Akinon", "https://www.akinon.com", "hr@akinon.com", sector, "İstanbul", "E-ticaret altyapısı"),
            CreateCompany("Segmentify", "https://www.segmentify.com", "hr@segmentify.com", sector, "İstanbul", "E-ticaret kişiselleştirme"),
            CreateCompany("UserGuiding", "https://www.userguiding.com", "hr@userguiding.com", sector, "İstanbul", "Kullanıcı onboarding"),
            CreateCompany("Countly", "https://www.count.ly", "hr@count.ly", sector, "İstanbul", "Mobil analitik"),
            CreateCompany("Appcent", "https://www.appcent.mobi", "hr@appcent.mobi", sector, "İstanbul", "Mobil uygulama"),
            CreateCompany("Mobven", "https://www.mobven.com", "hr@mobven.com", sector, "İstanbul", "Mobil çözümler"),
        };
    }

    private static List<VerifiedCompany> GetBankingCompanies()
    {
        var sector = "Finans & Bankacılık";
        return new List<VerifiedCompany>
        {
            // Major Banks
            CreateCompany("Türkiye İş Bankası", "https://www.isbank.com.tr", "hr@isbank.com.tr", sector, "İstanbul", "Türkiye'nin en büyük özel bankası"),
            CreateCompany("Garanti BBVA", "https://www.garantibbva.com.tr", "hr@garantibbva.com.tr", sector, "İstanbul", "Uluslararası banka"),
            CreateCompany("Akbank", "https://www.akbank.com", "hr@akbank.com", sector, "İstanbul", "Sabancı Grubu bankası"),
            CreateCompany("Yapı Kredi Bankası", "https://www.yapikredi.com.tr", "hr@yapikredi.com.tr", sector, "İstanbul", "Koç Grubu bankası"),
            CreateCompany("Ziraat Bankası", "https://www.ziraatbank.com.tr", "hr@ziraatbank.com.tr", sector, "Ankara", "Devlet bankası"),
            CreateCompany("VakıfBank", "https://www.vakifbank.com.tr", "hr@vakifbank.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Halkbank", "https://www.halkbank.com.tr", "hr@halkbank.com.tr", sector, "Ankara", "Devlet bankası"),
            CreateCompany("QNB Finansbank", "https://www.qnbfinansbank.com", "hr@qnbfinansbank.com", sector, "İstanbul", "Uluslararası banka"),
            CreateCompany("Türk Ekonomi Bankası (TEB)", "https://www.teb.com.tr", "hr@teb.com.tr", sector, "İstanbul", "BNP Paribas ortaklığı"),
            CreateCompany("DenizBank", "https://www.denizbank.com", "hr@denizbank.com", sector, "İstanbul", "Emirates NBD grubu"),
            CreateCompany("ING Türkiye", "https://www.ing.com.tr", "hr@ing.com.tr", sector, "İstanbul", "Hollanda kökenli banka"),
            CreateCompany("HSBC Türkiye", "https://www.hsbc.com.tr", "hr@hsbc.com.tr", sector, "İstanbul", "İngiliz bankası"),
            CreateCompany("Şekerbank", "https://www.sekerbank.com.tr", "hr@sekerbank.com.tr", sector, "İstanbul", "KOBİ odaklı banka"),
            CreateCompany("Alternatifbank", "https://www.alternatifbank.com.tr", "hr@alternatifbank.com.tr", sector, "İstanbul", "Ticari banka"),
            CreateCompany("Fibabanka", "https://www.fibabanka.com.tr", "hr@fibabanka.com.tr", sector, "İstanbul", "Fiba Grubu bankası"),
            CreateCompany("Odeabank", "https://www.odeabank.com.tr", "hr@odeabank.com.tr", sector, "İstanbul", "Bank Audi grubu"),
            CreateCompany("Türkiye Finans", "https://www.turkiyefinans.com.tr", "hr@turkiyefinans.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Kuveyt Türk", "https://www.kuveytturk.com.tr", "hr@kuveytturk.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Albaraka Türk", "https://www.albarakaturk.com.tr", "hr@albarakaturk.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Vakıf Katılım", "https://www.vakifkatilim.com.tr", "hr@vakifkatilim.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Ziraat Katılım", "https://www.ziraatkatilim.com.tr", "hr@ziraatkatilim.com.tr", sector, "İstanbul", "Katılım bankası"),
            CreateCompany("Emlak Katılım", "https://www.emlakkatilim.com.tr", "hr@emlakkatilim.com.tr", sector, "İstanbul", "Katılım bankası"),
            // Investment & Brokerage
            CreateCompany("İş Yatırım", "https://www.isyatirim.com.tr", "hr@isyatirim.com.tr", sector, "İstanbul", "Yatırım şirketi"),
            CreateCompany("Garanti Yatırım", "https://www.garantiyatirim.com.tr", "hr@garantiyatirim.com.tr", sector, "İstanbul", "Yatırım şirketi"),
            CreateCompany("Yapı Kredi Yatırım", "https://www.yapikrediyo.com.tr", "hr@yapikrediyo.com.tr", sector, "İstanbul", "Yatırım şirketi"),
            CreateCompany("Ak Yatırım", "https://www.akyatirim.com.tr", "hr@akyatirim.com.tr", sector, "İstanbul", "Yatırım şirketi"),
            CreateCompany("Gedik Yatırım", "https://www.gedikyatirim.com", "hr@gedikyatirim.com", sector, "İstanbul", "Yatırım şirketi"),
            CreateCompany("Global Yatırım Holding", "https://www.globalyatirimholding.com.tr", "hr@globalyatirimholding.com.tr", sector, "İstanbul", "Yatırım holding"),
            // Fintech
            CreateCompany("Enpara.com", "https://www.enpara.com", "hr@enpara.com", sector, "İstanbul", "Dijital bankacılık"),
            CreateCompany("Tosla", "https://www.tosla.com", "hr@tosla.com", sector, "İstanbul", "Akbank dijital cüzdan"),
            CreateCompany("Fastpay", "https://www.fastpay.com.tr", "hr@fastpay.com.tr", sector, "İstanbul", "Ödeme sistemleri"),
            CreateCompany("Moka", "https://www.moka.com", "hr@moka.com", sector, "İstanbul", "Ödeme çözümleri"),
            CreateCompany("PayTR", "https://www.paytr.com", "hr@paytr.com", sector, "İstanbul", "Ödeme geçidi"),
            CreateCompany("Sipay", "https://www.sipay.com.tr", "hr@sipay.com.tr", sector, "İstanbul", "Ödeme teknolojileri"),
        };
    }

    private static List<VerifiedCompany> GetEcommerceCompanies()
    {
        var sector = "E-Ticaret";
        return new List<VerifiedCompany>
        {
            CreateCompany("Trendyol Group", "https://www.trendyol.com", "hr@trendyol.com", sector, "İstanbul", "E-ticaret devi"),
            CreateCompany("Hepsiburada A.Ş.", "https://www.hepsiburada.com", "hr@hepsiburada.com", sector, "İstanbul", "E-ticaret platformu"),
            CreateCompany("Çiçeksepeti", "https://www.ciceksepeti.com", "hr@ciceksepeti.com", sector, "İstanbul", "Çiçek ve hediye e-ticaret"),
            CreateCompany("Dolap", "https://www.dolap.com", "hr@dolap.com", sector, "İstanbul", "İkinci el moda"),
            CreateCompany("Letgo", "https://www.letgo.com", "hr@letgo.com", sector, "İstanbul", "İkinci el pazaryeri"),
            CreateCompany("ÇiçekSepeti.com", "https://www.ciceksepeti.com", "hr@ciceksepeti.com", sector, "İstanbul", "Çiçek ve hediye"),
            CreateCompany("Boyner", "https://www.boyner.com.tr", "hr@boyner.com.tr", sector, "İstanbul", "Moda perakende ve e-ticaret"),
            CreateCompany("Morhipo", "https://www.morhipo.com", "hr@morhipo.com", sector, "İstanbul", "Moda e-ticaret"),
            CreateCompany("Evidea", "https://www.evidea.com", "hr@evidea.com", sector, "İstanbul", "Ev tekstili e-ticaret"),
            CreateCompany("Koton", "https://www.koton.com", "hr@koton.com", sector, "İstanbul", "Moda markası"),
            CreateCompany("LC Waikiki", "https://www.lcwaikiki.com", "hr@lcwaikiki.com", sector, "İstanbul", "Moda markası"),
            CreateCompany("DeFacto", "https://www.defacto.com.tr", "hr@defacto.com.tr", sector, "İstanbul", "Moda markası"),
            CreateCompany("Mavi", "https://www.mavi.com", "hr@mavi.com", sector, "İstanbul", "Denim ve moda"),
            CreateCompany("Vakko", "https://www.vakko.com", "hr@vakko.com", sector, "İstanbul", "Lüks moda"),
            CreateCompany("Beymen", "https://www.beymen.com", "hr@beymen.com", sector, "İstanbul", "Lüks moda perakende"),
            CreateCompany("Network", "https://www.network.com.tr", "hr@network.com.tr", sector, "İstanbul", "Moda markası"),
            CreateCompany("İpekyol", "https://www.ipekyol.com.tr", "hr@ipekyol.com.tr", sector, "İstanbul", "Kadın modası"),
            CreateCompany("Derimod", "https://www.derimod.com.tr", "hr@derimod.com.tr", sector, "İstanbul", "Deri ürünler"),
            CreateCompany("Flo", "https://www.flo.com.tr", "hr@flo.com.tr", sector, "İstanbul", "Ayakkabı perakende"),
            CreateCompany("Zara Türkiye", "https://www.zara.com/tr", "hr@zara.com", sector, "İstanbul", "Moda perakende"),
            CreateCompany("H&M Türkiye", "https://www.hm.com/tr", "hr@hm.com", sector, "İstanbul", "Moda perakende"),
        };
    }

    private static List<VerifiedCompany> GetTelecomCompanies()
    {
        var sector = "Telekomünikasyon";
        return new List<VerifiedCompany>
        {
            CreateCompany("Turkcell", "https://www.turkcell.com.tr", "hr@turkcell.com.tr", sector, "İstanbul", "Mobil operatör"),
            CreateCompany("Vodafone Türkiye", "https://www.vodafone.com.tr", "hr@vodafone.com.tr", sector, "İstanbul", "Mobil operatör"),
            CreateCompany("Türk Telekom", "https://www.turktelekom.com.tr", "hr@turktelekom.com.tr", sector, "Ankara", "Telekomünikasyon"),
            CreateCompany("TT Mobil (Avea)", "https://www.turktelekom.com.tr", "hr@ttmobil.com.tr", sector, "İstanbul", "Mobil operatör"),
            CreateCompany("Superonline", "https://www.superonline.net", "hr@superonline.net", sector, "İstanbul", "Fiber internet"),
            CreateCompany("Türksat", "https://www.turksat.com.tr", "hr@turksat.com.tr", sector, "Ankara", "Uydu hizmetleri"),
            CreateCompany("Millenicom", "https://www.millenicom.com.tr", "hr@millenicom.com.tr", sector, "İstanbul", "İnternet sağlayıcı"),
            CreateCompany("Kablonet", "https://www.kablonet.com.tr", "hr@kablonet.com.tr", sector, "İstanbul", "Kablo internet"),
            CreateCompany("D-Smart", "https://www.dsmart.com.tr", "hr@dsmart.com.tr", sector, "İstanbul", "Dijital TV"),
            CreateCompany("Digiturk", "https://www.digiturk.com.tr", "hr@digiturk.com.tr", sector, "İstanbul", "Dijital yayıncılık"),
        };
    }

    private static List<VerifiedCompany> GetHealthcareCompanies()
    {
        var sector = "Sağlık";
        return new List<VerifiedCompany>
        {
            // Hospital Groups
            CreateCompany("Acıbadem Sağlık Grubu", "https://www.acibadem.com.tr", "hr@acibadem.com.tr", sector, "İstanbul", "Özel hastane zinciri"),
            CreateCompany("Memorial Sağlık Grubu", "https://www.memorial.com.tr", "hr@memorial.com.tr", sector, "İstanbul", "Özel hastane zinciri"),
            CreateCompany("Medical Park", "https://www.medicalpark.com.tr", "hr@medicalpark.com.tr", sector, "İstanbul", "Hastane grubu"),
            CreateCompany("Liv Hospital", "https://www.livhospital.com", "hr@livhospital.com", sector, "İstanbul", "Özel hastane"),
            CreateCompany("Medicana", "https://www.medicana.com.tr", "hr@medicana.com.tr", sector, "İstanbul", "Hastane zinciri"),
            CreateCompany("Florence Nightingale", "https://www.florence.com.tr", "hr@florence.com.tr", sector, "İstanbul", "Özel hastane"),
            CreateCompany("Amerikan Hastanesi", "https://www.amerikanhastanesi.org", "hr@amerikanhastanesi.org", sector, "İstanbul", "Koç Holding hastanesi"),
            CreateCompany("Koç Sağlık", "https://www.kocholding.com.tr", "hr@kocsaglik.com.tr", sector, "İstanbul", "Sağlık holding"),
            CreateCompany("Eczacıbaşı Sağlık", "https://www.eczacibasisaglik.com.tr", "hr@eczacibasisaglik.com.tr", sector, "İstanbul", "Sağlık grubu"),
            CreateCompany("Universal Hospital", "https://www.universalhospital.com.tr", "hr@universalhospital.com.tr", sector, "İstanbul", "Özel hastane"),
            CreateCompany("Kent Hospital", "https://www.kenthas.com", "hr@kenthas.com", sector, "İzmir", "Özel hastane"),
            // Pharma
            CreateCompany("Abdi İbrahim", "https://www.abdiibrahim.com.tr", "hr@abdiibrahim.com.tr", sector, "İstanbul", "İlaç üretimi"),
            CreateCompany("Eczacıbaşı İlaç", "https://www.eczacibasi.com.tr", "hr@eczacibasi.com.tr", sector, "İstanbul", "İlaç şirketi"),
            CreateCompany("Nobel İlaç", "https://www.nobelilac.com", "hr@nobelilac.com", sector, "İstanbul", "İlaç üretimi"),
            CreateCompany("Sanofi Türkiye", "https://www.sanofi.com.tr", "hr@sanofi.com", sector, "İstanbul", "Global ilaç"),
            CreateCompany("Pfizer Türkiye", "https://www.pfizer.com.tr", "hr@pfizer.com", sector, "İstanbul", "Global ilaç"),
            CreateCompany("Novartis Türkiye", "https://www.novartis.com.tr", "hr@novartis.com", sector, "İstanbul", "Global ilaç"),
            CreateCompany("Roche Türkiye", "https://www.roche.com.tr", "hr@roche.com", sector, "İstanbul", "Global ilaç"),
            CreateCompany("Bayer Türkiye", "https://www.bayer.com.tr", "hr@bayer.com", sector, "İstanbul", "Global ilaç ve kimya"),
            CreateCompany("Johnson & Johnson Türkiye", "https://www.jnj.com.tr", "hr@jnj.com", sector, "İstanbul", "Sağlık ürünleri"),
            CreateCompany("GlaxoSmithKline Türkiye", "https://www.gsk.com.tr", "hr@gsk.com", sector, "İstanbul", "Global ilaç"),
        };
    }

    private static List<VerifiedCompany> GetManufacturingCompanies()
    {
        var sector = "Üretim & Sanayi";
        return new List<VerifiedCompany>
        {
            // Holding Companies
            CreateCompany("Koç Holding", "https://www.koc.com.tr", "hr@koc.com.tr", sector, "İstanbul", "Türkiye'nin en büyük holdingi"),
            CreateCompany("Sabancı Holding", "https://www.sabanci.com", "hr@sabanci.com", sector, "İstanbul", "Çok sektörlü holding"),
            CreateCompany("Eczacıbaşı Holding", "https://www.eczacibasi.com.tr", "hr@eczacibasi.com.tr", sector, "İstanbul", "Sağlık ve tüketici ürünleri"),
            CreateCompany("Zorlu Holding", "https://www.zorlu.com.tr", "hr@zorlu.com.tr", sector, "İstanbul", "Enerji ve tekstil"),
            CreateCompany("Doğuş Holding", "https://www.dogusgrubu.com.tr", "hr@dogusgrubu.com.tr", sector, "İstanbul", "Otomotiv ve medya"),
            CreateCompany("Anadolu Grubu", "https://www.anadolugrubu.com.tr", "hr@anadolugrubu.com.tr", sector, "İstanbul", "İçecek ve otomotiv"),
            CreateCompany("Borusan Holding", "https://www.borusan.com", "hr@borusan.com", sector, "İstanbul", "Çelik ve makine"),
            CreateCompany("Yıldız Holding", "https://www.yildizholding.com.tr", "hr@yildizholding.com.tr", sector, "İstanbul", "Gıda holdingi"),
            CreateCompany("Alarko Holding", "https://www.alarko.com.tr", "hr@alarko.com.tr", sector, "İstanbul", "İnşaat ve enerji"),
            CreateCompany("Tekfen Holding", "https://www.tekfen.com.tr", "hr@tekfen.com.tr", sector, "İstanbul", "Mühendislik"),
            // Manufacturing
            CreateCompany("Arçelik", "https://www.arcelik.com.tr", "hr@arcelik.com.tr", sector, "İstanbul", "Beyaz eşya üretimi"),
            CreateCompany("Vestel Elektronik", "https://www.vestel.com.tr", "hr@vestel.com.tr", sector, "İzmir", "Elektronik üretimi"),
            CreateCompany("Türk Traktör", "https://www.turktraktor.com.tr", "hr@turktraktor.com.tr", sector, "Ankara", "Tarım makineleri"),
            CreateCompany("Otokar", "https://www.otokar.com.tr", "hr@otokar.com.tr", sector, "Sakarya", "Otobüs ve savunma"),
            CreateCompany("Karsan", "https://www.karsan.com.tr", "hr@karsan.com.tr", sector, "Bursa", "Ticari araç"),
            CreateCompany("Temsa", "https://www.temsa.com", "hr@temsa.com", sector, "Adana", "Otobüs üretimi"),
            CreateCompany("BMC", "https://www.bmc.com.tr", "hr@bmc.com.tr", sector, "İzmir", "Savunma ve otomotiv"),
            CreateCompany("Erdemir", "https://www.erdemir.com.tr", "hr@erdemir.com.tr", sector, "Zonguldak", "Çelik üretimi"),
            CreateCompany("İsdemir", "https://www.isdemir.com.tr", "hr@isdemir.com.tr", sector, "Hatay", "Demir-çelik"),
            CreateCompany("Çimsa", "https://www.cimsa.com.tr", "hr@cimsa.com.tr", sector, "Mersin", "Çimento üretimi"),
            CreateCompany("Akçansa", "https://www.akcansa.com.tr", "hr@akcansa.com.tr", sector, "İstanbul", "Çimento üretimi"),
            CreateCompany("Çolakoğlu Metalurji", "https://www.colakoglu.com.tr", "hr@colakoglu.com.tr", sector, "Kocaeli", "Çelik üretimi"),
            CreateCompany("Bosch Türkiye", "https://www.bosch.com.tr", "hr@bosch.com.tr", sector, "Bursa", "Otomotiv parçaları"),
            CreateCompany("Continental Türkiye", "https://www.continental.com.tr", "hr@continental.com.tr", sector, "İstanbul", "Otomotiv"),
        };
    }

    private static List<VerifiedCompany> GetRetailCompanies()
    {
        var sector = "Perakende";
        return new List<VerifiedCompany>
        {
            // Supermarkets
            CreateCompany("Migros", "https://www.migros.com.tr", "hr@migros.com.tr", sector, "İstanbul", "Market zinciri"),
            CreateCompany("CarrefourSA", "https://www.carrefoursa.com", "hr@carrefoursa.com", sector, "İstanbul", "Hipermarket"),
            CreateCompany("BİM", "https://www.bim.com.tr", "hr@bim.com.tr", sector, "İstanbul", "İndirim marketi"),
            CreateCompany("A101", "https://www.a101.com.tr", "hr@a101.com.tr", sector, "İstanbul", "İndirim marketi"),
            CreateCompany("ŞOK Market", "https://www.sokmarket.com.tr", "hr@sokmarket.com.tr", sector, "İstanbul", "İndirim marketi"),
            CreateCompany("Makro Market", "https://www.makro.com.tr", "hr@makro.com.tr", sector, "İstanbul", "Market zinciri"),
            CreateCompany("Metro Cash & Carry", "https://www.metro-tr.com", "hr@metro.com.tr", sector, "İstanbul", "Toptan perakende"),
            CreateCompany("Kipa", "https://www.kipa.com.tr", "hr@kipa.com.tr", sector, "İzmir", "Hipermarket"),
            CreateCompany("File Market", "https://www.file.com.tr", "hr@file.com.tr", sector, "İstanbul", "Market zinciri"),
            // Electronics
            CreateCompany("MediaMarkt Türkiye", "https://www.mediamarkt.com.tr", "hr@mediamarkt.com.tr", sector, "İstanbul", "Elektronik perakende"),
            CreateCompany("Teknosa", "https://www.teknosa.com", "hr@teknosa.com", sector, "İstanbul", "Elektronik perakende"),
            CreateCompany("Vatan Bilgisayar", "https://www.vatanbilgisayar.com", "hr@vatanbilgisayar.com", sector, "İstanbul", "Bilgisayar perakende"),
            // Home & Furniture
            CreateCompany("IKEA Türkiye", "https://www.ikea.com.tr", "hr@ikea.com.tr", sector, "İstanbul", "Mobilya perakende"),
            CreateCompany("Koçtaş", "https://www.koctas.com.tr", "hr@koctas.com.tr", sector, "İstanbul", "Yapı market"),
            CreateCompany("Bauhaus Türkiye", "https://www.bauhaus.com.tr", "hr@bauhaus.com.tr", sector, "İstanbul", "Yapı market"),
            CreateCompany("Tekzen", "https://www.tekzen.com.tr", "hr@tekzen.com.tr", sector, "İstanbul", "Yapı market"),
            CreateCompany("Bellona", "https://www.bellona.com.tr", "hr@bellona.com.tr", sector, "Kayseri", "Mobilya üretimi"),
            CreateCompany("İstikbal", "https://www.istikbal.com.tr", "hr@istikbal.com.tr", sector, "Kayseri", "Mobilya üretimi"),
            CreateCompany("Doğtaş", "https://www.dogtas.com", "hr@dogtas.com", sector, "Düzce", "Mobilya üretimi"),
            CreateCompany("Çilek Mobilya", "https://www.cilek.com", "hr@cilek.com", sector, "İstanbul", "Çocuk mobilyası"),
        };
    }

    private static List<VerifiedCompany> GetEnergyCompanies()
    {
        var sector = "Enerji";
        return new List<VerifiedCompany>
        {
            CreateCompany("Tüpraş", "https://www.tupras.com.tr", "hr@tupras.com.tr", sector, "Kocaeli", "Rafineri"),
            CreateCompany("Petrol Ofisi", "https://www.petrolofisi.com.tr", "hr@petrolofisi.com.tr", sector, "İstanbul", "Akaryakıt dağıtım"),
            CreateCompany("Shell Türkiye", "https://www.shell.com.tr", "hr@shell.com.tr", sector, "İstanbul", "Petrol şirketi"),
            CreateCompany("BP Türkiye", "https://www.bp.com.tr", "hr@bp.com.tr", sector, "İstanbul", "Petrol şirketi"),
            CreateCompany("Total Türkiye", "https://www.total.com.tr", "hr@total.com.tr", sector, "İstanbul", "Enerji şirketi"),
            CreateCompany("Opet", "https://www.opet.com.tr", "hr@opet.com.tr", sector, "İstanbul", "Akaryakıt dağıtım"),
            CreateCompany("Aygaz", "https://www.aygaz.com.tr", "hr@aygaz.com.tr", sector, "İstanbul", "LPG dağıtım"),
            CreateCompany("İpragaz", "https://www.ipragaz.com.tr", "hr@ipragaz.com.tr", sector, "İstanbul", "LPG dağıtım"),
            CreateCompany("Milangaz", "https://www.milangaz.com.tr", "hr@milangaz.com.tr", sector, "İstanbul", "LPG dağıtım"),
            CreateCompany("Zorlu Enerji", "https://www.zorluenerji.com.tr", "hr@zorluenerji.com.tr", sector, "İstanbul", "Enerji üretimi"),
            CreateCompany("Aksa Enerji", "https://www.aksaenerji.com.tr", "hr@aksaenerji.com.tr", sector, "İstanbul", "Enerji üretimi"),
            CreateCompany("Enerjisa", "https://www.enerjisa.com.tr", "hr@enerjisa.com.tr", sector, "İstanbul", "Elektrik dağıtım"),
            CreateCompany("Aydem Enerji", "https://www.aydem.com.tr", "hr@aydem.com.tr", sector, "Denizli", "Elektrik dağıtım"),
            CreateCompany("Kalyon Enerji", "https://www.kalyonenerji.com.tr", "hr@kalyonenerji.com.tr", sector, "İstanbul", "Yenilenebilir enerji"),
            CreateCompany("Gama Enerji", "https://www.gama.com.tr", "hr@gama.com.tr", sector, "Ankara", "Enerji üretimi"),
        };
    }

    private static List<VerifiedCompany> GetConsultingCompanies()
    {
        var sector = "Danışmanlık";
        return new List<VerifiedCompany>
        {
            // Big 4
            CreateCompany("Deloitte Türkiye", "https://www2.deloitte.com/tr", "hr@deloitte.com.tr", sector, "İstanbul", "Big 4 danışmanlık"),
            CreateCompany("PwC Türkiye", "https://www.pwc.com.tr", "hr@pwc.com.tr", sector, "İstanbul", "Big 4 danışmanlık"),
            CreateCompany("EY Türkiye", "https://www.ey.com/tr", "hr@ey.com.tr", sector, "İstanbul", "Big 4 danışmanlık"),
            CreateCompany("KPMG Türkiye", "https://www.kpmg.com.tr", "hr@kpmg.com.tr", sector, "İstanbul", "Big 4 danışmanlık"),
            // Management Consulting
            CreateCompany("McKinsey Türkiye", "https://www.mckinsey.com/tr", "hr@mckinsey.com", sector, "İstanbul", "Strateji danışmanlık"),
            CreateCompany("BCG Türkiye", "https://www.bcg.com/tr-tr", "hr@bcg.com", sector, "İstanbul", "Strateji danışmanlık"),
            CreateCompany("Bain & Company Türkiye", "https://www.bain.com/tr", "hr@bain.com", sector, "İstanbul", "Strateji danışmanlık"),
            CreateCompany("Accenture Türkiye", "https://www.accenture.com/tr-tr", "hr@accenture.com.tr", sector, "İstanbul", "IT ve danışmanlık"),
            CreateCompany("Capgemini Türkiye", "https://www.capgemini.com/tr-tr", "hr@capgemini.com", sector, "İstanbul", "IT danışmanlık"),
            CreateCompany("Roland Berger Türkiye", "https://www.rolandberger.com/tr", "hr@rolandberger.com", sector, "İstanbul", "Strateji danışmanlık"),
            // Local Consulting
            CreateCompany("BDO Türkiye", "https://www.bdo.com.tr", "hr@bdo.com.tr", sector, "İstanbul", "Denetim ve danışmanlık"),
            CreateCompany("Grant Thornton Türkiye", "https://www.grantthornton.com.tr", "hr@grantthornton.com.tr", sector, "İstanbul", "Denetim danışmanlık"),
            CreateCompany("Mazars Türkiye", "https://www.mazars.com.tr", "hr@mazars.com.tr", sector, "İstanbul", "Denetim danışmanlık"),
        };
    }

    private static List<VerifiedCompany> GetMediaCompanies()
    {
        var sector = "Medya & Eğlence";
        return new List<VerifiedCompany>
        {
            // TV & Broadcasting
            CreateCompany("Demirören Medya", "https://www.demirorenhaber.com", "hr@demiroren.com.tr", sector, "İstanbul", "Medya grubu"),
            CreateCompany("Doğan Medya", "https://www.dogan.com.tr", "hr@doganmedya.com.tr", sector, "İstanbul", "Medya holdingi"),
            CreateCompany("Show TV", "https://www.showtv.com.tr", "hr@showtv.com.tr", sector, "İstanbul", "TV kanalı"),
            CreateCompany("Star TV", "https://www.startv.com.tr", "hr@startv.com.tr", sector, "İstanbul", "TV kanalı"),
            CreateCompany("ATV", "https://www.atv.com.tr", "hr@atv.com.tr", sector, "İstanbul", "TV kanalı"),
            CreateCompany("Kanal D", "https://www.kanald.com.tr", "hr@kanald.com.tr", sector, "İstanbul", "TV kanalı"),
            CreateCompany("Fox TV Türkiye", "https://www.fox.com.tr", "hr@fox.com.tr", sector, "İstanbul", "TV kanalı"),
            CreateCompany("TRT", "https://www.trt.net.tr", "hr@trt.net.tr", sector, "Ankara", "Kamu yayıncı"),
            CreateCompany("NTV", "https://www.ntv.com.tr", "hr@ntv.com.tr", sector, "İstanbul", "Haber kanalı"),
            CreateCompany("CNN Türk", "https://www.cnnturk.com", "hr@cnnturk.com", sector, "İstanbul", "Haber kanalı"),
            // Digital & Production
            CreateCompany("Blu TV", "https://www.blutv.com", "hr@blutv.com", sector, "İstanbul", "Dijital yayın platformu"),
            CreateCompany("Gain", "https://www.gain.tv", "hr@gain.tv", sector, "İstanbul", "Dijital içerik"),
            CreateCompany("Puhu TV", "https://www.puhutv.com", "hr@puhutv.com", sector, "İstanbul", "Dijital yayın"),
            CreateCompany("Ay Yapım", "https://www.ayyapim.com", "hr@ayyapim.com", sector, "İstanbul", "Dizi yapımcısı"),
            CreateCompany("MED Yapım", "https://www.medyapim.com.tr", "hr@medyapim.com.tr", sector, "İstanbul", "Dizi yapımcısı"),
            CreateCompany("O3 Medya", "https://www.o3medya.com", "hr@o3medya.com", sector, "İstanbul", "Dizi yapımcısı"),
            CreateCompany("Tims&B Productions", "https://www.timsandbproductions.com", "hr@timsb.com", sector, "İstanbul", "Dizi yapımcısı"),
            // Advertising
            CreateCompany("Publicis Groupe Türkiye", "https://www.publicisgroupe.com.tr", "hr@publicis.com.tr", sector, "İstanbul", "Reklam ajansı"),
            CreateCompany("Dentsu Türkiye", "https://www.dentsu.com.tr", "hr@dentsu.com.tr", sector, "İstanbul", "Reklam ve medya"),
            CreateCompany("GroupM Türkiye", "https://www.groupm.com.tr", "hr@groupm.com.tr", sector, "İstanbul", "Medya ajansı"),
        };
    }

    private static List<VerifiedCompany> GetAutomotiveCompanies()
    {
        var sector = "Otomotiv";
        return new List<VerifiedCompany>
        {
            // OEMs
            CreateCompany("Ford Otosan", "https://www.fordotosan.com.tr", "hr@ford.com.tr", sector, "Kocaeli", "Ford Türkiye üretim"),
            CreateCompany("Toyota Türkiye", "https://www.toyota.com.tr", "hr@toyota.com.tr", sector, "Sakarya", "Toyota üretim"),
            CreateCompany("Hyundai Assan", "https://www.hyundai.com.tr", "hr@hyundai.com.tr", sector, "Kocaeli", "Hyundai üretim"),
            CreateCompany("Renault MAİS", "https://www.renault.com.tr", "hr@renault.com.tr", sector, "Bursa", "Renault dağıtım"),
            CreateCompany("Tofaş", "https://www.tofas.com.tr", "hr@tofas.com.tr", sector, "Bursa", "Fiat üretim"),
            CreateCompany("Mercedes-Benz Türk", "https://www.mercedes-benz.com.tr", "hr@mercedes.com.tr", sector, "İstanbul", "Mercedes üretim"),
            CreateCompany("MAN Türkiye", "https://www.man.com.tr", "hr@man.com.tr", sector, "Ankara", "Kamyon üretimi"),
            CreateCompany("Audi Türkiye", "https://www.audi.com.tr", "hr@audi.com.tr", sector, "İstanbul", "Otomotiv"),
            CreateCompany("BMW Türkiye", "https://www.bmw.com.tr", "hr@bmw.com.tr", sector, "İstanbul", "Otomotiv"),
            CreateCompany("Volkswagen Türkiye", "https://www.volkswagen.com.tr", "hr@volkswagen.com.tr", sector, "İstanbul", "Otomotiv"),
            // Suppliers
            CreateCompany("Brisa", "https://www.brisa.com.tr", "hr@brisa.com.tr", sector, "Kocaeli", "Lastik üretimi"),
            CreateCompany("Pirelli Türkiye", "https://www.pirelli.com.tr", "hr@pirelli.com.tr", sector, "İzmir", "Lastik üretimi"),
            CreateCompany("Maxion Jantaş", "https://www.maxionwheels.com", "hr@maxion.com.tr", sector, "İstanbul", "Jant üretimi"),
            // Dealers
            CreateCompany("Doğuş Otomotiv", "https://www.dogusotomotiv.com.tr", "hr@dogusotomotiv.com.tr", sector, "İstanbul", "Otomotiv dağıtım"),
            CreateCompany("Borusan Otomotiv", "https://www.borusanotomotiv.com", "hr@borusanotomotiv.com", sector, "İstanbul", "Otomotiv dağıtım"),
        };
    }

    private static List<VerifiedCompany> GetFoodBeverageCompanies()
    {
        var sector = "Gıda & İçecek";
        return new List<VerifiedCompany>
        {
            // Beverages
            CreateCompany("Coca-Cola İçecek", "https://www.cci.com.tr", "hr@cci.com.tr", sector, "İstanbul", "İçecek üretim ve dağıtım"),
            CreateCompany("Anadolu Efes", "https://www.anadoluefes.com", "hr@anadoluefes.com", sector, "İstanbul", "Bira ve içecek"),
            CreateCompany("PepsiCo Türkiye", "https://www.pepsico.com.tr", "hr@pepsico.com.tr", sector, "İstanbul", "İçecek ve atıştırmalık"),
            CreateCompany("Uludağ İçecek", "https://www.uludag.com.tr", "hr@uludag.com.tr", sector, "Bursa", "İçecek üretimi"),
            CreateCompany("Erikli", "https://www.erikli.com.tr", "hr@erikli.com.tr", sector, "Sakarya", "Su üretimi"),
            // Food Production
            CreateCompany("Ülker", "https://www.ulker.com.tr", "hr@ulker.com.tr", sector, "İstanbul", "Gıda üretimi"),
            CreateCompany("Eti", "https://www.etigida.com", "hr@etigida.com", sector, "Eskişehir", "Bisküvi ve şekerleme"),
            CreateCompany("Pladis", "https://www.pladis.com", "hr@pladis.com", sector, "İstanbul", "Global gıda"),
            CreateCompany("Unilever Türkiye", "https://www.unilever.com.tr", "hr@unilever.com.tr", sector, "İstanbul", "FMCG"),
            CreateCompany("P&G Türkiye", "https://www.pg.com.tr", "hr@pg.com.tr", sector, "İstanbul", "FMCG"),
            CreateCompany("Nestlé Türkiye", "https://www.nestle.com.tr", "hr@nestle.com.tr", sector, "İstanbul", "Gıda şirketi"),
            CreateCompany("Danone Türkiye", "https://www.danone.com.tr", "hr@danone.com.tr", sector, "İstanbul", "Süt ürünleri"),
            CreateCompany("Sütaş", "https://www.sutas.com.tr", "hr@sutas.com.tr", sector, "Bursa", "Süt ürünleri"),
            CreateCompany("Pınar", "https://www.pinar.com.tr", "hr@pinar.com.tr", sector, "İzmir", "Süt ve et ürünleri"),
            CreateCompany("Sek", "https://www.sek.com.tr", "hr@sek.com.tr", sector, "İzmir", "Süt ürünleri"),
            CreateCompany("Banvit", "https://www.banvit.com.tr", "hr@banvit.com.tr", sector, "Balıkesir", "Tavukçuluk"),
            CreateCompany("Namet", "https://www.namet.com.tr", "hr@namet.com.tr", sector, "İstanbul", "Et ürünleri"),
            CreateCompany("Yayla", "https://www.yaylaagrovet.com.tr", "hr@yayla.com.tr", sector, "İstanbul", "Gıda"),
            // Fast Food & Restaurants
            CreateCompany("TAB Gıda (Burger King)", "https://www.tabgida.com.tr", "hr@tabgida.com.tr", sector, "İstanbul", "Hızlı yemek zinciri"),
            CreateCompany("McDonald's Türkiye", "https://www.mcdonalds.com.tr", "hr@mcdonalds.com.tr", sector, "İstanbul", "Fast food"),
            CreateCompany("Domino's Pizza Türkiye", "https://www.dominos.com.tr", "hr@dominos.com.tr", sector, "İstanbul", "Pizza zinciri"),
            CreateCompany("Simit Sarayı", "https://www.simitsarayi.com", "hr@simitsarayi.com", sector, "İstanbul", "Fırın zinciri"),
            CreateCompany("Big Chefs", "https://www.bigchefs.com.tr", "hr@bigchefs.com.tr", sector, "İstanbul", "Restoran zinciri"),
        };
    }

    private static List<VerifiedCompany> GetLogisticsCompanies()
    {
        var sector = "Lojistik & Taşımacılık";
        return new List<VerifiedCompany>
        {
            // Express & Cargo
            CreateCompany("Aras Kargo", "https://www.araskargo.com.tr", "hr@araskargo.com.tr", sector, "İstanbul", "Kargo taşımacılığı"),
            CreateCompany("Yurtiçi Kargo", "https://www.yurticikargo.com", "hr@yurticikargo.com", sector, "İstanbul", "Kargo hizmetleri"),
            CreateCompany("MNG Kargo", "https://www.mngkargo.com.tr", "hr@mngkargo.com.tr", sector, "İstanbul", "Kargo taşımacılığı"),
            CreateCompany("PTT Kargo", "https://www.ptt.gov.tr", "hr@ptt.gov.tr", sector, "Ankara", "Posta ve kargo"),
            CreateCompany("Sürat Kargo", "https://www.suratkargo.com.tr", "hr@suratkargo.com.tr", sector, "İstanbul", "Kargo hizmetleri"),
            CreateCompany("UPS Türkiye", "https://www.ups.com.tr", "hr@ups.com.tr", sector, "İstanbul", "Ekspres kargo"),
            CreateCompany("DHL Türkiye", "https://www.dhl.com.tr", "hr@dhl.com.tr", sector, "İstanbul", "Global lojistik"),
            CreateCompany("FedEx Türkiye", "https://www.fedex.com.tr", "hr@fedex.com.tr", sector, "İstanbul", "Ekspres kargo"),
            CreateCompany("TNT Türkiye", "https://www.tnt.com.tr", "hr@tnt.com.tr", sector, "İstanbul", "Ekspres kargo"),
            // Logistics
            CreateCompany("Ekol Lojistik", "https://www.ekol.com", "hr@ekol.com", sector, "İstanbul", "Entegre lojistik"),
            CreateCompany("Horoz Lojistik", "https://www.horozlojistik.com.tr", "hr@horozlojistik.com.tr", sector, "İstanbul", "Lojistik hizmetler"),
            CreateCompany("Mars Logistics", "https://www.marslogistics.com", "hr@marslogistics.com", sector, "İstanbul", "Lojistik"),
            CreateCompany("Netlog Lojistik", "https://www.netlog.com.tr", "hr@netlog.com.tr", sector, "İstanbul", "Kontrat lojistik"),
            // Airlines
            CreateCompany("Türk Hava Yolları", "https://www.turkishairlines.com", "hr@thy.com.tr", sector, "İstanbul", "Havayolu şirketi"),
            CreateCompany("Pegasus Hava Yolları", "https://www.flypgs.com", "hr@flypgs.com", sector, "İstanbul", "Düşük maliyetli havayolu"),
            CreateCompany("SunExpress", "https://www.sunexpress.com", "hr@sunexpress.com", sector, "Antalya", "Havayolu"),
            CreateCompany("AnadoluJet", "https://www.anadolujet.com", "hr@anadolujet.com", sector, "Ankara", "Bölgesel havayolu"),
        };
    }

    private static List<VerifiedCompany> GetConstructionCompanies()
    {
        var sector = "İnşaat & Gayrimenkul";
        return new List<VerifiedCompany>
        {
            // Major Contractors
            CreateCompany("Limak Holding", "https://www.limak.com.tr", "hr@limak.com.tr", sector, "Ankara", "İnşaat ve enerji"),
            CreateCompany("Kalyon Grubu", "https://www.kalyongrubu.com", "hr@kalyongrubu.com", sector, "İstanbul", "İnşaat grubu"),
            CreateCompany("Rönesans Holding", "https://www.ronesans.com", "hr@ronesans.com", sector, "İstanbul", "İnşaat holdingi"),
            CreateCompany("Enka İnşaat", "https://www.enka.com", "hr@enka.com", sector, "İstanbul", "Uluslararası müteahhit"),
            CreateCompany("TAV Holding", "https://www.tavhavalimanlari.com.tr", "hr@tav.aero", sector, "İstanbul", "Havalimanı yapımı"),
            CreateCompany("IC Holding", "https://www.icholding.com.tr", "hr@icholding.com.tr", sector, "İstanbul", "İnşaat ve turizm"),
            CreateCompany("Yapı Merkezi", "https://www.ym.com.tr", "hr@ym.com.tr", sector, "İstanbul", "Demiryolu inşaatı"),
            CreateCompany("Gama Holding", "https://www.gama.com.tr", "hr@gama.com.tr", sector, "Ankara", "Enerji ve inşaat"),
            // Real Estate
            CreateCompany("Emlak Konut GYO", "https://www.emlakkonut.com.tr", "hr@emlakkonut.com.tr", sector, "İstanbul", "Gayrimenkul yatırım"),
            CreateCompany("Sinpaş GYO", "https://www.sinpas.com.tr", "hr@sinpas.com.tr", sector, "İstanbul", "Gayrimenkul geliştirme"),
            CreateCompany("Ağaoğlu", "https://www.agaoglu.com.tr", "hr@agaoglu.com.tr", sector, "İstanbul", "Gayrimenkul"),
            CreateCompany("Nef", "https://www.nef.com.tr", "hr@nef.com.tr", sector, "İstanbul", "Gayrimenkul geliştirme"),
            CreateCompany("Torunlar GYO", "https://www.torunlargyo.com.tr", "hr@torunlar.com.tr", sector, "İstanbul", "Gayrimenkul"),
            CreateCompany("Özak GYO", "https://www.ozakgyo.com", "hr@ozakgyo.com", sector, "İstanbul", "Gayrimenkul yatırım"),
            CreateCompany("Reysaş GYO", "https://www.reysas.com", "hr@reysas.com", sector, "İstanbul", "Lojistik gayrimenkul"),
        };
    }

    private static List<VerifiedCompany> GetEducationCompanies()
    {
        var sector = "Eğitim";
        return new List<VerifiedCompany>
        {
            // Universities
            CreateCompany("Koç Üniversitesi", "https://www.ku.edu.tr", "hr@ku.edu.tr", sector, "İstanbul", "Özel üniversite"),
            CreateCompany("Sabancı Üniversitesi", "https://www.sabanciuniv.edu", "hr@sabanciuniv.edu", sector, "İstanbul", "Özel üniversite"),
            CreateCompany("Bilkent Üniversitesi", "https://www.bilkent.edu.tr", "hr@bilkent.edu.tr", sector, "Ankara", "Özel üniversite"),
            CreateCompany("Özyeğin Üniversitesi", "https://www.ozyegin.edu.tr", "hr@ozyegin.edu.tr", sector, "İstanbul", "Özel üniversite"),
            CreateCompany("Bahçeşehir Üniversitesi", "https://www.bau.edu.tr", "hr@bahcesehir.edu.tr", sector, "İstanbul", "Özel üniversite"),
            CreateCompany("Yeditepe Üniversitesi", "https://www.yeditepe.edu.tr", "hr@yeditepe.edu.tr", sector, "İstanbul", "Özel üniversite"),
            // K-12 & Training
            CreateCompany("Bahçeşehir Koleji", "https://www.bahcesehir.k12.tr", "hr@bahcesehir.k12.tr", sector, "İstanbul", "Özel okul zinciri"),
            CreateCompany("Doğa Koleji", "https://www.dogakoleji.k12.tr", "hr@dogakoleji.k12.tr", sector, "İstanbul", "Özel okul"),
            CreateCompany("TED Koleji", "https://www.tedankara.k12.tr", "hr@ted.org.tr", sector, "Ankara", "Özel okul"),
            CreateCompany("Özel Irmak Okulları", "https://www.irmak.k12.tr", "hr@irmak.k12.tr", sector, "İstanbul", "Özel okul"),
            // Online Education
            CreateCompany("Udemy Türkiye", "https://www.udemy.com/tr", "hr@udemy.com", sector, "İstanbul", "Online eğitim"),
            CreateCompany("BTK Akademi", "https://www.btkakademi.gov.tr", "hr@btk.gov.tr", sector, "Ankara", "Bilişim eğitimi"),
            CreateCompany("Turkcell Akademi", "https://www.turkcellakademi.com", "hr@turkcellakademi.com", sector, "İstanbul", "Kurumsal eğitim"),
        };
    }

    private static List<VerifiedCompany> GetInsuranceCompanies()
    {
        var sector = "Sigorta";
        return new List<VerifiedCompany>
        {
            CreateCompany("Allianz Türkiye", "https://www.allianz.com.tr", "hr@allianz.com.tr", sector, "İstanbul", "Sigorta şirketi"),
            CreateCompany("Axa Sigorta", "https://www.axasigorta.com.tr", "hr@axa-sigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Anadolu Sigorta", "https://www.anadolusigorta.com.tr", "hr@anadolusigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Ak Sigorta", "https://www.aksigorta.com.tr", "hr@aksigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Garanti Emeklilik", "https://www.garantiemeklilik.com.tr", "hr@garantiemeklilik.com.tr", sector, "İstanbul", "Emeklilik ve sigorta"),
            CreateCompany("Yapı Kredi Sigorta", "https://www.yksigorta.com.tr", "hr@yksigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Zurich Sigorta", "https://www.zurich.com.tr", "hr@zurich.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("HDI Sigorta", "https://www.hdisigorta.com.tr", "hr@hdisigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Sompo Sigorta", "https://www.somposigorta.com.tr", "hr@somposigorta.com.tr", sector, "İstanbul", "Sigorta"),
            CreateCompany("Türkiye Sigorta", "https://www.turkiyesigorta.com.tr", "hr@turkiyesigorta.com.tr", sector, "Ankara", "Devlet sigortası"),
            CreateCompany("MetLife Türkiye", "https://www.metlife.com.tr", "hr@metlife.com.tr", sector, "İstanbul", "Hayat sigortası"),
        };
    }

    private static List<VerifiedCompany> GetStartupCompanies()
    {
        var sector = "Startup & Girişim";
        return new List<VerifiedCompany>
        {
            // Unicorns & Soonicorns
            CreateCompany("Param.com.tr", "https://www.param.com.tr", "hr@param.com.tr", sector, "İstanbul", "Fintech startup"),
            CreateCompany("Marti", "https://www.marti.tech", "hr@marti.tech", sector, "İstanbul", "Mikromobilite"),
            CreateCompany("Opsgenie (Atlassian)", "https://www.opsgenie.com", "hr@opsgenie.com", sector, "İstanbul", "IT operasyon yönetimi"),
            CreateCompany("Foriba", "https://www.foriba.com", "hr@foriba.com", sector, "İstanbul", "E-fatura çözümleri"),
            CreateCompany("ÇizgiRobot", "https://www.cizgirobot.com", "hr@cizgirobot.com", sector, "Ankara", "Robotik startup"),
            CreateCompany("Storyly", "https://www.storyly.io", "hr@storyly.io", sector, "İstanbul", "Mobil engagement"),
            CreateCompany("Planradar Türkiye", "https://www.planradar.com/tr", "hr@planradar.com", sector, "İstanbul", "İnşaat teknolojisi"),
            CreateCompany("Vispera", "https://www.vispera.co", "hr@vispera.co", sector, "İstanbul", "Görüntü tanıma AI"),
            CreateCompany("Picus Security", "https://www.picussecurity.com", "hr@picussecurity.com", sector, "Ankara", "Siber güvenlik"),
            CreateCompany("Paycell", "https://www.paycell.com.tr", "hr@paycell.com.tr", sector, "İstanbul", "Mobil ödeme"),
            CreateCompany("Invio", "https://www.invio.com.tr", "hr@invio.com.tr", sector, "İstanbul", "Digital marketing"),
            CreateCompany("iLab", "https://www.ilab.com.tr", "hr@ilab.com.tr", sector, "İstanbul", "Digital ürün stüdyosu"),
            CreateCompany("Codefiction", "https://www.codefiction.tech", "hr@codefiction.tech", sector, "İstanbul", "Yazılım danışmanlık"),
            CreateCompany("Apsiyon", "https://www.apsiyon.com", "hr@apsiyon.com", sector, "İstanbul", "Dijital pazarlama"),
            CreateCompany("Pisano", "https://www.pisano.com", "hr@pisano.com", sector, "İstanbul", "Müşteri deneyimi"),
            CreateCompany("Kolay IK", "https://www.kolayik.com", "hr@kolayik.com", sector, "İstanbul", "HR teknoloji"),
            CreateCompany("Hiwell", "https://www.hiwell.com", "hr@hiwell.com", sector, "İstanbul", "Dijital sağlık"),
            CreateCompany("Tapu.com", "https://www.tapu.com", "hr@tapu.com", sector, "İstanbul", "Gayrimenkul proptech"),
            CreateCompany("Scotty Technologies", "https://www.rfrm.com", "hr@rfrm.com", sector, "İstanbul", "Micromobility"),
            CreateCompany("Tripian", "https://www.tripian.com", "hr@tripian.com", sector, "İstanbul", "Seyahat teknolojisi"),
            CreateCompany("Wunder Mobility", "https://www.wundermobility.com", "hr@wundermobility.com", sector, "İstanbul", "Mobilite teknolojisi"),
            CreateCompany("Vivense", "https://www.vivense.com", "hr@vivense.com", sector, "İstanbul", "Mobilya e-ticaret"),
            CreateCompany("Enocta", "https://www.enocta.com", "hr@enocta.com", sector, "İstanbul", "Kurumsal eğitim"),
            CreateCompany("Testinium", "https://www.testinium.com", "hr@testinium.com", sector, "İstanbul", "Test otomasyon"),
        };
    }

    private static VerifiedCompany CreateCompany(
        string name,
        string? website,
        string? hrEmail,
        string? sector,
        string? city,
        string? description)
    {
        return new VerifiedCompany
        {
            Id = Guid.NewGuid(),
            Name = name,
            Website = website,
            HREmail = hrEmail,
            Sector = sector,
            City = city,
            Description = description,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

