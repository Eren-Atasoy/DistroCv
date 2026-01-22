import { useState, useEffect } from 'react';
import { skillGapApi } from '../services/api';
import type { SkillGap, SkillDevelopmentProgress, CourseRecommendation, ProjectSuggestion, CertificationRecommendation } from '../services/api';

// Icons
const BookIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
    </svg>
);


const AwardIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
    </svg>
);

const CheckCircleIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
);

const PlayIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
);

const ExternalLinkIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
    </svg>
);

const getCategoryColor = (category: string) => {
    switch (category) {
        case 'Technical': return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
        case 'Certification': return 'bg-purple-500/20 text-purple-400 border-purple-500/30';
        case 'Experience': return 'bg-amber-500/20 text-amber-400 border-amber-500/30';
        case 'SoftSkill': return 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30';
        default: return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
    }
};

const getStatusColor = (status: string) => {
    switch (status) {
        case 'Completed': return 'bg-emerald-500/20 text-emerald-400';
        case 'InProgress': return 'bg-blue-500/20 text-blue-400';
        default: return 'bg-slate-500/20 text-slate-400';
    }
};

const getImportanceStars = (level: number) => {
    return '★'.repeat(level) + '☆'.repeat(5 - level);
};

export default function SkillGapPage() {
    const [skillGaps, setSkillGaps] = useState<SkillGap[]>([]);
    const [progress, setProgress] = useState<SkillDevelopmentProgress | null>(null);
    const [loading, setLoading] = useState(true);
    const [analyzing, setAnalyzing] = useState(false);
    const [selectedCategory, setSelectedCategory] = useState<string>('');
    const [selectedStatus, setSelectedStatus] = useState<string>('');
    const [selectedGap, setSelectedGap] = useState<SkillGap | null>(null);
    const [showDetailModal, setShowDetailModal] = useState(false);
    const [courses, setCourses] = useState<CourseRecommendation[]>([]);
    const [projects, setProjects] = useState<ProjectSuggestion[]>([]);
    const [certifications, setCertifications] = useState<CertificationRecommendation[]>([]);
    const [loadingResources, setLoadingResources] = useState(false);
    const [activeTab, setActiveTab] = useState<'courses' | 'projects' | 'certifications'>('courses');

    useEffect(() => {
        loadData();
    }, [selectedCategory, selectedStatus]);

    const loadData = async () => {
        setLoading(true);
        try {
            const [gapsResponse, progressData] = await Promise.all([
                skillGapApi.getSkillGaps({
                    category: selectedCategory || undefined,
                    status: selectedStatus || undefined,
                }),
                skillGapApi.getDevelopmentProgress()
            ]);
            setSkillGaps(gapsResponse.gaps);
            setProgress(progressData);
        } catch (error) {
            console.error('Error loading data:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleAnalyzeCareer = async () => {
        setAnalyzing(true);
        try {
            await skillGapApi.analyzeCareer();
            await loadData();
            alert('Kariyer analizi tamamlandı!');
        } catch (error) {
            console.error('Error analyzing career:', error);
            alert('Analiz sırasında hata oluştu');
        } finally {
            setAnalyzing(false);
        }
    };

    const handleViewDetails = async (gap: SkillGap) => {
        setSelectedGap(gap);
        setShowDetailModal(true);
        setCourses([]);
        setProjects([]);
        setCertifications([]);
        setLoadingResources(true);
        setActiveTab('courses');

        try {
            const [coursesRes, projectsRes, certsRes] = await Promise.all([
                skillGapApi.getCourseRecommendations(gap.skillName, gap.category),
                skillGapApi.getProjectSuggestions(gap.skillName, gap.category),
                skillGapApi.getCertificationRecommendations(gap.skillName, gap.category)
            ]);
            setCourses(coursesRes.courses || []);
            setProjects(projectsRes.projects || []);
            setCertifications(certsRes.certifications || []);
        } catch (error) {
            console.error('Error loading resources:', error);
        } finally {
            setLoadingResources(false);
        }
    };

    const handleStartLearning = async (gap: SkillGap) => {
        try {
            await skillGapApi.updateProgress(gap.id, { status: 'InProgress', progressPercentage: 0 });
            await loadData();
        } catch (error) {
            console.error('Error starting learning:', error);
        }
    };

    const handleComplete = async (gap: SkillGap) => {
        try {
            await skillGapApi.markAsCompleted(gap.id);
            await loadData();
            setShowDetailModal(false);
        } catch (error) {
            console.error('Error completing:', error);
        }
    };

    const handleUpdateProgress = async (gap: SkillGap, percentage: number) => {
        try {
            await skillGapApi.updateProgress(gap.id, { progressPercentage: percentage });
            await loadData();
        } catch (error) {
            console.error('Error updating progress:', error);
        }
    };

    const categories = ['Technical', 'Certification', 'Experience', 'SoftSkill'];
    const statuses = ['NotStarted', 'InProgress', 'Completed'];

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
            {/* Header */}
            <div className="bg-slate-800/50 border-b border-slate-700">
                <div className="max-w-7xl mx-auto px-4 py-6">
                    <div className="flex items-center justify-between">
                        <div>
                            <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                                <BookIcon />
                                Yetenek Geliştirme
                            </h1>
                            <p className="text-slate-400 mt-1">Eksik becerilerinizi analiz edin ve geliştirin</p>
                        </div>
                        <button
                            onClick={handleAnalyzeCareer}
                            disabled={analyzing}
                            className="px-4 py-2 bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700 text-white rounded-lg font-medium flex items-center gap-2 transition-all disabled:opacity-50"
                        >
                            <AwardIcon />
                            {analyzing ? 'Analiz Ediliyor...' : 'Kariyer Analizi Yap'}
                        </button>
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-4 py-6">
                {/* Progress Overview */}
                {progress && (
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Toplam Beceri Açığı</p>
                            <p className="text-2xl font-bold text-white">{progress.totalSkillGaps}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Tamamlanan</p>
                            <p className="text-2xl font-bold text-emerald-400">{progress.completedSkills}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Devam Eden</p>
                            <p className="text-2xl font-bold text-blue-400">{progress.inProgressSkills}</p>
                        </div>
                        <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
                            <p className="text-slate-400 text-sm">Genel İlerleme</p>
                            <div className="flex items-center gap-2">
                                <div className="flex-1 h-2 bg-slate-700 rounded-full overflow-hidden">
                                    <div 
                                        className="h-full bg-gradient-to-r from-purple-500 to-blue-500 rounded-full transition-all"
                                        style={{ width: `${progress.overallProgress}%` }}
                                    />
                                </div>
                                <span className="text-white font-bold">{Math.round(progress.overallProgress)}%</span>
                            </div>
                        </div>
                    </div>
                )}

                {/* Learning Roadmap */}
                {progress && (progress.currentlyLearning.length > 0 || progress.recentlyCompleted.length > 0) && (
                    <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 mb-6">
                        <h2 className="text-lg font-bold text-white mb-4 flex items-center gap-2">
                            🗺️ Öğrenme Yolculuğunuz
                        </h2>
                        <div className="relative">
                            {/* Timeline line */}
                            <div className="absolute left-4 top-0 bottom-0 w-0.5 bg-gradient-to-b from-emerald-500 via-blue-500 to-slate-600"></div>
                            
                            {/* Completed Skills */}
                            {progress.recentlyCompleted.length > 0 && (
                                <div className="mb-6">
                                    <div className="flex items-center gap-2 mb-3">
                                        <div className="w-8 h-8 bg-emerald-500 rounded-full flex items-center justify-center z-10">
                                            <CheckCircleIcon />
                                        </div>
                                        <span className="text-emerald-400 font-medium">Tamamlanan</span>
                                    </div>
                                    <div className="ml-12 space-y-2">
                                        {progress.recentlyCompleted.slice(0, 3).map((skill) => (
                                            <div key={skill.id} className="bg-emerald-500/10 border border-emerald-500/20 rounded-lg px-3 py-2 text-sm">
                                                <span className="text-emerald-300">{skill.skillName}</span>
                                                <span className="text-slate-500 text-xs ml-2">
                                                    {skill.completedAt && new Date(skill.completedAt).toLocaleDateString('tr-TR')}
                                                </span>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}

                            {/* In Progress Skills */}
                            {progress.currentlyLearning.length > 0 && (
                                <div className="mb-6">
                                    <div className="flex items-center gap-2 mb-3">
                                        <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center z-10 animate-pulse">
                                            <PlayIcon />
                                        </div>
                                        <span className="text-blue-400 font-medium">Şu an öğreniyorsunuz</span>
                                    </div>
                                    <div className="ml-12 space-y-2">
                                        {progress.currentlyLearning.slice(0, 3).map((skill) => (
                                            <div key={skill.id} className="bg-blue-500/10 border border-blue-500/20 rounded-lg px-3 py-2">
                                                <div className="flex items-center justify-between">
                                                    <span className="text-blue-300 text-sm">{skill.skillName}</span>
                                                    <span className="text-blue-400 text-xs font-medium">{skill.progressPercentage}%</span>
                                                </div>
                                                <div className="h-1.5 bg-slate-700 rounded-full mt-2 overflow-hidden">
                                                    <div 
                                                        className="h-full bg-blue-500 rounded-full"
                                                        style={{ width: `${skill.progressPercentage}%` }}
                                                    />
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}

                            {/* Upcoming Skills */}
                            {skillGaps.filter(g => g.status === 'NotStarted').length > 0 && (
                                <div>
                                    <div className="flex items-center gap-2 mb-3">
                                        <div className="w-8 h-8 bg-slate-600 rounded-full flex items-center justify-center z-10">
                                            <span className="text-slate-400">📋</span>
                                        </div>
                                        <span className="text-slate-400 font-medium">Sıradaki beceriler</span>
                                    </div>
                                    <div className="ml-12 space-y-2">
                                        {skillGaps.filter(g => g.status === 'NotStarted').slice(0, 3).map((skill) => (
                                            <div key={skill.id} className="bg-slate-700/30 border border-slate-600 rounded-lg px-3 py-2 text-sm">
                                                <span className="text-slate-400">{skill.skillName}</span>
                                                <span className="text-amber-400 text-xs ml-2">{getImportanceStars(skill.importanceLevel)}</span>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Category Distribution */}
                {progress && progress.gapsByCategory && Object.keys(progress.gapsByCategory).length > 0 && (
                    <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 mb-6">
                        <h2 className="text-lg font-bold text-white mb-4">📊 Kategori Dağılımı</h2>
                        <div className="space-y-3">
                            {Object.entries(progress.gapsByCategory).map(([category, count]) => {
                                const completed = progress.completedByCategory?.[category] || 0;
                                const percentage = count > 0 ? (completed / count) * 100 : 0;
                                return (
                                    <div key={category}>
                                        <div className="flex justify-between text-sm mb-1">
                                            <span className={`${
                                                category === 'Technical' ? 'text-blue-400' :
                                                category === 'Certification' ? 'text-purple-400' :
                                                category === 'Experience' ? 'text-amber-400' : 'text-emerald-400'
                                            }`}>
                                                {category === 'Technical' ? '💻 Teknik' :
                                                 category === 'Certification' ? '🏆 Sertifika' :
                                                 category === 'Experience' ? '📈 Deneyim' : '🤝 Soft Skill'}
                                            </span>
                                            <span className="text-slate-400">{completed}/{count}</span>
                                        </div>
                                        <div className="h-2 bg-slate-700 rounded-full overflow-hidden">
                                            <div 
                                                className={`h-full rounded-full transition-all ${
                                                    category === 'Technical' ? 'bg-blue-500' :
                                                    category === 'Certification' ? 'bg-purple-500' :
                                                    category === 'Experience' ? 'bg-amber-500' : 'bg-emerald-500'
                                                }`}
                                                style={{ width: `${percentage}%` }}
                                            />
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                )}

                {/* Filters */}
                <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4 mb-6">
                    <div className="flex flex-wrap gap-4">
                        <select
                            value={selectedCategory}
                            onChange={(e) => setSelectedCategory(e.target.value)}
                            className="px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-purple-500"
                        >
                            <option value="">Tüm Kategoriler</option>
                            {categories.map(cat => (
                                <option key={cat} value={cat}>
                                    {cat === 'Technical' ? 'Teknik Beceriler' : 
                                     cat === 'Certification' ? 'Sertifikalar' :
                                     cat === 'Experience' ? 'Deneyim' : 'Soft Skills'}
                                </option>
                            ))}
                        </select>
                        <select
                            value={selectedStatus}
                            onChange={(e) => setSelectedStatus(e.target.value)}
                            className="px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-purple-500"
                        >
                            <option value="">Tüm Durumlar</option>
                            {statuses.map(status => (
                                <option key={status} value={status}>
                                    {status === 'NotStarted' ? 'Başlanmadı' : 
                                     status === 'InProgress' ? 'Devam Ediyor' : 'Tamamlandı'}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                {/* Skill Gaps Grid */}
                {loading ? (
                    <div className="text-center py-12 text-slate-400">Yükleniyor...</div>
                ) : skillGaps.length === 0 ? (
                    <div className="text-center py-12">
                        <BookIcon />
                        <p className="text-slate-400 mt-4">Henüz beceri açığı analizi yapılmamış.</p>
                        <p className="text-slate-500 text-sm mt-2">Kariyer analizi yaparak başlayın.</p>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {skillGaps.map((gap) => (
                            <div
                                key={gap.id}
                                className="bg-slate-800/50 border border-slate-700 rounded-xl p-4 hover:border-purple-500/50 transition-all"
                            >
                                <div className="flex items-start justify-between mb-3">
                                    <div>
                                        <h3 className="text-white font-semibold">{gap.skillName}</h3>
                                        <div className="flex items-center gap-2 mt-1">
                                            <span className={`px-2 py-0.5 rounded-full text-xs border ${getCategoryColor(gap.category)}`}>
                                                {gap.category}
                                            </span>
                                            <span className={`px-2 py-0.5 rounded-full text-xs ${getStatusColor(gap.status)}`}>
                                                {gap.status === 'NotStarted' ? 'Başlanmadı' : 
                                                 gap.status === 'InProgress' ? 'Devam Ediyor' : 'Tamamlandı'}
                                            </span>
                                        </div>
                                    </div>
                                    <span className="text-amber-400 text-sm">{getImportanceStars(gap.importanceLevel)}</span>
                                </div>

                                {gap.description && (
                                    <p className="text-slate-400 text-sm mb-3 line-clamp-2">{gap.description}</p>
                                )}

                                <div className="flex items-center gap-4 text-sm text-slate-500 mb-3">
                                    <span>⏱ {gap.estimatedLearningHours} saat</span>
                                    <span>📚 {gap.recommendedCourses?.length || 0} kurs</span>
                                </div>

                                {/* Progress bar */}
                                {gap.status === 'InProgress' && (
                                    <div className="mb-3">
                                        <div className="flex justify-between text-xs text-slate-400 mb-1">
                                            <span>İlerleme</span>
                                            <span>{gap.progressPercentage}%</span>
                                        </div>
                                        <div className="h-2 bg-slate-700 rounded-full overflow-hidden">
                                            <div 
                                                className="h-full bg-blue-500 rounded-full transition-all"
                                                style={{ width: `${gap.progressPercentage}%` }}
                                            />
                                        </div>
                                    </div>
                                )}

                                <div className="flex gap-2">
                                    <button
                                        onClick={() => handleViewDetails(gap)}
                                        className="flex-1 px-3 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg text-sm transition-colors"
                                    >
                                        Detaylar
                                    </button>
                                    {gap.status === 'NotStarted' && (
                                        <button
                                            onClick={() => handleStartLearning(gap)}
                                            className="px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm flex items-center gap-1 transition-colors"
                                        >
                                            <PlayIcon /> Başla
                                        </button>
                                    )}
                                    {gap.status === 'InProgress' && (
                                        <button
                                            onClick={() => handleComplete(gap)}
                                            className="px-3 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-sm flex items-center gap-1 transition-colors"
                                        >
                                            <CheckCircleIcon /> Tamamla
                                        </button>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Detail Modal */}
            {showDetailModal && selectedGap && (
                <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 overflow-y-auto p-4">
                    <div className="bg-slate-800 border border-slate-700 rounded-xl w-full max-w-3xl my-8">
                        <div className="p-6 border-b border-slate-700">
                            <div className="flex items-start justify-between">
                                <div>
                                    <h2 className="text-xl font-bold text-white">{selectedGap.skillName}</h2>
                                    <div className="flex items-center gap-2 mt-2">
                                        <span className={`px-2 py-0.5 rounded-full text-xs border ${getCategoryColor(selectedGap.category)}`}>
                                            {selectedGap.category}
                                        </span>
                                        <span className="text-amber-400 text-sm">{getImportanceStars(selectedGap.importanceLevel)}</span>
                                    </div>
                                </div>
                                <button
                                    onClick={() => setShowDetailModal(false)}
                                    className="text-slate-400 hover:text-white text-2xl"
                                >
                                    ×
                                </button>
                            </div>
                            {selectedGap.description && (
                                <p className="text-slate-400 mt-3">{selectedGap.description}</p>
                            )}
                        </div>

                        <div className="p-6 max-h-[60vh] overflow-y-auto space-y-6">
                            {/* Progress Update */}
                            {selectedGap.status === 'InProgress' && (
                                <div className="bg-slate-700/30 rounded-lg p-4">
                                    <h3 className="text-white font-semibold mb-3">📊 İlerleme Güncelle</h3>
                                    <div className="flex items-center gap-4">
                                        <input
                                            type="range"
                                            min="0"
                                            max="100"
                                            value={selectedGap.progressPercentage}
                                            onChange={(e) => handleUpdateProgress(selectedGap, parseInt(e.target.value))}
                                            className="flex-1 h-2 bg-slate-600 rounded-lg appearance-none cursor-pointer accent-blue-500"
                                        />
                                        <span className="text-white font-bold w-16 text-right">{selectedGap.progressPercentage}%</span>
                                    </div>
                                    <div className="h-3 bg-slate-700 rounded-full overflow-hidden mt-3">
                                        <div 
                                            className="h-full bg-gradient-to-r from-blue-500 to-purple-500 rounded-full transition-all duration-300"
                                            style={{ width: `${selectedGap.progressPercentage}%` }}
                                        />
                                    </div>
                                </div>
                            )}

                            {/* Learning Stats */}
                            <div className="grid grid-cols-3 gap-3">
                                <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-3 text-center">
                                    <p className="text-2xl font-bold text-blue-400">{courses.length}</p>
                                    <p className="text-xs text-slate-400">Kurs</p>
                                </div>
                                <div className="bg-purple-500/10 border border-purple-500/20 rounded-lg p-3 text-center">
                                    <p className="text-2xl font-bold text-purple-400">{projects.length}</p>
                                    <p className="text-xs text-slate-400">Proje</p>
                                </div>
                                <div className="bg-amber-500/10 border border-amber-500/20 rounded-lg p-3 text-center">
                                    <p className="text-2xl font-bold text-amber-400">{certifications.length}</p>
                                    <p className="text-xs text-slate-400">Sertifika</p>
                                </div>
                            </div>

                            {/* Tabs */}
                            <div className="border-b border-slate-700">
                                <div className="flex gap-1">
                                    <button
                                        onClick={() => setActiveTab('courses')}
                                        className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                            activeTab === 'courses' 
                                                ? 'border-blue-500 text-blue-400' 
                                                : 'border-transparent text-slate-400 hover:text-white'
                                        }`}
                                    >
                                        📚 Kurslar ({courses.length})
                                    </button>
                                    <button
                                        onClick={() => setActiveTab('projects')}
                                        className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                            activeTab === 'projects' 
                                                ? 'border-purple-500 text-purple-400' 
                                                : 'border-transparent text-slate-400 hover:text-white'
                                        }`}
                                    >
                                        💻 Projeler ({projects.length})
                                    </button>
                                    <button
                                        onClick={() => setActiveTab('certifications')}
                                        className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                                            activeTab === 'certifications' 
                                                ? 'border-amber-500 text-amber-400' 
                                                : 'border-transparent text-slate-400 hover:text-white'
                                        }`}
                                    >
                                        🏆 Sertifikalar ({certifications.length})
                                    </button>
                                </div>
                            </div>

                            {/* Tab Content */}
                            {loadingResources ? (
                                <div className="text-center py-8">
                                    <div className="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full mx-auto"></div>
                                    <p className="text-slate-400 mt-2">Öneriler yükleniyor...</p>
                                </div>
                            ) : (
                                <>
                                    {/* Courses Tab */}
                                    {activeTab === 'courses' && (
                                        <div className="space-y-3">
                                            {courses.length > 0 ? courses.map((course, idx) => (
                                                <div key={idx} className="bg-slate-700/50 rounded-lg p-4 hover:bg-slate-700/70 transition-colors">
                                                    <div className="flex items-start justify-between">
                                                        <div className="flex-1">
                                                            <h4 className="text-white font-medium">{course.title}</h4>
                                                            <p className="text-blue-400 text-sm">{course.provider}</p>
                                                            {course.description && (
                                                                <p className="text-slate-400 text-sm mt-2">{course.description}</p>
                                                            )}
                                                        </div>
                                                        {course.url && (
                                                            <a
                                                                href={course.url}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                className="ml-3 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-lg flex items-center gap-1 transition-colors"
                                                            >
                                                                Kursa Git <ExternalLinkIcon />
                                                            </a>
                                                        )}
                                                    </div>
                                                    <div className="flex items-center gap-4 mt-3 text-xs">
                                                        <span className="px-2 py-1 bg-slate-600 text-slate-300 rounded">{course.level}</span>
                                                        <span className="text-slate-400">⏱ {course.estimatedHours} saat</span>
                                                        {course.rating && <span className="text-amber-400">⭐ {course.rating}</span>}
                                                        {course.price !== undefined && (
                                                            <span className="text-emerald-400">
                                                                {course.price === 0 ? '🆓 Ücretsiz' : `💰 $${course.price}`}
                                                            </span>
                                                        )}
                                                    </div>
                                                </div>
                                            )) : (
                                                <div className="text-center py-8 text-slate-500">
                                                    <p>Kurs önerisi bulunamadı</p>
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    {/* Projects Tab */}
                                    {activeTab === 'projects' && (
                                        <div className="space-y-3">
                                            {projects.length > 0 ? projects.map((project, idx) => (
                                                <div key={idx} className="bg-slate-700/50 rounded-lg p-4 hover:bg-slate-700/70 transition-colors">
                                                    <div className="flex items-start justify-between">
                                                        <div>
                                                            <h4 className="text-white font-medium">{project.title}</h4>
                                                            <span className={`inline-block mt-1 px-2 py-0.5 rounded text-xs ${
                                                                project.difficulty === 'Beginner' ? 'bg-emerald-500/20 text-emerald-400' :
                                                                project.difficulty === 'Intermediate' ? 'bg-amber-500/20 text-amber-400' :
                                                                'bg-red-500/20 text-red-400'
                                                            }`}>
                                                                {project.difficulty === 'Beginner' ? '🌱 Başlangıç' :
                                                                 project.difficulty === 'Intermediate' ? '📈 Orta' : '🚀 İleri'}
                                                            </span>
                                                        </div>
                                                        {project.gitHubTemplate && (
                                                            <a
                                                                href={project.gitHubTemplate}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                className="ml-3 px-3 py-1.5 bg-slate-600 hover:bg-slate-500 text-white text-sm rounded-lg flex items-center gap-1"
                                                            >
                                                                GitHub <ExternalLinkIcon />
                                                            </a>
                                                        )}
                                                    </div>
                                                    <p className="text-slate-400 text-sm mt-2">{project.description}</p>
                                                    {project.learningOutcomes && (
                                                        <p className="text-purple-400 text-sm mt-2">✨ {project.learningOutcomes}</p>
                                                    )}
                                                    <div className="flex flex-wrap gap-2 mt-3">
                                                        {project.technologies.map((tech, i) => (
                                                            <span key={i} className="px-2 py-1 bg-purple-500/20 text-purple-300 rounded text-xs">
                                                                {tech}
                                                            </span>
                                                        ))}
                                                    </div>
                                                    <div className="mt-3 text-xs text-slate-400">
                                                        ⏱ Tahmini süre: {project.estimatedHours} saat
                                                    </div>
                                                </div>
                                            )) : (
                                                <div className="text-center py-8 text-slate-500">
                                                    <p>Proje önerisi bulunamadı</p>
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    {/* Certifications Tab */}
                                    {activeTab === 'certifications' && (
                                        <div className="space-y-3">
                                            {certifications.length > 0 ? certifications.map((cert, idx) => (
                                                <div key={idx} className="bg-slate-700/50 rounded-lg p-4 hover:bg-slate-700/70 transition-colors">
                                                    <div className="flex items-start justify-between">
                                                        <div className="flex-1">
                                                            <h4 className="text-white font-medium">{cert.name}</h4>
                                                            <p className="text-amber-400 text-sm">{cert.provider}</p>
                                                            {cert.description && (
                                                                <p className="text-slate-400 text-sm mt-2">{cert.description}</p>
                                                            )}
                                                        </div>
                                                        {cert.url && (
                                                            <a
                                                                href={cert.url}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                className="ml-3 px-3 py-1.5 bg-amber-600 hover:bg-amber-700 text-white text-sm rounded-lg flex items-center gap-1 transition-colors"
                                                            >
                                                                Başvur <ExternalLinkIcon />
                                                            </a>
                                                        )}
                                                    </div>
                                                    <div className="flex items-center gap-4 mt-3 text-xs">
                                                        <span className="px-2 py-1 bg-amber-500/20 text-amber-300 rounded">{cert.level}</span>
                                                        {cert.cost !== undefined && (
                                                            <span className="text-slate-400">💰 ${cert.cost}</span>
                                                        )}
                                                        {cert.validityYears && (
                                                            <span className="text-slate-400">📅 {cert.validityYears} yıl geçerli</span>
                                                        )}
                                                    </div>
                                                    {cert.prerequisites && cert.prerequisites.length > 0 && (
                                                        <div className="mt-3">
                                                            <p className="text-xs text-slate-500 mb-1">Ön koşullar:</p>
                                                            <div className="flex flex-wrap gap-2">
                                                                {cert.prerequisites.map((prereq, i) => (
                                                                    <span key={i} className="px-2 py-0.5 bg-slate-600 text-slate-300 rounded text-xs">
                                                                        {prereq}
                                                                    </span>
                                                                ))}
                                                            </div>
                                                        </div>
                                                    )}
                                                </div>
                                            )) : (
                                                <div className="text-center py-8 text-slate-500">
                                                    <p>Sertifika önerisi bulunamadı</p>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </>
                            )}
                        </div>

                        <div className="p-6 border-t border-slate-700 flex justify-end gap-3">
                            <button
                                onClick={() => setShowDetailModal(false)}
                                className="px-4 py-2 bg-slate-700 text-white rounded-lg"
                            >
                                Kapat
                            </button>
                            {selectedGap.status !== 'Completed' && (
                                <button
                                    onClick={() => handleComplete(selectedGap)}
                                    className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg flex items-center gap-2"
                                >
                                    <CheckCircleIcon /> Tamamlandı Olarak İşaretle
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

