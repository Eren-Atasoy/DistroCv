import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { filterApi } from '../services/api';
import type { SectorDto, CityDto } from '../services/api';

// Icons
const BriefcaseIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
    </svg>
);

const MapPinIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
);

const CurrencyIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
);

const HomeIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
    </svg>
);

const CheckIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
    </svg>
);

const SaveIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
    </svg>
);

const LoadingSpinner = () => (
    <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
    </svg>
);

export default function PreferencesPage() {
    const { t, i18n } = useTranslation();
    const isEn = i18n.language === 'en';
    
    const [sectors, setSectors] = useState<SectorDto[]>([]);
    const [cities, setCities] = useState<CityDto[]>([]);
    const [majorCities, setMajorCities] = useState<CityDto[]>([]);
    
    const [selectedSectors, setSelectedSectors] = useState<number[]>([]);
    const [selectedCities, setSelectedCities] = useState<number[]>([]);
    const [minSalary, setMinSalary] = useState<string>('');
    const [maxSalary, setMaxSalary] = useState<string>('');
    const [isRemotePreferred, setIsRemotePreferred] = useState(false);
    
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [showAllCities, setShowAllCities] = useState(false);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    
    const [sectorSearch, setSectorSearch] = useState('');
    const [citySearch, setCitySearch] = useState('');

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setIsLoading(true);
        try {
            const [sectorsRes, citiesRes, prefsRes] = await Promise.all([
                filterApi.getSectors(),
                filterApi.getCities(),
                filterApi.getPreferences()
            ]);

            setSectors(sectorsRes.sectors);
            setCities(citiesRes.cities);
            setMajorCities(citiesRes.majorCities);
            
            // Load existing preferences
            setSelectedSectors(prefsRes.preferredSectors.map(s => s.id));
            setSelectedCities(prefsRes.preferredCities.map(c => c.id));
            setMinSalary(prefsRes.minSalary?.toString() || '');
            setMaxSalary(prefsRes.maxSalary?.toString() || '');
            setIsRemotePreferred(prefsRes.isRemotePreferred);
        } catch (error) {
            console.error('Error loading data:', error);
            setErrorMessage(isEn ? 'Failed to load preferences' : 'Tercihler yüklenemedi');
        } finally {
            setIsLoading(false);
        }
    };

    const handleSectorToggle = (sectorId: number) => {
        setSelectedSectors(prev => 
            prev.includes(sectorId) 
                ? prev.filter(id => id !== sectorId)
                : [...prev, sectorId]
        );
    };

    const handleCityToggle = (cityId: number) => {
        setSelectedCities(prev => 
            prev.includes(cityId) 
                ? prev.filter(id => id !== cityId)
                : [...prev, cityId]
        );
    };

    const handleSave = async () => {
        setIsSaving(true);
        setSuccessMessage(null);
        setErrorMessage(null);
        
        try {
            await filterApi.updatePreferences({
                preferredSectors: selectedSectors,
                preferredCities: selectedCities,
                minSalary: minSalary ? parseFloat(minSalary) : undefined,
                maxSalary: maxSalary ? parseFloat(maxSalary) : undefined,
                isRemotePreferred
            });
            
            setSuccessMessage(isEn ? 'Preferences saved successfully!' : 'Tercihler başarıyla kaydedildi!');
            setTimeout(() => setSuccessMessage(null), 3000);
        } catch (error) {
            console.error('Error saving preferences:', error);
            setErrorMessage(isEn ? 'Failed to save preferences' : 'Tercihler kaydedilemedi');
        } finally {
            setIsSaving(false);
        }
    };

    const filteredSectors = sectors.filter(s => {
        const searchLower = sectorSearch.toLowerCase();
        return s.nameTr.toLowerCase().includes(searchLower) || 
               s.nameEn.toLowerCase().includes(searchLower);
    });

    const displayedCities = showAllCities ? cities : majorCities;
    const filteredCities = displayedCities.filter(c => 
        c.name.toLowerCase().includes(citySearch.toLowerCase())
    );

    if (isLoading) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <LoadingSpinner />
                <span className="ml-2 text-surface-300">{t('common.loading')}</span>
            </div>
        );
    }

    return (
        <div className="max-w-6xl mx-auto space-y-8">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-display font-bold text-white">
                        {isEn ? 'Job Preferences' : 'İş Tercihleri'}
                    </h1>
                    <p className="text-surface-400 mt-2">
                        {isEn 
                            ? 'Configure your sector and location preferences to find the best matching jobs'
                            : 'En uygun işleri bulmak için sektör ve konum tercihlerinizi yapılandırın'}
                    </p>
                </div>
                
                <button
                    onClick={handleSave}
                    disabled={isSaving}
                    className="flex items-center gap-2 px-6 py-3 bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-xl font-medium transition-colors"
                >
                    {isSaving ? <LoadingSpinner /> : <SaveIcon />}
                    {isEn ? 'Save Preferences' : 'Tercihleri Kaydet'}
                </button>
            </div>

            {/* Success/Error Messages */}
            {successMessage && (
                <div className="bg-emerald-500/20 border border-emerald-500/50 text-emerald-300 px-4 py-3 rounded-xl flex items-center gap-2">
                    <CheckIcon />
                    {successMessage}
                </div>
            )}
            {errorMessage && (
                <div className="bg-red-500/20 border border-red-500/50 text-red-300 px-4 py-3 rounded-xl">
                    {errorMessage}
                </div>
            )}

            {/* Sectors Section */}
            <div className="bg-surface-800/50 rounded-2xl border border-surface-700 p-6">
                <div className="flex items-center gap-3 mb-6">
                    <div className="w-10 h-10 rounded-xl bg-primary-500/20 flex items-center justify-center">
                        <BriefcaseIcon />
                    </div>
                    <div>
                        <h2 className="text-xl font-semibold text-white">
                            {isEn ? 'Preferred Sectors' : 'Tercih Edilen Sektörler'}
                        </h2>
                        <p className="text-surface-400 text-sm">
                            {isEn 
                                ? `${selectedSectors.length} sector(s) selected` 
                                : `${selectedSectors.length} sektör seçildi`}
                        </p>
                    </div>
                </div>

                {/* Sector Search */}
                <input
                    type="text"
                    placeholder={isEn ? 'Search sectors...' : 'Sektör ara...'}
                    value={sectorSearch}
                    onChange={(e) => setSectorSearch(e.target.value)}
                    className="w-full px-4 py-3 bg-surface-700 border border-surface-600 rounded-xl text-white placeholder-surface-400 mb-4 focus:outline-none focus:ring-2 focus:ring-primary-500"
                />

                {/* Sector Grid */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 max-h-80 overflow-y-auto pr-2">
                    {filteredSectors.map(sector => (
                        <button
                            key={sector.id}
                            onClick={() => handleSectorToggle(sector.id)}
                            className={`flex items-center justify-between px-4 py-3 rounded-xl text-left transition-all ${
                                selectedSectors.includes(sector.id)
                                    ? 'bg-primary-500/20 border-2 border-primary-500 text-white'
                                    : 'bg-surface-700/50 border border-surface-600 text-surface-300 hover:border-primary-500/50'
                            }`}
                        >
                            <span className="text-sm">
                                {isEn ? sector.nameEn : sector.nameTr}
                            </span>
                            {selectedSectors.includes(sector.id) && (
                                <span className="text-primary-400">
                                    <CheckIcon />
                                </span>
                            )}
                        </button>
                    ))}
                </div>
            </div>

            {/* Cities Section */}
            <div className="bg-surface-800/50 rounded-2xl border border-surface-700 p-6">
                <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl bg-cyan-500/20 flex items-center justify-center text-cyan-400">
                            <MapPinIcon />
                        </div>
                        <div>
                            <h2 className="text-xl font-semibold text-white">
                                {isEn ? 'Preferred Cities' : 'Tercih Edilen Şehirler'}
                            </h2>
                            <p className="text-surface-400 text-sm">
                                {isEn 
                                    ? `${selectedCities.length} city(ies) selected` 
                                    : `${selectedCities.length} şehir seçildi`}
                            </p>
                        </div>
                    </div>
                    
                    <button
                        onClick={() => setShowAllCities(!showAllCities)}
                        className="text-sm text-primary-400 hover:text-primary-300 transition-colors"
                    >
                        {showAllCities 
                            ? (isEn ? 'Show Major Cities Only' : 'Sadece Büyük Şehirler')
                            : (isEn ? 'Show All 81 Cities' : 'Tüm 81 Şehri Göster')}
                    </button>
                </div>

                {/* City Search */}
                <input
                    type="text"
                    placeholder={isEn ? 'Search cities...' : 'Şehir ara...'}
                    value={citySearch}
                    onChange={(e) => setCitySearch(e.target.value)}
                    className="w-full px-4 py-3 bg-surface-700 border border-surface-600 rounded-xl text-white placeholder-surface-400 mb-4 focus:outline-none focus:ring-2 focus:ring-primary-500"
                />

                {/* City Grid */}
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 max-h-80 overflow-y-auto pr-2">
                    {filteredCities.map(city => (
                        <button
                            key={city.id}
                            onClick={() => handleCityToggle(city.id)}
                            className={`flex items-center justify-between px-4 py-3 rounded-xl text-left transition-all ${
                                selectedCities.includes(city.id)
                                    ? 'bg-cyan-500/20 border-2 border-cyan-500 text-white'
                                    : 'bg-surface-700/50 border border-surface-600 text-surface-300 hover:border-cyan-500/50'
                            }`}
                        >
                            <span className="text-sm">{city.name}</span>
                            {selectedCities.includes(city.id) && (
                                <span className="text-cyan-400">
                                    <CheckIcon />
                                </span>
                            )}
                        </button>
                    ))}
                </div>
            </div>

            {/* Salary & Remote Section */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Salary Range */}
                <div className="bg-surface-800/50 rounded-2xl border border-surface-700 p-6">
                    <div className="flex items-center gap-3 mb-6">
                        <div className="w-10 h-10 rounded-xl bg-emerald-500/20 flex items-center justify-center text-emerald-400">
                            <CurrencyIcon />
                        </div>
                        <div>
                            <h2 className="text-xl font-semibold text-white">
                                {isEn ? 'Salary Range (TL)' : 'Maaş Aralığı (TL)'}
                            </h2>
                            <p className="text-surface-400 text-sm">
                                {isEn ? 'Monthly gross salary' : 'Aylık brüt maaş'}
                            </p>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm text-surface-400 mb-2">
                                {isEn ? 'Minimum' : 'Minimum'}
                            </label>
                            <input
                                type="number"
                                value={minSalary}
                                onChange={(e) => setMinSalary(e.target.value)}
                                placeholder="30000"
                                className="w-full px-4 py-3 bg-surface-700 border border-surface-600 rounded-xl text-white placeholder-surface-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
                            />
                        </div>
                        <div>
                            <label className="block text-sm text-surface-400 mb-2">
                                {isEn ? 'Maximum' : 'Maksimum'}
                            </label>
                            <input
                                type="number"
                                value={maxSalary}
                                onChange={(e) => setMaxSalary(e.target.value)}
                                placeholder="100000"
                                className="w-full px-4 py-3 bg-surface-700 border border-surface-600 rounded-xl text-white placeholder-surface-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
                            />
                        </div>
                    </div>
                </div>

                {/* Remote Work */}
                <div className="bg-surface-800/50 rounded-2xl border border-surface-700 p-6">
                    <div className="flex items-center gap-3 mb-6">
                        <div className="w-10 h-10 rounded-xl bg-violet-500/20 flex items-center justify-center text-violet-400">
                            <HomeIcon />
                        </div>
                        <div>
                            <h2 className="text-xl font-semibold text-white">
                                {isEn ? 'Remote Work' : 'Uzaktan Çalışma'}
                            </h2>
                            <p className="text-surface-400 text-sm">
                                {isEn ? 'Work from home preference' : 'Evden çalışma tercihi'}
                            </p>
                        </div>
                    </div>

                    <button
                        onClick={() => setIsRemotePreferred(!isRemotePreferred)}
                        className={`w-full flex items-center justify-between px-6 py-4 rounded-xl transition-all ${
                            isRemotePreferred
                                ? 'bg-violet-500/20 border-2 border-violet-500 text-white'
                                : 'bg-surface-700/50 border border-surface-600 text-surface-300 hover:border-violet-500/50'
                        }`}
                    >
                        <span>
                            {isEn 
                                ? 'I prefer remote work opportunities'
                                : 'Uzaktan çalışma fırsatlarını tercih ediyorum'}
                        </span>
                        <div className={`w-12 h-6 rounded-full transition-colors ${
                            isRemotePreferred ? 'bg-violet-500' : 'bg-surface-600'
                        }`}>
                            <div className={`w-5 h-5 rounded-full bg-white shadow-md transform transition-transform ${
                                isRemotePreferred ? 'translate-x-6' : 'translate-x-0.5'
                            } mt-0.5`} />
                        </div>
                    </button>
                </div>
            </div>

            {/* Summary */}
            <div className="bg-gradient-to-r from-primary-500/10 to-cyan-500/10 rounded-2xl border border-primary-500/30 p-6">
                <h3 className="text-lg font-semibold text-white mb-4">
                    {isEn ? 'Your Preferences Summary' : 'Tercih Özetiniz'}
                </h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
                    <div>
                        <div className="text-2xl font-bold text-primary-400">{selectedSectors.length}</div>
                        <div className="text-sm text-surface-400">{isEn ? 'Sectors' : 'Sektör'}</div>
                    </div>
                    <div>
                        <div className="text-2xl font-bold text-cyan-400">{selectedCities.length}</div>
                        <div className="text-sm text-surface-400">{isEn ? 'Cities' : 'Şehir'}</div>
                    </div>
                    <div>
                        <div className="text-2xl font-bold text-emerald-400">
                            {minSalary && maxSalary 
                                ? `${(parseInt(minSalary)/1000).toFixed(0)}K-${(parseInt(maxSalary)/1000).toFixed(0)}K`
                                : '-'}
                        </div>
                        <div className="text-sm text-surface-400">{isEn ? 'Salary' : 'Maaş'}</div>
                    </div>
                    <div>
                        <div className="text-2xl font-bold text-violet-400">
                            {isRemotePreferred ? '✓' : '-'}
                        </div>
                        <div className="text-sm text-surface-400">{isEn ? 'Remote' : 'Uzaktan'}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}

