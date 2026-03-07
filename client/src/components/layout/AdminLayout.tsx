import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
    LayoutDashboard,
    Building2,
    LogOut,
    Menu,
    X,
    ArrowLeft,
    Database,
    Shield
} from 'lucide-react'
import { useState } from 'react'
import { useAuth } from '../../contexts/AuthContext'

const adminNavItems = [
    { path: '/admin', icon: LayoutDashboard, label: 'Dashboard' },
    { path: '/admin/companies', icon: Building2, label: 'Şirketler' },
    { path: '/admin/seed', icon: Database, label: 'İş İlanı Seed' },
]

export default function AdminLayout() {
    const location = useLocation()
    const navigate = useNavigate()
    const [sidebarOpen, setSidebarOpen] = useState(false)
    const { logout, user } = useAuth()

    const handleLogout = async () => {
        await logout()
        navigate('/login')
    }

    return (
        <div className="min-h-screen bg-surface-950 flex">
            {/* Mobile Menu Button */}
            <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className="lg:hidden fixed top-4 left-4 z-50 p-2 rounded-lg bg-surface-800 text-white"
            >
                {sidebarOpen ? <X size={24} /> : <Menu size={24} />}
            </button>

            {/* Sidebar */}
            <aside
                className={`
                    fixed lg:static inset-y-0 left-0 z-40
                    w-64 bg-red-950/30 backdrop-blur-xl border-r border-red-900/30
                    transform transition-transform duration-300 ease-in-out
                    ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
                `}
            >
                <div className="flex flex-col h-full">
                    {/* Logo / Admin Badge */}
                    <div className="p-6 border-b border-red-900/30">
                        <Link to="/admin" className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-red-500 to-red-700 flex items-center justify-center">
                                <Shield size={20} className="text-white" />
                            </div>
                            <div>
                                <span className="font-display font-bold text-xl text-red-400">Admin</span>
                                <p className="text-xs text-surface-500">DistroCV Yönetim</p>
                            </div>
                        </Link>
                    </div>

                    {/* Navigation */}
                    <nav className="flex-1 p-4 space-y-2">
                        {adminNavItems.map((item) => {
                            const isActive = location.pathname === item.path
                            return (
                                <Link
                                    key={item.path}
                                    to={item.path}
                                    onClick={() => setSidebarOpen(false)}
                                    className={`
                                        flex items-center gap-3 px-4 py-3 rounded-xl
                                        transition-all duration-200 group
                                        ${isActive
                                            ? 'bg-red-500/20 text-red-400 shadow-glow-sm'
                                            : 'text-surface-400 hover:text-white hover:bg-surface-800'
                                        }
                                    `}
                                >
                                    <item.icon
                                        size={20}
                                        className={`transition-transform duration-200 ${isActive ? 'scale-110' : 'group-hover:scale-110'}`}
                                    />
                                    <span className="font-medium">{item.label}</span>
                                    {isActive && (
                                        <motion.div
                                            layoutId="adminActiveIndicator"
                                            className="ml-auto w-1.5 h-1.5 rounded-full bg-red-400"
                                        />
                                    )}
                                </Link>
                            )
                        })}
                    </nav>

                    {/* Bottom Section */}
                    <div className="p-4 border-t border-red-900/30 space-y-2">
                        <div className="px-4 py-2 text-sm text-surface-500">
                            {user?.fullName} <span className="text-red-400 text-xs">(Admin)</span>
                        </div>
                        <Link
                            to="/dashboard"
                            className="flex items-center gap-3 px-4 py-3 rounded-xl text-surface-400 hover:text-primary-400 hover:bg-surface-800 transition-all duration-200"
                        >
                            <ArrowLeft size={20} />
                            <span className="font-medium">Kullanıcı Paneli</span>
                        </Link>
                        <button
                            onClick={handleLogout}
                            className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-surface-400 hover:text-red-400 hover:bg-red-500/10 transition-all duration-200"
                        >
                            <LogOut size={20} />
                            <span className="font-medium">Çıkış</span>
                        </button>
                    </div>
                </div>
            </aside>

            {/* Overlay for mobile */}
            {sidebarOpen && (
                <div
                    className="fixed inset-0 bg-black/50 z-30 lg:hidden"
                    onClick={() => setSidebarOpen(false)}
                />
            )}

            {/* Main Content */}
            <main className="flex-1 min-h-screen overflow-auto">
                <div className="p-6 lg:p-8">
                    <Outlet />
                </div>
            </main>
        </div>
    )
}
