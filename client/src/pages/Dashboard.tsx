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
import { dashboardApi, applicationsApi, jobsApi, type JobMatch, type ApplicationDto, type DashboardStats, type DashboardTrends } from '../services/api'

// Types
interface Stat {
    label: string
    value: number
    icon: any
    change: string
    color: string
}

// Helper to format relative time
const timeAgo = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    let interval = seconds / 31536000;
    if (interval > 1) return Math.floor(interval) + " yıl önce";
    interval = seconds / 2592000;
    if (interval > 1) return Math.floor(interval) + " ay önce";
    interval = seconds / 86400;
    if (interval > 1) return Math.floor(interval) + " gün önce";
    interval = seconds / 3600;
    if (interval > 1) return Math.floor(interval) + " saat önce";
    interval = seconds / 60;
    if (interval > 1) return Math.floor(interval) + " dakika önce";
    return "Az önce";
};

// Helper for status colors
const getStatusColor = (status: string) => {
    switch (status) {
        case 'Viewed': return 'text-accent-violet';
        case 'Sent': return 'text-primary-400';
        case 'Responded': return 'text-accent-emerald';
        case 'Queued': return 'text-amber-400';
        case 'Rejected': return 'text-red-400';
        default: return 'text-surface-400';
    }
};

const getStatusLabel = (status: string) => {
    switch (status) {
        case 'Viewed': return 'Görüntülendi';
        case 'Sent': return 'Gönderildi';
        case 'Responded': return 'Yanıt Alındı';
        case 'Queued': return 'Beklemede';
        case 'Rejected': return 'Reddedildi';
        case 'Interview': return 'Mülakat';
        default: return status;
    }
};

export default function Dashboard() {
    const [stats, setStats] = useState<DashboardStats | null>(null);
    const [trends, setTrends] = useState<DashboardTrends | null>(null);
    const [recentApplications, setRecentApplications] = useState<ApplicationDto[]>([]);
    const [matchingJobs, setMatchingJobs] = useState<JobMatch[]>([]);
    const [loading, setLoading] = useState(true);

    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);

    // Initial Data Fetch
    useEffect(() => {
        const fetchData = async () => {
            try {
                // Parallel fetching
                const [statsData, trendsData, appsData, jobsData] = await Promise.all([
                    dashboardApi.getStats(),
                    dashboardApi.getTrends(),
                    applicationsApi.list(undefined, 0, 5).then(res => res.applications),
                    jobsApi.getMatchedJobs().then(jobs => jobs.slice(0, 5))
                ]);

                setStats(statsData);
                setTrends(trendsData);
                setRecentApplications(appsData);
                setMatchingJobs(jobsData);
            } catch (error) {
                console.error("Failed to fetch dashboard data", error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    // SignalR Connection
    useEffect(() => {
        const newConnection = new HubConnectionBuilder()
            .withUrl("http://localhost:5001/hubs/notifications") // Monitor port or env
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('SignalR Connected!');
                    setIsConnected(true);

                    connection.on("ReceiveStatsUpdate", (type: string, data: any) => {
                        if (type === 'DashboardStats') {
                            setStats(prev => ({ ...prev, ...data }));
                        }
                    });

                    connection.on("ReceiveNotification", (notification: any) => {
                        // Refresh recent applications if status changed
                        if (notification.type === 'ApplicationStatusUpdate') {
                            applicationsApi.list(undefined, 0, 5).then(res => setRecentApplications(res.applications));
                            dashboardApi.getStats().then(setStats);
                        }
                    });
                })
                .catch(e => console.error('Connection failed: ', e));
        }

        return () => {
            if (connection) {
                connection.off("ReceiveStatsUpdate");
                connection.off("ReceiveNotification");
            }
        };
    }, [connection]);

    // Derived Stats for Display
    const displayStats: Stat[] = [
        {
            label: 'Toplam Başvuru',
            value: stats?.totalApplications || 0,
            icon: FileText,
            change: '', // TODO: Calculate change
            color: 'from-primary-500 to-primary-600'
        },
        {
            label: 'Gönderilen',
            value: stats?.sentApplications || 0,
            icon: Send,
            change: `${stats?.totalApplications ? Math.round((stats.sentApplications / stats.totalApplications) * 100) : 0}%`,
            color: 'from-accent-cyan to-primary-500'
        },
        {
            label: 'Görüntülenen',
            value: stats?.viewedApplications || 0,
            icon: Eye,
            change: '',
            color: 'from-accent-violet to-accent-fuchsia'
        },
        {
            label: 'Yanıt Alınan',
            value: stats?.respondedApplications || 0,
            icon: MessageSquare,
            change: `${stats?.responseRate || 0}% Oran`,
            color: 'from-accent-emerald to-accent-cyan'
        },
    ];

    // Chart Data Transformation
    const chartData = trends?.weeklyApplications.map(p => ({
        name: new Date(p.date).toLocaleDateString('tr-TR', { weekday: 'short' }),
        basvuru: p.count
    })) || [];

    const statusChartData = trends?.statusBreakdown.map(s => ({
        name: getStatusLabel(s.status),
        value: s.count
    })) || [];

    if (loading) {
        return <div className="p-8 text-center text-surface-400">Yükleniyor...</div>;
    }

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
                {displayStats.map((stat, index) => (
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
                            {stat.change && (
                                <span className="text-xs text-accent-emerald bg-accent-emerald/10 px-2 py-1 rounded-full">
                                    {stat.change}
                                </span>
                            )}
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
                            <BarChart data={statusChartData}>
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
                                            {app.jobPosting.title}
                                        </h3>
                                        <p className="text-surface-400 text-sm">{app.jobPosting.companyName}</p>
                                    </div>
                                </div>
                                <div className="text-right">
                                    <span className={`text-sm font-medium ${getStatusColor(app.status)}`}>
                                        {getStatusLabel(app.status)}
                                    </span>
                                    <p className="text-surface-500 text-xs mt-1">{timeAgo(app.createdAt)}</p>
                                </div>
                            </div>
                        ))}
                        {recentApplications.length === 0 && (
                            <div className="text-center text-surface-400 py-4">Henüz başvuru yok</div>
                        )}
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
                        {matchingJobs.map((match) => (
                            <div
                                key={match.id}
                                className="p-4 rounded-xl bg-surface-800/50 hover:bg-surface-800 transition-colors group cursor-pointer"
                            >
                                <div className="flex items-center justify-between mb-2">
                                    <h3 className="text-white font-medium text-sm group-hover:text-primary-400 transition-colors">
                                        {match.jobPosting.title}
                                    </h3>
                                    <div className="flex items-center gap-1">
                                        <span className="text-accent-emerald font-bold">{match.matchScore}%</span>
                                    </div>
                                </div>
                                <p className="text-surface-400 text-sm">{match.jobPosting.companyName}</p>
                                <div className="mt-3 h-1.5 bg-surface-700 rounded-full overflow-hidden">
                                    <div
                                        className="h-full bg-gradient-to-r from-accent-emerald to-accent-cyan rounded-full transition-all duration-500"
                                        style={{ width: `${match.matchScore}%` }}
                                    />
                                </div>
                            </div>
                        ))}
                        {matchingJobs.length === 0 && (
                            <div className="text-center text-surface-400 py-4">Henüz eşleşme yok</div>
                        )}
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
