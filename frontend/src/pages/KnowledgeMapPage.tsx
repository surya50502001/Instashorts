import React, { useState, useEffect } from 'react';
import { Button, Card, Badge, Skeleton, EmptyState } from '../components/ui/Components';
import { KnowledgeMapNode, RetentionSummary, KnowledgeCheckDetail } from '../types';
import { KnowledgeCheckModal } from '../components/ui/KnowledgeCheckModal';
import {
  Network,
  BookOpen,
  Award,
  Sparkles,
  CheckCircle2,
  Clock,
  ArrowRight,
  TrendingUp,
} from 'lucide-react';
import { api } from '../services/api';

export const KnowledgeMapPage: React.FC = () => {
  const [nodes, setNodes] = useState<KnowledgeMapNode[]>([]);
  const [summary, setSummary] = useState<RetentionSummary | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [activeQuiz, setActiveQuiz] = useState<KnowledgeCheckDetail | null>(null);
  const [isQuizOpen, setIsQuizOpen] = useState<boolean>(false);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const [mapData, summaryData] = await Promise.all([
        api.getKnowledgeMap(),
        api.getRetentionSummary(),
      ]);
      setNodes(mapData);
      setSummary(summaryData);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleStartQuiz = async () => {
    try {
      const quiz = await api.getPendingKnowledgeCheck();
      if (quiz) {
        setActiveQuiz(quiz);
        setIsQuizOpen(true);
      } else {
        alert('No pending knowledge checks right now! Ingest more educational short-form content to trigger new checks.');
      }
    } catch {
      alert('Unable to load quiz check.');
    }
  };

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Header */}
      <div className="border-b border-slate-850 pb-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <Badge variant="blue">Cognitive Retention Engine</Badge>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-white mt-2">
            Knowledge Map & Spaced Retention
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Visual topic taxonomy built strictly from educational content you consume and retain.
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={handleStartQuiz}
          icon={BookOpen}
        >
          Take Spaced Quiz Check
        </Button>
      </div>

      {/* Retention Overview Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="p-5">
          <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Learning Retention</div>
          <div className="text-3xl font-extrabold text-white font-mono">
            {summary && summary.totalQuestionsAnswered > 0 ? `${summary.overallRetentionRate}%` : '—'}
          </div>
          <p className="text-[11px] text-slate-500 mt-1">
            {summary && summary.totalQuestionsAnswered > 0 ? 'Verified through active recall' : 'Not enough data yet'}
          </p>
        </Card>

        <Card className="p-5">
          <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Questions Answered</div>
          <div className="text-3xl font-extrabold text-blue-400 font-mono">
            {summary?.totalQuestionsAnswered || 0}
          </div>
          <p className="text-[11px] text-slate-500 mt-1">
            {summary?.totalQuestionsCorrect || 0} correct answers
          </p>
        </Card>

        <Card className="p-5">
          <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Mastered Topics</div>
          <div className="text-3xl font-extrabold text-emerald-400 font-mono">
            {summary?.masteredTopicsCount || 0}
          </div>
          <p className="text-[11px] text-slate-500 mt-1">≥75% accuracy rate</p>
        </Card>

        <Card className="p-5">
          <div className="text-xs uppercase font-semibold text-slate-400 mb-1">Developing Topics</div>
          <div className="text-3xl font-extrabold text-amber-400 font-mono">
            {summary?.developingTopicsCount || 0}
          </div>
          <p className="text-[11px] text-slate-500 mt-1">Active learning focus</p>
        </Card>
      </div>

      {/* Retention Message */}
      {summary?.retentionStatusMessage && (
        <Card className="border-blue-500/20 bg-blue-950/15 p-4 text-xs text-blue-300 flex items-center gap-3">
          <Sparkles className="w-4 h-4 text-blue-400 shrink-0" />
          <span>{summary.retentionStatusMessage}</span>
        </Card>
      )}

      {/* Visual Knowledge Map Nodes */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-semibold text-white">Your Topic Knowledge Taxonomy</h2>
          <span className="text-xs text-slate-400">{nodes.length} topics tracked</span>
        </div>

        {isLoading ? (
          <Skeleton className="h-48 w-full" />
        ) : nodes.length === 0 ? (
          <EmptyState
            icon={Network}
            title="Knowledge Map will appear here once you consume educational content."
            description="As you watch programming, finance, science, or educational videos, Scroll Guardian extracts technical topics and adds them to your map."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {nodes.map((node) => {
              const isMastered = node.masteryScore >= 75.0 && node.knowledgeChecksAttempted >= 2;
              return (
                <Card
                  key={node.topicId}
                  className={`p-5 transition-all space-y-4 border ${
                    isMastered ? 'border-emerald-500/30 hover:border-emerald-500/50' : 'border-slate-800 hover:border-slate-700'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <Badge variant={node.category === 'Programming' ? 'blue' : 'emerald'}>
                        {node.category}
                      </Badge>
                      <h3 className="text-base font-bold text-white mt-1.5">{node.topicName}</h3>
                      {node.parentTopicName && (
                        <p className="text-[11px] text-slate-500">
                          Branch of: <span className="text-slate-400 font-medium">{node.parentTopicName}</span>
                        </p>
                      )}
                    </div>

                    <div className="text-right">
                      <span className="text-lg font-bold font-mono text-emerald-400">
                        {node.masteryScore}%
                      </span>
                      <span className="text-[10px] text-slate-500 block">Mastery</span>
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="w-full bg-slate-800/80 rounded-full h-1.5 overflow-hidden">
                    <div
                      className="bg-gradient-to-r from-blue-500 to-emerald-400 h-full rounded-full transition-all duration-500"
                      style={{ width: `${Math.min(100, Math.max(5, node.masteryScore))}%` }}
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-slate-800 text-slate-400">
                    <div>
                      <span className="text-slate-500 block text-[10px]">Content Consumed</span>
                      <strong className="text-slate-200">{node.contentConsumedCount} items</strong>
                    </div>
                    <div>
                      <span className="text-slate-500 block text-[10px]">Knowledge Checks</span>
                      <strong className="text-slate-200">
                        {node.knowledgeChecksCorrect}/{node.knowledgeChecksAttempted} correct
                      </strong>
                    </div>
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      {/* Spaced Quiz Modal */}
      <KnowledgeCheckModal
        check={activeQuiz}
        isOpen={isQuizOpen}
        onClose={() => setIsQuizOpen(false)}
        onCompleted={() => {
          setIsQuizOpen(false);
          setActiveQuiz(null);
          fetchData();
        }}
      />
    </div>
  );
};
