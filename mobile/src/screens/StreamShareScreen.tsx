import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  Linking,
} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';
import { mobileApi } from '../services/api';
import { Share2, Send, CheckCircle2, Sparkles, ExternalLink } from 'lucide-react-native';

const PRESETS = [
  { label: '.NET 8 Clean Arch', url: 'https://www.youtube.com/shorts/dotNetCleanArch8', title: '.NET Clean Architecture' },
  { label: 'Docker Basics', url: 'https://www.youtube.com/shorts/dockerFastTips', title: 'Docker containerization essentials' },
  { label: 'Index Investing', url: 'https://www.youtube.com/shorts/indexInvesting101', title: 'Compound interest and index funds' },
  { label: 'Comedy Pranks', url: 'https://www.instagram.com/reel/funnyPrank2026', title: 'Street comedy prank' },
];

export const StreamShareScreen: React.FC = () => {
  const [url, setUrl] = useState<string>('https://www.youtube.com/shorts/dotNetCleanArch8');
  const [title, setTitle] = useState<string>('.NET 8 Clean Architecture');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [result, setResult] = useState<any>(null);

  const handleIngest = async () => {
    if (!url.trim()) return;
    setIsSubmitting(true);
    setResult(null);

    try {
      const res = await mobileApi.ingestContent(url.trim(), title.trim());
      setResult(res);
    } catch (err: any) {
      alert(err.message || 'Ingestion failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Text style={typography.h1}>Share Target Ingestion</Text>
        <Text style={[typography.caption, { marginTop: 4 }]}>
          Paste or share short-form video links from Instagram, YouTube, or TikTok to trigger AI analysis.
        </Text>
      </View>

      {/* Preset Chips */}
      <View style={styles.chipRow}>
        {PRESETS.map((p) => (
          <TouchableOpacity
            key={p.label}
            style={styles.chip}
            onPress={() => {
              setUrl(p.url);
              setTitle(p.title);
            }}
          >
            <Text style={styles.chipText}>+ {p.label}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Input Card */}
      <View style={styles.card}>
        <Text style={styles.fieldLabel}>Content URL</Text>
        <TextInput
          style={styles.input}
          value={url}
          onChangeText={setUrl}
          placeholder="https://www.youtube.com/shorts/..."
          placeholderTextColor={colors.textMuted}
          autoCapitalize="none"
        />

        <Text style={[styles.fieldLabel, { marginTop: spacing.md }]}>Video Title (Optional)</Text>
        <TextInput
          style={styles.input}
          value={title}
          onChangeText={setTitle}
          placeholder="e.g. Clean Architecture in .NET 8"
          placeholderTextColor={colors.textMuted}
        />

        <TouchableOpacity
          style={styles.submitBtn}
          onPress={handleIngest}
          disabled={isSubmitting}
        >
          {isSubmitting ? (
            <ActivityIndicator color="#fff" size="small" />
          ) : (
            <>
              <Send color="#fff" size={16} />
              <Text style={styles.submitBtnText}>Analyze & Ingest Event</Text>
            </>
          )}
        </TouchableOpacity>
      </View>

      {/* Result Card */}
      {result && (
        <View style={styles.resultCard}>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 8 }}>
            <CheckCircle2 color={colors.emerald} size={18} />
            <Text style={[typography.h3, { color: colors.emerald }]}>Ingested & Queued for AI Analysis</Text>
          </View>
          <Text style={typography.caption}>
            Event ID: <Text style={{ fontFamily: 'Courier', color: colors.textPrimary }}>{result.eventId}</Text>
          </Text>
          <Text style={[typography.caption, { marginTop: 4 }]}>
            Session ID: <Text style={{ fontFamily: 'Courier', color: colors.textPrimary }}>{result.sessionId}</Text>
          </Text>
        </View>
      )}

      {/* Share Target Info */}
      <View style={[styles.card, { marginTop: spacing.md }]}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 6 }}>
          <Share2 color={colors.primary} size={16} />
          <Text style={typography.h3}>Native Mobile Share Target</Text>
        </View>
        <Text style={[typography.body, { fontSize: 13, lineHeight: 19 }]}>
          When browsing Instagram Reels or YouTube Shorts on your mobile device, tap <Text style={{ fontWeight: '700', color: colors.textPrimary }}>Share → Scroll Guardian</Text>. The content URL is transmitted directly into your personal learning pipeline without copying links.
        </Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bgPrimary },
  content: { padding: spacing.lg, paddingBottom: 40 },
  header: { marginBottom: spacing.md },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 6, marginBottom: spacing.md },
  chip: {
    backgroundColor: colors.bgCard,
    borderColor: colors.borderSubtle,
    borderWidth: 1,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
  },
  chipText: { fontSize: 12, color: colors.textSecondary },
  card: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.lg,
  },
  fieldLabel: { fontSize: 12, fontWeight: '600', color: colors.textSecondary, marginBottom: 6 },
  input: {
    backgroundColor: colors.bgInput,
    borderColor: colors.borderSubtle,
    borderWidth: 1,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    color: colors.textPrimary,
    fontSize: 13,
  },
  submitBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 12,
    marginTop: spacing.lg,
  },
  submitBtnText: { color: '#fff', fontSize: 14, fontWeight: '700' },
  resultCard: {
    backgroundColor: 'rgba(16, 185, 129, 0.1)',
    borderColor: 'rgba(16, 185, 129, 0.3)',
    borderWidth: 1,
    borderRadius: 16,
    padding: spacing.md,
    marginTop: spacing.md,
  },
});
