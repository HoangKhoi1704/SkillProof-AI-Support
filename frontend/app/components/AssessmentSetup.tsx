'use client';

import { useState, useEffect } from 'react';
import { Role, RoleSkillsResponse, PublicSkill, AssessmentSelection } from '../types';
import { fetchRoleSkills } from '../api';

interface AssessmentSetupProps {
  role: Role;
  onContinue: (selection: AssessmentSelection, isAdaptive?: boolean) => void;
  onBack: () => void;
  initialSelection?: AssessmentSelection | null;
}

export default function AssessmentSetup({
  role,
  onContinue,
  onBack,
  initialSelection
}: AssessmentSetupProps) {
  const [catalog, setCatalog] = useState<RoleSkillsResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [mode, setMode] = useState<'recommended' | 'custom'>(initialSelection?.mode || 'recommended');
  const [selectedSkillIds, setSelectedSkillIds] = useState<string[]>(initialSelection?.selectedSkillIds || []);
  const [primaryLanguageId, setPrimaryLanguageId] = useState<string>(initialSelection?.primaryLanguageId || '');
  const [validationError, setValidationError] = useState<string | null>(null);

  // Load catalog on mount
  useEffect(() => {
    let isMounted = true;

    async function loadCatalog() {
      setIsLoading(true);
      setError(null);
      try {
        const data = await fetchRoleSkills(role.id);
        if (!isMounted) return;
        setCatalog(data);

        // Preselect core skills if no prior selection
        if (!initialSelection || initialSelection.selectedSkillIds.length === 0) {
          const coreIds = data.core.map((s) => s.id);
          setSelectedSkillIds(coreIds);
        }
      } catch (err: unknown) {
        if (!isMounted) return;
        const msg = err instanceof Error ? err.message : 'Failed to load skill catalog';
        setError(msg);
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    loadCatalog();

    return () => {
      isMounted = false;
    };
  }, [role.id, initialSelection]);

  const handleToggleSkill = (skillId: string) => {
    setValidationError(null);
    setSelectedSkillIds((prev) =>
      prev.includes(skillId) ? prev.filter((id) => id !== skillId) : [...prev, skillId]
    );
  };

  const handleSelectLanguage = (langId: string) => {
    setValidationError(null);
    setPrimaryLanguageId(langId);
  };

  const handleSwitchMode = (newMode: 'recommended' | 'custom') => {
    setMode(newMode);
    setValidationError(null);
    if (newMode === 'recommended' && catalog) {
      // In recommended mode, ensure all 6 core competencies are selected
      const coreIds = catalog.core.map((s) => s.id);
      setSelectedSkillIds(coreIds);
    }
  };

  const handleContinueClick = (isAdaptive = false) => {
    setValidationError(null);

    if (mode === 'recommended') {
      if (!primaryLanguageId) {
        setValidationError('Please select a primary programming language to continue.');
        return;
      }
      if (!catalog || catalog.core.length === 0) {
        setValidationError('Skill catalog is not loaded.');
        return;
      }

      // Recommended mode includes all core competencies
      const finalSkillIds = catalog.core.map((s) => s.id);

      onContinue({
        roleId: role.id,
        selectedSkillIds: finalSkillIds,
        primaryLanguageId,
        mode: 'recommended'
      }, isAdaptive);
    } else {
      // Custom mode: require at least one skill or language
      if (selectedSkillIds.length === 0 && !primaryLanguageId) {
        setValidationError('Please select at least one skill or language to evaluate.');
        return;
      }

      // Preserve semantic distinction: filter out primaryLanguageId from competencies
      const finalSkillIds = selectedSkillIds.filter(id => id !== primaryLanguageId);

      onContinue({
        roleId: role.id,
        selectedSkillIds: finalSkillIds,
        primaryLanguageId,
        mode: 'custom'
      }, isAdaptive);
    }
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16" data-testid="catalog-loading">
        <div className="w-12 h-12 border-4 border-indigo-500/30 border-t-indigo-500 rounded-full animate-spin mb-4"></div>
        <p className="text-slate-300 font-medium">Loading {role.name} Skill Catalog...</p>
        <p className="text-xs text-slate-500 mt-1">Retrieving competencies and programming languages from runtime catalog</p>
      </div>
    );
  }

  if (error || !catalog) {
    return (
      <div className="max-w-2xl mx-auto p-8 rounded-2xl bg-slate-900/80 border border-rose-500/30 text-center" data-testid="catalog-error">
        <div className="w-12 h-12 rounded-full bg-rose-500/10 text-rose-400 flex items-center justify-center mx-auto mb-4 text-xl font-bold">
          !
        </div>
        <h3 className="text-xl font-bold text-white mb-2">Catalog Unavailable</h3>
        <p className="text-rose-300/90 text-sm mb-6">{error || 'Could not load skills catalog.'}</p>
        <div className="flex justify-center gap-3">
          <button
            onClick={onBack}
            className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-sm font-medium transition-colors"
          >
            ← Back to Roles
          </button>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition-colors"
          >
            Retry Loading
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col max-w-4xl mx-auto w-full" data-testid="assessment-setup-screen">
      {/* Navigation & Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <button
            onClick={onBack}
            className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-800 transition-colors"
            data-testid="back-to-roles-button"
          >
            ← Change Role
          </button>
          <div>
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <span>{role.name} Assessment Setup</span>
            </h2>
            <p className="text-xs text-slate-400">Configure your target competencies & language focus</p>
          </div>
        </div>

        {/* Mode Selector Segmented Control */}
        <div className="inline-flex p-1 rounded-xl bg-slate-900 border border-slate-800">
          <button
            type="button"
            onClick={() => handleSwitchMode('recommended')}
            className={`px-4 py-1.5 rounded-lg text-xs font-medium transition-all ${
              mode === 'recommended'
                ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                : 'text-slate-400 hover:text-slate-200'
            }`}
            data-testid="mode-recommended-tab"
          >
            ⭐ Recommended Assessment
          </button>
          <button
            type="button"
            onClick={() => handleSwitchMode('custom')}
            className={`px-4 py-1.5 rounded-lg text-xs font-medium transition-all ${
              mode === 'custom'
                ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                : 'text-slate-400 hover:text-slate-200'
            }`}
            data-testid="mode-custom-tab"
          >
            🛠️ Customize Assessment
          </button>
        </div>
      </div>

      {/* Mode Explanation Notice */}
      <div className="mb-6 p-4 rounded-xl bg-slate-900/60 border border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-white">
            {mode === 'recommended' ? 'Recommended Assessment' : 'Customize Assessment'}
          </h3>
          <p className="text-xs text-slate-400 mt-0.5">
            {mode === 'recommended'
              ? 'Assess the essential skills commonly expected for backend development.'
              : 'Choose specific skills you want SkillProof to evaluate.'}
          </p>
          <p className="text-[11px] text-slate-500 mt-1 italic">
            * Note: These competencies reflect common backend industry interview benchmarks, not universal hiring requirements.
          </p>
        </div>
        <div className="text-xs font-mono text-indigo-400 shrink-0">
          {mode === 'recommended'
            ? `${catalog.core.length} Core Skills + 1 Language (${catalog.core.length + 1} Questions)`
            : `${selectedSkillIds.filter(id => catalog.core.some(c => c.id === id) || catalog.recommended.some(r => r.id === id)).length + (primaryLanguageId ? 1 : 0)} Questions to Assess (${selectedSkillIds.filter(id => catalog.core.some(c => c.id === id) || catalog.recommended.some(r => r.id === id)).length} Competencies${primaryLanguageId ? ' + 1 Language' : ''})`}
        </div>
      </div>

      {/* Validation Error Banner */}
      {validationError && (
        <div
          className="mb-6 p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-sm flex items-center justify-between"
          data-testid="setup-validation-error"
        >
          <span>{validationError}</span>
          <button
            onClick={() => setValidationError(null)}
            className="text-rose-400 hover:text-rose-200 text-xs px-2 py-0.5 rounded bg-rose-500/20"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* RECOMMENDED MODE VIEW */}
      {mode === 'recommended' && (
        <div className="flex flex-col gap-8" data-testid="recommended-view">
          {/* Section 1: Preselected Core Competencies */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <span>Core Competencies</span>
                <span className="text-xs px-2 py-0.5 rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-normal">
                  Preselected ({catalog.core.length})
                </span>
              </h4>
              <span className="text-xs text-slate-400">All 6 core competencies will be evaluated</span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5" data-testid="core-skills-grid">
              {catalog.core.map((skill) => (
                <div
                  key={skill.id}
                  className="p-4 rounded-xl bg-slate-900/70 border border-indigo-500/30 relative flex flex-col justify-between shadow-sm"
                  data-testid={`core-skill-card-${skill.id}`}
                >
                  <div>
                    <div className="flex items-center justify-between gap-2 mb-1.5">
                      <h5 className="font-semibold text-white text-sm flex items-center gap-2">
                        <span className="text-emerald-400 text-xs">✓</span>
                        <span>{skill.name}</span>
                      </h5>
                      <span className="text-[10px] uppercase font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                        {skill.category}
                      </span>
                    </div>
                    {skill.description && (
                      <p className="text-xs text-slate-400 leading-relaxed mb-3">
                        {skill.description}
                      </p>
                    )}
                  </div>

                  {skill.subskills && skill.subskills.length > 0 && (
                    <div className="flex flex-wrap gap-1 pt-2 border-t border-slate-800/80">
                      {skill.subskills.map((sub) => (
                        <span
                          key={sub.id}
                          className="text-[11px] px-2 py-0.5 rounded bg-slate-800/60 text-slate-400 border border-slate-700/50"
                          title={sub.description || sub.name}
                        >
                          {sub.name}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Section 2: Primary Programming Language Selector */}
          <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800" data-testid="primary-language-section">
            <div className="mb-4">
              <h4 className="text-base font-bold text-white flex items-center gap-2">
                <span>Select Your Primary Backend Language</span>
                <span className="text-xs px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  Required
                </span>
              </h4>
              <p className="text-xs text-slate-400 mt-1">
                Choose the primary language you code in. Diagnostic questions and reasoning prompts will be framed around your chosen backend stack.
              </p>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5" data-testid="languages-grid">
              {catalog.languages.map((lang) => {
                const isSelected = primaryLanguageId === lang.id;
                return (
                  <button
                    key={lang.id}
                    type="button"
                    onClick={() => handleSelectLanguage(lang.id)}
                    className={`p-3.5 rounded-xl border text-left transition-all flex flex-col justify-between ${
                      isSelected
                        ? 'bg-indigo-600/20 border-indigo-500 shadow-md shadow-indigo-500/10'
                        : 'bg-slate-900/80 border-slate-800 hover:border-slate-700 hover:bg-slate-850'
                    }`}
                    data-testid={`language-card-${lang.id}`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm font-bold text-white">{lang.name}</span>
                      <span className={`w-3.5 h-3.5 rounded-full border flex items-center justify-center ${
                        isSelected ? 'border-indigo-400 bg-indigo-500' : 'border-slate-600'
                      }`}>
                        {isSelected && <span className="w-1.5 h-1.5 rounded-full bg-white"></span>}
                      </span>
                    </div>
                    {lang.subskills && lang.subskills.length > 0 && (
                      <span className="text-[10px] text-slate-400 line-clamp-1">
                        {lang.subskills.map((s) => s.name).join(', ')}
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* CUSTOMIZE MODE VIEW */}
      {mode === 'custom' && (
        <div className="flex flex-col gap-8" data-testid="custom-view">
          {/* Group 1: Core Competencies */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <span>Core Competencies</span>
                <span className="text-xs px-2 py-0.5 rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                  {catalog.core.filter((s) => selectedSkillIds.includes(s.id)).length} / {catalog.core.length} Selected
                </span>
              </h4>
              <button
                type="button"
                onClick={() => {
                  const coreIds = catalog.core.map((s) => s.id);
                  const allSelected = coreIds.every((id) => selectedSkillIds.includes(id));
                  if (allSelected) {
                    setSelectedSkillIds((prev) => prev.filter((id) => !coreIds.includes(id)));
                  } else {
                    setSelectedSkillIds((prev) => Array.from(new Set([...prev, ...coreIds])));
                  }
                }}
                className="text-xs text-indigo-400 hover:text-indigo-300"
              >
                Toggle All Core
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3" data-testid="custom-core-skills-grid">
              {catalog.core.map((skill) => (
                <SkillSelectCard
                  key={skill.id}
                  skill={skill}
                  isSelected={selectedSkillIds.includes(skill.id)}
                  onToggle={() => handleToggleSkill(skill.id)}
                />
              ))}
            </div>
          </div>

          {/* Group 2: Recommended Competencies */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <span>Recommended Competencies</span>
                <span className="text-xs px-2 py-0.5 rounded bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
                  {catalog.recommended.filter((s) => selectedSkillIds.includes(s.id)).length} / {catalog.recommended.length} Selected
                </span>
              </h4>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3" data-testid="custom-recommended-skills-grid">
              {catalog.recommended.map((skill) => (
                <SkillSelectCard
                  key={skill.id}
                  skill={skill}
                  isSelected={selectedSkillIds.includes(skill.id)}
                  onToggle={() => handleToggleSkill(skill.id)}
                />
              ))}
            </div>
          </div>

          {/* Group 3: Optional Competencies */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <span>Optional Competencies</span>
                <span className="text-xs px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                  {catalog.optional.filter((s) => selectedSkillIds.includes(s.id)).length} / {catalog.optional.length} Selected
                </span>
              </h4>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3" data-testid="custom-optional-skills-grid">
              {catalog.optional.map((skill) => (
                <SkillSelectCard
                  key={skill.id}
                  skill={skill}
                  isSelected={selectedSkillIds.includes(skill.id)}
                  onToggle={() => handleToggleSkill(skill.id)}
                />
              ))}
            </div>
          </div>

          {/* Group 4: Programming Languages (Semantically & Visually Distinct) */}
          <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
            <div className="mb-4">
              <h4 className="text-base font-bold text-white flex items-center gap-2">
                <span>Programming Languages</span>
                <span className="text-xs px-2 py-0.5 rounded bg-violet-500/20 text-violet-300 border border-violet-500/30">
                  Language Stack
                </span>
              </h4>
              <p className="text-xs text-slate-400 mt-1">
                Select your primary programming language for the assessment. Languages are evaluated separately from engineering competencies.
              </p>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5" data-testid="custom-languages-grid">
              {catalog.languages.map((lang) => {
                const isSelected = primaryLanguageId === lang.id;
                return (
                  <button
                    key={lang.id}
                    type="button"
                    onClick={() => handleSelectLanguage(lang.id)}
                    className={`p-3.5 rounded-xl border text-left transition-all flex flex-col justify-between ${
                      isSelected
                        ? 'bg-violet-600/20 border-violet-500 shadow-md shadow-violet-500/10'
                        : 'bg-slate-900/80 border-slate-800 hover:border-slate-700 hover:bg-slate-850'
                    }`}
                    data-testid={`custom-language-card-${lang.id}`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm font-bold text-white">{lang.name}</span>
                      <span className={`w-3.5 h-3.5 rounded-full border flex items-center justify-center ${
                        isSelected ? 'border-violet-400 bg-violet-500' : 'border-slate-600'
                      }`}>
                        {isSelected && <span className="w-1.5 h-1.5 rounded-full bg-white"></span>}
                      </span>
                    </div>
                    {lang.subskills && lang.subskills.length > 0 && (
                      <span className="text-[10px] text-slate-400 line-clamp-1">
                        {lang.subskills.map((s) => s.name).join(', ')}
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* Action Footer */}
      <div className="mt-8 pt-6 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="text-xs text-slate-400">
          Selected: <strong className="text-white">{selectedSkillIds.length}</strong> competencies
          {primaryLanguageId ? (
            <span>
              {' '}• Primary Language:{' '}
              <strong className="text-indigo-300">
                {catalog.languages.find((l) => l.id === primaryLanguageId)?.name || primaryLanguageId}
              </strong>
            </span>
          ) : (
            <span className="text-amber-400 ml-1.5">⚠️ No primary language chosen</span>
          )}
          <span className="ml-2 font-mono text-indigo-400">
            ({mode === 'recommended' ? catalog.core.length + (primaryLanguageId ? 1 : 0) : selectedSkillIds.filter(id => catalog.core.some(c => c.id === id) || catalog.recommended.some(r => r.id === id)).length + (primaryLanguageId ? 1 : 0)} questions to assess)
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
          <button
            type="button"
            onClick={onBack}
            className="w-full sm:w-auto px-4 py-2.5 rounded-xl bg-slate-850 hover:bg-slate-800 text-slate-300 text-sm font-medium border border-slate-800 transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => handleContinueClick(false)}
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium text-sm transition-all border border-slate-700 flex items-center justify-center gap-2"
            data-testid="continue-to-assessment-button"
          >
            <span>Standard Assessment</span>
            <span>→</span>
          </button>
          <button
            type="button"
            onClick={() => handleContinueClick(true)}
            className="w-full sm:w-auto px-6 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm transition-all shadow-md shadow-indigo-600/20 hover:shadow-indigo-500/30 flex items-center justify-center gap-2"
            data-testid="start-adaptive-assessment-button"
          >
            <span>⚡ Start Adaptive Diagnostic</span>
            <span>→</span>
          </button>
        </div>
      </div>
    </div>
  );
}

function SkillSelectCard({
  skill,
  isSelected,
  onToggle
}: {
  skill: PublicSkill;
  isSelected: boolean;
  onToggle: () => void;
}) {
  return (
    <div
      onClick={onToggle}
      className={`p-4 rounded-xl border cursor-pointer transition-all flex flex-col justify-between ${
        isSelected
          ? 'bg-indigo-600/10 border-indigo-500/60 shadow-sm'
          : 'bg-slate-900/60 border-slate-800 hover:border-slate-700 hover:bg-slate-900/80'
      }`}
      data-testid={`skill-toggle-${skill.id}`}
    >
      <div>
        <div className="flex items-center justify-between gap-2 mb-1.5">
          <h5 className="font-semibold text-white text-sm flex items-center gap-2">
            <input
              type="checkbox"
              checked={isSelected}
              onChange={onToggle}
              className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-700 bg-slate-800 cursor-pointer"
              onClick={(e) => e.stopPropagation()}
            />
            <span>{skill.name}</span>
          </h5>
          <div className="flex items-center gap-1.5">
            <span className="text-[10px] uppercase font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">
              {skill.category}
            </span>
            {skill.category === 'optional' ? (
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800/80 text-slate-400 border border-slate-700/60">
                catalog-only
              </span>
            ) : (
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-indigo-950/60 text-indigo-300 border border-indigo-500/30">
                assessable
              </span>
            )}
          </div>
        </div>
        {skill.description && (
          <p className="text-xs text-slate-400 leading-relaxed mb-3">
            {skill.description}
          </p>
        )}
      </div>

      {skill.subskills && skill.subskills.length > 0 && (
        <div className="flex flex-wrap gap-1 pt-2 border-t border-slate-800/80">
          {skill.subskills.map((sub) => (
            <span
              key={sub.id}
              className="text-[11px] px-2 py-0.5 rounded bg-slate-800/60 text-slate-400 border border-slate-700/50"
            >
              {sub.name}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}
