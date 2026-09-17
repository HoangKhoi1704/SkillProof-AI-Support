import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const rootDir = path.resolve(__dirname, "../..");

const dataDir = process.argv[2]
    ? path.resolve(process.cwd(), process.argv[2])
    : path.resolve(rootDir, "data/backend");

const catalogPath = path.join(dataDir, "skill-catalog.json");
const sourcesPath = path.join(dataDir, "sources.json");
const questionsPath = path.join(dataDir, "questions.json");
const baselinePath = path.resolve(__dirname, "../baselines/v2.0-core-questions-baseline.json");

const errors = [];

function loadJson(filePath, label) {
    if (!fs.existsSync(filePath)) {
        errors.push(`Missing required file: ${filePath}`);
        return null;
    }
    try {
        const content = fs.readFileSync(filePath, "utf8");
        return JSON.parse(content);
    } catch (err) {
        errors.push(`Invalid JSON in ${label} (${filePath}): ${err.message}`);
        return null;
    }
}

function canonicalStringify(obj) {
    if (obj === null || typeof obj !== "object") return JSON.stringify(obj);
    if (Array.isArray(obj)) return "[" + obj.map(canonicalStringify).join(",") + "]";
    const keys = Object.keys(obj).sort();
    return "{" + keys.map(k => JSON.stringify(k) + ":" + canonicalStringify(obj[k])).join(",") + "}";
}

const catalogData = loadJson(catalogPath, "skill-catalog.json");
const sourcesData = loadJson(sourcesPath, "sources.json");
const questionsData = loadJson(questionsPath, "questions.json");
const baselineData = loadJson(baselinePath, "v2.0-core-questions-baseline.json");

if (errors.length > 0) {
    console.error("FAIL: Unable to load assessment dataset files:");
    for (const err of errors) console.error(` - ${err}`);
    process.exit(1);
}

// -------------------------------------------------------------
// 1. Validate Sources Catalog (sources.json) - Strict Provenance Gate
// -------------------------------------------------------------
const sourceIds = new Set();
const sourcesById = new Map();
const invalidSourceIds = new Set();
const referenceOnlySourceIds = new Set();
const claimVerifiedSourceIds = new Set();

const allowedSourceTypes = new Set([
    "career-framework",
    "official-standard",
    "official-documentation",
    "professional-reference",
    "interview-preparation-resource",
    "candidate-reported-interview",
    "company-published-interview-guidance"
]);

const allowedAccessStatuses = new Set([
    "accessible",
    "partially-accessible",
    "paywalled",
    "unavailable",
    "redirected",
    "not-verifiable"
]);

const allowedVerificationStatuses = new Set([
    "claim-verified",
    "source-identity-verified",
    "community-curated-verified",
    "reference-only",
    "rejected"
]);

if (!Array.isArray(sourcesData.sources)) {
    errors.push("sources.json must contain a top-level 'sources' array.");
} else {
    sourcesData.sources.forEach((src, idx) => {
        const id = src.sourceId;
        if (!id || typeof id !== "string" || !id.trim()) {
            errors.push(`[sources.json item ${idx}] sourceId must be a non-empty string.`);
        } else if (sourceIds.has(id)) {
            errors.push(`[sources.json item ${idx}] Duplicate sourceId: '${id}'.`);
        } else {
            sourceIds.add(id);
            sourcesById.set(id, src);
        }

        if (!src.publisher || typeof src.publisher !== "string" || !src.publisher.trim()) {
            errors.push(`[Source ${id || idx}] publisher must be a non-empty string.`);
        }

        if (!src.title || typeof src.title !== "string" || !src.title.trim()) {
            errors.push(`[Source ${id || idx}] title must be a non-empty string.`);
        }

        if (!src.url || typeof src.url !== "string" || !src.url.startsWith("http")) {
            errors.push(`[Source ${id || idx}] url must be a valid HTTP/HTTPS URL.`);
        }

        if (!src.canonicalUrl || typeof src.canonicalUrl !== "string" || !src.canonicalUrl.startsWith("http")) {
            errors.push(`[Source ${id || idx}] canonicalUrl must be a valid HTTP/HTTPS URL.`);
        }

        if (!allowedSourceTypes.has(src.sourceType)) {
            errors.push(`[Source ${id || idx}] Invalid sourceType: '${src.sourceType}'. Allowed: ${[...allowedSourceTypes].join(", ")}`);
        }

        if (!allowedAccessStatuses.has(src.accessStatus)) {
            errors.push(`[Source ${id || idx}] Invalid accessStatus: '${src.accessStatus}'. Allowed: ${[...allowedAccessStatuses].join(", ")}`);
        }

        if (!allowedVerificationStatuses.has(src.verificationStatus)) {
            errors.push(`[Source ${id || idx}] Invalid verificationStatus: '${src.verificationStatus}'. Allowed: ${[...allowedVerificationStatuses].join(", ")}`);
        }

        if (!src.accessedAt || typeof src.accessedAt !== "string") {
            errors.push(`[Source ${id || idx}] accessedAt must be a valid date string.`);
        }

        // Check verification invariants
        if (src.verificationStatus === "rejected" || src.accessStatus === "unavailable") {
            invalidSourceIds.add(id);
        }

        if (src.verificationStatus === "reference-only") {
            referenceOnlySourceIds.add(id);
        }

        if (src.verificationStatus === "claim-verified" || src.verificationStatus === "community-curated-verified") {
            claimVerifiedSourceIds.add(id);
            if (!src.verifiedAt || typeof src.verifiedAt !== "string") {
                errors.push(`[Source ${id || idx}] Claim-verified source requires verifiedAt date.`);
            }
        }

        if (src.sourceType === "candidate-reported-interview" && src.verificationStatus !== "rejected") {
            errors.push(`[Source ${id || idx}] Unaudited candidate-reported interviews are not permitted in v2.`);
        }
    });
}

