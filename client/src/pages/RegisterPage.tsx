import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { GoogleLogin } from '@react-oauth/google';
import { Mail, Lock, User as UserIcon, ArrowRight, Eye, EyeOff, Check, X } from 'lucide-react';

function validateEmail(email: string): string | null {
    if (!email) return 'E-posta adresi zorunludur.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Geçerli bir e-posta adresi giriniz.';
    if (email.length > 255) return 'E-posta adresi 255 karakterden uzun olamaz.';
    return null;
}

function validateFullName(name: string): string | null {
    if (!name) return 'Ad Soyad zorunludur.';
    if (name.trim().length < 2) return 'Ad Soyad en az 2 karakter olmalıdır.';
    if (name.length > 200) return 'Ad Soyad 200 karakterden uzun olamaz.';
    return null;
}

interface PasswordStrength {
    hasMinLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasDigit: boolean;
    hasSpecial: boolean;
}

function getPasswordStrength(password: string): PasswordStrength {
    return {
        hasMinLength: password.length >= 8,
        hasUppercase: /[A-Z]/.test(password),
        hasLowercase: /[a-z]/.test(password),
        hasDigit: /\d/.test(password),
        hasSpecial: /[@$!%*?&]/.test(password),
    };
}

function PasswordRule({ ok, label }: { ok: boolean; label: string }) {
    return (
        <li className={`flex items-center gap-1.5 text-xs ${ok ? 'text-green-400' : 'text-surface-500'}`}>
            {ok ? <Check size={12} /> : <X size={12} />}
            {label}
        </li>
    );
}

