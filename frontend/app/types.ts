export interface Role {
  id: string;
  name: string;
  description: string;
}

export interface DiagnosticQuestion {
  id: number | string;
  careerRoleId?: string;
  competencyId?: string;
  competency: string;
  difficulty?: string;
  type?: string;
  questionType?: string;
  questionText: string;
  sourceType?: string;
  sourceReference?: string;
}

export type AnswerMap = Record<string | number, string>;

export interface SelectedQuestionDto {
  id: string;
  competencyId: string;
  competency: string;
  difficulty: string;
  questionType: string;
  questionText: string;
  question: string;
}

export interface AssessmentCoverageDto {
  requestedSkillIds: string[];
  assessedSkillIds: string[];
  unsupportedSkillIds: string[];
}

export interface DynamicQuestionSelectionResponse {
  roleId: string;
  questions: SelectedQuestionDto[];
  assessmentCoverage: AssessmentCoverageDto;
}

export type QualitativeSkillLevel =
  | 'Beginner'
  | 'Intermediate'
  | 'Advanced'
  | 'Insufficient Evidence';

export interface SkillEvaluationItem {
  name: string;
  level: QualitativeSkillLevel;
  reason: string;
  evidence: string[];
}

export interface EvaluationResponse {
  roleId: string;
  skills: SkillEvaluationItem[];
  topGaps: string[];
}

export interface RoadmapItem {
  skill: string;
  priority: number;
  learningGoal: string;
  practiceTask: string;
  skillId?: string;
  currentLevel?: string;
  gapType?: string;
  targetArea?: string;
  whyThisMatters?: string;
  learningActions?: string[];
  evidenceTarget?: string;
  completionCriteria?: string[];
}

export interface ProjectGapTargetSkill {
  skillId: string;
  currentLevel: string;
  targetArea: string;
  practiceTask: string;
  evidenceTarget: string;
}

export interface ProjectGapContext {
  roleId: string;
  targetSkills: ProjectGapTargetSkill[];
}

export interface RoadmapResponse {
  roleId: string;
  items: RoadmapItem[];
  generatedAt?: string;
  sessionId?: string;
  projectContext?: ProjectGapContext;
}

export interface ProjectRequirement {
  requirement: string;
  targetsSkill: string;
  deliverable?: string;
  skillId?: string;
}

export interface ProjectRecommendationResponse {
  title: string;
  description: string;
  reason: string;
  requirements: ProjectRequirement[];
  expectedDeliverables?: string[];
}

export interface TargetedSkill {
  skillId: string;
  currentLevel: string;
  targetArea: string;
  whyIncluded: string;
}

export interface GapBasedProject {
  projectId: string;
  roleId: string;
  title: string;
  scenario: string;
  objective: string;
  targetedSkills: TargetedSkill[];
  requirements: ProjectRequirement[];
  deliverables: string[];
  evidenceRequirements: string[];
  evaluationCriteria: string[];
  portfolioOutcome: string;
}

export interface SubmitProjectEvidenceRequest {
  repositoryUrl?: string;
  projectSummary: string;
  implementationExplanation: string;
  architectureDecisions: string;
  testingExplanation: string;
  evidenceExcerpts?: string[];
}

export interface SkillEvidenceResult {
  skillId: string;
  evidenceStatus: 'Demonstrated' | 'Partially Demonstrated' | 'Insufficient Evidence';
  evidence: string[];
  missingEvidence: string[];
}

export interface RequirementEvaluationResult {
  requirement: string;
  targetsSkill: string;
  status: 'Demonstrated' | 'Partially Demonstrated' | 'Insufficient Evidence';
  evaluationNotes: string;
}

export interface PortfolioProof {
  projectTitle: string;
  summary: string;
  demonstratedSkills: string[];
  portfolioBullets: string[];
  cvBullets: string[];
  evidenceNotes: string[];
}

export interface ProjectEvaluation {
  projectId: string;
  overallStatus: 'Demonstrated' | 'Partially Demonstrated' | 'Insufficient Evidence';
  skillEvidence: SkillEvidenceResult[];
  requirementResults: RequirementEvaluationResult[];
  demonstratedEvidence: string[];
  missingEvidence: string[];
  improvementSuggestions: string[];
  portfolioProof?: PortfolioProof;
}

