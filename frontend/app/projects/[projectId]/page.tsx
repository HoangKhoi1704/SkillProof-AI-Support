'use client';

import React, { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import JourneyShell from '../../components/JourneyShell';
import { getCuratedProjectDetail, selectCuratedProject } from '../../api';
import { CuratedProjectDto } from '../../types';
import { useJourneyState } from '../../lib/journey-state';
import { normalizeTechnicalText } from '../../lib/text-utils';

export default function CuratedProjectDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { state, isLoaded, setProject } = useJourneyState();

  const rawProjectId = params?.projectId as string;
  const projectId = rawProjectId ? decodeURIComponent(rawProjectId) : '';

  const [project, setProjectDetail] = useState<CuratedProjectDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSelecting, setIsSelecting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Route Guard: Require active session and role
  useEffect(() => {
    if (isLoaded) {
      if (!state.selectedRoleId) {
        router.replace('/roles');
      } else if (!state.adaptiveSessionId && !state.evaluationResult) {
        router.replace('/assessment/skills');
      }
    }
  }, [isLoaded, state.selectedRoleId, state.adaptiveSessionId, state.evaluationResult, router]);

  // Load project specification
  useEffect(() => {
    if (!isLoaded || !projectId) return;

    let isMounted = true;

    async function fetchProject() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const detail = await getCuratedProjectDetail(projectId);
        if (isMounted) {
          setProjectDetail(detail);
        }
      } catch (err: unknown) {
        if (isMounted) {
          const message = err instanceof Error ? err.message : 'Failed to fetch curated project specification.';
          setErrorMessage(message);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    fetchProject();

    return () => {
      isMounted = false;
    };
  }, [isLoaded, projectId]);

  const handleSelectForPortfolio = async () => {
    if (!state.adaptiveSessionId || !project) {
      router.push('/portfolio');
      return;
    }

    setIsSelecting(true);
    setErrorMessage(null);

    try {
      const selected = await selectCuratedProject(state.adaptiveSessionId, project.id);
      setProject(selected);
      router.push('/portfolio');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to select project for portfolio';
      setErrorMessage(msg);
      setIsSelecting(false);
    }
  };

  if (!isLoaded) {
    return null;
  }

  const isDataAnalyst = state.selectedRoleId === 'data-analyst' || project?.roleIds.includes('data-analyst');

  return (
    <JourneyShell
      currentStepId="projects"
      title="Project Specification & Verification Criteria"
      subtitle="Examine core requirements, deliverable specifications, and verifiable evidence standards."
    >
      <div className="max-w-4xl mx-auto py-2 space-y-6" data-testid="project-detail-page">
        {/* Back Link */}
        <div>
          <button
            type="button"
            onClick={() => router.push('/projects')}
            className="inline-flex items-center gap-2 text-xs font-mono text-cyan-400 hover:text-cyan-300 transition-colors"
            data-testid="back-to-projects-button"
          >
            ← Back to Curated Projects
          </button>
        </div>

        {isLoading && (
          <div className="flex flex-col items-center justify-center py-20 text-slate-400">
            <span className="w-3 h-3 rounded-full bg-cyan-400 animate-ping mb-4"></span>
            <p className="text-sm font-medium">Retrieving verified project specification from catalog...</p>
          </div>
        )}

        {errorMessage && (
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm">
            <p className="font-semibold mb-1">Project Specification Unavailable</p>
            <p>{errorMessage}</p>
            <div className="mt-3">
              <button
                type="button"
                onClick={() => router.push('/projects')}
                className="text-xs text-cyan-400 underline"
              >
                Return to Projects List
              </button>
            </div>
          </div>
        )}

        {!isLoading && project && (
          <div className="space-y-6">
            {/* Main Header Card */}
            <div className="p-6 rounded-2xl border border-slate-800 bg-slate-900/80 shadow-sm space-y-5">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-800 pb-4">
                <div>
                  <div className="flex items-center gap-2 mb-2 flex-wrap">
                    <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-cyan-950 text-cyan-300 border border-cyan-800 font-mono">
                      {project.projectType === 'portfolio' ? 'Portfolio Evidence Asset' : 'Practice Exercise'}
                    </span>
                    <span className="text-slate-600">•</span>
                    <span className="text-xs font-mono text-slate-400">
                      Difficulty: {project.difficulty}
                    </span>
                    <span className="text-slate-600">•</span>
                    <span className="text-xs font-mono text-slate-400">
                      Scope: {project.estimatedScope}
                    </span>
                  </div>
                  <h1 className="text-2xl font-bold text-white font-mono" data-testid="detail-project-title">
                    {project.title}
                  </h1>
                </div>

                <button
                  type="button"
                  disabled={isSelecting}
                  onClick={handleSelectForPortfolio}
                  className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all font-mono whitespace-nowrap self-start md:self-auto"
                  data-testid="select-this-project-button"
                >
                  {isSelecting ? 'Selecting...' : 'Select for Portfolio Proof →'}
                </button>
              </div>

              {/* Description */}
              <div className="space-y-2">
                <h2 className="text-xs font-semibold text-slate-400 uppercase tracking-wider font-mono">
                  Project Description & Scenario
                </h2>
                <p className="text-sm text-slate-200 leading-relaxed">
                  {normalizeTechnicalText(project.description)}
                </p>
              </div>

              {/* Provenance & Source Attribution */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
                <div className="space-y-1">
                  <span className="text-[10px] text-slate-500 uppercase tracking-wider font-semibold block font-mono">
                    Provenance & Catalog Source:
                  </span>
                  <div className="flex items-center gap-2 font-mono text-slate-300">
                    <span className="font-semibold">{project.source}</span>
                    {project.sourceLocator && (
                      <>
                        <span className="text-slate-600">•</span>
                        <span className="text-slate-400">{project.sourceLocator}</span>
                      </>
                    )}
                  </div>
                </div>

                {project.sourceUrl && (
                  <a
                    href={project.sourceUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-cyan-400 hover:text-cyan-300 hover:underline flex items-center gap-1 font-mono"
                    data-testid="external-source-link"
                  >
                    View Authorized External Tutorial ↗
                  </a>
                )}
              </div>
            </div>

            {/* Targeted Canonical Competencies */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono flex items-center justify-between">
                <span>Practiced Canonical Competencies</span>
                <span className="text-xs text-slate-400 font-normal font-mono">
                  {project.canonicalSkillIds.length} skills
                </span>
              </h2>
              <div className="flex flex-wrap gap-2">
                {project.canonicalSkillIds.map(skId => (
                  <span
                    key={skId}
                    className="px-3 py-1.5 rounded-lg bg-slate-950/80 border border-slate-800 text-xs font-mono text-cyan-300"
                  >
                    {skId}
                  </span>
                ))}
              </div>
            </div>

            {/* Core Deliverables */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono">
                Required Technical Deliverables
              </h2>
              <ul className="space-y-2.5">
                {project.deliverables.map((del, idx) => (
                  <li
                    key={idx}
                    className="p-3 rounded-lg bg-slate-950/60 border border-slate-800 text-xs text-slate-200 flex items-start gap-2.5"
                  >
                    <span className="text-cyan-400 font-bold">✓</span>
                    <span className="leading-relaxed">{normalizeTechnicalText(del)}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* Verifiable Evidence Requirements */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono">
                Verifiable Evidence Standards
              </h2>
              <ul className="space-y-2.5">
                {project.evidenceRequirements.map((req, idx) => (
                  <li
                    key={idx}
                    className="p-3 rounded-lg bg-slate-950/60 border border-slate-800 text-xs text-slate-300 flex items-start gap-2.5"
                  >
                    <span className="text-amber-400 font-bold">★</span>
                    <span className="leading-relaxed">{normalizeTechnicalText(req)}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* Data Analyst Submission Note */}
            {isDataAnalyst && (
              <div className="p-4 rounded-xl bg-sky-950/20 border border-sky-800/40 text-xs text-sky-200 space-y-1">
                <span className="font-semibold text-sky-300 block font-mono">
                  📊 Data Analyst Evidence Flexibility:
                </span>
                <p className="leading-relaxed">
                  Deployed web applications are not required. Acceptable verification artifacts include GitHub repositories, Jupyter notebooks, raw SQL queries, sanitized datasets, or BI dashboard links (Power BI / Tableau / Streamlit).
                </p>
              </div>
            )}

            {/* Bottom Actions */}
            <div className="flex items-center justify-between pt-6 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/projects')}
                className="text-xs text-slate-400 hover:text-slate-200 transition-colors font-mono"
              >
                ← Return to Projects
              </button>
              <button
                type="button"
                disabled={isSelecting}
                onClick={handleSelectForPortfolio}
                className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all font-mono"
                data-testid="select-project-bottom-button"
              >
                {isSelecting ? 'Selecting...' : 'Select & Proceed to Evidence Submission →'}
              </button>
            </div>
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
