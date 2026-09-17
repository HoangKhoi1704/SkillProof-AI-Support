'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../../components/JourneyShell';
import { fetchRoleCanonicalFramework } from '../../api';
import { CanonicalSkillNodeV3 } from '../../types';
import { useJourneyState } from '../../lib/journey-state';

export default function SkillsPage() {
  const router = useRouter();
  const { state, isLoaded, setSelectedSkills, setPrimaryLanguage } = useJourneyState();

  const [frameworkNodes, setFrameworkNodes] = useState<CanonicalSkillNodeV3[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [selectedLanguage, setSelectedLanguage] = useState<string>('csharp');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [coverageNotice, setCoverageNotice] = useState<string | null>(null);

  // Route Guard: Redirect to /roles if no role selected
  useEffect(() => {
    if (isLoaded && !state.selectedRoleId) {
      router.replace('/roles');
    }
  }, [isLoaded, state.selectedRoleId, router]);

  // Load canonical framework for selected role
  useEffect(() => {
    if (!state.selectedRoleId) return;

    async function loadFramework() {
      setIsLoading(true);
      setErrorMessage(null);
      setCoverageNotice(null);
      try {
        const framework = await fetchRoleCanonicalFramework(state.selectedRoleId!);
        // Filter: only assessable competencies (hide toolkit-only, learning-only, non-eligible nodes)
        const assessable = framework.nodes.filter(
          (node: CanonicalSkillNodeV3) => node.assessmentEligible && !node.isToolkitOnly
        );
        setFrameworkNodes(assessable);

        // Pre-fill existing selection from session if available; otherwise starts at 0 preselected skills
        if (state.userSelectedSkillIds && state.userSelectedSkillIds.length > 0) {
          setSelectedIds(state.userSelectedSkillIds);
        } else {
          setSelectedIds([]); // Exactly 0 skills preselected
        }

        if (state.primaryLanguageId) {
          setSelectedLanguage(state.primaryLanguageId);
        }
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to load canonical skill framework';
        setErrorMessage(message);
      } finally {
        setIsLoading(false);
      }
    }
    loadFramework();
  }, [state.selectedRoleId, isLoaded]);

  const toggleSkill = (skillId: string) => {
    setCoverageNotice(null);
    const updated = selectedIds.includes(skillId)
      ? selectedIds.filter(id => id !== skillId)
      : [...selectedIds, skillId];
    setSelectedIds(updated);

    const mandatoryIds = frameworkNodes
      .filter(n => n.mandatoryFundamental)
      .map(n => n.canonicalSkillId);
    setSelectedSkills(updated, mandatoryIds);
  };

  const handleSelectAll = () => {
    setCoverageNotice(null);
    const updated =
      selectedIds.length === frameworkNodes.length
        ? []
        : frameworkNodes.map(n => n.canonicalSkillId);
    setSelectedIds(updated);

    const mandatoryIds = frameworkNodes
      .filter(n => n.mandatoryFundamental)
      .map(n => n.canonicalSkillId);
    setSelectedSkills(updated, mandatoryIds);
  };

  const handleContinue = () => {
    if (selectedIds.length === 0) {
      setErrorMessage('Please select at least one skill to assess.');
      return;
    }

    // Extract role mandatory fundamental IDs from framework for session tracking
    const mandatoryIds = frameworkNodes
      .filter(n => n.mandatoryFundamental)
      .map(n => n.canonicalSkillId);

    // Save skill selection to session
    setSelectedSkills(selectedIds, mandatoryIds);
    if (state.selectedRoleId === 'backend-developer') {
      setPrimaryLanguage(selectedLanguage);
    }

    // Question Coverage Check: check if any selected skill has question coverage
    const coveredSelectedNodes = frameworkNodes.filter(
      n => selectedIds.includes(n.canonicalSkillId) && n.hasQuestionCoverage
    );

    if (coveredSelectedNodes.length === 0) {
      // Prototype-safe message for unassessed roles (Frontend Developer, Data Analyst)
      setCoverageNotice(
        'Assessment questions for these skills are not available yet. Question Bank V3 for this role is currently in development.'
      );
      return;
    }

    // Proceed to interview for roles with question coverage (Backend Developer)
    router.push('/assessment/interview');
  };

  if (!isLoaded || !state.selectedRoleId) {
    return null;
  }

  const isBackend = state.selectedRoleId === 'backend-developer';

  return (
    <JourneyShell
      currentStepId="skills"
      title="Select Skills to Assess"
      subtitle={`Choose the competencies you want to evaluate for ${state.selectedRoleTitle || 'your role'}. (0 preselected — customize your benchmark)`}
    >
      <div className="max-w-3xl mx-auto py-2">
        {/* Backend Primary Language Choice */}
        {isBackend && (
          <div className="mb-6 p-4 rounded-xl border border-slate-800 bg-slate-900/60" data-testid="language-selection-card">
            <h3 className="text-sm font-semibold text-white mb-2">Primary Programming Language</h3>
            <p className="text-xs text-slate-400 mb-3">
              Select your primary language for applied backend evaluation.
            </p>
            <div className="flex flex-wrap gap-2">
              {[
                { id: 'csharp', label: 'C#' },
                { id: 'java', label: 'Java' },
                { id: 'python', label: 'Python' },
                { id: 'typescript', label: 'TypeScript' },
                { id: 'go', label: 'Go' },
                { id: 'rust', label: 'Rust' },
                { id: 'cpp', label: 'C++' },
                { id: 'javascript', label: 'JavaScript' }
              ].map(lang => (
                <button
                  key={lang.id}
                  type="button"
                  onClick={() => setSelectedLanguage(lang.id)}
                  className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors border ${
                    selectedLanguage === lang.id
                      ? 'bg-cyan-500 text-slate-950 border-cyan-400 font-semibold'
                      : 'bg-slate-800 text-slate-300 border-slate-700 hover:bg-slate-700 hover:text-white'
                  }`}
                  data-testid={`language-card-${lang.id}`}
                >
                  {lang.label}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Selection Toolbar */}
        <div className="flex items-center justify-between mb-4 pb-2 border-b border-slate-800/80">
          <span className="text-xs text-slate-400 font-medium">
            {selectedIds.length} of {frameworkNodes.length} skills selected
          </span>
          <button
            type="button"
            onClick={handleSelectAll}
            className="text-xs text-cyan-400 hover:text-cyan-300 hover:underline"
            data-testid="select-all-button"
          >
            {selectedIds.length === frameworkNodes.length ? 'Deselect all' : 'Select all'}
          </button>
        </div>

        {/* Error or Notice Alert */}
        {errorMessage && (
          <div className="p-3.5 rounded-lg bg-red-950/40 border border-red-800 text-red-300 text-xs mb-4" role="alert">
            {errorMessage}
          </div>
        )}

        {coverageNotice && (
          <div
            className="p-4 rounded-xl bg-amber-950/30 border border-amber-800/80 text-amber-200 text-xs mb-4 flex items-start gap-3"
            role="status"
            data-testid="coverage-notice"
          >
            <span className="text-base">ℹ️</span>
            <div>
              <p className="font-semibold text-amber-100 mb-1">Question Bank Pending</p>
              <p>{coverageNotice}</p>
              <p className="text-slate-400 mt-2">
                You can return to <button onClick={() => router.push('/roles')} className="text-cyan-400 underline">Role Selection</button> or select <strong>Backend Developer</strong> to experience the live calibrated diagnostic.
              </p>
            </div>
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-12 text-slate-400">
            <span className="w-2 h-2 rounded-full bg-cyan-400 animate-ping mr-3"></span>
            <span className="text-sm">Loading assessable skills...</span>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-8" data-testid="skills-list">
            {frameworkNodes.map(node => {
              const isChecked = selectedIds.includes(node.canonicalSkillId);
              return (
                <div
                  key={node.canonicalSkillId}
                  onClick={() => toggleSkill(node.canonicalSkillId)}
                  className={`p-3.5 rounded-xl border transition-all cursor-pointer flex items-center justify-between ${
                    isChecked
                      ? 'border-cyan-500/80 bg-cyan-950/20 shadow-[0_0_10px_rgba(6,182,212,0.1)]'
                      : 'border-slate-800 bg-slate-900/60 hover:border-slate-700 hover:bg-slate-900'
                  }`}
                  data-testid={`skill-card-${node.canonicalSkillId}`}
                  role="checkbox"
                  aria-checked={isChecked}
                  tabIndex={0}
                  onKeyDown={e => {
                    if (e.key === ' ' || e.key === 'Enter') {
                      e.preventDefault();
                      toggleSkill(node.canonicalSkillId);
                    }
                  }}
                >
                  <div className="flex items-center gap-3 pr-2 min-w-0">
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => {}} // Handled by container
                      className="w-4 h-4 rounded border-slate-700 text-cyan-500 focus:ring-cyan-400 focus:ring-offset-slate-900"
                      tabIndex={-1}
                    />
                    <span className="text-xs font-medium text-slate-200 truncate">
                      {node.displayName}
                    </span>
                  </div>

                  <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-slate-800 text-slate-400 shrink-0">
                    {node.category}
                  </span>
                </div>
              );
            })}
          </div>
        )}

        {/* Action Button */}
        <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
          <button
            type="button"
            onClick={() => router.push('/roles')}
            className="px-4 py-2 rounded-lg text-xs text-slate-400 hover:text-white transition-colors"
          >
            Back to Roles
          </button>
          <button
            type="button"
            onClick={handleContinue}
            disabled={selectedIds.length === 0}
            className={`px-5 py-2.5 rounded-lg text-xs font-semibold transition-all ${
              selectedIds.length > 0
                ? 'bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_15px_rgba(6,182,212,0.3)]'
                : 'bg-slate-800 text-slate-500 cursor-not-allowed'
            }`}
            data-testid="start-interview-button"
          >
            Continue to Assessment ({selectedIds.length})
          </button>
        </div>
      </div>
    </JourneyShell>
  );
}
