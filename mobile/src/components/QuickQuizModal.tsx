import React, { useState } from 'react';
import {
  Modal,
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  StyleProp,
  ViewStyle,
  TextStyle,
} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';
import { KnowledgeCheckDetail, SubmitAnswerResult } from '../types';
import { mobileApi } from '../services/api';
import { X, CheckCircle2, XCircle, ArrowRight, Sparkles } from 'lucide-react-native';

interface QuickQuizModalProps {
  visible: boolean;
  check: KnowledgeCheckDetail | null;
  onClose: () => void;
  onCompleted?: () => void;
}

export const QuickQuizModal: React.FC<QuickQuizModalProps> = ({
  visible,
  check,
  onClose,
  onCompleted,
}) => {
  const [currentIdx, setCurrentIdx] = useState<number>(0);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);
  const [result, setResult] = useState<SubmitAnswerResult | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  if (!check || check.questions.length === 0) return null;

  const currentQ = check.questions[currentIdx];

  const handleSelectOption = async (optionIdx: number) => {
    if (selectedIdx !== null || isSubmitting) return;

    setSelectedIdx(optionIdx);
    setIsSubmitting(true);

    try {
      const res = await mobileApi.submitAnswer(
        check.checkId,
        currentQ.questionId,
        optionIdx,
        3
      );
      setResult(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleNext = () => {
    if (currentIdx < check.questions.length - 1) {
      setCurrentIdx(prev => prev + 1);
      setSelectedIdx(null);
      setResult(null);
    } else {
      if (onCompleted) onCompleted();
      onClose();
      setCurrentIdx(0);
      setSelectedIdx(null);
      setResult(null);
    }
  };

  return (
    <Modal visible={visible} animationType="slide" transparent>
      <View style={styles.backdrop}>
        <View style={styles.modalCard}>
          {/* Header */}
          <View style={styles.modalHeader}>
            <View>
              <Text style={styles.topicBadge}>{check.topicName}</Text>
              <Text style={[typography.h3, { marginTop: 4 }]}>Quick Recall Check</Text>
            </View>
            <TouchableOpacity onPress={onClose} style={styles.closeBtn}>
              <X color={colors.textMuted} size={18} />
            </TouchableOpacity>
          </View>

          {/* Question Counter */}
          <Text style={styles.counterText}>
            Question {currentIdx + 1} of {check.questions.length}
          </Text>

          {/* Question Text */}
          <Text style={[typography.body, styles.questionText]}>
            {currentQ.questionText}
          </Text>

          {/* Options */}
          <View style={styles.optionsList}>
            {currentQ.options.map((opt, idx) => {
              const isSelected = selectedIdx === idx;
              const isCorrectOpt = result?.correctOptionIndex === idx;
              const isWrongSelected = isSelected && result && !result.isCorrect;

              let cardStyle: StyleProp<ViewStyle> = styles.optionItem;
              let textStyle: StyleProp<TextStyle> = styles.optionText;

              if (result && isCorrectOpt) {
                cardStyle = [styles.optionItem, styles.optionCorrect];
                textStyle = [styles.optionText, { color: colors.emerald }];
              } else if (result && isWrongSelected) {
                cardStyle = [styles.optionItem, styles.optionWrong];
                textStyle = [styles.optionText, { color: colors.rose }];
              } else if (result) {
                cardStyle = [styles.optionItem, { opacity: 0.4 }];
              }

              return (
                <TouchableOpacity
                  key={idx}
                  style={cardStyle}
                  onPress={() => handleSelectOption(idx)}
                  disabled={selectedIdx !== null}
                >
                  <Text style={textStyle}>{opt}</Text>
                  {result && isCorrectOpt && <CheckCircle2 color={colors.emerald} size={18} />}
                  {result && isWrongSelected && <XCircle color={colors.rose} size={18} />}
                </TouchableOpacity>
              );
            })}
          </View>

          {/* Result Feedback */}
          {result && (
            <View style={styles.feedbackBox}>
              <Text style={styles.feedbackTitle}>
                {result.isCorrect ? '✓ Correct! Retention logged.' : 'Explanation:'}
              </Text>
              <Text style={styles.explanationText}>{result.explanation}</Text>
              <TouchableOpacity style={styles.nextBtn} onPress={handleNext}>
                <Text style={styles.nextBtnText}>
                  {currentIdx < check.questions.length - 1 ? 'Next Question' : 'Complete Quiz'}
                </Text>
                <ArrowRight color="#fff" size={16} />
              </TouchableOpacity>
            </View>
          )}
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.85)',
    justifyContent: 'center',
    padding: spacing.lg,
  },
  modalCard: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 20,
    padding: spacing.lg,
  },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' },
  topicBadge: { color: colors.primary, fontSize: 11, fontWeight: '700', textTransform: 'uppercase' },
  closeBtn: { padding: 4 },
  counterText: { fontSize: 11, color: colors.textMuted, marginVertical: spacing.sm, fontFamily: 'Courier' },
  questionText: { fontSize: 15, fontWeight: '600', color: colors.textPrimary, marginBottom: spacing.md, lineHeight: 22 },
  optionsList: { gap: spacing.sm },
  optionItem: {
    backgroundColor: colors.bgInput,
    borderColor: colors.borderSubtle,
    borderWidth: 1,
    borderRadius: 12,
    padding: spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  optionText: { fontSize: 13, color: colors.textPrimary, flex: 1, fontWeight: '500' },
  optionCorrect: { borderColor: colors.emerald, backgroundColor: 'rgba(16, 185, 129, 0.15)' },
  optionWrong: { borderColor: colors.rose, backgroundColor: 'rgba(244, 63, 94, 0.15)' },
  feedbackBox: {
    backgroundColor: colors.bgCardElevated,
    borderRadius: 14,
    padding: spacing.md,
    marginTop: spacing.md,
    borderLeftWidth: 3,
    borderLeftColor: colors.primary,
  },
  feedbackTitle: { fontSize: 12, fontWeight: '700', color: colors.primary, marginBottom: 4 },
  explanationText: { fontSize: 12, color: colors.textSecondary, lineHeight: 17, marginBottom: spacing.md },
  nextBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 10,
    borderRadius: 10,
  },
  nextBtnText: { color: '#fff', fontSize: 13, fontWeight: '700' },
});
