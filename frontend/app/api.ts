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
  ProjectEvaluation,
  RoleSummaryV3,
  RoleCanonicalFrameworkV3,
  PersonalizedRoadmapGraph,
  NodeResourcesResponse,
  CuratedProjectDto,
  CuratedProjectRecommendationsResponse
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

export async function advanceAdaptiveSession(
  sessionId: string
): Promise<AdaptiveSessionResponse> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/next`, {
    method: 'POST',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Advancing adaptive session failed (${res.status})`;
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

export async function getCanonicalRoadmap(sessionId: string): Promise<PersonalizedRoadmapGraph> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/canonical-roadmap`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Fetching canonical roadmap failed (${res.status})`;
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

// ==========================================
// V3 Canonical Catalog API Functions & Adapters
// ==========================================

export async function fetchV3Roles(): Promise<RoleSummaryV3[]> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/v3/roles`, {
      method: 'GET',
      headers: { 'Accept': 'application/json' },
      cache: 'no-store'
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch V3 roles: ${res.status} ${res.statusText}`);
    }
    return await res.json();
  } catch (err) {
    console.warn('V3 roles API unavailable, using primary demo fallback:', err);
    return [
      {
        id: 'frontend-developer',
        title: 'Frontend Developer',
        description: 'Architects, develops, and optimizes user-facing client applications.',
        isPrimaryDemoRole: true,
        roadmapSourceUrl: 'https://roadmap.sh/frontend',
        displayOrder: 1
      },
      {
        id: 'backend-developer',
        title: 'Backend Developer',
        description: 'Designs, builds, tests, and maintains scalable server-side systems and APIs.',
        isPrimaryDemoRole: true,
        roadmapSourceUrl: 'https://roadmap.sh/backend',
        displayOrder: 2
      },
      {
        id: 'data-analyst',
        title: 'Data Analyst',
        description: 'Collects, cleans, transforms, models, and visualizes complex datasets.',
        isPrimaryDemoRole: true,
        roadmapSourceUrl: 'https://roadmap.sh/data-analyst',
        displayOrder: 3
      }
    ];
  }
}

export async function fetchRoleCanonicalFramework(roleId: string): Promise<RoleCanonicalFrameworkV3> {
  const res = await fetch(`${API_BASE_URL}/api/roles/${encodeURIComponent(roleId)}/canonical-framework`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch canonical framework for role '${roleId}' (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Adapter mapping V3 canonical skill IDs to legacy backend assessment skill IDs.
 * Preserves exact compatibility with the 48-question backend adaptive engine.
 */
const CANONICAL_TO_BACKEND_LEGACY_MAP: Record<string, string> = {
  'backend.rest-apis': 'rest-api',
  'shared.sql': 'sql',
  'backend.relational-databases': 'sql',
  'backend.testing': 'testing',
  'backend.authentication-security': 'authentication-security',
  'backend.system-design': 'system-design',
  'backend.nosql-databases': 'nosql',
  'backend.caching': 'caching',
  'shared.git': 'git',
  'shared.docker': 'docker',
  'backend.ci-cd': 'cicd',
  'backend.concurrency': 'concurrency',
  'backend.message-brokers': 'messaging',
  'backend.observability': 'observability',
  'assessment-ext.programming-fundamentals': 'programming-fundamentals',
  'backend.lang-csharp': 'csharp',
  'backend.lang-java': 'java',
  'shared.python': 'python',
  'backend.lang-cpp': 'cpp',
  'shared.javascript': 'javascript',
  'backend.lang-typescript': 'typescript',
  'backend.lang-go': 'go',
  'backend.lang-rust': 'rust'
};

export function mapCanonicalSkillsToBackendAssessment(roleId: string, canonicalSkillIds: string[]): string[] {
  if (roleId !== 'backend-developer') {
    return canonicalSkillIds;
  }

  const mapped = new Set<string>();
  for (const id of canonicalSkillIds) {
    if (CANONICAL_TO_BACKEND_LEGACY_MAP[id]) {
      mapped.add(CANONICAL_TO_BACKEND_LEGACY_MAP[id]);
    } else {
      mapped.add(id);
    }
  }
  return Array.from(mapped);
}

/**
 * Fetch verified canonical learning resources for a roadmap competency node.
 */
export async function getNodeResources(
  nodeId: string,
  roleId?: string,
  nodeState?: string,
  gapType?: string
): Promise<NodeResourcesResponse> {
  const params = new URLSearchParams();
  if (roleId) params.set('roleId', roleId);
  if (nodeState) params.set('nodeState', nodeState);
  if (gapType) params.set('gapType', gapType);

  const query = params.toString() ? `?${params.toString()}` : '';
  const res = await fetch(`${API_BASE_URL}/api/v3/roadmap/nodes/${encodeURIComponent(nodeId)}/resources${query}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch resources for node '${nodeId}' (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Fetch curated practice and portfolio projects matched deterministically from trusted session gaps.
 */
export async function getCuratedProjects(
  sessionId: string
): Promise<CuratedProjectRecommendationsResponse> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/projects`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch curated projects (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Select a curated project into the session for evidence verification.
 */
export async function selectCuratedProject(
  sessionId: string,
  projectId: string
): Promise<GapBasedProject> {
  const res = await fetch(`${API_BASE_URL}/api/diagnostics/adaptive/sessions/${encodeURIComponent(sessionId)}/projects/${encodeURIComponent(projectId)}/select`, {
    method: 'POST',
    headers: { 'Accept': 'application/json' }
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to select curated project (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Fetch curated project specification by ID.
 */
export async function getCuratedProjectDetail(
  projectId: string
): Promise<CuratedProjectDto> {
  const res = await fetch(`${API_BASE_URL}/api/v3/projects/${encodeURIComponent(projectId)}`, {
    method: 'GET',
    headers: { 'Accept': 'application/json' },
    cache: 'no-store'
  });

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    const message = errorBody?.error?.message || `Failed to fetch project detail for '${projectId}' (${res.status})`;
    throw new Error(message);
  }

  return await res.json();
}



