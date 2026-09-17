import type {
  AdaptiveSessionResponse,
  CareerReadinessProfile,
  EvaluationResponse,
  GapBasedProject,
  ProjectEvaluation,
  ProjectRecommendationResponse,
  RoadmapResponse,
  SubmitProjectEvidenceRequest
} from '../../types';

export const CURRENT_JOURNEY_SCHEMA_VERSION = 2;
export const JOURNEY_TTL_MS = 30 * 60 * 1000; // 30 minutes in milliseconds

export interface JourneyState {
  schemaVersion: number;
  createdAt: number;
  updatedAt: number;
  expiresAt: number;

  selectedRoleId: string | null;
  selectedRoleTitle: string | null;

  userSelectedSkillIds: string[];
  roleMandatoryFundamentalIds: string[];

  primaryLanguageId: string | null;

  adaptiveSession: AdaptiveSessionResponse | null;
  adaptiveSessionId: string | null;
  currentQuestionIndex: number;
  answers: Record<string, string>;

  evaluationResult: EvaluationResponse | null;
  careerProfile: CareerReadinessProfile | null;

  roadmapResult: RoadmapResponse | null;
  selectedRoadmapNodeId: string | null;
  projectResult: ProjectRecommendationResponse | null;
  adaptiveProjectResult: GapBasedProject | null;
  projectEvidenceForm: SubmitProjectEvidenceRequest | null;
  projectEvaluationResult: ProjectEvaluation | null;
}

export interface IJourneyStateRepository {
  getState(): JourneyState;
  saveState(updates: Partial<JourneyState>): JourneyState;
  clearState(): void;
  isExpired(): boolean;
  touch(): void;

  selectRole(roleId: string, roleTitle?: string): JourneyState;
  setSelectedSkills(skillIds: string[], mandatoryIds?: string[]): JourneyState;
  setPrimaryLanguage(languageId: string): JourneyState;
  setAdaptiveSession(session: AdaptiveSessionResponse): JourneyState;
  setEvaluation(evaluation: EvaluationResponse, profile?: CareerReadinessProfile): JourneyState;
  setRoadmap(roadmap: RoadmapResponse): JourneyState;
  setSelectedRoadmapNode(nodeId: string | null): JourneyState;
  setProject(project: ProjectRecommendationResponse | GapBasedProject): JourneyState;
  setProjectEvaluation(evalResult: ProjectEvaluation): JourneyState;
  resetJourney(): void;
}
