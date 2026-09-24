import React, { useState, useEffect } from 'react';
import { Button, Card, Badge, Skeleton, EmptyState } from '../components/ui/Components';
import { RecommendationResponse } from '../types';
import {
  Compass,
  Sparkles,
  ExternalLink,
  Bookmark,
  X,
  Target,
  BookOpen,
} from 'lucide-react';
import { api } from '../services/api';

export const RecommendationsPage: React.FC = () => {
  const [recommendations, setRecommendations] = useState<RecommendationResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isFindingUseful, setIsFindingUseful] = useState<boolean>(false);
  const [savedIds, setSavedIds] = useState<string[]>([]);

  const fetchRecommendations = async () => {
    setIsLoading(true);
    try {
      const data = await api.getRecommendations(8);
      setRecommendations(data);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchRecommendations();
  }, []);

  const handleFindSomethingUseful = async () => {
    setIsFindingUseful(true);
    try {
      const rec = await api.findSomethingUseful();
      if (rec && rec.url) {
        // Prepend to list if not present
        if (!recommendations.some(r => r.id === rec.id)) {
          setRecommendations([rec, ...recommendations]);
        }
        window.open(rec.url, '_blank');
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsFindingUseful(false);
    }
  };

  const handleFeedback = async (id: string, type: number) => {
    try {
      await api.recordRecommendationFeedback(id, type);
      if (type === 4) { // Dismiss
        setRecommendations(recommendations.filter(r => r.id !== id));
      } else if (type === 3) { // Save
        setSavedIds([...savedIds, id]);
      }
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Header */}
      <div className="border-b border-slate-850 pb-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <Badge variant="blue">Goal-Aligned Discovery</Badge>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-white mt-2">
            Personalized Recommendations
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Transform passive browsing intervals into high-signal learning opportunities.
          </p>
        </div>

        <Button
          variant="primary"
          size="md"
          onClick={handleFindSomethingUseful}
          isLoading={isFindingUseful}
          icon={Sparkles}
        >
          Find Something Useful Now ↗
        </Button>
      </div>

      {/* Recommendation List */}
      <div className="space-y-4">
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Skeleton className="h-44 w-full" />
            <Skeleton className="h-44 w-full" />
          </div>
        ) : recommendations.length === 0 ? (
          <EmptyState
            icon={Compass}
            title="No recommendations generated yet."
            description="Configure your learning goals in Settings to unlock tailored recommendations."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            {recommendations.map((rec) => {
              const isSaved = savedIds.includes(rec.id);
              return (
                <Card
                  key={rec.id}
                  className="p-6 flex flex-col justify-between space-y-4 hover:border-slate-700 transition-all border-slate-800"
                >
                  <div className="space-y-2.5">
                    <div className="flex items-center justify-between">
                      <Badge variant="emerald">{rec.sourceType}</Badge>
                      <span className="text-xs text-slate-500 font-mono">
                        {rec.relevanceScore}% Match
                      </span>
                    </div>

                    <h3 className="text-base font-bold text-white leading-snug">
                      {rec.title}
                    </h3>

                    <p className="text-xs text-slate-400 leading-relaxed">
                      {rec.description}
                    </p>

                    <div className="p-3 bg-slate-950/80 rounded-xl border border-slate-800 text-xs text-blue-300/90 leading-relaxed">
                      <strong className="text-blue-400 block mb-0.5">Why this recommendation:</strong>
                      {rec.reasonDescription}
                    </div>
                  </div>

                  <div className="flex items-center justify-between pt-3 border-t border-slate-800/80">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => handleFeedback(rec.id, 3)}
                        className={`p-2 rounded-lg border text-xs transition-colors ${
                          isSaved
                            ? 'border-emerald-500/40 bg-emerald-950/40 text-emerald-300'
                            : 'border-slate-800 text-slate-400 hover:text-white hover:bg-slate-800'
                        }`}
                        title="Save for later"
                      >
                        <Bookmark className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => handleFeedback(rec.id, 4)}
                        className="p-2 rounded-lg border border-slate-800 text-slate-400 hover:text-rose-400 hover:bg-slate-800 text-xs transition-colors"
                        title="Dismiss recommendation"
                      >
                        <X className="w-3.5 h-3.5" />
                      </button>
                    </div>

                    <a
                      href={rec.url}
                      target="_blank"
                      rel="noreferrer"
                      onClick={() => handleFeedback(rec.id, 2)}
                      className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-sm transition-all"
                    >
                      <span>Open Resource</span>
                      <ExternalLink className="w-3.5 h-3.5" />
                    </a>
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