export interface RecommendProjectRequest {
  roleId: string;
  topGaps: string[];
  roadmap?: {
    skill: string;
    priority?: number;
    learningGoal?: string;
  }[];
}

export interface PublicSubskill {
  id: string;
  name: string;
  description?: string | null;
}

export interface PublicSkill {
  id: string;
  name: string;
  category: 'core' | 'recommended' | 'optional' | 'language' | string;
  skillType: 'competency' | 'language' | 'tool' | string;
  description?: string | null;
  subskills: PublicSubskill[];
}

export interface RoleSkillsResponse {
  roleId: string;
  core: PublicSkill[];
  recommended: PublicSkill[];
  optional: PublicSkill[];
  languages: PublicSkill[];
}

export interface AssessmentSelection {
  roleId: string;
  selectedSkillIds: string[];
  primaryLanguageId: string;
  mode: 'recommended' | 'custom';
}

export interface AiRuntimeConfig {
  provider: string;
  model: string;
  liveEvaluationEnabled: boolean;
  credentialsConfigured: boolean;
  liveAiAvailable: boolean;
  activeEvaluator: string;
  environment: string;
  statusMessage: string;
}

export interface DevQuestionSummary {
  id: string;
  roleId: string;
  skillId: string;
  difficulty: string;
  questionType: string;
  questionText: string;
  verificationStatus: string;
}

export interface DevRubric {
  insufficientEvidence: string;
  beginner: string;
  intermediate: string;
  advanced: string;
}

export interface DevSourceRef {
  sourceId: string;
  title: string;
  publisher: string;
  sourceType: string;
  url: string;
}

export interface DevInterviewEvidence {
  sourceId: string;
  evidenceType: string;
  quotedSignals: string[];
  typicalQuestions: string[];
}

export interface DevQuestionDetail {
  id: string;
  roleId: string;
  skillId: string;
  difficulty: string;
  questionType: string;
  questionText: string;
  subskills: string[];
  verificationStatus: string;
  provenance: string;
  expectedSignals: string[];
  rubric: DevRubric;
  frameworkSources: DevSourceRef[];
  interviewEvidence: DevInterviewEvidence[];
}

export interface DevEvaluateRequest {
  questionId: string;
  candidateAnswer: string;
  mode: 'deterministic' | 'live';
}

export interface AiDiagnosticTrace {
  traceId: string;
  timestamp: string;
  mode: string;
  evaluator: string;
  durationMs: number;
  runtime: AiRuntimeConfig;
  question: {
    id: string;
    roleId: string;
    skillId: string;
    difficulty: string;
    questionType: string;
    questionText: string;
    subskills: string[];
  };
  evaluationContext: {
    expectedSignals: string[];
    rubric: DevRubric;
    frameworkSources: DevSourceRef[];
    interviewEvidence: DevInterviewEvidence[];
  };
  candidateAnswer: string;
  prompt: {
    systemInstructions: string;
    evaluationInstructions: string;
  };
  modelResponse: {
    rawResponse?: string | null;
    parsedResponse?: any;
  };
  validation: {
    schemaValid: boolean;
    levelValid: boolean;
    evidenceValid: boolean;
    questionIdMatched: boolean;
    rubricResolved: boolean;
    fallbackUsed: boolean;
    validationMessages: string[];
  };
  finalResult: {
    skill: string;
    level: string;
    reason: string;
    evidence: string[];
  };
  timing?: {
    durationMs: number;
  };
}

export interface AiDiagnosticTraceSummary {
  traceId: string;
  timestamp: string;
  questionId: string;
  skillId: string;
  mode: string;
  evaluator: string;
  level: string;
  durationMs: number;
}

export interface AdaptivePublicQuestion {
  id: string;
  skillId: string;
  skillName: string;
  difficulty: string;
  questionType: string;
  questionText: string;
}

