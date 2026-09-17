import fs from 'fs';
import path from 'path';

console.log('==================================================');
console.log('PASS: SkillProof Data V3 Projects Catalog Validator Starting');
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

const projectsData = loadJson('data/v3/projects.json');
const skillsData = loadJson('data/v3/canonical-skills.json');
const rolesData = loadJson('data/v3/roles.json');

if (!projectsData || !skillsData || !rolesData) {
  console.error('Fatal: Could not load required JSON files for project validation.');
  process.exit(1);
}

const validSkillIds = new Set(skillsData.canonicalSkills.map(s => s.id));
const validRoleIds = new Set(rolesData.roles.map(r => r.id));

const allowedProjectTypes = new Set(['practice', 'portfolio']);
const allowedDifficulties = new Set(['foundation', 'intermediate', 'advanced']);
const allowedProvenances = new Set(['external-curated', 'adapted', 'skillproof-curated']);

const projectIds = new Set();
let practiceCount = 0;
let portfolioCount = 0;

if (!Array.isArray(projectsData.projects) || projectsData.projects.length === 0) {
  errors.push('projects array is missing or empty.');
} else {
  projectsData.projects.forEach((proj, idx) => {
    const prefix = `[Project #${idx + 1} (${proj.id || 'NO_ID'})]`;

    if (!proj.id || typeof proj.id !== 'string') {
      errors.push(`${prefix} Missing or invalid id.`);
      return;
    }

    if (projectIds.has(proj.id)) {
      errors.push(`${prefix} Duplicate project ID: '${proj.id}'.`);
    }
    projectIds.add(proj.id);

    if (!proj.title || typeof proj.title !== 'string') {
      errors.push(`${prefix} Missing title.`);
    }

    if (!proj.description || typeof proj.description !== 'string') {
      errors.push(`${prefix} Missing description.`);
    }

    if (!allowedProjectTypes.has(proj.projectType)) {
      errors.push(`${prefix} Invalid projectType: '${proj.projectType}'. Allowed: ${[...allowedProjectTypes].join(', ')}`);
    } else {
      if (proj.projectType === 'practice') practiceCount++;
      if (proj.projectType === 'portfolio') portfolioCount++;
    }

    if (!allowedDifficulties.has(proj.difficulty)) {
      errors.push(`${prefix} Invalid difficulty: '${proj.difficulty}'. Allowed: ${[...allowedDifficulties].join(', ')}`);
    }

    if (!allowedProvenances.has(proj.provenance)) {
      errors.push(`${prefix} Invalid provenance: '${proj.provenance}'. Allowed: ${[...allowedProvenances].join(', ')}`);
    }

    // Provenance and source consistency checks
    if (proj.provenance === 'skillproof-curated') {
      if (proj.source !== 'skillproof-curated') {
        errors.push(`${prefix} Provenance is skillproof-curated but source is '${proj.source}'. Must match honestly.`);
      }
    } else if (proj.provenance === 'external-curated' || proj.provenance === 'adapted') {
      if (!proj.sourceUrl || !proj.sourceUrl.startsWith('http')) {
        errors.push(`${prefix} External/adapted project requires valid sourceUrl starting with http/https.`);
      }
      if (!proj.sourceLocator) {
        errors.push(`${prefix} External/adapted project requires sourceLocator (e.g. commit / section).`);
      }
    }

    if (proj.verificationStatus !== 'verified') {
      errors.push(`${prefix} Invalid verificationStatus: '${proj.verificationStatus}'. Must be 'verified'.`);
    }

    if (!proj.verifiedAt || isNaN(Date.parse(proj.verifiedAt))) {
      errors.push(`${prefix} Invalid verifiedAt timestamp: '${proj.verifiedAt}'.`);
    }

    if (!Array.isArray(proj.deliverables) || proj.deliverables.length === 0) {
      errors.push(`${prefix} Must provide non-empty deliverables array.`);
    }

    if (!Array.isArray(proj.evidenceRequirements) || proj.evidenceRequirements.length === 0) {
      errors.push(`${prefix} Must provide non-empty evidenceRequirements array.`);
    }

    if (!Array.isArray(proj.canonicalSkillIds) || proj.canonicalSkillIds.length === 0) {
      errors.push(`${prefix} Must map to at least one canonicalSkillId.`);
    } else {
      proj.canonicalSkillIds.forEach(skId => {
        if (!validSkillIds.has(skId)) {
          errors.push(`${prefix} Dangling canonicalSkillId: '${skId}' not found in canonical-skills.json.`);
        }
      });
    }

    if (!Array.isArray(proj.roadmapTargets) || proj.roadmapTargets.length === 0) {
      errors.push(`${prefix} Must map to at least one roadmapTarget.`);
    } else {
      proj.roadmapTargets.forEach(tgt => {
        if (!validSkillIds.has(tgt)) {
          errors.push(`${prefix} Dangling roadmapTarget: '${tgt}' not found in canonical-skills.json.`);
        }
      });
    }

    if (!Array.isArray(proj.roleIds) || proj.roleIds.length === 0) {
      errors.push(`${prefix} Must map to at least one roleId.`);
    } else {
      proj.roleIds.forEach(rId => {
        if (!validRoleIds.has(rId)) {
          errors.push(`${prefix} Invalid roleId: '${rId}' not found in roles.json.`);
        }
      });
    }

    // Guard against making Deep Learning mandatory for Data Analyst
    if (proj.roleIds.includes('data-analyst') && proj.projectType === 'portfolio') {
      if (proj.canonicalSkillIds.includes('data-analyst.deep-learning')) {
        errors.push(`${prefix} Deep Learning must NOT be a mandatory portfolio requirement for Data Analyst.`);
      }
    }

    // Guard against rubric or private expected signals leakage in projects catalog
    if (proj.rubric || proj.expectedSignals) {
      errors.push(`${prefix} Public project catalog must NOT leak internal rubrics or expectedSignals.`);
    }
  });
}

console.log(`Validated Curated Projects: ${projectIds.size} (${practiceCount} Practice, ${portfolioCount} Portfolio)`);
if (warnings.length > 0) {
  console.log(`Warnings (${warnings.length}):`);
  warnings.forEach(w => console.warn(`  - ${w}`));
}

if (errors.length > 0) {
  console.error(`FAILED: ${errors.length} validation error(s) found:`);
  errors.forEach(e => console.error(`  - ${e}`));
  process.exit(1);
}

console.log('==================================================');
console.log('PASS: All Curated Projects catalog checks passed!');
console.log('==================================================');
