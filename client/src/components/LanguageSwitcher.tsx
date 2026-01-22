import { useTranslation } from 'react-i18next';
import { useState } from 'react';

const GlobeIcon = () => (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
    </svg>
);

const CheckIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
    </svg>
);

const languages = [
    { code: 'tr', name: 'Türkçe', flag: '🇹🇷' },
    { code: 'en', name: 'English', flag: '🇬🇧' }
];

interface LanguageSwitcherProps {
    variant?: 'dropdown' | 'buttons' | 'minimal';
    className?: string;
}

export default function LanguageSwitcher({ variant = 'dropdown', className = '' }: LanguageSwitcherProps) {
    const { i18n } = useTranslation();
    const [isOpen, setIsOpen] = useState(false);

    const currentLanguage = languages.find(lang => lang.code === i18n.language) || languages[0];

    const changeLanguage = (code: string) => {
        i18n.changeLanguage(code);
        setIsOpen(false);
    };

    // Dropdown variant
    if (variant === 'dropdown') {
        return (
            <div className={`relative ${className}`}>
                <button
                    onClick={() => setIsOpen(!isOpen)}
                    className="flex items-center gap-2 px-3 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg text-white transition-colors"
                >
                    <GlobeIcon />
                    <span className="text-lg">{currentLanguage.flag}</span>
                    <span className="hidden sm:inline">{currentLanguage.name}</span>
                    <svg className={`w-4 h-4 transition-transform ${isOpen ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                </button>

                {isOpen && (
                    <>
                        <div 
                            className="fixed inset-0 z-10" 
                            onClick={() => setIsOpen(false)}
                        />
                        <div className="absolute right-0 mt-2 w-48 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-20 overflow-hidden">
                            {languages.map(lang => (
                                <button
                                    key={lang.code}
                                    onClick={() => changeLanguage(lang.code)}
                                    className={`w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-slate-700 transition-colors ${
                                        i18n.language === lang.code ? 'bg-slate-700/50 text-blue-400' : 'text-white'
                                    }`}
                                >
                                    <span className="text-xl">{lang.flag}</span>
                                    <span className="flex-1">{lang.name}</span>
                                    {i18n.language === lang.code && (
                                        <CheckIcon />
                                    )}
                                </button>
                            ))}
                        </div>
                    </>
                )}
            </div>
        );
    }

    // Buttons variant
    if (variant === 'buttons') {
        return (
            <div className={`flex items-center gap-1 bg-slate-700 rounded-lg p-1 ${className}`}>
                {languages.map(lang => (
                    <button
                        key={lang.code}
                        onClick={() => changeLanguage(lang.code)}
                        className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all ${
                            i18n.language === lang.code 
                                ? 'bg-blue-600 text-white' 
                                : 'text-slate-400 hover:text-white hover:bg-slate-600'
                        }`}
                    >
                        <span className="text-lg">{lang.flag}</span>
                        <span className="hidden sm:inline text-sm">{lang.code.toUpperCase()}</span>
                    </button>
                ))}
            </div>
        );
    }

    // Minimal variant (just flags)
    return (
        <div className={`flex items-center gap-2 ${className}`}>
            {languages.map(lang => (
                <button
                    key={lang.code}
                    onClick={() => changeLanguage(lang.code)}
                    className={`text-2xl transition-all ${
                        i18n.language === lang.code 
                            ? 'scale-110 opacity-100' 
                            : 'opacity-50 hover:opacity-100'
                    }`}
                    title={lang.name}
                >
                    {lang.flag}
                </button>
            ))}
        </div>
    );
}

