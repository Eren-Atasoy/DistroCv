import { useState } from 'react'
import { motion } from 'framer-motion'
import {
    MessageSquare,
    Mic,
    Sparkles,
    CheckCircle,
    ChevronRight,
    RotateCcw
} from 'lucide-react'

const mockQuestions = [
    "Bize kendinizden bahseder misiniz?",
    "Bu pozisyona neden başvurdunuz?",
    "React ile en zorlu projeniz neydi?",
    "Takım içi bir anlaşmazlığı nasıl çözersiniz?",
    "5 yıl sonra kendinizi nerede görüyorsunuz?",
    "TypeScript kullanmanın avantajları nelerdir?",
    "State management için hangi araçları tercih edersiniz?",
    "Bir önceki işinizden neden ayrıldınız?",
    "Stres altında nasıl çalışırsınız?",
    "Bizim için sorularınız var mı?",
]

export default function InterviewPrep() {
    const [currentQuestion, setCurrentQuestion] = useState(0)
    const [answer, setAnswer] = useState('')
    const [isRecording, setIsRecording] = useState(false)
    const [answeredQuestions, setAnsweredQuestions] = useState<number[]>([])
    const [feedback, setFeedback] = useState<string | null>(null)
    const [isAnalyzing, setIsAnalyzing] = useState(false)

    const handleSubmitAnswer = () => {
        if (!answer.trim()) return

        setIsAnalyzing(true)

        setTimeout(() => {
            setFeedback(`STAR Analizi tamamlandı. Cevabınız değerlendirildi ve öneriler hazırlandı. Genel Puan: 85/100`)
            setIsAnalyzing(false)
            setAnsweredQuestions(prev => [...prev, currentQuestion])
            setAnswer('')
        }, 2000)
    }

    const goToNextQuestion = () => {
        if (currentQuestion < mockQuestions.length - 1) {
            setCurrentQuestion(prev => prev + 1)
            setFeedback(null)
        }
    }

    const progress = (answeredQuestions.length / mockQuestions.length) * 100

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            <div>
                <h1 className="text-2xl font-display font-bold text-white mb-1">
                    Mülakat Hazırlığı
                </h1>
                <p className="text-surface-400">
                    Senior Frontend Developer - Trendyol pozisyonu için
                </p>
            </div>

            <div className="glass-card p-4">
                <div className="flex items-center justify-between mb-2">
                    <span className="text-surface-400 text-sm">İlerleme</span>
                    <span className="text-primary-400 font-medium">
                        {answeredQuestions.length} / {mockQuestions.length} soru
                    </span>
                </div>
                <div className="h-2 bg-surface-800 rounded-full overflow-hidden">
                    <motion.div
                        className="h-full bg-gradient-to-r from-primary-500 to-accent-cyan rounded-full"
                        initial={{ width: 0 }}
                        animate={{ width: `${progress}%` }}
                        transition={{ duration: 0.5 }}
                    />
                </div>
            </div>

            <div className="flex gap-2 overflow-x-auto pb-2">
                {mockQuestions.map((_, index) => (
                    <button
                        key={index}
                        onClick={() => { setCurrentQuestion(index); setFeedback(null); }}
                        className={`w-10 h-10 rounded-lg flex items-center justify-center shrink-0 transition-all ${currentQuestion === index
                                ? 'bg-primary-500 text-white'
                                : answeredQuestions.includes(index)
                                    ? 'bg-accent-emerald/20 text-accent-emerald'
                                    : 'bg-surface-800 text-surface-400 hover:text-white'
                            }`}
                    >
                        {answeredQuestions.includes(index) ? (
                            <CheckCircle size={18} />
                        ) : (
                            index + 1
                        )}
                    </button>
                ))}
            </div>

            <div className="grid lg:grid-cols-2 gap-6">
                <div className="space-y-4">
                    <motion.div
                        key={currentQuestion}
                        initial={{ opacity: 0, x: -20 }}
                        animate={{ opacity: 1, x: 0 }}
                        className="glass-card p-6"
                    >
                        <div className="flex items-center gap-2 text-primary-400 text-sm mb-3">
                            <MessageSquare size={16} />
                            Soru {currentQuestion + 1}
                        </div>
                        <h2 className="text-xl font-semibold text-white">
                            {mockQuestions[currentQuestion]}
                        </h2>
                    </motion.div>

                    <div className="glass-card p-6">
                        <div className="flex items-center justify-between mb-3">
                            <span className="text-surface-400 text-sm">Cevabınız</span>
                            <button
                                onClick={() => setIsRecording(!isRecording)}
                                className={`p-2 rounded-lg transition-colors ${isRecording
                                        ? 'bg-error/20 text-error'
                                        : 'bg-surface-800 text-surface-400 hover:text-white'
                                    }`}
                            >
                                <Mic size={18} />
                            </button>
                        </div>

                        <textarea
                            value={answer}
                            onChange={(e) => setAnswer(e.target.value)}
                            placeholder="Cevabınızı buraya yazın veya mikrofon ile kaydedin..."
                            className="w-full h-40 p-4 rounded-xl bg-surface-800 text-white placeholder-surface-500 border border-surface-700 focus:border-primary-500 transition-colors resize-none"
                        />

                        <div className="flex items-center justify-between mt-4">
                            <span className="text-surface-500 text-sm">
                                {answer.length} karakter
                            </span>
                            <button
                                onClick={handleSubmitAnswer}
                                disabled={!answer.trim() || isAnalyzing}
                                className="px-4 py-2 rounded-lg bg-primary-500 text-white font-medium hover:bg-primary-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                                {isAnalyzing ? (
                                    <>
                                        <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                                        Analiz Ediliyor...
                                    </>
                                ) : (
                                    <>
                                        <Sparkles size={16} />
                                        Analiz Et
                                    </>
                                )}
                            </button>
                        </div>
                    </div>

                    <div className="p-4 rounded-xl bg-accent-violet/10 border border-accent-violet/20">
                        <h4 className="text-accent-violet font-medium text-sm mb-2 flex items-center gap-2">
                            <Sparkles size={14} />
                            STAR Tekniği
                        </h4>
                        <ul className="text-surface-300 text-sm space-y-1">
                            <li><strong>S</strong>ituation - Durumu tanımlayın</li>
                            <li><strong>T</strong>ask - Görevinizi açıklayın</li>
                            <li><strong>A</strong>ction - Aldığınız aksiyonları belirtin</li>
                            <li><strong>R</strong>esult - Sonuçları paylaşın</li>
                        </ul>
                    </div>
                </div>

                <div className="space-y-4">
                    {feedback ? (
                        <motion.div
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            className="glass-card p-6"
                        >
                            <div className="flex items-center justify-between mb-4">
                                <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                                    <Sparkles size={18} className="text-primary-400" />
                                    AI Geri Bildirimi
                                </h3>
                                <button
                                    onClick={() => setFeedback(null)}
                                    className="p-2 rounded-lg bg-surface-800 text-surface-400 hover:text-white transition-colors"
                                >
                                    <RotateCcw size={16} />
                                </button>
                            </div>

                            <p className="text-surface-300 leading-relaxed mb-6">{feedback}</p>

                            <button
                                onClick={goToNextQuestion}
                                className="w-full py-3 rounded-xl bg-primary-500/10 text-primary-400 font-medium hover:bg-primary-500/20 transition-colors flex items-center justify-center gap-2"
                            >
                                Sonraki Soru
                                <ChevronRight size={18} />
                            </button>
                        </motion.div>
                    ) : (
                        <div className="glass-card p-6 text-center">
                            <div className="w-20 h-20 mx-auto mb-4 rounded-full bg-surface-800 flex items-center justify-center">
                                <MessageSquare size={36} className="text-surface-500" />
                            </div>
                            <h3 className="text-lg font-medium text-white mb-2">
                                Cevabınızı Bekliyor
                            </h3>
                            <p className="text-surface-400 text-sm">
                                Cevabınızı yazdıktan sonra AI analizi burada görünecek
                            </p>
                        </div>
                    )}

                    {answeredQuestions.length > 0 && (
                        <div className="glass-card p-6">
                            <h3 className="text-lg font-semibold text-white mb-4">Performans Özeti</h3>
                            <div className="grid grid-cols-2 gap-4">
                                <div className="p-4 rounded-xl bg-surface-800/50 text-center">
                                    <div className="text-2xl font-bold text-accent-emerald">85</div>
                                    <div className="text-surface-400 text-sm">Ortalama Puan</div>
                                </div>
                                <div className="p-4 rounded-xl bg-surface-800/50 text-center">
                                    <div className="text-2xl font-bold text-primary-400">{answeredQuestions.length}</div>
                                    <div className="text-surface-400 text-sm">Cevaplanan</div>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}
