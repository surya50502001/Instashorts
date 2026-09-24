import React, { useState, useEffect } from 'react';
import { Button, Card, Badge, Skeleton, EmptyState } from '../components/ui/Components';
import { ContentItemDetail, InterventionEvaluation } from '../types';
import {
  Radio,
  Send,
  Sparkles,
  ExternalLink,
  Clock,
  Play,
  CheckCircle2,
  AlertCircle,
  HelpCircle,
} from 'lucide-react';
import { api } from '../services/api';

const PRESET_SAMPLES = [
  {
    label: '.NET 8 Clean Architecture',
    url: 'https://www.youtube.com/shorts/dotNetCleanArch8',
    title: 'Clean Architecture in .NET 8 Web APIs',
    creator: 'Nick Chapsas',
    caption: 'Why you should separate Domain, Application, and Infrastructure with EF Core.',
  },
  {
    label: 'Docker Containers',
    url: 'https://www.youtube.com/shorts/dockerFastTips',
    title: 'Docker containerization essentials',
    creator: 'TechLead',
    caption: 'How containers isolate dependencies across environments.',
  },
  {
    label: 'Index Investing',
    url: 'https://www.youtube.com/shorts/indexInvesting101',
    title: 'Compound interest and index funds',
    creator: 'Ali Abdaal',
    caption: 'Why S&P 500 ETFs outperform 90% of stock pickers.',
  },
  {
    label: 'Comedy Pranks (Entertainment)',
    url: 'https://www.instagram.com/reel/funnyPrank2026',
    title: 'Hilarious street prank compilation',
    creator: 'LaughOutLoud',
    caption: 'Wait for the ending reaction lol',
  },
];

