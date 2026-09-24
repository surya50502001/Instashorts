import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button, Card, Badge, Modal, Skeleton } from '../components/ui/Components';
import { UserGoal, UserPreference } from '../types';
import {
  Sliders,
  Target,
  Shield,
  Download,
  Trash2,
  Plus,
  Lock,
  Check,
  AlertTriangle,
} from 'lucide-react';
import { api } from '../services/api';

export const SettingsPage: React.FC = () => {
  const { logout } = useAuth();
  const [goals, setGoals] = useState<UserGoal[]>([]);
  const [preferences, setPreferences] = useState<UserPreference | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // New goal modal
  const [isAddGoalOpen, setIsAddGoalOpen] = useState<boolean>(false);
  const [newGoalCategory, setNewGoalCategory] = useState<string>('Programming');
  const [newGoalTitle, setNewGoalTitle] = useState<string>('');
  const [newGoalDesc, setNewGoalDesc] = useState<string>('');
  const [newGoalHours, setNewGoalHours] = useState<number>(3.5);
  const [isSavingGoal, setIsSavingGoal] = useState<boolean>(false);

  // Delete account modal
  const [isDeleteAccountOpen, setIsDeleteAccountOpen] = useState<boolean>(false);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [goalsData, prefsData] = await Promise.all([
        api.getGoals(),
        api.getPreferences(),
      ]);
      setGoals(goalsData);
      setPreferences(prefsData);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreateGoal = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newGoalTitle.trim()) return;

    setIsSavingGoal(true);
    try {
      await api.createGoal({
        category: newGoalCategory,
        title: newGoalTitle.trim(),
        description: newGoalDesc.trim(),
        priority: goals.length + 1,
        targetWeeklyHours: newGoalHours,
      });
      setIsAddGoalOpen(false);
      setNewGoalTitle('');
      setNewGoalDesc('');
      await loadData();
    } catch (err: any) {
      alert(err.message || 'Failed to create goal');
    } finally {
      setIsSavingGoal(false);
    }
  };

  const handleDeleteGoal = async (id: string) => {
    if (!confirm('Are you sure you want to remove this goal?')) return;
    try {
      await api.deleteGoal(id);
      setGoals(goals.filter(g => g.id !== id));
    } catch (err: any) {
      alert(err.message || 'Failed to delete goal');
    }
  };

  const handleUpdatePreferences = async (updates: Partial<UserPreference>) => {
    if (!preferences) return;
    try {
      const updated = await api.updatePreferences(updates);
      setPreferences(updated);
    } catch (err: any) {
      alert(err.message || 'Failed to update preferences');
    }
  };

  const handleExportData = async () => {
    try {
      const exportData = await api.exportAllData();
      const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
      const downloadUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = downloadUrl;
      a.download = `scrollguardian-export-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
    } catch (err: any) {
      alert(err.message || 'Failed to export data');
    }
  };

  const handlePurgeHistory = async () => {
    if (!confirm('Are you sure you want to purge your past activity history? This cannot be undone.')) return;
    try {
      await api.purgeHistory();
      alert('Activity history purged successfully.');
    } catch (err: any) {
      alert(err.message || 'Failed to purge history');
    }
  };

  const handleDeleteAccount = async () => {
    setIsDeleting(true);
    try {
      await api.deleteAccount();
      await logout();
      window.location.href = '/login';
    } catch (err: any) {
      alert(err.message || 'Failed to delete account');
      setIsDeleting(false);
    }
  };

  if (isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }

  return (
    <div className="space-y-10 animate-fadeIn max-w-4xl">
      {/* Header */}
      <div className="border-b border-slate-850 pb-5">
        <h1 className="text-2xl font-bold tracking-tight text-white">Settings & Privacy Center</h1>
        <p className="text-sm text-slate-400 mt-1">
          Manage your personal goals, intervention intensity, and complete data sovereignty.
        </p>
      </div>

      {/* Goals Management Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-semibold text-white">Active Goals</h2>
            <p className="text-xs text-slate-400">Scroll Guardian uses your active goals to align recommendations and retention checks.</p>
          </div>
          <Button
            variant="primary"
            size="sm"
            onClick={() => setIsAddGoalOpen(true)}
            icon={Plus}
          >
            Add Goal
          </Button>
        </div>

        <div className="space-y-3">
          {goals.map((goal) => (
            <Card key={goal.id} className="p-4 flex items-center justify-between border-slate-800">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <Badge variant="blue">{goal.category}</Badge>
                  <span className="text-xs text-slate-400">Target: {goal.targetWeeklyHours}h / week</span>
                </div>
                <h4 className="text-sm font-semibold text-white">{goal.title}</h4>
                {goal.description && <p className="text-xs text-slate-400">{goal.description}</p>}
              </div>

              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleDeleteGoal(goal.id)}
                className="text-slate-500 hover:text-rose-400"
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            </Card>
          ))}
        </div>
      </div>

      {/* Intervention Frequency & Aggressiveness */}
      <div className="space-y-4 border-t border-slate-850 pt-8">
        <div>
          <h2 className="text-base font-semibold text-white">Intervention Controls</h2>
          <p className="text-xs text-slate-400">Configure how often Scroll Guardian prompts you to shift to high-signal learning.</p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {[
            { level: 1, title: 'Gentle', desc: '45m intervals' },
            { level: 2, title: 'Balanced', desc: '30m intervals (Recommended)' },
            { level: 3, title: 'Proactive', desc: '15m intervals' },
          ].map((opt) => (
            <button
              key={opt.level}
              type="button"
              onClick={() => handleUpdatePreferences({ interventionAggressiveness: opt.level })}
              className={`p-4 rounded-xl border text-left transition-all ${
                preferences?.interventionAggressiveness === opt.level
                  ? 'border-blue-500/50 bg-blue-600/15 text-blue-200'
                  : 'border-slate-800 bg-slate-900/50 hover:border-slate-700 text-slate-300'
              }`}
            >
              <div className="text-sm font-semibold text-white">{opt.title}</div>
              <div className="text-xs text-slate-400 mt-1">{opt.desc}</div>
            </button>
          ))}
        </div>

        <div className="p-4 bg-slate-900/50 rounded-xl border border-slate-800 space-y-3">
          <label className="flex items-center justify-between text-xs text-slate-300 cursor-pointer">
            <span>Enable Real-Time Floating HUD & Interventions</span>
            <input
              type="checkbox"
              checked={preferences?.isInterventionEnabled ?? true}
              onChange={(e) => handleUpdatePreferences({ isInterventionEnabled: e.target.checked })}
              className="rounded border-slate-700 text-blue-600 focus:ring-blue-500 bg-slate-900"
            />
          </label>

          <label className="flex items-center justify-between text-xs text-slate-300 cursor-pointer">
            <span>Enable Telemetry & Consumption Tracking</span>
            <input
              type="checkbox"
              checked={preferences?.isTrackingEnabled ?? true}
              onChange={(e) => handleUpdatePreferences({ isTrackingEnabled: e.target.checked })}
              className="rounded border-slate-700 text-blue-600 focus:ring-blue-500 bg-slate-900"
            />
          </label>
        </div>
      </div>

      {/* Privacy & Data Sovereignty Section */}
      <div className="space-y-4 border-t border-slate-850 pt-8">
        <div>
          <h2 className="text-base font-semibold text-white">Privacy & Data Governance</h2>
          <p className="text-xs text-slate-400">Full exportability and immediate deletion controls.</p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Card className="p-5 space-y-3">
            <div className="flex items-center gap-2 text-white font-semibold text-sm">
              <Download className="w-4 h-4 text-blue-400" />
              <span>Export All Data (JSON)</span>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Download your complete telemetry history, recorded knowledge check answers, and analytics summary.
            </p>
            <Button variant="outline" size="sm" onClick={handleExportData}>
              Download JSON Export
            </Button>
          </Card>

          <Card className="p-5 space-y-3">
            <div className="flex items-center gap-2 text-rose-400 font-semibold text-sm">
              <Trash2 className="w-4 h-4" />
              <span>Purge Activity History</span>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Permanently wipe all past video consumption events while keeping your goals and account intact.
            </p>
            <Button variant="danger" size="sm" onClick={handlePurgeHistory}>
              Purge History
            </Button>
          </Card>
        </div>

        <div className="p-5 rounded-xl border border-rose-500/20 bg-rose-950/10 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h4 className="text-sm font-semibold text-rose-300">Delete Account & All Associated Data</h4>
            <p className="text-xs text-slate-400 mt-0.5">
              Permanently delete your profile, subscription, goals, and telemetry.
            </p>
          </div>
          <Button variant="danger" size="sm" onClick={() => setIsDeleteAccountOpen(true)}>
            Delete Account
          </Button>
        </div>
      </div>

      {/* Add Goal Modal */}
      <Modal
        isOpen={isAddGoalOpen}
        onClose={() => setIsAddGoalOpen(false)}
        title="Add New Learning Goal"
        description="Set a new focus domain and target hours."
      >
        <form onSubmit={handleCreateGoal} className="space-y-4 pt-2">
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Category</label>
            <select
              value={newGoalCategory}
              onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setNewGoalCategory(e.target.value)}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:border-blue-500"
            >
              <option value="Programming">Programming</option>
              <option value="Career">Career</option>
              <option value="Finance">Finance</option>
              <option value="Fitness">Fitness</option>
              <option value="Science">Science</option>
              <option value="Education">Education</option>
              <option value="Business">Business</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Goal Title</label>
            <input
              type="text"
              required
              value={newGoalTitle}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setNewGoalTitle(e.target.value)}
              placeholder="e.g. Master ASP.NET Core Clean Architecture"
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Description</label>
            <textarea
              rows={2}
              value={newGoalDesc}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setNewGoalDesc(e.target.value)}
              placeholder="Context or specific targets"
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Target Weekly Hours: {newGoalHours}h</label>
            <input
              type="range"
              min={1}
              max={20}
              step={0.5}
              value={newGoalHours}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setNewGoalHours(parseFloat(e.target.value))}
              className="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-blue-500"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" size="sm" type="button" onClick={() => setIsAddGoalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" type="submit" isLoading={isSavingGoal}>
              Create Goal
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Account Confirmation Modal */}
      <Modal
        isOpen={isDeleteAccountOpen}
        onClose={() => setIsDeleteAccountOpen(false)}
        title="Permanently Delete Account"
        description="This action cannot be undone. All your history and profile data will be purged immediately."
      >
        <div className="space-y-4 pt-2">
          <p className="text-xs text-rose-300 leading-relaxed">
            By proceeding, all telemetry events, quizzes, recommendations, and personal identifiers will be wiped from our databases.
          </p>

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" size="sm" onClick={() => setIsDeleteAccountOpen(false)}>
              Cancel
            </Button>
            <Button variant="danger" size="sm" onClick={handleDeleteAccount} isLoading={isDeleting}>
              Confirm Permanent Deletion
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
