import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  AuthResponse,
  UserProfile,
  DashboardSummary,
  RecommendationResponse,
  KnowledgeCheckDetail,
  SubmitAnswerResult,
  KnowledgeMapNode,
  RetentionSummary,
  IngestResponse,
} from '../types';
import { Platform } from 'react-native';

// In Android emulator localhost is 10.0.2.2; on iOS or real device it uses host IP
const DEFAULT_API_HOST = Platform.OS === 'android' ? 'http://10.0.2.2:5000/api' : 'http://localhost:5000/api';

class MobileApiClient {
  private baseUrl: string = DEFAULT_API_HOST;

  public async setBaseUrl(url: string) {
    this.baseUrl = url;
    await AsyncStorage.setItem('sg_mobile_api_url', url);
  }

  public async getBaseUrl(): Promise<string> {
    const saved = await AsyncStorage.getItem('sg_mobile_api_url');
    if (saved) this.baseUrl = saved;
    return this.baseUrl;
  }

  private async getAccessToken(): Promise<string | null> {
    return AsyncStorage.getItem('sg_mobile_token');
  }

  public async setTokens(accessToken: string, refreshToken: string) {
    await AsyncStorage.setItem('sg_mobile_token', accessToken);
    await AsyncStorage.setItem('sg_mobile_refresh_token', refreshToken);
  }

  public async clearTokens() {
    await AsyncStorage.removeItem('sg_mobile_token');
    await AsyncStorage.removeItem('sg_mobile_refresh_token');
    await AsyncStorage.removeItem('sg_mobile_user');
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = await this.getAccessToken();
    const url = await this.getBaseUrl();

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${url}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401 && !endpoint.includes('/auth/')) {
      const refreshed = await this.tryRefreshToken();
      if (refreshed) {
        const newToken = await this.getAccessToken();
        headers['Authorization'] = `Bearer ${newToken}`;
        const retryRes = await fetch(`${url}${endpoint}`, { ...options, headers });
        if (retryRes.ok) return retryRes.json();
      }
      await this.clearTokens();
      throw new Error('Session expired. Please sign in.');
    }

    if (!response.ok) {
      let errorData;
      try {
        errorData = await response.json();
      } catch {
        errorData = { message: `Request failed with status ${response.status}` };
      }
      throw new Error(errorData.message || 'An error occurred');
    }

    if (response.status === 204) return {} as T;
    return response.json();
  }

  private async tryRefreshToken(): Promise<boolean> {
    const refreshToken = await AsyncStorage.getItem('sg_mobile_refresh_token');
    if (!refreshToken) return false;

    try {
      const url = await this.getBaseUrl();
      const res = await fetch(`${url}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });

      if (res.ok) {
        const data: AuthResponse = await res.json();
        await this.setTokens(data.accessToken, data.refreshToken);
        return true;
      }
    } catch {
      return false;
    }
    return false;
  }

  // Auth
  async login(email: string, password: string): Promise<AuthResponse> {
    const data = await this.request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, deviceName: 'Mobile Device', deviceType: Platform.OS }),
    });
    await this.setTokens(data.accessToken, data.refreshToken);
    await AsyncStorage.setItem('sg_mobile_user', JSON.stringify(data.user));
    return data;
  }

  async register(email: string, password: string, fullName: string): Promise<AuthResponse> {
    const data = await this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, fullName }),
    });
    await this.setTokens(data.accessToken, data.refreshToken);
    await AsyncStorage.setItem('sg_mobile_user', JSON.stringify(data.user));
    return data;
  }

  async getCurrentUser(): Promise<UserProfile> {
    return this.request<UserProfile>('/auth/me');
  }

  // Dashboard
  async getDashboardSummary(): Promise<DashboardSummary> {
    return this.request<DashboardSummary>('/analytics/dashboard');
  }

  // Recommendations & Find Something Useful
  async getRecommendations(): Promise<RecommendationResponse[]> {
    return this.request<RecommendationResponse[]>('/recommendations?limit=6');
  }

  async findSomethingUseful(): Promise<RecommendationResponse> {
    return this.request<RecommendationResponse>('/recommendations/find-useful');
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

  async submitAnswer(checkId: string, questionId: string, selectedOptionIndex: number, responseTimeSeconds = 3): Promise<SubmitAnswerResult> {
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

  // Content Ingestion (e.g. from Native Share Target)
  async ingestContent(url: string, title?: string, creator?: string, caption?: string): Promise<IngestResponse> {
    return this.request<IngestResponse>('/content/events', {
      method: 'POST',
      body: JSON.stringify({
        url,
        title: title || 'Short-form Shared Content',
        creator: creator || 'Creator',
        caption: caption || '',
        timeSpentSeconds: 30,
        completionPercentage: 100,
        sourceProvider: url.includes('instagram') ? 3 : (url.includes('youtube') ? 2 : 1),
      }),
    });
  }
}

export const mobileApi = new MobileApiClient();
