import { Role, DiagnosticQuestion, EvaluationResponse, RoadmapResponse, SkillEvaluationItem, ProjectRecommendationResponse, RecommendProjectRequest, RoadmapItem } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5068';

const FALLBACK_ROLES: Role[] = [
  {
    id: 'backend-developer',
    name: 'Backend Developer',
    description: 'Designs, builds, and maintains server-side logic, APIs, and databases.'
  },
  {
    id: 'financial-analyst',
    name: 'Financial Analyst',
    description: 'Analyzes financial data, builds financial models, and forecasts business performance.'
  }
];

export async function fetchRoles(): Promise<Role[]> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/roles`, {
      method: 'GET',
      headers: { 'Accept': 'application/json' },
      cache: 'no-store'
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch roles: ${res.status} ${res.statusText}`);
    }
    return await res.json();
  } catch (err) {
    console.warn('API unavailable, using fallback roles:', err);
    return FALLBACK_ROLES;
  }
}

export async function fetchDiagnosticQuestions(roleId: string): Promise<DiagnosticQuestion[]> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/questions?roleId=${encodeURIComponent(roleId)}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch questions (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function submitDiagnosticEvaluation(
  roleId: string,
  answers: Record<number, string>,
  questions?: DiagnosticQuestion[]
): Promise<EvaluationResponse> {
  // Ensure all questions for the role are submitted (empty answers default to "")
  const answerSubmissions = questions && questions.length > 0
    ? questions.map(q => ({
      questionId: q.id,
      answer: answers[q.id] || ''
    }))
    : Object.entries(answers).map(([qId, ans]) => ({
      questionId: parseInt(qId, 10),
      answer: ans
    }));

  const payload = {
    roleId,
    answers: answerSubmissions
  };

  const res = await fetch(`${API_BASE_URL}/api/diagnostics/evaluate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Evaluation request failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function generateRoadmap(
  roleId: string,
  topGaps: string[],
  skills: SkillEvaluationItem[]
): Promise<RoadmapResponse> {
  const payload = {
    roleId,
    topGaps,
    skills: skills.map(s => ({
      name: s.name,
      level: s.level,
      reason: s.reason,
      evidence: s.evidence
    }))
  };

  const res = await fetch(`${API_BASE_URL}/api/roadmaps/generate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Roadmap generation failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function recommendProject(
  roleId: string,
  topGaps: string[],
  roadmap?: RoadmapItem[]
): Promise<ProjectRecommendationResponse> {
  const payload: RecommendProjectRequest = {
    roleId,
    topGaps,
    roadmap: roadmap?.map(r => ({
      skill: r.skill,
      priority: r.priority,
      learningGoal: r.learningGoal
    }))
  };

  const res = await fetch(`${API_BASE_URL}/api/projects/recommend`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Project recommendation failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