export default function RegisterPage() {
    const [fullName, setFullName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState('');
    const [fieldErrors, setFieldErrors] = useState<{ [key: string]: string }>({});
    const [isLoading, setIsLoading] = useState(false);
    const [passwordFocused, setPasswordFocused] = useState(false);

    const { register, googleLogin, isAuthenticated, isLoading: isAuthLoading } = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        if (!isAuthLoading && isAuthenticated) {
            navigate('/dashboard', { replace: true });
        }
    }, [isAuthenticated, isAuthLoading, navigate]);

    const strength = getPasswordStrength(password);
    const passwordValid = Object.values(strength).every(Boolean);

    const validate = (): boolean => {
        const errors: { [key: string]: string } = {};
        const nameErr = validateFullName(fullName);
        const emailErr = validateEmail(email);
        if (nameErr) errors.fullname = nameErr;
        if (emailErr) errors.email = emailErr;
        if (!passwordValid) errors.password = 'Şifre gereksinimleri karşılanmıyor.';
        setFieldErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        if (!validate()) return;
        setIsLoading(true);

        try {
            await register({ fullName, email, password, preferredLanguage: 'tr' });
            navigate('/dashboard');
        } catch (err: any) {
            // Backend field-specific errors (ASP.NET ModelState: { FieldName: ["..."] })
            if (err.details && typeof err.details === 'object') {
                const fErrors: { [key: string]: string } = {};
                Object.entries(err.details).forEach(([field, messages]) => {
                    fErrors[field.toLowerCase()] = (messages as string[]).join(', ');
                });
                setFieldErrors(fErrors);
            }
            setError(err.message || 'Kayıt sırasında hata oluştu.');
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
            setError('Google ile kayıt başarısız oldu.');
        }
    };

    return (
        <div className="min-h-screen bg-surface-900 flex items-center justify-center p-6">
            <div className="w-full max-w-md">
                <div className="text-center mb-10">
                    <div className="w-16 h-16 mx-auto rounded-2xl bg-gradient-to-br from-primary-500 to-accent-cyan flex items-center justify-center mb-6">
                        <span className="text-white font-bold text-2xl">D</span>
                    </div>
                    <h1 className="text-3xl font-display font-bold text-white mb-2">Hesap Oluştur</h1>
                    <p className="text-surface-400">Yapay zeka ile kariyerini güçlendir</p>
                </div>

                <div className="glass-card p-8">
                    {error && (
                        <div className="bg-red-500/10 border border-red-500/50 text-red-500 px-4 py-3 rounded-lg mb-6">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div>
                            <label className="block text-sm font-medium text-surface-300 mb-2">Ad Soyad</label>
                            <div className="relative">
                                <input
                                    type="text"
                                    value={fullName}
                                    onChange={(e) => { setFullName(e.target.value); setFieldErrors(prev => ({ ...prev, fullname: '' })); }}
                                    className={`w-full bg-surface-800 border rounded-lg py-3 px-10 text-white placeholder:text-surface-500 focus:outline-none focus:ring-1 transition-all font-mono ${fieldErrors.fullname ? 'border-red-500 focus:border-red-500 focus:ring-red-500' : 'border-surface-700 focus:border-primary-500 focus:ring-primary-500'}`}
                                    placeholder="Ali Veli"
                                    autoComplete="name"
                                />
                                <UserIcon className="absolute left-3 top-3.5 text-surface-500" size={18} />
                            </div>
                            {fieldErrors.fullname && (
                                <p className="text-red-500 text-xs mt-1">{fieldErrors.fullname}</p>
                            )}
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-surface-300 mb-2">E-posta</label>
                            <div className="relative">
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => { setEmail(e.target.value); setFieldErrors(prev => ({ ...prev, email: '' })); }}
                                    className={`w-full bg-surface-800 border rounded-lg py-3 px-10 text-white placeholder:text-surface-500 focus:outline-none focus:ring-1 transition-all font-mono ${fieldErrors.email ? 'border-red-500 focus:border-red-500 focus:ring-red-500' : 'border-surface-700 focus:border-primary-500 focus:ring-primary-500'}`}
                                    placeholder="isim@sirket.com"
                                    autoComplete="email"
                                />
                                <Mail className="absolute left-3 top-3.5 text-surface-500" size={18} />
                            </div>
                            {fieldErrors.email && (
                                <p className="text-red-500 text-xs mt-1">{fieldErrors.email}</p>
                            )}
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-surface-300 mb-2">Şifre</label>
                            <div className="relative">
                                <input
                                    type={showPassword ? 'text' : 'password'}
                                    value={password}
                                    onChange={(e) => { setPassword(e.target.value); setFieldErrors(prev => ({ ...prev, password: '' })); }}
                                    onFocus={() => setPasswordFocused(true)}
                                    onBlur={() => setPasswordFocused(false)}
                                    className={`w-full bg-surface-800 border rounded-lg py-3 px-10 text-white placeholder:text-surface-500 focus:outline-none focus:ring-1 transition-all ${fieldErrors.password ? 'border-red-500 focus:border-red-500 focus:ring-red-500' : 'border-surface-700 focus:border-primary-500 focus:ring-primary-500'}`}
                                    placeholder="••••••••"
                                    autoComplete="new-password"
                                />
                                <Lock className="absolute left-3 top-3.5 text-surface-500" size={18} />
                                <button
                                    type="button"
                                    onClick={() => setShowPassword(v => !v)}
                                    className="absolute right-3 top-3.5 text-surface-500 hover:text-surface-300 transition-colors"
                                    tabIndex={-1}
                                >
                                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                                </button>
                            </div>
                            {(passwordFocused || password.length > 0) && (
                                <ul className="mt-2 space-y-1 bg-surface-800/60 rounded-lg p-3">
                                    <PasswordRule ok={strength.hasMinLength} label="En az 8 karakter" />
                                    <PasswordRule ok={strength.hasUppercase} label="En az bir büyük harf (A-Z)" />
                                    <PasswordRule ok={strength.hasLowercase} label="En az bir küçük harf (a-z)" />
                                    <PasswordRule ok={strength.hasDigit} label="En az bir rakam (0-9)" />
                                    <PasswordRule ok={strength.hasSpecial} label="En az bir özel karakter (@$!%*?&)" />
                                </ul>
                            )}
                            {fieldErrors.password && (
                                <p className="text-red-500 text-xs mt-1">{fieldErrors.password}</p>
                            )}
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full btn-glow bg-primary-500 hover:bg-primary-600 text-white py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                        >
                            {isLoading ? 'Katılınıyor...' : 'Hemen Başla'}
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
                                onError={() => setError('Google ile kayıt başarısız oldu.')}
                                theme="filled_black"
                                shape="rectangular"
                            />
                        </div>
                    </div>
                </div>

                <p className="text-center mt-8 text-surface-400">
                    Zaten hesabınız var mı? <Link to="/login" className="text-accent-cyan hover:text-accent-cyan/80">Giriş Yapın</Link>
                </p>
            </div>
        </div>
    );
}
