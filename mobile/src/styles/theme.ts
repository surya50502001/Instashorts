export const colors = {
  bgPrimary: '#090d16',
  bgCard: '#0f172a',
  bgCardElevated: '#131d33',
  bgInput: '#040711',
  borderSubtle: '#1e293b',
  borderHover: '#334155',
  primary: '#3b82f6',
  primaryDark: '#2563eb',
  emerald: '#10b981',
  amber: '#f59e0b',
  rose: '#f43f5e',
  indigo: '#6366f1',
  textPrimary: '#f8fafc',
  textSecondary: '#94a3b8',
  textMuted: '#64748b',
};

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
};

export const typography = {
  h1: { fontSize: 24, fontWeight: '700' as const, color: colors.textPrimary },
  h2: { fontSize: 20, fontWeight: '700' as const, color: colors.textPrimary },
  h3: { fontSize: 16, fontWeight: '600' as const, color: colors.textPrimary },
  body: { fontSize: 14, color: colors.textSecondary, lineHeight: 20 },
  caption: { fontSize: 12, color: colors.textMuted },
  mono: { fontFamily: 'Courier', fontSize: 13, color: colors.textPrimary },
};
