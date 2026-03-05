import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { GoogleLogin } from '@react-oauth/google';
import { Mail, Lock, ArrowRight } from 'lucide-react';

export default function LoginPage() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [fieldErrors, setFieldErrors] = useState<{ [key: string]: string[] }>({});
    const [isLoading, setIsLoading] = useState(false);

    const { login, googleLogin } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError('');
        setFieldErrors({});

        try {
            await login({ email, password });
            navigate('/dashboard');
        } catch (err: any) {
            setError(err.message || 'Giriş yapılamadı.');
            if (err.details && Array.isArray(err.details)) {
                const fErrors: { [key: string]: string[] } = {};
                err.details.forEach((d: any) => {
                    if (d.field && d.errors) {
                        fErrors[d.field.toLowerCase()] = d.errors;
                    }
                });
                setFieldErrors(fErrors);
            }
        } finally {
            setIsLoading(false);
        }
    };

    const handleGoogleSuccess = async (credentialResponse: any) => {
        try {
            if (credentialResponse.credential) {
                await googleLogin(credentialResponse.credential);
                navigate('/dashboard');
            }
        } catch (err: any) {
            setError('Google ile giriş başarısız oldu.');
        }
    };

    return (
        <div className="min-h-screen bg-surface-900 flex items-center justify-center p-6">
            <div className="w-full max-w-md">
                <div className="text-center mb-10">
                    <div className="w-16 h-16 mx-auto rounded-2xl bg-gradient-to-br from-primary-500 to-accent-cyan flex items-center justify-center mb-6">
                        <span className="text-white font-bold text-2xl">D</span>
                    </div>
                    <h1 className="text-3xl font-display font-bold text-white mb-2">Tekrar Hoş Geldiniz</h1>
                    <p className="text-surface-400">Kariyer asistanınız sizi bekliyor</p>
                </div>

                <div className="glass-card p-8">
                    {error && (
                        <div className="bg-red-500/10 border border-red-500/50 text-red-500 px-4 py-3 rounded-lg mb-6">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div>
                            <label className="block text-sm font-medium text-surface-300 mb-2">E-posta</label>
                            <div className="relative">
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    className="w-full bg-surface-800 border border-surface-700 rounded-lg py-3 px-10 text-white placeholder:text-surface-500 focus:outline-none focus:border-primary-500 focus:ring-1 focus:ring-primary-500 transition-all font-mono"
                                    placeholder="isim@sirket.com"
                                    required
                                />
                                <Mail className="absolute left-3 top-3.5 text-surface-500" size={18} />
                            </div>
                            {fieldErrors.email && (
                                <p className="text-red-500 text-xs mt-1">{fieldErrors.email.join(', ')}</p>
                            )}
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-surface-300 mb-2">Şifre</label>
                            <div className="relative">
                                <input
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    className="w-full bg-surface-800 border border-surface-700 rounded-lg py-3 px-10 text-white placeholder:text-surface-500 focus:outline-none focus:border-primary-500 focus:ring-1 focus:ring-primary-500 transition-all"
                                    placeholder="••••••••"
                                    required
                                />
                                <Lock className="absolute left-3 top-3.5 text-surface-500" size={18} />
                            </div>
                            {fieldErrors.password && (
                                <p className="text-red-500 text-xs mt-1">{fieldErrors.password.join(', ')}</p>
                            )}
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full btn-glow bg-primary-500 hover:bg-primary-600 text-white py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                        >
                            {isLoading ? 'Giriş Yapılıyor...' : 'Giriş Yap'}
                            {!isLoading && <ArrowRight size={18} />}
                        </button>
                    </form>

                    <div className="mt-6">
                        <div className="relative">
                            <div className="absolute inset-0 flex items-center">
                                <div className="w-full border-t border-surface-700"></div>
                            </div>
                            <div className="relative flex justify-center text-sm">
                                <span className="px-2 bg-[#0B0F19] text-surface-400">veya</span>
                            </div>
                        </div>

                        <div className="mt-6 flex justify-center">
                            <GoogleLogin
                                onSuccess={handleGoogleSuccess}
                                onError={() => setError('Google ile giriş başarısız oldu.')}
                                theme="filled_black"
                                shape="rectangular"
                            />
                        </div>
                    </div>
                </div>

                <p className="text-center mt-8 text-surface-400">
                    Hesabınız yok mu? <Link to="/register" className="text-primary-400 hover:text-primary-300">Hemen Kayıt Olun</Link>
                </p>
            </div>
        </div>
    );
}
