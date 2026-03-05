import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { authApi } from '../services/api';

interface User {
    id: string;
    email: string;
    fullName: string;
}

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (data: any) => Promise<void>;
    register: (data: any) => Promise<void>;
    googleLogin: (credential: string) => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // Check if user is logged in
        const token = localStorage.getItem('token');
        const storedUser = localStorage.getItem('user');

        if (token && storedUser) {
            try {
                setUser(JSON.parse(storedUser));
            } catch (e) {
                localStorage.removeItem('token');
                localStorage.removeItem('user');
            }
        }
        setIsLoading(false);
    }, []);

    const login = async (data: any) => {
        const response = await authApi.login(data);
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify({ email: data.email, fullName: "User" })); // Placeholder till real user object is mapped
        setUser({ id: '1', email: data.email, fullName: 'User' });
    };

    const register = async (data: any) => {
        const response = await authApi.register(data);
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify({ email: data.email, fullName: data.fullName }));
        setUser({ id: '1', email: data.email, fullName: data.fullName });
    };

    const googleLogin = async (credential: string) => {
        const response = await authApi.google({ idToken: credential, preferredLanguage: 'tr' });
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify(response.user || { email: 'google-user@example.com', fullName: 'Google User' }));
        setUser(response.user || { id: '1', email: 'google-user@example.com', fullName: 'Google User' });
    };

    const logout = async () => {
        try {
            await authApi.logout();
        } catch (error) {
            console.error('Logout failed:', error);
        } finally {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            setUser(null);
        }
    };

    return (
        <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, register, googleLogin, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
