import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Users, Building2, Briefcase, Activity, Database, ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { adminApi, type CompanyStats } from '../../services/api'

export default function AdminDashboard() {
    const [companyStats, setCompanyStats] = useState<CompanyStats | null>(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        const fetchData = async () => {
            try {
                const stats = await adminApi.getStats()
                setCompanyStats(stats)
            } catch (error) {
                console.error('Failed to fetch admin stats', error)
            } finally {
                setLoading(false)
            }
        }
        fetchData()
    }, [])

    if (loading) {
        return <div className="p-8 text-center text-surface-400">Yükleniyor...</div>
    }

    const stats = [
        {
            label: 'Toplam Şirket',
            value: companyStats?.totalCompanies ?? 0,
            icon: Building2,
            color: 'from-red-500 to-red-600',
        },
        {
            label: 'Doğrulanmış',
            value: companyStats?.verifiedCompanies ?? 0,
            icon: Users,
            color: 'from-emerald-500 to-emerald-600',
        },
        {
            label: 'Doğrulanmamış',
            value: companyStats?.unverifiedCompanies ?? 0,
            icon: Activity,
            color: 'from-amber-500 to-amber-600',
        },
        {
            label: 'Bağlı İlanlar',
            value: companyStats?.totalJobPostingsLinked ?? 0,
            icon: Briefcase,
            color: 'from-blue-500 to-blue-600',
        },
    ]

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-display font-bold text-white mb-2">
                    Admin Panel
                </h1>
                <p className="text-surface-400">
                    Sistem yönetimi ve istatistikler
                </p>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {stats.map((stat, index) => (
                    <motion.div
                        key={index}
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.4, delay: index * 0.1 }}
                        className="glass-card p-6 group hover:border-red-500/30 transition-all duration-300"
                    >
                        <div className="flex items-start justify-between mb-4">
                            <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${stat.color} flex items-center justify-center group-hover:scale-110 transition-transform duration-300`}>
                                <stat.icon size={24} className="text-white" />
                            </div>
                        </div>
                        <div className="text-3xl font-bold text-white mb-1">{stat.value}</div>
                        <div className="text-surface-400 text-sm">{stat.label}</div>
                    </motion.div>
                ))}
            </div>

            {/* Quick Actions */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Link to="/admin/companies" className="glass-card p-6 group hover:border-red-500/30 transition-all duration-300">
                    <div className="flex items-center justify-between">
                        <div>
                            <Building2 size={24} className="text-red-400 mb-3" />
                            <h3 className="text-white font-semibold mb-1">Şirket Yönetimi</h3>
                            <p className="text-surface-400 text-sm">Şirketleri ekle, düzenle, doğrula</p>
                        </div>
                        <ArrowRight size={20} className="text-surface-500 group-hover:text-red-400 transition-colors" />
                    </div>
                </Link>

                <Link to="/admin/seed" className="glass-card p-6 group hover:border-red-500/30 transition-all duration-300">
                    <div className="flex items-center justify-between">
                        <div>
                            <Database size={24} className="text-red-400 mb-3" />
                            <h3 className="text-white font-semibold mb-1">İş İlanı Seed</h3>
                            <p className="text-surface-400 text-sm">Test iş ilanları oluştur</p>
                        </div>
                        <ArrowRight size={20} className="text-surface-500 group-hover:text-red-400 transition-colors" />
                    </div>
                </Link>
            </div>
        </div>
    )
}
