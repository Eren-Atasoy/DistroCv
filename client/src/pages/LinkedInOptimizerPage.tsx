import { useState, useEffect } from 'react';
import { linkedInProfileApi } from '../services/api';
import type { LinkedInOptimizationResult, ProfileComparison, ProfileOptimizationHistory } from '../services/api';

// Icons
const LinkedInIcon = () => (
    <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
        <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
    </svg>
);

const CheckIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
    </svg>
);

const ArrowRightIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
    </svg>
);

const getScoreColor = (score: number) => {
    if (score >= 80) return 'text-emerald-400';
    if (score >= 60) return 'text-amber-400';
    return 'text-red-400';
};

const getScoreBgColor = (score: number) => {
    if (score >= 80) return 'bg-emerald-500';
    if (score >= 60) return 'bg-amber-500';
    return 'bg-red-500';
};

export default function LinkedInOptimizerPage() {
    const [linkedInUrl, setLinkedInUrl] = useState('');
    const [targetJobTitles, setTargetJobTitles] = useState('');
    const [analyzing, setAnalyzing] = useState(false);
    const [result, setResult] = useState<LinkedInOptimizationResult | null>(null);
    const [comparison, setComparison] = useState<ProfileComparison[]>([]);
    const [history, setHistory] = useState<ProfileOptimizationHistory[]>([]);
    const [activeTab, setActiveTab] = useState<'analysis' | 'comparison' | 'history'>('analysis');
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadHistory();
    }, []);

    const loadHistory = async () => {
        try {
            const response = await linkedInProfileApi.getHistory();
            setHistory(response.optimizations);
        } catch (err) {
            console.error('Error loading history:', err);
        }
    };

    const handleAnalyze = async () => {
        if (!linkedInUrl) {
            setError('LinkedIn URL gerekli');
            return;
        }

        setAnalyzing(true);
        setError(null);
        setResult(null);

        try {
            const targets = targetJobTitles.split(',').map(t => t.trim()).filter(t => t);
            const response = await linkedInProfileApi.analyzeProfile({
                linkedInUrl,
                targetJobTitles: targets.length > 0 ? targets : undefined
            });
            setResult(response.result);

            // Load comparison
            const compResponse = await linkedInProfileApi.getComparisonView(response.result.id);
            setComparison(compResponse.sections);

            // Refresh history
            await loadHistory();
        } catch (err: any) {
            console.error('Analysis error:', err);
            setError(err.message || 'Analiz sırasında hata oluştu');
        } finally {
            setAnalyzing(false);
        }
    };

    const handleLoadOptimization = async (id: string) => {
        try {
            const opt = await linkedInProfileApi.getOptimization(id);
            setResult(opt);
            const comp = await linkedInProfileApi.getComparisonView(id);
            setComparison(comp.sections);
            setActiveTab('analysis');
        } catch (err) {
            console.error('Error loading optimization:', err);
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
            {/* Header */}
            <div className="bg-slate-800/50 border-b border-slate-700">
                <div className="max-w-7xl mx-auto px-4 py-6">
                    <div className="flex items-center gap-3">
                        <div className="p-2 bg-blue-600 rounded-lg">
                            <LinkedInIcon />
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold text-white">LinkedIn Profil Optimizasyonu</h1>
                            <p className="text-slate-400 mt-1">Profilinizi SEO ve ATS uyumlu hale getirin</p>
                        </div>
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-4 py-6">
                {/* Input Section */}
                <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 mb-6">
                    <h2 className="text-lg font-semibold text-white mb-4">Profil Analizi</h2>
                    <div className="space-y-4">
                        <div>
                            <label className="block text-sm text-slate-400 mb-2">LinkedIn Profil URL'si</label>
                            <input
                                type="url"
                                value={linkedInUrl}
                                onChange={(e) => setLinkedInUrl(e.target.value)}
                                placeholder="https://linkedin.com/in/your-profile"
                                className="w-full px-4 py-3 bg-slate-700 border border-slate-600 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                            />
                        </div>
                        <div>
                            <label className="block text-sm text-slate-400 mb-2">Hedef Pozisyonlar (virgülle ayırın)</label>
                            <input
                                type="text"
                                value={targetJobTitles}
                                onChange={(e) => setTargetJobTitles(e.target.value)}
                                placeholder="Senior Developer, Tech Lead, Software Architect"
                                className="w-full px-4 py-3 bg-slate-700 border border-slate-600 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                            />
                        </div>
                        {error && (
                            <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-3 text-red-400 text-sm">
                                {error}
                            </div>
                        )}
                        <button
                            onClick={handleAnalyze}
                            disabled={analyzing}
                            className="w-full py-3 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white font-medium rounded-lg flex items-center justify-center gap-2 transition-all disabled:opacity-50"
                        >
                            {analyzing ? (
                                <>
                                    <div className="animate-spin w-5 h-5 border-2 border-white border-t-transparent rounded-full"></div>
                                    Analiz Ediliyor...
                                </>
                            ) : (
                                <>
                                    <LinkedInIcon />
                                    Profili Analiz Et
                                </>
                            )}
                        </button>
                    </div>
                </div>

                {/* Tabs */}
                {result && (
                    <div className="border-b border-slate-700 mb-6">
                        <div className="flex gap-1">
                            <button
                                onClick={() => setActiveTab('analysis')}
                                className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                    activeTab === 'analysis' 
                                        ? 'border-blue-500 text-blue-400' 
                                        : 'border-transparent text-slate-400 hover:text-white'
                                }`}
                            >
                                📊 Analiz Sonuçları
                            </button>
                            <button
                                onClick={() => setActiveTab('comparison')}
                                className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                    activeTab === 'comparison' 
                                        ? 'border-purple-500 text-purple-400' 
                                        : 'border-transparent text-slate-400 hover:text-white'
                                }`}
                            >
                                🔄 Karşılaştırma
                            </button>
                            <button
                                onClick={() => setActiveTab('history')}
                                className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                    activeTab === 'history' 
                                        ? 'border-amber-500 text-amber-400' 
                                        : 'border-transparent text-slate-400 hover:text-white'
                                }`}
                            >
                                📜 Geçmiş ({history.length})
                            </button>
                        </div>
                    </div>
                )}

                {/* Analysis Results */}
                {result && activeTab === 'analysis' && (
                    <div className="space-y-6">
                        {/* Score Overview */}
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6">
                            <div className="flex items-center justify-between mb-6">
                                <h3 className="text-lg font-semibold text-white">Profil Skoru</h3>
                                <div className={`text-4xl font-bold ${getScoreColor(result.profileScore)}`}>
                                    {result.profileScore}/100
                                </div>
                            </div>
                            
                            {/* Score Breakdown */}
                            <div className="grid grid-cols-5 gap-4">
                                {[
                                    { label: 'Başlık', score: result.scoreBreakdown.headlineScore, max: 20 },
                                    { label: 'Hakkında', score: result.scoreBreakdown.aboutScore, max: 25 },
                                    { label: 'Deneyim', score: result.scoreBreakdown.experienceScore, max: 30 },
                                    { label: 'Beceriler', score: result.scoreBreakdown.skillsScore, max: 15 },
                                    { label: 'Eğitim', score: result.scoreBreakdown.educationScore, max: 10 },
                                ].map((item) => (
                                    <div key={item.label} className="text-center">
                                        <div className="text-slate-400 text-sm mb-2">{item.label}</div>
                                        <div className="relative h-24 bg-slate-700 rounded-lg overflow-hidden">
                                            <div 
                                                className={`absolute bottom-0 left-0 right-0 ${getScoreBgColor((item.score / item.max) * 100)} transition-all`}
                                                style={{ height: `${(item.score / item.max) * 100}%` }}
                                            />
                                            <div className="absolute inset-0 flex items-center justify-center text-white font-bold">
                                                {item.score}/{item.max}
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>

                        {/* SEO Analysis */}
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6">
                            <h3 className="text-lg font-semibold text-white mb-4">🔍 SEO Analizi</h3>
                            <div className="grid grid-cols-3 gap-4 mb-4">
                                <div className="bg-slate-700/50 rounded-lg p-4 text-center">
                                    <div className="text-2xl font-bold text-blue-400">{Math.round(result.seoAnalysis.searchability)}%</div>
                                    <div className="text-sm text-slate-400">Bulunabilirlik</div>
                                </div>
                                <div className="bg-slate-700/50 rounded-lg p-4 text-center">
                                    <div className="text-2xl font-bold text-purple-400">{Math.round(result.seoAnalysis.keywordDensity)}%</div>
                                    <div className="text-sm text-slate-400">Anahtar Kelime Yoğunluğu</div>
                                </div>
                                <div className="bg-slate-700/50 rounded-lg p-4 text-center">
                                    <div className="text-2xl font-bold text-emerald-400">{Math.round(result.seoAnalysis.profileCompleteness)}%</div>
                                    <div className="text-sm text-slate-400">Profil Tamlığı</div>
                                </div>
                            </div>
                            
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <h4 className="text-sm text-slate-400 mb-2">Eksik Anahtar Kelimeler</h4>
                                    <div className="flex flex-wrap gap-2">
                                        {result.seoAnalysis.missingKeywords.map((kw, i) => (
                                            <span key={i} className="px-2 py-1 bg-red-500/20 text-red-400 rounded text-xs">{kw}</span>
                                        ))}
                                    </div>
                                </div>
                                <div>
                                    <h4 className="text-sm text-slate-400 mb-2">Güçlü Anahtar Kelimeler</h4>
                                    <div className="flex flex-wrap gap-2">
                                        {result.seoAnalysis.strongKeywords.map((kw, i) => (
                                            <span key={i} className="px-2 py-1 bg-emerald-500/20 text-emerald-400 rounded text-xs">{kw}</span>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Improvements */}
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6">
                            <h3 className="text-lg font-semibold text-white mb-4">💡 İyileştirme Önerileri</h3>
                            <div className="space-y-3">
                                {result.improvementAreas.map((area, i) => (
                                    <div key={i} className="flex items-start gap-3 bg-amber-500/10 border border-amber-500/20 rounded-lg p-3">
                                        <span className="text-amber-400">⚡</span>
                                        <span className="text-slate-300">{area}</span>
                                    </div>
                                ))}
                            </div>
                        </div>

                        {/* Optimized Suggestions */}
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6">
                            <h3 className="text-lg font-semibold text-white mb-4">✨ Optimize Edilmiş Öneriler</h3>
                            
                            {/* Headline */}
                            {result.optimizedProfile.headline && (
                                <div className="mb-6">
                                    <h4 className="text-sm text-slate-400 mb-2">Önerilen Başlık</h4>
                                    <div className="bg-emerald-500/10 border border-emerald-500/20 rounded-lg p-4">
                                        <p className="text-emerald-300">{result.optimizedProfile.headline}</p>
                                    </div>
                                </div>
                            )}

                            {/* About */}
                            {result.optimizedProfile.about && (
                                <div className="mb-6">
                                    <h4 className="text-sm text-slate-400 mb-2">Önerilen Hakkında</h4>
                                    <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-4">
                                        <p className="text-blue-300 whitespace-pre-wrap">{result.optimizedProfile.about}</p>
                                    </div>
                                </div>
                            )}

                            {/* Suggested Skills */}
                            {result.optimizedProfile.suggestedSkills.length > 0 && (
                                <div>
                                    <h4 className="text-sm text-slate-400 mb-2">Önerilen Beceriler</h4>
                                    <div className="flex flex-wrap gap-2">
                                        {result.optimizedProfile.suggestedSkills.map((skill, i) => (
                                            <span key={i} className="px-3 py-1 bg-purple-500/20 text-purple-300 rounded-full text-sm">
                                                {skill}
                                            </span>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Comparison View */}
                {result && activeTab === 'comparison' && (
                    <div className="space-y-4">
                        {comparison.map((section, i) => (
                            <div key={i} className="bg-slate-800/50 border border-slate-700 rounded-xl overflow-hidden">
                                <div className="bg-slate-700/50 px-4 py-3 flex items-center justify-between">
                                    <h3 className="text-white font-medium">{section.sectionName}</h3>
                                    <span className={`px-2 py-1 rounded text-xs ${
                                        section.improvementScore > 10 ? 'bg-emerald-500/20 text-emerald-400' : 'bg-slate-600 text-slate-400'
                                    }`}>
                                        +{section.improvementScore}%
                                    </span>
                                </div>
                                <div className="grid grid-cols-2 divide-x divide-slate-700">
                                    <div className="p-4">
                                        <div className="text-xs text-slate-500 mb-2 uppercase">Orijinal</div>
                                        <p className="text-slate-400 text-sm whitespace-pre-wrap">
                                            {section.originalContent || '(Boş)'}
                                        </p>
                                    </div>
                                    <div className="p-4 bg-emerald-500/5">
                                        <div className="text-xs text-emerald-500 mb-2 uppercase flex items-center gap-1">
                                            <CheckIcon /> Optimize Edilmiş
                                        </div>
                                        <p className="text-emerald-300 text-sm whitespace-pre-wrap">
                                            {section.optimizedContent || '(Boş)'}
                                        </p>
                                        {section.changes.length > 0 && (
                                            <div className="mt-3 pt-3 border-t border-slate-700">
                                                <div className="text-xs text-slate-500 mb-1">Değişiklikler:</div>
                                                <ul className="text-xs text-slate-400 space-y-1">
                                                    {section.changes.map((change, j) => (
                                                        <li key={j} className="flex items-center gap-1">
                                                            <ArrowRightIcon /> {change}
                                                        </li>
                                                    ))}
                                                </ul>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {/* History */}
                {activeTab === 'history' && (
                    <div className="bg-slate-800/50 border border-slate-700 rounded-xl overflow-hidden">
                        {history.length === 0 ? (
                            <div className="p-8 text-center text-slate-500">
                                Henüz analiz geçmişi yok
                            </div>
                        ) : (
                            <table className="w-full">
                                <thead className="bg-slate-700/50">
                                    <tr>
                                        <th className="px-4 py-3 text-left text-sm text-slate-400">URL</th>
                                        <th className="px-4 py-3 text-left text-sm text-slate-400">Skor</th>
                                        <th className="px-4 py-3 text-left text-sm text-slate-400">Durum</th>
                                        <th className="px-4 py-3 text-left text-sm text-slate-400">Tarih</th>
                                        <th className="px-4 py-3 text-left text-sm text-slate-400"></th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700">
                                    {history.map((item) => (
                                        <tr key={item.id} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-4 py-3 text-sm text-slate-300 truncate max-w-xs">{item.linkedInUrl}</td>
                                            <td className="px-4 py-3">
                                                <span className={`font-bold ${getScoreColor(item.profileScore)}`}>{item.profileScore}</span>
                                            </td>
                                            <td className="px-4 py-3">
                                                <span className={`px-2 py-1 rounded text-xs ${
                                                    item.status === 'Completed' ? 'bg-emerald-500/20 text-emerald-400' :
                                                    item.status === 'Analyzing' ? 'bg-blue-500/20 text-blue-400' :
                                                    'bg-slate-500/20 text-slate-400'
                                                }`}>
                                                    {item.status === 'Completed' ? 'Tamamlandı' :
                                                     item.status === 'Analyzing' ? 'Analiz Ediliyor' :
                                                     item.status === 'Failed' ? 'Başarısız' : item.status}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3 text-sm text-slate-400">
                                                {new Date(item.createdAt).toLocaleDateString('tr-TR')}
                                            </td>
                                            <td className="px-4 py-3">
                                                <button
                                                    onClick={() => handleLoadOptimization(item.id)}
                                                    className="text-blue-400 hover:text-blue-300 text-sm"
                                                >
                                                    Görüntüle
                                                </button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        )}
                    </div>
                )}

                {/* Empty State */}
                {!result && !analyzing && (
                    <div className="text-center py-12">
                        <div className="w-16 h-16 mx-auto mb-4 bg-blue-600/20 rounded-full flex items-center justify-center">
                            <LinkedInIcon />
                        </div>
                        <h3 className="text-xl text-white mb-2">LinkedIn Profilinizi Optimize Edin</h3>
                        <p className="text-slate-400 max-w-md mx-auto">
                            LinkedIn profil URL'nizi girin ve AI destekli analiz ile profilinizi 
                            SEO ve ATS uyumlu hale getirin.
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}

