import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button, Card, Badge } from '../components/ui/Components';
import {
  Sparkles,
  Target,
  Clock,
  ShieldCheck,
  Check,
  ArrowRight,
  ArrowLeft,
  Lock,
  Cpu,
  Server,
  Trash2,
} from 'lucide-react';
import { api } from '../services/api';

interface OnboardingPageProps {
  onCompleted: () => void;
}

const CATEGORY_OPTIONS = [
  'Programming',
  'Career',
  'Finance',
  'Fitness',
  'Science',
  'Education',
  'Business',
  'Music',
  'Creativity',
  'General Knowledge',
];

export const OnboardingPage: React.FC<OnboardingPageProps> = ({ onCompleted }) => {
  const { refreshProfile } = useAuth();
  const [step, setStep] = useState<number>(1);
  const [selectedCategories, setSelectedCategories] = useState<string[]>(['Programming', 'Career']);
  const [primaryGoalText, setPrimaryGoalText] = useState<string>(
    'I want to become a better .NET developer and prepare for product-company interviews.'
  );
  const [dailyMinutes, setDailyMinutes] = useState<number>(45);
  const [aggressiveness, setAggressiveness] = useState<number>(2); // 1 = Gentle, 2 = Balanced, 3 = Proactive
  const [consentTelemetry, setConsentTelemetry] = useState<boolean>(true);
  const [consentAi, setConsentAi] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const toggleCategory = (cat: string) => {
    if (selectedCategories.includes(cat)) {
      if (selectedCategories.length > 1) {
        setSelectedCategories(selectedCategories.filter(c => c !== cat));
      }
    } else {
      setSelectedCategories([...selectedCategories, cat]);
    }
  };

  const handleFinish = async () => {
    setIsSubmitting(true);
    setError(null);
    try {
      await api.submitOnboarding({
        selectedCategories,
        primaryGoalText: primaryGoalText.trim(),
        estimatedDailyMinutes: dailyMinutes,
        aggressiveness,
        consentToTelemetry: consentTelemetry,
        consentToAiAnalysis: consentAi,
      });
      await refreshProfile();
      onCompleted();
    } catch (err: any) {
      setError(err.message || 'Failed to complete onboarding');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-2xl space-y-6">
        {/* Step Indicator */}
        <div className="flex items-center justify-between px-2 mb-2">
          <span className="text-xs font-semibold text-blue-400 uppercase tracking-wider">
            Step {step} of 5
          </span>
          <div className="flex gap-1.5">
            {[1, 2, 3, 4, 5].map((s) => (
              <div
                key={s}
                className={`h-1.5 rounded-full transition-all duration-300 ${
                  s === step ? 'w-8 bg-blue-500' : s < step ? 'w-4 bg-blue-700' : 'w-4 bg-slate-800'
                }`}
              />
            ))}
          </div>
        </div>

        <Card className="border-slate-800 bg-slate-900/90 p-8 shadow-2xl backdrop-blur-xl">
          {error && (
            <div className="mb-6 p-3.5 rounded-lg bg-rose-950/40 border border-rose-500/30 text-rose-300 text-xs">
              {error}
            </div>
          )}

          {/* Step 1: Categories */}
          {step === 1 && (
            <div className="space-y-6 animate-fadeIn">
              <div>
                <h2 className="text-xl font-bold text-white mb-2">What are you trying to improve?</h2>
                <p className="text-sm text-slate-400">
                  Select the domains you care about. Scroll Guardian uses this to identify high-signal content in your feed.
                </p>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {CATEGORY_OPTIONS.map((cat) => {
                  const isSelected = selectedCategories.includes(cat);
                  return (
                    <button
                      key={cat}
                      type="button"
                      onClick={() => toggleCategory(cat)}
                      className={`p-3.5 rounded-xl border text-left text-sm font-medium transition-all flex items-center justify-between ${
                        isSelected
                          ? 'border-blue-500/50 bg-blue-600/15 text-blue-200'
                          : 'border-slate-800 bg-slate-950/50 hover:border-slate-700 text-slate-300'
                      }`}
                    >
                      <span>{cat}</span>
                      {isSelected && <Check className="w-4 h-4 text-blue-400" />}
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Step 2: Natural-Language Goals */}
          {step === 2 && (
            <div className="space-y-6 animate-fadeIn">
              <div>
                <h2 className="text-xl font-bold text-white mb-2">What are your current goals?</h2>
                <p className="text-sm text-slate-400">
                  Describe what you want to achieve in natural language. Our semantic engine matches Reels and Shorts to these topics.
                </p>
              </div>

              <div className="space-y-2">
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
                  Your Primary Learning Goal
                </label>
                <textarea
                  rows={4}
                  value={primaryGoalText}
                  onChange={(e) => setPrimaryGoalText(e.target.value)}
                  placeholder="Example: I want to become a better .NET developer and prepare for product-company interviews."
                  className="w-full p-4 bg-slate-950/80 border border-slate-800 rounded-xl text-sm text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 leading-relaxed"
                />
                <p className="text-[11px] text-slate-500">
                  You can refine or add multiple specialized goals anytime in settings.
                </p>
              </div>
            </div>
          )}

          {/* Step 3: Consumption Estimate */}
          {step === 3 && (
            <div className="space-y-6 animate-fadeIn">
              <div>
                <h2 className="text-xl font-bold text-white mb-2">How much short-form content do you normally consume?</h2>
                <p className="text-sm text-slate-400">
                  Provide an estimate of your typical daily viewing time on Reels, Shorts, and TikTok.
                </p>
              </div>

              <div className="p-6 bg-slate-950/60 rounded-2xl border border-slate-800 text-center space-y-4">
                <div className="text-4xl font-extrabold text-blue-400 font-mono">
                  {dailyMinutes} <span className="text-lg font-normal text-slate-400">min/day</span>
                </div>

                <input
                  type="range"
                  min={10}
                  max={240}
                  step={5}
                  value={dailyMinutes}
                  onChange={(e) => setDailyMinutes(parseInt(e.target.value))}
                  className="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-blue-500"
                />

                <div className="flex justify-between text-xs text-slate-500">
                  <span>10 mins</span>
                  <span>1 hour</span>
                  <span>2 hours</span>
                  <span>4 hours</span>
                </div>
              </div>
            </div>
          )}

          {/* Step 4: Intervention Aggressiveness */}
          {step === 4 && (
            <div className="space-y-6 animate-fadeIn">
              <div>
                <h2 className="text-xl font-bold text-white mb-2">How aggressive should Scroll Guardian be?</h2>
                <p className="text-sm text-slate-400">
                  Configure how quickly the floating HUD prompts you to pivot when passive scrolling is detected.
                </p>
              </div>

              <div className="space-y-3">
                {[
                  {
                    level: 1,
                    title: 'Gentle',
                    desc: 'Checks in after 45 minutes of passive scrolling. Soft nudges without urgency.',
                  },
                  {
                    level: 2,
                    title: 'Balanced (Recommended)',
                    desc: 'Checks in after 30 minutes. Offers goal-relevant resources or quick retention checks.',
                  },
                  {
                    level: 3,
                    title: 'Proactive',
                    desc: 'Checks in after 15 minutes. Maximizes intentionality and active skill retention.',
                  },
                ].map((opt) => (
                  <button
                    key={opt.level}
                    type="button"
                    onClick={() => setAggressiveness(opt.level)}
                    className={`w-full p-4 rounded-xl border text-left transition-all flex items-start gap-4 ${
                      aggressiveness === opt.level
                        ? 'border-blue-500/50 bg-blue-600/15'
                        : 'border-slate-800 bg-slate-950/50 hover:border-slate-700'
                    }`}
                  >
                    <div
                      className={`w-5 h-5 rounded-full border flex items-center justify-center mt-0.5 ${
                        aggressiveness === opt.level
                          ? 'border-blue-500 bg-blue-500 text-white'
                          : 'border-slate-700 bg-slate-900'
                      }`}
                    >
                      {aggressiveness === opt.level && <Check className="w-3 h-3 stroke-[3]" />}
                    </div>
                    <div>
                      <div className="text-sm font-semibold text-white">{opt.title}</div>
                      <div className="text-xs text-slate-400 mt-0.5">{opt.desc}</div>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 5: Privacy & Architecture Transparency */}
          {step === 5 && (
            <div className="space-y-6 animate-fadeIn">
              <div>
                <h2 className="text-xl font-bold text-white mb-2">Privacy & Local Processing</h2>
                <p className="text-sm text-slate-400">
                  Scroll Guardian is designed for privacy. We collect only what is necessary to categorize educational content.
                </p>
              </div>

              <div className="space-y-3">
                <div className="p-3.5 bg-slate-950/70 border border-slate-800 rounded-xl flex items-start gap-3">
                  <Lock className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
                  <div className="text-xs text-slate-300 leading-relaxed">
                    <strong className="text-white block mb-0.5">What is collected:</strong>
                    Only URLs and text captions from supported video platforms (Reels/Shorts). No passwords, keystrokes, personal messages, or unrelated browsing history are ever captured.
                  </div>
                </div>

                <div className="p-3.5 bg-slate-950/70 border border-slate-800 rounded-xl flex items-start gap-3">
                  <Cpu className="w-5 h-5 text-blue-400 shrink-0 mt-0.5" />
                  <div className="text-xs text-slate-300 leading-relaxed">
                    <strong className="text-white block mb-0.5">Local Deduplication:</strong>
                    Content hashes are checked locally to prevent redundant server and AI requests.
                  </div>
                </div>

                <div className="p-3.5 bg-slate-950/70 border border-slate-800 rounded-xl flex items-start gap-3">
                  <Trash2 className="w-5 h-5 text-rose-400 shrink-0 mt-0.5" />
                  <div className="text-xs text-slate-300 leading-relaxed">
                    <strong className="text-white block mb-0.5">Your Complete Autonomy:</strong>
                    You can export all collected telemetry as JSON, purge past activity, or delete your entire account instantly with 1 click in settings.
                  </div>
                </div>
              </div>

              <div className="space-y-2 pt-2 border-t border-slate-800">
                <label className="flex items-center gap-2.5 text-xs text-slate-300 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={consentTelemetry}
                    onChange={(e) => setConsentTelemetry(e.target.checked)}
                    className="rounded border-slate-700 text-blue-600 focus:ring-blue-500 bg-slate-900"
                  />
                  <span>Allow telemetry logging on supported platforms (Reels & Shorts)</span>
                </label>

                <label className="flex items-center gap-2.5 text-xs text-slate-300 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={consentAi}
                    onChange={(e) => setConsentAi(e.target.checked)}
                    className="rounded border-slate-700 text-blue-600 focus:ring-blue-500 bg-slate-900"
                  />
                  <span>Allow AI content analysis for topic tagging and quiz question generation</span>
                </label>
              </div>
            </div>
          )}

          {/* Navigation Controls */}
          <div className="flex items-center justify-between pt-6 border-t border-slate-800 mt-6">
            {step > 1 ? (
              <Button
                variant="outline"
                size="md"
                onClick={() => setStep(step - 1)}
                icon={ArrowLeft}
              >
                Back
              </Button>
            ) : (
              <div />
            )}

            {step < 5 ? (
              <Button
                variant="primary"
                size="md"
                onClick={() => setStep(step + 1)}
                icon={ArrowRight}
              >
                Continue
              </Button>
            ) : (
              <Button
                variant="emerald"
                size="md"
                onClick={handleFinish}
                isLoading={isSubmitting}
                icon={Sparkles}
              >
                Complete Onboarding & Enter Dashboard
              </Button>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
};
