export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: number;
  plan: number; // 1 = Free, 2 = Pro
  isEmailVerified: boolean;
  isOnboarded: boolean;
  createdAtUtc: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface UserGoal {
  id: string;
  category: string;
  title: string;
  description: string;
  priority: number;
  targetWeeklyHours: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface DashboardSummary {
  date: string;
  activeScrollMinutes: number;
  learningMinutes: number;
  entertainmentMinutes: number;
  goalRelevantMinutes: number;
  totalSessions: number;
  itemsConsumedCount: number;
  dominantCategory: string;
  scrollMinutesChangePct?: number;
  learningMinutesChangePct?: number;
  goalRelevanceChangePct?: number;
  trendComparisonText: string;
  timeline: Array<{
    eventId: string;
    timestampUtc: string;
    title: string;
    creator: string;
    category: string;
    primaryTopic: string;
    durationSeconds: number;
    timeSpentSeconds: number;
    educationalValue: number;
    isGoalRelevant: boolean;
    url: string;
  }>;
  knowledgeChecksToday: number;
  knowledgeAccuracyToday?: number;
  topRecommendation?: RecommendationResponse;
}

export interface RecommendationResponse {
  id: string;
  title: string;
  description: string;
  url: string;
  reasonDescription: string;
  sourceType: string;
  topicName?: string;
  goalTitle?: string;
  relevanceScore: number;
  priority: number;
}

export interface KnowledgeQuestion {
  questionId: string;
  questionText: string;
  options: string[];
  difficulty: number;
}

export interface KnowledgeCheckDetail {
  checkId: string;
  topicName: string;
  contentTitle?: string;
  contentUrl?: string;
  questions: KnowledgeQuestion[];
}

export interface SubmitAnswerResult {
  isCorrect: boolean;
  correctOptionIndex: number;
  explanation: string;
  updatedMasteryScore?: number;
  updatedRetentionScore?: number;
  isCheckCompleted: boolean;
  totalCheckScorePercentage?: number;
}

export interface KnowledgeMapNode {
  topicId: string;
  category: string;
  topicName: string;
  parentTopicName?: string;
  contentConsumedCount: number;
  totalTimeSpentMinutes: number;
  knowledgeChecksAttempted: number;
  knowledgeChecksCorrect: number;
  masteryScore: number;
  retentionScore: number;
  lastExposedAtUtc?: string;
}

export interface RetentionSummary {
  overallRetentionRate: number;
  totalQuestionsAnswered: number;
  totalQuestionsCorrect: number;
  masteredTopicsCount: number;
  developingTopicsCount: number;
  retentionStatusMessage: string;
}

export interface IngestResponse {
  eventId: string;
  sessionId: string;
  contentItemId: string;
  analysisQueued: boolean;
}
