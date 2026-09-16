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
const baselinePath = path.resolve(__dirname, "../baselines/v1.2-core-questions-baseline.json");

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
const baselineData = loadJson(baselinePath, "v1.2-core-questions-baseline.json");

if (errors.length > 0) {
    console.error("FAIL: Unable to load assessment dataset files:");
    for (const err of errors) console.error(` - ${err}`);
    process.exit(1);
}

// -------------------------------------------------------------
// 1. Validate Sources Catalog (sources.json)
// -------------------------------------------------------------
const sourceIds = new Set();
const sourcesById = new Map();
const allowedSourceTypes = new Set([
    "career-framework",
    "official-standard",
    "official-documentation",
    "professional-reference",
    "interview-preparation-resource",
    "candidate-reported-interview",
    "company-published-interview-guidance"
]);

const allowedVerificationStatuses = new Set([
    "verified-official-publication",
    "community-curated-verified",
    "candidate-reported-unverified"
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

        if (!allowedSourceTypes.has(src.sourceType)) {
            errors.push(`[Source ${id || idx}] Invalid sourceType: '${src.sourceType}'. Allowed: ${[...allowedSourceTypes].join(", ")}`);
        }

        if (!allowedVerificationStatuses.has(src.verificationStatus)) {
            errors.push(`[Source ${id || idx}] Invalid verificationStatus: '${src.verificationStatus}'. Allowed: ${[...allowedVerificationStatuses].join(", ")}`);
        }

        if (!src.accessedAt || typeof src.accessedAt !== "string") {
            errors.push(`[Source ${id || idx}] accessedAt must be a valid date string.`);
        }

        if (src.sourceType === "candidate-reported-interview" && src.verificationStatus !== "candidate-reported-unverified") {
            errors.push(`[Source ${id || idx}] Candidate-reported interviews must be marked as candidate-reported-unverified.`);
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
// 3. Frozen Data Foundation v1.2 Core Baseline & Integrity Lock
// -------------------------------------------------------------
const EXPECTED_BASELINE_FILE_SHA256 = "449b8db3d8b21ea9bac4567fef9fe5df85212a25503e62856fe3ae5792ed7e1b";

const FROZEN_CORE_QUESTION_FINGERPRINTS = {
    "q-be-prog-01": "b9f57c02769f5597bdfddf2dd8e7211db7db473ed72fa4380e0819d5b3d9e937",
    "q-be-prog-02": "befac0b1d617805add91b4171fe251843f55254e73af61e51bcd38484dcfd8e3",
    "q-be-prog-03": "67724f2856a2ca0824680c8cc1b809d6587e8d723b62885c14e905bbb86b607f",
    "q-be-rest-01": "8bbab3045319cd42125a3a5ae6549dd6565fb1f706dffa1e6ec8146b60499249",
    "q-be-rest-02": "73a5f56ea78ad6144e4d9ca5fad0d5ec09e55b89d5c5b648095ff1c477cfa2d0",
    "q-be-rest-03": "bec29f920ce7bcd0b03368e21b67e3c06d87f99319574e133a916e2abb3a4816",
    "q-be-sql-01": "bbceca980d30b892d2d78b2fce5f33b5ce716abb635ac702cb7f99c439bc2e70",
    "q-be-sql-02": "f3e38beb77806b167d67e14b045430fca031a634bd8b795093fbd73d9958d4ad",
    "q-be-sql-03": "a7cc43d56260a4d495f24f11d59107d95c32c27238475a104a7eaa0dff69f154",
    "q-be-test-01": "56f0e4d76bedd2dfdb34b931da3cd3bf852576a412bbd363fb40c6a6b25aa88c",
    "q-be-test-02": "11475e62639102223b879f07c149d2560382df9dacdf3e49ba1562441b18fdb8",
    "q-be-test-03": "4322a5204c2d1c6a692f09745603e30e2977a438e4fb364f37669b2c01f92319",
    "q-be-auth-01": "abfecdddeb1015f834591f161358abc761bf19d44e665e64c6af0b0d21908973",
    "q-be-auth-02": "a093cf3cf6a104e5baddd8fbb62345433be63fd19f1f7e4a072c45092bf63aa1",
    "q-be-auth-03": "9cf612a47f1fbc996441b0efc1f483a9583edf4244ec1199baa6fde2efb14bef",
    "q-be-sys-01": "239038b373b1e11da775fd8a55747242d4e1b90b92dd78fd192cc1ec45b5dfc5",
    "q-be-sys-02": "d8f95ac5c2325bf9b741beaf5706e555c98cfd200a06334ea6aa5aed012730e7",
    "q-be-sys-03": "ba4d661ea2240196af0cab158c48f56cd82c96559fcf5a7b2eefd2d3440706d7"
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
            // Verify semantic category purity: language questions map to language skills, not fake competencies
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
                    } else {
                        evSourceIds.add(ev.sourceId);
                        const refSrc = sourcesById.get(ev.sourceId);
                        if (refSrc && !allowedInterviewSourceTypes.has(refSrc.sourceType)) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] Source '${ev.sourceId}' is type '${refSrc.sourceType}', not interview guidance/prep.`);
                        }
                        if (refSrc && ev.evidenceType !== refSrc.sourceType) {
                            errors.push(`[Question ${qId || idx} evidence ${eIdx}] evidenceType '${ev.evidenceType}' must match source's sourceType '${refSrc.sourceType}'.`);
                        }
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
console.log("FROZEN DATA FOUNDATION v1.2 BASELINE INTEGRITY:");
console.log(`  - Baseline Snapshot: ${path.relative(rootDir, baselinePath)}`);
console.log(`  - SHA-256 File Checksum: ${EXPECTED_BASELINE_FILE_SHA256} (MATCH)`);
console.log(`  - Core Questions Verified: 18/18 identical to v1.2 baseline`);
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
