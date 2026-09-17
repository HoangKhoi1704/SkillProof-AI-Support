import fs from 'fs';
import path from 'path';

console.log('==================================================');
console.log('PASS: SkillProof Data V3 Catalog Validator Starting');
console.log('==================================================');

const errors = [];
const warnings = [];

// Helper loader
function loadJson(relPath) {
  const p = path.resolve(relPath);
  if (!fs.existsSync(p)) {
    errors.push(`Missing required file: ${relPath}`);
    return null;
  }
  return JSON.parse(fs.readFileSync(p, 'utf8'));
}

const rolesData = loadJson('data/v3/roles.json');
const skillsData = loadJson('data/v3/canonical-skills.json');
const roleNodesData = loadJson('data/v3/role-roadmap-nodes.json');
const relationshipsData = loadJson('data/v3/roadmap-relationships.json');
const legacyMappingsData = loadJson('data/v3/legacy-mappings.json');
const sourcesData = loadJson('data/v3/sources.json');
const backendQuestionsData = loadJson('data/backend/questions.json');

if (!rolesData || !skillsData || !roleNodesData || !relationshipsData || !legacyMappingsData || !sourcesData || !backendQuestionsData) {
  console.error('Fatal: Could not load all V3 data files.');
  process.exit(1);
}

// -------------------------------------------------------------
// 1. Validate Roles
// -------------------------------------------------------------
const primaryDemoRoles = rolesData.roles.filter(r => r.isPrimaryDemoRole);
if (primaryDemoRoles.length !== 3) {
  errors.push(`Expected exactly 3 primary demo roles, found ${primaryDemoRoles.length}.`);
}

const expectedPrimaryRoleIds = new Set(['frontend-developer', 'backend-developer', 'data-analyst']);
const allRoleIds = new Set(rolesData.roles.map(r => r.id));

for (const expId of expectedPrimaryRoleIds) {
  if (!allRoleIds.has(expId)) {
    errors.push(`Missing expected primary role: '${expId}'.`);
  }
}

// -------------------------------------------------------------
// 2. Validate Canonical Skills
// -------------------------------------------------------------
const allowedClassifications = new Set([
  'assessable-competency',
  'language',
  'technology-tool',
  'framework-library',
  'learning-topic',
  'career-toolkit',
  'optional-advanced',
  'assessment-policy-extension'
]);

const allowedSourceKinds = new Set([
  'roadmap-derived',
  'assessment-policy',
  'skillproof-toolkit'
]);

const canonicalSkillIds = new Set();
const canonicalSkillsById = new Map();

skillsData.canonicalSkills.forEach((s, idx) => {
  if (!s.id || typeof s.id !== 'string') {
    errors.push(`[Skill index ${idx}] Missing or invalid id.`);
    return;
  }
  if (canonicalSkillIds.has(s.id)) {
    errors.push(`Duplicate canonical skill ID: '${s.id}'.`);
  }
  canonicalSkillIds.add(s.id);
  canonicalSkillsById.set(s.id, s);

  if (!s.displayName || typeof s.displayName !== 'string') {
    errors.push(`[Skill ${s.id}] Missing displayName.`);
  }

  if (!allowedClassifications.has(s.classification)) {
    errors.push(`[Skill ${s.id}] Invalid classification: '${s.classification}'. Allowed: ${[...allowedClassifications].join(', ')}`);
  }

  if (!allowedSourceKinds.has(s.sourceKind)) {
    errors.push(`[Skill ${s.id}] Invalid sourceKind: '${s.sourceKind}'. Allowed: ${[...allowedSourceKinds].join(', ')}`);
  }

  // Traceability & Policy Extension rules
  if (s.sourceKind === 'roadmap-derived') {
    if (!s.roadmapSource || !s.roadmapSource.startsWith('https://roadmap.sh/')) {
      errors.push(`[Skill ${s.id}] Roadmap-derived skill requires valid roadmapSource URL starting with https://roadmap.sh/`);
    }
    if (!s.roadmapLabel || typeof s.roadmapLabel !== 'string') {
      errors.push(`[Skill ${s.id}] Roadmap-derived skill requires non-empty roadmapLabel.`);
    }
  }

  if (s.sourceKind === 'assessment-policy') {
    if (!s.id.startsWith('assessment-ext.')) {
      errors.push(`[Skill ${s.id}] Assessment policy extension must use 'assessment-ext.' prefix.`);
    }
    if (s.classification !== 'assessment-policy-extension') {
      errors.push(`[Skill ${s.id}] Assessment policy extension must have classification 'assessment-policy-extension'.`);
    }
    if (s.roadmapNodeId !== null) {
      errors.push(`[Skill ${s.id}] Assessment policy extension must NOT have a fake roadmapNodeId.`);
    }
  }

  if (s.sourceKind === 'skillproof-toolkit') {
    if (!s.id.startsWith('toolkit.')) {
      errors.push(`[Skill ${s.id}] SkillProof toolkit extension must use 'toolkit.' prefix.`);
    }
    if (s.classification !== 'career-toolkit') {
      errors.push(`[Skill ${s.id}] SkillProof toolkit extension must have classification 'career-toolkit'.`);
    }
  }
});

// -------------------------------------------------------------
// 3. Validate Role Roadmap Nodes (Junctions)
// -------------------------------------------------------------
const roleNodePairs = new Set();
let frontendMetaFrameworksFound = false;
let dataAnalystDeepLearningFound = false;

