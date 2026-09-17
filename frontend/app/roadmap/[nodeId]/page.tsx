'use client';

import React, { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import JourneyShell from '../../components/JourneyShell';
import { getCanonicalRoadmap, getNodeResources } from '../../api';
import { PersonalizedRoadmapGraph, PersonalizedRoadmapNode, LearningResourceDto } from '../../types';
import { useJourneyState } from '../../lib/journey-state';
import { normalizeTechnicalText } from '../../lib/text-utils';

export default function RoadmapNodeDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { state, isLoaded } = useJourneyState();

  const rawNodeId = params?.nodeId as string;
  const nodeId = rawNodeId ? decodeURIComponent(rawNodeId) : '';

  const [canonicalGraph, setCanonicalGraph] = useState<PersonalizedRoadmapGraph | null>(null);
  const [currentNode, setCurrentNode] = useState<PersonalizedRoadmapNode | null>(null);
  const [resources, setResources] = useState<LearningResourceDto[]>([]);
  const [isLoadingResources, setIsLoadingResources] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);
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

  // Load canonical graph and find node
  useEffect(() => {
    if (!isLoaded || !nodeId) return;

    let isMounted = true;

    async function fetchRoadmapNode() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        if (state.adaptiveSessionId) {
          const graph = await getCanonicalRoadmap(state.adaptiveSessionId);
          if (isMounted) {
            setCanonicalGraph(graph);
            const found = graph.nodes.find(n => n.canonicalSkillId === nodeId);
            if (found) {
              setCurrentNode(found);
              // Fetch verified resources for this canonical node
              setIsLoadingResources(true);
              getNodeResources(
                found.canonicalSkillId,
                state.selectedRoleId || undefined,
                found.nodeState,
                found.gapType
              )
                .then(res => {
                  if (isMounted) setResources(res.resources);
                })
                .catch(() => {
                  if (isMounted) setResources([]);
                })
                .finally(() => {
                  if (isMounted) setIsLoadingResources(false);
                });
            } else {
              setErrorMessage(`Competency node "${nodeId}" not found in canonical roadmap.`);
            }
          }
        } else {
          setErrorMessage('Diagnostic session required to inspect canonical node topology.');
        }
      } catch (err: unknown) {
        if (isMounted) {
          const message = err instanceof Error ? err.message : 'Failed to fetch roadmap node details.';
          setErrorMessage(message);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    fetchRoadmapNode();

    return () => {
      isMounted = false;
    };
  }, [isLoaded, nodeId, state.adaptiveSessionId]);

  if (!isLoaded) {
    return null;
  }

  // Prerequisite details lookup
  const prereqNodes = canonicalGraph && currentNode
    ? canonicalGraph.nodes.filter(n => currentNode.prerequisiteSkillIds.includes(n.canonicalSkillId))
    : [];

  return (
    <JourneyShell
      currentStepId="roadmap"
      title="Roadmap Competency Inspection"
      subtitle="Detailed canonical breakdown and diagnostic evidence status."
    >
      <div className="max-w-4xl mx-auto py-2 space-y-6" data-testid="roadmap-node-detail">
        {/* Back Link */}
        <div>
          <button
            type="button"
            onClick={() => router.push('/roadmap')}
            className="inline-flex items-center gap-2 text-xs font-mono text-cyan-400 hover:text-cyan-300 transition-colors"
            data-testid="back-to-roadmap-button"
          >
            ← Back to Learning Roadmap
          </button>
        </div>

        {isLoading && (
          <div className="flex flex-col items-center justify-center py-20 text-slate-400">
            <span className="w-3 h-3 rounded-full bg-cyan-400 animate-ping mb-4"></span>
            <p className="text-sm font-medium">Resolving competency placement and verified prerequisites...</p>
          </div>
        )}

        {errorMessage && (
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm">
            <p className="font-semibold mb-1">Competency Details Unavailable</p>
            <p>{errorMessage}</p>
            <div className="mt-3">
              <button
                type="button"
                onClick={() => router.push('/roadmap')}
                className="text-xs text-cyan-400 underline"
              >
                Return to Roadmap
              </button>
            </div>
          </div>
        )}

        {!isLoading && currentNode && (
          <div className="space-y-6">
            {/* Main Competency Header Card */}
            <div className={`p-6 rounded-2xl border ${
              currentNode.nodeState === 'Current'
                ? 'bg-slate-900/90 border-cyan-400 shadow-[0_0_25px_rgba(6,182,212,0.2)]'
                : currentNode.nodeState === 'Completed'
                ? 'bg-slate-900/80 border-emerald-800/80'
                : currentNode.nodeState === 'NeedsDevelopment'
                ? 'bg-slate-900/80 border-amber-800/80'
                : currentNode.nodeState === 'Available'
                ? 'bg-slate-900/80 border-sky-800/80'
                : currentNode.nodeState === 'Locked'
                ? 'bg-slate-950/70 border-slate-800 text-slate-400'
                : currentNode.nodeState === 'NotAssessed'
                ? 'bg-slate-950/70 border-dashed border-slate-700'
                : 'bg-slate-950/70 border-dashed border-violet-800/60'
            }`}>
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-800/80 pb-4 mb-5">
                <div>
                  <div className="flex items-center gap-2 mb-1.5">
                    <span className="text-xs font-mono uppercase tracking-wider text-slate-400">
                      {currentNode.category}
                    </span>
                    <span className="text-slate-600">•</span>
                    <span className="text-xs font-mono text-cyan-400">
                      {currentNode.canonicalSkillId}
                    </span>
                  </div>
                  <h1 className="text-2xl font-bold text-white font-mono">
                    {currentNode.name}
                  </h1>
                </div>

                {/* Qualitative Roadmap State Badge */}
                <div className="flex flex-wrap items-center gap-2">
                  {currentNode.nodeState === 'Current' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-cyan-950 border border-cyan-400 text-cyan-300 font-bold uppercase tracking-wider">
                      ★ Current Priority
                    </span>
                  )}
                  {currentNode.nodeState === 'Completed' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-emerald-950 border border-emerald-600 text-emerald-300 font-bold uppercase tracking-wider">
                      ✓ Completed
                    </span>
                  )}
                  {currentNode.nodeState === 'NeedsDevelopment' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-amber-950 border border-amber-600 text-amber-300 font-bold uppercase tracking-wider">
                      ⚠️ Needs Development
                    </span>
                  )}
                  {currentNode.nodeState === 'Available' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-sky-950 border border-sky-600 text-sky-300 font-bold uppercase tracking-wider">
                      ▶ Available
                    </span>
                  )}
                  {currentNode.nodeState === 'Locked' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-slate-900 border border-slate-700 text-slate-400 font-medium uppercase tracking-wider">
                      🔒 Locked
                    </span>
                  )}
                  {currentNode.nodeState === 'NotAssessed' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-slate-900 border border-slate-700 text-slate-300 font-medium uppercase tracking-wider">
                      ? Not Assessed
                    </span>
                  )}
                  {currentNode.nodeState === 'Optional' && (
                    <span className="text-xs px-3 py-1 rounded-full bg-violet-950/60 border border-violet-700 text-violet-300 font-medium uppercase tracking-wider">
                      ✨ Optional Elective
                    </span>
                  )}
                </div>
              </div>

              {/* Attributes Grid */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
                <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                  <span className="text-[10px] text-slate-500 uppercase tracking-wider block font-semibold mb-1">
                    Role Requirement
                  </span>
                  <span className="font-medium text-slate-200">
                    {currentNode.requirement}
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                  <span className="text-[10px] text-slate-500 uppercase tracking-wider block font-semibold mb-1">
                    Assessment Evidence
                  </span>
                  <span className="font-medium text-slate-200">
                    {currentNode.assessmentState}
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                  <span className="text-[10px] text-slate-500 uppercase tracking-wider block font-semibold mb-1">
                    Gap Semantics
                  </span>
                  <span className={`font-medium font-mono ${
                    currentNode.gapType === 'NO_GAP' ? 'text-emerald-400' : 'text-amber-400'
                  }`}>
                    {currentNode.gapType}
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                  <span className="text-[10px] text-slate-500 uppercase tracking-wider block font-semibold mb-1">
                    Classification
                  </span>
                  <span className="font-medium text-slate-200">
                    {currentNode.classification} {currentNode.isToolkit ? '(Toolkit)' : ''}
                  </span>
                </div>
              </div>
            </div>

            {/* Placement Rationale Section */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono">
                Why This Node Is Placed Here
              </h2>
              <p className="text-xs text-slate-300 leading-relaxed">
                {normalizeTechnicalText(currentNode.whyThisNode)}
              </p>
            </div>

            {/* Verified Prerequisites Section */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono flex items-center justify-between">
                <span>Verified Prerequisite Dependencies</span>
                <span className="text-xs text-slate-400 font-normal">
                  {prereqNodes.length} required
                </span>
              </h2>

              {prereqNodes.length === 0 ? (
                <div className="p-3 rounded-lg bg-slate-950/50 border border-slate-800 text-xs text-slate-400">
                  Foundational competency. No verified canonical prerequisites precede this node.
                </div>
              ) : (
                <div className="space-y-2">
                  {prereqNodes.map(prereq => (
                    <div
                      key={prereq.canonicalSkillId}
                      className="p-3 rounded-lg bg-slate-950/60 border border-slate-800 flex items-center justify-between gap-3 text-xs"
                    >
                      <div className="flex items-center gap-2">
                        <span className={`w-2 h-2 rounded-full ${
                          prereq.nodeState === 'Completed'
                            ? 'bg-emerald-400'
                            : prereq.nodeState === 'NeedsDevelopment'
                            ? 'bg-amber-400'
                            : 'bg-slate-600'
                        }`} />
                        <span className="font-medium text-white font-mono">{prereq.name}</span>
                        <span className="text-slate-500 font-mono text-[11px]">({prereq.canonicalSkillId})</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className={`text-[11px] px-2 py-0.5 rounded-full border ${
                          prereq.nodeState === 'Completed'
                            ? 'bg-emerald-950/60 border-emerald-700 text-emerald-300'
                            : prereq.nodeState === 'NeedsDevelopment'
                            ? 'bg-amber-950/60 border-amber-700 text-amber-300'
                            : 'bg-slate-900 border-slate-800 text-slate-400'
                        }`}>
                          {prereq.nodeState === 'Completed' ? 'Satisfied' : 'Pending Development'} ({prereq.nodeState})
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Verified Learning Resources Section */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-4" data-testid="learning-resources-section">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono flex items-center gap-2">
                  <span>Verified Learning Resources</span>
                  <span className="text-[10px] px-2 py-0.5 rounded-full bg-cyan-950 border border-cyan-700 text-cyan-300">
                    Official Catalog
                  </span>
                </h2>
                <span className="text-xs text-slate-400 font-mono">
                  {resources.length} available
                </span>
              </div>

              {isLoadingResources ? (
                <div className="py-6 text-center text-xs text-slate-400">
                  Loading verified resources...
                </div>
              ) : resources.length === 0 ? (
                <div className="p-4 rounded-lg bg-slate-950/50 border border-slate-800 text-xs text-slate-400 text-center" data-testid="resources-empty-state">
                  No curated resource is available for this node yet.
                </div>
              ) : (
                <div className="space-y-3" data-testid="resources-list">
                  {resources.map(res => (
                    <div
                      key={res.id}
                      className="p-4 rounded-xl bg-slate-950/70 border border-slate-800 hover:border-cyan-800/80 transition-all space-y-2.5"
                      data-testid="resource-card"
                    >
                      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                        <a
                          href={res.sourceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sm font-semibold text-cyan-400 hover:text-cyan-300 hover:underline flex items-center gap-1.5"
                          data-testid="resource-link"
                        >
                          {res.title}
                          <span className="text-xs">↗</span>
                        </a>
                        <div className="flex items-center gap-1.5 flex-wrap">
                          {res.isOfficial && (
                            <span className="text-[10px] px-2 py-0.5 rounded bg-emerald-950/70 text-emerald-300 border border-emerald-800 font-mono font-bold">
                              Official
                            </span>
                          )}
                          <span className="text-[10px] px-2 py-0.5 rounded bg-slate-900 text-slate-300 border border-slate-700 font-mono uppercase">
                            {res.resourceType}
                          </span>
                          <span className="text-[10px] px-2 py-0.5 rounded bg-slate-900 text-sky-300 border border-slate-700 font-mono uppercase">
                            {res.level}
                          </span>
                        </div>
                      </div>

                      <div className="text-xs text-slate-400 flex items-center gap-2">
                        <span className="text-slate-300 font-medium">{res.sourceName}</span>
                        {res.locator && (
                          <>
                            <span className="text-slate-600">•</span>
                            <span className="font-mono text-slate-500 text-[11px] truncate max-w-xs">{res.locator}</span>
                          </>
                        )}
                      </div>

                      {res.relevanceReason && (
                        <p className="text-xs text-slate-300 bg-slate-900/60 p-2.5 rounded-lg border border-slate-800/80 leading-relaxed">
                          💡 {normalizeTechnicalText(res.relevanceReason)}
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Next Action Section */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <h2 className="text-sm font-semibold text-white uppercase tracking-wider font-mono">
                Actionable Next Step
              </h2>
              <div className="p-3 rounded-lg bg-cyan-950/20 border border-cyan-800/40 text-xs text-cyan-200">
                <span className="font-semibold text-cyan-300 block mb-1">Recommended Action:</span>
                <p>{currentNode.nextAction}</p>
              </div>
            </div>


            {/* Bottom Actions */}
            <div className="flex items-center justify-between pt-6 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/roadmap')}
                className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
              >
                ← Return to Full Learning Path
              </button>
              <button
                type="button"
                onClick={() => router.push('/projects')}
                className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all"
                data-testid="continue-to-projects-button"
              >
                Continue to Practice Projects →
              </button>
            </div>
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
