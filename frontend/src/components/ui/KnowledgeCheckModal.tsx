import React, { useState } from 'react';
import { KnowledgeCheckDetail, SubmitAnswerResult } from '../../types';
import { Button, Modal, Badge } from './Components';
import { CheckCircle2, XCircle, ArrowRight, Sparkles, BookOpen } from 'lucide-react';
import confetti from 'canvas-confetti';
import { api } from '../../services/api';

interface KnowledgeCheckModalProps {
  check: KnowledgeCheckDetail | null;
  isOpen: boolean;
  onClose: () => void;
  onCompleted?: () => void;
}

export const KnowledgeCheckModal: React.FC<KnowledgeCheckModalProps> = ({
  check,
  isOpen,
  onClose,
  onCompleted,
}) => {
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState<number>(0);
  const [selectedOption, setSelectedOption] = useState<number | null>(null);
  const [result, setResult] = useState<SubmitAnswerResult | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [startTime] = useState<number>(Date.now());

  if (!check || check.questions.length === 0) return null;

  const currentQ = check.questions[currentQuestionIndex];

  const handleSelectOption = async (optionIndex: number) => {
    if (selectedOption !== null || isSubmitting) return;

    setSelectedOption(optionIndex);
    setIsSubmitting(true);

    const responseTime = Math.max(1, Math.round((Date.now() - startTime) / 1000));

    try {
      const res = await api.submitAnswer(
        check.checkId,
        currentQ.questionId,
        optionIndex,
        responseTime
      );
      setResult(res);

      if (res.isCorrect && res.isCheckCompleted) {
        confetti({
          particleCount: 80,
          spread: 70,
          origin: { y: 0.6 },
        });
      }
    } catch (err) {
      console.error('Failed to submit quiz answer', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleNext = () => {
    if (currentQuestionIndex < check.questions.length - 1) {
      setCurrentQuestionIndex(prev => prev + 1);
      setSelectedOption(null);
      setResult(null);
    } else {
      if (onCompleted) onCompleted();
      onClose();
      // Reset
      setCurrentQuestionIndex(0);
      setSelectedOption(null);
      setResult(null);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Quick Knowledge Retention Check"
      description={`Topic: ${check.topicName}`}
      maxWidth="lg"
    >
      <div className="space-y-6">
        {/* Topic & Question Counter */}
        <div className="flex items-center justify-between border-b border-slate-800 pb-3">
          <div className="flex items-center gap-2">
            <Badge variant="blue">{check.topicName}</Badge>
            {check.contentTitle && (
              <span className="text-xs text-slate-500 truncate max-w-xs">
                From: {check.contentTitle}
              </span>
            )}
          </div>
          <span className="text-xs font-mono text-slate-400">
            Question {currentQuestionIndex + 1} of {check.questions.length}
          </span>
        </div>

        {/* Question Text */}
        <div>
          <h4 className="text-base font-medium text-slate-100 leading-relaxed mb-4">
            {currentQ.questionText}
          </h4>

          {/* Options */}
          <div className="space-y-2.5">
            {currentQ.options.map((option, idx) => {
              const isSelected = selectedOption === idx;
              const isCorrectOption = result?.correctOptionIndex === idx;
              const isWrongSelected = isSelected && result && !result.isCorrect;

              let btnClasses = 'w-full text-left p-3.5 rounded-xl border text-sm font-medium transition-all flex items-center justify-between ';

              if (result === null) {
                btnClasses += 'border-slate-800 bg-slate-900/90 hover:border-slate-700 hover:bg-slate-850 text-slate-200 cursor-pointer';
              } else if (isCorrectOption) {
                btnClasses += 'border-emerald-500/50 bg-emerald-950/40 text-emerald-200';
              } else if (isWrongSelected) {
                btnClasses += 'border-rose-500/50 bg-rose-950/40 text-rose-200';
              } else {
                btnClasses += 'border-slate-800/40 bg-slate-900/40 text-slate-500 opacity-60';
              }

              return (
                <button
                  key={idx}
                  onClick={() => handleSelectOption(idx)}
                  disabled={selectedOption !== null || isSubmitting}
                  className={btnClasses}
                >
                  <span>{option}</span>
                  {result && isCorrectOption && (
                    <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0 ml-2" />
                  )}
                  {result && isWrongSelected && (
                    <XCircle className="w-4 h-4 text-rose-400 shrink-0 ml-2" />
                  )}
                </button>
              );
            })}
          </div>
        </div>

        {/* Feedback / Explanation Box */}
        {result && (
          <div className="space-y-4 animate-fadeIn">
            <div className={`p-4 rounded-xl border text-sm leading-relaxed ${
              result.isCorrect ? 'bg-emerald-950/20 border-emerald-500/30 text-emerald-300' : 'bg-rose-950/20 border-rose-500/30 text-rose-300'
            }`}>
              <div className="flex items-center gap-2 font-semibold mb-1">
                {result.isCorrect ? (
                  <>
                    <Sparkles className="w-4 h-4 text-emerald-400" />
                    <span>Correct! Great retention.</span>
                  </>
                ) : (
                  <>
                    <BookOpen className="w-4 h-4 text-rose-400" />
                    <span>Concept Breakdown:</span>
                  </>
                )}
              </div>
              <p className="text-slate-300 text-xs mt-1">{result.explanation}</p>
            </div>

            <div className="flex items-center justify-between pt-2">
              <div className="text-xs text-slate-400">
                {result.updatedMasteryScore !== undefined && (
                  <span>Updated topic mastery: <strong className="text-slate-200">{result.updatedMasteryScore}%</strong></span>
                )}
              </div>
              <Button variant="primary" size="md" onClick={handleNext} icon={ArrowRight}>
                {currentQuestionIndex < check.questions.length - 1 ? 'Next Question' : 'Complete Review'}
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};
