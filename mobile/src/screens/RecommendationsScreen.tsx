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
import { RecommendationResponse } from '../types';
import { mobileApi } from '../services/api';
import { Compass, Sparkles, ExternalLink } from 'lucide-react-native';

export const RecommendationsScreen: React.FC = () => {
  const [recommendations, setRecommendations] = useState<RecommendationResponse[]>([]);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [isFinding, setIsFinding] = useState<boolean>(false);

  const loadData = async () => {
    try {
      const data = await mobileApi.getRecommendations();
      setRecommendations(data);
    } catch (err) {
      console.error(err);
    } finally {
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleFindUseful = async () => {
    setIsFinding(true);
    try {
      const rec = await mobileApi.findSomethingUseful();
      if (rec && rec.url) {
        Linking.openURL(rec.url);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsFinding(false);
    }
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadData(); }} tintColor={colors.primary} />}
    >
      <View style={styles.header}>
        <Text style={typography.h1}>Recommendations Hub</Text>
        <Text style={[typography.caption, { marginTop: 4 }]}>
          High-signal curated resources matched to your personal goals
        </Text>
      </View>

      <TouchableOpacity
        style={styles.findBtn}
        onPress={handleFindUseful}
        disabled={isFinding}
      >
        <Sparkles color="#fff" size={16} />
        <Text style={styles.findBtnText}>
          {isFinding ? 'Matching Goals...' : 'Find Something Useful Now ↗'}
        </Text>
      </TouchableOpacity>

      <View style={{ marginTop: spacing.md }}>
        {recommendations.length === 0 ? (
          <View style={styles.emptyCard}>
            <Compass color={colors.textMuted} size={28} />
            <Text style={[typography.body, { textAlign: 'center', marginTop: 8 }]}>
              No recommendations generated yet. Set your learning goals to unlock personalized resources.
            </Text>
          </View>
        ) : (
          recommendations.map((rec) => (
            <View key={rec.id} style={styles.card}>
              <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
                <View style={styles.sourcePill}>
                  <Text style={styles.sourcePillText}>{rec.sourceType}</Text>
                </View>
                <Text style={styles.matchScore}>{rec.relevanceScore}% Match</Text>
              </View>

              <Text style={[typography.h3, { marginTop: 8 }]}>{rec.title}</Text>
              <Text style={[typography.body, { fontSize: 13, marginTop: 4, lineHeight: 18 }]}>
                {rec.description}
              </Text>

              <View style={styles.whyBox}>
                <Text style={styles.whyText}>
                  <Text style={{ fontWeight: '700', color: colors.primary }}>Why this recommendation: </Text>
                  {rec.reasonDescription}
                </Text>
              </View>

              <TouchableOpacity
                style={styles.openBtn}
                onPress={() => Linking.openURL(rec.url)}
              >
                <Text style={styles.openBtnText}>Open Resource</Text>
                <ExternalLink color="#fff" size={14} />
              </TouchableOpacity>
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
  findBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 12,
    marginBottom: spacing.md,
  },
  findBtnText: { color: '#fff', fontSize: 14, fontWeight: '700' },
  card: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.lg,
    marginBottom: spacing.md,
  },
  sourcePill: {
    backgroundColor: 'rgba(16, 185, 129, 0.15)',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
  },
  sourcePillText: { color: colors.emerald, fontSize: 11, fontWeight: '700' },
  matchScore: { color: colors.textMuted, fontSize: 11, fontFamily: 'Courier' },
  whyBox: {
    backgroundColor: colors.bgCardElevated,
    borderLeftWidth: 3,
    borderLeftColor: colors.primary,
    padding: 10,
    borderRadius: 8,
    marginVertical: 10,
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
    marginTop: 4,
  },
  openBtnText: { color: '#fff', fontSize: 13, fontWeight: '700' },
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
