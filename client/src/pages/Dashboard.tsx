import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import {
    FileText,
    Send,
    Eye,
    MessageSquare,
    TrendingUp,
    Briefcase,
    Target,
    Activity
} from 'lucide-react'
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr'
import {
    AreaChart,
    Area,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
    BarChart,
    Bar
} from 'recharts'

// Types
interface Stat {
    label: string
    value: number
    icon: any
    change: string
    color: string
}

interface Application {
    id: number
    company: string
    position: string
    status: string
    statusColor: string
    date: string
}

interface MatchingJob {
    id: number
    title: string
    company: string
    score: number
}

// Mock Data (Initial)
const initialStats: Stat[] = [
    {
        label: 'Toplam Başvuru',
        value: 24,
        icon: FileText,
        change: '+5 bu hafta',
        color: 'from-primary-500 to-primary-600'
    },
    {
        label: 'Gönderilen',
        value: 18,
        icon: Send,
        change: '75%',
        color: 'from-accent-cyan to-primary-500'
    },
    {
        label: 'Görüntülenen',
        value: 12,
        icon: Eye,
        change: '67%',
        color: 'from-accent-violet to-accent-fuchsia'
    },
    {
        label: 'Yanıt Alınan',
        value: 5,
        icon: MessageSquare,
        change: '28%',
        color: 'from-accent-emerald to-accent-cyan'
    },
]

const recentApplicationsData: Application[] = [
    {
        id: 1,
        company: 'Trendyol',
        position: 'Senior Frontend Developer',
        status: 'Görüntülendi',
        statusColor: 'text-accent-cyan',
        date: '2 saat önce'
    },
    {
        id: 2,
        company: 'Getir',
        position: 'Full Stack Developer',
        status: 'Gönderildi',
        statusColor: 'text-primary-400',
        date: '5 saat önce'
    },
    {
        id: 3,
        company: 'Insider',
        position: 'React Developer',
        status: 'Yanıt Alındı',
        statusColor: 'text-accent-emerald',
        date: '1 gün önce'
    },
    {
        id: 4,
        company: 'Peak Games',
        position: 'Software Engineer',
        status: 'Beklemede',
        statusColor: 'text-amber-400',
        date: '2 gün önce'
    },
]

const matchingJobsData: MatchingJob[] = [
    { id: 1, title: 'Senior React Developer', company: 'Hepsiburada', score: 94 },
    { id: 2, title: 'Frontend Lead', company: 'N11', score: 91 },
    { id: 3, title: 'Full Stack Engineer', company: 'Yemeksepeti', score: 88 },
]

const chartData = [
    { name: 'Pzt', basvuru: 4 },
    { name: 'Sal', basvuru: 3 },
    { name: 'Çar', basvuru: 7 },
    { name: 'Per', basvuru: 5 },
    { name: 'Cum', basvuru: 8 },
    { name: 'Cmt', basvuru: 2 },
    { name: 'Paz', basvuru: 4 },
]

