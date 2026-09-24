import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button, Card } from '../components/ui/Components';
import { Shield, Lock, Mail, User as UserIcon, ArrowRight, Sparkles } from 'lucide-react';

interface AuthPageProps {
  onNavigate: (path: string) => void;
  isRegister?: boolean;
}

export const AuthPage: React.FC<AuthPageProps> = ({ onNavigate, isRegister = false }) => {
  const { login, register } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      if (isRegister) {
        if (!fullName.trim()) {
          setError('Full name is required.');
          setIsLoading(false);
          return;
        }
        await register(email, password, fullName);
        onNavigate('/onboarding');
      } else {
        await login(email, password);
        onNavigate('/');
      }
    } catch (err: any) {
      setError(err.message || 'Authentication failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col justify-center items-center px-4 py-12 relative overflow-hidden">
      {/* Subtle Background Glow */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl pointer-events-none" />

      <div className="w-full max-w-md space-y-8 relative z-10">
        {/* Header Logo */}
        <div className="text-center space-y-2">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 text-white shadow-lg shadow-blue-500/25 mb-2">
            <Shield className="w-6 h-6" />
          </div>
          <h2 className="text-2xl font-bold tracking-tight text-white">
            {isRegister ? 'Create your account' : 'Welcome back'}
          </h2>
          <p className="text-sm text-slate-400">
            {isRegister
              ? 'Transform passive scrolling into intentional consumption.'
              : 'Sign in to monitor your digital diet and learning retention.'}
          </p>
        </div>

        {/* Form Card */}
        <Card className="border-slate-800 bg-slate-900/80 p-8 shadow-2xl backdrop-blur-xl">
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="p-3.5 rounded-lg bg-rose-950/40 border border-rose-500/30 text-rose-300 text-xs leading-relaxed">
                {error}
              </div>
            )}

            {isRegister && (
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1.5">Full Name</label>
                <div className="relative">
                  <UserIcon className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
                  <input
                    type="text"
                    required
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    placeholder="Alex Morgan"
                    className="w-full pl-9 pr-4 py-2.5 bg-slate-950/80 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors"
                  />
                </div>
              </div>
            )}

            <div>
              <label className="block text-xs font-medium text-slate-300 mb-1.5">Email Address</label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="alex@example.com"
                  className="w-full pl-9 pr-4 py-2.5 bg-slate-950/80 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-300 mb-1.5">Password</label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full pl-9 pr-4 py-2.5 bg-slate-950/80 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors"
                />
              </div>
              {isRegister && (
                <p className="text-[11px] text-slate-500 mt-1.5">
                  Must be at least 8 characters with 1 uppercase letter and 1 number.
                </p>
              )}
            </div>

            <Button
              type="submit"
              variant="primary"
              size="lg"
              isLoading={isLoading}
              icon={ArrowRight}
              className="w-full mt-2"
            >
              {isRegister ? 'Start Onboarding' : 'Sign In'}
            </Button>
          </form>

          <div className="mt-6 pt-6 border-t border-slate-800 text-center">
            {isRegister ? (
              <p className="text-xs text-slate-400">
                Already have an account?{' '}
                <button
                  onClick={() => onNavigate('/login')}
                  className="text-blue-400 hover:text-blue-300 font-medium ml-1"
                >
                  Sign in
                </button>
              </p>
            ) : (
              <p className="text-xs text-slate-400">
                New to Scroll Guardian?{' '}
                <button
                  onClick={() => onNavigate('/register')}
                  className="text-blue-400 hover:text-blue-300 font-medium ml-1"
                >
                  Create an account
                </button>
              </p>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
};
