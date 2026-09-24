import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  StyleSheet,
  Linking,
} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';
import { DashboardSummary, KnowledgeCheckDetail } from '../types';
import { mobileApi } from '../services/api';
import {
  Clock,
  BookOpen,
  Film,
  Target,
  Sparkles,
  ExternalLink,
  ChevronRight,
  TrendingUp,
  TrendingDown,
  Layers,
} from 'lucide-react-native';

interface DashboardScreenProps {
  navigation: any;
  onOpenQuiz: (quiz: KnowledgeCheckDetail) => void;
}

export const DashboardScreen: React.FC<DashboardScreenProps> = ({ navigation, onOpenQuiz }) => {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const loadData = async () => {
    try {
      const data = await mobileApi.getDashboardSummary();
      setSummary(data);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleRefresh = () => {
    setRefreshing(true);
    loadData();
  };

  const handleQuickQuiz = async () => {
    try {
      const quiz = await mobileApi.getPendingKnowledgeCheck();
      if (quiz) {
        onOpenQuiz(quiz);
      } else {
        alert('No pending knowledge checks right now! Ingest more educational short-form content to trigger new quizzes.');
      }
    } catch {
      alert('Unable to load quiz check.');
    }
  };

  const handleFindUseful = async () => {
    try {
      const rec = await mobileApi.findSomethingUseful();
      if (rec && rec.url) {
        Linking.openURL(rec.url);
      } else {
        navigation.navigate('Recommendations');
      }
    } catch {
      navigation.navigate('Recommendations');
    }
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={handleRefresh} tintColor={colors.primary} />}
    >
      {/* Header */}
      <View style={styles.header}>
        <View>
          <Text style={typography.h1}>Your digital diet today</Text>
          <Text style={[typography.caption, { marginTop: 4 }]}>
            Real telemetry from short-form consumption
          </Text>
        </View>
      </View>

      {/* Action Buttons */}
      <View style={styles.actionRow}>
        <TouchableOpacity style={styles.btnPrimary} onPress={handleFindUseful}>
          <Sparkles color="#fff" size={16} />
          <Text style={styles.btnPrimaryText}>Find Something Useful</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.btnSecondary} onPress={handleQuickQuiz}>
          <BookOpen color={colors.textPrimary} size={16} />
          <Text style={styles.btnSecondaryText}>Quiz Check</Text>
        </TouchableOpacity>
      </View>

      {/* 4 Stats Grid */}
      <View style={styles.statsGrid}>
        <View style={styles.statCard}>
          <View style={styles.statCardHeader}>
            <Text style={styles.statLabel}>Active Scroll</Text>
            <Clock color={colors.primary} size={14} />
          </View>
          <Text style={styles.statValue}>{summary?.activeScrollMinutes || 0}m</Text>
          <Text style={styles.statSub}>{summary?.totalSessions || 0} sessions</Text>
        </View>

        <View style={[styles.statCard, { borderColor: 'rgba(16, 185, 129, 0.3)' }]}>
          <View style={styles.statCardHeader}>
            <Text style={[styles.statLabel, { color: colors.emerald }]}>Learning</Text>
            <BookOpen color={colors.emerald} size={14} />
          </View>
          <Text style={[styles.statValue, { color: colors.emerald }]}>
            {summary?.learningMinutes || 0}m
          </Text>
          <Text style={styles.statSub}>Goal aligned</Text>
        </View>

        <View style={styles.statCard}>
          <View style={styles.statCardHeader}>
            <Text style={styles.statLabel}>Entertainment</Text>
            <Film color={colors.amber} size={14} />
          </View>
          <Text style={styles.statValue}>{summary?.entertainmentMinutes || 0}m</Text>
          <Text style={styles.statSub}>{summary?.dominantCategory || 'None'}</Text>
        </View>

        <View style={[styles.statCard, { borderColor: 'rgba(99, 102, 241, 0.3)' }]}>
          <View style={styles.statCardHeader}>
            <Text style={[styles.statLabel, { color: colors.indigo }]}>Goal Focus</Text>
            <Target color={colors.indigo} size={14} />
          </View>
          <Text style={[styles.statValue, { color: colors.indigo }]}>
            {summary?.goalRelevantMinutes || 0}m
          </Text>
          <Text style={styles.statSub}>High-signal</Text>
        </View>
      </View>

      {/* "What's changing?" Section */}
      <View style={styles.card}>
        <Text style={[styles.statLabel, { marginBottom: 6 }]}>WHAT'S CHANGING?</Text>
        <Text style={typography.body}>{summary?.trendComparisonText}</Text>
      </View>

      {/* Top Recommended Next Step */}
      {summary?.topRecommendation && (
        <View style={[styles.card, { borderColor: 'rgba(59, 130, 246, 0.4)' }]}>
          <View style={styles.recBadge}>
            <Text style={styles.recBadgeText}>Recommended Next Step</Text>
          </View>
          <Text style={[typography.h3, { marginTop: 8 }]}>{summary.topRecommendation.title}</Text>
          <Text style={[typography.caption, { marginTop: 4, lineHeight: 18 }]}>
            {summary.topRecommendation.description}
          </Text>
          <View style={styles.whyBox}>
            <Text style={styles.whyText}>
              <Text style={{ fontWeight: '700', color: colors.primary }}>Why this recommendation: </Text>
              {summary.topRecommendation.reasonDescription}
            </Text>
          </View>
          <TouchableOpacity
            style={styles.openBtn}
            onPress={() => Linking.openURL(summary.topRecommendation!.url)}
          >
            <Text style={styles.openBtnText}>Open Resource</Text>
            <ExternalLink color="#fff" size={14} />
          </TouchableOpacity>
        </View>
      )}

      {/* Consumption Timeline */}
      <View style={styles.section}>
        <View style={styles.sectionHeader}>
          <Layers color={colors.textSecondary} size={16} />
          <Text style={[typography.h3, { marginLeft: 6 }]}>Today's Consumption Timeline</Text>
        </View>

        {!summary || summary.timeline.length === 0 ? (
          <View style={styles.emptyCard}>
            <Text style={[typography.body, { textAlign: 'center' }]}>
              No short-form content tracked today. Share a Reel or Short from Instagram or YouTube to start analyzing!
            </Text>
          </View>
        ) : (
          summary.timeline.map((item) => (
            <TouchableOpacity
              key={item.eventId}
              style={styles.timelineItem}
              onPress={() => Linking.openURL(item.url)}
            >
              <View style={{ flex: 1 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 4 }}>
                  <View style={styles.categoryPill}>
                    <Text style={styles.categoryPillText}>{item.category}</Text>
                  </View>
                  <Text style={typography.caption}>{item.primaryTopic}</Text>
                </View>
                <Text style={[typography.h3, { fontSize: 14 }]} numberOfLines={1}>
                  {item.title}
                </Text>
                <Text style={typography.caption}>
                  By {item.creator} • {item.timeSpentSeconds}s watched
                </Text>
              </View>
              <ChevronRight color={colors.textMuted} size={18} />
            </TouchableOpacity>
          ))
        )}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bgPrimary },
  content: { padding: spacing.lg, paddingBottom: 40 },
  header: { marginBottom: spacing.lg },
  actionRow: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.lg },
  btnPrimary: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    backgroundColor: colors.primary,
    paddingVertical: 12,
    borderRadius: 12,
  },
  btnPrimaryText: { color: '#fff', fontSize: 13, fontWeight: '700' },
  btnSecondary: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    backgroundColor: colors.bgCard,
    borderColor: colors.borderSubtle,
    borderWidth: 1,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 12,
  },
  btnSecondaryText: { color: colors.textPrimary, fontSize: 13, fontWeight: '600' },
  statsGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm, marginBottom: spacing.lg },
  statCard: {
    flex: 1,
    minWidth: '47%',
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.md,
  },
  statCardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  statLabel: { fontSize: 11, fontWeight: '700', color: colors.textMuted, textTransform: 'uppercase' },
  statValue: { fontSize: 24, fontWeight: '800', color: colors.textPrimary, marginVertical: 4 },
  statSub: { fontSize: 11, color: colors.textMuted },
  card: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.lg,
    marginBottom: spacing.lg,
  },
  recBadge: {
    backgroundColor: 'rgba(59, 130, 246, 0.15)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    alignSelf: 'flex-start',
  },
  recBadgeText: { color: colors.primary, fontSize: 11, fontWeight: '700' },
  whyBox: {
    backgroundColor: colors.bgCardElevated,
    borderLeftWidth: 3,
    borderLeftColor: colors.primary,
    padding: 10,
    borderRadius: 8,
    marginVertical: 12,
  },
  whyText: { fontSize: 12, color: colors.textSecondary, lineHeight: 17 },
  openBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 10,
    borderRadius: 10,
  },
  openBtnText: { color: '#fff', fontSize: 13, fontWeight: '700' },
  section: { marginTop: spacing.sm },
  sectionHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: spacing.md },
  timelineItem: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 14,
    padding: spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: spacing.sm,
  },
  categoryPill: {
    backgroundColor: 'rgba(59, 130, 246, 0.15)',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
  },
  categoryPillText: { color: colors.primary, fontSize: 10, fontWeight: '700' },
  emptyCard: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderStyle: 'dashed',
    borderRadius: 16,
    padding: spacing.xxl,
    alignItems: 'center',
  },
});
