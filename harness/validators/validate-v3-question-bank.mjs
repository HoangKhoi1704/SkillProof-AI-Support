import fs from 'fs';
import path from 'path';

console.log('==================================================');
console.log('SkillProof V2.3 Question Bank V3 Validator Starting');
console.log('==================================================');

const errors = [];
const warnings = [];

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
const questionSourcesData = loadJson('data/v3/question-sources.json');
const frontendQuestions = loadJson('data/v3/questions/frontend.json');
const dataAnalystQuestions = loadJson('data/v3/questions/data-analyst.json');
const backendMappings = loadJson('data/v3/questions/backend-mappings.json');
const backendQuestionsData = loadJson('data/backend/questions.json');

if (!rolesData || !skillsData || !roleNodesData || !questionSourcesData || 
    !frontendQuestions || !dataAnalystQuestions || !backendMappings || !backendQuestionsData) {
  console.error('Fatal: Could not load all required files.');
  process.exit(1);
}

const validRoleIds = new Set(rolesData.roles.map(r => r.id));
const validCanonicalSkillIds = new Set(skillsData.canonicalSkills.map(s => s.id));
const validSourceIds = new Set(questionSourcesData.sources.map(s => s.sourceId));

const approvedQuestionTypes = new Set([
  'definition',
  'concept',
  'scenario',
  'debugging',
  'design',
  'reasoning',
  'code-reading'
]);

const approvedDifficulties = new Set([
  'foundation',
  'applied',
  'advanced-reasoning'
]);

const requiredRubricKeys = [
  'Insufficient Evidence',
  'Beginner',
  'Intermediate',
  'Advanced'
];

// 1. Validate Sources
for (const src of questionSourcesData.sources) {
  if (!src.sourceId || !src.url || !src.verificationStatus || !src.verifiedAt) {
    errors.push(`Source ${src.sourceId || 'UNKNOWN'} is missing required fields (url, verificationStatus, verifiedAt).`);
  }
  if (src.verificationStatus === 'rejected') {
    errors.push(`Rejected source ${src.sourceId} cannot be registered as an active question source.`);
  }
}

// 2. Validate Question Collections
const seenQuestionIds = new Set();
const seenQuestionTexts = new Set();

