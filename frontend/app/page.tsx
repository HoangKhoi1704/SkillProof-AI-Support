'use client';

import { useState, useEffect } from 'react';
import { Role, DiagnosticQuestion, AnswerMap, EvaluationResponse, QualitativeSkillLevel, RoadmapResponse, ProjectRecommendationResponse, AssessmentSelection, AssessmentCoverageDto, AdaptiveSessionResponse, SkillEvaluationItem, CareerReadinessProfile, GapBasedProject, SubmitProjectEvidenceRequest, ProjectEvaluation } from './types';
import { fetchRoles, fetchDiagnosticQuestions, selectDiagnosticQuestions, submitDiagnosticEvaluation, generateRoadmap, generateAdaptiveRoadmap, recommendProject, recommendAdaptiveProject, submitAdaptiveProjectEvidence, startAdaptiveSession, submitAdaptiveAnswer } from './api';
import AssessmentSetup from './components/AssessmentSetup';

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
  
  // Assessment Setup & Selection state (Milestones I2 & I3)
  const [isSettingUpAssessment, setIsSettingUpAssessment] = useState<boolean>(false);
  const [assessmentSelection, setAssessmentSelection] = useState<AssessmentSelection | null>(null);
  const [assessmentCoverage, setAssessmentCoverage] = useState<AssessmentCoverageDto | null>(null);

  // Adaptive Diagnostic state (Milestone I6)
  const [adaptiveSession, setAdaptiveSession] = useState<AdaptiveSessionResponse | null>(null);
  const [completedAdaptiveSessionId, setCompletedAdaptiveSessionId] = useState<string | null>(null);
  const [currentAdaptiveAnswer, setCurrentAdaptiveAnswer] = useState<string>('');
  const [isSubmittingAdaptive, setIsSubmittingAdaptive] = useState<boolean>(false);

  // Career Readiness Profile state (Milestone I7)
  const [careerProfile, setCareerProfile] = useState<CareerReadinessProfile | null>(null);
  const [expandedSkills, setExpandedSkills] = useState<Record<string, boolean>>({});

  const toggleSkillExplain = (skillId: string) => {
    setExpandedSkills(prev => ({
      ...prev,
      [skillId]: !prev[skillId]
    }));
  };

  // Gap-Based Project & Evaluation state (Milestone I9)
  const [adaptiveProjectResult, setAdaptiveProjectResult] = useState<GapBasedProject | null>(null);
  const [projectEvidenceForm, setProjectEvidenceForm] = useState<SubmitProjectEvidenceRequest>({
    repositoryUrl: '',
    projectSummary: '',
    implementationExplanation: '',
    architectureDecisions: '',
    testingExplanation: '',
    evidenceExcerpts: []
  });
  const [isSubmittingEvidence, setIsSubmittingEvidence] = useState<boolean>(false);
  const [projectEvaluationResult, setProjectEvaluationResult] = useState<ProjectEvaluation | null>(null);

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

  // Handle role selection (Backend Developer enters Assessment Setup; others proceed directly)
  const handleSelectRole = async (role: Role) => {
    if (role.id === 'backend-developer') {
      setSelectedRole(role);
      setIsSettingUpAssessment(true);
      setErrorMessage(null);
      setCurrentQuestionIndex(0);
      setAnswers({});
      setIsReviewing(false);
      setEvaluationResult(null);
      setCareerProfile(null);
      setRoadmapResult(null);
      setProjectResult(null);
      return;
    }

    // Preserve existing flow for other roles (e.g. financial-analyst)
    setSelectedRole(role);
    setIsSettingUpAssessment(false);
    setAssessmentSelection(null);
    setAssessmentCoverage(null);
    setIsLoadingQuestions(true);
    setErrorMessage(null);
    setCurrentQuestionIndex(0);
    setAnswers({});
    setIsReviewing(false);
    setEvaluationResult(null);
    setCareerProfile(null);
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

  // Continue from Assessment Setup to Questions
  const handleContinueFromSetup = async (selection: AssessmentSelection, isAdaptive = false) => {
    setAssessmentSelection(selection);
    setIsSettingUpAssessment(false);
    setIsLoadingQuestions(true);
    setErrorMessage(null);
    setCurrentQuestionIndex(0);
    setAnswers({});
    setIsReviewing(false);

    const normalizedSkillIds = (selection.selectedSkillIds || []).filter(
      id => id !== selection.primaryLanguageId
    );

    if (isAdaptive) {
      try {
        const res = await startAdaptiveSession(
          selection.roleId,
          normalizedSkillIds,
          selection.primaryLanguageId
        );
        setAdaptiveSession(res);
        setCurrentAdaptiveAnswer('');
        if (res.unsupportedSkillIds && res.unsupportedSkillIds.length > 0) {
          setAssessmentCoverage({
            requestedSkillIds: selection.selectedSkillIds,
            assessedSkillIds: [],
            unsupportedSkillIds: res.unsupportedSkillIds
          });
        }
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to start adaptive session';
        setErrorMessage(message);
      } finally {
        setIsLoadingQuestions(false);
      }
      return;
    }

    try {
      const res = await selectDiagnosticQuestions(
        selection.roleId,
        normalizedSkillIds,
        selection.primaryLanguageId
      );
      setAssessmentCoverage(res.assessmentCoverage);
      const mappedQuestions: DiagnosticQuestion[] = res.questions.map(q => ({
        id: q.id,
        careerRoleId: res.roleId,
        competencyId: q.competencyId,
        competency: q.competency,
        difficulty: q.difficulty,
        type: q.questionType,
        questionType: q.questionType,
        questionText: q.questionText || q.question,
        sourceType: 'sqlite-catalog',
        sourceReference: 'sqlite://catalog/questions'
      }));
      setQuestions(mappedQuestions);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load diagnostic questions';
      setErrorMessage(message);
    } finally {
      setIsLoadingQuestions(false);
    }
  };

  const handleBackToSetup = () => {
    setIsSettingUpAssessment(true);
    setQuestions([]);
    setAnswers({});
    setAdaptiveSession(null);
    setCompletedAdaptiveSessionId(null);
    setCurrentAdaptiveAnswer('');
    setIsReviewing(false);
  };

  const handleAnswerChange = (questionId: number | string, text: string) => {
    setAnswers(prev => ({ ...prev, [questionId]: text }));
  };

  const handleResetToRoleSelection = () => {
    setSelectedRole(null);
    setIsSettingUpAssessment(false);
    setAssessmentSelection(null);
    setAssessmentCoverage(null);
    setQuestions([]);
    setAnswers({});
    setAdaptiveSession(null);
    setCompletedAdaptiveSessionId(null);
    setCurrentAdaptiveAnswer('');
    setIsReviewing(false);
    setEvaluationResult(null);
    setRoadmapResult(null);
    setProjectResult(null);
    setIsGeneratingRoadmap(false);
    setIsRecommendingProject(false);
    setErrorMessage(null);
  };

  const handleSubmitAdaptiveAnswer = async () => {
    if (!adaptiveSession || !adaptiveSession.currentQuestion) return;
    setIsSubmittingAdaptive(true);
    setErrorMessage(null);

    try {
      const res = await submitAdaptiveAnswer(
        adaptiveSession.sessionId,
        adaptiveSession.currentQuestion.id,
        currentAdaptiveAnswer
      );

      if (res.status === 'completed') {
        const mappedSkills: SkillEvaluationItem[] = (res.skills || []).map(s => ({
          name: s.skillName,
          level: s.finalLevel,
          reason: s.reason,
          evidence: s.evidence
        }));
        setEvaluationResult({
          roleId: res.roleId,
          skills: mappedSkills,
          topGaps: res.topGaps || []
        });
        if (res.profile) {
          setCareerProfile(res.profile);
        }
        setCompletedAdaptiveSessionId(res.sessionId);
        setAdaptiveSession(null);
        setCurrentAdaptiveAnswer('');
      } else {
        setAdaptiveSession(res);
        setCurrentAdaptiveAnswer('');
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to submit adaptive answer';
      setErrorMessage(message);
    } finally {
      setIsSubmittingAdaptive(false);
    }
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
    if (!selectedRole) return;
    setIsGeneratingRoadmap(true);
    setErrorMessage(null);

    try {
      if (completedAdaptiveSessionId) {
        const result = await generateAdaptiveRoadmap(completedAdaptiveSessionId);
        setRoadmapResult(result);
      } else if (adaptiveSession) {
        const result = await generateAdaptiveRoadmap(adaptiveSession.sessionId);
        setRoadmapResult(result);
      } else if (evaluationResult) {
        const result = await generateRoadmap(selectedRole.id, evaluationResult.topGaps, evaluationResult.skills);
        setRoadmapResult(result);
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to generate learning roadmap';
      setErrorMessage(message);
    } finally {
      setIsGeneratingRoadmap(false);
    }
  };

  // Recommend gap-backward real-world project
  const handleRecommendProject = async () => {
    if (!selectedRole) return;
    setIsRecommendingProject(true);
    setErrorMessage(null);

    try {
      if (completedAdaptiveSessionId) {
        const result = await recommendAdaptiveProject(completedAdaptiveSessionId);
        setAdaptiveProjectResult(result);
      } else if (evaluationResult) {
        const result = await recommendProject(
          selectedRole.id,
          evaluationResult.topGaps,
          roadmapResult?.items
        );
        setProjectResult(result);
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to recommend project';
      setErrorMessage(message);
    } finally {
      setIsRecommendingProject(false);
    }
  };

  // Submit project evidence for qualitative evaluation & portfolio proof
  const handleSubmitProjectEvidence = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!completedAdaptiveSessionId || !adaptiveProjectResult) return;
    setIsSubmittingEvidence(true);
    setErrorMessage(null);

    try {
      const evaluation = await submitAdaptiveProjectEvidence(
        completedAdaptiveSessionId,
        projectEvidenceForm
      );
      setProjectEvaluationResult(evaluation);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to evaluate project evidence';
      setErrorMessage(message);
    } finally {
      setIsSubmittingEvidence(false);
    }
  };

  // Re-assess Skills entry point: resets project proof and loops back to setup
  const handleReassessSkills = () => {
    setAdaptiveProjectResult(null);
    setProjectEvaluationResult(null);
    setProjectResult(null);
    setRoadmapResult(null);
    setEvaluationResult(null);
    setCareerProfile(null);
    setCompletedAdaptiveSessionId(null);
    setAdaptiveSession(null);
    setAnswers({});
    setCurrentQuestionIndex(0);
    setIsSettingUpAssessment(true);
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

        {/* STAGE 1.5: ASSESSMENT SETUP (BACKEND DEVELOPER) */}
        {selectedRole && isSettingUpAssessment && !evaluationResult && (
          <AssessmentSetup
            role={selectedRole}
            onContinue={handleContinueFromSetup}
            onBack={handleResetToRoleSelection}
            initialSelection={assessmentSelection}
          />
        )}

        {/* STAGE 2-ADAPTIVE: ADAPTIVE CAREER READINESS DIAGNOSTIC */}
        {selectedRole && !isSettingUpAssessment && adaptiveSession && !evaluationResult && (
          <div className="flex flex-col" data-testid="adaptive-diagnostic-view">
            {/* Context & Navigation Bar */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-slate-800">
              <div className="flex items-center gap-3">
                <button
                  onClick={handleResetToRoleSelection}
                  className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
                >
                  ← Change Role
                </button>
                <button
                  onClick={handleBackToSetup}
                  className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors flex items-center gap-1.5"
                  data-testid="adaptive-back-to-setup-button"
                >
                  <span>⚙️</span>
                  <span>Setup</span>
                </button>
                <div>
                  <h2 className="text-lg font-bold text-white flex items-center gap-2">
                    <span>{selectedRole.name} Adaptive Diagnostic</span>
                    <span className="text-xs px-2.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 font-medium">
                      ⚡ Adaptive Engine
                    </span>
                  </h2>
                  <p className="text-xs text-slate-400">Two-Stage Competency & Branching Assessment</p>
                </div>
              </div>

              {adaptiveSession.progress && (
                <div className="flex items-center gap-3 text-xs">
                  <span className="px-2.5 py-1 rounded-md bg-slate-900 border border-slate-800 text-slate-300 font-medium">
                    Skill <span className="text-indigo-400 font-bold">{adaptiveSession.progress.currentSkillIndex + 1}</span> of {adaptiveSession.progress.totalSkills}: <span className="text-white font-semibold">{adaptiveSession.progress.currentSkillName}</span>
                  </span>
                  <span className="px-2.5 py-1 rounded-md bg-slate-900 border border-slate-800 text-slate-300">
                    Question <span className="text-indigo-400 font-bold">{adaptiveSession.progress.totalAnswered + 1}</span>
                  </span>
                </div>
              )}
            </div>

            {/* Neutral Intro / Progression Banner */}
            <div className="mb-6 p-4 rounded-xl bg-indigo-950/30 border border-indigo-500/30 text-xs text-slate-300 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <span className="text-lg">🎯</span>
                <div>
                  <span className="font-semibold text-white">Targeted Competency Diagnostic: </span>
                  <span className="text-slate-300">
                    {adaptiveSession.progress?.stage === 'applied'
                      ? 'Beginning with a practical applied challenge to observe your problem-solving approach.'
                      : 'Verifying competency depth with a targeted follow-up question.'}
                  </span>
                </div>
              </div>
              <span className={`px-3 py-1 rounded-full font-mono text-[11px] font-semibold border ${
                adaptiveSession.progress?.stage === 'applied'
                  ? 'bg-indigo-500/10 text-indigo-300 border-indigo-500/30'
                  : 'bg-cyan-500/10 text-cyan-300 border-cyan-500/30'
              }`}>
                {adaptiveSession.progress?.stage === 'applied' ? 'Stage 1: Applied' : 'Stage 2: Follow-up'}
              </span>
            </div>

            {/* Unsupported catalog skills notice */}
            {assessmentCoverage && assessmentCoverage.unsupportedSkillIds && assessmentCoverage.unsupportedSkillIds.length > 0 && (
              <div
                className="mb-6 p-4 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 text-xs flex items-start gap-3"
                data-testid="adaptive-unsupported-notice"
              >
                <span className="text-base">ℹ️</span>
                <div>
                  <p className="font-semibold text-slate-200">Catalog-Only Skills Selected</p>
                  <p className="text-slate-400 mt-0.5">
                    The following selections are catalog skills — assessment questions not available in this prototype: <span className="font-mono text-indigo-300">{assessmentCoverage.unsupportedSkillIds.join(', ')}</span>.
                  </p>
                  <p className="text-slate-500 mt-0.5 italic">Not included in the current diagnostic.</p>
                </div>
              </div>
            )}

            {/* Progress Bar */}
            {adaptiveSession.progress && (
              <div className="w-full bg-slate-900 rounded-full h-1.5 mb-8 overflow-hidden">
                <div
                  className="bg-gradient-to-r from-indigo-500 via-cyan-500 to-emerald-500 h-1.5 rounded-full transition-all duration-300"
                  style={{ width: `${Math.min(100, Math.max(5, ((adaptiveSession.progress.totalAnswered + 1) / (adaptiveSession.progress.totalSkills * 2)) * 100))}%` }}
                ></div>
              </div>
            )}

            {/* Question Card */}
            {adaptiveSession.currentQuestion && (
              <div className="p-6 sm:p-8 rounded-2xl bg-slate-900/80 border border-slate-800 shadow-2xl flex flex-col">
                {/* Meta Badges */}
                <div className="flex flex-wrap items-center gap-2 mb-5">
                  <span className="px-3 py-1 rounded-md text-xs font-semibold bg-indigo-500/10 text-indigo-300 border border-indigo-500/20" data-testid="adaptive-skill-badge">
                    {adaptiveSession.currentQuestion.skillName}
                  </span>
                  <span className="px-2.5 py-1 rounded-md text-xs font-semibold uppercase tracking-wider bg-amber-500/10 text-amber-300 border border-amber-500/30" data-testid="adaptive-difficulty-badge">
                    {adaptiveSession.currentQuestion.difficulty}
                  </span>
                  <span className="px-2.5 py-1 rounded-md text-xs font-mono uppercase bg-slate-800 text-slate-300 border border-slate-700/60">
                    {adaptiveSession.currentQuestion.questionType.replace('_', ' ')}
                  </span>
                  <span className="px-2.5 py-1 rounded-md text-xs font-medium bg-slate-850 text-slate-400 border border-slate-800">
                    {adaptiveSession.currentQuestion.id}
                  </span>
                </div>

                {/* Question Prompt */}
                <h3 className="text-xl sm:text-2xl font-semibold text-white leading-snug mb-6" data-testid="adaptive-question-text">
                  {adaptiveSession.currentQuestion.questionText}
                </h3>

                {/* Answer Textarea */}
                <div className="flex flex-col">
                  <div className="flex items-center justify-between mb-2 text-xs text-slate-400">
                    <label htmlFor={`adaptive-answer-${adaptiveSession.currentQuestion.id}`} className="font-medium text-slate-300">
                      Your Answer & Technical Explanation:
                    </label>
                    <span>
                      {currentAdaptiveAnswer.length} characters
                    </span>
                  </div>

                  <textarea
                    id={`adaptive-answer-${adaptiveSession.currentQuestion.id}`}
                    rows={6}
                    value={currentAdaptiveAnswer}
                    onChange={(e) => setCurrentAdaptiveAnswer(e.target.value)}
                    placeholder="Provide your solution, technical reasoning, and real-world considerations..."
                    className="w-full p-4 rounded-xl bg-slate-950/80 border border-slate-700/80 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 text-slate-100 placeholder-slate-500 text-sm sm:text-base leading-relaxed resize-y transition-all outline-none"
                    data-testid="adaptive-answer-input"
                  />
                  <p className="text-xs text-slate-500 mt-2">
                    💡 Tip: SkillProof evaluates technical clarity, correct application of concepts, and problem-solving structure.
                  </p>
                </div>

                {/* Action Controls */}
                <div className="flex items-center justify-end mt-8 pt-6 border-t border-slate-800">
                  <button
                    type="button"
                    onClick={handleSubmitAdaptiveAnswer}
                    disabled={isSubmittingAdaptive}
                    className="px-6 py-3 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white font-medium text-sm transition-all shadow-md shadow-indigo-600/20 flex items-center gap-2"
                    data-testid="submit-adaptive-answer-button"
                  >
                    {isSubmittingAdaptive ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                        <span>Evaluating & Adapting...</span>
                      </>
                    ) : (
                      <>
                        <span>Submit Answer & Continue</span>
                        <span>→</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* STAGE 2: DIAGNOSTIC QUESTIONS */}
        {selectedRole && !isSettingUpAssessment && !isReviewing && !evaluationResult && !adaptiveSession && (
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
                {assessmentSelection && (
                  <button
                    onClick={handleBackToSetup}
                    className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors flex items-center gap-1.5"
                    data-testid="back-to-setup-button"
                  >
                    <span>⚙️</span>
                    <span>Setup</span>
                  </button>
                )}
                <div>
                  <h2 className="text-lg font-bold text-white flex items-center gap-2">
                    <span>{selectedRole.name} Diagnostic</span>
                    {assessmentSelection && (
                      <span className="text-xs px-2.5 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-normal">
                        {assessmentSelection.mode === 'recommended' ? '⭐ Recommended' : '🛠️ Custom'} • {assessmentSelection.primaryLanguageId ? assessmentSelection.primaryLanguageId.toUpperCase() : `${assessmentSelection.selectedSkillIds.length} skills`}
                      </span>
                    )}
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
              <div className="flex flex-col">
                {/* Unsupported catalog skills notice */}
                {assessmentCoverage && assessmentCoverage.unsupportedSkillIds && assessmentCoverage.unsupportedSkillIds.length > 0 && (
                  <div
                    className="mb-6 p-4 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 text-xs flex items-start gap-3"
                    data-testid="unsupported-skills-notice"
                  >
                    <span className="text-base">ℹ️</span>
                    <div>
                      <p className="font-semibold text-slate-200">Catalog-Only Skills Selected</p>
                      <p className="text-slate-400 mt-0.5">
                        The following selections are catalog skills — assessment questions not available in this prototype: <span className="font-mono text-indigo-300">{assessmentCoverage.unsupportedSkillIds.join(', ')}</span>.
                      </p>
                      <p className="text-slate-500 mt-0.5 italic">Not included in the current diagnostic.</p>
                    </div>
                  </div>
                )}

                <div className="p-6 sm:p-8 rounded-2xl bg-slate-900/80 border border-slate-800 shadow-2xl flex flex-col">
                  {/* Meta Badges */}
                  <div className="flex flex-wrap items-center gap-2 mb-5">
                    <span className="px-3 py-1 rounded-md text-xs font-semibold bg-indigo-500/10 text-indigo-300 border border-indigo-500/20">
                      {currentQuestion.competency}
                    </span>
                    <span className="px-2.5 py-1 rounded-md text-xs font-medium uppercase tracking-wider bg-slate-800 text-slate-300 border border-slate-700/60">
                      {(currentQuestion.type || currentQuestion.questionType || '').replace('_', ' ')}
                    </span>
                    {currentQuestion.difficulty && (
                      <span className="px-2.5 py-1 rounded-md text-xs font-semibold uppercase tracking-wider bg-amber-500/10 text-amber-300 border border-amber-500/30">
                        {currentQuestion.difficulty}
                      </span>
                    )}
                    {currentQuestion.sourceReference && (
                      <span className="px-2.5 py-1 rounded-md text-xs font-mono bg-slate-800/50 text-slate-400 border border-slate-800">
                        {currentQuestion.sourceReference}
                      </span>
                    )}
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

            {/* Milestone I7: Neutral Career Readiness Profile Summary */}
            {careerProfile && (
              <div className="mb-8 p-5 rounded-2xl bg-slate-900/80 border border-slate-800 shadow-lg" data-testid="career-readiness-summary">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div>
                    <h2 className="text-xs font-bold uppercase tracking-wider text-indigo-400 mb-1">
                      Career Readiness Evidence Summary
                    </h2>
                    <p className="text-xs text-slate-400">
                      Demonstrated qualitative evidence across assessed competencies. No arbitrary percentages or numeric scores.
                    </p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="px-3 py-1 rounded-lg bg-indigo-500/10 text-indigo-300 border border-indigo-500/20 text-xs font-semibold">
                      {careerProfile.summary.intermediateCount} Intermediate
                    </span>
                    <span className="px-3 py-1 rounded-lg bg-sky-500/10 text-sky-300 border border-sky-500/20 text-xs font-semibold">
                      {careerProfile.summary.beginnerCount} Beginner
                    </span>
                    <span className="px-3 py-1 rounded-lg bg-emerald-500/10 text-emerald-300 border border-emerald-500/20 text-xs font-semibold">
                      {careerProfile.summary.advancedCount} Advanced
                    </span>
                    <span className="px-3 py-1 rounded-lg bg-slate-800 text-slate-300 border border-slate-700 text-xs font-semibold">
                      {careerProfile.summary.insufficientEvidenceCount} Insufficient Evidence
                    </span>
                  </div>
                </div>
              </div>
            )}

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
                {careerProfile ? (
                  careerProfile.skills.map(skill => (
                    <div
                      key={skill.skillId}
                      className="p-5 rounded-xl bg-slate-900/70 border border-slate-800/90 hover:border-slate-700 transition-all flex flex-col justify-between shadow-md"
                      data-testid={`skill-card-${skill.skillId}`}
                    >
                      <div>
                        <div className="flex items-center justify-between gap-2 mb-3">
                          <h3 className="font-bold text-white text-base">
                            {skill.skillName}
                          </h3>
                          <span className={`px-2.5 py-0.5 rounded-md text-xs font-bold border ${getLevelBadgeClass(skill.finalLevel)}`} data-testid={`skill-level-${skill.skillId}`}>
                            {skill.finalLevel}
                          </span>
                        </div>

                        {/* Insufficient Evidence Notice */}
                        {skill.finalLevel.toLowerCase() === 'insufficient evidence' && (
                          <div className="p-3 mb-3 rounded-lg bg-slate-800/60 border border-slate-700/60 text-xs text-slate-300" data-testid={`insufficient-evidence-notice-${skill.skillId}`}>
                            <p className="font-medium text-slate-200 mb-0.5">ℹ️ Insufficient Diagnostic Evidence</p>
                            <p className="text-slate-400 text-xs leading-relaxed">
                              We did not collect enough evidence to determine your current level. This indicates missing diagnostic data, not a lack of ability or failure.
                            </p>
                          </div>
                        )}

                        {/* Demonstrated Strengths / Evidence */}
                        {skill.demonstratedStrengths && skill.demonstratedStrengths.length > 0 && (
                          <div className="mb-3" data-testid={`demonstrated-strengths-${skill.skillId}`}>
                            <p className="text-[11px] font-semibold text-emerald-400 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
                              <span>✓</span>
                              <span>Demonstrated Evidence & Strengths:</span>
                            </p>
                            <ul className="space-y-1">
                              {skill.demonstratedStrengths.map((st, idx) => (
                                <li key={idx} className="text-xs text-slate-300 flex items-start gap-1.5">
                                  <span className="text-emerald-400 font-bold">✓</span>
                                  <span>{st}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {/* Evidence Gaps / Development Gaps */}
                        {skill.evidenceGaps && skill.evidenceGaps.length > 0 && (
                          <div className="mb-3" data-testid={`evidence-gaps-${skill.skillId}`}>
                            <p className="text-[11px] font-semibold text-amber-400 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
                              <span>•</span>
                              <span>{skill.finalLevel.toLowerCase() === 'insufficient evidence' ? 'Evidence Gap:' : 'Development Gaps:'}</span>
                            </p>
                            <ul className="space-y-1">
                              {skill.evidenceGaps.map((gap, idx) => (
                                <li key={idx} className="text-xs text-slate-300 flex items-start gap-1.5">
                                  <span className="text-amber-400 font-bold">•</span>
                                  <span>{gap}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {/* Next Development Areas */}
                        {skill.nextDevelopmentAreas && skill.nextDevelopmentAreas.length > 0 && (
                          <div className="mb-3" data-testid={`next-development-areas-${skill.skillId}`}>
                            <p className="text-[11px] font-semibold text-indigo-400 uppercase tracking-wider mb-1.5 flex items-center gap-1.5">
                              <span>→</span>
                              <span>Next Development Areas:</span>
                            </p>
                            <div className="flex flex-wrap gap-1.5">
                              {skill.nextDevelopmentAreas.map((area, idx) => (
                                <span key={idx} className="px-2 py-0.5 rounded-md bg-indigo-500/10 text-indigo-300 border border-indigo-500/20 text-[11px]">
                                  → {area}
                                </span>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>

                      {/* Explainability Accordion: Why this level? */}
                      <div className="mt-2 pt-3 border-t border-slate-800/80">
                        <button
                          type="button"
                          onClick={() => toggleSkillExplain(skill.skillId)}
                          className="text-xs text-slate-400 hover:text-slate-200 flex items-center gap-1.5 transition-colors font-medium cursor-pointer"
                          data-testid={`explainability-toggle-${skill.skillId}`}
                        >
                          <span>{expandedSkills[skill.skillId] ? '▾' : '▸'}</span>
                          <span>Why this level?</span>
                        </button>

                        {expandedSkills[skill.skillId] && (
                          <div className="mt-2.5 p-3 rounded-lg bg-slate-950/80 border border-slate-800 text-xs space-y-2" data-testid={`explainability-content-${skill.skillId}`}>
                            <div>
                              <p className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1">
                                Candidate Reasoning Observed:
                              </p>
                              {skill.reasoning.map((r, rIdx) => (
                                <p key={rIdx} className="text-slate-300 text-xs leading-relaxed mb-1">
                                  {r}
                                </p>
                              ))}
                            </div>
                            {skill.questionsAnswered.length > 0 && (
                              <div className="pt-1.5 border-t border-slate-800/60 text-[11px] text-slate-500">
                                Assessed via questions: {skill.questionsAnswered.join(', ')}
                              </div>
                            )}
                            <p className="text-[10px] text-slate-500 italic pt-1 border-t border-slate-800/40">
                              Evaluation based solely on your submitted responses. Evaluator scoring rubrics remain private.
                            </p>
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : (
                  evaluationResult.skills.map(skill => (
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
                  ))
                )}
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
        {roadmapResult && !projectResult && !adaptiveProjectResult && (
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
              {roadmapResult.items.map((item) => {
                const skillKey = (item.skillId || item.skill).toLowerCase().replace(/[^a-z0-9]/g, '-');
                return (
                  <div
                    key={item.skill}
                    data-testid={`roadmap-item-${skillKey}`}
                    className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/90 to-slate-950/80 border border-slate-800 hover:border-slate-700 transition-all shadow-xl shadow-black/20"
                  >
                    {/* Card Header */}
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 mb-4 border-b border-slate-800/80">
                      <div className="flex flex-wrap items-center gap-2.5">
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
                          {item.targetArea && item.targetArea !== item.skill && (
                            <span className="text-slate-400 font-normal text-sm ml-2">
                              — {item.targetArea}
                            </span>
                          )}
                        </h2>

                        {item.currentLevel && (
                          <span className={`px-2 py-0.5 rounded text-[11px] font-semibold border ${
                            item.currentLevel === 'Advanced' ? 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30' :
                            item.currentLevel === 'Intermediate' ? 'bg-cyan-500/10 text-cyan-400 border-cyan-500/30' :
                            item.currentLevel === 'Beginner' ? 'bg-amber-500/10 text-amber-400 border-amber-500/30' :
                            'bg-slate-700/50 text-slate-300 border-slate-600/40'
                          }`}>
                            Current: {item.currentLevel}
                          </span>
                        )}

                        {item.gapType && (
                          <span className={`px-2 py-0.5 rounded text-[11px] font-semibold border ${
                            item.gapType === 'Evidence Gap'
                              ? 'bg-amber-500/10 text-amber-400 border-amber-500/30'
                              : 'bg-violet-500/10 text-violet-400 border-violet-500/30'
                          }`}>
                            {item.gapType === 'Evidence Gap' ? 'Evidence-Building Activity' : 'Development Gap'}
                          </span>
                        )}
                      </div>

                      <span className="text-xs text-slate-400 font-medium">
                        {item.priority === 1 ? 'Immediate High-Impact Gap' : item.priority === 2 ? 'Core Competency Building' : 'Targeted Enhancement'}
                      </span>
                    </div>

                    {/* Why this is recommended */}
                    {item.whyThisMatters && (
                      <div className="mb-4 p-3.5 rounded-xl bg-indigo-950/30 border border-indigo-500/20" data-testid={`why-recommended-${skillKey}`}>
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                            Why this is recommended:
                          </span>
                        </div>
                        <p className="text-xs text-slate-300 leading-relaxed">
                          {item.whyThisMatters}
                        </p>
                      </div>
                    )}

                    {/* Grid Layout for Learning Goal and Practice Activity */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                      {/* Learning Goal Box */}
                      <div className="p-4 rounded-xl bg-slate-950/70 border border-indigo-500/20 flex flex-col gap-2" data-testid={`learning-objective-${skillKey}`}>
                        <div className="flex items-center gap-2">
                          <span className="text-base">🎯</span>
                          <h3 className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                            Target Learning Goal
                          </h3>
                        </div>
                        <p className="text-xs sm:text-sm text-slate-200 leading-relaxed">
                          {item.learningGoal}
                        </p>

                        {/* Focused Learning Topics */}
                        {item.learningActions && item.learningActions.length > 0 && (
                          <div className="mt-2 pt-2 border-t border-slate-800/80">
                            <span className="text-[11px] font-semibold text-indigo-300 block mb-1">
                              Focused Learning Topics:
                            </span>
                            <ul className="space-y-1">
                              {item.learningActions.map((action, aIdx) => (
                                <li key={aIdx} className="text-xs text-slate-400 flex items-start gap-1.5">
                                  <span className="text-indigo-400">•</span>
                                  <span>{action}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>

                      {/* Actionable Practice Activity Box */}
                      <div className="p-4 rounded-xl bg-slate-950/70 border border-emerald-500/20 flex flex-col gap-2" data-testid={`practice-task-${skillKey}`}>
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

                    {/* Milestone I8 Evidence Target & Completion Criteria */}
                    {(item.evidenceTarget || (item.completionCriteria && item.completionCriteria.length > 0)) && (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-1">
                        {/* Evidence Target */}
                        {item.evidenceTarget && (
                          <div className="p-4 rounded-xl bg-slate-950/70 border border-cyan-500/20 flex flex-col gap-2" data-testid={`evidence-target-${skillKey}`}>
                            <div className="flex items-center gap-2">
                              <span className="text-base">📋</span>
                              <h3 className="text-xs font-bold uppercase tracking-wider text-cyan-400">
                                Observable Evidence to Produce
                              </h3>
                            </div>
                            <p className="text-xs sm:text-sm text-slate-200 leading-relaxed">
                              {item.evidenceTarget}
                            </p>
                          </div>
                        )}

                        {/* Completion Criteria */}
                        {item.completionCriteria && item.completionCriteria.length > 0 && (
                          <div className="p-4 rounded-xl bg-slate-950/70 border border-emerald-500/20 flex flex-col gap-2" data-testid={`completion-criteria-${skillKey}`}>
                            <div className="flex items-center gap-2">
                              <span className="text-base">✅</span>
                              <h3 className="text-xs font-bold uppercase tracking-wider text-emerald-400">
                                Completion Criteria
                              </h3>
                            </div>
                            <ul className="space-y-1.5 mt-0.5">
                              {item.completionCriteria.map((crit, cIdx) => (
                                <li key={cIdx} className="text-xs text-slate-300 flex items-start gap-1.5">
                                  <span className="text-emerald-400 font-bold">✓</span>
                                  <span>{crit}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
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
        {projectResult && !adaptiveProjectResult && (
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

        {/* STAGE 7: MILESTONE I9 GAP-BASED PROJECT, EVALUATION & PORTFOLIO PROOF */}
        {adaptiveProjectResult && (
          <div data-testid="adaptive-project-view" className="flex flex-col">
            {/* Header Banner */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 mb-8 border-b border-slate-800">
              <div>
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 uppercase tracking-wide">
                    Milestone I9 — Gap-Based Project & Portfolio Proof
                  </span>
                  <span className="text-xs text-slate-500">•</span>
                  <span className="text-xs font-medium text-slate-300 capitalize">
                    {selectedRole?.name || adaptiveProjectResult.roleId}
                  </span>
                </div>
                <h1 className="text-2xl sm:text-3xl font-bold text-white mt-2 tracking-tight">
                  {adaptiveProjectResult.title}
                </h1>
                <p className="text-sm text-slate-400 mt-1">
                  {adaptiveProjectResult.scenario}
                </p>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={() => setAdaptiveProjectResult(null)}
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

            {/* Strategic Rationale & Targeted Skills */}
            <div className="mb-8 p-6 rounded-2xl bg-gradient-to-r from-indigo-950/40 via-slate-900 to-violet-950/30 border border-indigo-500/40 shadow-xl shadow-black/30">
              <div className="flex items-center gap-2 mb-2">
                <span className="w-2.5 h-2.5 rounded-full bg-indigo-400 animate-pulse"></span>
                <h2 className="text-xs font-bold uppercase tracking-wider text-indigo-300">
                  Targeted Competency Gaps & Practice Rationale
                </h2>
              </div>
              <p className="text-sm text-slate-200 leading-relaxed font-medium mb-4">
                {adaptiveProjectResult.objective}
              </p>

              <div data-testid="project-targeted-skills" className="grid grid-cols-1 md:grid-cols-2 gap-3 pt-4 border-t border-indigo-500/20">
                {adaptiveProjectResult.targetedSkills.map(skill => (
                  <div key={skill.skillId} className="p-3 rounded-xl bg-slate-950/70 border border-indigo-500/20 flex flex-col gap-1">
                    <div className="flex items-center justify-between">
                      <span className="text-xs font-bold text-white uppercase">{skill.skillId}</span>
                      <span className="text-xs px-2 py-0.5 rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                        {skill.currentLevel}
                      </span>
                    </div>
                    <span className="text-xs text-indigo-300 font-medium">Target: {skill.targetArea}</span>
                    <span className="text-xs text-slate-400">{skill.whyIncluded}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Core Project Requirements Mapped to Gaps */}
            <div className="mb-8">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold text-white">Project Requirements Traced to Diagnosed Gaps</h2>
                <span className="text-xs text-slate-400">
                  Each requirement exercises an observed skill gap
                </span>
              </div>

              <div data-testid="project-requirements-list" className="space-y-4">
                {adaptiveProjectResult.requirements.map((req, idx) => (
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

            {/* Deliverables & Evidence Requirements */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-10">
              <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
                <h3 className="text-sm font-bold text-white uppercase tracking-wider mb-3 flex items-center gap-2">
                  <span className="text-emerald-400">📦</span>
                  Expected Deliverables
                </h3>
                <ul className="space-y-2">
                  {adaptiveProjectResult.deliverables.map((deliv, idx) => (
                    <li key={idx} className="text-xs text-slate-300 flex items-start gap-2">
                      <span className="text-emerald-400 font-bold">✓</span>
                      <span>{deliv}</span>
                    </li>
                  ))}
                </ul>
              </div>

              <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
                <h3 className="text-sm font-bold text-white uppercase tracking-wider mb-3 flex items-center gap-2">
                  <span className="text-indigo-400">📋</span>
                  Evidence & Verification Criteria
                </h3>
                <ul className="space-y-2">
                  {adaptiveProjectResult.evaluationCriteria.map((crit, idx) => (
                    <li key={idx} className="text-xs text-slate-300 flex items-start gap-2">
                      <span className="text-indigo-400 font-bold">•</span>
                      <span>{crit}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>

            {/* Evidence Submission Form */}
            <div data-testid="project-evidence-submission-form" className="mb-10 p-7 rounded-2xl bg-gradient-to-br from-slate-900 to-slate-950 border border-slate-800 shadow-xl">
              <div className="mb-6">
                <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 uppercase tracking-wide">
                  Step 2 — Submit Work
                </span>
                <h2 className="text-xl font-bold text-white mt-2">
                  Submit Project Evidence for Qualitative Evaluation
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Submit your implementation notes, architectural trade-offs, and test verification details to generate verified portfolio and CV claims.
                </p>
              </div>

              <form onSubmit={handleSubmitProjectEvidence} className="space-y-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1">
                    Repository URL (Optional)
                  </label>
                  <input
                    data-testid="evidence-repo-url"
                    type="text"
                    value={projectEvidenceForm.repositoryUrl || ''}
                    onChange={e => setProjectEvidenceForm(prev => ({ ...prev, repositoryUrl: e.target.value }))}
                    placeholder="https://github.com/your-username/resilient-inventory-service"
                    className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1">
                    Project Summary
                  </label>
                  <textarea
                    data-testid="evidence-summary-input"
                    required
                    rows={2}
                    value={projectEvidenceForm.projectSummary}
                    onChange={e => setProjectEvidenceForm(prev => ({ ...prev, projectSummary: e.target.value }))}
                    placeholder="Briefly describe what you built, system scope, and overall architecture."
                    className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1">
                    Implementation Explanation
                  </label>
                  <textarea
                    data-testid="evidence-implementation-input"
                    required
                    rows={3}
                    value={projectEvidenceForm.implementationExplanation}
                    onChange={e => setProjectEvidenceForm(prev => ({ ...prev, implementationExplanation: e.target.value }))}
                    placeholder="Detail key modules, transaction handling, locking strategies, API endpoints, or caching logic."
                    className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1">
                    Architecture & Trade-off Decisions
                  </label>
                  <textarea
                    data-testid="evidence-architecture-input"
                    required
                    rows={2}
                    value={projectEvidenceForm.architectureDecisions}
                    onChange={e => setProjectEvidenceForm(prev => ({ ...prev, architectureDecisions: e.target.value }))}
                    placeholder="Explain why specific isolation levels, caching strategies, or rate limiting algorithms were selected."
                    className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1">
                    Testing & Verification Details
                  </label>
                  <textarea
                    data-testid="evidence-testing-input"
                    required
                    rows={2}
                    value={projectEvidenceForm.testingExplanation}
                    onChange={e => setProjectEvidenceForm(prev => ({ ...prev, testingExplanation: e.target.value }))}
                    placeholder="Detail automated unit and integration tests, mocks for third-party boundaries, and concurrency test suites."
                    className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500"
                  />
                </div>

                <div className="pt-2 flex justify-end">
                  <button
                    type="submit"
                    data-testid="submit-evidence-btn"
                    disabled={isSubmittingEvidence}
                    className="px-6 py-3 rounded-xl bg-gradient-to-r from-emerald-600 to-indigo-600 hover:from-emerald-500 hover:to-indigo-500 disabled:opacity-50 text-white font-medium text-xs transition-all shadow-lg flex items-center gap-2"
                  >
                    {isSubmittingEvidence ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                        <span>Evaluating Evidence...</span>
                      </>
                    ) : (
                      <>
                        <span>Submit Project Evidence</span>
                        <span>→</span>
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>

            {/* Project Evidence Evaluation View */}
            {projectEvaluationResult && (
              <div data-testid="project-evaluation-view" className="mb-10 space-y-6">
                {/* Evaluation Status Banner */}
                <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900 to-slate-950 border border-indigo-500/30 shadow-xl">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-800">
                    <div>
                      <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                        Evidence Evaluation Result
                      </span>
                      <h3 className="text-xl font-bold text-white mt-1">
                        Qualitative Competency Verification
                      </h3>
                    </div>

                    <div className="flex items-center gap-3">
                      <span className="text-xs text-slate-400">Overall Status:</span>
                      <span
                        data-testid="evaluation-overall-status"
                        className={`px-3 py-1 rounded-full text-xs font-bold border ${
                          projectEvaluationResult.overallStatus === 'Demonstrated'
                            ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
                            : projectEvaluationResult.overallStatus === 'Partially Demonstrated'
                            ? 'bg-amber-500/10 text-amber-300 border-amber-500/30'
                            : 'bg-slate-800 text-slate-300 border-slate-700'
                        }`}
                      >
                        {projectEvaluationResult.overallStatus}
                      </span>
                    </div>
                  </div>

                  {/* Skill Evidence Breakdown */}
                  <div data-testid="skill-evidence-list" className="mt-6 space-y-4">
                    <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400">
                      Evaluated Skill Evidence Breakdown:
                    </h4>
                    {projectEvaluationResult.skillEvidence.map(skillEv => (
                      <div key={skillEv.skillId} className="p-4 rounded-xl bg-slate-950/60 border border-slate-800">
                        <div className="flex items-center justify-between mb-2">
                          <span className="text-xs font-bold text-white uppercase">{skillEv.skillId}</span>
                          <span className={`px-2.5 py-0.5 rounded text-xs font-semibold border ${
                            skillEv.evidenceStatus === 'Demonstrated'
                              ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
                              : skillEv.evidenceStatus === 'Partially Demonstrated'
                              ? 'bg-amber-500/10 text-amber-300 border-amber-500/30'
                              : 'bg-slate-800 text-slate-400 border-slate-700'
                          }`}>
                            {skillEv.evidenceStatus}
                          </span>
                        </div>

                        {skillEv.evidence.length > 0 && (
                          <div className="mb-2">
                            <span className="text-xs font-bold text-emerald-400">Observed Evidence:</span>
                            <ul className="mt-1 space-y-1">
                              {skillEv.evidence.map((e, idx) => (
                                <li key={idx} className="text-xs text-slate-300 flex items-start gap-1.5">
                                  <span className="text-emerald-400 font-bold">✓</span>
                                  <span>{e}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {skillEv.missingEvidence.length > 0 && (
                          <div>
                            <span className="text-xs font-bold text-slate-400">Evidence Gaps:</span>
                            <ul className="mt-1 space-y-1">
                              {skillEv.missingEvidence.map((m, idx) => (
                                <li key={idx} className="text-xs text-slate-400 flex items-start gap-1.5">
                                  <span>•</span>
                                  <span>{m}</span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>

                  {/* Summary Lists: Demonstrated, Missing, Suggestions */}
                  <div className="mt-6 grid grid-cols-1 md:grid-cols-2 gap-4">
                    {projectEvaluationResult.demonstratedEvidence.length > 0 && (
                      <div className="p-4 rounded-xl bg-emerald-950/20 border border-emerald-500/20">
                        <h4 className="text-xs font-bold uppercase text-emerald-400 mb-2">
                          Verified Demonstrated Evidence
                        </h4>
                        <ul data-testid="demonstrated-evidence-list" className="space-y-1.5">
                          {projectEvaluationResult.demonstratedEvidence.map((d, idx) => (
                            <li key={idx} className="text-xs text-slate-300 flex items-start gap-1.5">
                              <span className="text-emerald-400">✓</span>
                              <span>{d}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {projectEvaluationResult.missingEvidence.length > 0 && (
                      <div className="p-4 rounded-xl bg-slate-950/40 border border-slate-800">
                        <h4 className="text-xs font-bold uppercase text-slate-400 mb-2">
                          Missing Evidence (Neutral)
                        </h4>
                        <ul data-testid="missing-evidence-list" className="space-y-1.5">
                          {projectEvaluationResult.missingEvidence.map((m, idx) => (
                            <li key={idx} className="text-xs text-slate-400 flex items-start gap-1.5">
                              <span>•</span>
                              <span>{m}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>

                  {projectEvaluationResult.improvementSuggestions.length > 0 && (
                    <div className="mt-4 p-4 rounded-xl bg-slate-950/40 border border-slate-800">
                      <h4 className="text-xs font-bold uppercase text-indigo-400 mb-2">
                        Constructive Growth Suggestions
                      </h4>
                      <ul data-testid="improvement-suggestions-list" className="space-y-1.5">
                        {projectEvaluationResult.improvementSuggestions.map((s, idx) => (
                          <li key={idx} className="text-xs text-slate-300 flex items-start gap-1.5">
                            <span className="text-indigo-400">→</span>
                            <span>{s}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>

                {/* Portfolio Proof View */}
                {projectEvaluationResult.portfolioProof && (
                  <div data-testid="portfolio-proof-view" className="p-7 rounded-2xl bg-gradient-to-br from-emerald-950/30 via-slate-900 to-indigo-950/30 border border-emerald-500/30 shadow-xl">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse"></span>
                      <span className="text-xs font-bold uppercase tracking-wider text-emerald-400">
                        Verified Portfolio & CV Evidence Proof
                      </span>
                    </div>

                    <h3 className="text-xl font-bold text-white mb-2">
                      {projectEvaluationResult.portfolioProof.projectTitle}
                    </h3>

                    <p className="text-xs sm:text-sm text-slate-300 mb-6">
                      {projectEvaluationResult.portfolioProof.summary}
                    </p>

                    {/* Demonstrated Skills Badges */}
                    {projectEvaluationResult.portfolioProof.demonstratedSkills.length > 0 && (
                      <div className="mb-6">
                        <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block mb-2">
                          Verified Demonstrated Skills:
                        </span>
                        <div data-testid="demonstrated-skills-badges" className="flex flex-wrap gap-2">
                          {projectEvaluationResult.portfolioProof.demonstratedSkills.map(skill => (
                            <span key={skill} className="px-3 py-1 rounded-lg bg-emerald-500/10 text-emerald-300 border border-emerald-500/30 text-xs font-bold">
                              ✓ {skill}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Portfolio and CV Bullets */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
                      <div className="p-4 rounded-xl bg-slate-950/70 border border-slate-800">
                        <h4 className="text-xs font-bold uppercase text-indigo-300 mb-3 flex items-center gap-2">
                          <span>📁</span> Portfolio Bullets
                        </h4>
                        <ul data-testid="portfolio-bullets-list" className="space-y-2">
                          {projectEvaluationResult.portfolioProof.portfolioBullets.length > 0 ? (
                            projectEvaluationResult.portfolioProof.portfolioBullets.map((b, idx) => (
                              <li key={idx} className="text-xs text-slate-300 flex items-start gap-2">
                                <span className="text-indigo-400 font-bold">•</span>
                                <span>{b}</span>
                              </li>
                            ))
                          ) : (
                            <li className="text-xs text-slate-500 italic">No verified portfolio claims available (insufficient submitted evidence).</li>
                          )}
                        </ul>
                      </div>

                      <div className="p-4 rounded-xl bg-slate-950/70 border border-slate-800">
                        <h4 className="text-xs font-bold uppercase text-emerald-300 mb-3 flex items-center gap-2">
                          <span>📄</span> CV / Resume Bullet Points
                        </h4>
                        <ul data-testid="cv-bullets-list" className="space-y-2">
                          {projectEvaluationResult.portfolioProof.cvBullets.length > 0 ? (
                            projectEvaluationResult.portfolioProof.cvBullets.map((b, idx) => (
                              <li key={idx} className="text-xs text-slate-200 flex items-start gap-2">
                                <span className="text-emerald-400 font-bold">•</span>
                                <span>{b}</span>
                              </li>
                            ))
                          ) : (
                            <li className="text-xs text-slate-500 italic">No CV claims authorized (only Demonstrated evidence generates CV claims).</li>
                          )}
                        </ul>
                      </div>
                    </div>

                    {/* Evidence Notes */}
                    {projectEvaluationResult.portfolioProof.evidenceNotes.length > 0 && (
                      <div className="mb-6 p-4 rounded-xl bg-slate-950/60 border border-slate-800">
                        <h4 className="text-xs font-bold uppercase text-slate-400 mb-2">
                          Evidence Notes & Claim Audit Trail:
                        </h4>
                        <ul data-testid="evidence-notes-list" className="space-y-1">
                          {projectEvaluationResult.portfolioProof.evidenceNotes.map((note, idx) => (
                            <li key={idx} className="text-xs text-slate-400 flex items-start gap-1.5">
                              <span>ℹ</span>
                              <span>{note}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {/* Re-assess Action Card */}
                    <div className="pt-6 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4">
                      <div>
                        <h4 className="text-sm font-bold text-white">Complete the Learning Loop</h4>
                        <p className="text-xs text-slate-400 mt-0.5">
                          Practice your new skills or take a diagnostic re-assessment.
                        </p>
                      </div>

                      <button
                        data-testid="reassess-skills-btn"
                        onClick={handleReassessSkills}
                        className="w-full sm:w-auto px-6 py-3 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-xs transition-colors flex items-center justify-center gap-2"
                      >
                        <span>Re-assess Skills</span>
                        <span>↺</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
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
