import {
  CURRENT_JOURNEY_SCHEMA_VERSION,
  IJourneyStateRepository,
  JOURNEY_TTL_MS,
  JourneyState
} from './types';
import type {
  AdaptiveSessionResponse,
  CareerReadinessProfile,
  EvaluationResponse,
  GapBasedProject,
  ProjectEvaluation,
  ProjectRecommendationResponse,
  RoadmapResponse
} from '../../types';

export const STORAGE_KEY = 'skillproof_journey_v2';

export function createInitialJourneyState(): JourneyState {
  const now = Date.now();
  return {
    schemaVersion: CURRENT_JOURNEY_SCHEMA_VERSION,
    createdAt: now,
    updatedAt: now,
    expiresAt: now + JOURNEY_TTL_MS,

    selectedRoleId: null,
    selectedRoleTitle: null,

    userSelectedSkillIds: [],
    roleMandatoryFundamentalIds: [],

    primaryLanguageId: null,

    adaptiveSession: null,
    adaptiveSessionId: null,
    currentQuestionIndex: 0,
    answers: {},

    evaluationResult: null,
    careerProfile: null,

    roadmapResult: null,
    selectedRoadmapNodeId: null,
    projectResult: null,
    adaptiveProjectResult: null,
    projectEvidenceForm: null,
    projectEvaluationResult: null
  };
}

export class SessionStorageJourneyStateRepository implements IJourneyStateRepository {
  private inMemoryFallback: JourneyState = createInitialJourneyState();

  private isStorageAvailable(): boolean {
    return typeof window !== 'undefined';
  }

  private getStorage(): Storage | null {
    if (typeof window === 'undefined') return null;
    try {
      if (typeof window.localStorage !== 'undefined') {
        return window.localStorage;
      }
    } catch {
      // Fallback
    }
    try {
      if (typeof window.sessionStorage !== 'undefined') {
        return window.sessionStorage;
      }
    } catch {
      // Fallback
    }
    return null;
  }

  public getState(): JourneyState {
    const storage = this.getStorage();
    if (!storage) {
      return this.inMemoryFallback;
    }

    try {
      let raw = storage.getItem(STORAGE_KEY);
      // If not in localStorage yet, check sessionStorage for backwards compatibility
      if (!raw && typeof window !== 'undefined' && window.sessionStorage) {
        raw = window.sessionStorage.getItem(STORAGE_KEY);
        if (raw) {
          try {
            storage.setItem(STORAGE_KEY, raw);
          } catch {
            // ignore
          }
        }
      }

      if (!raw) {
        return createInitialJourneyState();
      }

      const parsed: JourneyState = JSON.parse(raw);

      // Check schema version
      if (parsed.schemaVersion !== CURRENT_JOURNEY_SCHEMA_VERSION) {
        console.warn('Journey state schema mismatch. Clearing stale state.');
        this.clearState();
        return createInitialJourneyState();
      }

      // Check expiration (2-hour sliding TTL)
      const now = Date.now();
      if (parsed.expiresAt && now > parsed.expiresAt) {
        console.info('Journey state has expired (>2 hours). Resetting.');
        this.clearState();
        return createInitialJourneyState();
      }

      return parsed;
    } catch (err) {
      console.error('Failed to parse journey state from storage:', err);
      this.clearState();
      return createInitialJourneyState();
    }
  }

  public saveState(updates: Partial<JourneyState>): JourneyState {
    const current = this.getState();
    const now = Date.now();

    const merged: JourneyState = {
      ...current,
      ...updates,
      schemaVersion: CURRENT_JOURNEY_SCHEMA_VERSION,
      updatedAt: now,
      expiresAt: now + JOURNEY_TTL_MS // Sliding expiration: 2 hours from last interaction
    };

    const storage = this.getStorage();
    if (!storage) {
      this.inMemoryFallback = merged;
      return merged;
    }

    try {
      storage.setItem(STORAGE_KEY, JSON.stringify(merged));
      // Also sync to sessionStorage if available
      if (typeof window !== 'undefined' && window.sessionStorage && storage !== window.sessionStorage) {
        try {
          window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(merged));
        } catch {
          // ignore
        }
      }
      // Dispatch custom event for reactive tabs/components
      window.dispatchEvent(new CustomEvent('skillproof_journey_updated', { detail: merged }));
    } catch (err) {
      console.error('Failed to persist journey state to storage:', err);
    }

