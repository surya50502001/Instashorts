import React, { useState, useEffect } from 'react';
import { DashboardSummary } from '../types';
import { Button, Card, Badge, Skeleton, EmptyState } from '../components/ui/Components';
import {
  Clock,
  BookOpen,
  Film,
  Target,
  TrendingUp,
  TrendingDown,
  ArrowRight,
  Sparkles,
  ExternalLink,
  ShieldCheck,
  Radio,
  Layers,
} from 'lucide-react';
import { api } from '../services/api';

interface DashboardPageProps {
  onNavigate: (path: string) => void;
}

export const DashboardPage: React.FC<DashboardPageProps> = ({ onNavigate }) => {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchSummary = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await api.getDashboardSummary();
        setSummary(data);
      } catch (err: any) {
        setError(err.message || 'Failed to load dashboard metrics');
      } finally {
        setIsLoading(false);
      }
    };
    fetchSummary();
  }, []);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-40 w-full" />
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
        </div>
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-rose-900/50 bg-rose-950/20 text-center py-10">
        <p className="text-rose-300 text-sm mb-4">{error}</p>
        <Button variant="outline" size="sm" onClick={() => window.location.reload()}>
          Retry Loading
        </Button>
      </Card>
    );
  }

  const hasActivity = summary && summary.itemsConsumedCount > 0;

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Top Header & Overview */}
      <div className="border-b border-slate-850 pb-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white">Your digital diet today</h1>
          <p className="text-sm text-slate-400 mt-1">
            Real telemetry and cognitive retention from your short-form consumption.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onNavigate('/stream')}
            icon={Radio}
            className="text-xs"
          >
            Live Ingestion Stream
          </Button>
        </div>
      </div>

      {/* Main Stats Row */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Active Scrolling */}
        <div className="bg-slate-900/80 border border-slate-800/80 rounded-2xl p-5 backdrop-blur-sm relative overflow-hidden group">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Active Scrolling</span>
            <Clock className="w-4 h-4 text-blue-400" />
          </div>
          <div className="text-3xl font-extrabold text-white font-mono tracking-tight">
            {summary?.activeScrollMinutes || 0}
            <span className="text-xs font-normal text-slate-400 ml-1">min</span>
          </div>
          <p className="text-[11px] text-slate-400 mt-1">
            {summary?.totalSessions || 0} active sessions logged
          </p>
        </div>

        {/* Learning Time */}
        <div className="bg-slate-900/80 border border-emerald-500/20 rounded-2xl p-5 backdrop-blur-sm relative overflow-hidden group">
          <div className="flex items-center justify-between text-emerald-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Learning Time</span>
            <BookOpen className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="text-3xl font-extrabold text-white font-mono tracking-tight">
            {summary?.learningMinutes || 0}
            <span className="text-xs font-normal text-slate-400 ml-1">min</span>
          </div>
          <p className="text-[11px] text-emerald-400/80 mt-1">
            {summary && summary.activeScrollMinutes > 0
              ? `${Math.round((summary.learningMinutes / summary.activeScrollMinutes) * 100)}% educational ratio`
              : 'Goal-aligned topics'}
          </p>
        </div>

        {/* Entertainment Time */}
        <div className="bg-slate-900/80 border border-slate-800/80 rounded-2xl p-5 backdrop-blur-sm relative overflow-hidden group">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Entertainment</span>
            <Film className="w-4 h-4 text-amber-400" />
          </div>
          <div className="text-3xl font-extrabold text-white font-mono tracking-tight">
            {summary?.entertainmentMinutes || 0}
            <span className="text-xs font-normal text-slate-400 ml-1">min</span>
          </div>
          <p className="text-[11px] text-slate-400 mt-1">
            Dominant category: <strong className="text-slate-300 font-medium">{summary?.dominantCategory || 'None'}</strong>
          </p>
        </div>

        {/* Goal-Relevant Time */}
        <div className="bg-slate-900/80 border border-indigo-500/20 rounded-2xl p-5 backdrop-blur-sm relative overflow-hidden group">
          <div className="flex items-center justify-between text-indigo-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Goal-Relevant</span>
            <Target className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="text-3xl font-extrabold text-white font-mono tracking-tight">
            {summary?.goalRelevantMinutes || 0}
            <span className="text-xs font-normal text-slate-400 ml-1">min</span>
          </div>
          <p className="text-[11px] text-indigo-300/80 mt-1">
            Direct match with active goals
          </p>
        </div>
      </div>

      {/* "What's changing?" Section */}
      <Card className="border-slate-800 bg-slate-900/50 p-6">
        <div className="flex items-center gap-2 mb-2">
          <span className="text-xs font-bold uppercase tracking-wider text-slate-400">What's Changing?</span>
        </div>
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="text-sm text-slate-300 max-w-2xl leading-relaxed">
            {summary?.trendComparisonText}
          </div>
          {summary?.learningMinutesChangePct !== undefined && summary.learningMinutesChangePct !== null && (
            <div className="flex items-center gap-2 shrink-0">
              <Badge variant={summary.learningMinutesChangePct >= 0 ? 'emerald' : 'amber'}>
                {summary.learningMinutesChangePct >= 0 ? (
                  <TrendingUp className="w-3.5 h-3.5 mr-1 text-emerald-400 inline" />
                ) : (
                  <TrendingDown className="w-3.5 h-3.5 mr-1 text-amber-400 inline" />
                )}
                {Math.abs(summary.learningMinutesChangePct)}% vs yesterday
              </Badge>
            </div>
          )}
        </div>
      </Card>

      {/* Recommended Next Step (One Strong Recommendation) */}
      {summary?.topRecommendation && (
        <Card className="border-blue-500/30 bg-gradient-to-r from-slate-900 via-blue-950/20 to-slate-900 p-6 relative overflow-hidden">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
            <div className="space-y-2 max-w-2xl">
              <div className="flex items-center gap-2">
                <Badge variant="blue">Recommended Next Step</Badge>
                <span className="text-xs text-slate-400">{summary.topRecommendation.sourceType}</span>
              </div>
              <h3 className="text-lg font-bold text-white tracking-tight">
                {summary.topRecommendation.title}
              </h3>
              <p className="text-xs text-slate-300 leading-relaxed">
                {summary.topRecommendation.description}
              </p>
              <div className="text-[11px] text-blue-300/90 pt-1">
                <strong>Why this recommendation:</strong> {summary.topRecommendation.reasonDescription}
              </div>
            </div>

            <div className="flex items-center gap-3 shrink-0">
              <a
                href={summary.topRecommendation.url}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-2 text-xs font-semibold px-4 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white transition-all shadow-sm"
              >
                <span>Open Resource</span>
                <ExternalLink className="w-3.5 h-3.5" />
              </a>
            </div>
          </div>
        </Card>
      )}

      {/* Today's Consumption Timeline vs Empty State */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Layers className="w-4 h-4 text-slate-400" />
            <h2 className="text-base font-semibold text-white">Today's Consumption Timeline</h2>
          </div>
          <span className="text-xs text-slate-500">
            {summary?.timeline.length || 0} items logged today
          </span>
        </div>

        {!hasActivity ? (
          <EmptyState
            icon={ShieldCheck}
            title="Your dashboard will appear here once Scroll Guardian has enough activity to analyze."
            description="Install the browser extension for automatic telemetry on Instagram Reels, YouTube Shorts, and TikTok, or test content ingestion using the Live Stream simulator."
            actionText="Open Live Stream Simulator"
            onAction={() => onNavigate('/stream')}
          />
        ) : (
          <div className="space-y-2.5">
            {summary.timeline.map((item) => (
              <div
                key={item.eventId}
                className="p-4 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-slate-700/80 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3"
              >
                <div className="space-y-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <Badge variant={item.category === 'Programming' || item.category === 'Education' ? 'emerald' : 'slate'}>
                      {item.category}
                    </Badge>
                    <span className="text-xs text-slate-500 font-medium">
                      {item.primaryTopic || 'General'}
                    </span>
                    {item.isGoalRelevant && (
                      <Badge variant="blue" className="text-[10px] py-0">Goal Aligned</Badge>
                    )}
                  </div>
                  <div className="text-sm font-medium text-slate-200 truncate">
                    {item.title}
                  </div>
                  <div className="text-xs text-slate-500">
                    By {item.creator} • {new Date(item.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>

                <div className="flex items-center gap-4 text-right shrink-0">
                  <div>
                    <div className="text-xs font-mono font-semibold text-slate-300">
                      {item.timeSpentSeconds}s watched
                    </div>
                    <div className="text-[10px] text-slate-500">
                      {item.educationalValue >= 50 ? 'High Signal' : 'Passive'}
                    </div>
                  </div>
                  <a
                    href={item.url}
                    target="_blank"
                    rel="noreferrer"
                    className="p-2 rounded-lg bg-slate-800/60 hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
                    title="View Content"
                  >
                    <ExternalLink className="w-3.5 h-3.5" />
                  </a>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