roleNodesData.roleRoadmapNodes.forEach((rn, idx) => {
  if (!allRoleIds.has(rn.roleId)) {
    errors.push(`[RoleNode ${idx}] References unknown roleId: '${rn.roleId}'.`);
  }
  if (!canonicalSkillIds.has(rn.canonicalSkillId)) {
    errors.push(`[RoleNode ${idx}] References unknown canonicalSkillId: '${rn.canonicalSkillId}'.`);
  }

  const pairKey = `${rn.roleId}::${rn.canonicalSkillId}`;
  if (roleNodePairs.has(pairKey)) {
    errors.push(`Duplicate role-skill pair: '${pairKey}'.`);
  }
  roleNodePairs.add(pairKey);

  if (typeof rn.assessmentEligible !== 'boolean') {
    errors.push(`[RoleNode ${pairKey}] assessmentEligible must be explicitly boolean.`);
  }

  if (typeof rn.mandatoryFundamental !== 'boolean') {
    errors.push(`[RoleNode ${pairKey}] mandatoryFundamental must be explicitly boolean.`);
  }

  // Invariant: Mandatory fundamental implies assessmentEligible
  if (rn.mandatoryFundamental && !rn.assessmentEligible) {
    errors.push(`[RoleNode ${pairKey}] Mandatory fundamental must be assessmentEligible.`);
  }

  // Invariant: Optional/advanced cannot be mandatory core
  if (rn.isOptional && rn.mandatoryFundamental) {
    errors.push(`[RoleNode ${pairKey}] Optional node cannot be a mandatory fundamental.`);
  }

  // Specific rule: Frontend meta-frameworks non-mandatory
  if (rn.roleId === 'frontend-developer' && rn.canonicalSkillId === 'frontend.meta-frameworks') {
    frontendMetaFrameworksFound = true;
    if (rn.mandatoryFundamental) {
      errors.push(`Frontend meta-frameworks must NOT be mandatory.`);
    }
  }

  // Specific rule: Data Analyst Deep Learning optional
  if (rn.roleId === 'data-analyst' && rn.canonicalSkillId === 'data-analyst.deep-learning') {
    dataAnalystDeepLearningFound = true;
    if (!rn.isOptional || rn.mandatoryFundamental || rn.assessmentEligible) {
      errors.push(`Data Analyst Deep Learning must be optional, non-mandatory, and excluded from assessment.`);
    }
  }
});

if (!frontendMetaFrameworksFound) {
  errors.push(`Missing role node for frontend.meta-frameworks.`);
}
if (!dataAnalystDeepLearningFound) {
  errors.push(`Missing role node for data-analyst.deep-learning.`);
}

// -------------------------------------------------------------
// 4. Validate Roadmap Graph Relationships
// -------------------------------------------------------------
relationshipsData.relationships.forEach((rel, idx) => {
  if (!allRoleIds.has(rel.roleId)) {
    errors.push(`[Relationship ${rel.id || idx}] References unknown roleId '${rel.roleId}'.`);
  }
  if (!canonicalSkillIds.has(rel.sourceSkillId)) {
    errors.push(`[Relationship ${rel.id || idx}] References unknown sourceSkillId '${rel.sourceSkillId}'.`);
  }
  if (!canonicalSkillIds.has(rel.targetSkillId)) {
    errors.push(`[Relationship ${rel.id || idx}] References unknown targetSkillId '${rel.targetSkillId}'.`);
  }
  if (rel.sourceSkillId === rel.targetSkillId) {
    errors.push(`[Relationship ${rel.id || idx}] Self-loop detected on '${rel.sourceSkillId}'.`);
  }
});

// -------------------------------------------------------------
// 5. Validate Legacy Mappings & Question Traceability
// -------------------------------------------------------------
const legacySkillMap = new Map();
legacyMappingsData.skillMappings.forEach(sm => {
  if (!canonicalSkillIds.has(sm.canonicalId)) {
    errors.push(`[LegacySkillMapping ${sm.legacyId}] References unknown canonicalId '${sm.canonicalId}'.`);
  }
  legacySkillMap.set(sm.legacyId, sm.canonicalId);
});

// All 48 Backend questions must resolve
if (!Array.isArray(backendQuestionsData.questions) || backendQuestionsData.questions.length !== 48) {
  errors.push(`backend questions.json must contain exactly 48 questions.`);
} else {
  const mappedQuestions = new Set(legacyMappingsData.questionMappings.map(qm => qm.questionId));
  backendQuestionsData.questions.forEach(q => {
    if (!mappedQuestions.has(q.id)) {
      errors.push(`[Question ${q.id}] Missing from legacy-mappings.json questionMappings.`);
    }
    const qm = legacyMappingsData.questionMappings.find(x => x.questionId === q.id);
    if (qm && !canonicalSkillIds.has(qm.canonicalSkillId)) {
      errors.push(`[Question ${q.id}] Maps to unknown canonicalSkillId '${qm.canonicalSkillId}'.`);
    }
  });
}

// -------------------------------------------------------------
// Summary & Verdict
// -------------------------------------------------------------
console.log('--------------------------------------------------');
console.log(`Roles Validated: ${rolesData.roles.length} (${primaryDemoRoles.length} Primary Demo Roles)`);
console.log(`Canonical Skills Validated: ${canonicalSkillIds.size}`);
console.log(`Role-Skill Junctions Validated: ${roleNodePairs.size}`);
console.log(`Graph Relationships Validated: ${relationshipsData.relationships.length}`);
console.log(`Legacy Backend Skills Mapped: ${legacyMappingsData.skillMappings.length}`);
console.log(`Backend Questions Mapped: ${legacyMappingsData.questionMappings.length} / 48`);
console.log('--------------------------------------------------');

if (errors.length > 0) {
  console.error(`FAIL: Data V3 validation encountered ${errors.length} error(s):`);
  errors.forEach(e => console.error(`  - ${e}`));
  process.exit(1);
} else {
  console.log('PASS: Data V3 Production Foundation is 100% Valid and Compliant!');
  console.log('==================================================');
}
