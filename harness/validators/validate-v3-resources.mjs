import fs from 'fs';
import path from 'path';

console.log('==================================================');
console.log('PASS: SkillProof Data V3 Learning Resources Validator Starting');
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

const resourcesData = loadJson('data/v3/learning-resources.json');
const skillsData = loadJson('data/v3/canonical-skills.json');
const rolesData = loadJson('data/v3/roles.json');

if (!resourcesData || !skillsData || !rolesData) {
  console.error('Fatal: Could not load required JSON files for resource validation.');
  process.exit(1);
}

const validSkillIds = new Set(skillsData.canonicalSkills.map(s => s.id));
const validRoleIds = new Set(rolesData.roles.map(r => r.id));

const allowedResourceTypes = new Set([
  'official-doc',
  'guide',
  'tutorial',
  'interactive-practice',
  'reference'
]);

const allowedLevels = new Set(['foundation', 'applied', 'advanced']);

const resourceIds = new Set();

if (!Array.isArray(resourcesData.learningResources) || resourcesData.learningResources.length === 0) {
  errors.push('learningResources array is missing or empty.');
} else {
  resourcesData.learningResources.forEach((res, idx) => {
    const prefix = `[Resource #${idx + 1} (${res.id || 'NO_ID'})]`;

    if (!res.id || typeof res.id !== 'string') {
      errors.push(`${prefix} Missing or invalid id.`);
      return;
    }

    if (resourceIds.has(res.id)) {
      errors.push(`${prefix} Duplicate resource ID: '${res.id}'.`);
    }
    resourceIds.add(res.id);

    if (!res.title || typeof res.title !== 'string') {
      errors.push(`${prefix} Missing title.`);
    }

    if (!res.sourceName || typeof res.sourceName !== 'string') {
      errors.push(`${prefix} Missing sourceName.`);
    }

    if (!res.sourceUrl || typeof res.sourceUrl !== 'string' || !res.sourceUrl.startsWith('http')) {
      errors.push(`${prefix} Invalid or missing sourceUrl (must start with http/https): '${res.sourceUrl}'.`);
    }

    // Zero YouTube policy check
    if (res.sourceUrl && (res.sourceUrl.includes('youtube.com') || res.sourceUrl.includes('youtu.be'))) {
      errors.push(`${prefix} YouTube resources are prohibited under strict provenance policy.`);
    }

    if (!allowedResourceTypes.has(res.resourceType)) {
      errors.push(`${prefix} Invalid resourceType: '${res.resourceType}'. Allowed: ${[...allowedResourceTypes].join(', ')}`);
    }

    if (!allowedLevels.has(res.level)) {
      errors.push(`${prefix} Invalid level: '${res.level}'. Allowed: ${[...allowedLevels].join(', ')}`);
    }

    if (typeof res.isOfficial !== 'boolean') {
      errors.push(`${prefix} isOfficial must be a boolean.`);
    }

    if (res.verificationStatus !== 'verified') {
      errors.push(`${prefix} Invalid verificationStatus: '${res.verificationStatus}'. Must be 'verified'.`);
    }

    if (!res.verifiedAt || isNaN(Date.parse(res.verifiedAt))) {
      errors.push(`${prefix} Invalid verifiedAt timestamp: '${res.verifiedAt}'.`);
    }

    if (!Array.isArray(res.canonicalSkillIds) || res.canonicalSkillIds.length === 0) {
      errors.push(`${prefix} Must map to at least one canonicalSkillId.`);
    } else {
      res.canonicalSkillIds.forEach(skId => {
        if (!validSkillIds.has(skId)) {
          errors.push(`${prefix} Dangling canonicalSkillId: '${skId}' not found in canonical-skills.json.`);
        }
      });
    }

    if (!Array.isArray(res.roleIds) || res.roleIds.length === 0) {
      errors.push(`${prefix} Must map to at least one roleId.`);
    } else {
      res.roleIds.forEach(rId => {
        if (!validRoleIds.has(rId)) {
          errors.push(`${prefix} Invalid roleId: '${rId}' not found in roles.json.`);
        }
      });
    }
  });
}

console.log(`Validated Learning Resources: ${resourceIds.size}`);
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
console.log('PASS: All Learning Resource catalog checks passed!');
console.log('==================================================');
