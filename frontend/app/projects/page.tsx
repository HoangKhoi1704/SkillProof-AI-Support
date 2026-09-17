'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../components/JourneyShell';
import { getCuratedProjects, selectCuratedProject, recommendAdaptiveProject, recommendProject } from '../api';
import { CuratedProjectDto, CuratedProjectRecommendationsResponse, GapBasedProject, ProjectRecommendationResponse } from '../types';
import { useJourneyState } from '../lib/journey-state';
import { normalizeTechnicalText } from '../lib/text-utils';

export default function ProjectsPage() {
  const router = useRouter();
  const { state, isLoaded, setProject } = useJourneyState();

  const [curatedRecs, setCuratedRecs] = useState<CuratedProjectRecommendationsResponse | null>(null);
  const [legacyProject, setLegacyProject] = useState<GapBasedProject | ProjectRecommendationResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSelectingId, setIsSelectingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Route Guard: Require evaluation result or career profile
  useEffect(() => {
    if (isLoaded) {
      if (!state.selectedRoleId) {
        router.replace('/roles');
      } else if (!state.evaluationResult && !state.careerProfile && !state.adaptiveSessionId) {
        router.replace('/assessment/skills');
      }
    }
  }, [isLoaded, state.selectedRoleId, state.evaluationResult, state.careerProfile, state.adaptiveSessionId, router]);

  // Load curated recommendations
  useEffect(() => {
    if (!isLoaded || !state.selectedRoleId) return;

    let isMounted = true;

    async function loadProjects() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        if (state.adaptiveSessionId) {
          const recs = await getCuratedProjects(state.adaptiveSessionId);
          if (isMounted) {
            setCuratedRecs(recs);
            // If session already has a selected project, maintain it
            if (state.adaptiveProjectResult) {
              setLegacyProject(state.adaptiveProjectResult);
            }
          }
        } else if (state.adaptiveProjectResult) {
          if (isMounted) setLegacyProject(state.adaptiveProjectResult);
        } else if (state.evaluationResult) {
          const res = await recommendProject(
            state.selectedRoleId!,
            state.evaluationResult.topGaps || [],
            state.roadmapResult?.items
          );
          if (isMounted) setLegacyProject(res);
        }
      } catch (err: unknown) {
        if (isMounted) {
          const message = err instanceof Error ? err.message : 'Failed to load project recommendations';
          setErrorMessage(message);
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    loadProjects();

    return () => {
      isMounted = false;
    };
  }, [state.selectedRoleId, state.adaptiveSessionId, isLoaded]);

  const handleSelectPortfolioProject = async (proj: CuratedProjectDto) => {
    if (!state.adaptiveSessionId) {
      router.push('/portfolio');
      return;
    }

    setIsSelectingId(proj.id);
    setErrorMessage(null);

    try {
      const selected = await selectCuratedProject(state.adaptiveSessionId, proj.id);
      setProject(selected);
      router.push('/portfolio');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to select project for portfolio';
      setErrorMessage(msg);
      setIsSelectingId(null);
    }
  };

  if (!isLoaded) {
    return null;
  }

  return (
    <JourneyShell
      currentStepId="projects"
      title="Curated Practice & Portfolio Projects"
      subtitle="Engineered backward from your diagnosed skill gaps to provide verifiable portfolio evidence."
    >
      <div className="max-w-4xl mx-auto py-2 space-y-8" data-testid="projects-page">
        {isLoading && (
          <div className="flex flex-col items-center justify-center py-20 text-slate-400">
            <span className="w-3 h-3 rounded-full bg-cyan-400 animate-ping mb-4"></span>
            <p className="text-sm font-medium">Synthesizing targeted practice and portfolio blueprints...</p>
          </div>
        )}

        {errorMessage && (
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm">
            {errorMessage}
          </div>
        )}

        {!isLoading && curatedRecs && (
          <div className="space-y-8">
            {/* Section 1: Practice Projects */}
            <div className="space-y-4" data-testid="practice-projects-section">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div>
                  <h2 className="text-lg font-bold text-white flex items-center gap-2 font-mono">
                    <span>Targeted Practice Projects</span>
                    <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800 text-slate-300 font-normal">
                      Reinforce Roadmap Nodes
                    </span>
                  </h2>
                  <p className="text-xs text-slate-400 mt-1">
                    Focused implementation exercises designed to close immediate gaps and solidify foundational competencies.
                  </p>
                </div>
                <span className="text-xs font-mono text-cyan-400 font-semibold">
                  {curatedRecs.practiceProjects.length} available
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {curatedRecs.practiceProjects.map(proj => (
                  <div
                    key={proj.id}
                    className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition-all flex flex-col justify-between space-y-4"
                    data-testid="practice-project-card"
                  >
                    <div className="space-y-3">
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-sky-950/80 text-sky-300 border border-sky-800 font-mono">
                          Practice
                        </span>
                        <span className="text-[10px] font-mono text-slate-400">
                          ⏱ {proj.estimatedScope}
                        </span>
                      </div>

                      <h3 className="text-base font-bold text-white leading-snug">
                        {proj.title}
                      </h3>

                      <p className="text-xs text-slate-300 line-clamp-3 leading-relaxed">
                        {normalizeTechnicalText(proj.description)}
                      </p>

                      {/* Targeted Gaps */}
                      <div className="space-y-1.5 pt-1">
                        <span className="text-[10px] text-slate-400 uppercase font-semibold block tracking-wider font-mono">
                          Targeted Competencies:
                        </span>
                        <div className="flex flex-wrap gap-1.5">
                          {proj.canonicalSkillIds.slice(0, 3).map(sk => (
                            <span key={sk} className="text-[10px] px-2 py-0.5 rounded bg-slate-950 text-slate-300 border border-slate-800 font-mono">
                              {sk}
                            </span>
                          ))}
                        </div>
                      </div>

                      {/* Why Recommended */}
                      {proj.whyRecommended && (
                        <div className="text-[11px] text-cyan-300 bg-cyan-950/30 p-2.5 rounded-lg border border-cyan-900/40">
                          🎯 {proj.whyRecommended}
                        </div>
                      )}
                    </div>

                    <div className="pt-2 border-t border-slate-800/80 flex items-center justify-between">
                      <button
                        type="button"
                        onClick={() => router.push(`/projects/${proj.id}`)}
                        className="text-xs text-cyan-400 hover:text-cyan-300 font-mono flex items-center gap-1"
                        data-testid="view-practice-detail"
                      >
                        Inspect Specifications →
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Section 2: Portfolio Projects */}
            <div className="space-y-4" data-testid="portfolio-projects-section">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div>
                  <h2 className="text-lg font-bold text-white flex items-center gap-2 font-mono">
                    <span>Portfolio Projects</span>
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-cyan-950 border border-cyan-700 text-cyan-300 font-bold uppercase tracking-wider">
                      ★ Verifiable Evidence Artifact
                    </span>
                  </h2>
                  <p className="text-xs text-slate-400 mt-1">
                    Comprehensive multi-competency systems engineered to demonstrate career-ready execution and pass rigorous rubric gates.
                  </p>
                </div>
                <span className="text-xs font-mono text-cyan-400 font-semibold">
                  {curatedRecs.portfolioProjects.length} recommended
                </span>
              </div>

              <div className="space-y-4">
                {curatedRecs.portfolioProjects.map(proj => (
                  <div
                    key={proj.id}
                    className="p-6 rounded-2xl bg-slate-900/80 border border-cyan-800/50 shadow-[0_0_20px_rgba(6,182,212,0.1)] space-y-5"
                    data-testid="portfolio-project-card"
                  >
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800 pb-4">
                      <div>
                        <div className="flex items-center gap-2 mb-1.5">
                          <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-cyan-950 text-cyan-300 border border-cyan-800 font-mono">
                            Portfolio Asset
                          </span>
                          <span className="text-slate-600">•</span>
                          <span className="text-[10px] font-mono text-slate-400">
                            Difficulty: {proj.difficulty}
                          </span>
                          <span className="text-slate-600">•</span>
                          <span className="text-[10px] font-mono text-slate-400">
                            Scope: {proj.estimatedScope}
                          </span>
                        </div>
                        <h3 className="text-xl font-bold text-white font-mono" data-testid="portfolio-title">
                          {proj.title}
                        </h3>
                      </div>

                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          onClick={() => router.push(`/projects/${proj.id}`)}
                          className="px-3 py-2 rounded-lg text-xs font-mono border border-slate-700 text-slate-300 hover:text-white hover:border-slate-600 transition-colors"
                          data-testid="view-portfolio-detail"
                        >
                          View Details
                        </button>
                        <button
                          type="button"
                          disabled={isSelectingId === proj.id}
                          onClick={() => handleSelectPortfolioProject(proj)}
                          className="px-4 py-2 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all font-mono"
                          data-testid="select-portfolio-project-button"
                        >
                          {isSelectingId === proj.id ? 'Selecting...' : 'Select & Submit Evidence →'}
                        </button>
                      </div>
                    </div>

                    <p className="text-sm text-slate-300 leading-relaxed">
                      {normalizeTechnicalText(proj.description)}
                    </p>

                    {/* Targeted Gaps */}
                    <div className="space-y-1.5">
                      <span className="text-[10px] text-slate-400 uppercase font-semibold block tracking-wider font-mono">
                        Targeted Role Competencies:
                      </span>
                      <div className="flex flex-wrap gap-2">
                        {proj.canonicalSkillIds.map(sk => (
                          <span key={sk} className="text-xs px-2.5 py-1 rounded-md bg-slate-950 text-slate-200 border border-slate-800 font-mono">
                            {sk}
                          </span>
                        ))}
                      </div>
                    </div>

                    {/* Why Recommended */}
                    {proj.whyRecommended && (
                      <div className="p-3.5 rounded-xl bg-cyan-950/20 border border-cyan-800/40 text-xs text-cyan-200">
                        <strong className="text-cyan-300 block mb-1">Recommendation Rationale:</strong>
                        {proj.whyRecommended}
                      </div>
                    )}

                    {/* Deliverables Preview */}
                    <div className="space-y-2 pt-2 border-t border-slate-800/80">
                      <span className="text-xs font-semibold text-slate-300 uppercase tracking-wider font-mono">
                        Core Deliverables:
                      </span>
                      <ul className="list-disc list-inside text-xs text-slate-400 space-y-1 pl-1">
                        {proj.deliverables.map((del, idx) => (
                          <li key={idx} className="leading-relaxed">
                            {normalizeTechnicalText(del)}
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Legacy Project Fallback */}
        {!isLoading && !curatedRecs && legacyProject && (
          <div className="p-6 rounded-2xl border border-slate-800 bg-slate-900/70 space-y-5" data-testid="legacy-project-card">
            <h2 className="text-xl font-bold text-white">{legacyProject.title}</h2>
            <p className="text-sm text-slate-300">
              {'objective' in legacyProject ? legacyProject.objective : legacyProject.description}
            </p>
            <div className="pt-4 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/portfolio')}
                className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950"
              >
                Continue to Portfolio Evidence →
              </button>
            </div>
          </div>
        )}

        {/* Bottom Navigation */}
        <div className="flex items-center justify-between pt-6 border-t border-slate-800">
          <button
            type="button"
            onClick={() => router.push('/roadmap')}
            className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
          >
            ← Return to Learning Roadmap
          </button>
          <button
            type="button"
            onClick={() => router.push('/portfolio')}
            className="px-5 py-2.5 rounded-lg text-xs font-semibold border border-slate-700 hover:border-slate-600 text-slate-300 hover:text-white transition-all font-mono"
            data-testid="go-to-portfolio-direct"
          >
            Proceed to Portfolio Verification →
          </button>
        </div>
      </div>
    </JourneyShell>
  );
}