// -------------------------------------------------------------
// 2. Validate Skill Catalog (skill-catalog.json)
// -------------------------------------------------------------
const allSkillIds = new Set();
const competencyIds = new Set();
const coreCompetencyIds = new Set();
const recommendedCompetencyIds = new Set();
const optionalCompetencyIds = new Set();
const languageSkillIds = new Set();
const subskillsBySkill = new Map();

if (catalogData.roleId !== "backend-developer") {
    errors.push(`skill-catalog.json roleId must be 'backend-developer' (got '${catalogData.roleId}').`);
}

const requiredCoreCompetencies = new Set([
    "programming-fundamentals",
    "rest-api",
    "sql",
    "testing",
    "authentication-security",
    "system-design"
]);

const requiredRecommendedCompetencies = new Set([
    "nosql",
    "caching"
]);

if (!Array.isArray(catalogData.competencies)) {
    errors.push("skill-catalog.json must contain a 'competencies' array.");
} else {
    catalogData.competencies.forEach((comp, idx) => {
        const id = comp.id;
        if (!id || typeof id !== "string" || !id.trim()) {
            errors.push(`[competencies item ${idx}] Competency id must be a non-empty string.`);
            return;
        }

        if (allSkillIds.has(id)) {
            errors.push(`[Competency ${id}] Duplicate skill ID detected.`);
        }
        allSkillIds.add(id);
        competencyIds.add(id);

        if (comp.category === "core") {
            coreCompetencyIds.add(id);
        } else if (comp.category === "recommended") {
            recommendedCompetencyIds.add(id);
        } else if (comp.category === "optional") {
            optionalCompetencyIds.add(id);
        }

        if (!["core", "recommended", "optional"].includes(comp.category)) {
            errors.push(`[Competency ${id}] Invalid category: '${comp.category}'.`);
        }

        if (!comp.name || typeof comp.name !== "string") {
            errors.push(`[Competency ${id}] Missing or invalid name.`);
        }

        if (!comp.description || typeof comp.description !== "string") {
            errors.push(`[Competency ${id}] Missing or invalid description.`);
        }

        const subskillSet = new Set();
        if (!Array.isArray(comp.subskills) || comp.subskills.length === 0) {
            errors.push(`[Competency ${id}] Must have a non-empty 'subskills' array.`);
        } else {
            comp.subskills.forEach((sub, sIdx) => {
                if (!sub.id || typeof sub.id !== "string") {
                    errors.push(`[Competency ${id} subskill ${sIdx}] Subskill id must be string.`);
                } else if (subskillSet.has(sub.id)) {
                    errors.push(`[Competency ${id}] Duplicate subskill id: '${sub.id}'.`);
                } else {
                    subskillSet.add(sub.id);
                }
            });
        }
        subskillsBySkill.set(id, subskillSet);
    });
}

// Verify all 6 required core competencies exist
for (const reqCore of requiredCoreCompetencies) {
    if (!coreCompetencyIds.has(reqCore)) {
        errors.push(`skill-catalog.json is missing required core competency: '${reqCore}'.`);
    }
}

