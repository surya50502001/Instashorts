import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  StyleSheet,
} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';
import { KnowledgeMapNode, RetentionSummary, KnowledgeCheckDetail } from '../types';
import { mobileApi } from '../services/api';
import { Network, BookOpen, Award, CheckCircle2 } from 'lucide-react-native';

interface KnowledgeMapScreenProps {
  onOpenQuiz: (quiz: KnowledgeCheckDetail) => void;
}

export const KnowledgeMapScreen: React.FC<KnowledgeMapScreenProps> = ({ onOpenQuiz }) => {
  const [nodes, setNodes] = useState<KnowledgeMapNode[]>([]);
  const [summary, setSummary] = useState<RetentionSummary | null>(null);
  const [refreshing, setRefreshing] = useState<boolean>(false);

  const loadData = async () => {
    try {
      const [mapData, sumData] = await Promise.all([
        mobileApi.getKnowledgeMap(),
        mobileApi.getRetentionSummary(),
      ]);
      setNodes(mapData);
      setSummary(sumData);
    } catch (err) {
      console.error(err);
    } finally {
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleStartQuiz = async () => {
    try {
      const quiz = await mobileApi.getPendingKnowledgeCheck();
      if (quiz) {
        onOpenQuiz(quiz);
      } else {
        alert('No pending knowledge checks right now! Ingest more educational short-form content to trigger new quizzes.');
      }
    } catch {
      alert('Unable to load quiz.');
    }
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadData(); }} tintColor={colors.primary} />}
    >
      <View style={styles.header}>
        <Text style={typography.h1}>Knowledge Retention</Text>
        <Text style={[typography.caption, { marginTop: 4 }]}>
          Verified recall and topic mastery taxonomy
        </Text>
      </View>

      {/* Start Quiz CTA */}
      <TouchableOpacity style={styles.ctaBtn} onPress={handleStartQuiz}>
        <BookOpen color="#fff" size={16} />
        <Text style={styles.ctaBtnText}>Start Spaced Recall Quiz</Text>
      </TouchableOpacity>

      {/* Stats Row */}
      <View style={styles.statsRow}>
        <View style={styles.statBox}>
          <Text style={styles.statLabel}>Retention</Text>
          <Text style={styles.statValue}>
            {summary && summary.totalQuestionsAnswered > 0 ? `${summary.overallRetentionRate}%` : '—'}
          </Text>
          <Text style={styles.statSub}>Accuracy rate</Text>
        </View>

        <View style={styles.statBox}>
          <Text style={styles.statLabel}>Answered</Text>
          <Text style={[styles.statValue, { color: colors.primary }]}>
            {summary?.totalQuestionsAnswered || 0}
          </Text>
          <Text style={styles.statSub}>
            {summary?.totalQuestionsCorrect || 0} correct
          </Text>
        </View>

        <View style={styles.statBox}>
          <Text style={styles.statLabel}>Mastered</Text>
          <Text style={[styles.statValue, { color: colors.emerald }]}>
            {summary?.masteredTopicsCount || 0}
          </Text>
          <Text style={styles.statSub}>≥75% score</Text>
        </View>
      </View>

      {/* Retention Message */}
      {summary?.retentionStatusMessage && (
        <View style={styles.messageBox}>
          <Text style={styles.messageText}>{summary.retentionStatusMessage}</Text>
        </View>
      )}

      {/* Topic Taxonomy Node Cards */}
      <View style={{ marginTop: spacing.md }}>
        <Text style={[typography.h3, { marginBottom: spacing.md }]}>Your Topic Taxonomy</Text>

        {nodes.length === 0 ? (
          <View style={styles.emptyCard}>
            <Network color={colors.textMuted} size={28} />
            <Text style={[typography.body, { textAlign: 'center', marginTop: 8 }]}>
              Knowledge Map will populate as you consume educational short-form content.
            </Text>
          </View>
        ) : (
          nodes.map((node) => (
            <View key={node.topicId} style={styles.nodeCard}>
              <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <View>
                  <View style={styles.catBadge}>
                    <Text style={styles.catBadgeText}>{node.category}</Text>
                  </View>
                  <Text style={[typography.h2, { fontSize: 16, marginTop: 4 }]}>{node.topicName}</Text>
                  {node.parentTopicName && (
                    <Text style={typography.caption}>Branch of {node.parentTopicName}</Text>
                  )}
                </View>
                <View style={{ alignItems: 'flex-end' }}>
                  <Text style={[typography.h2, { color: colors.emerald, fontSize: 18 }]}>
                    {node.masteryScore}%
                  </Text>
                  <Text style={typography.caption}>Mastery</Text>
                </View>
              </View>

              {/* Progress Bar */}
              <View style={styles.progressTrack}>
                <View style={[styles.progressFill, { width: `${Math.min(100, Math.max(5, node.masteryScore))}%` }]} />
              </View>

              <View style={styles.nodeFooter}>
                <Text style={typography.caption}>{node.contentConsumedCount} items watched</Text>
                <Text style={typography.caption}>
                  {node.knowledgeChecksCorrect}/{node.knowledgeChecksAttempted} checks correct
                </Text>
              </View>
            </View>
          ))
        )}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bgPrimary },
  content: { padding: spacing.lg, paddingBottom: 40 },
  header: { marginBottom: spacing.md },
  ctaBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 12,
    marginBottom: spacing.lg,
  },
  ctaBtnText: { color: '#fff', fontSize: 14, fontWeight: '700' },
  statsRow: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.md },
  statBox: {
    flex: 1,
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 14,
    padding: spacing.md,
    alignItems: 'center',
  },
  statLabel: { fontSize: 11, fontWeight: '700', color: colors.textMuted, textTransform: 'uppercase' },
  statValue: { fontSize: 20, fontWeight: '800', color: colors.textPrimary, marginVertical: 2 },
  statSub: { fontSize: 10, color: colors.textMuted },
  messageBox: {
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
    borderColor: 'rgba(59, 130, 246, 0.3)',
    borderWidth: 1,
    borderRadius: 12,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  messageText: { fontSize: 12, color: colors.primary, lineHeight: 18 },
  emptyCard: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderStyle: 'dashed',
    borderRadius: 16,
    padding: spacing.xxl,
    alignItems: 'center',
  },
  nodeCard: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  catBadge: {
    backgroundColor: 'rgba(59, 130, 246, 0.15)',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    alignSelf: 'flex-start',
  },
  catBadgeText: { color: colors.primary, fontSize: 10, fontWeight: '700' },
  progressTrack: {
    height: 6,
    backgroundColor: colors.bgCardElevated,
    borderRadius: 3,
    marginVertical: 12,
    overflow: 'hidden',
  },
  progressFill: { height: 6, backgroundColor: colors.emerald, borderRadius: 3 },
  nodeFooter: { flexDirection: 'row', justifyContent: 'space-between', borderTopWidth: 1, borderTopColor: colors.borderSubtle, paddingTop: 8 },
});
