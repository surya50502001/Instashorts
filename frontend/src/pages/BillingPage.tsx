import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button, Card, Badge, Skeleton } from '../components/ui/Components';
import { SubscriptionStatus } from '../types';
import {
  CreditCard,
  Check,
  Zap,
  Shield,
  Sparkles,
  ArrowRight,
} from 'lucide-react';
import { api } from '../services/api';

export const BillingPage: React.FC = () => {
  const { user, refreshProfile } = useAuth();
  const [subStatus, setSubStatus] = useState<SubscriptionStatus | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isUpgrading, setIsUpgrading] = useState<boolean>(false);

  const fetchStatus = async () => {
    setIsLoading(true);
    try {
      const data = await api.getSubscriptionStatus();
      setSubStatus(data);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchStatus();
  }, []);

  const handleUpgradeToPro = async () => {
    setIsUpgrading(true);
    try {
      const res = await api.createCheckoutSession(2); // Pro Plan
      await refreshProfile();
      await fetchStatus();
      alert('Your account has been upgraded to Scroll Guardian Pro!');
    } catch (err: any) {
      alert(err.message || 'Upgrade failed');
    } finally {
      setIsUpgrading(false);
    }
  };

  if (isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }

  const isPro = subStatus?.plan === 2 || user?.plan === 2;

  return (
    <div className="space-y-8 animate-fadeIn max-w-4xl">
      {/* Header */}
      <div className="border-b border-slate-850 pb-5">
        <h1 className="text-2xl font-bold tracking-tight text-white">Subscription & Billing</h1>
        <p className="text-sm text-slate-400 mt-1">
          Simple, transparent pricing to accelerate your intentional digital consumption.
        </p>
      </div>

      {/* Current Plan Badge */}
      <Card className="border-blue-500/30 bg-slate-900/80 p-5 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-600/20 border border-blue-500/30 flex items-center justify-center text-blue-400">
            <CreditCard className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs text-slate-400">Current Plan</div>
            <div className="text-base font-bold text-white flex items-center gap-2">
              <span>{isPro ? 'Scroll Guardian Pro' : 'Free Tier'}</span>
              <Badge variant={isPro ? 'emerald' : 'slate'}>
                {isPro ? 'Active' : 'Standard'}
              </Badge>
            </div>
          </div>
        </div>

        <div className="text-right text-xs text-slate-400">
          <div>{subStatus?.activeGoalsCount} / {subStatus?.maxGoalsAllowed} goals used</div>
        </div>
      </Card>

      {/* Pricing Comparison */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-4">
        {/* Free Plan */}
        <Card className="p-6 border-slate-800 space-y-5">
          <div>
            <div className="text-xs uppercase font-semibold text-slate-400">Free Tier</div>
            <div className="text-2xl font-bold text-white mt-1">$0 <span className="text-xs text-slate-500 font-normal">/ month</span></div>
            <p className="text-xs text-slate-400 mt-2">Essential intentionality tools for individual digital habit awareness.</p>
          </div>

          <div className="space-y-2.5 text-xs text-slate-300 border-t border-slate-800 pt-4">
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-slate-400" />
              <span>Up to 3 Active Learning Goals</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-slate-400" />
              <span>Basic AI Content Categorization</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-slate-400" />
              <span>Real-Time Browser Extension HUD</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-slate-400" />
              <span>7-day Historical Analytics</span>
            </div>
          </div>

          <Button variant="outline" size="md" className="w-full" disabled={!isPro}>
            {!isPro ? 'Current Plan' : 'Downgrade to Free'}
          </Button>
        </Card>

        {/* Pro Plan */}
        <Card className="p-6 border-blue-500/40 bg-gradient-to-b from-slate-900 to-slate-950 relative overflow-hidden space-y-5 shadow-xl">
          <div className="absolute top-0 right-0 bg-blue-600 text-white text-[10px] font-bold px-3 py-1 rounded-bl-lg uppercase tracking-wider">
            Most Popular
          </div>

          <div>
            <div className="text-xs uppercase font-semibold text-blue-400">Scroll Guardian Pro</div>
            <div className="text-2xl font-bold text-white mt-1">$12 <span className="text-xs text-slate-500 font-normal">/ month</span></div>
            <p className="text-xs text-slate-400 mt-2">Deep semantic analysis, unlimited goals, and advanced spaced recall mastery.</p>
          </div>

          <div className="space-y-2.5 text-xs text-slate-300 border-t border-slate-800 pt-4">
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-emerald-400" />
              <span><strong>Unlimited</strong> Active Goals & Domains</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-emerald-400" />
              <span><strong>Deep AI Analysis</strong> & Automated Quiz Generation</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-emerald-400" />
              <span><strong>Unlimited</strong> History & Full JSON Telemetry Export</span>
            </div>
            <div className="flex items-center gap-2">
              <Check className="w-4 h-4 text-emerald-400" />
              <span>Advanced Spaced Retention Graph</span>
            </div>
          </div>

          <Button
            variant={isPro ? 'secondary' : 'emerald'}
            size="md"
            className="w-full"
            onClick={handleUpgradeToPro}
            isLoading={isUpgrading}
            disabled={isPro}
            icon={Sparkles}
          >
            {isPro ? 'Active Pro Subscription' : 'Upgrade to Pro ($12/mo)'}
          </Button>
        </Card>
      </div>
    </div>
  );
};
