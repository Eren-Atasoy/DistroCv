import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import type { PanInfo } from 'framer-motion'
import {
    X,
    Heart,
    MapPin,
    Building2,
    DollarSign,
    Clock,
    Sparkles
} from 'lucide-react'

interface JobCard {
    id: number
    title: string
    company: string
    location: string
    salary: string
    matchScore: number
    matchReason: string
    requirements: string[]
    postedAt: string
    logo?: string
}

const mockJobs: JobCard[] = [
    {
        id: 1,
        title: 'Senior Frontend Developer',
        company: 'Trendyol',
        location: 'İstanbul, Türkiye',
        salary: '₺80.000 - ₺120.000',
        matchScore: 94,
        matchReason: 'React, TypeScript ve e-ticaret deneyiminiz bu pozisyon için mükemmel uyum sağlıyor.',
        requirements: ['React', 'TypeScript', 'Next.js', 'TailwindCSS'],
        postedAt: '2 gün önce',
    },
    {
        id: 2,
        title: 'Full Stack Engineer',
        company: 'Getir',
        location: 'İstanbul, Türkiye (Remote)',
        salary: '₺70.000 - ₺100.000',
        matchScore: 91,
        matchReason: 'Full stack becerileriniz ve startup deneyiminiz ideal.',
        requirements: ['Node.js', 'React', 'PostgreSQL', 'Docker'],
        postedAt: '3 gün önce',
    },
    {
        id: 3,
        title: 'React Developer',
        company: 'Insider',
        location: 'İstanbul, Türkiye (Hybrid)',
        salary: '₺60.000 - ₺90.000',
        matchScore: 88,
        matchReason: 'React uzmanlığınız ve SaaS geçmişiniz bu rol için uygun.',
        requirements: ['React', 'Redux', 'Jest', 'CSS-in-JS'],
        postedAt: '1 hafta önce',
    },
]

const rejectReasons = [
    'Maaş beklentimin altında',
    'Lokasyon uygun değil',
    'Teknoloji stack\'i eski',
    'Şirket kültürü uyuşmuyor',
    'Diğer',
]

