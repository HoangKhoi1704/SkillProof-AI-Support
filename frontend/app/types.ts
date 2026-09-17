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
  deployedUrl?: string;
  notebookUrl?: string;
  dashboardUrl?: string;
  datasetUrl?: string;
  notes?: string;
  projectSummary?: string;
  implementationExplanation?: string;
  architectureDecisions?: string;
  testingExplanation?: string;
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
  evidenceFound?: string[];
  sourceArtifact?: string;
}

export interface PortfolioProof {
  projectTitle: string;
  summary: string;
  demonstratedSkills: string[];
  portfolioBullets: string[];
  cvBullets: string[];
  evidenceNotes: string[];
  claimTraceability?: string[];
}

export interface VerificationArtifactItem {
  artifactType: string;
  source: string;
  status: string;
  details: string;
}

export interface CompositeEvidenceReport {
  overallStatus: string;
  totalArtifactsChecked: number;
  verifiedArtifactsCount: number;
  partiallyVerifiedCount: number;
  unverifiedCount: number;
  verifiedEvidence: VerificationArtifactItem[];
  partiallyVerifiedEvidence: VerificationArtifactItem[];
  unverifiedEvidence: VerificationArtifactItem[];
  securityWarnings: string[];
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
  verificationReport?: CompositeEvidenceReport;
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

export type SkillMatrixState =
  | 'Advanced'
  | 'Intermediate'
  | 'Beginner'
  | 'Insufficient Evidence'
  | 'Not Assessed';

export type SkillMatrixGapType =
  | 'ASSESSED GAP'
  | 'EVIDENCE GAP'
  | 'ROLE COVERAGE GAP'
  | 'NONE';

export interface SkillMatrixItem {
  canonicalSkillId: string;
  skillName: string;
  category: string;
  isMandatoryFundamental: boolean;
  overallStatus: 'Advanced' | 'Intermediate' | 'Beginner' | 'Insufficient Evidence' | 'Not Assessed' | string;
  fundamentalsDimension: string;
  appliedDimension: string;
  reasoningDimension: string;
  gapType: 'ASSESSED GAP' | 'EVIDENCE GAP' | 'ROLE COVERAGE GAP' | 'NONE' | string;
  evidenceObserved: string[];
  whyThisLevel: string;
  whatToImproveNext: string[];
}

export interface PostAnswerExplanation {
  questionId: string;
  skillId: string;
  skillName: string;
  evaluatedLevel: string;
  whatYouCovered: string[];
  whatCouldBeStronger: string[];
  referenceExplanation: string;
}

export interface CareerReadinessProfile {
  roleId: string;
  assessmentType: string;
  completedAt: string;
  summary: ProfileSummary;
  skills: SkillProfileItem[];
  topGaps: string[];
  roadmapInput: RoadmapHandoffContract;
  skillMatrix?: SkillMatrixItem[];
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
  lastExplanation?: PostAnswerExplanation | null;
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

// ==========================================
// V3 Canonical Catalog & Framework Types
// ==========================================

export interface RoleSummaryV3 {
  id: string;
  title: string;
  description: string | null;
  isPrimaryDemoRole: boolean;
  roadmapSourceUrl: string | null;
  displayOrder: number;
}

export interface CanonicalSkillNodeV3 {
  canonicalSkillId: string;
  displayName: string;
  classification: string;
  category: string;
  importance: string;
  sourceKind: string;
  roadmapSource: string | null;
  roadmapNodeId: string | null;
  roadmapLabel: string | null;
  description: string | null;
  assessmentEligible: boolean;
  mandatoryFundamental: boolean;
  isToolkitOnly: boolean;
  isOptional: boolean;
  hasQuestionCoverage: boolean;
  displayOrder: number;
}

export interface RoadmapRelationshipV3 {
  id: number;
  sourceSkillId: string;
  targetSkillId: string;
  relationshipType: string;
  rationale: string | null;
}

export interface RoleCanonicalFrameworkV3 {
  roleId: string;
  roleTitle: string;
  description: string | null;
  isPrimaryDemoRole: boolean;
  roadmapSourceUrl: string | null;
  nodes: CanonicalSkillNodeV3[];
  relationships: RoadmapRelationshipV3[];
}

export type RoadmapNodeState =
  | 'Completed'
  | 'Current'
  | 'Available'
  | 'Locked'
  | 'NeedsDevelopment'
  | 'NotAssessed'
  | 'Optional';

export interface PersonalizedRoadmapNode {
  canonicalSkillId: string;
  name: string;
  classification: string;
  requirement: string;
  nodeState: RoadmapNodeState;
  gapType: string;
  assessmentState: string;
  whyThisNode: string;
  nextAction: string;
  displayOrder: number;
  isToolkit: boolean;
  isOptional: boolean;
  category: string;
  importance: string;
  prerequisiteSkillIds: string[];
}

export interface PersonalizedRoadmapEdge {
  from: string;
  to: string;
  relationshipType: string;
  rationale?: string | null;
}

export interface RoadmapGraphSummary {
  currentNodeIds: string[];
  completedCount: number;
  needsDevelopmentCount: number;
  notAssessedCount: number;
  availableCount: number;
  lockedCount: number;
  optionalCount: number;
  totalNodes: number;
}

export interface PersonalizedRoadmapGraph {
  roleId: string;
  roleTitle: string;
  sessionId?: string | null;
  nodes: PersonalizedRoadmapNode[];
  edges: PersonalizedRoadmapEdge[];
  summary: RoadmapGraphSummary;
}

export interface LearningResourceDto {
  id: string;
  title: string;
  sourceName: string;
  sourceUrl: string;
  resourceType: 'official-doc' | 'guide' | 'tutorial' | 'interactive-practice' | 'reference' | string;
  canonicalSkillIds: string[];
  roleIds: string[];
  level: 'foundation' | 'applied' | 'advanced' | string;
  isOfficial: boolean;
  verificationStatus: string;
  verifiedAt: string;
  locator?: string | null;
  relevanceReason?: string | null;
}

export interface NodeResourcesResponse {
  canonicalSkillId: string;
  resources: LearningResourceDto[];
}

export interface CuratedProjectDto {
  id: string;
  title: string;
  source: string;
  sourceUrl?: string | null;
  sourceLocator?: string | null;
  provenance: string;
  roleIds: string[];
  canonicalSkillIds: string[];
  roadmapTargets: string[];
  projectType: 'practice' | 'portfolio';
  difficulty: string;
  estimatedScope: string;
  description: string;
  deliverables: string[];
  evidenceRequirements: string[];
  verificationStatus: string;
  verifiedAt: string;
  targetedGaps: string[];
  whyRecommended: string;
}

export interface CuratedProjectRecommendationsResponse {
  roleId: string;
  practiceProjects: CuratedProjectDto[];
  portfolioProjects: CuratedProjectDto[];
}



