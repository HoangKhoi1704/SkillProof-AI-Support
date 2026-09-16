'use client';

import React, { useState, useEffect, useMemo } from 'react';
import {
  AiRuntimeConfig,
  DevQuestionSummary,
  DevQuestionDetail,
  AiDiagnosticTrace,
  AiDiagnosticTraceSummary,
  QualitativeSkillLevel,
  DevAdaptiveInspection
} from '../../types';
import {
  fetchDevRuntimeConfig,
  fetchDevQuestions,
  fetchDevQuestionDetail,
  evaluateDevAnswer,
  fetchDevTraces,
  fetchDevTraceDetail,
  getDevAdaptiveSession
} from '../../api';

export default function DevAiInspectorPage() {
  // Runtime Config State
  const [runtimeConfig, setRuntimeConfig] = useState<AiRuntimeConfig | null>(null);
  const [loadingConfig, setLoadingConfig] = useState(true);
  const [configError, setConfigError] = useState<string | null>(null);

  // Question List & Filters
  const [questions, setQuestions] = useState<DevQuestionSummary[]>([]);
  const [loadingQuestions, setLoadingQuestions] = useState(true);
  const [selectedQuestionId, setSelectedQuestionId] = useState<string>('q-be-sql-02');
  const [selectedRoleFilter, setSelectedRoleFilter] = useState<string>('backend-developer');
  const [selectedSkillFilter, setSelectedSkillFilter] = useState<string>('all');
  const [selectedDifficultyFilter, setSelectedDifficultyFilter] = useState<string>('all');

  // Selected Question Details (with Rubric & Signals)
  const [questionDetail, setQuestionDetail] = useState<DevQuestionDetail | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // Candidate Answer & Evaluation State
  const [candidateAnswer, setCandidateAnswer] = useState<string>('');
  const [evaluatingMode, setEvaluatingMode] = useState<'deterministic' | 'live' | null>(null);
  const [evaluationError, setEvaluationError] = useState<string | null>(null);

  // Active Trace State
  const [activeTrace, setActiveTrace] = useState<AiDiagnosticTrace | null>(null);
  const [expandedSections, setExpandedSections] = useState<{
    prompt: boolean;
    rawResponse: boolean;
    rubricContext: boolean;
  }>({
    prompt: false,
    rawResponse: false,
    rubricContext: true
  });

  // Trace History State
  const [traceHistory, setTraceHistory] = useState<AiDiagnosticTraceSummary[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);

  // Adaptive Session Inspection State (Milestone I7)
  const [adaptiveSessionIdInput, setAdaptiveSessionIdInput] = useState<string>('');
  const [adaptiveInspection, setAdaptiveInspection] = useState<DevAdaptiveInspection | null>(null);
  const [loadingAdaptiveInspection, setLoadingAdaptiveInspection] = useState<boolean>(false);
  const [adaptiveInspectionError, setAdaptiveInspectionError] = useState<string | null>(null);

  async function handleInspectAdaptiveSession() {
    if (!adaptiveSessionIdInput.trim()) return;
    setLoadingAdaptiveInspection(true);
    setAdaptiveInspectionError(null);
    try {
      const data = await getDevAdaptiveSession(adaptiveSessionIdInput.trim());
      setAdaptiveInspection(data);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to inspect adaptive session';
      setAdaptiveInspectionError(msg);
    } finally {
      setLoadingAdaptiveInspection(false);
    }
  }

  // 1. Load Runtime Config & Trace History on Mount
  useEffect(() => {
    loadRuntimeConfig();
    loadTraceHistory();
  }, []);

  // 2. Load Questions when Filters change
  useEffect(() => {
    loadQuestions();
  }, [selectedRoleFilter, selectedSkillFilter, selectedDifficultyFilter]);

  // 3. Load Question Detail when selected question changes
  useEffect(() => {
    if (selectedQuestionId) {
      loadQuestionDetail(selectedQuestionId);
    }
  }, [selectedQuestionId]);

  async function loadRuntimeConfig() {
    setLoadingConfig(true);
    setConfigError(null);
    try {
      const config = await fetchDevRuntimeConfig();
      setRuntimeConfig(config);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load runtime configuration';
      setConfigError(msg);
    } finally {
      setLoadingConfig(false);
    }
  }

  async function loadQuestions() {
    setLoadingQuestions(true);
    try {
      const params: { roleId?: string; skillId?: string; difficulty?: string } = {};
      if (selectedRoleFilter && selectedRoleFilter !== 'all') params.roleId = selectedRoleFilter;
      if (selectedSkillFilter && selectedSkillFilter !== 'all') params.skillId = selectedSkillFilter;
      if (selectedDifficultyFilter && selectedDifficultyFilter !== 'all') params.difficulty = selectedDifficultyFilter;

      const data = await fetchDevQuestions(params);
      setQuestions(data);

      // If current selectedQuestionId not in filtered list, select first available
      if (data.length > 0 && !data.some(q => q.id === selectedQuestionId)) {
        setSelectedQuestionId(data[0].id);
      }
    } catch (err: unknown) {
      console.error('Failed to load questions', err);
    } finally {
      setLoadingQuestions(false);
    }
  }

  async function loadQuestionDetail(qId: string) {
    setLoadingDetail(true);
    try {
      const detail = await fetchDevQuestionDetail(qId);
      setQuestionDetail(detail);

      // Pre-fill a realistic sample answer if candidate answer is currently empty
      if (!candidateAnswer) {
        if (qId === 'q-be-sql-02') {
          setCandidateAnswer(
            'For a slow query, I first use EXPLAIN ANALYZE to inspect the execution plan, looking for sequential scans on large tables or high-cost joins. I verify whether appropriate indexes exist on filtered and joined columns, avoiding functions on indexed columns that invalidate index usage. I also check table statistics with ANALYZE to ensure the query planner has accurate cardinality estimates.'
          );
        } else if (qId === 'q-be-prog-01') {
          setCandidateAnswer(
            'A hash map provides average O(1) time complexity for lookups, insertions, and deletions by computing a hash of the key and indexing into buckets. Hash collisions occur when two distinct keys yield the same bucket index. They are commonly handled using separate chaining (linked list or tree per bucket) or open addressing (linear probing, quadratic probing, double hashing). In the worst case where all keys collide, lookup degrades to O(n).'
          );
        }
      }
    } catch (err: unknown) {
      console.error('Failed to load question detail', err);
    } finally {
      setLoadingDetail(false);
    }
  }

  async function loadTraceHistory() {
    setLoadingHistory(true);
    try {
      const history = await fetchDevTraces();
      setTraceHistory(history);
    } catch (err: unknown) {
      console.error('Failed to load trace history', err);
    } finally {
      setLoadingHistory(false);
    }
  }

  async function handleRunEvaluation(mode: 'deterministic' | 'live') {
    if (!selectedQuestionId || !candidateAnswer.trim()) {
      setEvaluationError('Please select a question and provide a candidate answer.');
      return;
    }

    setEvaluatingMode(mode);
    setEvaluationError(null);

    try {
      const trace = await evaluateDevAnswer({
        questionId: selectedQuestionId,
        candidateAnswer: candidateAnswer.trim(),
        mode
      });
      setActiveTrace(trace);
      // Refresh trace history list
      loadTraceHistory();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Evaluation failed';
      setEvaluationError(msg);
    } finally {
      setEvaluatingMode(null);
    }
  }

  async function handleSelectHistoryItem(traceId: string) {
    try {
      const trace = await fetchDevTraceDetail(traceId);
      setActiveTrace(trace);
      // Also sync question selector to this trace's question
      if (trace.question?.id) {
        setSelectedQuestionId(trace.question.id);
      }
      if (trace.candidateAnswer) {
        setCandidateAnswer(trace.candidateAnswer);
      }
    } catch (err: unknown) {
      console.error('Failed to load trace detail', err);
    }
  }

  const getLevelBadgeClass = (level: string) => {
    switch (level) {
      case 'Advanced':
        return 'bg-purple-500/20 text-purple-300 border-purple-500/40';
      case 'Intermediate':
        return 'bg-cyan-500/20 text-cyan-300 border-cyan-500/40';
      case 'Beginner':
        return 'bg-amber-500/20 text-amber-300 border-amber-500/40';
      case 'Insufficient Evidence':
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  const getDifficultyBadgeClass = (diff: string) => {
    switch (diff) {
      case 'foundation':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30';
      case 'applied':
        return 'bg-blue-500/10 text-blue-400 border-blue-500/30';
      case 'advanced-reasoning':
        return 'bg-purple-500/10 text-purple-400 border-purple-500/30';
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  // Distinct skills available in questions
  const availableSkills = useMemo(() => {
    const skills = new Set<string>();
    questions.forEach(q => skills.add(q.skillId));
    return Array.from(skills).sort();
  }, [questions]);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans selection:bg-indigo-500 selection:text-white pb-20">
      {/* Top Bar / Header */}
      <header className="border-b border-slate-800/80 bg-slate-900/80 backdrop-blur-md sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-cyan-500 to-indigo-600 flex items-center justify-center font-mono font-bold text-white shadow-lg shadow-indigo-500/20">
              AI
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-bold text-lg tracking-tight text-white">AI Diagnostic Inspector</span>
                <span className="px-2 py-0.5 text-xs font-mono font-semibold rounded-full bg-amber-500/15 text-amber-400 border border-amber-500/30">
                  DEVELOPER TOOL
                </span>
                <span className="px-2 py-0.5 text-xs font-mono rounded bg-slate-800 text-slate-400 border border-slate-700">
                  DEV ONLY
                </span>
              </div>
              <p className="text-xs text-slate-400">Diagnostic Pipeline Observability & Rubric Grounding</p>
            </div>
          </div>

          <div className="flex items-center gap-3 text-xs font-mono">
            <button
              onClick={loadRuntimeConfig}
              className="px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
              title="Refresh Runtime Config"
            >
              ↻ Refresh Config
            </button>
          </div>
        </div>
      </header>

      {/* Privacy Notice Banner */}
      <div className="bg-amber-950/40 border-b border-amber-900/50 px-4 sm:px-6 py-2.5">
        <div className="max-w-7xl mx-auto flex items-center justify-between text-xs text-amber-200/90 font-mono">
          <div className="flex items-center gap-2">
            <span className="text-amber-400 text-sm">⚠️</span>
            <span>
              <strong>Developer diagnostic tool.</strong> Avoid entering real personal or sensitive candidate information. Traces are stored in-memory only.
            </span>
          </div>
          <span className="text-amber-400/70 hidden sm:inline">No secrets exposed</span>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 pt-6 space-y-6">
        {/* ==================================================================== */}
        {/* 1. RUNTIME CONFIGURATION PANEL                                      */}
        {/* ==================================================================== */}
        <section className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <span className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono" data-testid="runtime-config-title">
                Runtime Configuration
              </span>
              <span className="text-xs text-slate-500 font-mono">(Safe Allow-listed Fields)</span>
            </div>
            {runtimeConfig?.environment && (
              <span className="px-2.5 py-0.5 text-xs font-mono rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20" data-testid="runtime-environment">
                ENV: {runtimeConfig.environment}
              </span>
            )}
          </div>

          {loadingConfig ? (
            <div className="text-xs text-slate-400 font-mono animate-pulse">Loading runtime configuration...</div>
          ) : configError ? (
            <div className="text-xs text-rose-400 font-mono bg-rose-950/20 border border-rose-900/40 p-3 rounded">
              Error: {configError} (Ensure backend is running in Development mode on port 5068)
            </div>
          ) : runtimeConfig ? (
            <div className="space-y-3" data-testid="dev-runtime-config-panel">
              <div className="grid grid-cols-2 sm:grid-cols-5 gap-3 font-mono text-xs">
                {/* Provider */}
                <div className="bg-slate-950/60 border border-slate-800/80 rounded-lg p-3">
                  <div className="text-slate-500 text-[10px] uppercase">Provider</div>
                  <div className="font-semibold text-slate-200 mt-1 text-sm" data-testid="runtime-provider">{runtimeConfig.provider}</div>
                </div>

                {/* Model */}
                <div className="bg-slate-950/60 border border-slate-800/80 rounded-lg p-3">
                  <div className="text-slate-500 text-[10px] uppercase">Model</div>
                  <div className="font-semibold text-cyan-400 mt-1 text-sm" data-testid="runtime-model">{runtimeConfig.model}</div>
                </div>

                {/* Live AI Status */}
                <div className="bg-slate-950/60 border border-slate-800/80 rounded-lg p-3">
                  <div className="text-slate-500 text-[10px] uppercase">Live AI Enabled</div>
                  <div className="mt-1 flex items-center gap-1.5" data-testid="runtime-live-status">
                    <span
                      className={`inline-block w-2 h-2 rounded-full ${
                        runtimeConfig.liveEvaluationEnabled ? 'bg-emerald-400' : 'bg-rose-500'
                      }`}
                    />
                    <span
                      className={`font-semibold ${
                        runtimeConfig.liveEvaluationEnabled ? 'text-emerald-400' : 'text-slate-400'
                      }`}
                    >
                      {runtimeConfig.liveEvaluationEnabled ? 'ENABLED' : 'OFF'}
                    </span>
                  </div>
                </div>

                {/* Active Evaluator */}
                <div className="bg-slate-950/60 border border-slate-800/80 rounded-lg p-3 sm:col-span-2">
                  <div className="text-slate-500 text-[10px] uppercase">Active Diagnostic Evaluator</div>
                  <div className="font-semibold text-indigo-300 mt-1 text-xs truncate" data-testid="runtime-evaluator" title={runtimeConfig.activeEvaluator}>
                    {runtimeConfig.activeEvaluator}
                  </div>
                </div>
              </div>

              {/* Status Message */}
              <div className="bg-slate-950/40 border border-slate-800/60 rounded-lg px-3 py-2 flex items-center gap-2 text-xs font-mono">
                <span className="text-slate-500">Status:</span>
                <span className={runtimeConfig.liveEvaluationEnabled ? 'text-emerald-300' : 'text-amber-300'}>
                  {runtimeConfig.statusMessage}
                </span>
              </div>
            </div>
          ) : null}
        </section>

        {/* ==================================================================== */}
        {/* 2-COLUMN WORKSPACE: LEFT (QUESTION & CONTEXT) | RIGHT (EVAL & TRACE) */}
        {/* ==================================================================== */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* ------------------------------------------------------------------ */}
          {/* LEFT COLUMN (5/12): QUESTION INSPECTOR & SERVER EVALUATION CONTEXT */}
          {/* ------------------------------------------------------------------ */}
          <div className="lg:col-span-5 space-y-6">
            {/* Question Selector Card */}
            <div className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono">
                  Question Inspector
                </h2>
                <span className="text-xs text-slate-500 font-mono">{questions.length} questions</span>
              </div>

              {/* Filters */}
              <div className="grid grid-cols-2 gap-2 text-xs font-mono">
                <div>
                  <label className="text-slate-400 block mb-1">Skill Filter</label>
                  <select
                    value={selectedSkillFilter}
                    onChange={e => setSelectedSkillFilter(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-1.5 text-slate-200 focus:outline-none focus:border-indigo-500"
                  >
                    <option value="all">All Skills ({questions.length})</option>
                    {availableSkills.map(s => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="text-slate-400 block mb-1">Difficulty</label>
                  <select
                    value={selectedDifficultyFilter}
                    onChange={e => setSelectedDifficultyFilter(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-1.5 text-slate-200 focus:outline-none focus:border-indigo-500"
                  >
                    <option value="all">All Difficulties</option>
                    <option value="foundation">Foundation</option>
                    <option value="applied">Applied</option>
                    <option value="advanced-reasoning">Advanced-Reasoning</option>
                  </select>
                </div>
              </div>

              {/* Question Dropdown */}
              <div>
                <label className="text-slate-400 block text-xs font-mono mb-1">Select Question</label>
                <select
                  value={selectedQuestionId}
                  onChange={e => setSelectedQuestionId(e.target.value)}
                  data-testid="question-selector"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs font-mono text-slate-200 focus:outline-none focus:border-indigo-500 truncate"
                >
                  {questions.map(q => (
                    <option key={q.id} value={q.id}>
                      [{q.id}] ({q.skillId}) {q.questionText.slice(0, 50)}...
                    </option>
                  ))}
                </select>
              </div>

              {/* Question Metadata Header */}
              {loadingDetail ? (
                <div className="text-xs text-slate-400 font-mono animate-pulse">Loading question metadata...</div>
              ) : questionDetail ? (
                <div className="space-y-3 pt-2 border-t border-slate-800/80" data-testid="question-detail-card">
                  <div className="flex flex-wrap items-center gap-1.5 text-xs font-mono">
                    <span className="font-bold text-slate-200 bg-slate-800 px-2 py-0.5 rounded" data-testid="question-id-badge">
                      {questionDetail.id}
                    </span>
                    <span className="bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 px-2 py-0.5 rounded">
                      {questionDetail.skillId}
                    </span>
                    <span className={`px-2 py-0.5 rounded border ${getDifficultyBadgeClass(questionDetail.difficulty)}`}>
                      {questionDetail.difficulty}
                    </span>
                    <span className="bg-slate-800 text-slate-400 border border-slate-700 px-2 py-0.5 rounded">
                      {questionDetail.questionType}
                    </span>
                    <span className="bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 px-2 py-0.5 rounded text-[11px]">
                      ✓ {questionDetail.verificationStatus}
                    </span>
                  </div>

                  {/* Question Text */}
                  <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-3">
                    <div className="text-[11px] font-mono text-slate-500 uppercase mb-1">Question Prompt</div>
                    <p className="text-sm text-slate-200 leading-relaxed font-sans">{questionDetail.questionText}</p>
                  </div>

                  {/* Subskills */}
                  {questionDetail.subskills?.length > 0 && (
                    <div>
                      <div className="text-[11px] font-mono text-slate-500 mb-1">Subskills Tested:</div>
                      <div className="flex flex-wrap gap-1">
                        {questionDetail.subskills.map(s => (
                          <span
                            key={s}
                            className="text-[11px] font-mono bg-slate-800/80 text-slate-300 px-2 py-0.5 rounded"
                          >
                            {s}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Provenance */}
                  {questionDetail.provenance && (
                    <div className="text-[11px] font-mono text-slate-500">
                      Provenance:{' '}
                      <span className="text-slate-400">{questionDetail.provenance}</span>
                    </div>
                  )}
                </div>
              ) : null}
            </div>

            {/* Server Evaluation Context (Rubric & Signals) */}
            {questionDetail && (
              <div className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-4" data-testid="server-evaluation-context">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono">
                      Server Evaluation Context
                    </h3>
                    <p className="text-xs text-amber-400/80 font-mono">
                      Internal Server Rubrics (Hidden from Public Assessment API)
                    </p>
                  </div>
                  <button
                    onClick={() =>
                      setExpandedSections(prev => ({
                        ...prev,
                        rubricContext: !prev.rubricContext
                      }))
                    }
                    className="text-xs font-mono text-indigo-400 hover:text-indigo-300"
                  >
                    {expandedSections.rubricContext ? 'Collapse ▲' : 'Expand ▼'}
                  </button>
                </div>

                {expandedSections.rubricContext && (
                  <div className="space-y-4 text-xs font-mono">
                    {/* Expected Signals */}
                    <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3" data-testid="expected-signals-list">
                      <div className="text-cyan-400 font-semibold mb-2 flex items-center gap-1.5">
                        <span>🎯</span>
                        <span>Expected Signals ({questionDetail.expectedSignals?.length || 0})</span>
                      </div>
                      <ul className="space-y-1.5 text-slate-300 pl-4 list-disc font-sans text-xs">
                        {questionDetail.expectedSignals?.map((sig, i) => (
                          <li key={i} className="leading-snug">
                            {sig}
                          </li>
                        ))}
                      </ul>
                    </div>

                    {/* Rubric Levels */}
                    {questionDetail.rubric && (
                      <div className="space-y-2" data-testid="server-rubric-container">
                        <div className="text-slate-400 font-semibold">Qualitative Rubric</div>

                        {/* Insufficient Evidence */}
                        <div className="bg-slate-950/60 border border-slate-800 rounded-lg p-2.5">
                          <div className="text-slate-400 font-bold mb-1">Insufficient Evidence:</div>
                          <p className="text-slate-400 font-sans text-xs leading-relaxed">
                            {questionDetail.rubric.insufficientEvidence}
                          </p>
                        </div>

                        {/* Beginner */}
                        <div className="bg-slate-950/60 border border-amber-900/30 rounded-lg p-2.5">
                          <div className="text-amber-400 font-bold mb-1">Beginner:</div>
                          <p className="text-slate-300 font-sans text-xs leading-relaxed">
                            {questionDetail.rubric.beginner}
                          </p>
                        </div>

                        {/* Intermediate */}
                        <div className="bg-slate-950/60 border border-cyan-900/30 rounded-lg p-2.5">
                          <div className="text-cyan-400 font-bold mb-1">Intermediate:</div>
                          <p className="text-slate-300 font-sans text-xs leading-relaxed">
                            {questionDetail.rubric.intermediate}
                          </p>
                        </div>

                        {/* Advanced */}
                        <div className="bg-slate-950/60 border border-purple-900/30 rounded-lg p-2.5">
                          <div className="text-purple-400 font-bold mb-1">Advanced:</div>
                          <p className="text-slate-300 font-sans text-xs leading-relaxed">
                            {questionDetail.rubric.advanced}
                          </p>
                        </div>
                      </div>
                    )}

                    {/* Sources & Evidence */}
                    {questionDetail.frameworkSources?.length > 0 && (
                      <div className="pt-2 border-t border-slate-800/80">
                        <div className="text-slate-400 mb-1">Framework Sources ({questionDetail.frameworkSources.length})</div>
                        <div className="flex flex-wrap gap-1">
                          {questionDetail.frameworkSources.map(s => (
                            <span
                              key={s.sourceId}
                              className="text-[10px] bg-slate-800 text-slate-300 px-2 py-0.5 rounded border border-slate-700"
                              title={`${s.title} (${s.publisher})`}
                            >
                              {s.sourceId} ({s.publisher})
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* ------------------------------------------------------------------ */}
          {/* RIGHT COLUMN (7/12): CANDIDATE ANSWER INPUT, ACTIONS & TRACE VIEW  */}
          {/* ------------------------------------------------------------------ */}
          <div className="lg:col-span-7 space-y-6">
            {/* Candidate Answer Box */}
            <div className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono">
                  Candidate Answer (Simulated Input)
                </h2>
                <div className="flex items-center gap-1.5">
                  <button
                    onClick={() => {
                      if (selectedQuestionId === 'q-be-sql-02') {
                        setCandidateAnswer(
                          'For a slow query, I first use EXPLAIN ANALYZE to inspect the execution plan, looking for sequential scans on large tables or high-cost joins. I verify whether appropriate indexes exist on filtered and joined columns, avoiding functions on indexed columns that invalidate index usage. I also check table statistics with ANALYZE to ensure the query planner has accurate cardinality estimates.'
                        );
                      } else {
                        setCandidateAnswer(
                          'I would analyze the problem by identifying the root cause, applying the standard architectural approach, and evaluating tradeoffs between latency, throughput, and consistency.'
                        );
                      }
                    }}
                    data-testid="strong-sample-btn"
                    className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
                  >
                    Strong Sample
                  </button>
                  <button
                    onClick={() => {
                      setCandidateAnswer('I would look at the logs and restart the database service or add more memory.');
                    }}
                    data-testid="basic-sample-btn"
                    className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
                  >
                    Basic Sample
                  </button>
                  <button
                    onClick={() => setCandidateAnswer('')}
                    data-testid="clear-answer-btn"
                    className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-400 border border-slate-700 transition"
                  >
                    Clear
                  </button>
                </div>
              </div>

              <textarea
                value={candidateAnswer}
                onChange={e => setCandidateAnswer(e.target.value)}
                placeholder="Enter simulated candidate answer to test against real diagnostic pipeline..."
                rows={5}
                data-testid="candidate-answer-input"
                className="w-full bg-slate-950 border border-slate-800 rounded-lg p-3 text-sm text-slate-200 placeholder-slate-600 focus:outline-none focus:border-indigo-500 font-sans"
              />

              <div className="flex items-center justify-between text-xs font-mono text-slate-500">
                <span>{candidateAnswer.length} characters</span>
                <span>Uses shared diagnostic evaluator & prompt builder</span>
              </div>

              {/* Evaluation Action Buttons */}
              <div className="flex flex-wrap items-center gap-3 pt-2">
                {/* Button A: Deterministic Preview */}
                <button
                  onClick={() => handleRunEvaluation('deterministic')}
                  disabled={evaluatingMode !== null || !candidateAnswer.trim()}
                  data-testid="deterministic-preview-btn"
                  className="px-4 py-2.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed text-white font-mono text-xs font-semibold shadow-lg shadow-indigo-600/20 transition flex items-center gap-2"
                >
                  {evaluatingMode === 'deterministic' ? (
                    <>
                      <span className="inline-block w-3 h-3 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                      <span>Evaluating Deterministic...</span>
                    </>
                  ) : (
                    <>
                      <span>▶ Run Deterministic Preview</span>
                      <span className="text-[10px] opacity-80 font-normal">(0 OpenAI calls)</span>
                    </>
                  )}
                </button>

                {/* Button B: Live AI Evaluation */}
                <div className="relative group">
                  <button
                    onClick={() => handleRunEvaluation('live')}
                    disabled={
                      evaluatingMode !== null ||
                      !candidateAnswer.trim() ||
                      !runtimeConfig?.liveAiAvailable
                    }
                    data-testid="live-evaluation-btn"
                    className="px-4 py-2.5 rounded-lg bg-purple-600 hover:bg-purple-500 disabled:opacity-40 disabled:cursor-not-allowed text-white font-mono text-xs font-semibold shadow-lg shadow-purple-600/20 transition flex items-center gap-2"
                  >
                    {evaluatingMode === 'live' ? (
                      <>
                        <span className="inline-block w-3 h-3 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        <span>Evaluating Live AI...</span>
                      </>
                    ) : (
                      <>
                        <span>⚡ Run Live AI Evaluation</span>
                        <span className="text-[10px] opacity-80 font-normal">
                          {runtimeConfig?.liveAiAvailable ? `(${runtimeConfig.model})` : '(Disabled)'}
                        </span>
                      </>
                    )}
                  </button>

                  {!runtimeConfig?.liveAiAvailable && (
                    <div className="absolute bottom-full mb-2 left-0 hidden group-hover:block w-72 bg-slate-900 border border-slate-700 text-amber-300 text-[11px] font-mono rounded p-2 shadow-xl z-20">
                      Live AI evaluation unavailable because LiveEvaluationEnabled is false or OpenAI credentials are not configured.
                    </div>
                  )}
                </div>
              </div>

              {evaluationError && (
                <div className="text-xs text-rose-400 font-mono bg-rose-950/30 border border-rose-900/50 p-3 rounded-lg">
                  Evaluation Error: {evaluationError}
                </div>
              )}
            </div>

            {/* Active Trace View */}
            {activeTrace && (
              <div className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-5" data-testid="active-trace-card">
                {/* Trace Banner */}
                <div className="flex flex-wrap items-center justify-between gap-2 pb-3 border-b border-slate-800">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-mono text-slate-500">Trace:</span>
                      <span className="text-xs font-mono font-bold text-slate-300" data-testid="active-trace-id">{activeTrace.traceId}</span>
                      <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                        {(activeTrace.mode || 'deterministic').toUpperCase()}
                      </span>
                    </div>
                    <div className="text-[11px] font-mono text-slate-500 mt-0.5">
                      Evaluator: <span className="text-slate-400">{activeTrace.runtime?.activeEvaluator || activeTrace.evaluator}</span> • Duration:{' '}
                      <span className="text-slate-300 font-bold">{activeTrace.timing?.durationMs ?? activeTrace.durationMs ?? 0}ms</span>
                    </div>
                  </div>

                  {/* Final Level Big Badge */}
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-mono text-slate-400 uppercase">Assigned Level:</span>
                    <span
                      data-testid="assigned-level-badge"
                      className={`text-sm font-bold font-mono px-3 py-1 rounded-lg border ${getLevelBadgeClass(
                        activeTrace.finalResult?.level
                      )}`}
                    >
                      {activeTrace.finalResult?.level}
                    </span>
                  </div>
                </div>

                {/* ================================================================ */}
                {/* EVIDENCE GROUNDING DEBUG VIEW                                    */}
                {/* ================================================================ */}
                <div className="space-y-3">
                  <h3 className="text-xs font-semibold text-slate-300 uppercase tracking-wider font-mono flex items-center gap-1.5">
                    <span>🔍</span>
                    <span>Evidence Grounding Debug View</span>
                  </h3>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    {/* Column 1: AI Evidence Extracted */}
                    <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3">
                      <div className="text-cyan-400 font-mono font-semibold mb-1.5 flex items-center gap-1">
                        <span>📋</span>
                        <span>AI Evidence ({activeTrace.finalResult?.evidence?.length || 0})</span>
                      </div>
                      {activeTrace.finalResult?.evidence?.length > 0 ? (
                        <ul className="space-y-1 pl-4 list-disc text-slate-300 font-sans text-xs">
                          {activeTrace.finalResult.evidence.map((ev, i) => (
                            <li key={i}>{ev}</li>
                          ))}
                        </ul>
                      ) : (
                        <p className="text-slate-500 italic font-mono text-[11px]">No specific evidence items returned.</p>
                      )}
                    </div>

                    {/* Column 2: AI Reasoning */}
                    <div className="bg-slate-950/70 border border-slate-800 rounded-lg p-3">
                      <div className="text-indigo-400 font-mono font-semibold mb-1.5 flex items-center gap-1">
                        <span>🧠</span>
                        <span>AI Reasoning</span>
                      </div>
                      <p className="text-slate-300 font-sans text-xs leading-relaxed">
                        {activeTrace.finalResult?.reason || 'No reasoning provided.'}
                      </p>
                    </div>
                  </div>
                </div>

                {/* ================================================================ */}
                {/* VALIDATION TRACE                                                 */}
                {/* ================================================================ */}
                <div className="bg-slate-950/80 border border-slate-800 rounded-lg p-3.5 space-y-2 font-mono text-xs">
                  <div className="text-slate-400 font-semibold uppercase tracking-wider text-[11px]">
                    Backend Validation Stages
                  </div>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                    <div className="flex items-center gap-1.5">
                      <span className={activeTrace.validation?.schemaValid ? 'text-emerald-400' : 'text-rose-400'}>
                        {activeTrace.validation?.schemaValid ? '✓' : '✗'}
                      </span>
                      <span className="text-slate-300">Schema Valid</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <span className={activeTrace.validation?.levelValid ? 'text-emerald-400' : 'text-rose-400'}>
                        {activeTrace.validation?.levelValid ? '✓' : '✗'}
                      </span>
                      <span className="text-slate-300">Level Valid</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <span className={activeTrace.validation?.evidenceValid ? 'text-emerald-400' : 'text-rose-400'}>
                        {activeTrace.validation?.evidenceValid ? '✓' : '✗'}
                      </span>
                      <span className="text-slate-300">Evidence Present</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <span className={activeTrace.validation?.questionIdMatched ? 'text-emerald-400' : 'text-rose-400'}>
                        {activeTrace.validation?.questionIdMatched ? '✓' : '✗'}
                      </span>
                      <span className="text-slate-300">Question ID Match</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <span className={activeTrace.validation?.rubricResolved ? 'text-emerald-400' : 'text-rose-400'}>
                        {activeTrace.validation?.rubricResolved ? '✓' : '✗'}
                      </span>
                      <span className="text-slate-300">Rubric Resolved</span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <span className={!activeTrace.validation?.fallbackUsed ? 'text-emerald-400' : 'text-amber-400'}>
                        {!activeTrace.validation?.fallbackUsed ? '✓' : '⚠'}
                      </span>
                      <span className="text-slate-300">
                        {activeTrace.validation?.fallbackUsed ? 'Fallback Evaluator' : 'Primary Engine'}
                      </span>
                    </div>
                  </div>

                  {activeTrace.validation?.validationMessages?.length > 0 && (
                    <div className="pt-2 border-t border-slate-800 text-[11px] text-amber-300">
                      Messages: {activeTrace.validation.validationMessages.join('; ')}
                    </div>
                  )}
                </div>

                {/* ================================================================ */}
                {/* PROMPT VISIBILITY (SHARED PROMPT BUILDER)                        */}
                {/* ================================================================ */}
                <div className="border border-slate-800 rounded-lg overflow-hidden">
                  <button
                    onClick={() => setExpandedSections(prev => ({ ...prev, prompt: !prev.prompt }))}
                    className="w-full bg-slate-950/90 px-3.5 py-2.5 text-left text-xs font-mono text-slate-300 flex items-center justify-between hover:bg-slate-900 transition"
                  >
                    <span className="flex items-center gap-1.5">
                      <span>📄</span>
                      <span>Shared Evaluation Prompt & Instructions</span>
                    </span>
                    <span className="text-indigo-400">{expandedSections.prompt ? 'Hide ▲' : 'Show ▼'}</span>
                  </button>

                  {expandedSections.prompt && (
                    <div className="p-3.5 bg-slate-950/60 border-t border-slate-800 space-y-3 font-mono text-xs">
                      <div>
                        <div className="text-[11px] text-slate-500 uppercase mb-1">System Instructions</div>
                        <pre className="p-2.5 rounded bg-slate-950 border border-slate-800 text-slate-300 whitespace-pre-wrap text-[11px] max-h-48 overflow-y-auto">
                          {activeTrace.prompt?.systemInstructions}
                        </pre>
                      </div>

                      <div>
                        <div className="text-[11px] text-slate-500 uppercase mb-1">Evaluation Instructions (User Prompt)</div>
                        <pre className="p-2.5 rounded bg-slate-950 border border-slate-800 text-slate-300 whitespace-pre-wrap text-[11px] max-h-64 overflow-y-auto">
                          {activeTrace.prompt?.evaluationInstructions}
                        </pre>
                      </div>
                    </div>
                  )}
                </div>

                {/* ================================================================ */}
                {/* MODEL RESPONSE DETAILS                                           */}
                {/* ================================================================ */}
                <div className="border border-slate-800 rounded-lg overflow-hidden">
                  <button
                    onClick={() => setExpandedSections(prev => ({ ...prev, rawResponse: !prev.rawResponse }))}
                    className="w-full bg-slate-950/90 px-3.5 py-2.5 text-left text-xs font-mono text-slate-300 flex items-center justify-between hover:bg-slate-900 transition"
                  >
                    <span className="flex items-center gap-1.5">
                      <span>🤖</span>
                      <span>Evaluator Response Payload</span>
                    </span>
                    <span className="text-indigo-400">{expandedSections.rawResponse ? 'Hide ▲' : 'Show ▼'}</span>
                  </button>

                  {expandedSections.rawResponse && (
                    <div className="p-3.5 bg-slate-950/60 border-t border-slate-800 space-y-3 font-mono text-xs">
                      {activeTrace.modelResponse?.rawResponse && (
                        <div>
                          <div className="text-[11px] text-slate-500 uppercase mb-1">Raw Model Response</div>
                          <pre className="p-2.5 rounded bg-slate-950 border border-slate-800 text-slate-300 whitespace-pre-wrap text-[11px] max-h-48 overflow-y-auto">
                            {activeTrace.modelResponse.rawResponse}
                          </pre>
                        </div>
                      )}

                      <div>
                        <div className="text-[11px] text-slate-500 uppercase mb-1">Parsed Response Object</div>
                        <pre className="p-2.5 rounded bg-slate-950 border border-slate-800 text-slate-300 whitespace-pre-wrap text-[11px] max-h-48 overflow-y-auto">
                          {JSON.stringify(activeTrace.modelResponse?.parsedResponse, null, 2)}
                        </pre>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* ==================================================================== */}
        {/* 3. RECENT DEVELOPMENT TRACES (IN-MEMORY HISTORY, MAX 20)             */}
        {/* ==================================================================== */}
        <section className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono">
                Recent Development Traces ({traceHistory.length} / 20)
              </h2>
              <p className="text-xs text-slate-500 font-mono">
                In-memory development history. Cleared on backend restart. No persistence to SQLite.
              </p>
            </div>
            <button
              onClick={loadTraceHistory}
              disabled={loadingHistory}
              className="px-2.5 py-1 text-xs font-mono rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
            >
              {loadingHistory ? 'Refreshing...' : '↻ Refresh History'}
            </button>
          </div>

          {traceHistory.length === 0 ? (
            <div className="text-xs text-slate-500 font-mono py-4 text-center bg-slate-950/40 rounded-lg border border-slate-800/60">
              No development traces recorded in this session. Run an evaluation above to record a trace.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-xs font-mono text-left" data-testid="trace-history-table">
                <thead className="bg-slate-950/80 text-slate-400 border-b border-slate-800 uppercase text-[10px]">
                  <tr>
                    <th className="py-2.5 px-3">Trace ID</th>
                    <th className="py-2.5 px-3">Timestamp</th>
                    <th className="py-2.5 px-3">Question</th>
                    <th className="py-2.5 px-3">Skill</th>
                    <th className="py-2.5 px-3">Evaluator</th>
                    <th className="py-2.5 px-3">Level</th>
                    <th className="py-2.5 px-3 text-right">Duration</th>
                    <th className="py-2.5 px-3 text-center">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/60">
                  {traceHistory.map(tr => (
                    <tr
                      key={tr.traceId}
                      className={`hover:bg-slate-800/40 transition cursor-pointer ${
                        activeTrace?.traceId === tr.traceId ? 'bg-indigo-950/20' : ''
                      }`}
                      onClick={() => handleSelectHistoryItem(tr.traceId)}
                    >
                      <td className="py-2 px-3 font-semibold text-slate-300">{tr.traceId}</td>
                      <td className="py-2 px-3 text-slate-500">{new Date(tr.timestamp).toLocaleTimeString()}</td>
                      <td className="py-2 px-3 text-cyan-400">{tr.questionId}</td>
                      <td className="py-2 px-3 text-slate-400">{tr.skillId}</td>
                      <td className="py-2 px-3 text-slate-400 text-[11px] truncate max-w-[140px]" title={tr.evaluator}>
                        {tr.evaluator}
                      </td>
                      <td className="py-2 px-3">
                        <span className={`px-2 py-0.5 rounded text-[11px] border ${getLevelBadgeClass(tr.level)}`}>
                          {tr.level}
                        </span>
                      </td>
                      <td className="py-2 px-3 text-right text-slate-400">{tr.durationMs}ms</td>
                      <td className="py-2 px-3 text-center">
                        <button
                          onClick={e => {
                            e.stopPropagation();
                            handleSelectHistoryItem(tr.traceId);
                          }}
                          className="px-2 py-0.5 rounded bg-indigo-500/10 text-indigo-400 hover:bg-indigo-500/20 border border-indigo-500/20 text-[11px]"
                        >
                          View Trace
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {/* ==================================================================== */}
        {/* 4. ADAPTIVE SESSION INSPECTOR (MILESTONE I7)                        */}
        {/* ==================================================================== */}
        <section className="bg-slate-900/70 border border-slate-800 rounded-xl p-5 space-y-4" data-testid="adaptive-session-inspector-section">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-sm font-semibold text-slate-300 uppercase tracking-wider font-mono">
                Adaptive Session Inspector
              </h2>
              <p className="text-xs text-slate-500 font-mono">
                Inspect end-to-end adaptive evaluation traces, delineating AI evaluator outputs from deterministic backend derivations.
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <input
              type="text"
              value={adaptiveSessionIdInput}
              onChange={e => setAdaptiveSessionIdInput(e.target.value)}
              placeholder="Enter adaptive session ID (e.g. session-adp-...)"
              className="flex-1 px-3 py-2 rounded-lg bg-slate-950 border border-slate-700 text-xs font-mono text-slate-200 placeholder-slate-500 outline-none focus:border-indigo-500"
              data-testid="adaptive-session-id-input"
            />
            <button
              onClick={handleInspectAdaptiveSession}
              disabled={loadingAdaptiveInspection || !adaptiveSessionIdInput.trim()}
              className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-mono font-medium transition cursor-pointer"
              data-testid="inspect-adaptive-session-btn"
            >
              {loadingAdaptiveInspection ? 'Inspecting...' : 'Inspect Session'}
            </button>
          </div>

          {adaptiveInspectionError && (
            <div className="p-3 rounded-lg bg-rose-950/30 border border-rose-900/40 text-xs text-rose-300 font-mono">
              {adaptiveInspectionError}
            </div>
          )}

          {adaptiveInspection && (
            <div className="space-y-4 pt-2 border-t border-slate-800" data-testid="adaptive-inspection-results">
              <div className="flex flex-wrap items-center justify-between gap-3 p-3 rounded-lg bg-slate-950 border border-slate-800 text-xs font-mono">
                <div>
                  <span className="text-slate-500">Session ID:</span> <span className="text-indigo-300 font-bold">{adaptiveInspection.sessionId}</span>
                </div>
                <div>
                  <span className="text-slate-500">Role:</span> <span className="text-slate-300">{adaptiveInspection.roleId}</span>
                </div>
                <div>
                  <span className="text-slate-500">Status:</span> <span className="text-emerald-400 font-bold">{adaptiveInspection.status}</span>
                </div>
                {adaptiveInspection.summary && (
                  <div className="text-slate-400">
                    Levels: {adaptiveInspection.summary.intermediateCount} Int, {adaptiveInspection.summary.beginnerCount} Beg, {adaptiveInspection.summary.advancedCount} Adv, {adaptiveInspection.summary.insufficientEvidenceCount} IE
                  </div>
                )}
              </div>

              <div className="space-y-4">
                {adaptiveInspection.skills.map(sk => (
                  <div key={sk.skillId} className="p-4 rounded-xl bg-slate-950/80 border border-slate-800 space-y-3 font-mono text-xs">
                    <div className="flex items-center justify-between border-b border-slate-800/80 pb-2">
                      <span className="font-bold text-white text-sm">{sk.skillName}</span>
                      <span className="text-slate-400">Skill ID: {sk.skillId}</span>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {/* Left: AI Evaluator Outputs */}
                      <div className="p-3 rounded-lg bg-slate-900/90 border border-slate-800/90 space-y-2.5">
                        <div className="text-[11px] font-bold text-indigo-400 uppercase tracking-wider">
                          🤖 AI Evaluator Outputs (Evidence Extraction)
                        </div>

                        {/* Stage 1 */}
                        <div className="p-2.5 rounded bg-slate-950 border border-slate-800/80 space-y-1">
                          <div className="flex items-center justify-between text-[11px]">
                            <span className="text-slate-400">Stage 1 (Applied):</span>
                            <span className={`px-2 py-0.5 rounded font-bold border ${getLevelBadgeClass(sk.stage1Evaluation.provisionalLevel)}`}>
                              {sk.stage1Evaluation.provisionalLevel}
                            </span>
                          </div>
                          <div className="text-[10px] text-slate-500">Q: {sk.stage1Evaluation.questionId} • Evaluator: {sk.stage1Evaluation.evaluator}</div>
                          <div className="text-slate-300 text-[11px]">{sk.stage1Evaluation.reason}</div>
                          {sk.stage1Evaluation.observedEvidence.length > 0 && (
                            <ul className="text-[10px] text-emerald-400 space-y-0.5 list-disc pl-4 pt-1">
                              {sk.stage1Evaluation.observedEvidence.map((ev, i) => <li key={i}>{ev}</li>)}
                            </ul>
                          )}
                        </div>

                        {/* Stage 2 */}
                        {sk.stage2Evaluation && (
                          <div className="p-2.5 rounded bg-slate-950 border border-slate-800/80 space-y-1">
                            <div className="flex items-center justify-between text-[11px]">
                              <span className="text-slate-400">Stage 2 ({sk.stage2Evaluation.difficulty}):</span>
                              <span className={`px-2 py-0.5 rounded font-bold border ${getLevelBadgeClass(sk.stage2Evaluation.provisionalLevel)}`}>
                                {sk.stage2Evaluation.provisionalLevel}
                              </span>
                            </div>
                            <div className="text-[10px] text-slate-500">Q: {sk.stage2Evaluation.questionId} • Evaluator: {sk.stage2Evaluation.evaluator}</div>
                            <div className="text-slate-300 text-[11px]">{sk.stage2Evaluation.reason}</div>
                            {sk.stage2Evaluation.observedEvidence.length > 0 && (
                              <ul className="text-[10px] text-emerald-400 space-y-0.5 list-disc pl-4 pt-1">
                                {sk.stage2Evaluation.observedEvidence.map((ev, i) => <li key={i}>{ev}</li>)}
                              </ul>
                            )}
                          </div>
                        )}
                      </div>

                      {/* Right: Backend Deterministic Derivations */}
                      <div className="p-3 rounded-lg bg-slate-900/90 border border-slate-800/90 space-y-2.5">
                        <div className="text-[11px] font-bold text-amber-400 uppercase tracking-wider">
                          ⚙️ Backend Deterministic Derivations
                        </div>

                        {/* Branch Decision */}
                        <div className="p-2.5 rounded bg-slate-950 border border-slate-800/80 text-[11px] space-y-1">
                          <div className="text-slate-400 font-semibold">Adaptive Branch Decision:</div>
                          <div className="text-cyan-300">Branch: {sk.branchDecision.branchChosen} → Target: {sk.branchDecision.targetDifficulty}</div>
                          <div className="text-[10px] text-slate-500">{sk.branchDecision.ruleApplied}</div>
                        </div>

                        {/* Backend Profile Derivations */}
                        {sk.backendDerivedProfile && (
                          <div className="p-2.5 rounded bg-slate-950 border border-slate-800/80 text-[11px] space-y-2">
                            <div className="flex items-center justify-between">
                              <span className="text-slate-400 font-semibold">Final Aggregated Level:</span>
                              <span className={`px-2 py-0.5 rounded font-bold border ${getLevelBadgeClass(sk.backendDerivedProfile.finalAggregatedLevel)}`}>
                                {sk.backendDerivedProfile.finalAggregatedLevel}
                              </span>
                            </div>
                            <div className="text-[10px] text-slate-500">{sk.backendDerivedProfile.aggregationRule}</div>

                            {/* Gaps */}
                            <div className="pt-1 border-t border-slate-800/60">
                              <div className="text-amber-400 font-semibold text-[10px]">Evidence / Development Gaps:</div>
                              <ul className="list-disc pl-4 text-[10px] text-slate-300 space-y-0.5">
                                {sk.backendDerivedProfile.evidenceGaps.map((g, i) => <li key={i}>{g}</li>)}
                              </ul>
                            </div>

                            {/* Next Development Areas */}
                            <div className="pt-1 border-t border-slate-800/60">
                              <div className="text-indigo-400 font-semibold text-[10px]">Approved Catalog Next Areas:</div>
                              <div className="flex flex-wrap gap-1 pt-1">
                                {sk.backendDerivedProfile.nextDevelopmentAreas.map((a, i) => (
                                  <span key={i} className="px-1.5 py-0.5 rounded bg-indigo-500/10 text-indigo-300 border border-indigo-500/20 text-[10px]">
                                    → {a}
                                  </span>
                                ))}
                              </div>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Roadmap Pipeline Inspection (Milestone I8) */}
              {adaptiveInspection.roadmap && (
                <div className="mt-6 p-4 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-base">🗺️</span>
                      <h4 className="text-sm font-bold text-white">Personalized Roadmap Generation Pipeline</h4>
                    </div>
                    <span className="px-2.5 py-0.5 rounded text-xs font-semibold bg-indigo-500/10 text-indigo-400 border border-indigo-500/30">
                      Provider: {adaptiveInspection.roadmap.provider}
                    </span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    {adaptiveInspection.roadmap.finalRoadmapItems.map((item) => (
                      <div key={item.skill} className="p-3 rounded-lg bg-slate-950 border border-slate-800 space-y-1.5">
                        <div className="flex items-center justify-between">
                          <span className="font-bold text-white">Priority {item.priority}: {item.skill}</span>
                          <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${
                            item.gapType === 'Evidence Gap' ? 'bg-amber-500/10 text-amber-400 border-amber-500/30' : 'bg-violet-500/10 text-violet-400 border-violet-500/30'
                          }`}>
                            {item.gapType}
                          </span>
                        </div>
                        {item.whyThisMatters && (
                          <p className="text-slate-400 text-[11px] leading-relaxed">{item.whyThisMatters}</p>
                        )}
                        <div className="pt-1 text-[11px] text-slate-300">
                          <span className="text-emerald-400 font-semibold">Deliverable Target: </span>
                          {item.evidenceTarget}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Project, Evidence Evaluation & Portfolio Proof Inspection (Milestone I9) */}
              {adaptiveInspection.project && (
                <div className="mt-6 p-4 rounded-xl bg-slate-900/60 border border-slate-800 space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-base">📁</span>
                      <h4 className="text-sm font-bold text-white">Gap-Based Project & Portfolio Proof Pipeline</h4>
                    </div>
                    <span className="px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/30">
                      Provider: {adaptiveInspection.project.provider}
                    </span>
                  </div>

                  <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800 text-xs space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-white text-sm">{adaptiveInspection.project.finalProject.title}</span>
                      <span className="text-slate-400 font-mono text-[10px]">{adaptiveInspection.project.projectId}</span>
                    </div>
                    <p className="text-slate-300 text-[11px] leading-relaxed">{adaptiveInspection.project.finalProject.scenario}</p>
                    <div className="pt-2 border-t border-slate-800 flex flex-wrap gap-1.5">
                      <span className="text-slate-400 font-semibold text-[10px]">Traced Gaps:</span>
                      {adaptiveInspection.project.finalProject.targetedSkills.map(ts => (
                        <span key={ts.skillId} className="px-2 py-0.5 rounded bg-indigo-500/10 text-indigo-300 border border-indigo-500/20 text-[10px]">
                          {ts.skillId} ({ts.currentLevel})
                        </span>
                      ))}
                    </div>
                  </div>

                  {/* Evaluation Trace if submitted */}
                  {adaptiveInspection.project.evaluation && (
                    <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800 text-xs space-y-2">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-300">Project Evaluation Status</span>
                        <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/10 text-emerald-400 border border-emerald-500/30">
                          {adaptiveInspection.project.evaluation.overallStatus}
                        </span>
                      </div>
                      <div className="space-y-1">
                        {adaptiveInspection.project.evaluation.skillEvidence.map(se => (
                          <div key={se.skillId} className="flex items-center justify-between text-[11px]">
                            <span className="text-slate-400 uppercase">{se.skillId}</span>
                            <span className="text-slate-300 font-mono">{se.evidenceStatus}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Portfolio Proof if generated */}
                  {adaptiveInspection.project.portfolioProof && (
                    <div className="p-3.5 rounded-lg bg-emerald-950/20 border border-emerald-500/20 text-xs space-y-2">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-emerald-400">Gated Portfolio Claims</span>
                        <span className="text-[10px] text-emerald-300">
                          {adaptiveInspection.project.portfolioProof.cvBullets.length} Verified CV Bullets
                        </span>
                      </div>
                      <ul className="list-disc pl-4 text-[11px] text-slate-300 space-y-1">
                        {adaptiveInspection.project.portfolioProof.cvBullets.map((cv, idx) => (
                          <li key={idx}>{cv}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
