'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../../components/JourneyShell';
import {
  selectDiagnosticQuestions,
  startAdaptiveSession,
  submitAdaptiveAnswer,
  advanceAdaptiveSession,
  mapCanonicalSkillsToBackendAssessment
} from '../../api';
import { AdaptiveSessionResponse } from '../../types';
import { useJourneyState, journeyStateRepository } from '../../lib/journey-state';
import { normalizeTechnicalText } from '../../lib/text-utils';
import { TechnicalText } from '../../components/TechnicalText';

export default function InterviewPage() {
  const router = useRouter();
  const {
    state,
    isLoaded,
    setAdaptiveSession,
    setEvaluation,
    touch
  } = useJourneyState();

  const [currentSession, setCurrentSession] = useState<AdaptiveSessionResponse | null>(null);
  const [candidateAnswer, setCandidateAnswer] = useState<string>('');
  const [isInitializing, setIsInitializing] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [isAdvancing, setIsAdvancing] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Route Guard: require role and skills
  useEffect(() => {
    if (isLoaded) {
      if (!state.selectedRoleId) {
        router.replace('/roles');
      } else if (!state.userSelectedSkillIds || state.userSelectedSkillIds.length === 0) {
        router.replace('/assessment/skills');
      }
    }
  }, [isLoaded, state.selectedRoleId, state.userSelectedSkillIds, router]);

  // Initialize or resume assessment session
  useEffect(() => {
    if (!state.selectedRoleId || !state.userSelectedSkillIds || state.userSelectedSkillIds.length === 0) {
      return;
    }

    async function initSession() {
      // If already active in session, restore it
      if (state.adaptiveSession && state.adaptiveSession.status !== 'completed') {
        setCurrentSession(state.adaptiveSession);
        setIsInitializing(false);
        return;
      }

      setIsInitializing(true);
      setErrorMessage(null);

      try {
        // Map V3 canonical skills to legacy Backend assessment skill IDs
        const backendSkillIds = mapCanonicalSkillsToBackendAssessment(
          state.selectedRoleId!,
          state.userSelectedSkillIds
        );

        // Start adaptive session
        const sessionRes = await startAdaptiveSession(
          state.selectedRoleId!,
          backendSkillIds,
          state.primaryLanguageId || undefined
        );

        setCurrentSession(sessionRes);
        setAdaptiveSession(sessionRes);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to initialize assessment session';
        setErrorMessage(message);
      } finally {
        setIsInitializing(false);
      }
    }

    initSession();
  }, [state.selectedRoleId, isLoaded]);

  const handleSubmitAnswer = async () => {
    if (!currentSession || !currentSession.currentQuestion || !candidateAnswer.trim()) {
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);
    touch();

    try {
      const updated = await submitAdaptiveAnswer(
        currentSession.sessionId,
        currentSession.currentQuestion.id,
        candidateAnswer.trim()
      );

      setCurrentSession(updated);
      setAdaptiveSession(updated);

      if (updated.status === 'completed' && updated.profile) {
        journeyStateRepository.saveState({ careerProfile: updated.profile });
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to evaluate answer';
      setErrorMessage(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAdvanceNext = async () => {
    if (!currentSession) return;

    if (currentSession.status === 'completed') {
      if (currentSession.profile) {
        journeyStateRepository.saveState({ careerProfile: currentSession.profile });
      }
      router.push('/assessment/result');
      return;
    }

    setIsAdvancing(true);
    setErrorMessage(null);
    try {
      const nextSession = await advanceAdaptiveSession(currentSession.sessionId);
      setCurrentSession(nextSession);
      setAdaptiveSession(nextSession);
      setCandidateAnswer('');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to advance to next question';
      setErrorMessage(message);
    } finally {
      setIsAdvancing(false);
    }
  };

  if (!isLoaded || isInitializing) {
    return (
      <JourneyShell currentStepId="interview">
        <div className="flex flex-col items-center justify-center py-20 text-slate-400">
          <span className="w-3 h-3 rounded-full bg-cyan-400 animate-ping mb-4"></span>
          <p className="text-sm font-medium">Configuring calibrated interview questions...</p>
        </div>
      </JourneyShell>
    );
  }

  if (errorMessage && !currentSession) {
    return (
      <JourneyShell currentStepId="interview">
        <div className="max-w-xl mx-auto py-8">
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm mb-4">
            {errorMessage}
          </div>
          <button
            onClick={() => router.push('/assessment/skills')}
            className="px-4 py-2 rounded-lg bg-slate-800 text-slate-200 hover:bg-slate-700 text-xs"
          >
            Return to Skill Selection
          </button>
        </div>
      </JourneyShell>
    );
  }

  const currentQ = currentSession?.currentQuestion;
  const lastExplanation = currentSession?.lastExplanation;
  const totalSkills = currentSession?.progress?.totalSkills || 1;
  const completedSkills = currentSession?.progress?.completedSkills || 0;
  const totalAnswered = currentSession?.progress?.totalAnswered || 0;
  const progressPercent = Math.min(100, Math.round((completedSkills / totalSkills) * 100));

  return (
    <JourneyShell
      currentStepId="interview"
      title="Diagnostic Interview Assessment"
      subtitle="Demonstrate your practical reasoning, architectural decisions, and conceptual mastery."
    >
      <div className="max-w-3xl mx-auto py-2">
        {/* Progress bar */}
        <div className="mb-6">
          <div className="flex items-center justify-between text-xs text-slate-400 mb-1.5 font-medium">
            <span>
              Skill {Math.min(completedSkills + 1, totalSkills)} of {totalSkills} · {totalAnswered} Questions Answered
            </span>
            <span>{progressPercent}% Complete</span>
          </div>
          <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden">
            <div
              className="h-full bg-cyan-400 transition-all duration-300 rounded-full"
              style={{ width: `${progressPercent}%` }}
            ></div>
          </div>
        </div>

        {errorMessage && (
          <div className="p-3.5 rounded-lg bg-red-950/40 border border-red-800 text-red-300 text-xs mb-4" role="alert">
            {errorMessage}
          </div>
        )}

        {/* Post-Answer Explanation Card */}
        {lastExplanation && (
          <div className="p-6 rounded-2xl border border-cyan-800/80 bg-slate-900/90 mb-6 shadow-lg shadow-cyan-950/20" data-testid="post-answer-explanation">
            <div className="flex items-center justify-between pb-3 mb-4 border-b border-slate-800">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-cyan-400"></span>
                <span className="text-xs uppercase font-bold tracking-wider text-cyan-300">
                  Evaluated: {lastExplanation.skillName || lastExplanation.skillId}
                </span>
              </div>
              <span className="text-xs px-2.5 py-0.5 rounded-full border border-cyan-800 bg-cyan-950 text-cyan-200 font-semibold">
                {lastExplanation.evaluatedLevel}
              </span>
            </div>

            {/* What you covered */}
            {lastExplanation.whatYouCovered && lastExplanation.whatYouCovered.length > 0 && (
              <div className="mb-4" data-testid="what-you-covered">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-emerald-400 mb-2 flex items-center gap-1.5">
                  <span>✓</span> What you covered
                </h4>
                <ul className="space-y-1.5 text-xs text-slate-300 bg-slate-950/50 p-3 rounded-lg border border-slate-800/70">
                  {lastExplanation.whatYouCovered.map((cov, idx) => (
                    <li key={idx} className="flex items-start gap-2">
                      <span className="text-emerald-400 font-bold">•</span>
                      <span><TechnicalText text={cov} /></span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* What could be stronger */}
            {lastExplanation.whatCouldBeStronger && lastExplanation.whatCouldBeStronger.length > 0 && (
              <div className="mb-4" data-testid="what-could-be-stronger">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-amber-400 mb-2 flex items-center gap-1.5">
                  <span>▲</span> What could be stronger
                </h4>
                <ul className="space-y-1.5 text-xs text-slate-300 bg-slate-950/50 p-3 rounded-lg border border-slate-800/70">
                  {lastExplanation.whatCouldBeStronger.map((stronger, idx) => (
                    <li key={idx} className="flex items-start gap-2">
                      <span className="text-amber-400 font-bold">•</span>
                      <span><TechnicalText text={stronger} /></span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Reference technical explanation */}
            {lastExplanation.referenceExplanation && (
              <div className="mb-6" data-testid="reference-explanation">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-300 mb-2">
                  Reference Explanation
                </h4>
                <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800 text-xs text-slate-300 leading-relaxed font-sans whitespace-pre-line">
                  <TechnicalText text={lastExplanation.referenceExplanation} />
                </div>
              </div>
            )}

            {/* Action to advance */}
            <div className="flex items-center justify-end pt-2 border-t border-slate-800/80">
              <button
                type="button"
                onClick={handleAdvanceNext}
                disabled={isAdvancing}
                className="px-6 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_15px_rgba(6,182,212,0.35)] transition-all flex items-center gap-2"
                data-testid="next-question-button"
              >
                {isAdvancing ? (
                  <>
                    <span className="w-3 h-3 rounded-full border-2 border-slate-950 border-t-transparent animate-spin"></span>
                    <span>Loading next question...</span>
                  </>
                ) : currentSession.status === 'completed' ? (
                  <span>View Career Readiness Skill Matrix →</span>
                ) : (
                  <span>Next Question →</span>
                )}
              </button>
            </div>
          </div>
        )}

        {/* Question & Answer Card (Visible when not reviewing a submitted answer) */}
        {!lastExplanation && currentQ && (
          <div className="p-6 rounded-2xl border border-slate-800 bg-slate-900/70 mb-6 shadow-sm" data-testid="question-card">
            {/* Metadata Badges */}
            <div className="flex flex-wrap items-center gap-2 mb-4">
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-cyan-950 text-cyan-300 border border-cyan-800/60">
                {currentQ.skillId}
              </span>
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                {currentQ.difficulty}
              </span>
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                {currentQ.questionType}
              </span>
            </div>

            {/* Technical Question Text with UTF-8 NFC, Safe Normalization, and inline code formatting */}
            <h2 className="text-base sm:text-lg font-medium text-slate-100 leading-relaxed mb-6 whitespace-pre-line" data-testid="question-text">
              <TechnicalText text={currentQ.questionText} />
            </h2>

            {/* Answer Input Area */}
            <div className="mb-4">
              <label htmlFor="answer-input" className="block text-xs font-semibold text-slate-300 mb-2">
                Your Technical Explanation:
              </label>
              <textarea
                id="answer-input"
                rows={7}
                value={candidateAnswer}
                onChange={e => setCandidateAnswer(e.target.value)}
                placeholder="Explain your approach, design decisions, edge-case mitigations, and architectural trade-offs..."
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 transition-colors font-mono"
                data-testid="answer-input"
              />
              <div className="flex justify-between items-center text-[11px] text-slate-500 mt-1.5">
                <span>Provide concrete technical details, trade-offs, and examples.</span>
                <span>{candidateAnswer.length} characters</span>
              </div>
            </div>

            {/* Action Bar */}
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={handleSubmitAnswer}
                disabled={isSubmitting || !candidateAnswer.trim()}
                className={`px-5 py-2.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-2 ${
                  candidateAnswer.trim() && !isSubmitting
                    ? 'bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)]'
                    : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                }`}
                data-testid="submit-answer-button"
              >
                {isSubmitting ? (
                  <>
                    <span className="w-3 h-3 rounded-full border-2 border-slate-950 border-t-transparent animate-spin"></span>
                    <span>Evaluating...</span>
                  </>
                ) : (
                  <span>Submit Answer →</span>
                )}
              </button>
            </div>
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
