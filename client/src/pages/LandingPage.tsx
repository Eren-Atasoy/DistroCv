import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { useDropzone } from 'react-dropzone'
import {
    Upload,
    Sparkles,
    Zap,
    Shield,
    Target,
    Bot,
    ArrowRight,
    CheckCircle,
    FileText,
    Briefcase
} from 'lucide-react'
import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import LanguageSwitcher from '../components/LanguageSwitcher'

const useFeatures = () => {
    const { t } = useTranslation()
    return [
        {
            icon: Target,
            title: t('landing.features.smartMatching'),
            description: t('landing.features.smartMatchingDesc'),
            gradient: 'from-primary-500 to-primary-600',
        },
        {
            icon: Sparkles,
            title: 'Dinamik CV Optimizasyonu',
            description: 'Her iş ilanına özel, anahtar kelime optimize edilmiş CV oluşturuyoruz.',
            gradient: 'from-accent-cyan to-primary-500',
        },
        {
            icon: Bot,
            title: t('landing.features.autoApply'),
            description: t('landing.features.autoApplyDesc'),
            gradient: 'from-accent-violet to-accent-fuchsia',
        },
        {
            icon: Shield,
            title: 'Anti-Bot Koruma',
            description: 'Akıllı hız sınırlama ile hesap güvenliğinizi koruyoruz.',
            gradient: 'from-accent-emerald to-accent-cyan',
        },
    ]
}

const useStats = () => {
    const { i18n } = useTranslation()
    const isEn = i18n.language === 'en'
    return [
        { value: '1000+', label: isEn ? 'Daily Jobs Scanned' : 'Günlük Taranan İlan' },
        { value: isEn ? '87%' : '%87', label: isEn ? 'Average Match' : 'Ortalama Eşleşme' },
        { value: isEn ? '5min' : '5dk', label: isEn ? 'Avg. Application Time' : 'Ortalama Başvuru Süresi' },
        { value: isEn ? '+35%' : '%35', label: isEn ? 'Response Rate Increase' : 'Yanıt Oranı Artışı' },
    ]
}

