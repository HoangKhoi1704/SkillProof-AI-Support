'use client';

import { useState, useEffect } from 'react';
import { Role, DiagnosticQuestion, AnswerMap, EvaluationResponse, QualitativeSkillLevel, RoadmapResponse, ProjectRecommendationResponse } from './types';
import { fetchRoles, fetchDiagnosticQuestions, submitDiagnosticEvaluation, generateRoadmap, recommendProject } from './api';

export default function Home() {
  // Navigation & Data state
  const [roles, setRoles] = useState<Role[]>([]);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [questions, setQuestions] = useState<DiagnosticQuestion[]>([]);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState<number>(0);
  const [answers, setAnswers] = useState<AnswerMap>({});
  const [evaluationResult, setEvaluationResult] = useState<EvaluationResponse | null>(null);
  const [roadmapResult, setRoadmapResult] = useState<RoadmapResponse | null>(null);
  const [projectResult, setProjectResult] = useState<ProjectRecommendationResponse | null>(null);
  
  // Status state
  const [isLoadingRoles, setIsLoadingRoles] = useState<boolean>(true);
  const [isLoadingQuestions, setIsLoadingQuestions] = useState<boolean>(false);
  const [isEvaluating, setIsEvaluating] = useState<boolean>(false);
  const [isGeneratingRoadmap, setIsGeneratingRoadmap] = useState<boolean>(false);
  const [isRecommendingProject, setIsRecommendingProject] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isReviewing, setIsReviewing] = useState<boolean>(false);

  // Load roles on mount
  useEffect(() => {
    async function loadRoles() {
      setIsLoadingRoles(true);
      setErrorMessage(null);
      try {
        const data = await fetchRoles();
        setRoles(data);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to load roles';
        setErrorMessage(message);
      } finally {
        setIsLoadingRoles(false);
      }
    }
    loadRoles();
  }, []);

  // Handle role selection and fetch questions
  const handleSelectRole = async (role: Role) => {
    setSelectedRole(role);
    setIsLoadingQuestions(true);
    setErrorMessage(null);
    setCurrentQuestionIndex(0);
    setAnswers({});
    setIsReviewing(false);
    setEvaluationResult(null);
    setRoadmapResult(null);
    setProjectResult(null);

    try {
      const qData = await fetchDiagnosticQuestions(role.id);
      setQuestions(qData);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load diagnostic questions';
      setErrorMessage(message);
    } finally {
      setIsLoadingQuestions(false);
    }
  };

  const handleAnswerChange = (questionId: number, text: string) => {
    setAnswers(prev => ({ ...prev, [questionId]: text }));
  };

  const handleResetToRoleSelection = () => {
    setSelectedRole(null);
    setQuestions([]);
    setAnswers({});
    setIsReviewing(false);
    setEvaluationResult(null);
    setRoadmapResult(null);
    setProjectResult(null);
    setIsGeneratingRoadmap(false);
    setIsRecommendingProject(false);
    setErrorMessage(null);
  };

  // Submit diagnostic answers for evaluation
  const handleSubmitEvaluation = async () => {
    if (!selectedRole) return;
    setIsEvaluating(true);
    setErrorMessage(null);

    try {
      const result = await submitDiagnosticEvaluation(selectedRole.id, answers, questions);
      setEvaluationResult(result);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Evaluation failed';
      setErrorMessage(message);
    } finally {
      setIsEvaluating(false);
    }
  };

  // Generate personalized roadmap from diagnosed top gaps
  const handleGenerateRoadmap = async () => {
    if (!selectedRole || !evaluationResult) return;
    setIsGeneratingRoadmap(true);
    setErrorMessage(null);

    try {
      const result = await generateRoadmap(selectedRole.id, evaluationResult.topGaps, evaluationResult.skills);
      setRoadmapResult(result);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to generate learning roadmap';
      setErrorMessage(message);
    } finally {
      setIsGeneratingRoadmap(false);
    }
  };

  // Recommend gap-backward real-world project
  const handleRecommendProject = async () => {
    if (!selectedRole || !evaluationResult) return;
    setIsRecommendingProject(true);
    setErrorMessage(null);

    try {
      const result = await recommendProject(
        selectedRole.id,
        evaluationResult.topGaps,
        roadmapResult?.items
      );
      setProjectResult(result);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to recommend project';
      setErrorMessage(message);
    } finally {
      setIsRecommendingProject(false);
    }
  };

  const currentQuestion = questions[currentQuestionIndex];
  const totalQuestions = questions.length;
  const answeredCount = Object.values(answers).filter(a => a.trim().length > 0).length;

  const getLevelBadgeClass = (level: QualitativeSkillLevel) => {
    switch (level) {
      case 'Advanced':
        return 'bg-purple-500/10 text-purple-300 border-purple-500/30';
      case 'Intermediate':
        return 'bg-cyan-500/10 text-cyan-300 border-cyan-500/30';
      case 'Beginner':
        return 'bg-amber-500/10 text-amber-300 border-amber-500/30';
      case 'Insufficient Evidence':
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-indigo-500 selection:text-white">
      {/* Header */}
      <header className="border-b border-slate-800/80 bg-slate-900/60 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3 cursor-pointer" onClick={handleResetToRoleSelection}>
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-indigo-500 to-violet-500 flex items-center justify-center font-bold text-white shadow-lg shadow-indigo-500/20">
              SP
            </div>
            <div>
              <span className="font-bold text-lg tracking-tight text-white">SkillProof</span>
              <span className="hidden sm:inline-block ml-2 px-2 py-0.5 text-xs font-medium rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
                AI Career Readiness
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2 text-xs font-mono text-slate-400">
            <span className="text-indigo-400 font-semibold">Assess</span>
            <span>→</span>
            <span className={evaluationResult ? "text-indigo-400 font-semibold" : ""}>Profile</span>
            <span>→</span>
            <span className={roadmapResult ? "text-indigo-400 font-semibold" : ""}>Roadmap</span>
            <span>→</span>
            <span className={projectResult ? "text-emerald-400 font-semibold" : ""}>Build Project</span>
            <span>→</span>
            <span className="text-violet-400 font-semibold">Prove</span>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 max-w-5xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-12 flex flex-col justify-center">
        {/* Error Banner */}
        {errorMessage && (
          <div className="mb-8 p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 flex items-start justify-between gap-3">
            <div>
              <p className="font-semibold text-sm">System Notice</p>
              <p className="text-sm mt-0.5 text-rose-300/90">{errorMessage}</p>
            </div>
            <button 
              onClick={() => setErrorMessage(null)} 
              className="text-rose-400 hover:text-rose-200 text-xs font-medium px-2 py-1 rounded bg-rose-500/20"
            >
              Dismiss
            </button>
          </div>
        )}

        {/* STAGE 1: ROLE SELECTION */}
        {!selectedRole && !evaluationResult && (
          <div className="flex flex-col items-center">
            <div className="text-center max-w-2xl mb-10">
              <span className="px-3 py-1 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 tracking-wide uppercase">
                Career Readiness Diagnostic
              </span>
              <h1 className="text-3xl sm:text-4xl font-bold tracking-tight text-white mt-4">
                Validate Your Job Readiness With Realistic Challenges
              </h1>
              <p className="text-slate-400 mt-3 text-base sm:text-lg leading-relaxed">
                Choose your target career role to begin a diagnostic assessment built from real interview patterns and practical cases.
              </p>
            </div>

            {isLoadingRoles ? (
              <div className="w-full grid grid-cols-1 sm:grid-cols-2 gap-6 max-w-3xl">
                {[1, 2].map(n => (
                  <div key={n} className="p-8 rounded-2xl bg-slate-900/50 border border-slate-800 animate-pulse h-64 flex flex-col justify-between">
                    <div className="h-6 w-32 bg-slate-800 rounded"></div>
                    <div className="h-16 w-full bg-slate-800/60 rounded"></div>
                    <div className="h-10 w-full bg-slate-800 rounded"></div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="w-full grid grid-cols-1 sm:grid-cols-2 gap-6 max-w-3xl">
                {roles.map(role => (
                  <div
                    key={role.id}
                    className="group relative p-7 rounded-2xl bg-slate-900/70 hover:bg-slate-900 border border-slate-800 hover:border-indigo-500/50 transition-all duration-200 flex flex-col justify-between shadow-xl shadow-black/40 hover:shadow-indigo-500/10"
                  >
                    <div>
                      <div className="w-12 h-12 rounded-xl bg-slate-800 group-hover:bg-indigo-600/20 text-indigo-400 flex items-center justify-center text-xl font-bold mb-5 transition-colors">
                        {role.id === 'backend-developer' ? '⚙️' : '📊'}
                      </div>
                      <h2 className="text-xl font-bold text-white group-hover:text-indigo-300 transition-colors">
                        {role.name}
                      </h2>
                      <p className="text-slate-400 text-sm mt-2 leading-relaxed">
                        {role.description}
                      </p>
                      
                      <div className="mt-6 flex flex-wrap gap-1.5">
                        {role.id === 'backend-developer' ? (
                          ['REST API', 'SQL', 'Testing', 'System Design', 'Auth'].map(skill => (
                            <span key={skill} className="px-2 py-0.5 text-xs rounded-md bg-slate-800/80 text-slate-300 border border-slate-700/60">
                              {skill}
                            </span>
                          ))
                        ) : (
                          ['Statements', 'Excel', 'Modeling', 'Forecasting', 'Ratios'].map(skill => (
                            <span key={skill} className="px-2 py-0.5 text-xs rounded-md bg-slate-800/80 text-slate-300 border border-slate-700/60">
                              {skill}
                            </span>
                          ))
                        )}
                      </div>
                    </div>

                    <button
                      onClick={() => handleSelectRole(role)}
                      className="mt-8 w-full py-3 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm transition-all shadow-md shadow-indigo-600/20 group-hover:shadow-indigo-500/30 flex items-center justify-center gap-2"
                    >
                      <span>Start Career Diagnostic</span>
                      <span>→</span>
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* STAGE 2: DIAGNOSTIC QUESTIONS */}
        {selectedRole && !isReviewing && !evaluationResult && (
          <div className="flex flex-col">
            {/* Context & Navigation Bar */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-slate-800">
              <div className="flex items-center gap-3">
                <button
                  onClick={handleResetToRoleSelection}
                  className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  ← Change Role
                </button>
                <div>
                  <h2 className="text-lg font-bold text-white flex items-center gap-2">
                    <span>{selectedRole.name} Diagnostic</span>
                  </h2>
                  <p className="text-xs text-slate-400">Career Readiness Assessment</p>
                </div>
              </div>

              {totalQuestions > 0 && (
                <div className="flex items-center gap-2">
                  <span className="text-xs text-slate-400">
                    Question <span className="text-indigo-400 font-bold">{currentQuestionIndex + 1}</span> of {totalQuestions}
                  </span>
                  <span className="text-xs text-slate-500">|</span>
                  <span className="text-xs text-slate-400">
                    {answeredCount} Answered
                  </span>
                </div>
              )}
            </div>

            {/* Progress Bar */}
            {totalQuestions > 0 && (
              <div className="w-full bg-slate-900 rounded-full h-1.5 mb-8 overflow-hidden">
                <div
                  className="bg-gradient-to-r from-indigo-500 to-violet-500 h-1.5 rounded-full transition-all duration-300"
                  style={{ width: `${((currentQuestionIndex + 1) / totalQuestions) * 100}%` }}
                ></div>
              </div>
            )}

            {/* Question Card */}
            {isLoadingQuestions ? (
              <div className="p-8 rounded-2xl bg-slate-900/60 border border-slate-800 animate-pulse h-96 flex flex-col justify-between">
                <div className="h-6 w-40 bg-slate-800 rounded"></div>
                <div className="h-20 w-full bg-slate-800/70 rounded"></div>
                <div className="h-40 w-full bg-slate-800/40 rounded"></div>
              </div>
            ) : currentQuestion ? (
              <div className="p-6 sm:p-8 rounded-2xl bg-slate-900/80 border border-slate-800 shadow-2xl flex flex-col">
                {/* Meta Badges */}
                <div className="flex flex-wrap items-center gap-2 mb-5">
                  <span className="px-3 py-1 rounded-md text-xs font-semibold bg-indigo-500/10 text-indigo-300 border border-indigo-500/20">
                    {currentQuestion.competency}
                  </span>
                  <span className="px-2.5 py-1 rounded-md text-xs font-medium uppercase tracking-wider bg-slate-800 text-slate-300 border border-slate-700/60">
                    {currentQuestion.type.replace('_', ' ')}
                  </span>
                  <span className="px-2.5 py-1 rounded-md text-xs font-mono bg-slate-800/50 text-slate-400 border border-slate-800">
                    {currentQuestion.sourceReference}
                  </span>
                </div>

                {/* Question Prompt */}
                <h3 className="text-xl sm:text-2xl font-semibold text-white leading-snug mb-6">
                  {currentQuestion.questionText}
                </h3>

                {/* Answer Textarea */}
                <div className="flex flex-col">
                  <div className="flex items-center justify-between mb-2 text-xs text-slate-400">
                    <label htmlFor={`answer-${currentQuestion.id}`} className="font-medium text-slate-300">
                      Your Answer & Reasoning:
                    </label>
                    <span>
                      {(answers[currentQuestion.id] || '').length} characters
                    </span>
                  </div>

                  <textarea
                    id={`answer-${currentQuestion.id}`}
                    rows={6}
                    value={answers[currentQuestion.id] || ''}
                    onChange={(e) => handleAnswerChange(currentQuestion.id, e.target.value)}
                    placeholder="Structure your answer clearly. Explain relevant concepts, trade-offs, step-by-step diagnostic reasoning, and real-world considerations..."
                    className="w-full p-4 rounded-xl bg-slate-950/80 border border-slate-700/80 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 text-slate-100 placeholder-slate-500 text-sm sm:text-base leading-relaxed resize-y transition-all outline-none"
                  />
                  <p className="text-xs text-slate-500 mt-2">
                    💡 Tip: SkillProof evaluates technical depth, problem-solving structure, and evidence of practical understanding.
                  </p>
                </div>

                {/* Action Controls */}
                <div className="mt-8 pt-6 border-t border-slate-800 flex items-center justify-between">
                  <button
                    onClick={() => setCurrentQuestionIndex(prev => Math.max(0, prev - 1))}
                    disabled={currentQuestionIndex === 0}
                    className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed text-slate-200 text-sm font-medium transition-colors"
                  >
                    ← Previous
                  </button>

                  <div className="flex items-center gap-3">
                    {currentQuestionIndex < totalQuestions - 1 ? (
                      <button
                        onClick={() => setCurrentQuestionIndex(prev => prev + 1)}
                        className="px-6 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition-colors shadow-md shadow-indigo-600/20"
                      >
                        Next Question →
                      </button>
                    ) : (
                      <button
                        onClick={() => setIsReviewing(true)}
                        className="px-6 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition-colors shadow-md shadow-indigo-600/20 flex items-center gap-1.5"
                      >
                        <span>Review All Answers</span>
                        <span>→</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ) : null}
          </div>
        )}

        {/* STAGE 3: REVIEW & SUBMIT */}
        {selectedRole && isReviewing && !evaluationResult && (
          <div className="flex flex-col">
            <div className="flex items-center justify-between pb-4 mb-6 border-b border-slate-800">
              <div>
                <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 uppercase tracking-wide">
                  Review Diagnostic Answers
                </span>
                <h2 className="text-2xl font-bold text-white mt-2">
                  {selectedRole.name} Assessment Review
                </h2>
                <p className="text-sm text-slate-400 mt-1">
                  Check your answers before submitting for diagnostic skill evaluation.
                </p>
              </div>

              <button
                onClick={() => setIsReviewing(false)}
                className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition-colors"
              >
                ← Back to Questions
              </button>
            </div>

            {/* Answer List */}
            <div className="space-y-4 mb-8">
              {questions.map((q, idx) => {
                const answer = answers[q.id] || '';
                const hasAnswer = answer.trim().length > 0;
                return (
                  <div
                    key={q.id}
                    className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 flex flex-col gap-2 hover:border-slate-700 transition-colors"
                  >
                    <div className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-indigo-400">Q{idx + 1}</span>
                        <span className="font-semibold text-slate-300">{q.competency}</span>
                        <span className="text-slate-500">({q.type})</span>
                      </div>
                      <span className={`px-2 py-0.5 rounded text-xs font-medium ${hasAnswer ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-amber-500/10 text-amber-400 border border-amber-500/20'}`}>
                        {hasAnswer ? 'Answered' : 'Empty'}
                      </span>
                    </div>

                    <p className="text-sm text-slate-200 font-medium">
                      {q.questionText}
                    </p>

                    <div className="mt-2 p-3 rounded-lg bg-slate-950/70 border border-slate-800 text-xs font-mono text-slate-300 whitespace-pre-wrap">
                      {hasAnswer ? answer : <span className="text-slate-500 italic">No answer entered. (Will be evaluated as Insufficient Evidence)</span>}
                    </div>

                    <div className="flex justify-end mt-1">
                      <button
                        onClick={() => {
                          setCurrentQuestionIndex(idx);
                          setIsReviewing(false);
                        }}
                        className="text-xs text-indigo-400 hover:text-indigo-300 font-medium"
                      >
                        Edit Answer →
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Submit Action Card */}
            <div className="p-6 rounded-2xl bg-gradient-to-br from-indigo-950/50 via-slate-900 to-violet-950/40 border border-indigo-500/30 flex flex-col sm:flex-row items-center justify-between gap-4">
              <div>
                <h3 className="text-base font-bold text-white">Ready for Diagnostic Evaluation</h3>
                <p className="text-xs text-slate-400 mt-1">
                  Evaluates your answers against the role's competency rubric to establish your qualitative 4-tier skill profile.
                </p>
              </div>

              <button
                onClick={handleSubmitEvaluation}
                disabled={isEvaluating}
                className="w-full sm:w-auto px-7 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-medium text-sm transition-all shadow-lg shadow-emerald-600/20 flex items-center justify-center gap-2 whitespace-nowrap"
              >
                {isEvaluating ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                    <span>Evaluating Answers...</span>
                  </>
                ) : (
                  <>
                    <span>Submit Diagnostic Assessment</span>
                    <span>✓</span>
                  </>
                )}
              </button>
            </div>
          </div>
        )}

        {/* STAGE 4: SKILL PROFILE & TOP GAPS (MILESTONE 2 & 3) */}
        {evaluationResult && !roadmapResult && (
          <div className="flex flex-col">
            {/* Header Banner */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 mb-8 border-b border-slate-800">
              <div>
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 uppercase tracking-wide">
                    Milestone 2 — Skill Profile
                  </span>
                  <span className="text-xs text-slate-500">•</span>
                  <span className="text-xs font-medium text-slate-300 capitalize">
                    {selectedRole?.name || evaluationResult.roleId}
                  </span>
                </div>
                <h1 className="text-2xl sm:text-3xl font-bold text-white mt-2 tracking-tight">
                  Career Readiness Skill Profile
                </h1>
                <p className="text-sm text-slate-400 mt-1">
                  Qualitative 4-tier evaluation based on demonstrated technical reasoning and evidence.
                </p>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={() => {
                    setEvaluationResult(null);
                    setIsReviewing(true);
                  }}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  ← Review Answers
                </button>
                <button
                  onClick={handleResetToRoleSelection}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  Change Role
                </button>
              </div>
            </div>

            {/* Top Skill Gaps Banner */}
            <div className="mb-8 p-6 rounded-2xl bg-gradient-to-r from-amber-950/30 via-slate-900 to-rose-950/20 border border-amber-500/30 shadow-xl shadow-black/30">
              <div className="flex items-center gap-2 mb-2">
                <span className="w-2 h-2 rounded-full bg-amber-400 animate-pulse"></span>
                <h2 className="text-xs font-bold uppercase tracking-wider text-amber-300">
                  Top Prioritized Skill Gaps (Max 3)
                </h2>
              </div>
              <p className="text-xs text-slate-400 mb-4">
                These core gaps directly anchor your personalized learning roadmap and gap-backward project recommendation.
              </p>
              
              <div data-testid="top-gaps-list" className="flex flex-wrap gap-3">
                {evaluationResult.topGaps.map((gap, idx) => (
                  <div
                    key={gap}
                    className="flex items-center gap-2.5 px-4 py-2 rounded-xl bg-slate-900/90 border border-amber-500/40 text-amber-200 text-sm font-semibold shadow-md"
                  >
                    <span className="w-5 h-5 rounded-full bg-amber-500/20 text-amber-300 flex items-center justify-center text-xs font-mono font-bold">
                      {idx + 1}
                    </span>
                    <span>{gap}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Competencies Grid */}
            <div className="mb-10">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold text-white">Competency Breakdown</h2>
                <span className="text-xs text-slate-400">
                  4 Allowed Tiers: Beginner • Intermediate • Advanced • Insufficient Evidence
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {evaluationResult.skills.map(skill => (
                  <div
                    key={skill.name}
                    className="p-5 rounded-xl bg-slate-900/70 border border-slate-800/90 hover:border-slate-700 transition-all flex flex-col justify-between shadow-md"
                  >
                    <div>
                      <div className="flex items-center justify-between gap-2 mb-3">
                        <h3 className="font-bold text-white text-base">
                          {skill.name}
                        </h3>
                        <span className={`px-2.5 py-0.5 rounded-md text-xs font-bold border ${getLevelBadgeClass(skill.level)}`}>
                          {skill.level}
                        </span>
                      </div>

                      <p className="text-xs text-slate-300 leading-relaxed mb-3">
                        {skill.reason}
                      </p>
                    </div>

                    {skill.evidence && skill.evidence.length > 0 && (
                      <div className="mt-2 pt-3 border-t border-slate-800/80">
                        <p className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">
                          Demonstrated Evidence:
                        </p>
                        <ul className="space-y-1">
                          {skill.evidence.map((ev: string, evIdx: number) => (
                            <li key={evIdx} className="text-xs text-slate-400 flex items-start gap-1.5">
                              <span className="text-indigo-400 font-bold">•</span>
                              <span>{ev}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* Milestone 2 Exit Confirmation Card */}
            <div className="mb-6 p-5 rounded-2xl bg-slate-900/60 border border-slate-800 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-base border border-emerald-500/30">
                  ✓
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">
                    Milestone 2 Exit Criteria Satisfied
                  </h3>
                  <p className="text-xs text-slate-400">
                    Diagnostic submitted and evaluated into qualitative 4-tier profile with prioritized skill gaps.
                  </p>
                </div>
              </div>
              <span className="px-3 py-1 rounded-lg bg-slate-800 text-xs text-slate-400 font-mono hidden sm:inline-block">
                Evaluation Verified
              </span>
            </div>

            {/* Milestone 4: Next Step Action Card (Generate Roadmap) */}
            <div className="p-6 rounded-2xl bg-gradient-to-br from-indigo-950/60 via-slate-900 to-emerald-950/40 border border-indigo-500/40 shadow-xl flex flex-col sm:flex-row items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                  <span className="text-xs font-bold uppercase tracking-wider text-emerald-300">
                    Next Step — Learning Roadmap
                  </span>
                </div>
                <h3 className="text-lg font-bold text-white">
                  Ready for Your Personalized Learning Roadmap?
                </h3>
                <p className="text-xs text-slate-400 mt-1 max-w-xl">
                  Transforms your top diagnosed gaps ({evaluationResult.topGaps.join(', ')}) into high-impact learning goals and concrete, role-specific practice activities.
                </p>
              </div>

              <button
                data-testid="generate-roadmap-btn"
                onClick={handleGenerateRoadmap}
                disabled={isGeneratingRoadmap}
                className="w-full sm:w-auto px-7 py-3.5 rounded-xl bg-gradient-to-r from-indigo-600 to-emerald-600 hover:from-indigo-500 hover:to-emerald-500 disabled:opacity-50 text-white font-medium text-sm transition-all shadow-lg shadow-indigo-600/25 flex items-center justify-center gap-2 whitespace-nowrap"
              >
                {isGeneratingRoadmap ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                    <span>Synthesizing Roadmap...</span>
                  </>
                ) : (
                  <>
                    <span>Generate My Roadmap</span>
                    <span>→</span>
                  </>
                )}
              </button>
            </div>
          </div>
        )}

        {/* STAGE 5: PERSONALIZED LEARNING ROADMAP (MILESTONE 4) */}
        {roadmapResult && !projectResult && (
          <div className="flex flex-col">
            {/* Header Banner */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 mb-8 border-b border-slate-800">
              <div>
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 uppercase tracking-wide">
                    Milestone 4 — Personalized Roadmap
                  </span>
                  <span className="text-xs text-slate-500">•</span>
                  <span className="text-xs font-medium text-slate-300 capitalize">
                    {selectedRole?.name || roadmapResult.roleId}
                  </span>
                </div>
                <h1 className="text-2xl sm:text-3xl font-bold text-white mt-2 tracking-tight">
                  Personalized Career Readiness Roadmap
                </h1>
                <p className="text-sm text-slate-400 mt-1">
                  Actionable learning goals and hands-on practice activities engineered specifically around your top diagnosed skill gaps.
                </p>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={() => setRoadmapResult(null)}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  ← Back to Skill Profile
                </button>
                <button
                  onClick={handleResetToRoleSelection}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  Change Role
                </button>
              </div>
            </div>

            {/* Gap Addressing Context Banner */}
            <div className="mb-8 p-5 rounded-xl bg-slate-900/60 border border-slate-800 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center font-bold text-lg border border-emerald-500/20">
                  🗺️
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Targeted Gap Remediation</h3>
                  <p className="text-xs text-slate-400">
                    Prioritizes your {roadmapResult.items.length} critical gaps in sequence to prepare for the gap-backward project.
                  </p>
                </div>
              </div>

              <span className="px-3 py-1 rounded-full bg-emerald-500/10 text-emerald-400 text-xs font-semibold border border-emerald-500/20 hidden sm:inline-block">
                {roadmapResult.items.length} Prioritized Focus Areas
              </span>
            </div>

            {/* Roadmap Items List */}
            <div data-testid="roadmap-items-list" className="space-y-6 mb-8">
              {roadmapResult.items.map((item) => (
                <div
                  key={item.skill}
                  className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/90 to-slate-950/80 border border-slate-800 hover:border-slate-700 transition-all shadow-xl shadow-black/20"
                >
                  {/* Card Header */}
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 mb-4 border-b border-slate-800/80">
                    <div className="flex items-center gap-3">
                      <span className={`px-3 py-1 rounded-lg text-xs font-bold font-mono ${
                        item.priority === 1
                          ? 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                          : item.priority === 2
                          ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                          : 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/30'
                      }`}>
                        Priority {item.priority}
                      </span>
                      <h2 className="text-lg font-bold text-white">
                        {item.skill}
                      </h2>
                    </div>

                    <span className="text-xs text-slate-400 font-medium">
                      {item.priority === 1 ? 'Immediate High-Impact Gap' : item.priority === 2 ? 'Core Competency Building' : 'Targeted Enhancement'}
                    </span>
                  </div>

                  {/* Two Column Layout for Learning Goal and Practice Activity */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Learning Goal Box */}
                    <div className="p-4 rounded-xl bg-slate-950/70 border border-indigo-500/20 flex flex-col gap-2">
                      <div className="flex items-center gap-2">
                        <span className="text-base">🎯</span>
                        <h3 className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                          Target Learning Goal
                        </h3>
                      </div>
                      <p className="text-xs sm:text-sm text-slate-200 leading-relaxed">
                        {item.learningGoal}
                      </p>
                    </div>

                    {/* Actionable Practice Activity Box */}
                    <div className="p-4 rounded-xl bg-slate-950/70 border border-emerald-500/20 flex flex-col gap-2">
                      <div className="flex items-center gap-2">
                        <span className="text-base">🛠️</span>
                        <h3 className="text-xs font-bold uppercase tracking-wider text-emerald-400">
                          Actionable Practice Task
                        </h3>
                      </div>
                      <p className="text-xs sm:text-sm text-slate-200 leading-relaxed">
                        {item.practiceTask}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Milestone 4 Exit Confirmation Card */}
            <div className="mb-6 p-6 rounded-2xl bg-gradient-to-br from-emerald-950/40 via-slate-900 to-indigo-950/30 border border-emerald-500/30 text-center flex flex-col items-center">
              <div className="w-12 h-12 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-xl font-bold mb-3 border border-emerald-500/30">
                ✓
              </div>
              <h3 className="text-lg font-bold text-white">
                Milestone 4 Exit Criteria Satisfied
              </h3>
              <p className="text-slate-300 text-sm mt-1 max-w-lg">
                Personalized learning roadmap generated directly from diagnosed top skill gaps with actionable practice activities and zero invented student experience.
              </p>
            </div>

            {/* Milestone 5: Next Step Action Card (Build a Project) */}
            <div className="p-6 rounded-2xl bg-gradient-to-br from-indigo-950/60 via-slate-900 to-emerald-950/40 border border-indigo-500/40 shadow-xl flex flex-col sm:flex-row items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
                  <span className="text-xs font-bold uppercase tracking-wider text-indigo-300">
                    Next Step — Real-World Project
                  </span>
                </div>
                <h3 className="text-lg font-bold text-white">
                  Ready to Build Observable Evidence?
                </h3>
                <p className="text-xs text-slate-400 mt-1 max-w-xl">
                  Engineers ONE focused real-world project designed backward from your diagnosed top gaps ({evaluationResult?.topGaps.join(', ')}).
                </p>
              </div>

              <button
                data-testid="recommend-project-btn"
                onClick={handleRecommendProject}
                disabled={isRecommendingProject}
                className="w-full sm:w-auto px-7 py-3.5 rounded-xl bg-gradient-to-r from-emerald-600 to-indigo-600 hover:from-emerald-500 hover:to-indigo-500 disabled:opacity-50 text-white font-medium text-sm transition-all shadow-lg shadow-indigo-600/25 flex items-center justify-center gap-2 whitespace-nowrap"
              >
                {isRecommendingProject ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                    <span>Designing Gap-Backward Project...</span>
                  </>
                ) : (
                  <>
                    <span>Build a Project</span>
                    <span>→</span>
                  </>
                )}
              </button>
            </div>
          </div>
        )}

        {/* STAGE 6: RECOMMENDED REAL-WORLD PROJECT (MILESTONE 5) */}
        {projectResult && (
          <div className="flex flex-col">
            {/* Header Banner */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 mb-8 border-b border-slate-800">
              <div>
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 uppercase tracking-wide">
                    Milestone 5 — Gap-Backward Project
                  </span>
                  <span className="text-xs text-slate-500">•</span>
                  <span className="text-xs font-medium text-slate-300 capitalize">
                    {selectedRole?.name || evaluationResult?.roleId}
                  </span>
                </div>
                <h1 className="text-2xl sm:text-3xl font-bold text-white mt-2 tracking-tight">
                  Recommended Real-World Portfolio Project
                </h1>
                <p className="text-sm text-slate-400 mt-1">
                  Engineered backward from your diagnosed skill gaps to provide hands-on practice and produce observable evidence of job readiness.
                </p>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={() => setProjectResult(null)}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  ← Back to Roadmap
                </button>
                <button
                  onClick={handleResetToRoleSelection}
                  className="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  Change Role
                </button>
              </div>
            </div>

            {/* Strategic Rationale Banner (WHY this project was recommended) */}
            <div className="mb-8 p-6 rounded-2xl bg-gradient-to-r from-indigo-950/40 via-slate-900 to-violet-950/30 border border-indigo-500/40 shadow-xl shadow-black/30">
              <div className="flex items-center gap-2 mb-2">
                <span className="w-2.5 h-2.5 rounded-full bg-indigo-400 animate-pulse"></span>
                <h2 className="text-xs font-bold uppercase tracking-wider text-indigo-300">
                  Strategic Gap-Backward Rationale
                </h2>
              </div>
              <p className="text-sm text-slate-200 leading-relaxed font-medium">
                {projectResult.reason}
              </p>
              
              <div className="mt-4 pt-3 border-t border-indigo-500/20 flex flex-wrap items-center gap-2">
                <span className="text-xs text-slate-400">Directly addresses diagnosed gaps:</span>
                {evaluationResult?.topGaps.map(gap => (
                  <span key={gap} className="px-2.5 py-0.5 rounded-md bg-indigo-500/10 text-indigo-300 text-xs font-semibold border border-indigo-500/30">
                    {gap}
                  </span>
                ))}
              </div>
            </div>

            {/* Project Overview Card */}
            <div className="mb-8 p-7 rounded-2xl bg-gradient-to-br from-slate-900/90 to-slate-950/80 border border-slate-800 shadow-xl">
              <div className="flex items-start justify-between gap-4 mb-4">
                <div>
                  <span className="text-xs font-semibold uppercase tracking-wider text-emerald-400">
                    Target Portfolio Project
                  </span>
                  <h2 className="text-2xl sm:text-3xl font-bold text-white mt-1">
                    {projectResult.title}
                  </h2>
                </div>
                <div className="w-12 h-12 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center text-2xl border border-emerald-500/20 shrink-0">
                  📁
                </div>
              </div>

              <p className="text-slate-300 text-sm sm:text-base leading-relaxed mb-6">
                {projectResult.description}
              </p>

              {/* Expected Deliverables Chips */}
              {projectResult.expectedDeliverables && projectResult.expectedDeliverables.length > 0 && (
                <div className="pt-4 border-t border-slate-800">
                  <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">
                    Observable Deliverables & Evidence Artifacts:
                  </h3>
                  <div className="flex flex-wrap gap-2">
                    {projectResult.expectedDeliverables.map((deliv, idx) => (
                      <div
                        key={idx}
                        className="px-3 py-1.5 rounded-lg bg-slate-950/80 border border-slate-700 text-xs text-slate-300 flex items-center gap-2"
                      >
                        <span className="text-emerald-400 font-bold">📄</span>
                        <span>{deliv}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* Project Requirements Mapped to Diagnosed Gaps */}
            <div className="mb-10">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold text-white">Project Requirements Mapped to Diagnosed Gaps</h2>
                <span className="text-xs text-slate-400">
                  Every major requirement develops a diagnosed weakness
                </span>
              </div>

              <div data-testid="project-requirements-list" className="space-y-4">
                {projectResult.requirements.map((req, idx) => (
                  <div
                    key={idx}
                    className="p-5 rounded-xl bg-slate-900/80 border border-slate-800 hover:border-slate-700 transition-all flex flex-col gap-3 shadow-md"
                  >
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <div className="flex items-center gap-2.5">
                        <span className="w-6 h-6 rounded-md bg-indigo-500/20 text-indigo-300 text-xs font-bold flex items-center justify-center font-mono">
                          {idx + 1}
                        </span>
                        <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">
                          Requirement
                        </span>
                      </div>

                      <div className="flex items-center gap-2">
                        <span className="text-xs text-slate-400">Develops:</span>
                        <span className="px-3 py-1 rounded-md text-xs font-bold bg-amber-500/10 text-amber-300 border border-amber-500/30">
                          {req.targetsSkill}
                        </span>
                      </div>
                    </div>

                    <p className="text-sm text-slate-100 font-medium leading-relaxed">
                      {req.requirement}
                    </p>

                    {req.deliverable && (
                      <div className="p-3 rounded-lg bg-slate-950/70 border border-slate-800/80 text-xs flex items-start gap-2 text-slate-300">
                        <span className="text-emerald-400 font-bold">↳ Deliverable:</span>
                        <span className="font-mono text-slate-300">{req.deliverable}</span>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* Milestone 5 Exit Confirmation Card */}
            <div className="p-6 rounded-2xl bg-gradient-to-br from-emerald-950/40 via-slate-900 to-indigo-950/30 border border-emerald-500/30 text-center flex flex-col items-center">
              <div className="w-12 h-12 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-xl font-bold mb-3 border border-emerald-500/30">
                ✓
              </div>
              <h3 className="text-lg font-bold text-white">
                Milestone 5 Exit Criteria Satisfied
              </h3>
              <p className="text-slate-300 text-sm mt-1 max-w-lg">
                Gap-backward real-world project recommended with explicit requirement-to-gap mapping, observable deliverables, and strict schema validation.
              </p>
              <div className="mt-4 inline-flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-slate-800/80 border border-slate-700 text-xs text-slate-400 font-mono">
                <span>Next: Milestone 6 — Candidate Project Submission & Verification</span>
              </div>
            </div>
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800/80 py-6 text-center text-xs text-slate-500">
        <p>SkillProof © 2026 — AI Career Readiness Diagnostic & Project Coach</p>
        <p className="mt-1 text-slate-600">This assessment is intended to guide skill development and is based on the information and evidence provided.</p>
      </footer>
    </div>
  );
}
