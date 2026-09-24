import React from 'react';
import { LucideIcon, Loader2, X } from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: any[]) {
  return twMerge(clsx(inputs));
}

// Button
interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'emerald';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  icon?: LucideIcon;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  icon: Icon,
  className,
  disabled,
  ...props
}) => {
  const baseStyles = 'inline-flex items-center justify-center font-medium transition-all duration-150 rounded-lg focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';

  const sizeStyles = {
    sm: 'text-xs px-2.5 py-1.5 gap-1.5',
    md: 'text-sm px-4 py-2 gap-2',
    lg: 'text-base px-5 py-2.5 gap-2.5',
  };

  const variantStyles = {
    primary: 'bg-blue-600 hover:bg-blue-500 text-white shadow-sm focus:ring-blue-500 border border-blue-500/30',
    emerald: 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm focus:ring-emerald-500 border border-emerald-500/30',
    secondary: 'bg-slate-800 hover:bg-slate-700 text-slate-100 border border-slate-700 focus:ring-slate-500',
    outline: 'bg-transparent hover:bg-slate-800/60 text-slate-300 border border-slate-700/80 hover:text-white focus:ring-slate-500',
    ghost: 'bg-transparent hover:bg-slate-800/40 text-slate-400 hover:text-slate-200 focus:ring-slate-500',
    danger: 'bg-rose-600/20 hover:bg-rose-600 text-rose-300 hover:text-white border border-rose-500/30 focus:ring-rose-500',
  };

  return (
    <button
      className={cn(baseStyles, sizeStyles[size], variantStyles[variant], className)}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? <Loader2 className="w-4 h-4 animate-spin text-current" /> : Icon ? <Icon className="w-4 h-4" /> : null}
      {children}
    </button>
  );
};

// Card
export const Card: React.FC<{
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}> = ({ children, className, onClick }) => {
  return (
    <div
      onClick={onClick}
      className={cn(
        'bg-slate-900/70 border border-slate-800 rounded-xl p-5 backdrop-blur-sm transition-all duration-150',
        onClick && 'cursor-pointer hover:border-slate-700 hover:bg-slate-800/50',
        className
      )}
    >
      {children}
    </div>
  );
};

// Badge
export const Badge: React.FC<{
  children: React.ReactNode;
  variant?: 'blue' | 'emerald' | 'amber' | 'purple' | 'slate' | 'rose';
  className?: string;
}> = ({ children, variant = 'slate', className }) => {
  const variantStyles = {
    blue: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
    emerald: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
    amber: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
    purple: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
    slate: 'bg-slate-800 text-slate-400 border-slate-700',
    rose: 'bg-rose-500/10 text-rose-400 border-rose-500/20',
  };

  return (
    <span className={cn('inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium border', variantStyles[variant], className)}>
      {children}
    </span>
  );
};

// Empty State
export const EmptyState: React.FC<{
  icon: LucideIcon;
  title: string;
  description: string;
  actionText?: string;
  onAction?: () => void;
  className?: string;
}> = ({ icon: Icon, title, description, actionText, onAction, className }) => {
  return (
    <div className={cn('flex flex-col items-center justify-center text-center py-12 px-4 rounded-xl border border-dashed border-slate-800 bg-slate-950/40', className)}>
      <div className="w-12 h-12 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-center text-slate-400 mb-4 shadow-inner">
        <Icon className="w-6 h-6 text-slate-400" />
      </div>
      <h3 className="text-base font-semibold text-slate-200 mb-1">{title}</h3>
      <p className="text-sm text-slate-500 max-w-sm mb-5 leading-relaxed">{description}</p>
      {actionText && onAction && (
        <Button variant="secondary" size="sm" onClick={onAction}>
          {actionText}
        </Button>
      )}
    </div>
  );
};

// Skeleton Loader
export const Skeleton: React.FC<{ className?: string }> = ({ className }) => {
  return <div className={cn('animate-pulse bg-slate-800/80 rounded-md', className)} />;
};

// Modal
export const Modal: React.FC<{
  isOpen: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl';
}> = ({ isOpen, onClose, title, description, children, maxWidth = 'md' }) => {
  if (!isOpen) return null;

  const widthStyles = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-fadeIn">
      <div className={cn('bg-slate-900 border border-slate-800 rounded-2xl w-full p-6 shadow-2xl relative animate-scaleUp', widthStyles[maxWidth])}>
        <div className="flex items-start justify-between mb-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-100">{title}</h3>
            {description && <p className="text-sm text-slate-400 mt-1">{description}</p>}
          </div>
          <button
            onClick={onClose}
            className="text-slate-500 hover:text-slate-300 p-1.5 rounded-lg hover:bg-slate-800 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
        <div>{children}</div>
      </div>
    </div>
  );
};