export default function Dashboard() {
    const [stats, setStats] = useState<Stat[]>(initialStats)
    const [recentApplications, setRecentApplications] = useState<Application[]>(recentApplicationsData)
    const [connection, setConnection] = useState<HubConnection | null>(null)
    const [isConnected, setIsConnected] = useState(false)

    useEffect(() => {
        // Initialize SignalR connection
        const newConnection = new HubConnectionBuilder()
            .withUrl("http://localhost:5001/hubs/notifications") // Adjust API URL as needed
            .withAutomaticReconnect()
            .build()

        setConnection(newConnection)
    }, [])

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('SignalR Connected!');
                    setIsConnected(true);

                    // Subscribe to stats updates
                    connection.on("ReceiveStatsUpdate", (type: string, data: any) => {
                        console.log('Received stats update:', type, data);
                        if (type === 'DashboardStats') {
                            // Update stats logic here
                            // map backend DTO to frontend Stat interface
                        }
                    });

                    connection.on("ReceiveNotification", (notification: any) => {
                        console.log('New notification:', notification);
                        // Show toast or update list
                    });
                })
                .catch(e => console.error('Connection failed: ', e));
        }

        return () => {
            if (connection) {
                connection.off("ReceiveStatsUpdate");
                connection.off("ReceiveNotification");
            }
        }
    }, [connection])

    return (
        <div className="space-y-8">
            <div className="flex justify-between items-start">
                <div>
                    <h1 className="text-3xl font-display font-bold text-white mb-2">
                        Hoş Geldin, Eren 👋
                    </h1>
                    <p className="text-surface-400">
                        İşte kariyer yolculuğunun özeti
                    </p>
                </div>
                <div className="flex items-center gap-2 px-3 py-1 rounded-full bg-surface-800 border border-surface-700">
                    <div className={`w-2 h-2 rounded-full ${isConnected ? 'bg-accent-emerald' : 'bg-red-500'} animate-pulse`} />
                    <span className="text-xs text-surface-400">
                        {isConnected ? 'Canlı Bağlantı' : 'Bağlantı Yok'}
                    </span>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {stats.map((stat, index) => (
                    <motion.div
                        key={index}
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.4, delay: index * 0.1 }}
                        className="glass-card p-6 group hover:border-primary-500/30 transition-all duration-300"
                    >
                        <div className="flex items-start justify-between mb-4">
                            <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${stat.color} flex items-center justify-center group-hover:scale-110 transition-transform duration-300`}>
                                <stat.icon size={24} className="text-white" />
                            </div>
                            <span className="text-xs text-accent-emerald bg-accent-emerald/10 px-2 py-1 rounded-full">
                                {stat.change}
                            </span>
                        </div>
                        <div className="text-3xl font-bold text-white mb-1">{stat.value}</div>
                        <div className="text-surface-400 text-sm">{stat.label}</div>
                    </motion.div>
                ))}
            </div>

            {/* Charts Section */}
            <div className="grid lg:grid-cols-2 gap-6">
                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4, delay: 0.35 }}
                    className="glass-card p-6"
                >
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-xl font-semibold text-white flex items-center gap-2">
                            <Activity size={20} className="text-accent-violet" />
                            Haftalık Aktivite
                        </h2>
                    </div>
                    <div className="h-[250px] w-full">
                        <ResponsiveContainer width="100%" height="100%">
                            <AreaChart data={chartData}>
                                <defs>
                                    <linearGradient id="colorBasvuru" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.3} />
                                        <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0} />
                                    </linearGradient>
                                </defs>
                                <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                                <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
                                <YAxis stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
                                <Tooltip
                                    contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '8px' }}
                                    itemStyle={{ color: '#fff' }}
                                />
                                <Area type="monotone" dataKey="basvuru" stroke="#8b5cf6" strokeWidth={2} fillOpacity={1} fill="url(#colorBasvuru)" />
                            </AreaChart>
                        </ResponsiveContainer>
                    </div>
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4, delay: 0.4 }}
                    className="glass-card p-6"
                >
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-xl font-semibold text-white flex items-center gap-2">
                            <TrendingUp size={20} className="text-accent-cyan" />
                            Başvuru Durumu
                        </h2>
                    </div>
                    <div className="h-[250px] w-full">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={[
                                { name: 'Bekleyen', value: 12 },
                                { name: 'Gönderilen', value: 18 },
                                { name: 'Görüntülenen', value: 8 },
                                { name: 'Geri Dönüş', value: 5 },
                            ]}>
                                <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                                <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
                                <YAxis stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
                                <Tooltip
                                    cursor={{ fill: '#334155', opacity: 0.2 }}
                                    contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '8px' }}
                                    itemStyle={{ color: '#fff' }}
                                />
                                <Bar dataKey="value" fill="#06b6d4" radius={[4, 4, 0, 0]} barSize={40} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                </motion.div>
            </div>

            <div className="grid lg:grid-cols-3 gap-6">
                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4, delay: 0.4 }}
                    className="lg:col-span-2 glass-card p-6"
                >
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-xl font-semibold text-white">Son Başvurular</h2>
                        <button className="text-primary-400 text-sm hover:text-primary-300 transition-colors">
                            Tümünü Gör →
                        </button>
                    </div>

                    <div className="space-y-4">
                        {recentApplications.map((app) => (
                            <div
                                key={app.id}
                                className="flex items-center justify-between p-4 rounded-xl bg-surface-800/50 hover:bg-surface-800 transition-colors group"
                            >
                                <div className="flex items-center gap-4">
                                    <div className="w-12 h-12 rounded-xl bg-surface-700 flex items-center justify-center">
                                        <Briefcase size={20} className="text-surface-400" />
                                    </div>
                                    <div>
                                        <h3 className="text-white font-medium group-hover:text-primary-400 transition-colors">
                                            {app.position}
                                        </h3>
                                        <p className="text-surface-400 text-sm">{app.company}</p>
                                    </div>
                                </div>
                                <div className="text-right">
                                    <span className={`text-sm font-medium ${app.statusColor}`}>
                                        {app.status}
                                    </span>
                                    <p className="text-surface-500 text-xs mt-1">{app.date}</p>
                                </div>
                            </div>
                        ))}
                    </div>
                </motion.div>

                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4, delay: 0.5 }}
                    className="glass-card p-6"
                >
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-xl font-semibold text-white">Eşleşen İşler</h2>
                        <Target size={20} className="text-primary-400" />
                    </div>

                    <div className="space-y-4">
                        {matchingJobsData.map((job) => (
                            <div
                                key={job.id}
                                className="p-4 rounded-xl bg-surface-800/50 hover:bg-surface-800 transition-colors group cursor-pointer"
                            >
                                <div className="flex items-center justify-between mb-2">
                                    <h3 className="text-white font-medium text-sm group-hover:text-primary-400 transition-colors">
                                        {job.title}
                                    </h3>
                                    <div className="flex items-center gap-1">
                                        <span className="text-accent-emerald font-bold">{job.score}%</span>
                                    </div>
                                </div>
                                <p className="text-surface-400 text-sm">{job.company}</p>
                                <div className="mt-3 h-1.5 bg-surface-700 rounded-full overflow-hidden">
                                    <div
                                        className="h-full bg-gradient-to-r from-accent-emerald to-accent-cyan rounded-full transition-all duration-500"
                                        style={{ width: `${job.score}%` }}
                                    />
                                </div>
                            </div>
                        ))}
                    </div>

                    <button className="w-full mt-4 py-3 rounded-xl bg-primary-500/10 text-primary-400 font-medium hover:bg-primary-500/20 transition-colors">
                        Keşfet →
                    </button>
                </motion.div>
            </div>

            {/* Action Buttons */}
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.4, delay: 0.6 }}
                className="grid grid-cols-1 md:grid-cols-3 gap-4"
            >
                <button className="glass-card p-6 text-left group hover:border-primary-500/30 transition-all duration-300">
                    <Target size={24} className="text-primary-400 mb-4 group-hover:scale-110 transition-transform" />
                    <h3 className="text-white font-medium mb-1">İş Keşfet</h3>
                    <p className="text-surface-400 text-sm">Yeni eşleşmeleri gör</p>
                </button>

                <button className="glass-card p-6 text-left group hover:border-accent-cyan/30 transition-all duration-300">
                    <FileText size={24} className="text-accent-cyan mb-4 group-hover:scale-110 transition-transform" />
                    <h3 className="text-white font-medium mb-1">CV Güncelle</h3>
                    <p className="text-surface-400 text-sm">Profilini yenile</p>
                </button>

                <button className="glass-card p-6 text-left group hover:border-accent-violet/30 transition-all duration-300">
                    <TrendingUp size={24} className="text-accent-violet mb-4 group-hover:scale-110 transition-transform" />
                    <h3 className="text-white font-medium mb-1">İstatistikler</h3>
                    <p className="text-surface-400 text-sm">Detaylı analiz</p>
                </button>
            </motion.div>
        </div>
    )
}
