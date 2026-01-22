import { useState, useEffect } from 'react';
import { 
    adminApi, 
    VerifiedCompany, 
    CompanyStats, 
    CompanyCultureAnalysis,
    CompanyNews,
    CreateCompanyRequest 
} from '../services/api';

// Icons
const SearchIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
);

const BuildingIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
    </svg>
);

const CheckIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
    </svg>
);

const XIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
);

const PlusIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
    </svg>
);

const SparklesIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
    </svg>
);

const NewspaperIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
    </svg>
);

export default function AdminCompanies() {
    const [companies, setCompanies] = useState<VerifiedCompany[]>([]);
    const [stats, setStats] = useState<CompanyStats | null>(null);
    const [sectors, setSectors] = useState<string[]>([]);
    const [cities, setCities] = useState<string[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedSector, setSelectedSector] = useState('');
    const [selectedCity, setSelectedCity] = useState('');
    const [verifiedFilter, setVerifiedFilter] = useState<boolean | undefined>(undefined);
    const [page, setPage] = useState(0);
    const [total, setTotal] = useState(0);
    const pageSize = 20;

    // Modal states
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [showDetailModal, setShowDetailModal] = useState(false);
    const [selectedCompany, setSelectedCompany] = useState<VerifiedCompany | null>(null);
    const [cultureAnalysis, setCultureAnalysis] = useState<CompanyCultureAnalysis | null>(null);
    const [companyNews, setCompanyNews] = useState<CompanyNews[]>([]);
    const [actionLoading, setActionLoading] = useState(false);

    // Form state
    const [formData, setFormData] = useState<CreateCompanyRequest>({
        name: '',
        website: '',
        taxNumber: '',
        hrEmail: '',
        hrPhone: '',
        sector: '',
        city: '',
        description: ''
    });

    useEffect(() => {
        loadInitialData();
    }, []);

    useEffect(() => {
        loadCompanies();
    }, [searchTerm, selectedSector, selectedCity, verifiedFilter, page]);

    const loadInitialData = async () => {
        try {
            const [sectorsData, citiesData, statsData] = await Promise.all([
                adminApi.getSectors(),
                adminApi.getCities(),
                adminApi.getStats()
            ]);
            setSectors(sectorsData);
            setCities(citiesData);
            setStats(statsData);
        } catch (error) {
            console.error('Error loading initial data:', error);
        }
    };

    const loadCompanies = async () => {
        setLoading(true);
        try {
            const response = await adminApi.getCompanies({
                searchTerm: searchTerm || undefined,
                sector: selectedSector || undefined,
                city: selectedCity || undefined,
                isVerified: verifiedFilter,
                skip: page * pageSize,
                take: pageSize
            });
            setCompanies(response.companies);
            setTotal(response.total);
        } catch (error) {
            console.error('Error loading companies:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSeedCompanies = async () => {
        if (!confirm('Bu işlem veritabanına 1247+ şirket ekleyecektir. Devam etmek istiyor musunuz?')) {
            return;
        }
        
        setActionLoading(true);
        try {
            const result = await adminApi.seedCompanies();
            alert(`Şirket ekleme tamamlandı! Toplam: ${result.totalCompanies} şirket`);
            loadCompanies();
            loadInitialData();
        } catch (error) {
            alert('Şirket ekleme sırasında hata oluştu!');
        } finally {
            setActionLoading(false);
        }
    };

    const handleCreateCompany = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!formData.name.trim()) {
            alert('Şirket adı zorunludur!');
            return;
        }

        setActionLoading(true);
        try {
            await adminApi.createCompany(formData);
            setShowCreateModal(false);
            setFormData({
                name: '',
                website: '',
                taxNumber: '',
                hrEmail: '',
                hrPhone: '',
                sector: '',
                city: '',
                description: ''
            });
            loadCompanies();
            loadInitialData();
        } catch (error) {
            alert('Şirket oluşturulurken hata oluştu!');
        } finally {
            setActionLoading(false);
        }
    };

    const handleVerifyCompany = async (company: VerifiedCompany) => {
        if (company.isVerified) {
            alert('Bu şirket zaten doğrulanmış!');
            return;
        }

        setActionLoading(true);
        try {
            await adminApi.verifyCompany(company.id, {
                taxNumber: company.taxNumber,
                hrEmail: company.hrEmail,
                website: company.website
            });
            loadCompanies();
            loadInitialData();
        } catch (error: any) {
            alert(`Doğrulama hatası: ${error.message}`);
        } finally {
            setActionLoading(false);
        }
    };

    const handleAnalyzeCulture = async (company: VerifiedCompany) => {
        setSelectedCompany(company);
        setCultureAnalysis(null);
        setCompanyNews([]);
        setShowDetailModal(true);
        setActionLoading(true);

        try {
            const result = await adminApi.analyzeCulture(company.id);
            setCultureAnalysis(result.analysis);
        } catch (error) {
            console.error('Error analyzing culture:', error);
        } finally {
            setActionLoading(false);
        }
    };

    const handleScrapeNews = async () => {
        if (!selectedCompany) return;

        setActionLoading(true);
        try {
            const result = await adminApi.scrapeNews(selectedCompany.id);
            setCompanyNews(result.news);
        } catch (error) {
            console.error('Error scraping news:', error);
        } finally {
            setActionLoading(false);
        }
    };

    const handleDeleteCompany = async (company: VerifiedCompany) => {
        if (!confirm(`"${company.name}" şirketini silmek istediğinizden emin misiniz?`)) {
            return;
        }

        setActionLoading(true);
        try {
            await adminApi.deleteCompany(company.id);
            loadCompanies();
            loadInitialData();
        } catch (error) {
            alert('Şirket silinirken hata oluştu!');
        } finally {
            setActionLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
            {/* Header */}
            <div className="bg-slate-800/50 border-b border-slate-700">
                <div className="max-w-7xl mx-auto px-4 py-6">
                    <div className="flex items-center justify-between">
                        <div>
                            <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                                <BuildingIcon />
                                Şirket Yönetimi
                            </h1>
                            <p className="text-slate-400 mt-1">Doğrulanmış şirketleri yönetin ve analiz edin</p>
                        </div>
                        <div className="flex gap-3">
                            <button
                                onClick={handleSeedCompanies}
                                disabled={actionLoading}
                                className="px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50"
                            >
                                1247+ Şirket Yükle
                            </button>
                            <button
                                onClick={() => setShowCreateModal(true)}
                                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-medium flex items-center gap-2 transition-colors"
                            >
                                <PlusIcon />
                                Yeni Şirket
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-4 py-6">
                {/* Stats */}
                {stats && (
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Toplam Şirket</p>
                            <p className="text-2xl font-bold text-white">{stats.totalCompanies.toLocaleString()}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Doğrulanmış</p>
                            <p className="text-2xl font-bold text-emerald-400">{stats.verifiedCompanies.toLocaleString()}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Bekleyen</p>
                            <p className="text-2xl font-bold text-amber-400">{stats.unverifiedCompanies.toLocaleString()}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Bağlı İlanlar</p>
                            <p className="text-2xl font-bold text-blue-400">{stats.totalJobPostingsLinked.toLocaleString()}</p>
                        </div>
                    </div>
                )}

                {/* Filters */}
                <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4 mb-6">
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div className="relative">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                                <SearchIcon />
                            </div>
                            <input
                                type="text"
                                placeholder="Şirket ara..."
                                value={searchTerm}
                                onChange={(e) => { setSearchTerm(e.target.value); setPage(0); }}
                                className="w-full pl-10 pr-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                            />
                        </div>
                        <select
                            value={selectedSector}
                            onChange={(e) => { setSelectedSector(e.target.value); setPage(0); }}
                            className="px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                        >
                            <option value="">Tüm Sektörler</option>
                            {sectors.map(sector => (
                                <option key={sector} value={sector}>{sector}</option>
                            ))}
                        </select>
                        <select
                            value={selectedCity}
                            onChange={(e) => { setSelectedCity(e.target.value); setPage(0); }}
                            className="px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                        >
                            <option value="">Tüm Şehirler</option>
                            {cities.map(city => (
                                <option key={city} value={city}>{city}</option>
                            ))}
                        </select>
                        <select
                            value={verifiedFilter === undefined ? '' : String(verifiedFilter)}
                            onChange={(e) => { 
                                setVerifiedFilter(e.target.value === '' ? undefined : e.target.value === 'true'); 
                                setPage(0); 
                            }}
                            className="px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                        >
                            <option value="">Tüm Durumlar</option>
                            <option value="true">Doğrulanmış</option>
                            <option value="false">Beklemede</option>
                        </select>
                    </div>
                </div>

                {/* Companies Table */}
                <div className="bg-slate-800/50 border border-slate-700 rounded-xl overflow-hidden">
                    {loading ? (
                        <div className="p-8 text-center text-slate-400">Yükleniyor...</div>
                    ) : companies.length === 0 ? (
                        <div className="p-8 text-center text-slate-400">Şirket bulunamadı</div>
                    ) : (
                        <>
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead className="bg-slate-700/50">
                                        <tr>
                                            <th className="text-left px-4 py-3 text-slate-300 font-medium">Şirket</th>
                                            <th className="text-left px-4 py-3 text-slate-300 font-medium">Sektör</th>
                                            <th className="text-left px-4 py-3 text-slate-300 font-medium">Şehir</th>
                                            <th className="text-left px-4 py-3 text-slate-300 font-medium">Durum</th>
                                            <th className="text-right px-4 py-3 text-slate-300 font-medium">İşlemler</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {companies.map((company) => (
                                            <tr key={company.id} className="border-t border-slate-700 hover:bg-slate-700/30">
                                                <td className="px-4 py-3">
                                                    <div>
                                                        <p className="text-white font-medium">{company.name}</p>
                                                        {company.website && (
                                                            <a 
                                                                href={company.website} 
                                                                target="_blank" 
                                                                rel="noopener noreferrer"
                                                                className="text-sm text-blue-400 hover:underline"
                                                            >
                                                                {company.website}
                                                            </a>
                                                        )}
                                                    </div>
                                                </td>
                                                <td className="px-4 py-3 text-slate-300">{company.sector || '-'}</td>
                                                <td className="px-4 py-3 text-slate-300">{company.city || '-'}</td>
                                                <td className="px-4 py-3">
                                                    {company.isVerified ? (
                                                        <span className="inline-flex items-center gap-1 px-2 py-1 bg-emerald-500/20 text-emerald-400 rounded-full text-sm">
                                                            <CheckIcon /> Doğrulanmış
                                                        </span>
                                                    ) : (
                                                        <span className="inline-flex items-center gap-1 px-2 py-1 bg-amber-500/20 text-amber-400 rounded-full text-sm">
                                                            <XIcon /> Beklemede
                                                        </span>
                                                    )}
                                                </td>
                                                <td className="px-4 py-3">
                                                    <div className="flex justify-end gap-2">
                                                        <button
                                                            onClick={() => handleAnalyzeCulture(company)}
                                                            className="p-2 text-purple-400 hover:bg-purple-500/20 rounded-lg transition-colors"
                                                            title="Kültür Analizi"
                                                        >
                                                            <SparklesIcon />
                                                        </button>
                                                        {!company.isVerified && (
                                                            <button
                                                                onClick={() => handleVerifyCompany(company)}
                                                                className="p-2 text-emerald-400 hover:bg-emerald-500/20 rounded-lg transition-colors"
                                                                title="Doğrula"
                                                            >
                                                                <CheckIcon />
                                                            </button>
                                                        )}
                                                        <button
                                                            onClick={() => handleDeleteCompany(company)}
                                                            className="p-2 text-red-400 hover:bg-red-500/20 rounded-lg transition-colors"
                                                            title="Sil"
                                                        >
                                                            <XIcon />
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>

                            {/* Pagination */}
                            <div className="flex items-center justify-between px-4 py-3 border-t border-slate-700">
                                <p className="text-slate-400 text-sm">
                                    Toplam {total.toLocaleString()} şirket
                                </p>
                                <div className="flex gap-2">
                                    <button
                                        onClick={() => setPage(p => Math.max(0, p - 1))}
                                        disabled={page === 0}
                                        className="px-4 py-2 bg-slate-700 text-white rounded-lg disabled:opacity-50"
                                    >
                                        Önceki
                                    </button>
                                    <button
                                        onClick={() => setPage(p => p + 1)}
                                        disabled={(page + 1) * pageSize >= total}
                                        className="px-4 py-2 bg-slate-700 text-white rounded-lg disabled:opacity-50"
                                    >
                                        Sonraki
                                    </button>
                                </div>
                            </div>
                        </>
                    )}
                </div>
            </div>

            {/* Create Company Modal */}
            {showCreateModal && (
                <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
                    <div className="bg-slate-800 border border-slate-700 rounded-xl p-6 w-full max-w-lg mx-4">
                        <h2 className="text-xl font-bold text-white mb-4">Yeni Şirket Ekle</h2>
                        <form onSubmit={handleCreateCompany} className="space-y-4">
                            <div>
                                <label className="block text-slate-300 text-sm mb-1">Şirket Adı *</label>
                                <input
                                    type="text"
                                    value={formData.name}
                                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white"
                                    required
                                />
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-slate-300 text-sm mb-1">Sektör</label>
                                    <input
                                        type="text"
                                        value={formData.sector}
                                        onChange={(e) => setFormData({ ...formData, sector: e.target.value })}
                                        className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white"
                                    />
                                </div>
                                <div>
                                    <label className="block text-slate-300 text-sm mb-1">Şehir</label>
                                    <input
                                        type="text"
                                        value={formData.city}
                                        onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                                        className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white"
                                    />
                                </div>
                            </div>
                            <div>
                                <label className="block text-slate-300 text-sm mb-1">Website</label>
                                <input
                                    type="url"
                                    value={formData.website}
                                    onChange={(e) => setFormData({ ...formData, website: e.target.value })}
                                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white"
                                />
                            </div>
                            <div>
                                <label className="block text-slate-300 text-sm mb-1">HR E-posta</label>
                                <input
                                    type="email"
                                    value={formData.hrEmail}
                                    onChange={(e) => setFormData({ ...formData, hrEmail: e.target.value })}
                                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white"
                                />
                            </div>
                            <div>
                                <label className="block text-slate-300 text-sm mb-1">Açıklama</label>
                                <textarea
                                    value={formData.description}
                                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white h-20"
                                />
                            </div>
                            <div className="flex justify-end gap-3 pt-4">
                                <button
                                    type="button"
                                    onClick={() => setShowCreateModal(false)}
                                    className="px-4 py-2 bg-slate-600 text-white rounded-lg"
                                >
                                    İptal
                                </button>
                                <button
                                    type="submit"
                                    disabled={actionLoading}
                                    className="px-4 py-2 bg-emerald-600 text-white rounded-lg disabled:opacity-50"
                                >
                                    {actionLoading ? 'Ekleniyor...' : 'Ekle'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Company Detail Modal */}
            {showDetailModal && selectedCompany && (
                <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 overflow-y-auto">
                    <div className="bg-slate-800 border border-slate-700 rounded-xl p-6 w-full max-w-2xl mx-4 my-8">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-xl font-bold text-white">{selectedCompany.name}</h2>
                            <button
                                onClick={() => setShowDetailModal(false)}
                                className="p-2 text-slate-400 hover:text-white"
                            >
                                <XIcon />
                            </button>
                        </div>

                        <div className="space-y-6">
                            {/* Company Info */}
                            <div className="grid grid-cols-2 gap-4 text-sm">
                                <div>
                                    <p className="text-slate-400">Sektör</p>
                                    <p className="text-white">{selectedCompany.sector || '-'}</p>
                                </div>
                                <div>
                                    <p className="text-slate-400">Şehir</p>
                                    <p className="text-white">{selectedCompany.city || '-'}</p>
                                </div>
                                <div>
                                    <p className="text-slate-400">Website</p>
                                    <p className="text-blue-400">{selectedCompany.website || '-'}</p>
                                </div>
                                <div>
                                    <p className="text-slate-400">HR E-posta</p>
                                    <p className="text-white">{selectedCompany.hrEmail || '-'}</p>
                                </div>
                            </div>

                            {/* Culture Analysis */}
                            <div>
                                <h3 className="text-lg font-semibold text-white mb-3 flex items-center gap-2">
                                    <SparklesIcon />
                                    Kültür Analizi
                                </h3>
                                {actionLoading && !cultureAnalysis ? (
                                    <p className="text-slate-400">Analiz ediliyor...</p>
                                ) : cultureAnalysis ? (
                                    <div className="bg-slate-700/50 rounded-lg p-4 space-y-3">
                                        <div>
                                            <p className="text-slate-400 text-sm">Şirket Kültürü</p>
                                            <p className="text-white">{cultureAnalysis.culture}</p>
                                        </div>
                                        <div>
                                            <p className="text-slate-400 text-sm">Değerler</p>
                                            <p className="text-white">{cultureAnalysis.values}</p>
                                        </div>
                                        <div>
                                            <p className="text-slate-400 text-sm">Çalışma Ortamı</p>
                                            <p className="text-white">{cultureAnalysis.workEnvironment}</p>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <p className="text-slate-400 text-sm">Genel Skor:</p>
                                            <span className="px-2 py-1 bg-emerald-500/20 text-emerald-400 rounded-full text-sm font-medium">
                                                {cultureAnalysis.overallScore}/100
                                            </span>
                                        </div>
                                    </div>
                                ) : null}
                            </div>

                            {/* News Section */}
                            <div>
                                <div className="flex items-center justify-between mb-3">
                                    <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                                        <NewspaperIcon />
                                        Son Haberler
                                    </h3>
                                    <button
                                        onClick={handleScrapeNews}
                                        disabled={actionLoading}
                                        className="px-3 py-1 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm disabled:opacity-50"
                                    >
                                        {actionLoading ? 'Yükleniyor...' : 'Haberleri Getir'}
                                    </button>
                                </div>
                                {companyNews.length > 0 ? (
                                    <div className="space-y-3">
                                        {companyNews.map((news, index) => (
                                            <div key={index} className="bg-slate-700/50 rounded-lg p-3">
                                                <p className="text-white font-medium">{news.title}</p>
                                                <p className="text-slate-400 text-sm mt-1">{news.summary}</p>
                                                <div className="flex items-center gap-2 mt-2 text-xs text-slate-500">
                                                    <span>{news.source}</span>
                                                    <span>•</span>
                                                    <span>{new Date(news.publishedAt).toLocaleDateString('tr-TR')}</span>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                ) : (
                                    <p className="text-slate-400 text-sm">Haber yüklemek için butona tıklayın</p>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

