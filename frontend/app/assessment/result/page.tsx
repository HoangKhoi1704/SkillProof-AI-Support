'use client';

import { useEffect, useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../../components/JourneyShell';
import { useJourneyState } from '../../lib/journey-state';
import { normalizeTechnicalText } from '../../lib/text-utils';
import { SkillMatrixItem } from '../../types';

export default function ResultPage() {
  const router = useRouter();
  const { state, isLoaded } = useJourneyState();
  const [selectedSkillId, setSelectedSkillId] = useState<string | null>(null);
  const [filterMode, setFilterMode] = useState<'all' | 'mandatory' | 'gaps'>('all');

  // Route Guard: Require evaluation result or career profile
  useEffect(() => {
    if (isLoaded) {
      if (!state.selectedRoleId) {
        router.replace('/roles');
      } else if (!state.evaluationResult && !state.careerProfile) {
        router.replace('/assessment/skills');
      }
    }
  }, [isLoaded, state.selectedRoleId, state.evaluationResult, state.careerProfile, router]);

  const profile = state.careerProfile;
  const evaluation = state.evaluationResult;

  // Build unified Skill Matrix items
  const matrixItems: SkillMatrixItem[] = useMemo(() => {
    if (profile?.skillMatrix && profile.skillMatrix.length > 0) {
      return profile.skillMatrix;
    }

    // Fallback: derive matrix from profile.skills or evaluation.skills
    if (profile?.skills) {
      return profile.skills.map(s => {
        const gapType =
          s.finalLevel === 'Advanced'
            ? 'NONE'
            : s.finalLevel === 'Insufficient Evidence'
            ? 'EVIDENCE GAP'
            : 'ASSESSED GAP';

        return {
          canonicalSkillId: s.skillId,
          skillName: s.skillName || s.skillId,
          category: 'core',
          isMandatoryFundamental: true,
          overallStatus: s.finalLevel,
          fundamentalsDimension: s.finalLevel === 'Advanced' || s.finalLevel === 'Intermediate' ? 'Demonstrated' : s.finalLevel === 'Beginner' ? 'Emerging' : 'Insufficient Evidence',
          appliedDimension: s.finalLevel === 'Advanced' || s.finalLevel === 'Intermediate' ? 'Demonstrated' : s.finalLevel === 'Beginner' ? 'Emerging' : 'Insufficient Evidence',
          reasoningDimension: s.finalLevel === 'Advanced' ? 'Demonstrated' : 'Emerging',
          gapType,
          evidenceObserved: s.evidence || [],
          whyThisLevel: s.reasoning?.join(' ') || 'Assessed via calibrated interview sequence.',
          whatToImproveNext: s.nextDevelopmentAreas || []
        };
      });
    }

    if (evaluation?.skills) {
      return evaluation.skills.map((e: { name: string; level: string; evidence: string[]; reason: string }) => ({
        canonicalSkillId: e.name,
        skillName: e.name,
        category: 'core',
        isMandatoryFundamental: true,
        overallStatus: e.level,
        fundamentalsDimension: e.level === 'Advanced' || e.level === 'Intermediate' ? 'Demonstrated' : 'Emerging',
        appliedDimension: e.level === 'Advanced' || e.level === 'Intermediate' ? 'Demonstrated' : 'Emerging',
        reasoningDimension: e.level === 'Advanced' ? 'Demonstrated' : 'Emerging',
        gapType: e.level === 'Advanced' ? 'NONE' : e.level === 'Insufficient Evidence' ? 'EVIDENCE GAP' : 'ASSESSED GAP',
        evidenceObserved: e.evidence || [],
        whyThisLevel: e.reason || 'Assessed via evaluation.',
        whatToImproveNext: []
      }));
    }

    return [];
  }, [profile, evaluation]);

  // Set default selected skill
  useEffect(() => {
    if (matrixItems.length > 0 && !selectedSkillId) {
      setSelectedSkillId(matrixItems[0].canonicalSkillId);
    }
  }, [matrixItems, selectedSkillId]);

  if (!isLoaded || (!state.evaluationResult && !state.careerProfile)) {
    return null;
  }

  // Filter items
  const filteredItems = matrixItems.filter(item => {
    if (filterMode === 'mandatory') return item.isMandatoryFundamental;
    if (filterMode === 'gaps') return item.gapType !== 'NONE';
    return true;
  });

  const selectedItem = matrixItems.find(i => i.canonicalSkillId === selectedSkillId) || matrixItems[0];

  // Counts for overview
  const advancedCount = matrixItems.filter(i => i.overallStatus === 'Advanced').length;
  const intermediateCount = matrixItems.filter(i => i.overallStatus === 'Intermediate').length;
  const beginnerCount = matrixItems.filter(i => i.overallStatus === 'Beginner').length;
  const insufficientCount = matrixItems.filter(i => i.overallStatus === 'Insufficient Evidence').length;
  const notAssessedCount = matrixItems.filter(i => i.overallStatus === 'Not Assessed').length;

  const assessedGapsCount = matrixItems.filter(i => i.gapType === 'ASSESSED GAP').length;
  const evidenceGapsCount = matrixItems.filter(i => i.gapType === 'EVIDENCE GAP').length;
  const roleCoverageGapsCount = matrixItems.filter(i => i.gapType === 'ROLE COVERAGE GAP').length;

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Advanced':
        return 'bg-emerald-950/80 text-emerald-300 border-emerald-800';
      case 'Intermediate':
        return 'bg-cyan-950/80 text-cyan-300 border-cyan-800';
      case 'Beginner':
        return 'bg-amber-950/80 text-amber-300 border-amber-800';
      case 'Insufficient Evidence':
        return 'bg-rose-950/60 text-rose-300 border-rose-800/80';
      case 'Not Assessed':
        return 'bg-slate-900 text-slate-400 border-slate-700';
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  const getGapBadge = (gapType: string) => {
    switch (gapType) {
      case 'ROLE COVERAGE GAP':
        return 'bg-purple-950/80 text-purple-300 border-purple-800';
      case 'EVIDENCE GAP':
        return 'bg-rose-950/80 text-rose-300 border-rose-800';
      case 'ASSESSED GAP':
        return 'bg-amber-950/80 text-amber-300 border-amber-800';
      case 'NONE':
        return 'bg-emerald-950/60 text-emerald-400 border-emerald-800/60';
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  return (
    <JourneyShell
      currentStepId="result"
      title="Visual Skill Matrix"
      subtitle="Comprehensive qualitative readiness across role competencies with strict evidence grounding."
    >
      <div className="max-w-5xl mx-auto py-2">
        {/* Role Summary & State Counts Bar */}
        <div className="p-6 rounded-2xl border border-slate-800 bg-slate-900/70 mb-6 shadow-sm" data-testid="skill-matrix-header">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-5 border-b border-slate-800">
            <div>
              <span className="text-xs uppercase font-bold tracking-wider text-cyan-400">Target Role Benchmark</span>
              <h2 className="text-xl font-bold text-white mt-0.5" data-testid="target-role-name">
                {state.selectedRoleTitle || state.selectedRoleId}
              </h2>
            </div>

            {/* Qualitative State Badges (No numeric scores) */}
            <div className="flex flex-wrap items-center gap-2 text-xs">
              <span className="px-2.5 py-1 rounded-md bg-emerald-950/70 text-emerald-300 border border-emerald-800/80" data-testid="count-advanced">
                Advanced: {advancedCount}
              </span>
              <span className="px-2.5 py-1 rounded-md bg-cyan-950/70 text-cyan-300 border border-cyan-800/80" data-testid="count-intermediate">
                Intermediate: {intermediateCount}
              </span>
              <span className="px-2.5 py-1 rounded-md bg-amber-950/70 text-amber-300 border border-amber-800/80" data-testid="count-beginner">
                Beginner: {beginnerCount}
              </span>
              <span className="px-2.5 py-1 rounded-md bg-rose-950/50 text-rose-300 border border-rose-800/70" data-testid="count-insufficient">
                Insufficient Evidence: {insufficientCount}
              </span>
              <span className="px-2.5 py-1 rounded-md bg-slate-800 text-slate-400 border border-slate-700" data-testid="count-not-assessed">
                Not Assessed: {notAssessedCount}
              </span>
            </div>
          </div>

          {/* Gap Breakdown Bar */}
          <div className="pt-4 flex flex-wrap items-center justify-between gap-3 text-xs">
            <div className="flex items-center gap-2">
              <span className="text-slate-400 font-semibold">Identified Gap Semantics:</span>
              <span className="px-2 py-0.5 rounded bg-amber-950/60 text-amber-300 border border-amber-800/70 text-[11px]">
                {assessedGapsCount} Assessed Gaps
              </span>
              <span className="px-2 py-0.5 rounded bg-rose-950/60 text-rose-300 border border-rose-800/70 text-[11px]">
                {evidenceGapsCount} Evidence Gaps
              </span>
              <span className="px-2 py-0.5 rounded bg-purple-950/60 text-purple-300 border border-purple-800/70 text-[11px]">
                {roleCoverageGapsCount} Role Coverage Gaps
              </span>
            </div>

            {/* Filter Toggle */}
            <div className="flex items-center gap-1 bg-slate-950 p-1 rounded-lg border border-slate-800">
              <button
                type="button"
                onClick={() => setFilterMode('all')}
                className={`px-2.5 py-1 rounded text-[11px] font-medium transition-all ${
                  filterMode === 'all' ? 'bg-cyan-500 text-slate-950 font-semibold' : 'text-slate-400 hover:text-slate-200'
                }`}
                data-testid="filter-all"
              >
                All ({matrixItems.length})
              </button>
              <button
                type="button"
                onClick={() => setFilterMode('mandatory')}
                className={`px-2.5 py-1 rounded text-[11px] font-medium transition-all ${
                  filterMode === 'mandatory' ? 'bg-cyan-500 text-slate-950 font-semibold' : 'text-slate-400 hover:text-slate-200'
                }`}
                data-testid="filter-mandatory"
              >
                Mandatory Fundamentals
              </button>
              <button
                type="button"
                onClick={() => setFilterMode('gaps')}
                className={`px-2.5 py-1 rounded text-[11px] font-medium transition-all ${
                  filterMode === 'gaps' ? 'bg-cyan-500 text-slate-950 font-semibold' : 'text-slate-400 hover:text-slate-200'
                }`}
                data-testid="filter-gaps"
              >
                Gaps Only
              </button>
            </div>
          </div>
        </div>

        {/* Matrix Grid + Detail Panel Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 mb-8">
          {/* Main Matrix Table (7 cols) */}
          <div className="lg:col-span-7 bg-slate-900/60 border border-slate-800 rounded-2xl overflow-hidden">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-300">
                Competency Matrix ({filteredItems.length})
              </h3>
              <span className="text-[11px] text-slate-500">Click any row to inspect grounded evidence</span>
            </div>

            <div className="divide-y divide-slate-800/60 max-h-[560px] overflow-y-auto" data-testid="skill-matrix-table">
              {filteredItems.map(item => {
                const isSelected = selectedSkillId === item.canonicalSkillId;
                return (
                  <div
                    key={item.canonicalSkillId}
                    onClick={() => setSelectedSkillId(item.canonicalSkillId)}
                    className={`p-3.5 flex items-center justify-between cursor-pointer transition-colors ${
                      isSelected
                        ? 'bg-cyan-950/40 border-l-4 border-l-cyan-400 pl-2.5'
                        : 'hover:bg-slate-800/40'
                    }`}
                    data-testid={`matrix-row-${item.canonicalSkillId}`}
                  >
                    <div className="space-y-1 pr-3">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-xs font-semibold text-slate-100 font-mono">
                          {item.skillName}
                        </span>
                        {item.isMandatoryFundamental && (
                          <span className="text-[9px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded bg-cyan-950 text-cyan-300 border border-cyan-800/60">
                            Core Fundamental
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 text-[11px] text-slate-400">
                        <span>Fund: <span className="text-slate-300">{item.fundamentalsDimension}</span></span>
                        <span>•</span>
                        <span>App: <span className="text-slate-300">{item.appliedDimension}</span></span>
                        <span>•</span>
                        <span>Reas: <span className="text-slate-300">{item.reasoningDimension}</span></span>
                      </div>
                    </div>

                    <div className="flex flex-col items-end gap-1 shrink-0">
                      <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${getStatusBadge(item.overallStatus)}`} data-testid={`status-${item.canonicalSkillId}`}>
                        {item.overallStatus}
                      </span>
                      {item.gapType !== 'NONE' && (
                        <span className={`text-[9px] uppercase font-bold px-1.5 py-0.5 rounded border ${getGapBadge(item.gapType)}`} data-testid={`gap-${item.canonicalSkillId}`}>
                          {item.gapType}
                        </span>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Selected Competency Detail Card (5 cols) */}
          <div className="lg:col-span-5 bg-slate-900/80 border border-slate-800 rounded-2xl p-5" data-testid="skill-detail-panel">
            {selectedItem ? (
              <div className="space-y-4">
                <div className="pb-3 border-b border-slate-800">
                  <div className="flex items-center justify-between gap-2 mb-1">
                    <span className="text-[10px] uppercase font-bold tracking-wider text-cyan-400">
                      {selectedItem.category} competency
                    </span>
                    <span className={`text-xs font-semibold px-2.5 py-0.5 rounded-full border ${getStatusBadge(selectedItem.overallStatus)}`}>
                      {selectedItem.overallStatus}
                    </span>
                  </div>
                  <h4 className="text-base font-bold text-white font-mono" data-testid="selected-skill-name">
                    {selectedItem.skillName}
                  </h4>
                  {selectedItem.isMandatoryFundamental && (
                    <span className="inline-block mt-1 text-[10px] text-cyan-300 bg-cyan-950/60 px-2 py-0.5 rounded border border-cyan-800/60">
                      Authoritative Mandatory Fundamental
                    </span>
                  )}
                </div>

                {/* Gap Semantic Definition */}
                <div>
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 block mb-1">
                    Gap Classification
                  </span>
                  <div className="flex items-center gap-2">
                    <span className={`text-xs font-bold px-2.5 py-1 rounded border ${getGapBadge(selectedItem.gapType)}`} data-testid="selected-gap-type">
                      {selectedItem.gapType}
                    </span>
                    <span className="text-[11px] text-slate-400">
                      {selectedItem.gapType === 'ASSESSED GAP' && 'Evidence exists, but further mastery is needed.'}
                      {selectedItem.gapType === 'EVIDENCE GAP' && 'Insufficient verifiable evidence provided.'}
                      {selectedItem.gapType === 'ROLE COVERAGE GAP' && 'Competency was not assessed in this session.'}
                      {selectedItem.gapType === 'NONE' && 'Meets or exceeds career benchmark.'}
                    </span>
                  </div>
                </div>

                {/* Why This Level */}
                <div>
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 block mb-1">
                    Why This Level
                  </span>
                  <p className="text-xs text-slate-300 bg-slate-950 p-3 rounded-lg border border-slate-800 leading-relaxed font-sans" data-testid="selected-why-level">
                    {normalizeTechnicalText(selectedItem.whyThisLevel)}
                  </p>
                </div>

                {/* Evidence Observed */}
                <div>
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 block mb-1">
                    Verifiable Evidence Observed ({selectedItem.evidenceObserved?.length || 0})
                  </span>
                  {selectedItem.evidenceObserved && selectedItem.evidenceObserved.length > 0 ? (
                    <ul className="space-y-1 text-xs text-slate-300 bg-slate-950 p-3 rounded-lg border border-slate-800 font-mono text-[11px]">
                      {selectedItem.evidenceObserved.map((ev, idx) => (
                        <li key={idx} className="flex items-start gap-2">
                          <span className="text-cyan-400">•</span>
                          <span>{normalizeTechnicalText(ev)}</span>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="text-xs text-slate-500 italic bg-slate-950 p-3 rounded-lg border border-slate-800">
                      {selectedItem.overallStatus === 'Not Assessed'
                        ? 'Not evaluated in this diagnostic run.'
                        : 'No positive grounded signals detected.'}
                    </p>
                  )}
                </div>

                {/* What to Improve Next */}
                {selectedItem.whatToImproveNext && selectedItem.whatToImproveNext.length > 0 && (
                  <div>
                    <span className="text-[11px] font-semibold uppercase tracking-wider text-amber-400 block mb-1">
                      Targeted Next Development Areas
                    </span>
                    <ul className="space-y-1 text-xs text-slate-300 bg-slate-950 p-3 rounded-lg border border-slate-800">
                      {selectedItem.whatToImproveNext.map((tip, idx) => (
                        <li key={idx} className="flex items-start gap-2">
                          <span className="text-amber-400">→</span>
                          <span>{tip}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            ) : (
              <div className="text-center py-16 text-slate-500 text-xs">
                Select a competency row to view detailed evidence and improvement points.
              </div>
            )}
          </div>
        </div>

        {/* Bottom Actions */}
        <div className="flex items-center justify-between pt-6 border-t border-slate-800">
          <button
            type="button"
            onClick={() => router.push('/assessment/skills')}
            className="text-xs text-slate-400 hover:text-slate-200"
          >
            ← Modify Skill Selection
          </button>
          <button
            type="button"
            onClick={() => router.push('/roadmap')}
            className="px-6 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all"
            data-testid="continue-to-roadmap-button"
          >
            Continue to Learning Roadmap →
          </button>
        </div>
      </div>
    </JourneyShell>
  );
}
