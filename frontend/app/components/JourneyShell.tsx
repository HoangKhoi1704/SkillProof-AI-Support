'use client';

import React from 'react';
import Link from 'next/link';
import { useRouter, usePathname } from 'next/navigation';
import { useJourneyState } from '../lib/journey-state';

export interface JourneyStep {
  id: string;
  label: string;
  path: string;
}

const JOURNEY_STEPS: JourneyStep[] = [
  { id: 'roles', label: 'Role', path: '/roles' },
  { id: 'skills', label: 'Skills', path: '/assessment/skills' },
  { id: 'interview', label: 'Interview', path: '/assessment/interview' },
  { id: 'result', label: 'Result', path: '/assessment/result' },
  { id: 'roadmap', label: 'Roadmap', path: '/roadmap' },
  { id: 'projects', label: 'Projects', path: '/projects' },
  { id: 'portfolio', label: 'Portfolio', path: '/portfolio' }
];

interface JourneyShellProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  currentStepId: 'roles' | 'skills' | 'interview' | 'result' | 'roadmap' | 'projects' | 'portfolio';
}

export default function JourneyShell({
  children,
  title,
  subtitle,
  currentStepId
}: JourneyShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { state, isLoaded, resetJourney, touch } = useJourneyState();

  const handleStartOver = () => {
    if (window.confirm('Start over and reset your current session?')) {
      resetJourney();
      router.push('/roles');
    }
  };

  // Determine accessibility of each step based on current journey state
  const isStepAccessible = (stepId: string): boolean => {
    if (stepId === 'roles') return true;
    if (!state.selectedRoleId) return false;

    if (stepId === 'skills') return true;

    if (stepId === 'interview') {
      return state.userSelectedSkillIds.length > 0;
    }

    if (stepId === 'result') {
      return !!state.evaluationResult || !!state.careerProfile;
    }

    if (stepId === 'roadmap') {
      return !!state.evaluationResult || !!state.careerProfile || !!state.roadmapResult;
    }

    if (stepId === 'projects') {
      return !!state.roadmapResult || !!state.projectResult || !!state.adaptiveProjectResult;
    }

    if (stepId === 'portfolio') {
      return !!state.projectResult || !!state.adaptiveProjectResult || !!state.projectEvaluationResult;
    }

    return false;
  };

  const currentStepIndex = JOURNEY_STEPS.findIndex(s => s.id === currentStepId);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans" onClick={touch}>
      {/* Top Header */}
      <header className="border-b border-slate-800 bg-slate-900/80 backdrop-blur sticky top-0 z-40">
        <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link
              href="/roles"
              className="text-lg font-bold tracking-tight text-white flex items-center gap-2 hover:opacity-90"
              data-testid="brand-link"
            >
              <span className="w-2.5 h-2.5 rounded-full bg-cyan-400 inline-block shadow-[0_0_8px_rgba(34,211,238,0.8)]"></span>
              SkillProof
            </Link>
            {state.selectedRoleTitle && (
              <span
                className="text-xs px-2.5 py-0.5 rounded-full bg-slate-800 border border-slate-700 text-slate-300 font-medium"
                data-testid="selected-role-badge"
              >
                {state.selectedRoleTitle}
              </span>
            )}
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={handleStartOver}
              className="text-xs text-slate-400 hover:text-slate-200 px-3 py-1.5 rounded-md hover:bg-slate-800/80 transition-colors border border-transparent hover:border-slate-700"
              data-testid="start-over-button"
            >
              Start over
            </button>
          </div>
        </div>

        {/* Progress Stepper Bar */}
        <div className="max-w-6xl mx-auto px-4 py-2 overflow-x-auto">
          <nav className="flex items-center gap-1 sm:gap-2 min-w-max text-xs" aria-label="Journey Progress">
            {JOURNEY_STEPS.map((step, idx) => {
              const isCurrent = step.id === currentStepId;
              const isPast = idx < currentStepIndex;
              const accessible = isStepAccessible(step.id);

              return (
                <React.Fragment key={step.id}>
                  {idx > 0 && (
                    <span className="text-slate-700 select-none">/</span>
                  )}
                  {accessible && !isCurrent ? (
                    <Link
                      href={step.path}
                      className={`px-2 py-1 rounded transition-colors ${
                        isPast
                          ? 'text-slate-300 hover:text-white hover:bg-slate-800/60 font-medium'
                          : 'text-slate-400 hover:text-white'
                      }`}
                      data-testid={`nav-step-${step.id}`}
                    >
                      {step.label}
                    </Link>
                  ) : (
                    <span
                      className={`px-2 py-1 rounded select-none ${
                        isCurrent
                          ? 'bg-cyan-500/10 text-cyan-300 border border-cyan-500/30 font-semibold'
                          : 'text-slate-600 cursor-not-allowed opacity-50'
                      }`}
                      data-testid={`nav-step-${step.id}`}
                      aria-current={isCurrent ? 'step' : undefined}
                    >
                      {step.label}
                    </span>
                  )}
                </React.Fragment>
              );
            })}
          </nav>
        </div>
      </header>

      {/* Main Container */}
      <main className="flex-1 max-w-5xl w-full mx-auto px-4 py-8 flex flex-col">
        {(title || subtitle) && (
          <div className="mb-6">
            {title && <h1 className="text-2xl font-bold tracking-tight text-white mb-1">{title}</h1>}
            {subtitle && <p className="text-sm text-slate-400">{subtitle}</p>}
          </div>
        )}

        <div className="flex-1">
          {children}
        </div>
      </main>

      {/* Subtle Footer */}
      <footer className="border-t border-slate-800/80 py-4 text-center text-xs text-slate-500">
        SkillProof V2 Production Data & Navigation Foundation — 30-Minute Browser Session
      </footer>
    </div>
  );
}