function validateQuestions(qCollection, expectedRoleId) {
  if (qCollection.roleId !== expectedRoleId) {
    errors.push(`Collection roleId mismatch: expected '${expectedRoleId}', found '${qCollection.roleId}'.`);
  }
  if (!Array.isArray(qCollection.questions) || qCollection.questions.length === 0) {
    errors.push(`Collection for role '${expectedRoleId}' contains no questions.`);
    return;
  }

  for (const q of qCollection.questions) {
    // Unique ID
    if (!q.id || typeof q.id !== 'string') {
      errors.push(`Question in '${expectedRoleId}' missing ID.`);
      continue;
    }
    if (seenQuestionIds.has(q.id)) {
      errors.push(`Duplicate question ID detected: '${q.id}'.`);
    }
    seenQuestionIds.add(q.id);

    // Duplicate text check
    const normalizedText = q.question?.toLowerCase().replace(/\s+/g, ' ').trim();
    if (normalizedText) {
      if (seenQuestionTexts.has(normalizedText)) {
        errors.push(`Duplicate or near-duplicate question text found for ID: '${q.id}'.`);
      }
      seenQuestionTexts.add(normalizedText);
    } else {
      errors.push(`Question '${q.id}' has empty question text.`);
    }

    // Role ID validity
    if (!validRoleIds.has(q.roleId)) {
      errors.push(`Question '${q.id}' has invalid roleId: '${q.roleId}'.`);
    }

    // Canonical Skill ID validity
    if (!validCanonicalSkillIds.has(q.skillId)) {
      errors.push(`Question '${q.id}' has invalid canonical skillId: '${q.skillId}'.`);
    }

    // Question Type
    if (!approvedQuestionTypes.has(q.questionType)) {
      errors.push(`Question '${q.id}' has unapproved questionType: '${q.questionType}'. Allowed: ${[...approvedQuestionTypes].join(', ')}`);
    }

    // Difficulty
    if (!approvedDifficulties.has(q.difficulty)) {
      errors.push(`Question '${q.id}' has invalid difficulty: '${q.difficulty}'. Allowed: ${[...approvedDifficulties].join(', ')}`);
    }

    // Reference explanation
    if (!q.referenceExplanation || typeof q.referenceExplanation !== 'string' || q.referenceExplanation.trim().length < 30) {
      errors.push(`Question '${q.id}' missing required reference explanation (minimum 30 characters).`);
    }

    // Expected signals
    if (!Array.isArray(q.expectedSignals) || q.expectedSignals.length < 2) {
      errors.push(`Question '${q.id}' must have at least 2 expectedSignals.`);
    }

    // Rubric 4 tiers
    if (!q.rubric || typeof q.rubric !== 'object') {
      errors.push(`Question '${q.id}' missing rubric object.`);
    } else {
      for (const tier of requiredRubricKeys) {
        if (!q.rubric[tier] || typeof q.rubric[tier] !== 'string' || q.rubric[tier].trim().length < 10) {
          errors.push(`Question '${q.id}' rubric missing or has invalid description for tier: '${tier}'.`);
        }
      }
    }

    // Provenance & Source Locator Consistency
    const allowedVerificationStatuses = new Set(['claim-verified', 'source-identity-verified', 'reference-only', 'unsupported']);
    const allowedProvenances = new Set(['concept-derived', 'skillproof-curated', 'baseline-verified']);

    if (!q.provenance || !allowedProvenances.has(q.provenance)) {
      errors.push(`Question '${q.id}' has invalid or missing provenance: '${q.provenance}'. Allowed: ${[...allowedProvenances].join(', ')}`);
    }
    if (!q.verificationStatus || !allowedVerificationStatuses.has(q.verificationStatus)) {
      errors.push(`Question '${q.id}' has invalid or missing verificationStatus: '${q.verificationStatus}'. Allowed: ${[...allowedVerificationStatuses].join(', ')}`);
    }
    if (!q.verifiedAt || typeof q.verifiedAt !== 'string') {
      errors.push(`Question '${q.id}' missing verifiedAt date stamp.`);
    }

    // Structural Consistency based on conceptual state
    if (q.verificationStatus === 'claim-verified' || q.verificationStatus === 'source-identity-verified') {
      if (!Array.isArray(q.frameworkSourceIds) || q.frameworkSourceIds.length === 0) {
        errors.push(`Question '${q.id}' is '${q.verificationStatus}' but has empty frameworkSourceIds.`);
      }
      if (!q.sourceLocator || typeof q.sourceLocator !== 'string' || q.sourceLocator.trim().length < 5) {
        errors.push(`Question '${q.id}' is '${q.verificationStatus}' but is missing required sourceLocator.`);
      }
    } else if (q.verificationStatus === 'reference-only') {
      if (!Array.isArray(q.frameworkSourceIds) || q.frameworkSourceIds.length === 0) {
        errors.push(`Question '${q.id}' is 'reference-only' but has no referenced source.`);
      }
      for (const srcId of (q.frameworkSourceIds || [])) {
        const srcObj = questionSourcesData.sources.find(s => s.sourceId === srcId);
        if (srcObj && srcObj.verificationStatus !== 'reference-only') {
          errors.push(`Question '${q.id}' is marked reference-only but cites fully-verified source '${srcId}'.`);
        }
      }
    } else if (q.verificationStatus === 'unsupported') {
      // Honestly curated without direct external grounding
      if (Array.isArray(q.frameworkSourceIds) && q.frameworkSourceIds.length > 0) {
        errors.push(`Question '${q.id}' has status 'unsupported' but has frameworkSourceIds attached. Must be empty [].`);
      }
      if (q.sourceLocator !== null && q.sourceLocator !== undefined && q.sourceLocator !== '') {
        errors.push(`Question '${q.id}' has status 'unsupported' but has non-empty sourceLocator: '${q.sourceLocator}'. Must be null.`);
      }
    }

    // Source Registry check
    if (Array.isArray(q.frameworkSourceIds)) {
      for (const srcId of q.frameworkSourceIds) {
        if (!validSourceIds.has(srcId)) {
          errors.push(`Question '${q.id}' references unregistered source ID: '${srcId}'.`);
        }
      }
    } else {
      errors.push(`Question '${q.id}' missing frameworkSourceIds array.`);
    }

    // Specific Provenance Red Flag Guards
    // Guard 1: Hadley Wickham Tidy Data can ONLY support q-da-wrang-01
    if (Array.isArray(q.frameworkSourceIds) && q.frameworkSourceIds.includes('src-doc-hadley-tidy-data')) {
      if (q.id !== 'q-da-wrang-01') {
        errors.push(`Provenance Violation: 'src-doc-hadley-tidy-data' cannot be used as source for '${q.id}'. Only q-da-wrang-01 may cite Tidy Data.`);
      }
    }

    // Guard 2: Reject synthetic locator patterns
    const suspiciousLocatorPrefixes = [
      'Spreadsheet Formula Specs:',
      'Spreadsheet Architecture:',
      'Data Aggregation Models:',
      'Performance Metrics:'
    ];
    if (q.sourceLocator && typeof q.sourceLocator === 'string') {
      for (const prefix of suspiciousLocatorPrefixes) {
        if (q.sourceLocator.startsWith(prefix)) {
          errors.push(`Provenance Violation: Question '${q.id}' has suspicious synthetic locator: '${q.sourceLocator}'.`);
        }
      }
    }
  }
}

