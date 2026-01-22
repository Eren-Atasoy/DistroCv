import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
    FileText,
    Sparkles,
    Download,
    Eye,
    Check,
    RefreshCw,
    Sliders
} from 'lucide-react'

const originalResume = `
# Eren Atasoy
Senior Frontend Developer

## İletişim
- Email: eren@email.com
- Telefon: +90 555 123 4567
- LinkedIn: linkedin.com/in/erenatasoy

## Deneyim

### Frontend Developer - ABC Tech (2021-Günümüz)
- Web uygulamaları geliştirdim
- React ve TypeScript kullandım
- Takım ile çalıştım

### Junior Developer - XYZ Startup (2019-2021)
- Web siteleri yaptım
- JavaScript öğrendim
- Projelere katkıda bulundum

## Beceriler
React, JavaScript, TypeScript, HTML, CSS, Node.js
`

const tailoredResume = `
# Eren Atasoy
Senior Frontend Developer | E-ticaret & Ölçeklenebilir Web Uygulamaları Uzmanı

## İletişim
- Email: eren@email.com
- Telefon: +90 555 123 4567
- LinkedIn: linkedin.com/in/erenatasoy

## Profesyonel Özet
5+ yıllık deneyime sahip, yüksek trafikli e-ticaret platformlarında uzmanlaşmış Frontend Developer. React, TypeScript ve Next.js ile milyon kullanıcılı uygulamalar geliştirme konusunda kanıtlanmış başarı.

## Deneyim

### Senior Frontend Developer - ABC Tech (2021-Günümüz)
- **React ve TypeScript** ile mikro-frontend mimarisi kullanarak **%40 performans artışı** sağladım
- E-ticaret checkout akışını optimize ederek **dönüşüm oranını %25 artırdım**
- 5 kişilik frontend ekibine teknik liderlik yaparak **Agile metodolojileri** uyguladım
- **Next.js SSR** implementasyonu ile SEO skorlarını %60 iyileştirdim

### Frontend Developer - XYZ Startup (2019-2021)
- **Sıfırdan ölçeklenebilir** web uygulaması geliştirerek 100K+ aktif kullanıcıya ulaştım
- **REST API entegrasyonları** ve state management (Redux) implementasyonları yaptım
- **CI/CD pipeline** kurulumuna katkıda bulunarak deployment süresini %50 azalttım

## Teknik Beceriler
**Frontend:** React, TypeScript, Next.js, TailwindCSS, Redux
**Backend:** Node.js, Express, PostgreSQL
**DevOps:** Docker, AWS, GitHub Actions
**Test:** Jest, React Testing Library, Cypress
`

export default function ResumeEditor() {
    const { id: _applicationId } = useParams()
    const [viewMode, setViewMode] = useState<'split' | 'tailored'>('split')
    const [tone, setTone] = useState(50)
    const [isGenerating, setIsGenerating] = useState(false)

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