export interface AdaptiveProgress {
  currentSkillIndex: number;
  totalSkills: number;
  currentSkillId: string;
  currentSkillName: string;
  stage: 'applied' | 'follow-up' | string;
  completedSkills: number;
  totalAnswered: number;
}

export interface AdaptiveSkillResult {
  skillId: string;
  skillName: string;
  finalLevel: QualitativeSkillLevel;
  reason: string;
  evidence: string[];
  questionsAnswered: number;
  branch: string;
  appliedDifficulty: string;
  appliedLevel: string;
  followUpDifficulty: string;
  followUpLevel: string;
}

export interface ProfileSummary {
  intermediateCount: number;
  beginnerCount: number;
  advancedCount: number;
  insufficientEvidenceCount: number;
  totalSkillsAssessed: number;
}

export interface SkillProfileItem {
  skillId: string;
  skillName: string;
  finalLevel: QualitativeSkillLevel;
  evidence: string[];
  reasoning: string[];
  demonstratedStrengths: string[];
  evidenceGaps: string[];
  nextDevelopmentAreas: string[];
  questionsAnswered: string[];
}

export interface RoadmapSkillGapInput {
  skillId: string;
  skillName: string;
  currentLevel: string;
  developmentAreas: string[];
  evidenceGaps: string[];
}

export interface RoadmapHandoffContract {
  roleId: string;
  skillGaps: RoadmapSkillGapInput[];
}

export interface CareerReadinessProfile {
  roleId: string;
  assessmentType: string;
  completedAt: string;
  summary: ProfileSummary;
  skills: SkillProfileItem[];
  topGaps: string[];
  roadmapInput: RoadmapHandoffContract;
}

export interface AdaptiveSessionResponse {
  sessionId: string;
  roleId: string;
  status: 'in-progress' | 'completed' | string;
  currentQuestion?: AdaptivePublicQuestion | null;
  progress?: AdaptiveProgress | null;
  skills?: AdaptiveSkillResult[] | null;
  topGaps?: string[] | null;
  unsupportedSkillIds: string[];
  profile?: CareerReadinessProfile | null;
}

export interface DevAiEvaluationDetail {
  stage: string;
  questionId: string;
  difficulty: string;
  evaluator: string;
  provisionalLevel: string;
  reason: string;
  observedEvidence: string[];
}

export interface DevAdaptiveBranchDetail {
  branchChosen: string;
  ruleApplied: string;
  targetDifficulty: string;
}

export interface DevBackendDerivedProfile {
  finalAggregatedLevel: string;
  aggregationRule: string;
  demonstratedStrengths: string[];
  evidenceGaps: string[];
  nextDevelopmentAreas: string[];
}

export interface DevAdaptiveSkillInspection {
  skillId: string;
  skillName: string;
  stage1Evaluation: DevAiEvaluationDetail;
  branchDecision: DevAdaptiveBranchDetail;
  stage2Evaluation?: DevAiEvaluationDetail | null;
  backendDerivedProfile?: DevBackendDerivedProfile | null;
}

export interface DevRoadmapInspection {
  sessionId: string;
  roleId: string;
  provider: string;
  validationSucceeded: boolean;
  rawModelOutput?: string | null;
  trustedHandoffInput: RoadmapHandoffContract;
  finalRoadmapItems: RoadmapItem[];
}

export interface DevProjectInspection {
  projectId: string;
  roleId: string;
  provider: string;
  validationSucceeded: boolean;
  trustedGapContext: ProjectGapContext;
  finalProject: GapBasedProject;
  evaluation?: {
    provider: string;
    validationSucceeded: boolean;
    submittedEvidence: SubmitProjectEvidenceRequest;
    predefinedCriteria: string[];
    overallStatus: string;
    skillEvidence: SkillEvidenceResult[];
  } | null;
  portfolioProof?: PortfolioProof | null;
}

export interface DevAdaptiveInspection {
  sessionId: string;
  roleId: string;
  status: string;
  skills: DevAdaptiveSkillInspection[];
  summary?: ProfileSummary | null;
  topGaps?: string[] | null;
  roadmap?: DevRoadmapInspection | null;
  project?: DevProjectInspection | null;
}

