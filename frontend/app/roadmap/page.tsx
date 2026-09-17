'use client';

import React, { useEffect, useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../components/JourneyShell';
import { getCanonicalRoadmap, generateRoadmap } from '../api';
import { PersonalizedRoadmapGraph, PersonalizedRoadmapNode, RoadmapNodeState, RoadmapResponse } from '../types';
import { useJourneyState } from '../lib/journey-state';
import { normalizeTechnicalText } from '../lib/text-utils';

type FilterTab = 'ALL' | 'ACTIONABLE' | 'GAPS' | 'COMPLETED' | 'TOOLKIT_OPTIONAL';

export default function RoadmapPage() {
  const router = useRouter();
  const { state, isLoaded, setSelectedRoadmapNode } = useJourneyState();

  const [canonicalGraph, setCanonicalGraph] = useState<PersonalizedRoadmapGraph | null>(null);
  const [legacyRoadmap, setLegacyRoadmap] = useState<RoadmapResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<FilterTab>('ALL');

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

  // Load canonical personalized roadmap (or fallback to legacy)
  useEffect(() => {
    if (!isLoaded || !state.selectedRoleId) return;

    let isMounted = true;

    async function loadRoadmap() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        if (state.adaptiveSessionId) {
          // Canonical V2.4 resolver path (zero OpenAI calls)
          const graph = await getCanonicalRoadmap(state.adaptiveSessionId);
          if (isMounted) {
            setCanonicalGraph(graph);
          }
        } else if (state.evaluationResult) {
          // Fallback legacy generator
          const legacy = await generateRoadmap(
            state.selectedRoleId!,
            state.evaluationResult.topGaps || [],
            state.evaluationResult.skills || []
          );
          if (isMounted) {
            setLegacyRoadmap(legacy);
          }
        } else {
          throw new Error('No assessment session or evaluation profile found.');
        }
      } catch (err: unknown) {
        if (isMounted) {
          const message = err instanceof Error ? err.message : 'Failed to load canonical roadmap';
          setErrorMessage(message);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadRoadmap();

    return () => {
      isMounted = false;
    };
  }, [isLoaded, state.selectedRoleId, state.adaptiveSessionId]);

  const handleNodeClick = (node: PersonalizedRoadmapNode) => {
    setSelectedRoadmapNode(node.canonicalSkillId);
    router.push(`/roadmap/${encodeURIComponent(node.canonicalSkillId)}`);
  };

  // Node filtering
  const filteredNodes = useMemo(() => {
    if (!canonicalGraph) return [];
    switch (activeFilter) {
      case 'ACTIONABLE':
        return canonicalGraph.nodes.filter(n =>
          n.nodeState === 'Current' || n.nodeState === 'Available' || n.nodeState === 'NeedsDevelopment'
        );
      case 'GAPS':
        return canonicalGraph.nodes.filter(n =>
          n.nodeState === 'NeedsDevelopment' || n.nodeState === 'NotAssessed'
        );
      case 'COMPLETED':
        return canonicalGraph.nodes.filter(n => n.nodeState === 'Completed');
      case 'TOOLKIT_OPTIONAL':
        return canonicalGraph.nodes.filter(n => n.isToolkit || n.isOptional || n.nodeState === 'Optional');
      case 'ALL':
      default:
        return canonicalGraph.nodes;
    }
  }, [canonicalGraph, activeFilter]);

  if (!isLoaded || (!state.evaluationResult && !state.careerProfile && !state.adaptiveSessionId)) {
    return null;
  }

  return (
    <JourneyShell
      currentStepId="roadmap"
      title="Personalized Learning Roadmap"
      subtitle="Canonical role topology personalized with verified diagnostic assessment evidence."
    >
      <div className="max-w-5xl mx-auto py-2 space-y-8" data-testid="roadmap-content">
        {isLoading && (
          <div className="flex flex-col items-center justify-center py-24 text-slate-400">
            <span className="w-4 h-4 rounded-full bg-cyan-400 animate-ping mb-4"></span>
            <p className="text-sm font-medium">Resolving canonical learning topology from diagnostic evidence...</p>
            <p className="text-xs text-slate-500 mt-1">Applying deterministic prerequisite resolution rules</p>
          </div>
        )}

        {errorMessage && (
          <div className="p-4 rounded-xl bg-red-950/40 border border-red-800 text-red-300 text-sm">
            <p className="font-semibold mb-1">Roadmap Resolution Error</p>
            <p>{errorMessage}</p>
          </div>
        )}

        {!isLoading && canonicalGraph && (
          <div className="space-y-8">
            {/* Header & Qualitative Summary Card (Strictly NO Percentage Scores) */}
            <div className="p-6 rounded-2xl bg-slate-900/80 border border-slate-800 shadow-xl space-y-6">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-800 pb-5">
                <div>
                  <span className="text-[11px] font-mono uppercase tracking-wider text-cyan-400 font-semibold">
                    Canonical Framework • roadmap.sh Topology
                  </span>
                  <h2 className="text-xl font-bold text-white mt-1">
                    {canonicalGraph.roleTitle} Learning Path
                  </h2>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Deterministic graph resolution based on your assessed competencies and verified prerequisites.
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs px-3 py-1 rounded-full bg-cyan-950/60 border border-cyan-800/80 text-cyan-300 font-mono">
                    Zero-AI Graph Resolution
                  </span>
                </div>
              </div>

              {/* Qualitative Counts Summary Grid */}
              <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
                <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800 text-center">
                  <span className="block text-2xl font-bold font-mono text-white">
                    {canonicalGraph.summary.totalNodes}
                  </span>
                  <span className="text-[10px] text-slate-400 uppercase tracking-wider font-semibold">
                    Total Nodes
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-cyan-950/30 border border-cyan-800/60 text-center">
                  <span className="block text-2xl font-bold font-mono text-cyan-400">
                    {canonicalGraph.summary.currentNodeIds.length}
                  </span>
                  <span className="text-[10px] text-cyan-300 uppercase tracking-wider font-semibold">
                    Current
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-emerald-950/30 border border-emerald-800/60 text-center">
                  <span className="block text-2xl font-bold font-mono text-emerald-400">
                    {canonicalGraph.summary.completedCount}
                  </span>
                  <span className="text-[10px] text-emerald-300 uppercase tracking-wider font-semibold">
                    Completed
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-amber-950/30 border border-amber-800/60 text-center">
                  <span className="block text-2xl font-bold font-mono text-amber-400">
                    {canonicalGraph.summary.needsDevelopmentCount}
                  </span>
                  <span className="text-[10px] text-amber-300 uppercase tracking-wider font-semibold">
                    Needs Dev
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-sky-950/30 border border-sky-800/60 text-center">
                  <span className="block text-2xl font-bold font-mono text-sky-400">
                    {canonicalGraph.summary.availableCount}
                  </span>
                  <span className="text-[10px] text-sky-300 uppercase tracking-wider font-semibold">
                    Available
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/40 border border-slate-800/80 text-center">
                  <span className="block text-2xl font-bold font-mono text-slate-400">
                    {canonicalGraph.summary.lockedCount}
                  </span>
                  <span className="text-[10px] text-slate-400 uppercase tracking-wider font-semibold">
                    Locked
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/40 border border-slate-800/80 text-center">
                  <span className="block text-2xl font-bold font-mono text-slate-300">
                    {canonicalGraph.summary.notAssessedCount}
                  </span>
                  <span className="text-[10px] text-slate-400 uppercase tracking-wider font-semibold">
                    Not Assessed
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-violet-950/20 border border-violet-800/40 text-center">
                  <span className="block text-2xl font-bold font-mono text-violet-400">
                    {canonicalGraph.summary.optionalCount}
                  </span>
                  <span className="text-[10px] text-violet-300 uppercase tracking-wider font-semibold">
                    Optional
                  </span>
                </div>
              </div>
            </div>

            {/* Filter Tabs */}
            <div className="flex flex-wrap items-center gap-2 border-b border-slate-800 pb-3">
              <button
                type="button"
                onClick={() => setActiveFilter('ALL')}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  activeFilter === 'ALL'
                    ? 'bg-cyan-500/20 border border-cyan-500/50 text-cyan-300'
                    : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                All Competencies ({canonicalGraph.nodes.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveFilter('ACTIONABLE')}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  activeFilter === 'ACTIONABLE'
                    ? 'bg-cyan-500/20 border border-cyan-500/50 text-cyan-300'
                    : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                Actionable Path (
                {canonicalGraph.nodes.filter(
                  n => n.nodeState === 'Current' || n.nodeState === 'Available' || n.nodeState === 'NeedsDevelopment'
                ).length}
                )
              </button>
              <button
                type="button"
                onClick={() => setActiveFilter('GAPS')}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  activeFilter === 'GAPS'
                    ? 'bg-amber-500/20 border border-amber-500/50 text-amber-300'
                    : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                Gaps & Needs Dev ({canonicalGraph.summary.needsDevelopmentCount + canonicalGraph.summary.notAssessedCount})
              </button>
              <button
                type="button"
                onClick={() => setActiveFilter('COMPLETED')}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  activeFilter === 'COMPLETED'
                    ? 'bg-emerald-500/20 border border-emerald-500/50 text-emerald-300'
                    : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                Completed ({canonicalGraph.summary.completedCount})
              </button>
              <button
                type="button"
                onClick={() => setActiveFilter('TOOLKIT_OPTIONAL')}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  activeFilter === 'TOOLKIT_OPTIONAL'
                    ? 'bg-violet-500/20 border border-violet-500/50 text-violet-300'
                    : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                Toolkit & Electives (
                {canonicalGraph.nodes.filter(n => n.isToolkit || n.isOptional || n.nodeState === 'Optional').length}
                )
              </button>
            </div>

            {/* Visual Roadmap Topology Timeline */}
            <div className="relative space-y-4">
              {/* Central Spine Connector Line for Desktop */}
              <div className="hidden md:block absolute left-8 top-6 bottom-6 w-0.5 bg-gradient-to-b from-cyan-500/40 via-slate-800 to-slate-800" />

              {filteredNodes.map((node, index) => {
                const isCurrent = node.nodeState === 'Current';
                const isCompleted = node.nodeState === 'Completed';
                const isNeedsDev = node.nodeState === 'NeedsDevelopment';
                const isAvailable = node.nodeState === 'Available';
                const isLocked = node.nodeState === 'Locked';
                const isNotAssessed = node.nodeState === 'NotAssessed';
                const isOptional = node.nodeState === 'Optional';

                // Prerequisite status lookup
                const prereqNodes = canonicalGraph.nodes.filter(n =>
                  node.prerequisiteSkillIds.includes(n.canonicalSkillId)
                );

                return (
                  <div
                    key={node.canonicalSkillId}
                    data-testid={`roadmap-node-${node.canonicalSkillId}`}
                    onClick={() => handleNodeClick(node)}
                    className={`group relative p-5 md:pl-16 rounded-2xl border transition-all cursor-pointer ${
                      isCurrent
                        ? 'bg-slate-900/90 border-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.25)] hover:border-cyan-300'
                        : isCompleted
                        ? 'bg-slate-900/60 border-emerald-800/60 hover:border-emerald-700/80'
                        : isNeedsDev
                        ? 'bg-slate-900/70 border-amber-800/70 hover:border-amber-700'
                        : isAvailable
                        ? 'bg-slate-900/60 border-sky-800/60 hover:border-sky-700'
                        : isLocked
                        ? 'bg-slate-950/40 border-slate-800/60 text-slate-400 opacity-80 hover:opacity-100 hover:border-slate-700'
                        : isNotAssessed
                        ? 'bg-slate-950/30 border-dashed border-slate-800 hover:border-slate-700'
                        : 'bg-slate-900/40 border-dashed border-violet-800/40 hover:border-violet-700'
                    }`}
                  >
                    {/* Node Position Marker */}
                    <div
                      className={`hidden md:flex absolute left-5 top-6 -translate-x-1/2 w-7 h-7 rounded-full items-center justify-center border text-xs font-mono font-bold z-10 ${
                        isCurrent
                          ? 'bg-cyan-500 text-slate-950 border-cyan-300 ring-4 ring-cyan-500/20 animate-pulse'
                          : isCompleted
                          ? 'bg-emerald-950 text-emerald-400 border-emerald-600'
                          : isNeedsDev
                          ? 'bg-amber-950 text-amber-400 border-amber-600'
                          : isAvailable
                          ? 'bg-sky-950 text-sky-400 border-sky-600'
                          : isLocked
                          ? 'bg-slate-900 text-slate-500 border-slate-800'
                          : isNotAssessed
                          ? 'bg-slate-950 text-slate-400 border-dashed border-slate-700'
                          : 'bg-violet-950 text-violet-400 border-violet-700'
                      }`}
                    >
                      {isCompleted ? '✓' : isLocked ? '🔒' : isCurrent ? '★' : index + 1}
                    </div>

                    <div className="flex flex-col md:flex-row md:items-start justify-between gap-3 mb-2">
                      <div className="space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className="text-base font-semibold text-white group-hover:text-cyan-300 transition-colors font-mono">
                            {node.name}
                          </h3>

                          {/* State Badges with distinct colors and explicit accessible labels */}
                          {isCurrent && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-cyan-950 border border-cyan-400 text-cyan-300 font-bold uppercase tracking-wider">
                              Current Priority
                            </span>
                          )}
                          {isCompleted && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-emerald-950 border border-emerald-600 text-emerald-300 font-bold uppercase tracking-wider">
                              Completed
                            </span>
                          )}
                          {isNeedsDev && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-amber-950 border border-amber-600 text-amber-300 font-bold uppercase tracking-wider">
                              Needs Development
                            </span>
                          )}
                          {isAvailable && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-sky-950 border border-sky-600 text-sky-300 font-bold uppercase tracking-wider">
                              Available
                            </span>
                          )}
                          {isLocked && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-slate-900 border border-slate-700 text-slate-400 font-medium uppercase tracking-wider">
                              Locked
                            </span>
                          )}
                          {isNotAssessed && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-slate-900/80 border border-slate-700 text-slate-300 font-medium uppercase tracking-wider">
                              Not Assessed
                            </span>
                          )}
                          {isOptional && (
                            <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-violet-950/60 border border-violet-700 text-violet-300 font-medium uppercase tracking-wider">
                              Optional Elective
                            </span>
                          )}

                          {/* Requirement / Classification Badges */}
                          <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800 text-slate-400 font-mono">
                            {node.requirement}
                          </span>
                          {node.isToolkit && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800 border border-slate-700 text-slate-300 font-mono">
                              Toolkit
                            </span>
                          )}
                        </div>

                        <div className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
                          <span>Category: <span className="text-slate-300">{node.category}</span></span>
                          <span>•</span>
                          <span>
                            Assessment: <span className="text-slate-200 font-medium">{node.assessmentState}</span>
                          </span>
                          {node.gapType !== 'NO_GAP' && (
                            <>
                              <span>•</span>
                              <span className="text-amber-400 font-mono text-[11px]">{node.gapType}</span>
                            </>
                          )}
                        </div>
                      </div>

                      <div className="flex items-center gap-2 self-start">
                        <span className="text-xs text-cyan-400 group-hover:underline font-mono">
                          Inspect Node →
                        </span>
                      </div>
                    </div>

                    {/* Evidence-grounded rationale */}
                    <p className="text-xs text-slate-300 leading-relaxed mt-2 mb-3">
                      {normalizeTechnicalText(node.whyThisNode)}
                    </p>

                    {/* Prerequisite Connectors (Snake and Ladder Semantics) */}
                    {prereqNodes.length > 0 && (
                      <div className="mb-3 p-2.5 rounded-lg bg-slate-950/70 border border-slate-800/80 text-xs">
                        <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider block mb-1.5">
                          Prerequisites ({prereqNodes.length}):
                        </span>
                        <div className="flex flex-wrap gap-2">
                          {prereqNodes.map(prereq => (
                            <span
                              key={prereq.canonicalSkillId}
                              className={`text-[11px] px-2 py-0.5 rounded-md border font-mono ${
                                prereq.nodeState === 'Completed'
                                  ? 'bg-emerald-950/40 border-emerald-800 text-emerald-300'
                                  : prereq.nodeState === 'NeedsDevelopment'
                                  ? 'bg-amber-950/40 border-amber-800 text-amber-300'
                                  : 'bg-slate-900 border-slate-800 text-slate-400'
                              }`}
                            >
                              {prereq.nodeState === 'Completed' ? '✓' : '⚠️'} {prereq.name} ({prereq.nodeState})
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Next Action */}
                    <div className="flex items-center justify-between pt-2 border-t border-slate-800/60 text-xs">
                      <div className="flex items-center gap-1.5 text-slate-400">
                        <span className="font-semibold text-cyan-400">Next Action:</span>
                        <span>{node.nextAction}</span>
                      </div>
                      <span className="text-[10px] text-slate-500 font-mono">
                        ID: {node.canonicalSkillId}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Bottom Actions */}
            <div className="flex items-center justify-between pt-6 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/assessment/result')}
                className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
              >
                ← View Skill Profile
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

        {/* Fallback Legacy Linear Roadmap View */}
        {!isLoading && !canonicalGraph && legacyRoadmap && (
          <div className="space-y-6">
            <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 text-xs text-slate-400">
              Viewing linear roadmap fallback.
            </div>
            <div className="space-y-4">
              {legacyRoadmap.items.map((item, idx) => (
                <div
                  key={idx}
                  className="p-5 rounded-xl border border-slate-800 bg-slate-900/70 hover:border-slate-700 transition-colors"
                  data-testid={`roadmap-item-${idx}`}
                >
                  <div className="flex items-start justify-between gap-4 mb-3">
                    <div className="flex items-center gap-3">
                      <span className="w-6 h-6 rounded-full bg-cyan-950 text-cyan-400 border border-cyan-800 flex items-center justify-center text-xs font-bold font-mono">
                        {item.priority || idx + 1}
                      </span>
                      <h3 className="text-base font-semibold text-white font-mono">
                        {item.skill}
                      </h3>
                    </div>
                    {item.currentLevel && (
                      <span className="text-xs px-2.5 py-0.5 rounded-full bg-slate-800 text-slate-400 font-medium">
                        Current: {item.currentLevel}
                      </span>
                    )}
                  </div>
                  {item.learningGoal && (
                    <p className="text-xs text-slate-300 leading-relaxed mb-3">
                      {normalizeTechnicalText(item.learningGoal)}
                    </p>
                  )}
                  {item.practiceTask && (
                    <div className="p-3 rounded-lg bg-slate-950/60 border border-slate-800/80 text-xs mb-3">
                      <span className="font-semibold text-cyan-400 block mb-1">Practice Task:</span>
                      <p className="text-slate-400 leading-relaxed">
                        {normalizeTechnicalText(item.practiceTask)}
                      </p>
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="flex items-center justify-between pt-6 border-t border-slate-800">
              <button
                type="button"
                onClick={() => router.push('/assessment/result')}
                className="text-xs text-slate-400 hover:text-slate-200"
              >
                ← View Skill Profile
              </button>
              <button
                type="button"
                onClick={() => router.push('/projects')}
                className="px-5 py-2.5 rounded-lg text-xs font-semibold bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-[0_0_12px_rgba(6,182,212,0.3)] transition-all"
                data-testid="continue-to-projects-button"
              >
                Recommend Practice Projects →
              </button>
            </div>
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
