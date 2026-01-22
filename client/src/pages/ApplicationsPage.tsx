import { motion } from 'framer-motion'
import {
    FileText,
    Clock,
    CheckCircle,
    Eye,
    MessageSquare,
    XCircle,
    MoreVertical,
    Search,
    ExternalLink
} from 'lucide-react'
import { useState } from 'react'

interface Application {
    id: number
    company: string
    position: string
    status: 'pending' | 'sent' | 'viewed' | 'responded' | 'rejected'
    method: 'email' | 'linkedin'
    matchScore: number
    appliedAt: string
    updatedAt: string
}

const mockApplications: Application[] = [
    { id: 1, company: 'Trendyol', position: 'Senior Frontend Developer', status: 'viewed', method: 'linkedin', matchScore: 94, appliedAt: '2024-01-20', updatedAt: '2 saat önce' },
    { id: 2, company: 'Getir', position: 'Full Stack Developer', status: 'sent', method: 'email', matchScore: 91, appliedAt: '2024-01-19', updatedAt: '5 saat önce' },
    { id: 3, company: 'Insider', position: 'React Developer', status: 'responded', method: 'linkedin', matchScore: 88, appliedAt: '2024-01-18', updatedAt: '1 gün önce' },
    { id: 4, company: 'Peak Games', position: 'Software Engineer', status: 'pending', method: 'email', matchScore: 85, appliedAt: '2024-01-17', updatedAt: '2 gün önce' },
    { id: 5, company: 'Hepsiburada', position: 'Frontend Lead', status: 'rejected', method: 'linkedin', matchScore: 82, appliedAt: '2024-01-15', updatedAt: '4 gün önce' },
]

const statusConfig = {
    pending: { label: 'Beklemede', icon: Clock, color: 'text-amber-400', bg: 'bg-amber-400/10' },
    sent: { label: 'Gönderildi', icon: CheckCircle, color: 'text-primary-400', bg: 'bg-primary-400/10' },
    viewed: { label: 'Görüntülendi', icon: Eye, color: 'text-accent-cyan', bg: 'bg-accent-cyan/10' },
    responded: { label: 'Yanıt Alındı', icon: MessageSquare, color: 'text-accent-emerald', bg: 'bg-accent-emerald/10' },
    rejected: { label: 'Reddedildi', icon: XCircle, color: 'text-error', bg: 'bg-error/10' },
}

export default function ApplicationsPage() {
    const [filter, setFilter] = useState<string>('all')
    const [searchQuery, setSearchQuery] = useState('')

    const filteredApplications = mockApplications.filter(app => {
        if (filter !== 'all' && app.status !== filter) return false
        if (searchQuery && !app.company.toLowerCase().includes(searchQuery.toLowerCase()) &&
            !app.position.toLowerCase().includes(searchQuery.toLowerCase())) return false
        return true
    })

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-display font-bold text-white mb-1">
                        Başvurularım
                    </h1>
                    <p className="text-surface-400">
                        Toplam {mockApplications.length} başvuru
                    </p>
                </div>
            </div>

            {/* Filters */}
            <div className="glass-card p-4">
                <div className="flex flex-col md:flex-row md:items-center gap-4">
                    {/* Search */}
                    <div className="relative flex-1">
                        <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-surface-500" />
                        <input
                            type="text"
                            placeholder="Şirket veya pozisyon ara..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 rounded-lg bg-surface-800 text-white placeholder-surface-500 border border-surface-700 focus:border-primary-500 transition-colors"
                        />
                    </div>

                    {/* Status Filter */}
                    <div className="flex items-center gap-2 overflow-x-auto pb-2 md:pb-0">
                        <button
                            onClick={() => setFilter('all')}
                            className={`px-4 py-2 rounded-lg text-sm whitespace-nowrap transition-colors ${filter === 'all'
                                ? 'bg-primary-500 text-white'
                                : 'bg-surface-800 text-surface-400 hover:text-white'
                                }`}
                        >
                            Tümü
                        </button>
                        {Object.entries(statusConfig).map(([key, config]) => (
                            <button
                                key={key}
                                onClick={() => setFilter(key)}
                                className={`px-4 py-2 rounded-lg text-sm whitespace-nowrap transition-colors ${filter === key
                                    ? `${config.bg} ${config.color}`
                                    : 'bg-surface-800 text-surface-400 hover:text-white'
                                    }`}
                            >
                                {config.label}
                            </button>
                        ))}
                    </div>
                </div>
            </div>

            {/* Applications List */}
            <div className="space-y-4">
                {filteredApplications.map((app, index) => {
                    const status = statusConfig[app.status]
                    return (
                        <motion.div
                            key={app.id}
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.3, delay: index * 0.05 }}
                            className="glass-card p-6 group hover:border-primary-500/30 transition-all duration-300"
                        >
                            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                                {/* Left Section */}
                                <div className="flex items-start gap-4">
                                    <div className="w-14 h-14 rounded-xl bg-surface-700 flex items-center justify-center shrink-0">
                                        <span className="text-xl font-bold text-surface-400">
                                            {app.company.charAt(0)}
                                        </span>
                                    </div>

                                    <div>
                                        <h3 className="text-lg font-semibold text-white group-hover:text-primary-400 transition-colors">
                                            {app.position}
                                        </h3>
                                        <p className="text-surface-400 mb-2">{app.company}</p>

                                        <div className="flex items-center gap-4 text-sm">
                                            <div className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full ${status.bg}`}>
                                                <status.icon size={14} className={status.color} />
                                                <span className={status.color}>{status.label}</span>
                                            </div>

                                            <span className="text-surface-500">
                                                {app.method === 'linkedin' ? 'LinkedIn' : 'E-posta'}
                                            </span>

                                            <span className="text-surface-500">
                                                {app.updatedAt}
                                            </span>
                                        </div>
                                    </div>
                                </div>

                                {/* Right Section */}
                                <div className="flex items-center gap-4">
                                    {/* Match Score */}
                                    <div className="text-center">
                                        <div className="text-2xl font-bold text-accent-emerald">{app.matchScore}%</div>
                                        <div className="text-xs text-surface-500">Eşleşme</div>
                                    </div>

                                    {/* Actions */}
                                    <div className="flex items-center gap-2">
                                        <button className="p-2 rounded-lg bg-surface-800 text-surface-400 hover:text-white hover:bg-surface-700 transition-colors">
                                            <ExternalLink size={18} />
                                        </button>
                                        <button className="p-2 rounded-lg bg-surface-800 text-surface-400 hover:text-white hover:bg-surface-700 transition-colors">
                                            <FileText size={18} />
                                        </button>
                                        <button className="p-2 rounded-lg bg-surface-800 text-surface-400 hover:text-white hover:bg-surface-700 transition-colors">
                                            <MoreVertical size={18} />
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </motion.div>
                    )
                })}
            </div>

            {/* Empty State */}
            {filteredApplications.length === 0 && (
                <div className="text-center py-16">
                    <div className="w-16 h-16 mx-auto rounded-full bg-surface-800 flex items-center justify-center mb-4">
                        <FileText size={32} className="text-surface-500" />
                    </div>
                    <h3 className="text-lg font-semibold text-white mb-2">Başvuru Bulunamadı</h3>
                    <p className="text-surface-400">Arama kriterlerinize uygun başvuru yok.</p>
                </div>
            )}
        </div>
    )
}