export default function LandingPage() {
    const navigate = useNavigate()
    const { t, i18n } = useTranslation()
    const [isUploading, setIsUploading] = useState(false)
    const [uploadedFile, setUploadedFile] = useState<File | null>(null)
    const features = useFeatures()
    const stats = useStats()
    const isEn = i18n.language === 'en'

    const onDrop = useCallback((acceptedFiles: File[]) => {
        const file = acceptedFiles[0]
        if (file) {
            setUploadedFile(file)
            setIsUploading(true)
            setTimeout(() => {
                setIsUploading(false)
                navigate('/dashboard')
            }, 2000)
        }
    }, [navigate])

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf'],
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
            'text/plain': ['.txt'],
        },
        maxFiles: 1,
        maxSize: 10 * 1024 * 1024,
    })

    return (
        <div className="min-h-screen bg-gradient-hero overflow-hidden">
            <div className="absolute inset-0 overflow-hidden">
                <div className="absolute top-1/4 -left-1/4 w-96 h-96 bg-primary-500/20 rounded-full blur-3xl animate-pulse-slow" />
                <div className="absolute bottom-1/4 -right-1/4 w-96 h-96 bg-accent-cyan/20 rounded-full blur-3xl animate-pulse-slow delay-1000" />
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-accent-violet/10 rounded-full blur-3xl" />
            </div>

            <header className="relative z-10 container mx-auto px-6 py-6">
                <nav className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary-500 to-accent-cyan flex items-center justify-center">
                            <span className="text-white font-bold text-lg">D</span>
                        </div>
                        <span className="font-display font-bold text-2xl text-white">DistroCV</span>
                    </div>
                    <div className="flex items-center gap-4">
                        <button className="text-surface-300 hover:text-white transition-colors">
                            {isEn ? 'Features' : 'Özellikler'}
                        </button>
                        <button className="text-surface-300 hover:text-white transition-colors">
                            {isEn ? 'Pricing' : 'Fiyatlandırma'}
                        </button>
                        <LanguageSwitcher variant="minimal" />
                        <button
                            onClick={() => navigate('/dashboard')}
                            className="px-4 py-2 rounded-lg bg-white/10 text-white hover:bg-white/20 transition-all"
                        >
                            {isEn ? 'Login' : 'Giriş Yap'}
                        </button>
                    </div>
                </nav>
            </header>

            <main className="relative z-10 container mx-auto px-6 pt-16 pb-24">
                <div className="max-w-4xl mx-auto text-center mb-16">
                    <motion.div
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.6 }}
                    >
                        <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary-500/20 text-primary-300 text-sm font-medium mb-6">
                            <Sparkles size={16} />
                            {isEn ? 'AI-Powered Career Assistant' : 'AI Destekli Kariyer Asistanı'}
                        </span>
                    </motion.div>

                    <motion.h1
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.6, delay: 0.1 }}
                        className="font-display font-bold text-5xl md:text-6xl lg:text-7xl text-white mb-6 leading-tight"
                    >
                        {isEn ? 'Applying for Jobs' : 'İş Başvurusu Yapmak'}
                        <br />
                        <span className="gradient-text">{isEn ? 'Has Never Been Easier' : 'Hiç Bu Kadar Kolay Olmamıştı'}</span>
                    </motion.h1>

                    <motion.p
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.6, delay: 0.2 }}
                        className="text-xl text-surface-300 mb-12 max-w-2xl mx-auto"
                    >
                        {t('landing.heroSubtitle')}
                    </motion.p>
                </div>

                <motion.div
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6, delay: 0.3 }}
                    className="max-w-2xl mx-auto mb-24"
                >
                    <div
                        {...getRootProps()}
                        className={`
              dropzone p-12 text-center cursor-pointer
              ${isDragActive ? 'dragover' : ''}
              ${isUploading ? 'pointer-events-none' : ''}
            `}
                    >
                        <input {...getInputProps()} />

                        {isUploading ? (
                            <div className="space-y-4">
                                <div className="w-16 h-16 mx-auto rounded-full bg-primary-500/20 flex items-center justify-center">
                                    <div className="w-8 h-8 border-2 border-primary-500 border-t-transparent rounded-full animate-spin" />
                                </div>
                                <p className="text-white font-medium">{isEn ? 'Analyzing resume...' : 'CV analiz ediliyor...'}</p>
                                <p className="text-surface-400 text-sm">{uploadedFile?.name}</p>
                            </div>
                        ) : uploadedFile ? (
                            <div className="space-y-4">
                                <div className="w-16 h-16 mx-auto rounded-full bg-accent-emerald/20 flex items-center justify-center">
                                    <CheckCircle size={32} className="text-accent-emerald" />
                                </div>
                                <p className="text-white font-medium">{isEn ? 'Resume uploaded!' : 'CV yüklendi!'}</p>
                                <p className="text-surface-400 text-sm">{uploadedFile.name}</p>
                            </div>
                        ) : (
                            <>
                                <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-primary-500/20 to-accent-cyan/20 flex items-center justify-center">
                                    <Upload size={36} className="text-primary-400" />
                                </div>
                                <h3 className="text-xl font-semibold text-white mb-2">
                                    {isDragActive 
                                        ? (isEn ? 'Drop your resume here!' : "CV'yi buraya bırak!") 
                                        : t('landing.uploadResume')}
                                </h3>
                                <p className="text-surface-400 mb-4">
                                    {t('landing.supportedFormats')}
                                </p>
                                <button className="btn-glow px-6 py-3 rounded-xl bg-primary-500 text-white font-medium">
                                    {isEn ? 'Select File' : 'Dosya Seç'}
                                </button>
                            </>
                        )}
                    </div>
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6, delay: 0.4 }}
                    className="grid grid-cols-2 md:grid-cols-4 gap-6 max-w-4xl mx-auto mb-24"
                >
                    {stats.map((stat, index) => (
                        <div key={index} className="glass-card p-6 text-center">
                            <div className="text-3xl md:text-4xl font-bold gradient-text mb-2">{stat.value}</div>
                            <div className="text-surface-400 text-sm">{stat.label}</div>
                        </div>
                    ))}
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6, delay: 0.5 }}
                >
                    <h2 className="text-3xl font-display font-bold text-white text-center mb-12">
                        {isEn ? 'Why DistroCV?' : 'Neden DistroCV?'}
                    </h2>
                    <div className="grid md:grid-cols-2 gap-6 max-w-5xl mx-auto">
                        {features.map((feature, index) => (
                            <motion.div
                                key={index}
                                initial={{ opacity: 0, y: 20 }}
                                animate={{ opacity: 1, y: 0 }}
                                transition={{ duration: 0.5, delay: 0.6 + index * 0.1 }}
                                className="glass-card p-8 group hover:border-primary-500/50 transition-all duration-300"
                            >
                                <div className={`w-14 h-14 rounded-xl bg-gradient-to-br ${feature.gradient} flex items-center justify-center mb-6 group-hover:scale-110 transition-transform duration-300`}>
                                    <feature.icon size={28} className="text-white" />
                                </div>
                                <h3 className="text-xl font-semibold text-white mb-3">{feature.title}</h3>
                                <p className="text-surface-400 leading-relaxed">{feature.description}</p>
                            </motion.div>
                        ))}
                    </div>
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6, delay: 0.8 }}
                    className="mt-32"
                >
                    <h2 className="text-3xl font-display font-bold text-white text-center mb-4">
                        {isEn ? 'How It Works?' : 'Nasıl Çalışır?'}
                    </h2>
                    <p className="text-surface-400 text-center mb-12 max-w-2xl mx-auto">
                        {isEn ? 'Apply to your dream job in 4 simple steps' : '4 basit adımda hayalinizdeki işe başvurun'}
                    </p>

                    <div className="flex flex-col md:flex-row items-center justify-center gap-6 md:gap-0 max-w-5xl mx-auto">
                        {(isEn ? [
                            { icon: FileText, title: 'Upload Resume', desc: 'Upload your resume or import from LinkedIn' },
                            { icon: Target, title: 'See Matches', desc: 'AI shows you the best matching jobs' },
                            { icon: Sparkles, title: 'Approve', desc: 'Approve the jobs you like' },
                            { icon: Briefcase, title: 'Apply', desc: 'Apply with one click' },
                        ] : [
                            { icon: FileText, title: 'CV Yükle', desc: "CV'ni yükle ya da LinkedIn'den çek" },
                            { icon: Target, title: 'Eşleşmeleri Gör', desc: 'AI en uygun işleri sana göstersin' },
                            { icon: Sparkles, title: 'Onay Ver', desc: 'Beğendiğin işleri onayla' },
                            { icon: Briefcase, title: 'Başvur', desc: 'Tek tıkla otomatik başvur' },
                        ]).map((step, index) => (
                            <div key={index} className="flex items-center">
                                <div className="flex flex-col items-center text-center">
                                    <div className="w-16 h-16 rounded-2xl bg-surface-800 border border-surface-700 flex items-center justify-center mb-4 group-hover:border-primary-500 transition-colors">
                                        <step.icon size={28} className="text-primary-400" />
                                    </div>
                                    <h4 className="text-white font-semibold mb-1">{step.title}</h4>
                                    <p className="text-surface-400 text-sm max-w-[150px]">{step.desc}</p>
                                </div>
                                {index < 3 && (
                                    <ArrowRight size={24} className="text-surface-600 mx-6 hidden md:block" />
                                )}
                            </div>
                        ))}
                    </div>
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6, delay: 1 }}
                    className="mt-32 text-center"
                >
                    <div className="glass-card max-w-3xl mx-auto p-12">
                        <h2 className="text-3xl font-display font-bold text-white mb-4">
                            {isEn ? 'Start Your Career Journey' : 'Kariyer Yolculuğunuza Başlayın'}
                        </h2>
                        <p className="text-surface-400 mb-8">
                            {isEn ? 'Start for free, upgrade anytime.' : 'Ücretsiz başlayın, istediğiniz zaman yükseltin.'}
                        </p>
                        <button
                            onClick={() => document.querySelector('.dropzone')?.scrollIntoView({ behavior: 'smooth' })}
                            className="btn-glow px-8 py-4 rounded-xl bg-gradient-to-r from-primary-500 to-accent-cyan text-white font-semibold text-lg inline-flex items-center gap-2"
                        >
                            {t('landing.getStarted')}
                            <Zap size={20} />
                        </button>
                    </div>
                </motion.div>
            </main>

            <footer className="relative z-10 border-t border-surface-800 py-8">
                <div className="container mx-auto px-6 text-center text-surface-500 text-sm">
                    <p>© 2024 DistroCV. {isEn ? 'All rights reserved.' : 'Tüm hakları saklıdır.'}</p>
                </div>
            </footer>
        </div>
    )
}
