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

export interface UserPreference {
  interventionAggressiveness: number; // 1 = Gentle, 2 = Balanced, 3 = Proactive
  sessionInterventionIntervalMinutes: number;
  snoozeDurationMinutes: number;
  isInterventionEnabled: boolean;
  isAiAnalysisEnabled: boolean;
  isTrackingEnabled: boolean;
  excludedCategories: string[];
  theme: string;
}

export interface ContentAnalysisSummary {
  category: string;
  primaryTopic: string;
  informationDepth: number;
  educationalValue: number;
  goalRelevance: number;
  repetitionScore: number;
  summary: string;
  keyTakeaways: string[];
}

export interface ContentItemDetail {
  id: string;
  url: string;
  platformContentId: string;
  title: string;
  creator: string;
  caption: string;
  durationSeconds: number;
  sourceProvider: number;
  createdAtUtc: string;
  analysis?: ContentAnalysisSummary;
}

export interface ConsumptionTimelineEvent {
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
  timeline: ConsumptionTimelineEvent[];
  knowledgeChecksToday: number;
  knowledgeAccuracyToday?: number;
  topRecommendation?: RecommendationResponse;
}

export interface DailyAnalytics {
  date: string;
  totalScrollMinutes: number;
  learningMinutes: number;
  entertainmentMinutes: number;
  goalRelevantMinutes: number;
  sessionCount: number;
  averageSessionMinutes: number;
  longestSessionMinutes: number;
  repetitionScore: number;
  categoryDistributionMinutes: Record<string, number>;
  topicDistributionMinutes: Record<string, number>;
  knowledgeChecksCompleted: number;
  knowledgeAccuracyRate: number;
}

export interface TopicSummary {
  topicName: string;
  category: string;
  minutesSpent: number;
  itemsCount: number;
}

export interface DailyBreakdown {
  date: string;
  dayName: string;
  scrollMinutes: number;
  learningMinutes: number;
  goalRelevantMinutes: number;
}

export interface WeeklyReview {
  weekStartDate: string;
  weekEndDate: string;
  totalScrollHours: number;
  goalRelevantHours: number;
  learningHours: number;
  totalKnowledgeChecks: number;
  averageAccuracy: number;
  dynamicInsights: string[];
  topTopics: TopicSummary[];
  dailyBreakdown: DailyBreakdown[];
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

export interface SubscriptionStatus {
  plan: number; // 1 = Free, 2 = Pro
  status: number;
  currentPeriodEndUtc?: string;
  cancelAtPeriodEnd: boolean;
  activeGoalsCount: number;
  maxGoalsAllowed: number;
  hasDeepAiAnalysis: boolean;
  hasUnlimitedHistory: boolean;
}

export interface InterventionEvaluation {
  shouldIntervene: boolean;
  interventionId?: string;
  reason: number;
  suggestedAction: number;
  sessionMinutes: number;
  messageTitle: string;
  messageBody: string;
  callToActionText: string;
  attachedRecommendation?: RecommendationResponse;
  attachedKnowledgeCheck?: KnowledgeCheckDetail;
}
