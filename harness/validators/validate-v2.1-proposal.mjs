import fs from 'fs';
import path from 'path';

console.log('==================================================');
console.log('SkillProof V2.1A Canonical Framework Proposal Validator');
console.log('==================================================');

const errors = [];
const warnings = [];

// 1. Check raw snapshots
const rawFiles = [
  'docs/v2.1/roadmap-frontend-raw.md',
  'docs/v2.1/roadmap-backend-raw.md',
  'docs/v2.1/roadmap-data-analyst-raw.md'
];

for (const rf of rawFiles) {
  if (!fs.existsSync(rf)) {
    errors.push(`Missing raw roadmap snapshot: ${rf}`);
  } else {
    const text = fs.readFileSync(rf, 'utf8');
    const matches = text.match(/\| \d+ \|/g) || [];
    if (matches.length === 0) {
      errors.push(`Raw roadmap snapshot ${rf} has 0 parsed items.`);
    } else {
      console.log(`[PASS] ${rf} contains ${matches.length} verified raw items.`);
    }
  }
}

// 2. Check proposal document
const proposalPath = 'docs/v2.1/V2_1_CANONICAL_FRAMEWORK_PROPOSAL.md';
if (!fs.existsSync(proposalPath)) {
  errors.push(`Missing proposal document: ${proposalPath}`);
} else {
  const proposalText = fs.readFileSync(proposalPath, 'utf8');
  
  // Verify required sections
  const requiredSections = [
    'Executive Summary',
    'Source Provenance',
    'Canonical Identity',
    'Frontend Developer Framework Proposal',
    'Backend Developer Framework Proposal',
    'Data Analyst Framework Proposal',
    'Assessment Policy Extensions',
    'Career Toolkit Strategy',
    'Human-Review Approval Tables'
  ];

  for (const sec of requiredSections) {
    if (!proposalText.includes(sec)) {
      errors.push(`Proposal document missing required section: "${sec}"`);
    }
  }

  // Extract canonical IDs
  const idRegex = /`((?:shared|frontend|backend|data-analyst|assessment-ext|toolkit)\.[a-z0-9-]+)`/g;
  let match;
  const canonicalIds = new Set();
  while ((match = idRegex.exec(proposalText)) !== null) {
    canonicalIds.add(match[1]);
  }
  console.log(`[PASS] Identified ${canonicalIds.size} unique canonical proposal IDs.`);

  // 3. Check question mapping audit
  const mappingPath = 'docs/v2.1/V2_1_QUESTION_MAPPING_AUDIT.md';
  if (!fs.existsSync(mappingPath)) {
    errors.push(`Missing question mapping audit: ${mappingPath}`);
  } else {
    const mappingText = fs.readFileSync(mappingPath, 'utf8');
    const questionsData = JSON.parse(fs.readFileSync('data/backend/questions.json', 'utf8'));

    let mappedCount = 0;
    for (const q of questionsData.questions) {
      if (!mappingText.includes(`\`${q.id}\``)) {
        errors.push(`Question ${q.id} not mapped in ${mappingPath}`);
      } else {
        mappedCount++;
      }
    }
    console.log(`[PASS] All ${mappedCount} current backend questions mapped in audit.`);
  }
}

console.log('--------------------------------------------------');
if (errors.length > 0) {
  console.error(`FAIL: Found ${errors.length} validation errors:`);
  errors.forEach(e => console.error(`  - ${e}`));
  process.exit(1);
} else {
  console.log('PASS: SkillProof V2.1A Proposal Artifacts are fully validated!');
  console.log('==================================================');
}