export const LiveStreamPage: React.FC = () => {
  const [items, setItems] = useState<ContentItemDetail[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [url, setUrl] = useState<string>('https://www.youtube.com/shorts/dotNetCleanArch8');
  const [title, setTitle] = useState<string>('Clean Architecture in .NET 8 Web APIs');
  const [creator, setCreator] = useState<string>('Nick Chapsas');
  const [caption, setCaption] = useState<string>('Why you should separate Domain, Application, and Infrastructure with EF Core.');
  const [timeSpent, setTimeSpent] = useState<number>(35);
  const [isIngesting, setIsIngesting] = useState<boolean>(false);
  const [latestAnalysis, setLatestAnalysis] = useState<any>(null);

  // Intervention simulation
  const [simulatedSessionMinutes, setSimulatedSessionMinutes] = useState<number>(0);
  const [activeIntervention, setActiveIntervention] = useState<InterventionEvaluation | null>(null);
  const [isEvaluating, setIsEvaluating] = useState<boolean>(false);

  const fetchItems = async () => {
    try {
      const data = await api.getContentItems(1, 20);
      setItems(data);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchItems();
  }, []);

  const handleApplyPreset = (sample: typeof PRESET_SAMPLES[0]) => {
    setUrl(sample.url);
    setTitle(sample.title);
    setCreator(sample.creator);
    setCaption(sample.caption);
  };

  const handleIngest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) return;

    setIsIngesting(true);
    setLatestAnalysis(null);

    try {
      const res = await api.ingestContentEvent({
        url: url.trim(),
        title: title.trim(),
        creator: creator.trim(),
        caption: caption.trim(),
        timeSpentSeconds: timeSpent,
        completionPercentage: 100,
      });

      setLatestAnalysis(res);
      await fetchItems();
    } catch (err: any) {
      alert(err.message || 'Ingestion failed');
    } finally {
      setIsIngesting(false);
    }
  };

  const handleSimulateHeartbeat = async (minutesToAdd: number) => {
    setIsEvaluating(true);
    try {
      const newMinutes = simulatedSessionMinutes + minutesToAdd;
      setSimulatedSessionMinutes(newMinutes);

      const res = await api.sendHeartbeat(undefined, minutesToAdd * 60, 0);
      if (res.triggerIntervention && res.intervention) {
        setActiveIntervention(res.intervention);
      }
    } catch (err: any) {
      console.error(err);
    } finally {
      setIsEvaluating(false);
    }
  };

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Header */}
      <div className="border-b border-slate-850 pb-5">
        <div className="flex items-center gap-2">
          <Badge variant="blue">Real-Time Ingestion Bridge</Badge>
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-white mt-2">
          Live Telemetry & Pipeline Simulator
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Test live short-form content ingestion, observe background AI topic classification, and simulate session interventions.
        </p>
      </div>

      {/* Simulator Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Ingest Form */}
        <Card className="border-slate-800 p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-semibold text-white">Ingest Short-Form Content</h3>
            <span className="text-xs text-slate-400">Reels / Shorts / TikTok</span>
          </div>

          {/* Presets */}
          <div className="flex flex-wrap gap-1.5">
            {PRESET_SAMPLES.map((s) => (
              <button
                key={s.label}
                type="button"
                onClick={() => handleApplyPreset(s)}
                className="text-[11px] px-2.5 py-1 bg-slate-800/80 hover:bg-slate-750 text-slate-300 rounded-md border border-slate-700 transition-colors"
              >
                + {s.label}
              </button>
            ))}
          </div>

          <form onSubmit={handleIngest} className="space-y-3 pt-2">
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1">Content URL</label>
              <input
                type="url"
                required
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                placeholder="https://www.youtube.com/shorts/..."
                className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-white placeholder-slate-500 focus:outline-none focus:border-blue-500"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Title</label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Video title"
                  className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-white placeholder-slate-500 focus:outline-none focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Creator</label>
                <input
                  type="text"
                  value={creator}
                  onChange={(e) => setCreator(e.target.value)}
                  placeholder="Creator name"
                  className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-white placeholder-slate-500 focus:outline-none focus:border-blue-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1">Caption / Transcript</label>
              <textarea
                rows={2}
                value={caption}
                onChange={(e) => setCaption(e.target.value)}
                placeholder="Description or key takeaways"
                className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-lg text-xs text-white placeholder-slate-500 focus:outline-none focus:border-blue-500"
              />
            </div>

            <div className="flex items-center justify-between pt-2">
              <div className="flex items-center gap-2 text-xs text-slate-400">
                <Clock className="w-4 h-4 text-slate-500" />
                <span>Watched {timeSpent}s</span>
              </div>

              <Button
                type="submit"
                variant="primary"
                size="sm"
                isLoading={isIngesting}
                icon={Send}
              >
                Send Event to Backend
              </Button>
            </div>
          </form>

          {/* Ingestion Response Notification */}
          {latestAnalysis && (
            <div className="p-4 rounded-xl bg-slate-950 border border-emerald-500/30 text-xs space-y-2 animate-fadeIn">
              <div className="flex items-center gap-2 text-emerald-400 font-semibold">
                <CheckCircle2 className="w-4 h-4" />
                <span>Ingested & Queued for AI Processing</span>
              </div>
              <div className="text-slate-400 text-[11px]">
                Event ID: <code className="text-slate-300">{latestAnalysis.eventId}</code>
              </div>
            </div>
          )}
        </Card>

        {/* Real-time Session & Intervention Simulator */}
        <Card className="border-slate-800 p-6 space-y-4 flex flex-col justify-between">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="text-base font-semibold text-white">Intervention Simulator</h3>
              <Badge variant="purple">Session Engine</Badge>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Test how Scroll Guardian calculates passive scrolling duration and triggers context-aware interventions.
            </p>
          </div>

          <div className="p-5 bg-slate-950 rounded-xl border border-slate-850 text-center space-y-3">
            <div className="text-xs uppercase font-semibold text-slate-500">Active Simulated Session</div>
            <div className="text-3xl font-extrabold text-blue-400 font-mono">
              {simulatedSessionMinutes} <span className="text-sm font-normal text-slate-400">minutes</span>
            </div>
            <div className="flex justify-center gap-2 pt-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => handleSimulateHeartbeat(15)}
                isLoading={isEvaluating}
              >
                + 15m Scroll
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => handleSimulateHeartbeat(30)}
                isLoading={isEvaluating}
              >
                + 30m Scroll
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setSimulatedSessionMinutes(0)}
              >
                Reset Session
              </Button>
            </div>
          </div>

          <div className="text-xs text-slate-500 leading-relaxed border-t border-slate-850 pt-3">
            Configured threshold: <strong>30 minutes (Balanced)</strong>. Once exceeded on non-educational content, the intervention modal will trigger.
          </div>
        </Card>
      </div>

      {/* Tracked Content Table */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-semibold text-white">Recent Ingested Content Items</h2>
          <Button variant="ghost" size="sm" onClick={fetchItems}>Refresh</Button>
        </div>

        {isLoading ? (
          <Skeleton className="h-32 w-full" />
        ) : items.length === 0 ? (
          <EmptyState
            icon={Radio}
            title="No content items tracked yet."
            description="Use the simulator above or the browser extension to ingest short-form video events."
          />
        ) : (
          <div className="space-y-3">
            {items.map((item) => (
              <Card key={item.id} className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div className="space-y-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <Badge variant={item.analysis?.category === 'Programming' || item.analysis?.category === 'Education' ? 'emerald' : 'slate'}>
                      {item.analysis?.category || 'Analyzing...'}
                    </Badge>
                    <span className="text-xs font-semibold text-slate-300">
                      {item.analysis?.primaryTopic || 'Pending'}
                    </span>
                    {item.analysis && item.analysis.educationalValue >= 50 && (
                      <span className="text-[10px] text-emerald-400 font-mono">
                        {item.analysis.educationalValue}/100 Educational Depth
                      </span>
                    )}
                  </div>
                  <div className="text-sm font-semibold text-white truncate max-w-xl">
                    {item.title}
                  </div>
                  <div className="text-xs text-slate-400 truncate max-w-xl">
                    {item.analysis?.summary || item.caption || 'No summary'}
                  </div>
                </div>

                <div className="flex items-center gap-3 shrink-0">
                  <a
                    href={item.url}
                    target="_blank"
                    rel="noreferrer"
                    className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition-colors"
                  >
                    <ExternalLink className="w-4 h-4" />
                  </a>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Intervention Modal Triggered in Simulator */}
      {activeIntervention && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-fadeIn">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between">
              <Badge variant="amber">Scroll Guardian Active Nudge</Badge>
              <span className="text-xs text-slate-400">{activeIntervention.sessionMinutes}m into session</span>
            </div>
            <div>
              <h3 className="text-lg font-bold text-white mb-1">{activeIntervention.messageTitle}</h3>
              <p className="text-sm text-slate-400 leading-relaxed">{activeIntervention.messageBody}</p>
            </div>
            <div className="flex flex-col gap-2 pt-2">
              <Button
                variant="primary"
                size="md"
                onClick={() => {
                  setActiveIntervention(null);
                  alert('Redirecting to personalized high-signal learning resource...');
                }}
              >
                {activeIntervention.callToActionText}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setActiveIntervention(null)}
              >
                Snooze 15 Minutes
              </Button>
              <button
                onClick={() => setActiveIntervention(null)}
                className="text-xs text-slate-500 hover:text-slate-400 py-1"
              >
                Continue Scrolling
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