    return merged;
  }

  public clearState(): void {
    this.inMemoryFallback = createInitialJourneyState();
    const storage = this.getStorage();
    if (storage) {
      try {
        storage.removeItem(STORAGE_KEY);
      } catch (err) {
        console.error('Failed to clear journey state from storage:', err);
      }
    }
    if (typeof window !== 'undefined' && window.sessionStorage) {
      try {
        window.sessionStorage.removeItem(STORAGE_KEY);
      } catch {
        // ignore
      }
    }
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent('skillproof_journey_cleared'));
    }
  }

  public isExpired(): boolean {
    const state = this.getState();
    if (!state.selectedRoleId) return false;
    return Date.now() > state.expiresAt;
  }

  public touch(): void {
    const current = this.getState();
    if (current.selectedRoleId) {
      this.saveState({});
    }
  }

  public selectRole(roleId: string, roleTitle?: string): JourneyState {
    const current = this.getState();
    // If role changed, invalidate all downstream state
    if (current.selectedRoleId !== roleId) {
      return this.saveState({
        selectedRoleId: roleId,
        selectedRoleTitle: roleTitle || roleId,
        userSelectedSkillIds: [],
        roleMandatoryFundamentalIds: [],
        primaryLanguageId: null,
        adaptiveSession: null,
        adaptiveSessionId: null,
        currentQuestionIndex: 0,
        answers: {},
        evaluationResult: null,
        careerProfile: null,
        roadmapResult: null,
        projectResult: null,
        adaptiveProjectResult: null,
        projectEvidenceForm: null,
        projectEvaluationResult: null
      });
    }

    return this.saveState({
      selectedRoleTitle: roleTitle || current.selectedRoleTitle
    });
  }

  public setSelectedSkills(skillIds: string[], mandatoryIds: string[] = []): JourneyState {
    const current = this.getState();
    // Compare skills set
    const sameSkills =
      current.userSelectedSkillIds.length === skillIds.length &&
      skillIds.every(id => current.userSelectedSkillIds.includes(id));

    if (!sameSkills) {
      // Invalidate downstream assessment if skills changed
      return this.saveState({
        userSelectedSkillIds: skillIds,
        roleMandatoryFundamentalIds: mandatoryIds,
        adaptiveSession: null,
        adaptiveSessionId: null,
        currentQuestionIndex: 0,
        answers: {},
        evaluationResult: null,
        careerProfile: null,
        roadmapResult: null,
        projectResult: null,
        adaptiveProjectResult: null,
        projectEvidenceForm: null,
        projectEvaluationResult: null
      });
    }

    return this.saveState({
      roleMandatoryFundamentalIds: mandatoryIds
    });
  }

  public setPrimaryLanguage(languageId: string): JourneyState {
    return this.saveState({ primaryLanguageId: languageId });
  }

  public setAdaptiveSession(session: AdaptiveSessionResponse): JourneyState {
    return this.saveState({
      adaptiveSession: session,
      adaptiveSessionId: session.sessionId,
      currentQuestionIndex: session.progress?.totalAnswered || 0
    });
  }

  public setEvaluation(evaluation: EvaluationResponse, profile?: CareerReadinessProfile): JourneyState {
    return this.saveState({
      evaluationResult: evaluation,
      careerProfile: profile || null
    });
  }

  public setRoadmap(roadmap: RoadmapResponse): JourneyState {
    return this.saveState({ roadmapResult: roadmap });
  }

  public setSelectedRoadmapNode(nodeId: string | null): JourneyState {
    return this.saveState({ selectedRoadmapNodeId: nodeId });
  }

  public setProject(project: ProjectRecommendationResponse | GapBasedProject): JourneyState {
    if ('role' in project) {
      return this.saveState({ projectResult: project as ProjectRecommendationResponse });
    }
    return this.saveState({ adaptiveProjectResult: project as GapBasedProject });
  }

  public setProjectEvaluation(evalResult: ProjectEvaluation): JourneyState {
    return this.saveState({ projectEvaluationResult: evalResult });
  }

  public resetJourney(): void {
    this.clearState();
  }
}

// Singleton instance
export const journeyStateRepository: IJourneyStateRepository = new SessionStorageJourneyStateRepository();
