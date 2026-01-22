import { Outlet, Link, useLocation } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
    LayoutDashboard,
    Compass,
    FileText,
    Settings,
    LogOut,
    Menu,
    X,
    Building2,
    GraduationCap,
    Linkedin
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import LanguageSwitcher from '../LanguageSwitcher'

const useNavItems = () => {
    const { t } = useTranslation()
    return [
        { path: '/dashboard', icon: LayoutDashboard, label: t('nav.dashboard') },
        { path: '/discover', icon: Compass, label: t('nav.discover') },
        { path: '/applications', icon: FileText, label: t('nav.applications') },
        { path: '/skills', icon: GraduationCap, label: t('nav.skills') },
        { path: '/linkedin-optimizer', icon: Linkedin, label: t('nav.linkedin') },
        { path: '/admin/companies', icon: Building2, label: t('nav.companies') },
    ]
}

export default function Layout() {
    const location = useLocation()
    const [sidebarOpen, setSidebarOpen] = useState(false)
    const { t } = useTranslation()
    const navItems = useNavItems()

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
          w-64 bg-surface-900/80 backdrop-blur-xl border-r border-surface-700
          transform transition-transform duration-300 ease-in-out
          ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        `}
            >
                <div className="flex flex-col h-full">
                    {/* Logo */}
                    <div className="p-6 border-b border-surface-700">
                        <Link to="/dashboard" className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary-500 to-accent-cyan flex items-center justify-center">
                                <span className="text-white font-bold text-lg">D</span>
                            </div>
                            <span className="font-display font-bold text-xl gradient-text">DistroCV</span>
                        </Link>
                    </div>

                    {/* Navigation */}
                    <nav className="flex-1 p-4 space-y-2">
                        {navItems.map((item) => {
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
                                            ? 'bg-primary-500/20 text-primary-400 shadow-glow-sm'
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
                                            layoutId="activeIndicator"
                                            className="ml-auto w-1.5 h-1.5 rounded-full bg-primary-400"
                                        />
                                    )}
                                </Link>
                            )
                        })}
                    </nav>

                    {/* Language Switcher */}
                    <div className="p-4 border-t border-surface-700">
                        <LanguageSwitcher variant="buttons" className="w-full justify-center" />
                    </div>

                    {/* Bottom Section */}
                    <div className="p-4 border-t border-surface-700 space-y-2">
                        <Link
                            to="/settings"
                            className="flex items-center gap-3 px-4 py-3 rounded-xl text-surface-400 hover:text-white hover:bg-surface-800 transition-all duration-200"
                        >
                            <Settings size={20} />
                            <span className="font-medium">{t('nav.settings')}</span>
                        </Link>
                        <button
                            className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-surface-400 hover:text-error hover:bg-error/10 transition-all duration-200"
                        >
                            <LogOut size={20} />
                            <span className="font-medium">{t('nav.logout')}</span>
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
