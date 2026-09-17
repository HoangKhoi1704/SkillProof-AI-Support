'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import JourneyShell from '../components/JourneyShell';
import { fetchV3Roles } from '../api';
import { RoleSummaryV3 } from '../types';
import { useJourneyState } from '../lib/journey-state';

export default function RolesPage() {
  const router = useRouter();
  const { state, selectRole } = useJourneyState();

  const [roles, setRoles] = useState<RoleSummaryV3[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    async function loadRoles() {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        const allRoles = await fetchV3Roles();
        // Show ONLY the three primary demo roles; exclude legacy roles like Financial Analyst
        const primaryRoles = allRoles
          .filter(r => r.isPrimaryDemoRole)
          .sort((a, b) => a.displayOrder - b.displayOrder);

        setRoles(primaryRoles);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to load roles';
        setErrorMessage(message);
      } finally {
        setIsLoading(false);
      }
    }
    loadRoles();
  }, []);

  const handleSelectRole = (role: RoleSummaryV3) => {
    selectRole(role.id, role.title);
    router.push('/assessment/skills');
  };

  return (
    <JourneyShell
      currentStepId="roles"
      title="Select Your Target Career Role"
      subtitle="Choose a primary role to benchmark your technical competencies against canonical industry roadmaps."
    >
      <div className="max-w-2xl mx-auto py-4">
        {isLoading && (
          <div className="flex items-center justify-center py-12 text-slate-400">
            <span className="w-2 h-2 rounded-full bg-cyan-400 animate-ping mr-3"></span>
            <span className="text-sm">Loading canonical career roles...</span>
          </div>
        )}

        {errorMessage && (
          <div className="p-4 rounded-lg bg-red-950/40 border border-red-800 text-red-300 text-sm mb-6" role="alert">
            {errorMessage}
          </div>
        )}

        {!isLoading && !errorMessage && (
          <div className="grid grid-cols-1 gap-4" data-testid="roles-list">
            {roles.map(role => {
              const isSelected = state.selectedRoleId === role.id;
              return (
                <div
                  key={role.id}
                  onClick={() => handleSelectRole(role)}
                  className={`group relative p-5 rounded-xl border transition-all cursor-pointer flex items-center justify-between ${
                    isSelected
                      ? 'border-cyan-500 bg-cyan-950/20 shadow-[0_0_15px_rgba(6,182,212,0.15)]'
                      : 'border-slate-800 bg-slate-900/60 hover:border-slate-700 hover:bg-slate-900'
                  }`}
                  data-testid={`role-card-${role.id}`}
                  role="button"
                  tabIndex={0}
                  onKeyDown={e => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      handleSelectRole(role);
                    }
                  }}
                >
                  <div className="flex items-center gap-4">
                    <div
                      className={`w-10 h-10 rounded-lg flex items-center justify-center font-bold text-sm transition-colors ${
                        isSelected
                          ? 'bg-cyan-500 text-slate-950'
                          : 'bg-slate-800 text-slate-300 group-hover:bg-slate-700'
                      }`}
                    >
                      {role.title.charAt(0)}
                    </div>
                    <div>
                      <h2 className="text-base font-semibold text-white group-hover:text-cyan-300 transition-colors">
                        {role.title}
                      </h2>
                      <p className="text-xs text-slate-400 mt-0.5">
                        {role.id === 'frontend-developer' && 'User interfaces, state management & client architecture'}
                        {role.id === 'backend-developer' && 'Scalable server systems, databases, APIs & system design'}
                        {role.id === 'data-analyst' && 'Data modeling, SQL analytics, statistics & business insights'}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <span
                      className={`text-xs font-medium px-3 py-1.5 rounded-md transition-colors ${
                        isSelected
                          ? 'bg-cyan-500 text-slate-950 font-semibold'
                          : 'bg-slate-800 text-slate-300 group-hover:bg-slate-700 group-hover:text-white'
                      }`}
                    >
                      {isSelected ? 'Selected' : 'Select'}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </JourneyShell>
  );
}
