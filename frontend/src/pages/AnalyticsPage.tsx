import React, { useState, useEffect } from 'react';
import { Button, Card, Badge, Skeleton, EmptyState } from '../components/ui/Components';
import { DailyAnalytics, WeeklyReview } from '../types';
import {
  BarChart3,
  Calendar,
  Clock,
  BookOpen,
  PieChart,
  Repeat,
  Sparkles,
  TrendingUp,
  Layers,
} from 'lucide-react';
import { api } from '../services/api';

export const AnalyticsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'daily' | 'weekly'>('daily');
  const [dailyData, setDailyData] = useState<DailyAnalytics | null>(null);
  const [weeklyData, setWeeklyData] = useState<WeeklyReview | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const loadAnalytics = async () => {
      setIsLoading(true);
      try {
        if (activeTab === 'daily') {
          const data = await api.getDailyAnalytics();
          setDailyData(data);
        } else {
          const data = await api.getWeeklyReview();
          setWeeklyData(data);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    };
    loadAnalytics();
  }, [activeTab]);

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Header & Tabs */}
      <div className="border-b border-slate-850 pb-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white">Digital Diet Analytics</h1>
          <p className="text-sm text-slate-400 mt-1">
            Examine your short-form distribution, topic depth, and learning consistency over time.
          </p>
        </div>

        <div className="flex bg-slate-900 border border-slate-800 p-1 rounded-xl">
          <button
            onClick={() => setActiveTab('daily')}
            className={`px-4 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'daily'
                ? 'bg-blue-600 text-white shadow-sm'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            Daily Breakdown
          </button>
          <button
            onClick={() => setActiveTab('weekly')}
            className={`px-4 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'weekly'
                ? 'bg-blue-600 text-white shadow-sm'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            Weekly Review
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-36 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      ) : activeTab === 'daily' ? (
        /* Daily View */
        <div className="space-y-6">
          {/* Key Metrics */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Total Scroll Time</div>
              <div className="text-3xl font-extrabold text-white font-mono">
                {dailyData?.totalScrollMinutes || 0} <span className="text-xs font-normal text-slate-400">min</span>
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                Avg session: {dailyData?.averageSessionMinutes || 0} min
              </p>
            </Card>

            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-emerald-400 mb-1">Learning Time</div>
              <div className="text-3xl font-extrabold text-emerald-400 font-mono">
                {dailyData?.learningMinutes || 0} <span className="text-xs font-normal text-slate-400">min</span>
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                Goal-relevant: {dailyData?.goalRelevantMinutes || 0} min
              </p>
            </Card>

            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-amber-400 mb-1">Repetition Score</div>
              <div className="text-3xl font-extrabold text-amber-400 font-mono">
                {dailyData && dailyData.totalScrollMinutes > 0 ? `${dailyData.repetitionScore}%` : '—'}
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                Similarity to trending tropes
              </p>
            </Card>

            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-blue-400 mb-1">Quiz Accuracy</div>
              <div className="text-3xl font-extrabold text-blue-400 font-mono">
                {dailyData && dailyData.knowledgeChecksCompleted > 0 ? `${dailyData.knowledgeAccuracyRate}%` : '—'}
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                {dailyData?.knowledgeChecksCompleted || 0} checks completed today
              </p>
            </Card>
          </div>

          {/* Category Distribution Breakdown */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card className="p-6 space-y-4">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <h3 className="text-base font-semibold text-white">Category Distribution</h3>
                <PieChart className="w-4 h-4 text-slate-400" />
              </div>

              {!dailyData || Object.keys(dailyData.categoryDistributionMinutes).length === 0 ? (
                <p className="text-xs text-slate-500 py-6 text-center">No categories logged today.</p>
              ) : (
                <div className="space-y-3">
                  {Object.entries(dailyData.categoryDistributionMinutes).map(([cat, mins]) => {
                    const pct = dailyData.totalScrollMinutes > 0 ? Math.round((mins / dailyData.totalScrollMinutes) * 100) : 0;
                    return (
                      <div key={cat} className="space-y-1">
                        <div className="flex justify-between text-xs font-medium">
                          <span className="text-slate-300">{cat}</span>
                          <span className="text-slate-400">{mins}m ({pct}%)</span>
                        </div>
                        <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                          <div
                            className="bg-blue-500 h-full rounded-full"
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </Card>

            <Card className="p-6 space-y-4">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <h3 className="text-base font-semibold text-white">Topic Breakdown</h3>
                <Layers className="w-4 h-4 text-slate-400" />
              </div>

              {!dailyData || Object.keys(dailyData.topicDistributionMinutes).length === 0 ? (
                <p className="text-xs text-slate-500 py-6 text-center">No topics logged today.</p>
              ) : (
                <div className="space-y-3">
                  {Object.entries(dailyData.topicDistributionMinutes).map(([topic, mins]) => (
                    <div key={topic} className="flex items-center justify-between text-xs border-b border-slate-850/60 pb-2">
                      <span className="text-slate-200 font-medium">{topic}</span>
                      <span className="text-slate-400 font-mono">{mins} min</span>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          </div>
        </div>
      ) : (
        /* Weekly View */
        <div className="space-y-6">
          {/* Weekly Summary Metrics */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Total Short-Form Time</div>
              <div className="text-3xl font-extrabold text-white font-mono">
                {weeklyData?.totalScrollHours || 0} <span className="text-xs font-normal text-slate-400">hours</span>
              </div>
              <p className="text-[11px] text-slate-500 mt-1">This 7-day period</p>
            </Card>

            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-emerald-400 mb-1">Goal-Relevant Time</div>
              <div className="text-3xl font-extrabold text-emerald-400 font-mono">
                {weeklyData?.goalRelevantHours || 0} <span className="text-xs font-normal text-slate-400">hours</span>
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                {weeklyData?.learningHours || 0} hours educational content
              </p>
            </Card>

            <Card className="p-5">
              <div className="text-xs uppercase font-semibold text-blue-400 mb-1">Knowledge Accuracy</div>
              <div className="text-3xl font-extrabold text-blue-400 font-mono">
                {weeklyData && weeklyData.totalKnowledgeChecks > 0 ? `${weeklyData.averageAccuracy}%` : '—'}
              </div>
              <p className="text-[11px] text-slate-500 mt-1">
                {weeklyData?.totalKnowledgeChecks || 0} total knowledge checks
              </p>
            </Card>
          </div>

          {/* Dynamic Insights Box */}
          <Card className="border-blue-500/30 bg-slate-900/80 p-6 space-y-3">
            <div className="flex items-center gap-2 text-blue-400 font-semibold text-sm">
              <Sparkles className="w-4 h-4" />
              <span>Weekly Insights & Patterns</span>
            </div>
            <div className="space-y-2">
              {weeklyData?.dynamicInsights.map((insight, idx) => (
                <div key={idx} className="text-xs text-slate-300 leading-relaxed flex items-start gap-2">
                  <span className="text-blue-500 font-bold">•</span>
                  <span>{insight}</span>
                </div>
              ))}
            </div>
          </Card>

          {/* Daily 7-Day Chart Breakdown */}
          <Card className="p-6 space-y-4">
            <h3 className="text-base font-semibold text-white">Daily Consumption This Week</h3>
            <div className="grid grid-cols-7 gap-2 pt-4">
              {weeklyData?.dailyBreakdown.map((day) => {
                const heightPct = Math.min(100, Math.max(8, (day.scrollMinutes / 120) * 100));
                return (
                  <div key={day.date} className="flex flex-col items-center gap-2 text-center">
                    <div className="text-[11px] text-slate-400 font-mono">{day.scrollMinutes}m</div>
                    <div className="w-full bg-slate-800/80 rounded-t-lg h-32 flex items-end justify-center p-1">
                      <div
                        className="w-full bg-gradient-to-t from-blue-600 to-emerald-400 rounded-t-md transition-all"
                        style={{ height: `${heightPct}%` }}
                      />
                    </div>
                    <div className="text-xs font-semibold text-slate-300">{day.dayName.slice(0, 3)}</div>
                  </div>
                );
              })}
            </div>
          </Card>
        </div>
      )}
    </div>
  );
};
