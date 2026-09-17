'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../components/JourneyShell';
import { submitAdaptiveProjectEvidence } from '../api';
import {
  ProjectEvaluation,
  SubmitProjectEvidenceRequest,
  RequirementEvaluationResult,
  VerificationArtifactItem,
  SkillEvidenceResult
} from '../types';
import { useJourneyState } from '../lib/journey-state';
import { normalizeTechnicalText } from '../lib/text-utils';

export default function PortfolioPage() {
  const router = useRouter();
  const { state, isLoaded, setProjectEvaluation, resetJourney } = useJourneyState();

  const roleId = state.selectedRoleId || 'backend-developer';
  const isFrontend = roleId === 'frontend-developer';
  const isBackend = roleId === 'backend-developer';
  const isDataAnalyst = roleId === 'data-analyst';

  // Selected project details
  const selectedProject = state.adaptiveProjectResult;

  const [form, setForm] = useState<SubmitProjectEvidenceRequest>({
    repositoryUrl: state.projectEvidenceForm?.repositoryUrl || '',
    deployedUrl: state.projectEvidenceForm?.deployedUrl || '',
    notebookUrl: state.projectEvidenceForm?.notebookUrl || '',
    dashboardUrl: state.projectEvidenceForm?.dashboardUrl || '',
    datasetUrl: state.projectEvidenceForm?.datasetUrl || '',
    notes: state.projectEvidenceForm?.notes || '',
    projectSummary: state.projectEvidenceForm?.projectSummary || '',
    implementationExplanation: state.projectEvidenceForm?.implementationExplanation || '',
    architectureDecisions: state.projectEvidenceForm?.architectureDecisions || '',
    testingExplanation: state.projectEvidenceForm?.testingExplanation || '',
    evidenceExcerpts: state.projectEvidenceForm?.evidenceExcerpts || []
  });

  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [verificationStage, setVerificationStage] = useState<string>('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [evaluation, setEvaluation] = useState<ProjectEvaluation | null>(state.projectEvaluationResult || null);
  const [showTraceability, setShowTraceability] = useState<boolean>(false);

  // Route Guard: Require adaptiveSessionId or projectResult
  useEffect(() => {
    if (isLoaded) {
      if (!state.selectedRoleId) {
        router.replace('/roles');
      } else if (!state.adaptiveProjectResult && !state.projectResult && !state.evaluationResult) {
        router.replace('/projects');
      }
    }
  }, [isLoaded, state.selectedRoleId, state.adaptiveProjectResult, state.projectResult, state.evaluationResult, router]);

  const handleSubmitEvidence = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!state.adaptiveSessionId) {
      setErrorMessage('Active adaptive diagnostic session required to verify portfolio evidence.');
      return;
    }

    if (!form.repositoryUrl || !form.repositoryUrl.trim()) {
      setErrorMessage('Repository URL is required for baseline code verification.');
      return;
    }

    setIsSubmitting(true);
    setVerificationStage('Validating repository and deployment URLs...');
    setErrorMessage(null);

    try {
      const res = await submitAdaptiveProjectEvidence(state.adaptiveSessionId, form);
      setEvaluation(res);
      setProjectEvaluation(res);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to evaluate project evidence';
      setErrorMessage(message);
    } finally {
      setIsSubmitting(false);
      setVerificationStage('');
    }
  };

  const handleReassess = (_skillId?: string) => {
    router.push('/assessment/skills');
  };

  if (!isLoaded || (!state.adaptiveProjectResult && !state.projectResult && !state.evaluationResult)) {
    return null;
  }

  return (
    <JourneyShell
      currentStepId="portfolio"
      title="Portfolio Evidence & Verification"
      subtitle="Submit repository links, deployment artifacts, and notebooks to verify evidence against benchmark criteria."
    >
      <div className="max-w-4xl mx-auto py-2">
        {errorMessage && (
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm mb-6">
            {errorMessage}
          </div>
        )}

        {/* Selected Project Summary Header */}
        {selectedProject && (
          <div className="mb-6 p-5 rounded-2xl border border-slate-800 bg-slate-900/60 backdrop-blur-sm">
            <div className="flex items-center justify-between mb-2">
              <span className="text-[11px] font-mono tracking-wider uppercase text-cyan-400 font-semibold">
                Target Project: {selectedProject.projectId}
              </span>
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-slate-800 text-slate-300 border border-slate-700 font-medium">
                {isFrontend ? 'Frontend Developer' : isBackend ? 'Backend Developer' : 'Data Analyst'}
              </span>
            </div>
            <h2 className="text-lg font-bold text-white mb-1.5">{selectedProject.title}</h2>
            <p className="text-xs text-slate-400 leading-relaxed mb-3">
              {selectedProject.objective || selectedProject.scenario}
            </p>
            {selectedProject.deliverables && selectedProject.deliverables.length > 0 && (
              <div className="pt-2 border-t border-slate-800/80">
                <span className="text-[10px] uppercase tracking-wider font-semibold text-slate-400 block mb-1">
                  Expected Deliverables:
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {selectedProject.deliverables.map((deliv: string, idx: number) => (
                    <span
                      key={idx}
                      className="text-[11px] px-2 py-0.5 rounded bg-slate-800/80 text-slate-300 border border-slate-700/60"
                    >
                      {deliv}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Evidence Submission Form */}
        {!evaluation ? (
          <form
            onSubmit={handleSubmitEvidence}
            className="p-6 rounded-2xl border border-slate-800 bg-slate-900/70 space-y-5"
            data-testid="evidence-form"
          >
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div>
                <h2 className="text-base font-semibold text-white">Submit Role-Specific Implementation Evidence</h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Claims must be grounded in verifiable project evidence. Only verified competencies generate strong portfolio proof.
                </p>
              </div>
              {isSubmitting && (
                <span className="text-xs px-3 py-1 rounded-full bg-cyan-950 text-cyan-300 border border-cyan-800 animate-pulse">
                  {verificationStage || 'Checking evidence...'}
                </span>
              )}
            </div>

            {/* Role-specific Notice */}
            {isDataAnalyst && (
              <div className="p-3.5 rounded-xl bg-cyan-950/30 border border-cyan-800/60 text-xs text-cyan-200">
                <strong className="text-cyan-300">Data Analyst Evidence:</strong> A live website deployment is{' '}
                <span className="underline font-semibold">not required</span>. Verification deterministically evaluates Jupyter notebooks, data schemas, SQL scripts, and dashboard links.
              </div>
            )}

            {/* 1. Repository URL (All Roles) */}
            <div>
              <label htmlFor="repo-url" className="block text-xs font-medium text-slate-300 mb-1.5">
                GitHub / Git Repository URL <span className="text-cyan-400">*</span>
              </label>
              <input
                id="repo-url"
                type="url"
                required
                value={form.repositoryUrl}
                onChange={e => setForm(prev => ({ ...prev, repositoryUrl: e.target.value }))}
                placeholder="https://github.com/username/project-repo"
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 font-mono"
                data-testid="repo-url-input"
              />
              <p className="text-[11px] text-slate-500 mt-1">
                Static inspection analyzes repository structure, manifests, and test fixtures within safe bounded limits.
              </p>
            </div>

            {/* 2. Frontend / Backend Deployed URL */}
            {(isFrontend || isBackend) && (
              <div>
                <label htmlFor="deployed-url" className="block text-xs font-medium text-slate-300 mb-1.5">
                  {isFrontend ? 'Deployed Web Application URL (Optional)' : 'Deployed API / OpenAPI Documentation URL (Optional)'}
                </label>
                <input
                  id="deployed-url"
                  type="url"
                  value={form.deployedUrl || ''}
                  onChange={e => setForm(prev => ({ ...prev, deployedUrl: e.target.value }))}
                  placeholder={isFrontend ? 'https://my-app.vercel.app' : 'https://api.example.com/swagger/v1/swagger.json'}
                  className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 font-mono"
                  data-testid="deployed-url-input"
                />
                <p className="text-[11px] text-slate-500 mt-1">
                  Verified with SSRF-safe GET/HEAD probes. Private IPs, localhost, and internal clouds are blocked.
                </p>
              </div>
            )}

            {/* 3. Data Analyst Specific Fields */}
            {isDataAnalyst && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="notebook-url" className="block text-xs font-medium text-slate-300 mb-1.5">
                    Jupyter / Colab Notebook URL (.ipynb)
                  </label>
                  <input
                    id="notebook-url"
                    type="url"
                    value={form.notebookUrl || ''}
                    onChange={e => setForm(prev => ({ ...prev, notebookUrl: e.target.value }))}
                    placeholder="https://github.com/user/repo/blob/main/analysis.ipynb"
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none font-mono"
                    data-testid="notebook-url-input"
                  />
                </div>

                <div>
                  <label htmlFor="dashboard-url" className="block text-xs font-medium text-slate-300 mb-1.5">
                    Dashboard / BI / Streamlit URL
                  </label>
                  <input
                    id="dashboard-url"
                    type="url"
                    value={form.dashboardUrl || ''}
                    onChange={e => setForm(prev => ({ ...prev, dashboardUrl: e.target.value }))}
                    placeholder="https://share.streamlit.io/user/repo/app.py"
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none font-mono"
                    data-testid="dashboard-url-input"
                  />
                </div>

                <div className="md:col-span-2">
                  <label htmlFor="dataset-url" className="block text-xs font-medium text-slate-300 mb-1.5">
                    Dataset or Processed Data Artifact URL (Optional)
                  </label>
                  <input
                    id="dataset-url"
                    type="url"
                    value={form.datasetUrl || ''}
                    onChange={e => setForm(prev => ({ ...prev, datasetUrl: e.target.value }))}
                    placeholder="https://raw.githubusercontent.com/user/repo/main/data/cleaned.csv"
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none font-mono"
                    data-testid="dataset-url-input"
                  />
                </div>
              </div>
            )}

            {/* 4. Candidate Implementation Notes */}
            <div>
              <label htmlFor="project-summary" className="block text-xs font-medium text-slate-300 mb-1.5">
                Implementation Notes & Project Summary
              </label>
              <textarea
                id="project-summary"
                rows={3}
                value={form.projectSummary || form.notes || ''}
                onChange={e => {
                  const val = e.target.value;
                  setForm(prev => ({ ...prev, projectSummary: val, notes: val }));
                }}
                placeholder="Concise overview of what was built and implemented..."
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 font-mono"
                data-testid="evidence-summary-input"
              />
            </div>

            {/* 5. Key Architecture & Tradeoffs */}
            <div>
              <label htmlFor="arch-decisions" className="block text-xs font-medium text-slate-300 mb-1.5">
                Key Architectural Decisions & Trade-Offs
              </label>
              <textarea
                id="arch-decisions"
                rows={3}
                value={form.architectureDecisions}
                onChange={e => setForm(prev => ({ ...prev, architectureDecisions: e.target.value }))}
                placeholder="Explain architectural tradeoffs, state management, indexing strategies, or resilience mechanisms..."
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 font-mono"
                data-testid="evidence-architecture-input"
              />
            </div>

            {/* 6. Testing & Validation Strategy */}
            <div>
              <label htmlFor="testing-explanation" className="block text-xs font-medium text-slate-300 mb-1.5">
                Testing & Validation Strategy
              </label>
              <textarea
                id="testing-explanation"
                rows={3}
                value={form.testingExplanation}
                onChange={e => setForm(prev => ({ ...prev, testingExplanation: e.target.value }))}
                placeholder="Explain test coverage, automated test runners, mock isolation, or edge cases verified..."
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400 font-mono"
                data-testid="evidence-testing-input"
              />
            </div>

            <div className="flex items-center justify-between pt-4 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/projects')}
                className="px-4 py-2 rounded-lg text-xs text-slate-400 hover:text-white"
              >
                ← Back to Projects
              </button>
              <button
                type="submit"
                disabled={isSubmitting || !form.repositoryUrl || !form.repositoryUrl.trim()}
                className={`px-5 py-2.5 rounded-lg text-xs font-semibold transition-all ${
                  form.repositoryUrl && form.repositoryUrl.trim() && !isSubmitting
                    ? 'bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] cursor-pointer'
                    : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                }`}
                data-testid="submit-evidence-button"
              >
                {isSubmitting ? 'Verifying Evidence...' : 'Submit for Verification →'}
              </button>
            </div>
          </form>
        ) : (
          /* =======================================================
             Verified Evaluation & Evidence Report Display
             ======================================================= */
          <div className="space-y-6" data-testid="verified-portfolio-view">
            {/* Header Card */}
            <div className="p-6 rounded-2xl border border-slate-800 bg-slate-900/70" data-testid="project-evaluation-view">
              <div className="flex items-center justify-between pb-4 mb-4 border-b border-slate-800">
                <div>
                  <span className="text-xs uppercase font-bold tracking-wider text-cyan-400">
                    Evidence Verification Report
                  </span>
                  <h2 className="text-lg font-bold text-white mt-0.5">Project Evaluation & Portfolio Proof</h2>
                </div>
                <div className="flex items-center gap-2">
                  <span
                    className={`text-xs px-3 py-1 rounded-full font-semibold border ${
                      evaluation.overallStatus === 'Demonstrated'
                        ? 'bg-emerald-950 text-emerald-300 border-emerald-800'
                        : evaluation.overallStatus === 'Partially Demonstrated'
                        ? 'bg-amber-950 text-amber-300 border-amber-800'
                        : 'bg-slate-800 text-slate-300 border-slate-700'
                    }`}
                    data-testid="evaluation-overall-status"
                  >
                    {evaluation.overallStatus}
                  </span>
                </div>
              </div>

              {/* Verification Summary (Artifacts Checked) */}
              {evaluation.verificationReport && (
                <div className="mb-6 p-4 rounded-xl bg-slate-950/60 border border-slate-800 text-xs">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
                      Observed Artifact Verification ({evaluation.verificationReport.totalArtifactsChecked} checked)
                    </span>
                    <span className="text-[11px] text-slate-400 font-mono">
                      {evaluation.verificationReport.verifiedArtifactsCount} Verified ·{' '}
                      {evaluation.verificationReport.partiallyVerifiedCount} Partially Verified ·{' '}
                      {evaluation.verificationReport.unverifiedCount} Unverified
                    </span>
                  </div>

                  <div className="space-y-2">
                    {/* Verified Items */}
                    {evaluation.verificationReport.verifiedEvidence?.map((art: VerificationArtifactItem, idx: number) => (
                      <div key={idx} className="flex items-start gap-2 text-slate-300">
                        <span className="text-emerald-400 font-bold shrink-0">✓ Verified:</span>
                        <span>
                          <strong className="text-white font-mono">{art.artifactType}</strong> ({art.source}) —{' '}
                          {normalizeTechnicalText(art.details)}
                        </span>
                      </div>
                    ))}

                    {/* Partially Verified Items */}
                    {evaluation.verificationReport.partiallyVerifiedEvidence?.map((art: VerificationArtifactItem, idx: number) => (
                      <div key={idx} className="flex items-start gap-2 text-slate-300">
                        <span className="text-amber-400 font-bold shrink-0">⚠ Partial:</span>
                        <span>
                          <strong className="text-white font-mono">{art.artifactType}</strong> ({art.source}) —{' '}
                          {normalizeTechnicalText(art.details)}
                        </span>
                      </div>
                    ))}

                    {/* Unverified Items */}
                    {evaluation.verificationReport.unverifiedEvidence?.map((art: VerificationArtifactItem, idx: number) => (
                      <div key={idx} className="flex items-start gap-2 text-slate-400">
                        <span className="text-slate-500 font-bold shrink-0">○ Could Not Verify:</span>
                        <span>
                          <strong className="text-slate-300 font-mono">{art.artifactType}</strong> ({art.source}) —{' '}
                          {normalizeTechnicalText(art.details)}
                        </span>
                      </div>
                    ))}
                  </div>

                  {/* Security Warnings if any */}
                  {evaluation.verificationReport.securityWarnings && evaluation.verificationReport.securityWarnings.length > 0 && (
                    <div className="mt-3 pt-3 border-t border-slate-800 text-amber-300 text-[11px] space-y-1">
                      {evaluation.verificationReport.securityWarnings.map((warn: string, i: number) => (
                        <div key={i} className="flex items-center gap-1.5">
                          <span>🛡️</span>
                          <span>{warn}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Requirement Evidence Matrix (Heart of V2.6) */}
              {evaluation.requirementResults && evaluation.requirementResults.length > 0 && (
                <div className="mb-6">
                  <h3 className="text-xs font-semibold text-slate-300 uppercase tracking-wider mb-3">
                    Requirement Evidence Matrix
                  </h3>
                  <div className="overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/80">
                    <table className="w-full text-left text-xs text-slate-300">
                      <thead className="bg-slate-900/80 text-[11px] text-slate-400 uppercase tracking-wider border-b border-slate-800">
                        <tr>
                          <th className="px-3 py-2.5">Project Requirement</th>
                          <th className="px-3 py-2.5">Targets Skill</th>
                          <th className="px-3 py-2.5">Evidence Found</th>
                          <th className="px-3 py-2.5">Status</th>
                          <th className="px-3 py-2.5">Source Artifact</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-800/80">
                        {evaluation.requirementResults.map((req: RequirementEvaluationResult, idx: number) => {
                          const evidenceText =
                            req.evidenceFound && req.evidenceFound.length > 0
                              ? req.evidenceFound.join('; ')
                              : req.evaluationNotes || '';
                          return (
                            <tr key={idx} className="hover:bg-slate-900/40">
                              <td className="px-3 py-2.5 font-medium text-slate-200">
                                {normalizeTechnicalText(req.requirement)}
                              </td>
                              <td className="px-3 py-2.5 font-mono text-cyan-400 whitespace-nowrap">
                                {req.targetsSkill}
                              </td>
                              <td className="px-3 py-2.5 text-slate-300">
                                {normalizeTechnicalText(evidenceText)}
                              </td>
                              <td className="px-3 py-2.5 whitespace-nowrap">
                                <span
                                  className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase ${
                                    req.status === 'Demonstrated'
                                      ? 'bg-emerald-950 text-emerald-300 border border-emerald-800'
                                      : req.status === 'Partially Demonstrated'
                                      ? 'bg-amber-950 text-amber-300 border border-amber-800'
                                      : 'bg-slate-800 text-slate-400 border border-slate-700'
                                  }`}
                                >
                                  {req.status}
                                </span>
                              </td>
                              <td className="px-3 py-2.5 font-mono text-[11px] text-slate-400">
                                {req.sourceArtifact || 'submission notes'}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Skill Evidence Breakdown */}
              <div className="space-y-3" data-testid="skill-evidence-list">
                <h3 className="text-xs font-semibold text-slate-300 uppercase tracking-wider">
                  Evaluated Competency Evidence:
                </h3>
                <div className="flex flex-wrap gap-1.5 mb-2" data-testid="demonstrated-skills-badges">
                  {evaluation.skillEvidence?.map((ev: SkillEvidenceResult) => (
                    <span
                      key={ev.skillId}
                      className={`text-[11px] px-2.5 py-1 rounded font-mono font-medium border ${
                        ev.evidenceStatus === 'Demonstrated'
                          ? 'bg-emerald-950/80 text-emerald-300 border-emerald-800'
                          : ev.evidenceStatus === 'Partially Demonstrated'
                          ? 'bg-amber-950/80 text-amber-300 border-amber-800'
                          : 'bg-slate-800 text-slate-400 border-slate-700'
                      }`}
                    >
                      {ev.skillId}: {ev.evidenceStatus}
                    </span>
                  ))}
                </div>

                {evaluation.skillEvidence?.map((ev: SkillEvidenceResult) => (
                  <div
                    key={ev.skillId}
                    className="p-3.5 rounded-xl border border-slate-800 bg-slate-950/60 text-xs"
                    data-testid="demonstrated-evidence-list"
                  >
                    <div className="flex items-center justify-between mb-1.5">
                      <span className="font-semibold text-white font-mono">{ev.skillId}</span>
                      <div className="flex items-center gap-2">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase ${
                            ev.evidenceStatus === 'Demonstrated'
                              ? 'bg-emerald-950 text-emerald-300 border border-emerald-800'
                              : ev.evidenceStatus === 'Partially Demonstrated'
                              ? 'bg-amber-950 text-amber-300 border border-amber-800'
                              : 'bg-slate-800 text-slate-400 border border-slate-700'
                          }`}
                        >
                          {ev.evidenceStatus}
                        </span>
                        {/* Re-assess action for non-demonstrated or to verify matrix */}
                        <button
                          type="button"
                          onClick={() => handleReassess(ev.skillId)}
                          className="text-[10px] px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-cyan-300 border border-slate-700 hover:border-cyan-500 transition-colors"
                          title="Re-assess this skill in diagnostic assessment"
                          data-testid="reassess-skills-btn"
                        >
                          Re-assess Skill ↻
                        </button>
                      </div>
                    </div>
                    {ev.evidence?.map((e: string, idx: number) => (
                      <p key={idx} className="text-slate-300 leading-relaxed">
                        {normalizeTechnicalText(e)}
                      </p>
                    ))}
                    {ev.missingEvidence && ev.missingEvidence.length > 0 && (
                      <div className="mt-2 text-slate-400">
                        <span className="text-amber-400 font-medium">Missing Evidence: </span>
                        {ev.missingEvidence.join(', ')}
                      </div>
                    )}
                  </div>
                ))}
              </div>

              {/* Verified Portfolio Proof & CV Bullets */}
              {evaluation.portfolioProof && (
                <div className="p-5 rounded-xl bg-cyan-950/20 border border-cyan-800/60 mt-6" data-testid="portfolio-proof-view">
                  <div className="flex items-center justify-between mb-2">
                    <h3 className="text-xs font-semibold text-cyan-400 uppercase tracking-wider">
                      Verified Portfolio Proof & Resume Claims:
                    </h3>
                    <button
                      type="button"
                      onClick={() => setShowTraceability(!showTraceability)}
                      className="text-[10px] text-cyan-300 hover:text-cyan-100 underline cursor-pointer"
                    >
                      {showTraceability ? 'Hide Evidence Traceability' : 'Show Evidence Traceability'}
                    </button>
                  </div>

                  <p className="text-xs text-slate-300 leading-relaxed mb-3">
                    {normalizeTechnicalText(evaluation.portfolioProof.summary)}
                  </p>

                  <div data-testid="cv-bullets-list">
                    <ul className="list-disc list-inside text-xs text-slate-200 space-y-2 pl-1">
                      {evaluation.portfolioProof.portfolioBullets?.map((bullet: string, idx: number) => (
                        <li key={idx} className="leading-relaxed">
                          {normalizeTechnicalText(bullet)}
                        </li>
                      ))}
                    </ul>
                  </div>

                  {/* Claim Traceability View */}
                  {showTraceability && evaluation.portfolioProof.claimTraceability && (
                    <div className="mt-4 pt-3 border-t border-cyan-900/60 text-[11px] space-y-2">
                      <span className="font-semibold text-cyan-300 uppercase tracking-wider block">
                        Claim Provenance & Evidence Traceability:
                      </span>
                      {evaluation.portfolioProof.claimTraceability.map((trace: string, idx: number) => (
                        <div key={idx} className="p-2 rounded bg-slate-950/80 border border-slate-800 text-slate-300 font-mono text-[10px]">
                          {trace}
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Evidence Notes / Talking Points */}
                  {evaluation.portfolioProof.evidenceNotes && evaluation.portfolioProof.evidenceNotes.length > 0 && (
                    <div className="mt-4 pt-3 border-t border-cyan-900/40">
                      <span className="text-[11px] font-semibold text-slate-300 block mb-1">
                        Interview Talking Points:
                      </span>
                      <ul className="list-disc list-inside text-xs text-slate-400 space-y-1 pl-1">
                        {evaluation.portfolioProof.evidenceNotes.map((tp: string, idx: number) => (
                          <li key={idx}>{normalizeTechnicalText(tp)}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Bottom Actions */}
            <div className="flex items-center justify-between pt-6 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setEvaluation(null)}
                className="text-xs text-slate-400 hover:text-slate-200 cursor-pointer"
              >
                ← Update Evidence Submission
              </button>
              <button
                type="button"
                onClick={() => {
                  resetJourney();
                  router.push('/roles');
                }}
                className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-200 transition-colors cursor-pointer"
                data-testid="finish-journey-button"
              >
                Finish & Start New Journey
              </button>
            </div>
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
