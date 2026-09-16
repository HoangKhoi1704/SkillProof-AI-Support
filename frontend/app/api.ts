import {
  Role,
  DiagnosticQuestion,
  EvaluationResponse,
  RoadmapResponse,
  SkillEvaluationItem,
  ProjectRecommendationResponse,
  RecommendProjectRequest,
  RoadmapItem,
  RoleSkillsResponse,
  DynamicQuestionSelectionResponse,
  AiRuntimeConfig,
  DevQuestionSummary,
  DevQuestionDetail,
  DevEvaluateRequest,
  AiDiagnosticTrace,
  AiDiagnosticTraceSummary,
  AdaptiveSessionResponse,
  CareerReadinessProfile,
  DevAdaptiveInspection,
  GapBasedProject,
  SubmitProjectEvidenceRequest,
  ProjectEvaluation
} from './types';

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

export async function selectDiagnosticQuestions(
  roleId: string,
  selectedSkillIds: string[],
  primaryLanguageId?: string | null
): Promise<DynamicQuestionSelectionResponse> {
  const payload = {
    roleId,
    selectedSkillIds,
    primaryLanguageId: primaryLanguageId || null
  };

  const res = await fetch(`${API_BASE_URL}/api/diagnostics/questions/select`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Question selection failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function startAdaptiveSession(
  roleId: string,
  selectedSkillIds: string[],
  primaryLanguageId?: string | null
): Promise<AdaptiveSessionResponse> {
  const payload = {
    roleId,
    selectedSkillIds,
    primaryLanguageId: primaryLanguageId || null
  };

  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Starting adaptive session failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function submitAdaptiveAnswer(
  sessionId: string,
  questionId: string,
  answer: string
): Promise<AdaptiveSessionResponse> {
  const payload = {
    questionId,
    answer
  };

  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/answers`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Submitting adaptive answer failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function getAdaptiveSession(
  sessionId: string
): Promise<AdaptiveSessionResponse> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Fetching adaptive session failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function getAdaptiveProfile(
  sessionId: string
): Promise<CareerReadinessProfile> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/profile`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Fetching adaptive profile failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function getDevAdaptiveSession(
  sessionId: string
): Promise<DevAdaptiveInspection> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/adaptive/sessions/${encodeURIComponent(sessionId)}`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Fetching dev adaptive session failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function submitDiagnosticEvaluation(
  roleId: string,
  answers: Record<string | number, string>,
  questions?: DiagnosticQuestion[]
): Promise<EvaluationResponse> {
  // Ensure all questions for the role are submitted (empty answers default to "")
  const answerSubmissions = questions && questions.length > 0
    ? questions.map(q => ({
      questionId: String(q.id),
      answer: answers[q.id] || ''
    }))
    : Object.entries(answers).map(([qId, ans]) => ({
      questionId: qId,
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
  skills: SkillEvaluationItem[],
  sessionId?: string
): Promise<RoadmapResponse> {
  const payload: Record<string, unknown> = {
    roleId,
    topGaps,
    skills: skills.map(s => ({
      name: s.name,
      level: s.level,
      reason: s.reason,
      evidence: s.evidence
    }))
  };

  if (sessionId) {
    payload.sessionId = sessionId;
  }

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

export async function generateAdaptiveRoadmap(sessionId: string): Promise<RoadmapResponse> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${sessionId}/roadmap`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Adaptive roadmap generation failed (${res.status})`;
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

export async function recommendAdaptiveProject(sessionId: string): Promise<GapBasedProject> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/project/recommend`, {
    method: 'POST',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Gap-based project recommendation failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function submitAdaptiveProjectEvidence(
  sessionId: string,
  payload: SubmitProjectEvidenceRequest
): Promise<ProjectEvaluation> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/project/submit`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Project evidence submission failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function getAdaptiveProject(sessionId: string): Promise<{ project: GapBasedProject; evaluation?: ProjectEvaluation }> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/project`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch adaptive project (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function fetchRoleSkills(roleId: string): Promise<RoleSkillsResponse> {
  const res = await fetch(`${API_BASE_URL}/api/roles/${encodeURIComponent(roleId)}/skills`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch role skills (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

// ==========================================
// Dev AI Inspector API Functions (Dev Only)
// ==========================================

export async function fetchDevRuntimeConfig(): Promise<AiRuntimeConfig> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/runtime`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch dev AI runtime config (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function fetchDevQuestions(params?: { roleId?: string; skillId?: string; difficulty?: string }): Promise<DevQuestionSummary[]> {
  const query = new URLSearchParams();
  if (params?.roleId) query.set('roleId', params.roleId);
  if (params?.skillId) query.set('skillId', params.skillId);
  if (params?.difficulty) query.set('difficulty', params.difficulty);

  const qs = query.toString() ? `?${query.toString()}` : '';
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/questions${qs}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch dev questions (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function fetchDevQuestionDetail(questionId: string): Promise<DevQuestionDetail> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/questions/${encodeURIComponent(questionId)}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch dev question detail (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function evaluateDevAnswer(req: DevEvaluateRequest): Promise<AiDiagnosticTrace> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/evaluate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify(req)
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Evaluation failed (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function fetchDevTraces(): Promise<AiDiagnosticTraceSummary[]> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/traces`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch dev traces (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

export async function fetchDevTraceDetail(traceId: string): Promise<AiDiagnosticTrace> {
  const res = await fetch(`${API_BASE_URL}/api/dev/ai/traces/${encodeURIComponent(traceId)}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch dev trace detail (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