// Verify all required recommended competencies exist
for (const reqRec of requiredRecommendedCompetencies) {
    if (!recommendedCompetencyIds.has(reqRec)) {
        errors.push(`skill-catalog.json is missing required recommended competency: '${reqRec}'.`);
    }
}

// Verify languages category is separate from competencies
if (!Array.isArray(catalogData.languages)) {
    errors.push("skill-catalog.json must contain a separate 'languages' array.");
} else {
    const requiredLanguages = new Set(["csharp", "java", "python", "cpp", "javascript", "typescript", "go", "rust"]);

    catalogData.languages.forEach((lang, idx) => {
        const id = lang.id;
        if (!id || typeof id !== "string") {
            errors.push(`[languages item ${idx}] Language id must be a string.`);
            return;
        }

        if (competencyIds.has(id)) {
            errors.push(`[Language ${id}] Language ID duplicates an existing competency ID.`);
        }
        allSkillIds.add(id);
        languageSkillIds.add(id);

        if (!lang.name || typeof lang.name !== "string") {
            errors.push(`[Language ${id}] Missing or invalid name.`);
        }

        const langSubskillSet = new Set();
        if (!Array.isArray(lang.subskills) || lang.subskills.length === 0) {
            errors.push(`[Language ${id}] Missing or empty subskills array.`);
        } else {
            lang.subskills.forEach((sub, sIdx) => {
                if (!sub.id || typeof sub.id !== "string") {
                    errors.push(`[Language ${id} subskill ${sIdx}] Subskill id must be string.`);
                } else if (langSubskillSet.has(sub.id)) {
                    errors.push(`[Language ${id}] Duplicate subskill id: '${sub.id}'.`);
                } else {
                    langSubskillSet.add(sub.id);
                }
            });
        }
        subskillsBySkill.set(id, langSubskillSet);
    });

    for (const reqLang of requiredLanguages) {
        if (!languageSkillIds.has(reqLang)) {
            errors.push(`skill-catalog.json is missing required language: '${reqLang}'.`);
        }
    }
}

// Total Assessable Skills in Data Foundation v2 = 6 Core + 2 Recommended + 8 Languages = 16
const assessableCompetencyIds = new Set([...coreCompetencyIds, ...recommendedCompetencyIds]);
const assessableSkillIds = new Set([...assessableCompetencyIds, ...languageSkillIds]);

// -------------------------------------------------------------
// 3. Frozen Data Foundation v2.0 Core Baseline & Integrity Lock
// -------------------------------------------------------------
const EXPECTED_BASELINE_FILE_SHA256 = "42a6b6b3a6b83f0fbb8c69163d23f178d0ad46c4b3ed1c81245ee04f27f23eb3";

const FROZEN_CORE_QUESTION_FINGERPRINTS = {
    "q-be-prog-01": "8c54c0e89b6fd77664bc2442cdd559e9438f17c982ed96742b35b4a420cada29",
    "q-be-prog-02": "befac0b1d617805add91b4171fe251843f55254e73af61e51bcd38484dcfd8e3",
    "q-be-prog-03": "c8a311692d30430372c90b19005357a1a5fe186dd7466a88a31d246878d0fa9f",
    "q-be-rest-01": "10d4dc2ebf6579831797b5fbaba3ccedc506710b97d14eb2420b917f965c5fba",
    "q-be-rest-02": "954a7c7097fe62a9d873af6a2d028f876eed5855d215910965fe5bce4e263404",
    "q-be-rest-03": "bec29f920ce7bcd0b03368e21b67e3c06d87f99319574e133a916e2abb3a4816",
    "q-be-sql-01": "74e6feda7262c53f7109ee7b6866f28f3bba41c3b1c2032ea8a01caa97a8368e",
    "q-be-sql-02": "9c0518f4fb7f07f5b0b5fa41da5cc428ad73752520e7736965382159331a1254",
    "q-be-sql-03": "a7cc43d56260a4d495f24f11d59107d95c32c27238475a104a7eaa0dff69f154",
    "q-be-test-01": "69ee4c50f6e7aa57a4836d5a603ce330504e13db39a693400896cfc9ed2ecbf9",
    "q-be-test-02": "aafbb0916b01ebc1fd5ad37c244a707a3256d56b8950717a34798f2fe5cb89d7",
    "q-be-test-03": "465c9129eb240bd6404002830e40b4079061b51575149c2927aef2dea2b9aa39",
    "q-be-auth-01": "abfecdddeb1015f834591f161358abc761bf19d44e665e64c6af0b0d21908973",
    "q-be-auth-02": "a093cf3cf6a104e5baddd8fbb62345433be63fd19f1f7e4a072c45092bf63aa1",
    "q-be-auth-03": "595c4c89f62302a489f2ddfae0510bf790a914ef1a99392a1fe7c34e90c6257a",
    "q-be-sys-01": "4a4ed8d74b7575bd99e42b5f9f2c69e0f1448d9e89c9d34b166b32644d2183ba",
    "q-be-sys-02": "3d3f7eadcb30c57b2724e9066492a60f75c3754bba81085125bd7a833dfbf05e",
    "q-be-sys-03": "2efc55456ecf74cb3b02bb4107508f2249440fae289382b434fcb3185d54ebe7"
};

