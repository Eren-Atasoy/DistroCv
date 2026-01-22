import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import {
    FileText,
    Sparkles,
    Download,
    Eye,
    Check,
    RefreshCw,
    Sliders,
    Edit3,
    Save,
    X,
    AlertCircle,
    Loader2,
    ArrowLeft,
    Send
} from 'lucide-react'
import { api } from '../services/api'

// Types
interface Application {
    id: string
    jobPostingId: string
    jobPosting: {
        id: string
        title: string
        companyName: string
        location: string
        description: string
    }
    tailoredResumeUrl?: string
    coverLetter?: string
    customMessage?: string
    status: string
}

interface TailoredResumeResult {
    htmlContent: string
    plainTextContent: string
    optimizedKeywords: string[]
    addedSkills: string[]
    highlightedExperiences: string[]
    atsScore: number
}

interface ResumeChange {
    section: string
    changeType: string
    originalText: string
    newText: string
    reason: string
}

interface ResumeComparison {
    originalContent: string
    tailoredContent: string
    changes: ResumeChange[]
    similarityScore: number
}

export default function ResumeEditor() {
    const { id: applicationId } = useParams()
    const navigate = useNavigate()

    // State
    const [application, setApplication] = useState<Application | null>(null)
    const [originalResume, setOriginalResume] = useState('')
    const [tailoredResume, setTailoredResume] = useState('')
    const [editedResume, setEditedResume] = useState('')
    const [coverLetter, setCoverLetter] = useState('')
    const [customMessage, setCustomMessage] = useState('')

    const [viewMode, setViewMode] = useState<'split' | 'tailored'>('split')
    const [isEditing, setIsEditing] = useState(false)
    const [tone, setTone] = useState(50)

    const [isLoading, setIsLoading] = useState(true)
    const [isGenerating, setIsGenerating] = useState(false)
    const [isSaving, setIsSaving] = useState(false)
    const [isExporting, setIsExporting] = useState(false)
    const [isSending, setIsSending] = useState(false)

    const [resumeResult, setResumeResult] = useState<TailoredResumeResult | null>(null)
    const [comparison, setComparison] = useState<ResumeComparison | null>(null)
    const [showPreview, setShowPreview] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const editorRef = useRef<HTMLTextAreaElement>(null)


    // Load application and resume data
    useEffect(() => {
        if (!applicationId) {
            setError('Application ID not found')
            setIsLoading(false)
            return
        }

        loadApplicationData()
    }, [applicationId])

    const loadApplicationData = async () => {
        try {
            setIsLoading(true)
            setError(null)

            // Get application details
            const app = await api.get<Application>(`/applications/${applicationId}`)
            setApplication(app)

            // Get user's digital twin for original resume
            const digitalTwin = await api.get<any>('/profile/digital-twin')
            setOriginalResume(digitalTwin.parsedResumeJson || '')

            // If tailored resume exists, load it
            if (app.tailoredResumeUrl) {
                // In real app, fetch from S3 URL
                // For now, generate it
                await generateTailoredResume(app.jobPostingId)
            }

            // Load cover letter and custom message
            setCoverLetter(app.coverLetter || '')
            setCustomMessage(app.customMessage || '')

        } catch (err) {
            console.error('Error loading application:', err)
            setError('Failed to load application data')
        } finally {
            setIsLoading(false)
        }
    }

    const generateTailoredResume = async (jobPostingId?: string) => {
        if (!applicationId) return

        try {
            setIsGenerating(true)
            setError(null)

            const userId = 'current-user-id' // Get from auth context
            const jobId = jobPostingId || application?.jobPostingId

            if (!jobId) {
                throw new Error('Job posting ID not found')
            }

            // Generate tailored resume
            const result = await api.post<TailoredResumeResult>(
                '/resume-tailoring/generate',
                { userId, jobPostingId: jobId }
            )

            setResumeResult(result)
            setTailoredResume(result.plainTextContent)
            setEditedResume(result.plainTextContent)

            // Get comparison
            const comparisonResult = await api.post<ResumeComparison>(
                '/resume-tailoring/compare',
                {
                    originalContent: originalResume,
                    tailoredContent: result.plainTextContent
                }
            )

            setComparison(comparisonResult)

        } catch (err) {
            console.error('Error generating resume:', err)
            setError('Failed to generate tailored resume')
        } finally {
            setIsGenerating(false)
        }
    }

    const handleRegenerate = async () => {
        await generateTailoredResume()
    }

    const handleToneChange = async (newTone: number) => {
        setTone(newTone)
        // Optionally regenerate with new tone
        // await generateTailoredResume()
    }

    const handleEdit = () => {
        setIsEditing(true)
        setEditedResume(tailoredResume)
    }

    const handleCancelEdit = () => {
        setIsEditing(false)
        setEditedResume(tailoredResume)
    }

    const handleSave = async () => {
        if (!applicationId) return

        try {
            setIsSaving(true)
            setError(null)

            // Save edited content
            await api.put(`/applications/${applicationId}/edit`, {
                customMessage: editedResume,
                coverLetter: coverLetter
            })

            setTailoredResume(editedResume)
            setIsEditing(false)

        } catch (err) {
            console.error('Error saving changes:', err)
            setError('Failed to save changes')
        } finally {
            setIsSaving(false)
        }
    }

    const handleExportPDF = async () => {
        if (!applicationId) return

        try {
            setIsExporting(true)
            setError(null)

            // Export to PDF
            const response = await fetch(`${api['baseUrl']}/resume-tailoring/export-pdf`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ htmlContent: resumeResult?.htmlContent || tailoredResume })
            })

            if (!response.ok) throw new Error('Export failed')

            const blob = await response.blob()
            const url = window.URL.createObjectURL(blob)
            const a = document.createElement('a')
            a.href = url
            a.download = `resume-${application?.jobPosting.companyName || 'tailored'}.pdf`
            document.body.appendChild(a)
            a.click()
            window.URL.revokeObjectURL(url)
            document.body.removeChild(a)

        } catch (err) {
            console.error('Error exporting PDF:', err)
            setError('Failed to export PDF')
        } finally {
            setIsExporting(false)
        }
    }

    const handlePreview = () => {
        setShowPreview(true)
    }

    const handleSendApplication = async () => {
        if (!applicationId) return

        try {
            setIsSending(true)
            setError(null)

            // Send application
            await api.post(`/applications/${applicationId}/send`, {
                confirmSend: true
            })

            // Navigate to applications page
            navigate('/applications')

        } catch (err) {
            console.error('Error sending application:', err)
            setError('Failed to send application')
        } finally {
            setIsSending(false)
        }
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center min-h-[400px]">
                <Loader2 className="w-8 h-8 text-primary-500 animate-spin" />
            </div>
        )
    }

    if (error && !application) {
        return (
            <div className="glass-card p-6">
                <div className="flex items-center gap-3 text-red-400">
                    <AlertCircle size={24} />
                    <div>
                        <h3 className="font-semibold">Error</h3>
                        <p className="text-sm">{error}</p>
                    </div>
                </div>
            </div>
        )
    }

    const handleRegenerate = () => {
        setIsGenerating(true)
        setTimeout(() => setIsGenerating(false), 2000)
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-display font-bold text-white mb-1">
                        CV Düzenleyici
                    </h1>
                    <p className="text-surface-400">
                        Senior Frontend Developer - Trendyol başvurusu için
                    </p>
                </div>

                <div className="flex items-center gap-3">
                    <button
                        onClick={handleRegenerate}
                        disabled={isGenerating}
                        className="px-4 py-2 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2 disabled:opacity-50"
                    >
                        <RefreshCw size={18} className={isGenerating ? 'animate-spin' : ''} />
                        Yeniden Oluştur
                    </button>
                    <button className="px-4 py-2 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2">
                        <Eye size={18} />
                        Önizle
                    </button>
                    <button className="px-4 py-2 rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors flex items-center gap-2">
                        <Download size={18} />
                        PDF İndir
                    </button>
                </div>
            </div>

            {/* Toolbar */}
            <div className="glass-card p-4">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    {/* View Mode Toggle */}
                    <div className="flex items-center gap-2">
                        <span className="text-surface-400 text-sm">Görünüm:</span>
                        <div className="flex bg-surface-800 rounded-lg p-1">
                            <button
                                onClick={() => setViewMode('split')}
                                className={`px-3 py-1.5 rounded-md text-sm transition-colors ${viewMode === 'split'
                                    ? 'bg-primary-500 text-white'
                                    : 'text-surface-400 hover:text-white'
                                    }`}
                            >
                                Karşılaştır
                            </button>
                            <button
                                onClick={() => setViewMode('tailored')}
                                className={`px-3 py-1.5 rounded-md text-sm transition-colors ${viewMode === 'tailored'
                                    ? 'bg-primary-500 text-white'
                                    : 'text-surface-400 hover:text-white'
                                    }`}
                            >
                                Sadece Optimized
                            </button>
                        </div>
                    </div>

                    {/* Tone Slider */}
                    <div className="flex items-center gap-4">
                        <Sliders size={18} className="text-surface-400" />
                        <div className="flex items-center gap-3">
                            <span className="text-surface-400 text-sm">Profesyonel</span>
                            <input
                                type="range"
                                min="0"
                                max="100"
                                value={tone}
                                onChange={(e) => setTone(Number(e.target.value))}
                                className="w-32 accent-primary-500"
                            />
                            <span className="text-surface-400 text-sm">Yaratıcı</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* AI Info */}
            <div className="p-4 rounded-xl bg-primary-500/10 border border-primary-500/20 flex items-start gap-3">
                <Sparkles size={20} className="text-primary-400 mt-0.5" />
                <div>
                    <p className="text-primary-300 font-medium text-sm mb-1">AI Optimizasyonu Tamamlandı</p>
                    <p className="text-surface-400 text-sm">
                        CV'niz "Senior Frontend Developer - Trendyol" pozisyonu için optimize edildi.
                        Anahtar kelimeler eklendi, başarılar ölçülebilir metriklerle güçlendirildi.
                    </p>
                </div>
            </div>

            {/* Editor */}
            <div className={`grid gap-6 ${viewMode === 'split' ? 'md:grid-cols-2' : 'md:grid-cols-1 max-w-3xl mx-auto'}`}>
                {/* Original */}
                {viewMode === 'split' && (
                    <motion.div
                        initial={{ opacity: 0, x: -20 }}
                        animate={{ opacity: 1, x: 0 }}
                        className="glass-card"
                    >
                        <div className="p-4 border-b border-surface-700 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <FileText size={18} className="text-surface-400" />
                                <span className="text-white font-medium">Orijinal CV</span>
                            </div>
                            <span className="text-xs text-surface-500">Salt okunur</span>
                        </div>
                        <div className="p-6 max-h-[600px] overflow-auto">
                            <pre className="text-surface-300 text-sm whitespace-pre-wrap font-sans leading-relaxed">
                                {originalResume}
                            </pre>
                        </div>
                    </motion.div>
                )}

                {/* Tailored */}
                <motion.div
                    initial={{ opacity: 0, x: 20 }}
                    animate={{ opacity: 1, x: 0 }}
                    className="glass-card"
                >
                    <div className="p-4 border-b border-surface-700 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Sparkles size={18} className="text-primary-400" />
                            <span className="text-white font-medium">Optimize Edilmiş CV</span>
                        </div>
                        <div className="flex items-center gap-2">
                            <Check size={14} className="text-accent-emerald" />
                            <span className="text-xs text-accent-emerald">Trendyol için optimize</span>
                        </div>
                    </div>
                    <div className="p-6 max-h-[600px] overflow-auto">
                        <pre className="text-surface-300 text-sm whitespace-pre-wrap font-sans leading-relaxed">
                            {tailoredResume}
                        </pre>
                    </div>
                </motion.div>
            </div>

            {/* Highlights */}
            <div className="glass-card p-6">
                <h3 className="text-lg font-semibold text-white mb-4">Yapılan İyileştirmeler</h3>
                <div className="grid md:grid-cols-3 gap-4">
                    {[
                        { label: 'Eklenen Anahtar Kelimeler', value: 8, items: ['E-ticaret', 'Next.js', 'SSR', 'Agile'] },
                        { label: 'Ölçülebilir Metrikler', value: 5, items: ['%40 performans', '%25 dönüşüm', '%60 SEO'] },
                        { label: 'Güçlendirilmiş Başarılar', value: 4, items: ['Teknik liderlik', 'CI/CD', 'Ölçeklenebilirlik'] },
                    ].map((section, index) => (
                        <div key={index} className="p-4 rounded-xl bg-surface-800/50">
                            <div className="text-2xl font-bold text-primary-400 mb-1">{section.value}</div>
                            <div className="text-surface-300 text-sm font-medium mb-3">{section.label}</div>
                            <div className="flex flex-wrap gap-2">
                                {section.items.map((item, i) => (
                                    <span key={i} className="px-2 py-1 rounded-full bg-primary-500/10 text-primary-400 text-xs">
                                        {item}
                                    </span>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Action */}
            <div className="flex justify-center">
                <button className="btn-glow px-8 py-4 rounded-xl bg-gradient-to-r from-primary-500 to-accent-cyan text-white font-semibold text-lg">
                    Başvuruyu Onayla ve Gönder
                </button>
            </div>
        </div>
    )
}
