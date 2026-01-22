import React, { useState, useEffect, useRef } from 'react'
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
    Send,
    Highlighter,
    CheckCircle2
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
    const [_customMessage, setCustomMessage] = useState('')

    const [viewMode, setViewMode] = useState<'split' | 'tailored' | 'preview'>('split')
    const [isEditing, setIsEditing] = useState(false)
    const [tone, setTone] = useState(50)
    const [showChanges, setShowChanges] = useState(true)

    const [isLoading, setIsLoading] = useState(true)
    const [isGenerating, setIsGenerating] = useState(false)
    const [isSaving, setIsSaving] = useState(false)
    const [isExporting, setIsExporting] = useState(false)
    const [isSending, setIsSending] = useState(false)

    const [resumeResult, setResumeResult] = useState<TailoredResumeResult | null>(null)
    const [comparison, setComparison] = useState<ResumeComparison | null>(null)
    const [_showPreview, setShowPreview] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [successMessage, setSuccessMessage] = useState<string | null>(null)

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
            const parsedResume = typeof digitalTwin.parsedResumeJson === 'string'
                ? JSON.parse(digitalTwin.parsedResumeJson)
                : digitalTwin.parsedResumeJson

            // Format original resume for display
            const formattedOriginal = formatResumeForDisplay(parsedResume)
            setOriginalResume(formattedOriginal)

            // If tailored resume exists, load it
            if (app.tailoredResumeUrl) {
                // In real app, fetch from S3 URL
                await generateTailoredResume(app.jobPostingId)
            } else {
                // Generate new tailored resume
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

    const formatResumeForDisplay = (resumeData: any): string => {
        if (typeof resumeData === 'string') return resumeData

        let formatted = ''

        if (resumeData.personalInfo) {
            formatted += `${resumeData.personalInfo.name || ''}\n`
            formatted += `${resumeData.personalInfo.email || ''} | ${resumeData.personalInfo.phone || ''}\n`
            formatted += `${resumeData.personalInfo.location || ''}\n\n`
        }

        if (resumeData.summary) {
            formatted += `PROFESSIONAL SUMMARY\n${resumeData.summary}\n\n`
        }

        if (resumeData.skills && resumeData.skills.length > 0) {
            formatted += `SKILLS\n${resumeData.skills.join(', ')}\n\n`
        }

        if (resumeData.experience && resumeData.experience.length > 0) {
            formatted += `WORK EXPERIENCE\n`
            resumeData.experience.forEach((exp: any) => {
                formatted += `\n${exp.title || ''} at ${exp.company || ''}\n`
                formatted += `${exp.startDate || ''} - ${exp.endDate || 'Present'}\n`
                formatted += `${exp.description || ''}\n`
            })
            formatted += '\n'
        }

        if (resumeData.education && resumeData.education.length > 0) {
            formatted += `EDUCATION\n`
            resumeData.education.forEach((edu: any) => {
                formatted += `\n${edu.degree || ''} in ${edu.field || ''}\n`
                formatted += `${edu.institution || ''} (${edu.graduationYear || ''})\n`
            })
        }

        return formatted
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

    const handleToneChange = async (newTone: number) => {
        setTone(newTone)

        // Regenerate with new tone if resume exists
        if (resumeResult && application) {
            setIsGenerating(true)
            try {
                // In real implementation, pass tone to API
                await generateTailoredResume(application.jobPostingId)
            } finally {
                setIsGenerating(false)
            }
        }
    }

    const handleEdit = () => {
        setIsEditing(true)
        setEditedResume(tailoredResume)
        setTimeout(() => {
            editorRef.current?.focus()
        }, 100)
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
            setSuccessMessage('Changes saved successfully!')

            setTimeout(() => setSuccessMessage(null), 3000)

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

            // Export to PDF using ResumeTailoringService
            const response = await fetch(`${api['baseUrl']}/resume-tailoring/export-pdf`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    htmlContent: resumeResult?.htmlContent || `<pre>${editedResume || tailoredResume}</pre>`
                })
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

            setSuccessMessage('PDF exported successfully!')
            setTimeout(() => setSuccessMessage(null), 3000)

        } catch (err) {
            console.error('Error exporting PDF:', err)
            setError('Failed to export PDF')
        } finally {
            setIsExporting(false)
        }
    }

    const handlePreview = () => {
        setViewMode('preview')
        setShowPreview(true)
    }

    const handleSendApplication = async () => {
        if (!applicationId) return

        const confirmed = window.confirm(
            'Are you sure you want to send this application? This action cannot be undone.'
        )

        if (!confirmed) return

        try {
            setIsSending(true)
            setError(null)

            // Send application
            await api.post(`/applications/${applicationId}/send`, {
                confirmSend: true
            })

            setSuccessMessage('Application sent successfully!')

            // Navigate to applications page after delay
            setTimeout(() => {
                navigate('/applications')
            }, 2000)

        } catch (err) {
            console.error('Error sending application:', err)
            setError('Failed to send application')
        } finally {
            setIsSending(false)
        }
    }

    const highlightChanges = (text: string, changes: ResumeChange[]): React.JSX.Element[] => {
        if (!showChanges || !changes || changes.length === 0) {
            return [<span key="0">{text}</span>]
        }

        const elements: React.JSX.Element[] = []
        let lastIndex = 0

        changes.forEach((change, idx) => {
            if (change.changeType === 'Added' || change.changeType === 'Modified') {
                const index = text.indexOf(change.newText, lastIndex)
                if (index !== -1) {
                    // Add text before change
                    if (index > lastIndex) {
                        elements.push(
                            <span key={`text-${idx}`}>
                                {text.substring(lastIndex, index)}
                            </span>
                        )
                    }

                    // Add highlighted change
                    elements.push(
                        <mark
                            key={`change-${idx}`}
                            className="bg-primary-500/20 text-primary-300 px-1 rounded"
                            title={change.reason}
                        >
                            {change.newText}
                        </mark>
                    )

                    lastIndex = index + change.newText.length
                }
            }
        })

        // Add remaining text
        if (lastIndex < text.length) {
            elements.push(
                <span key="remaining">{text.substring(lastIndex)}</span>
            )
        }

        return elements.length > 0 ? elements : [<span key="0">{text}</span>]
    }

    const handleRegenerate = async () => {
        await generateTailoredResume()
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
                <button
                    onClick={() => navigate('/applications')}
                    className="mt-4 px-4 py-2 rounded-lg bg-surface-800 text-white hover:bg-surface-700 transition-colors flex items-center gap-2"
                >
                    <ArrowLeft size={18} />
                    Back to Applications
                </button>
            </div>
        )
    }

    return (
        <div className="space-y-6">
            {/* Success Message */}
            <AnimatePresence>
                {successMessage && (
                    <motion.div
                        initial={{ opacity: 0, y: -20 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -20 }}
                        className="p-4 rounded-xl bg-accent-emerald/10 border border-accent-emerald/20 flex items-center gap-3"
                    >
                        <CheckCircle2 size={20} className="text-accent-emerald" />
                        <p className="text-accent-emerald font-medium">{successMessage}</p>
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Error Message */}
            <AnimatePresence>
                {error && (
                    <motion.div
                        initial={{ opacity: 0, y: -20 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -20 }}
                        className="p-4 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center gap-3"
                    >
                        <AlertCircle size={20} className="text-red-400" />
                        <p className="text-red-400 font-medium">{error}</p>
                        <button
                            onClick={() => setError(null)}
                            className="ml-auto text-red-400 hover:text-red-300"
                        >
                            <X size={18} />
                        </button>
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3 mb-2">
                        <button
                            onClick={() => navigate('/applications')}
                            className="text-surface-400 hover:text-white transition-colors"
                        >
                            <ArrowLeft size={20} />
                        </button>
                        <h1 className="text-2xl font-display font-bold text-white">
                            Resume Editor
                        </h1>
                    </div>
                    {application && (
                        <p className="text-surface-400">
                            {application.jobPosting.title} - {application.jobPosting.companyName}
                        </p>
                    )}
                </div>

                <div className="flex items-center gap-3">
                    <button
                        onClick={handleRegenerate}
                        disabled={isGenerating}
                        className="px-4 py-2 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2 disabled:opacity-50"
                    >
                        <RefreshCw size={18} className={isGenerating ? 'animate-spin' : ''} />
                        Regenerate
                    </button>
                    <button
                        onClick={handlePreview}
                        className="px-4 py-2 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2"
                    >
                        <Eye size={18} />
                        Preview
                    </button>
                    <button
                        onClick={handleExportPDF}
                        disabled={isExporting}
                        className="px-4 py-2 rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors flex items-center gap-2 disabled:opacity-50"
                    >
                        {isExporting ? (
                            <Loader2 size={18} className="animate-spin" />
                        ) : (
                            <Download size={18} />
                        )}
                        Export PDF
                    </button>
                </div>
            </div>

            {/* Toolbar */}
            <div className="glass-card p-4">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    {/* View Mode Toggle */}
                    <div className="flex items-center gap-2">
                        <span className="text-surface-400 text-sm">View:</span>
                        <div className="flex bg-surface-800 rounded-lg p-1">
                            <button
                                onClick={() => setViewMode('split')}
                                className={`px-3 py-1.5 rounded-md text-sm transition-colors ${viewMode === 'split'
                                    ? 'bg-primary-500 text-white'
                                    : 'text-surface-400 hover:text-white'
                                    }`}
                            >
                                Split View
                            </button>
                            <button
                                onClick={() => setViewMode('tailored')}
                                className={`px-3 py-1.5 rounded-md text-sm transition-colors ${viewMode === 'tailored'
                                    ? 'bg-primary-500 text-white'
                                    : 'text-surface-400 hover:text-white'
                                    }`}
                            >
                                Tailored Only
                            </button>
                            <button
                                onClick={() => setViewMode('preview')}
                                className={`px-3 py-1.5 rounded-md text-sm transition-colors ${viewMode === 'preview'
                                    ? 'bg-primary-500 text-white'
                                    : 'text-surface-400 hover:text-white'
                                    }`}
                            >
                                Preview
                            </button>
                        </div>
                    </div>

                    {/* Highlight Changes Toggle */}
                    <div className="flex items-center gap-3">
                        <button
                            onClick={() => setShowChanges(!showChanges)}
                            className={`px-3 py-1.5 rounded-lg text-sm transition-colors flex items-center gap-2 ${showChanges
                                ? 'bg-primary-500/20 text-primary-400'
                                : 'bg-surface-800 text-surface-400 hover:text-white'
                                }`}
                        >
                            <Highlighter size={16} />
                            Highlight Changes
                        </button>
                    </div>

                    {/* Tone Slider */}
                    <div className="flex items-center gap-4">
                        <Sliders size={18} className="text-surface-400" />
                        <div className="flex items-center gap-3">
                            <span className="text-surface-400 text-sm">Professional</span>
                            <input
                                type="range"
                                min="0"
                                max="100"
                                value={tone}
                                onChange={(e) => handleToneChange(Number(e.target.value))}
                                className="w-32 accent-primary-500"
                                disabled={isGenerating}
                            />
                            <span className="text-surface-400 text-sm">Creative</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* AI Info */}
            {resumeResult && (
                <div className="p-4 rounded-xl bg-primary-500/10 border border-primary-500/20 flex items-start gap-3">
                    <Sparkles size={20} className="text-primary-400 mt-0.5" />
                    <div className="flex-1">
                        <p className="text-primary-300 font-medium text-sm mb-1">AI Optimization Complete</p>
                        <p className="text-surface-400 text-sm">
                            Your resume has been optimized for "{application?.jobPosting.title}" at {application?.jobPosting.companyName}.
                            Keywords added, achievements enhanced with measurable metrics.
                        </p>
                        <div className="flex items-center gap-4 mt-3">
                            <div className="flex items-center gap-2">
                                <div className="w-2 h-2 rounded-full bg-accent-emerald"></div>
                                <span className="text-xs text-surface-400">
                                    ATS Score: <span className="text-accent-emerald font-semibold">{resumeResult.atsScore}/100</span>
                                </span>
                            </div>
                            <div className="flex items-center gap-2">
                                <div className="w-2 h-2 rounded-full bg-primary-400"></div>
                                <span className="text-xs text-surface-400">
                                    {resumeResult.optimizedKeywords.length} keywords optimized
                                </span>
                            </div>
                            <div className="flex items-center gap-2">
                                <div className="w-2 h-2 rounded-full bg-accent-cyan"></div>
                                <span className="text-xs text-surface-400">
                                    {resumeResult.addedSkills.length} skills highlighted
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Editor */}
            {viewMode === 'preview' ? (
                <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="glass-card"
                >
                    <div className="p-4 border-b border-surface-700 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Eye size={18} className="text-primary-400" />
                            <span className="text-white font-medium">Preview</span>
                        </div>
                        <button
                            onClick={() => setViewMode('split')}
                            className="text-surface-400 hover:text-white transition-colors"
                        >
                            <X size={18} />
                        </button>
                    </div>
                    <div className="p-8 bg-white text-gray-900 max-h-[800px] overflow-auto">
                        {resumeResult?.htmlContent ? (
                            <div dangerouslySetInnerHTML={{ __html: resumeResult.htmlContent }} />
                        ) : (
                            <pre className="whitespace-pre-wrap font-sans leading-relaxed">
                                {editedResume || tailoredResume}
                            </pre>
                        )}
                    </div>
                </motion.div>
            ) : (
                <div className={`grid gap-6 ${viewMode === 'split' ? 'md:grid-cols-2' : 'md:grid-cols-1 max-w-4xl mx-auto'}`}>
                    {/* Original Resume */}
                    {viewMode === 'split' && (
                        <motion.div
                            initial={{ opacity: 0, x: -20 }}
                            animate={{ opacity: 1, x: 0 }}
                            className="glass-card"
                        >
                            <div className="p-4 border-b border-surface-700 flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    <FileText size={18} className="text-surface-400" />
                                    <span className="text-white font-medium">Original Resume</span>
                                </div>
                                <span className="text-xs text-surface-500 px-2 py-1 rounded-full bg-surface-800">
                                    Read-only
                                </span>
                            </div>
                            <div className="p-6 max-h-[700px] overflow-auto">
                                <pre className="text-surface-300 text-sm whitespace-pre-wrap font-sans leading-relaxed">
                                    {originalResume}
                                </pre>
                            </div>
                        </motion.div>
                    )}

                    {/* Tailored Resume */}
                    <motion.div
                        initial={{ opacity: 0, x: 20 }}
                        animate={{ opacity: 1, x: 0 }}
                        className="glass-card"
                    >
                        <div className="p-4 border-b border-surface-700 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <Sparkles size={18} className="text-primary-400" />
                                <span className="text-white font-medium">Tailored Resume</span>
                            </div>
                            <div className="flex items-center gap-2">
                                {!isEditing ? (
                                    <>
                                        <button
                                            onClick={handleEdit}
                                            className="px-3 py-1.5 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2 text-sm"
                                        >
                                            <Edit3 size={14} />
                                            Edit
                                        </button>
                                        <div className="flex items-center gap-1.5 px-2 py-1 rounded-full bg-accent-emerald/10">
                                            <Check size={14} className="text-accent-emerald" />
                                            <span className="text-xs text-accent-emerald">Optimized</span>
                                        </div>
                                    </>
                                ) : (
                                    <>
                                        <button
                                            onClick={handleCancelEdit}
                                            disabled={isSaving}
                                            className="px-3 py-1.5 rounded-lg bg-surface-800 text-surface-300 hover:text-white hover:bg-surface-700 transition-colors flex items-center gap-2 text-sm disabled:opacity-50"
                                        >
                                            <X size={14} />
                                            Cancel
                                        </button>
                                        <button
                                            onClick={handleSave}
                                            disabled={isSaving}
                                            className="px-3 py-1.5 rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors flex items-center gap-2 text-sm disabled:opacity-50"
                                        >
                                            {isSaving ? (
                                                <Loader2 size={14} className="animate-spin" />
                                            ) : (
                                                <Save size={14} />
                                            )}
                                            Save
                                        </button>
                                    </>
                                )}
                            </div>
                        </div>
                        <div className="p-6 max-h-[700px] overflow-auto">
                            {isEditing ? (
                                <textarea
                                    ref={editorRef}
                                    value={editedResume}
                                    onChange={(e) => setEditedResume(e.target.value)}
                                    className="w-full min-h-[600px] bg-surface-800/50 text-surface-200 text-sm font-mono p-4 rounded-lg border border-surface-700 focus:border-primary-500 focus:outline-none resize-none"
                                    placeholder="Edit your tailored resume here..."
                                />
                            ) : (
                                <div className="text-surface-300 text-sm whitespace-pre-wrap font-sans leading-relaxed">
                                    {showChanges && comparison ? (
                                        highlightChanges(tailoredResume, comparison.changes)
                                    ) : (
                                        tailoredResume
                                    )}
                                </div>
                            )}
                        </div>
                    </motion.div>
                </div>
            )}

            {/* Changes Summary */}
            {comparison && comparison.changes.length > 0 && (
                <div className="glass-card p-6">
                    <div className="flex items-center justify-between mb-4">
                        <h3 className="text-lg font-semibold text-white">Changes Made</h3>
                        <span className="text-sm text-surface-400">
                            Similarity: <span className="text-primary-400 font-semibold">{comparison.similarityScore}%</span>
                        </span>
                    </div>
                    <div className="space-y-3 max-h-[300px] overflow-auto">
                        {comparison.changes.map((change, index) => (
                            <div
                                key={index}
                                className="p-3 rounded-lg bg-surface-800/50 border border-surface-700"
                            >
                                <div className="flex items-start gap-3">
                                    <div className={`px-2 py-1 rounded text-xs font-medium ${change.changeType === 'Added' ? 'bg-accent-emerald/20 text-accent-emerald' :
                                            change.changeType === 'Modified' ? 'bg-primary-500/20 text-primary-400' :
                                                change.changeType === 'Highlighted' ? 'bg-accent-cyan/20 text-accent-cyan' :
                                                    'bg-surface-700 text-surface-400'
                                        }`}>
                                        {change.changeType}
                                    </div>
                                    <div className="flex-1">
                                        <div className="text-surface-400 text-xs mb-1">{change.section}</div>
                                        {change.originalText && (
                                            <div className="text-surface-500 text-sm line-through mb-1">
                                                {change.originalText.substring(0, 100)}
                                                {change.originalText.length > 100 && '...'}
                                            </div>
                                        )}
                                        <div className="text-surface-200 text-sm">
                                            {change.newText.substring(0, 100)}
                                            {change.newText.length > 100 && '...'}
                                        </div>
                                        {change.reason && (
                                            <div className="text-surface-400 text-xs mt-2 italic">
                                                {change.reason}
                                            </div>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Highlights */}
            {resumeResult && (
                <div className="glass-card p-6">
                    <h3 className="text-lg font-semibold text-white mb-4">Optimization Highlights</h3>
                    <div className="grid md:grid-cols-3 gap-4">
                        <div className="p-4 rounded-xl bg-surface-800/50">
                            <div className="text-2xl font-bold text-primary-400 mb-1">
                                {resumeResult.optimizedKeywords.length}
                            </div>
                            <div className="text-surface-300 text-sm font-medium mb-3">Keywords Added</div>
                            <div className="flex flex-wrap gap-2">
                                {resumeResult.optimizedKeywords.slice(0, 4).map((keyword, i) => (
                                    <span key={i} className="px-2 py-1 rounded-full bg-primary-500/10 text-primary-400 text-xs">
                                        {keyword}
                                    </span>
                                ))}
                                {resumeResult.optimizedKeywords.length > 4 && (
                                    <span className="px-2 py-1 rounded-full bg-surface-700 text-surface-400 text-xs">
                                        +{resumeResult.optimizedKeywords.length - 4} more
                                    </span>
                                )}
                            </div>
                        </div>

                        <div className="p-4 rounded-xl bg-surface-800/50">
                            <div className="text-2xl font-bold text-accent-emerald mb-1">
                                {resumeResult.addedSkills.length}
                            </div>
                            <div className="text-surface-300 text-sm font-medium mb-3">Skills Highlighted</div>
                            <div className="flex flex-wrap gap-2">
                                {resumeResult.addedSkills.slice(0, 4).map((skill, i) => (
                                    <span key={i} className="px-2 py-1 rounded-full bg-accent-emerald/10 text-accent-emerald text-xs">
                                        {skill}
                                    </span>
                                ))}
                                {resumeResult.addedSkills.length > 4 && (
                                    <span className="px-2 py-1 rounded-full bg-surface-700 text-surface-400 text-xs">
                                        +{resumeResult.addedSkills.length - 4} more
                                    </span>
                                )}
                            </div>
                        </div>

                        <div className="p-4 rounded-xl bg-surface-800/50">
                            <div className="text-2xl font-bold text-accent-cyan mb-1">
                                {resumeResult.highlightedExperiences.length}
                            </div>
                            <div className="text-surface-300 text-sm font-medium mb-3">Experiences Enhanced</div>
                            <div className="flex flex-wrap gap-2">
                                {resumeResult.highlightedExperiences.slice(0, 3).map((exp, i) => (
                                    <span key={i} className="px-2 py-1 rounded-full bg-accent-cyan/10 text-accent-cyan text-xs">
                                        {exp.substring(0, 20)}...
                                    </span>
                                ))}
                                {resumeResult.highlightedExperiences.length > 3 && (
                                    <span className="px-2 py-1 rounded-full bg-surface-700 text-surface-400 text-xs">
                                        +{resumeResult.highlightedExperiences.length - 3} more
                                    </span>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Cover Letter Section */}
            <div className="glass-card p-6">
                <h3 className="text-lg font-semibold text-white mb-4">Cover Letter</h3>
                <textarea
                    value={coverLetter}
                    onChange={(e) => setCoverLetter(e.target.value)}
                    className="w-full min-h-[200px] bg-surface-800/50 text-surface-200 text-sm p-4 rounded-lg border border-surface-700 focus:border-primary-500 focus:outline-none resize-none"
                    placeholder="Your personalized cover letter will appear here..."
                />
            </div>

            {/* Action Buttons */}
            <div className="flex flex-col sm:flex-row justify-center gap-4">
                <button
                    onClick={() => navigate('/applications')}
                    className="px-6 py-3 rounded-xl bg-surface-800 text-white hover:bg-surface-700 transition-colors flex items-center justify-center gap-2"
                >
                    <ArrowLeft size={18} />
                    Back to Applications
                </button>
                <button
                    onClick={handleSendApplication}
                    disabled={isSending || !tailoredResume}
                    className="btn-glow px-8 py-3 rounded-xl bg-gradient-to-r from-primary-500 to-accent-cyan text-white font-semibold text-lg flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    {isSending ? (
                        <>
                            <Loader2 size={20} className="animate-spin" />
                            Sending...
                        </>
                    ) : (
                        <>
                            <Send size={20} />
                            Approve & Send Application
                        </>
                    )}
                </button>
            </div>
        </div>
    )
}