const protectedFields = [
    "id",
    "roleId",
    "skillId",
    "subskills",
    "difficulty",
    "questionType",
    "question",
    "expectedSignals",
    "rubric",
    "frameworkSourceIds",
    "interviewEvidenceIds",
    "interviewEvidence",
    "provenance",
    "verificationStatus"
];

// Verify baseline file integrity
if (baselineData) {
    const rawBaseline = fs.readFileSync(baselinePath, "utf8").replace(/\r\n/g, "\n");
    const actualBaselineHash = crypto.createHash("sha256").update(rawBaseline).digest("hex");
    if (actualBaselineHash !== EXPECTED_BASELINE_FILE_SHA256) {
        errors.push(`Frozen baseline snapshot file (${baselinePath}) failed SHA-256 integrity check. Expected: ${EXPECTED_BASELINE_FILE_SHA256}, Got: ${actualBaselineHash}`);
    }
}

// -------------------------------------------------------------
// 4. Validate Question Bank (questions.json)
// -------------------------------------------------------------
const allowedQuestionTypes = new Set(["knowledge", "interview", "practical_case", "reasoning"]);
const allowedDifficulties = new Set(["foundation", "applied", "advanced-reasoning"]);
const allowedProvenances = new Set(["skillproof-curated", "source-derived"]);
const allowedQuestionVerification = new Set([
    "framework-supported-only",
    "interview-practice-supported",
    "candidate-reported-unverified",
    "verified"
]);
const allowedEvidenceStrengths = new Set(["direct", "strong-topic-match", "supporting-context"]);
const allowedInterviewSourceTypes = new Set([
    "company-published-interview-guidance",
    "interview-preparation-resource",
    "candidate-reported-interview"
]);
const approvedRubricLevels = ["Insufficient Evidence", "Beginner", "Intermediate", "Advanced"];

const questionIds = new Set();
const questionCountBySkill = new Map();
const difficultiesBySkill = new Map();

for (const sId of assessableSkillIds) {
    questionCountBySkill.set(sId, 0);
    difficultiesBySkill.set(sId, new Map([
        ["foundation", 0],
        ["applied", 0],
        ["advanced-reasoning", 0]
    ]));
}

if (questionsData.roleId !== "backend-developer") {
    errors.push(`questions.json roleId must be 'backend-developer' (got '${questionsData.roleId}').`);
}

