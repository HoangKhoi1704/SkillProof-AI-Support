export interface Role {
  id: string;
  name: string;
  description: string;
}

export interface DiagnosticQuestion {
  id: number;
  careerRoleId: string;
  competency: string;
  type: string;
  questionText: string;
  sourceType: string;
  sourceReference: string;
}

export type AnswerMap = Record<number, string>;

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
}

export interface RoadmapResponse {
  roleId: string;
  items: RoadmapItem[];
}

export interface ProjectRequirement {
  requirement: string;
  targetsSkill: string;
  deliverable?: string;
}

export interface ProjectRecommendationResponse {
  title: string;
  description: string;
  reason: string;
  requirements: ProjectRequirement[];
  expectedDeliverables?: string[];
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