export default function SwipeInterface() {
    const [jobs] = useState(mockJobs)
    const [currentIndex, setCurrentIndex] = useState(0)
    const [showRejectModal, setShowRejectModal] = useState(false)
    const [direction, setDirection] = useState<'left' | 'right' | null>(null)

    const currentJob = jobs[currentIndex]

    const handleSwipe = (swipeDirection: 'left' | 'right') => {
        setDirection(swipeDirection)

        setTimeout(() => {
            if (swipeDirection === 'left') {
                setShowRejectModal(true)
            } else {
                // Approve - move to next
                goToNext()
            }
        }, 300)
    }

    const goToNext = () => {
        setDirection(null)
        if (currentIndex < jobs.length - 1) {
            setCurrentIndex(prev => prev + 1)
        }
        setShowRejectModal(false)
    }

    const handleDragEnd = (_event: MouseEvent | TouchEvent | PointerEvent, info: PanInfo) => {
        const threshold = 150
        if (info.offset.x > threshold) {
            handleSwipe('right')
        } else if (info.offset.x < -threshold) {
            handleSwipe('left')
        }
    }

    if (currentIndex >= jobs.length) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[70vh] text-center">
                <motion.div
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    className="w-24 h-24 rounded-full bg-primary-500/20 flex items-center justify-center mb-6"
                >
                    <Sparkles size={48} className="text-primary-400" />
                </motion.div>
                <h2 className="text-2xl font-display font-bold text-white mb-2">
                    Tüm İşleri Gördün!
                </h2>
                <p className="text-surface-400 mb-6">
                    Yeni iş ilanları bulunduğunda seni bilgilendireceğiz.
                </p>
                <button
                    onClick={() => setCurrentIndex(0)}
                    className="px-6 py-3 rounded-xl bg-primary-500 text-white font-medium hover:bg-primary-600 transition-colors"
                >
                    Başa Dön
                </button>
            </div>
        )
    }

    return (
        <div className="max-w-lg mx-auto">
            {/* Progress */}
            <div className="mb-6">
                <div className="flex items-center justify-between text-sm text-surface-400 mb-2">
                    <span>{currentIndex + 1} / {jobs.length} İş</span>
                    <span className="flex items-center gap-1">
                        <Sparkles size={14} className="text-primary-400" />
                        Bugün 12 yeni eşleşme
                    </span>
                </div>
                <div className="h-1 bg-surface-800 rounded-full overflow-hidden">
                    <div
                        className="h-full bg-gradient-to-r from-primary-500 to-accent-cyan transition-all duration-300"
                        style={{ width: `${((currentIndex + 1) / jobs.length) * 100}%` }}
                    />
                </div>
            </div>

            {/* Card Stack */}
            <div className="relative h-[600px]">
                <AnimatePresence>
                    {currentJob && (
                        <motion.div
                            key={currentJob.id}
                            initial={{ scale: 0.95, opacity: 0 }}
                            animate={{
                                scale: 1,
                                opacity: 1,
                                x: direction === 'left' ? -500 : direction === 'right' ? 500 : 0,
                                rotate: direction === 'left' ? -15 : direction === 'right' ? 15 : 0,
                            }}
                            exit={{ scale: 0.95, opacity: 0 }}
                            transition={{ duration: 0.3 }}
                            drag="x"
                            dragConstraints={{ left: 0, right: 0 }}
                            dragElastic={1}
                            onDragEnd={handleDragEnd}
                            className="absolute inset-0 glass-card p-6 cursor-grab active:cursor-grabbing"
                        >
                            {/* Match Score Badge */}
                            <div className="absolute -top-3 -right-3 w-16 h-16">
                                <div className="relative w-full h-full">
                                    <svg className="w-full h-full -rotate-90">
                                        <circle
                                            cx="32"
                                            cy="32"
                                            r="28"
                                            fill="none"
                                            stroke="rgba(255,255,255,0.1)"
                                            strokeWidth="4"
                                        />
                                        <circle
                                            cx="32"
                                            cy="32"
                                            r="28"
                                            fill="none"
                                            stroke="url(#scoreGradient)"
                                            strokeWidth="4"
                                            strokeDasharray={`${currentJob.matchScore * 1.76} 176`}
                                            strokeLinecap="round"
                                        />
                                        <defs>
                                            <linearGradient id="scoreGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                                                <stop offset="0%" stopColor="#10b981" />
                                                <stop offset="100%" stopColor="#06b6d4" />
                                            </linearGradient>
                                        </defs>
                                    </svg>
                                    <div className="absolute inset-0 flex items-center justify-center">
                                        <span className="text-white font-bold text-sm">{currentJob.matchScore}%</span>
                                    </div>
                                </div>
                            </div>

                            {/* Company Logo */}
                            <div className="w-16 h-16 rounded-xl bg-surface-700 flex items-center justify-center mb-4">
                                <Building2 size={28} className="text-surface-400" />
                            </div>

                            {/* Job Info */}
                            <h2 className="text-2xl font-display font-bold text-white mb-2">
                                {currentJob.title}
                            </h2>
                            <p className="text-primary-400 font-medium text-lg mb-4">{currentJob.company}</p>

                            <div className="space-y-3 mb-6">
                                <div className="flex items-center gap-2 text-surface-300">
                                    <MapPin size={18} className="text-surface-500" />
                                    {currentJob.location}
                                </div>
                                <div className="flex items-center gap-2 text-surface-300">
                                    <DollarSign size={18} className="text-surface-500" />
                                    {currentJob.salary}
                                </div>
                                <div className="flex items-center gap-2 text-surface-300">
                                    <Clock size={18} className="text-surface-500" />
                                    {currentJob.postedAt}
                                </div>
                            </div>

                            {/* Requirements */}
                            <div className="flex flex-wrap gap-2 mb-6">
                                {currentJob.requirements.map((req, index) => (
                                    <span
                                        key={index}
                                        className="px-3 py-1 rounded-full bg-primary-500/10 text-primary-400 text-sm"
                                    >
                                        {req}
                                    </span>
                                ))}
                            </div>

                            {/* Match Reasoning */}
                            <div className="p-4 rounded-xl bg-accent-emerald/10 border border-accent-emerald/20">
                                <div className="flex items-start gap-3">
                                    <Sparkles size={20} className="text-accent-emerald mt-0.5" />
                                    <div>
                                        <p className="text-accent-emerald font-medium text-sm mb-1">Neden Eşleştin?</p>
                                        <p className="text-surface-300 text-sm">{currentJob.matchReason}</p>
                                    </div>
                                </div>
                            </div>

                            {/* Swipe Hints */}
                            <div className="absolute bottom-6 left-6 right-6 flex items-center justify-between text-surface-500 text-sm">
                                <span className="flex items-center gap-1">
                                    <X size={14} /> Sola kaydır: Reddet
                                </span>
                                <span className="flex items-center gap-1">
                                    Sağa kaydır: Başvur <Heart size={14} />
                                </span>
                            </div>
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-center gap-8 mt-6">
                <button
                    onClick={() => handleSwipe('left')}
                    className="w-16 h-16 rounded-full bg-surface-800 border-2 border-surface-700 flex items-center justify-center text-error hover:border-error hover:bg-error/10 transition-all duration-300 group"
                >
                    <X size={28} className="group-hover:scale-110 transition-transform" />
                </button>

                <button
                    onClick={() => handleSwipe('right')}
                    className="w-20 h-20 rounded-full bg-gradient-to-br from-accent-emerald to-accent-cyan flex items-center justify-center text-white shadow-glow hover:shadow-glow-lg transition-all duration-300 group"
                >
                    <Heart size={32} className="group-hover:scale-110 transition-transform" />
                </button>
            </div>

            {/* Reject Modal */}
            <AnimatePresence>
                {showRejectModal && (
                    <motion.div
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4"
                        onClick={() => { setShowRejectModal(false); goToNext(); }}
                    >
                        <motion.div
                            initial={{ scale: 0.9, opacity: 0 }}
                            animate={{ scale: 1, opacity: 1 }}
                            exit={{ scale: 0.9, opacity: 0 }}
                            className="glass-card p-6 max-w-md w-full"
                            onClick={e => e.stopPropagation()}
                        >
                            <h3 className="text-xl font-semibold text-white mb-2">
                                Neden Reddettin?
                            </h3>
                            <p className="text-surface-400 text-sm mb-6">
                                Geri bildirimin eşleştirme kalitesini artırmamıza yardımcı olur.
                            </p>

                            <div className="space-y-3 mb-6">
                                {rejectReasons.map((reason, index) => (
                                    <button
                                        key={index}
                                        onClick={() => goToNext()}
                                        className="w-full p-3 rounded-xl bg-surface-800 text-surface-300 text-left hover:bg-surface-700 hover:text-white transition-colors"
                                    >
                                        {reason}
                                    </button>
                                ))}
                            </div>

                            <button
                                onClick={() => { setShowRejectModal(false); goToNext(); }}
                                className="w-full py-3 rounded-xl bg-surface-700 text-surface-400 hover:text-white transition-colors"
                            >
                                Atla
                            </button>
                        </motion.div>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    )
}