if (!Array.isArray(questionsData.questions)) {
    errors.push("questions.json must contain a 'questions' array.");
} else {
    // Total questions invariant in Data Foundation v2: exactly 48 questions (16 skills × 3 questions)
    if (questionsData.questions.length !== 48) {
        errors.push(`questions.json must contain exactly 48 questions in v2 (got ${questionsData.questions.length}).`);
    }

    // Baseline Hardening: verify all 18 frozen Core questions against the immutable baseline snapshot & SHA-256 fingerprints
    if (baselineData && Array.isArray(baselineData.questions)) {
        if (baselineData.questions.length !== 18) {
            errors.push(`Baseline file must contain exactly 18 questions (got ${baselineData.questions.length}).`);
        }

        for (const baseQ of baselineData.questions) {
            const currentQ = questionsData.questions.find(q => q.id === baseQ.id);
            if (!currentQ) {
                errors.push(`[Frozen Core Question ${baseQ.id}] Missing completely from questions.json.`);
                continue;
            }

            // Verify each protected field individually
            for (const field of protectedFields) {
                if (field === "rubric") {
                    for (const level of approvedRubricLevels) {
                        const baseRubric = baseQ.rubric?.[level];
                        const currRubric = currentQ.rubric?.[level];
                        if (baseRubric !== currRubric) {
                            errors.push(`[Frozen Core Question ${baseQ.id}] Baseline mismatch on 'rubric.${level}':\n    Expected: "${baseRubric}"\n    Got:      "${currRubric}"`);
                        }
                    }
                } else if (Array.isArray(baseQ[field]) || (baseQ[field] && typeof baseQ[field] === "object")) {
                    const baseJson = JSON.stringify(baseQ[field]);
                    const currJson = JSON.stringify(currentQ[field]);
                    if (baseJson !== currJson) {
                        errors.push(`[Frozen Core Question ${baseQ.id}] Baseline mismatch on protected field '${field}':\n    Expected: ${baseJson}\n    Got:      ${currJson}`);
                    }
                } else {
                    if (baseQ[field] !== currentQ[field]) {
                        errors.push(`[Frozen Core Question ${baseQ.id}] Baseline mismatch on protected field '${field}':\n    Expected: "${baseQ[field]}"\n    Got:      "${currentQ[field]}"`);
                    }
                }
            }

            // Verify canonical SHA-256 fingerprint
            const actualCanonical = canonicalStringify(currentQ);
            const actualFingerprint = crypto.createHash("sha256").update(actualCanonical).digest("hex");
            const expectedFingerprint = FROZEN_CORE_QUESTION_FINGERPRINTS[baseQ.id];

            if (!expectedFingerprint) {
                errors.push(`[Frozen Core Question ${baseQ.id}] Missing expected canonical fingerprint in validator.`);
            } else if (actualFingerprint !== expectedFingerprint) {
                errors.push(`[Frozen Core Question ${baseQ.id}] Canonical SHA-256 fingerprint mismatch:\n    Expected: ${expectedFingerprint}\n    Got:      ${actualFingerprint}`);
            }
        }
    }

    // Validate general question invariants across all 48 questions
    questionsData.questions.forEach((q, idx) => {
        const qId = q.id;
        if (!qId || typeof qId !== "string" || !qId.trim()) {
            errors.push(`[questions.json item ${idx}] Question id must be a non-empty string.`);
        } else if (questionIds.has(qId)) {
            errors.push(`[Question ${qId}] Duplicate question ID detected.`);
        } else {
            questionIds.add(qId);
        }

        if (q.roleId !== "backend-developer") {
            errors.push(`[Question ${qId || idx}] roleId must be 'backend-developer'.`);
        }

        // Validate skillId
        if (!q.skillId || !assessableSkillIds.has(q.skillId)) {
            errors.push(`[Question ${qId || idx}] skillId '${q.skillId}' must be one of the assessable skills: ${[...assessableSkillIds].join(", ")}`);
        } else {
            if (languageSkillIds.has(q.skillId) && competencyIds.has(q.skillId)) {
                errors.push(`[Question ${qId || idx}] Language skill '${q.skillId}' collides with competency taxonomy.`);
            }
            const currentCount = questionCountBySkill.get(q.skillId) || 0;
            questionCountBySkill.set(q.skillId, currentCount + 1);
        }

        // Validate subskills
        const validSubskills = subskillsBySkill.get(q.skillId);
        if (!Array.isArray(q.subskills) || q.subskills.length === 0) {
            errors.push(`[Question ${qId || idx}] subskills must be a non-empty array.`);
        } else if (validSubskills) {
            q.subskills.forEach(sub => {
                if (!validSubskills.has(sub)) {
                    errors.push(`[Question ${qId || idx}] Subskill '${sub}' is not defined under skill '${q.skillId}' in skill-catalog.json.`);
                }
            });
        }

        // Validate difficulty
        if (!q.difficulty || !allowedDifficulties.has(q.difficulty)) {
            errors.push(`[Question ${qId || idx}] Invalid difficulty: '${q.difficulty}'. Allowed: ${[...allowedDifficulties].join(", ")}`);
        } else if (q.skillId && assessableSkillIds.has(q.skillId)) {
            const sDiffs = difficultiesBySkill.get(q.skillId);
            if (sDiffs) {
                const currentDiffCount = sDiffs.get(q.difficulty) || 0;
                sDiffs.set(q.difficulty, currentDiffCount + 1);
            }
        }

        // Validate questionType
        if (!allowedQuestionTypes.has(q.questionType)) {
            errors.push(`[Question ${qId || idx}] Invalid questionType: '${q.questionType}'. Allowed: ${[...allowedQuestionTypes].join(", ")}`);
        }

        // Validate question text
        if (!q.question || typeof q.question !== "string" || q.question.trim().length < 20) {
            errors.push(`[Question ${qId || idx}] question must be a non-empty substantive string of at least 20 characters.`);
        }

        // Validate expectedSignals
        if (!Array.isArray(q.expectedSignals) || q.expectedSignals.length < 2) {
            errors.push(`[Question ${qId || idx}] expectedSignals must be an array containing at least 2 key signals.`);
        } else {
            q.expectedSignals.forEach((sig, sIdx) => {
                if (typeof sig !== "string" || !sig.trim()) {
                    errors.push(`[Question ${qId || idx}] expectedSignal ${sIdx} must be a non-empty string.`);
                }
            });
        }

        // Validate Rubric
        if (!q.rubric || typeof q.rubric !== "object") {
            errors.push(`[Question ${qId || idx}] Missing rubric object.`);
        } else {
            const rubricKeys = Object.keys(q.rubric);
            if (rubricKeys.length !== 4) {
                errors.push(`[Question ${qId || idx}] Rubric must have exactly 4 levels (got ${rubricKeys.length}: ${rubricKeys.join(", ")}).`);
            }

            approvedRubricLevels.forEach(level => {
                const desc = q.rubric[level];
                if (!desc || typeof desc !== "string" || desc.trim().length < 15) {
                    errors.push(`[Question ${qId || idx}] Rubric level '${level}' must contain a substantive description of at least 15 characters.`);
                }
            });
        }

        // Validate framework source references
        if (!Array.isArray(q.frameworkSourceIds) || q.frameworkSourceIds.length === 0) {
            errors.push(`[Question ${qId || idx}] frameworkSourceIds must contain at least 1 valid source ID.`);
        } else {
            q.frameworkSourceIds.forEach(fId => {
                if (!sourceIds.has(fId)) {
                    errors.push(`[Question ${qId || idx}] frameworkSourceId '${fId}' does not exist in sources.json.`);
                } else if (invalidSourceIds.has(fId)) {
                    errors.push(`[Question ${qId || idx}] frameworkSourceId '${fId}' references an unavailable/rejected source.`);
                }
            });
        }

        // Validate interview evidence IDs
        if (!Array.isArray(q.interviewEvidenceIds)) {
            errors.push(`[Question ${qId || idx}] interviewEvidenceIds must be an array.`);
        } else {
            q.interviewEvidenceIds.forEach(iId => {
                if (!sourceIds.has(iId)) {
                    errors.push(`[Question ${qId || idx}] interviewEvidenceId '${iId}' does not exist in sources.json.`);
                } else if (invalidSourceIds.has(iId)) {
                    errors.push(`[Question ${qId || idx}] interviewEvidenceId '${iId}' references an unavailable/rejected source.`);
                } else if (referenceOnlySourceIds.has(iId)) {
                    errors.push(`[Question ${qId || idx}] interviewEvidenceId '${iId}' references a reference-only source.`);
                } else {
                    const src = sourcesById.get(iId);
                    if (src && !allowedInterviewSourceTypes.has(src.sourceType)) {
                        errors.push(`[Question ${qId || idx}] interviewEvidenceId '${iId}' references source of type '${src.sourceType}', which is not an interview evidence type.`);
                    }
                }
            });
        }

        // Validate structured interviewEvidence objects
        if (q.interviewEvidence !== undefined) {
            if (!Array.isArray(q.interviewEvidence)) {
                errors.push(`[Question ${qId || idx}] interviewEvidence must be an array.`);
            } else {
                const evSourceIds = new Set();
                q.interviewEvidence.forEach((ev, eIdx) => {
                    if (!ev || typeof ev !== "object") {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] Evidence item must be an object.`);
                        return;
                    }

                    if (!ev.sourceId || !sourceIds.has(ev.sourceId)) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] Invalid or missing sourceId: '${ev.sourceId}'.`);
                    } else if (evSourceIds.has(ev.sourceId)) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] Duplicate evidence for sourceId '${ev.sourceId}'.`);
                    } else {
                        evSourceIds.add(ev.sourceId);
                        if (invalidSourceIds.has(ev.sourceId)) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] Evidence source '${ev.sourceId}' is unavailable/rejected.`);
                        }
                        if (referenceOnlySourceIds.has(ev.sourceId)) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] Evidence source '${ev.sourceId}' is reference-only.`);
                        }
                        const refSrc = sourcesById.get(ev.sourceId);
                        if (refSrc && !allowedInterviewSourceTypes.has(refSrc.sourceType)) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] Source '${ev.sourceId}' is type '${refSrc.sourceType}', not interview guidance/prep.`);
                        }
                        if (refSrc && ev.evidenceType !== refSrc.sourceType) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] evidenceType '${ev.evidenceType}' must match source's sourceType '${refSrc.sourceType}'.`);
                        }
                    }

                    if (!ev.canonicalUrl || typeof ev.canonicalUrl !== "string" || !ev.canonicalUrl.startsWith("http")) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] canonicalUrl must be a valid HTTP/HTTPS URL.`);
                    }

                    if (!ev.locator || typeof ev.locator !== "string" || ev.locator.trim().length < 5) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] locator must be a substantive section/heading string.`);
                    }

                    if (!ev.verifiedAt || typeof ev.verifiedAt !== "string") {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] verifiedAt date is required for claim evidence.`);
                    }

                    if (!Array.isArray(ev.supportedTopics) || ev.supportedTopics.length === 0) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] supportedTopics must be a non-empty array.`);
                    }

                    if (!allowedEvidenceStrengths.has(ev.evidenceStrength)) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] Invalid evidenceStrength: '${ev.evidenceStrength}'. Allowed: ${[...allowedEvidenceStrengths].join(", ")}`);
                    }

                    if (!ev.notes || typeof ev.notes !== "string" || ev.notes.trim().length < 15) {
                        errors.push(`[Question ${qId || idx} evidence ${eIdx}] notes must be a string of at least 15 characters.`);
                    }
                });

                // Ensure interviewEvidenceIds and interviewEvidence are consistent
                if (Array.isArray(q.interviewEvidenceIds)) {
                    for (const id of q.interviewEvidenceIds) {
                        if (!evSourceIds.has(id)) {
                            errors.push(`[Question ${qId || idx}] interviewEvidenceIds contains '${id}' but it is missing from interviewEvidence objects.`);
                        }
                    }
                    for (const id of evSourceIds) {
                        if (!q.interviewEvidenceIds.includes(id)) {
                            errors.push(`[Question ${qId || idx}] interviewEvidence contains '${id}' but it is missing from interviewEvidenceIds array.`);
                        }
                    }
                }
            }
        }

        // Validate provenance
        if (!allowedProvenances.has(q.provenance)) {
            errors.push(`[Question ${qId || idx}] Invalid provenance: '${q.provenance}'. Allowed: ${[...allowedProvenances].join(", ")}`);
        }

        // Validate verificationStatus
        if (!allowedQuestionVerification.has(q.verificationStatus)) {
            errors.push(`[Question ${qId || idx}] Invalid verificationStatus: '${q.verificationStatus}'. Allowed: ${[...allowedQuestionVerification].join(", ")}`);
        }

        // Rule: If no interview evidence, must be framework-supported-only
        if (Array.isArray(q.interviewEvidenceIds) && q.interviewEvidenceIds.length === 0) {
            if (q.verificationStatus !== "framework-supported-only") {
                errors.push(`[Question ${qId || idx}] Questions with no interview evidence must be marked as 'framework-supported-only' (got '${q.verificationStatus}').`);
            }
        }

        // Rule: If interview evidence exists, verificationStatus must reflect interview support
        if (Array.isArray(q.interviewEvidenceIds) && q.interviewEvidenceIds.length > 0) {
            if (q.verificationStatus === "framework-supported-only") {
                errors.push(`[Question ${qId || idx}] Questions with interview evidence cannot be marked as 'framework-supported-only'.`);
            }
        }
    });

    // Verify exactly 3 questions per assessable skill (1 foundation, 1 applied, 1 advanced-reasoning)
    for (const [sId, count] of questionCountBySkill.entries()) {
        if (count !== 3) {
            errors.push(`Assessable skill '${sId}' must have exactly 3 questions (found ${count}).`);
        }
        const diffMap = difficultiesBySkill.get(sId);
        if (diffMap) {
            for (const diffLevel of ["foundation", "applied", "advanced-reasoning"]) {
                const dCount = diffMap.get(diffLevel) || 0;
                if (dCount !== 1) {
                    errors.push(`Assessable skill '${sId}' must have exactly 1 '${diffLevel}' question (found ${dCount}).`);
                }
            }
        }
    }
}

// -------------------------------------------------------------
// 5. Output Summary and Coverage Metrics
// -------------------------------------------------------------
if (errors.length > 0) {
    console.error(`\nFAIL: Backend assessment dataset validation failed with ${errors.length} error(s):`);
    for (const err of errors) {
        console.error(`  - ${err}`);
    }
    process.exit(1);
}

// Compute coverage metrics
const skillCoverage = new Map();
for (const sId of assessableSkillIds) {
    skillCoverage.set(sId, 0);
}
let frameworkSupportedCount = 0;
let interviewPracticeSupportedCount = 0;
let frameworkOnlyCount = 0;
let companyPublishedSupportedCount = 0;
let candidateReportedSupportedCount = 0;

questionsData.questions.forEach(q => {
    if (q.frameworkSourceIds && q.frameworkSourceIds.length > 0) {
        frameworkSupportedCount++;
    }
    if (q.interviewEvidenceIds && q.interviewEvidenceIds.length > 0) {
        interviewPracticeSupportedCount++;
        const current = skillCoverage.get(q.skillId) || 0;
        skillCoverage.set(q.skillId, current + 1);
    } else {
        frameworkOnlyCount++;
    }
    const hasCompanyPub = (q.interviewEvidence || []).some(ev => ev.evidenceType === "company-published-interview-guidance");
    if (hasCompanyPub) {
        companyPublishedSupportedCount++;
    }
    const hasCandidateRep = (q.interviewEvidence || []).some(ev => ev.evidenceType === "candidate-reported-interview");
    if (hasCandidateRep) {
        candidateReportedSupportedCount++;
    }
});

console.log("\n==================================================");
console.log("PASS: Backend Assessment Data Foundation v2 is Valid!");
console.log("==================================================");
console.log(`Role: ${catalogData.roleId}`);
console.log(`Total Skills in Catalog: ${allSkillIds.size}`);
console.log(`  - Core Competencies: ${coreCompetencyIds.size}`);
console.log(`  - Recommended Competencies: ${recommendedCompetencyIds.size}`);
console.log(`  - Optional Competencies: ${optionalCompetencyIds.size}`);
console.log(`  - Programming Languages: ${languageSkillIds.size}`);
console.log(`Sources: ${sourcesData.sources.length}`);
console.log(`Questions: ${questionsData.questions.length} (Exactly 3 per Assessable Skill: 16 skills × 3 = 48)`);
console.log("--------------------------------------------------");
console.log("FROZEN DATA FOUNDATION v2.0 BASELINE INTEGRITY:");
console.log(`  - Baseline Snapshot: ${path.relative(rootDir, baselinePath)}`);
console.log(`  - SHA-256 File Checksum: ${EXPECTED_BASELINE_FILE_SHA256} (MATCH)`);
console.log(`  - Core Questions Verified: 18/18 identical to v2.0 baseline`);
console.log(`  - Protected Fields: ${protectedFields.join(", ")}`);
console.log(`  - Canonical Fingerprints: 18/18 MATCH`);
console.log("--------------------------------------------------");
console.log("EVIDENCE COVERAGE BY CORE COMPETENCY:");
for (const compId of coreCompetencyIds) {
    console.log(`  - ${compId}: ${skillCoverage.get(compId)}/3`);
}
console.log("EVIDENCE COVERAGE BY RECOMMENDED COMPETENCY:");
for (const compId of recommendedCompetencyIds) {
    console.log(`  - ${compId}: ${skillCoverage.get(compId)}/3`);
}
console.log("EVIDENCE COVERAGE BY PROGRAMMING LANGUAGE:");
for (const langId of languageSkillIds) {
    console.log(`  - ${langId}: ${skillCoverage.get(langId)}/3`);
}
console.log("--------------------------------------------------");
console.log(`Total questions: ${questionsData.questions.length}`);
console.log(`framework-supported questions: ${frameworkSupportedCount}/${questionsData.questions.length}`);
console.log(`interview-practice-supported questions: ${interviewPracticeSupportedCount}/${questionsData.questions.length}`);
console.log(`framework-supported-only questions: ${frameworkOnlyCount}/${questionsData.questions.length}`);
console.log(`company-published supported questions: ${companyPublishedSupportedCount}/${questionsData.questions.length}`);
console.log(`candidate-reported supported questions: ${candidateReportedSupportedCount}/${questionsData.questions.length}`);
console.log("==================================================\n");
process.exit(0);
