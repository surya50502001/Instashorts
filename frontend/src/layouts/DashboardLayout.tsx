import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import {
  LayoutDashboard,
  Radio,
  Network,
  BarChart3,
  Compass,
  Sliders,
  CreditCard,
  LogOut,
  Shield,
  Sparkles,
  Zap,
  BookOpen,
} from 'lucide-react';
import { Button, Badge } from '../components/ui/Components';
import { KnowledgeCheckModal } from '../components/ui/KnowledgeCheckModal';
import { KnowledgeCheckDetail, RecommendationResponse } from '../types';
import { api } from '../services/api';

interface DashboardLayoutProps {
  currentPath: string;
  onNavigate: (path: string) => void;
  children: React.ReactNode;
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({
  currentPath,
  onNavigate,
  children,
}) => {
  const { user, logout } = useAuth();
  const [activeQuiz, setActiveQuiz] = useState<KnowledgeCheckDetail | null>(null);
  const [isQuizOpen, setIsQuizOpen] = useState<boolean>(false);
  const [isFindingUseful, setIsFindingUseful] = useState<boolean>(false);
  const [usefulRec, setUsefulRec] = useState<RecommendationResponse | null>(null);

  const navItems = [
    { label: 'Dashboard', path: '/', icon: LayoutDashboard },
    { label: 'Live Stream', path: '/stream', icon: Radio },
    { label: 'Knowledge Map', path: '/knowledge', icon: Network },
    { label: 'Analytics', path: '/analytics', icon: BarChart3 },
    { label: 'Recommendations', path: '/recommendations', icon: Compass },
    { label: 'Goals & Privacy', path: '/settings', icon: Sliders },
    { label: 'Subscription', path: '/billing', icon: CreditCard },
  ];

  const handleTriggerQuickQuiz = async () => {
    try {
      const quiz = await api.getPendingKnowledgeCheck();
      if (quiz) {
        setActiveQuiz(quiz);
        setIsQuizOpen(true);
      } else {
        alert('No pending knowledge checks right now! Ingest more educational short-form content to trigger new quizzes.');
      }
    } catch {
      alert('Unable to load quiz at this time.');
    }
  };

  const handleQuickFindUseful = async () => {
    setIsFindingUseful(true);
    try {
      const rec = await api.findSomethingUseful();
      if (rec && rec.url) {
        setUsefulRec(rec);
      } else {
        onNavigate('/recommendations');
      }
    } catch {
      onNavigate('/recommendations');
    } finally {
      setIsFindingUseful(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col antialiased selection:bg-blue-500/30 selection:text-blue-200">
      {/* Topbar */}
      <header className="h-16 border-b border-slate-850 bg-slate-900/60 backdrop-blur-md sticky top-0 z-40 px-6 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <div
            onClick={() => onNavigate('/')}
            className="flex items-center gap-2.5 cursor-pointer select-none group"
          >
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/20 group-hover:scale-105 transition-transform">
              <Shield className="w-5 h-5" />
            </div>
            <div>
              <span className="font-semibold text-base tracking-tight text-white block leading-none">
                Scroll Guardian
              </span>
              <span className="text-[11px] text-slate-400 font-medium">Intentional Digital Habits</span>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* Action Buttons */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleTriggerQuickQuiz}
            icon={BookOpen}
            className="hidden sm:inline-flex text-xs border-slate-750 hover:bg-slate-800"
          >
            Quiz Check
          </Button>

          <Button
            variant="primary"
            size="sm"
            onClick={handleQuickFindUseful}
            isLoading={isFindingUseful}
            icon={Sparkles}
            className="text-xs bg-blue-600 hover:bg-blue-500 shadow-sm"
          >
            Find Something Useful
          </Button>

          {/* User Profile Header */}
          <div className="h-5 w-px bg-slate-800 mx-1 hidden sm:block" />

          <div className="flex items-center gap-2.5 pl-1">
            <div className="text-right hidden md:block">
              <div className="text-xs font-semibold text-slate-200">{user?.fullName || user?.email}</div>
              <div className="text-[10px] text-slate-400 flex items-center gap-1 justify-end">
                <span>{user?.plan === 2 ? 'Pro Plan' : 'Free Plan'}</span>
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 inline-block" />
              </div>
            </div>

            <Button
              variant="ghost"
              size="sm"
              onClick={logout}
              className="text-slate-400 hover:text-rose-400 px-2"
              title="Log out"
            >
              <LogOut className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </header>

      {/* Main Layout Body */}
      <div className="flex-1 flex max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 gap-8">
        {/* Sidebar */}
        <aside className="w-60 shrink-0 hidden md:block">
          <nav className="space-y-1 sticky top-24">
            {navItems.map(item => {
              const Icon = item.icon;
              const isActive = currentPath === item.path;
              return (
                <button
                  key={item.path}
                  onClick={() => onNavigate(item.path)}
                  className={`w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all ${
                    isActive
                      ? 'bg-blue-600/15 text-blue-400 border border-blue-500/20 font-semibold'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
                  }`}
                >
                  <Icon className={`w-4 h-4 ${isActive ? 'text-blue-400' : 'text-slate-500'}`} />
                  <span>{item.label}</span>
                </button>
              );
            })}

            <div className="pt-6 border-t border-slate-850/80 mt-6">
              <div className="bg-gradient-to-b from-slate-900 to-slate-950 border border-slate-800 rounded-xl p-3.5 text-xs text-slate-400 space-y-2">
                <div className="flex items-center gap-1.5 text-slate-200 font-semibold">
                  <Zap className="w-3.5 h-3.5 text-amber-400" />
                  <span>Philosophy</span>
                </div>
                <p className="text-[11px] leading-relaxed text-slate-400">
                  "Make the next 10 minutes useful." Scroll Guardian respects your autonomy and helps you retain practical skills.
                </p>
              </div>
            </div>
          </nav>
        </aside>

        {/* Dynamic Page Content */}
        <main className="flex-1 min-w-0 pb-16">{children}</main>
      </div>

      {/* Knowledge Quiz Modal */}
      <KnowledgeCheckModal
        check={activeQuiz}
        isOpen={isQuizOpen}
        onClose={() => setIsQuizOpen(false)}
        onCompleted={() => {
          setIsQuizOpen(false);
          setActiveQuiz(null);
        }}
      />

      {/* Quick Recommendation Trigger Popup */}
      {usefulRec && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-fadeIn">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between">
              <Badge variant="blue">Real-Time Recommendation</Badge>
              <button
                onClick={() => setUsefulRec(null)}
                className="text-slate-400 hover:text-white text-xs font-semibold"
              >
                Close
              </button>
            </div>
            <div>
              <h3 className="text-lg font-bold text-white mb-1.5">{usefulRec.title}</h3>
              <p className="text-sm text-slate-400 leading-relaxed">{usefulRec.description}</p>
            </div>
            <div className="p-3 bg-slate-950 rounded-xl border border-blue-500/20 text-xs text-blue-200 space-y-1">
              <strong className="text-blue-400 block font-semibold">Why this recommendation:</strong>
              <p className="text-slate-300">{usefulRec.reasonDescription}</p>
            </div>
            <div className="flex items-center justify-end gap-3 pt-2">
              <Button variant="outline" size="sm" onClick={() => setUsefulRec(null)}>
                Dismiss
              </Button>
              <a
                href={usefulRec.url}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center justify-center font-medium rounded-lg text-sm px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white shadow-sm"
              >
                Open Learning Resource ↗
              </a>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
