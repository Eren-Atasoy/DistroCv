import { useState } from 'react'
import { motion } from 'framer-motion'
import { Database, Loader2, CheckCircle, AlertTriangle } from 'lucide-react'
import { api } from '../../services/api'

interface SeedResult {
    message: string
    jobs?: any[]
    totalCreated?: number
}

export default function AdminSeedPage() {
    const [seeding, setSeeding] = useState(false)
    const [result, setResult] = useState<SeedResult | null>(null)
    const [error, setError] = useState<string | null>(null)

    const handleSeed = async () => {
        setSeeding(true)
        setError(null)
        setResult(null)

        try {
            const res = await api.post<SeedResult>('/jobs/seed')
            setResult(res)
        } catch (err: any) {
            setError(err.message || 'Seed işlemi başarısız oldu')
        } finally {
            setSeeding(false)
        }
    }

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-display font-bold text-white mb-2">
                    İş İlanı Seed
                </h1>
                <p className="text-surface-400">
                    Test amaçlı örnek iş ilanları ve embedding'leri oluşturun
                </p>
            </div>

            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="glass-card p-8 max-w-xl"
            >
                <div className="flex items-center gap-4 mb-6">
                    <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-red-500 to-red-600 flex items-center justify-center">
                        <Database size={24} className="text-white" />
                    </div>
                    <div>
                        <h2 className="text-lg font-semibold text-white">Seed İş İlanları</h2>
                        <p className="text-surface-400 text-sm">5 adet örnek iş ilanı ve Gemini embedding oluşturur</p>
                    </div>
                </div>

                <button
                    onClick={handleSeed}
                    disabled={seeding}
                    className="w-full py-3 rounded-xl bg-red-500 hover:bg-red-600 disabled:bg-surface-700 disabled:cursor-not-allowed text-white font-medium transition-colors flex items-center justify-center gap-2"
                >
                    {seeding ? (
                        <>
                            <Loader2 size={20} className="animate-spin" />
                            Seed ediliyor...
                        </>
                    ) : (
                        <>
                            <Database size={20} />
                            Seed Başlat
                        </>
                    )}
                </button>

                {result && (
                    <motion.div
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="mt-6 p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20"
                    >
                        <div className="flex items-center gap-2 mb-2">
                            <CheckCircle size={18} className="text-emerald-400" />
                            <span className="text-emerald-400 font-medium">{result.message}</span>
                        </div>
                        {result.jobs && (
                            <div className="mt-3 space-y-2">
                                {result.jobs.map((job: any, i: number) => (
                                    <div key={i} className="text-sm text-surface-300 flex items-center gap-2">
                                        <span className="w-2 h-2 rounded-full bg-emerald-400" />
                                        <span className="font-medium">{job.title}</span>
                                        <span className="text-surface-500">— {job.company}</span>
                                        {job.hasEmbedding && (
                                            <span className="text-xs text-emerald-400/70 ml-auto">embedding ✓</span>
                                        )}
                                    </div>
                                ))}
                            </div>
                        )}
                    </motion.div>
                )}

                {error && (
                    <motion.div
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="mt-6 p-4 rounded-xl bg-red-500/10 border border-red-500/20"
                    >
                        <div className="flex items-center gap-2">
                            <AlertTriangle size={18} className="text-red-400" />
                            <span className="text-red-400">{error}</span>
                        </div>
                    </motion.div>
                )}
            </motion.div>
        </div>
    )
}
