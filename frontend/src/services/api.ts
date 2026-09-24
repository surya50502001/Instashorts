import {
  AuthResponse,
  UserProfile,
  UserGoal,
  UserPreference,
  DashboardSummary,
  DailyAnalytics,
  WeeklyReview,
  ContentItemDetail,
  RecommendationResponse,
  KnowledgeCheckDetail,
  SubmitAnswerResult,
  KnowledgeMapNode,
  RetentionSummary,
  SubscriptionStatus,
  InterventionEvaluation,
} from '../types';

const API_BASE = '/api';

class ApiClient {
  private getAccessToken(): string | null {
    return localStorage.getItem('sg_access_token');
  }

  private setTokens(accessToken: string, refreshToken: string) {
    localStorage.setItem('sg_access_token', accessToken);
    localStorage.setItem('sg_refresh_token', refreshToken);
  }

  public clearTokens() {
    localStorage.removeItem('sg_access_token');
    localStorage.removeItem('sg_refresh_token');
    localStorage.removeItem('sg_user');
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = this.getAccessToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    let response = await fetch(`${API_BASE}${endpoint}`, {
      ...options,
      headers,
    });

    // Handle token refresh on 401
    if (response.status === 401 && !endpoint.includes('/auth/login') && !endpoint.includes('/auth/register')) {
      const refreshed = await this.tryRefreshToken();
      if (refreshed) {
        headers['Authorization'] = `Bearer ${this.getAccessToken()}`;
        response = await fetch(`${API_BASE}${endpoint}`, {
          ...options,
          headers,
        });
      } else {
        this.clearTokens();
        window.location.href = '/login';
        throw new Error('Session expired. Please log in again.');
      }
    }

    if (!response.ok) {
      let errorData;
      try {
        errorData = await response.json();
      } catch {
        errorData = { message: `Request failed with status ${response.status}` };
      }
      throw new Error(errorData.message || (errorData.errors && errorData.errors[0]) || 'An error occurred');
    }

    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  private async tryRefreshToken(): Promise<boolean> {
    const refreshToken = localStorage.getItem('sg_refresh_token');
    if (!refreshToken) return false;

    try {
      const res = await fetch(`${API_BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });

      if (res.ok) {
        const data: AuthResponse = await res.json();
        this.setTokens(data.accessToken, data.refreshToken);
        return true;
      }
    } catch {
      return false;
    }
    return false;
  }

  // Auth
  async register(email: string, password: string, fullName: string): Promise<AuthResponse> {
    const data = await this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, fullName }),
    });
    this.setTokens(data.accessToken, data.refreshToken);
    localStorage.setItem('sg_user', JSON.stringify(data.user));
    return data;
  }

  async login(email: string, password: string): Promise<AuthResponse> {
    const data = await this.request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, deviceName: 'Web App' }),
    });
    this.setTokens(data.accessToken, data.refreshToken);
    localStorage.setItem('sg_user', JSON.stringify(data.user));
    return data;
  }

  async getCurrentUser(): Promise<UserProfile> {
    return this.request<UserProfile>('/auth/me');
  }

  async logout(): Promise<void> {
    try {
      await this.request('/auth/logout', { method: 'POST' });
    } finally {
      this.clearTokens();
    }
  }

  // Onboarding
  async getOnboardingStatus(): Promise<{ isOnboarded: boolean; goals: UserGoal[]; preference?: UserPreference }> {
    return this.request('/onboarding/status');
  }

  async submitOnboarding(payload: {
    selectedCategories: string[];
    primaryGoalText: string;
    estimatedDailyMinutes: number;
    aggressiveness: number;
    consentToTelemetry: boolean;
    consentToAiAnalysis: boolean;
  }): Promise<any> {
    return this.request('/onboarding', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  // Goals
  async getGoals(): Promise<UserGoal[]> {
    return this.request<UserGoal[]>('/goals');
  }

  async createGoal(goal: { category: string; title: string; description: string; priority: number; targetWeeklyHours: number }): Promise<UserGoal> {
    return this.request<UserGoal>('/goals', {
      method: 'POST',
      body: JSON.stringify(goal),
    });
  }

  async updateGoal(id: string, goal: { title: string; description: string; priority: number; targetWeeklyHours: number; isActive: boolean }): Promise<UserGoal> {
    return this.request<UserGoal>(`/goals/${id}`, {
      method: 'PUT',
      body: JSON.stringify(goal),
    });
  }

  async deleteGoal(id: string): Promise<void> {
    return this.request(`/goals/${id}`, { method: 'DELETE' });
  }

  // Preferences
  async getPreferences(): Promise<UserPreference> {
    return this.request<UserPreference>('/preferences');
  }

  async updatePreferences(pref: Partial<UserPreference>): Promise<UserPreference> {
    return this.request<UserPreference>('/preferences', {
      method: 'PUT',
      body: JSON.stringify(pref),
    });
  }

  // Content Ingestion & Stream
  async ingestContentEvent(payload: {
    url: string;
    title?: string;
    creator?: string;
    caption?: string;
    timeSpentSeconds: number;
    completionPercentage: number;
    sourceProvider?: number;
    userAction?: number;
  }): Promise<any> {
    return this.request('/content/events', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  async sendHeartbeat(sessionId?: string, activeSeconds = 15, idleSeconds = 0): Promise<any> {
    return this.request('/content/session/heartbeat', {
      method: 'POST',
      body: JSON.stringify({
        sessionId,
        activeScrollSecondsIncrement: activeSeconds,
        idleSecondsIncrement: idleSeconds,
      }),
    });
  }

  async getContentItems(page = 1, pageSize = 20): Promise<ContentItemDetail[]> {
    return this.request<ContentItemDetail[]>(`/content/items?page=${page}&pageSize=${pageSize}`);
  }

  // Interventions
  async evaluateIntervention(sessionId: string): Promise<InterventionEvaluation> {
    return this.request<InterventionEvaluation>(`/interventions/evaluate?sessionId=${sessionId}`);
  }

  async recordInterventionAction(interventionId: string, response: number): Promise<void> {
    return this.request('/interventions/action', {
      method: 'POST',
      body: JSON.stringify({ interventionId, response }),
    });
  }

  async getInterventionHistory(): Promise<any[]> {
    return this.request('/interventions/history');
  }

  // Recommendations
  async getRecommendations(limit = 6): Promise<RecommendationResponse[]> {
    return this.request<RecommendationResponse[]>(`/recommendations?limit=${limit}`);
  }

  async findSomethingUseful(): Promise<RecommendationResponse> {
    return this.request<RecommendationResponse>('/recommendations/find-useful');
  }

  async recordRecommendationFeedback(id: string, interactionType: number): Promise<void> {
    return this.request(`/recommendations/${id}/feedback`, {
      method: 'POST',
      body: JSON.stringify({ interactionType }),
    });
  }

  // Knowledge & Retention
  async getPendingKnowledgeCheck(): Promise<KnowledgeCheckDetail | null> {
    try {
      const res: any = await this.request('/knowledge/check');
      if (res && res.checkId) return res as KnowledgeCheckDetail;
      return null;
    } catch {
      return null;
    }
  }

  async submitAnswer(checkId: string, questionId: string, selectedOptionIndex: number, responseTimeSeconds = 3.5): Promise<SubmitAnswerResult> {
    return this.request<SubmitAnswerResult>('/knowledge/answer', {
      method: 'POST',
      body: JSON.stringify({ checkId, questionId, selectedOptionIndex, responseTimeSeconds }),
    });
  }

  async getKnowledgeMap(): Promise<KnowledgeMapNode[]> {
    return this.request<KnowledgeMapNode[]>('/knowledge/map');
  }

  async getRetentionSummary(): Promise<RetentionSummary> {
    return this.request<RetentionSummary>('/knowledge/summary');
  }

  // Analytics
  async getDashboardSummary(date?: string): Promise<DashboardSummary> {
    const q = date ? `?date=${date}` : '';
    return this.request<DashboardSummary>(`/analytics/dashboard${q}`);
  }

  async getDailyAnalytics(date?: string): Promise<DailyAnalytics> {
    const q = date ? `?date=${date}` : '';
    return this.request<DailyAnalytics>(`/analytics/daily${q}`);
  }

  async getWeeklyReview(startDate?: string): Promise<WeeklyReview> {
    const q = startDate ? `?startDate=${startDate}` : '';
    return this.request<WeeklyReview>(`/analytics/weekly${q}`);
  }

  // Privacy & Data Governance
  async exportAllData(): Promise<any> {
    return this.request('/privacy/export');
  }

  async deleteEvent(id: string): Promise<void> {
    return this.request(`/privacy/event/${id}`, { method: 'DELETE' });
  }

  async purgeHistory(beforeDateUtc?: string): Promise<void> {
    return this.request('/privacy/purge', {
      method: 'POST',
      body: JSON.stringify({ beforeDateUtc }),
    });
  }

  async deleteAccount(): Promise<void> {
    return this.request('/privacy/account', { method: 'DELETE' });
  }

  // Subscriptions & Billing
  async getSubscriptionStatus(): Promise<SubscriptionStatus> {
    return this.request<SubscriptionStatus>('/subscriptions/status');
  }

  async createCheckoutSession(plan: number): Promise<{ checkoutUrl: string; sessionId: string }> {
    return this.request('/subscriptions/checkout', {
      method: 'POST',
      body: JSON.stringify({ plan }),
    });
  }
}

export const api = new ApiClient();
