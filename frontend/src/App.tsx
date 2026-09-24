import React, { useState, useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { AuthPage } from './pages/AuthPages';
import { OnboardingPage } from './pages/OnboardingPage';
import { DashboardLayout } from './layouts/DashboardLayout';
import { DashboardPage } from './pages/DashboardPage';
import { LiveStreamPage } from './pages/LiveStreamPage';
import { KnowledgeMapPage } from './pages/KnowledgeMapPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { RecommendationsPage } from './pages/RecommendationsPage';
import { SettingsPage } from './pages/SettingsPage';
import { BillingPage } from './pages/BillingPage';
import { Skeleton } from './components/ui/Components';

const AppContent: React.FC = () => {
  const { user, isLoading, isAuthenticated } = useAuth();
  const [currentPath, setCurrentPath] = useState<string>(window.location.pathname || '/');

  useEffect(() => {
    const handlePopState = () => {
      setCurrentPath(window.location.pathname || '/');
    };
    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  const navigate = (path: string) => {
    window.history.pushState({}, '', path);
    setCurrentPath(path);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-600 animate-pulse" />
          <span className="text-xs font-semibold text-slate-400">Loading Scroll Guardian...</span>
        </div>
      </div>
    );
  }

  // Unauthenticated routing
  if (!isAuthenticated) {
    if (currentPath === '/register') {
      return <AuthPage onNavigate={navigate} isRegister={true} />;
    }
    return <AuthPage onNavigate={navigate} isRegister={false} />;
  }

  // User not yet onboarded
  if (user && !user.isOnboarded) {
    return <OnboardingPage onCompleted={() => navigate('/')} />;
  }

  // Authenticated and onboarded app
  return (
    <DashboardLayout currentPath={currentPath} onNavigate={navigate}>
      {currentPath === '/' && <DashboardPage onNavigate={navigate} />}
      {currentPath === '/stream' && <LiveStreamPage />}
      {currentPath === '/knowledge' && <KnowledgeMapPage />}
      {currentPath === '/analytics' && <AnalyticsPage />}
      {currentPath === '/recommendations' && <RecommendationsPage />}
      {currentPath === '/settings' && <SettingsPage />}
      {currentPath === '/billing' && <BillingPage />}
    </DashboardLayout>
  );
};

export function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;