validateQuestions(frontendQuestions, 'frontend-developer');
validateQuestions(dataAnalystQuestions, 'data-analyst');

// 3. Validate Backend 48 Frozen Bank Resolution
const rawBackendQuestions = backendQuestionsData.questions;
if (!Array.isArray(rawBackendQuestions) || rawBackendQuestions.length !== 48) {
  errors.push(`Backend bank must contain exactly 48 frozen questions, found ${rawBackendQuestions?.length}.`);
}

const rawBackendIdSet = new Set(rawBackendQuestions.map(q => q.id));
for (const mapping of backendMappings.canonicalMappings) {
  if (!validCanonicalSkillIds.has(mapping.canonicalSkillId)) {
    errors.push(`Backend mapping has invalid canonicalSkillId: '${mapping.canonicalSkillId}'.`);
  }
  for (const qId of mapping.questionIds) {
    if (!rawBackendIdSet.has(qId)) {
      errors.push(`Backend mapping references unknown raw question ID: '${qId}'.`);
    }
  }
}

// 4. Validate Mandatory Competency Coverage
const feMandatoryNodes = roleNodesData.roleRoadmapNodes.filter(n => n.roleId === 'frontend-developer' && n.mandatoryFundamental);
const daMandatoryNodes = roleNodesData.roleRoadmapNodes.filter(n => n.roleId === 'data-analyst' && n.mandatoryFundamental);

const feCoveredSkills = new Set(frontendQuestions.questions.map(q => q.skillId));
const daCoveredSkills = new Set(dataAnalystQuestions.questions.map(q => q.skillId));

for (const node of feMandatoryNodes) {
  if (!feCoveredSkills.has(node.canonicalSkillId)) {
    errors.push(`Frontend mandatory fundamental '${node.canonicalSkillId}' has NO question coverage in Question Bank V3.`);
  }
  if (!node.hasQuestionCoverage) {
    errors.push(`Frontend node '${node.canonicalSkillId}' is covered in Question Bank V3 but hasQuestionCoverage is false in role-roadmap-nodes.json.`);
  }
}

for (const node of daMandatoryNodes) {
  if (!daCoveredSkills.has(node.canonicalSkillId)) {
    errors.push(`Data Analyst mandatory fundamental '${node.canonicalSkillId}' has NO question coverage in Question Bank V3.`);
  }
  if (!node.hasQuestionCoverage) {
    errors.push(`Data Analyst node '${node.canonicalSkillId}' is covered in Question Bank V3 but hasQuestionCoverage is false in role-roadmap-nodes.json.`);
  }
}

// 5. Anti-Hallucination & Scope Guards
// Guard: Deep Learning must NOT be mandatory or core in Data Analyst
const dlNode = roleNodesData.roleRoadmapNodes.find(n => n.roleId === 'data-analyst' && n.canonicalSkillId === 'data-analyst.deep-learning');
if (dlNode && (dlNode.mandatoryFundamental || dlNode.category === 'core')) {
  errors.push(`Violation: data-analyst.deep-learning must NOT be mandatory or core in Data Analyst.`);
}

// Guard: Frontend meta-frameworks (Next.js/Remix/Astro) must NOT be mandatory
const metaFrameworkNodes = roleNodesData.roleRoadmapNodes.filter(n => 
  n.roleId === 'frontend-developer' && 
  (n.canonicalSkillId.includes('meta-framework') || n.canonicalSkillId.includes('nextjs'))
);
for (const mf of metaFrameworkNodes) {
  if (mf.mandatoryFundamental) {
    errors.push(`Violation: Frontend meta-framework '${mf.canonicalSkillId}' must NOT be mandatory.`);
  }
}

// Report
console.log(`\nValidated Frontend Questions: ${frontendQuestions.questions.length}`);
console.log(`Validated Data Analyst Questions: ${dataAnalystQuestions.questions.length}`);
console.log(`Validated Backend Frozen Questions: ${rawBackendQuestions.length}`);

if (errors.length > 0) {
  console.error(`\nFAILED with ${errors.length} error(s):`);
  errors.forEach(e => console.error(`  - ${e}`));
  process.exit(1);
}

console.log('\n==================================================');
console.log('SUCCESS: All V2.3 Question Bank V3 checks passed!');
console.log('==================================================');
