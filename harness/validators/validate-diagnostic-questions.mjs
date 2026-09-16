import fs from "node:fs";

const file = process.argv[2];

if (!file) {
    console.error(
        "Usage: node harness/validators/validate-diagnostic-questions.mjs <json-file>"
    );
    process.exit(2);
}

let data;

try {
    data = JSON.parse(fs.readFileSync(file, "utf8"));
} catch {
    console.error("FAIL: invalid JSON");
    process.exit(1);
}

if (!Array.isArray(data)) {
    console.error("FAIL: Expected an array of questions");
    process.exit(1);
}

const allowedRoles = new Set(["backend-developer", "financial-analyst"]);
const allowedTypes = new Set(["knowledge", "interview", "practical_case", "reasoning"]);
const allowedSourceTypes = new Set(["team_curated", "public_interview_resource", "company_interview_source"]);

const errors = [];

data.forEach((q, idx) => {
    if (typeof q.id !== "number" || !Number.isInteger(q.id)) {
        errors.push(`[Item ${idx}] id must be an integer`);
    }
    if (!allowedRoles.has(q.careerRoleId)) {
        errors.push(`[Item ${idx}] invalid careerRoleId: ${q.careerRoleId}`);
    }
    if (typeof q.competency !== "string" || !q.competency.trim()) {
        errors.push(`[Item ${idx}] competency must be a non-empty string`);
    }
    if (!allowedTypes.has(q.type)) {
        errors.push(`[Item ${idx}] invalid type: ${q.type}`);
    }
    if (typeof q.questionText !== "string" || q.questionText.trim().length < 10) {
        errors.push(`[Item ${idx}] questionText must be at least 10 chars`);
    }
    if (!allowedSourceTypes.has(q.sourceType)) {
        errors.push(`[Item ${idx}] invalid sourceType: ${q.sourceType}`);
    }
    if (typeof q.sourceReference !== "string" || !q.sourceReference.trim()) {
        errors.push(`[Item ${idx}] sourceReference must be non-empty`);
    }
    // Invariant: public questions must NOT expose rubrics
    if (q.rubric !== undefined) {
        errors.push(`[Item ${idx}] public question DTO MUST NOT contain rubric`);
    }
});

if (errors.length > 0) {
    console.error("FAIL");
    for (const error of errors) {
        console.error(`- ${error}`);
    }
    process.exit(1);
}

console.log(`PASS: Validated ${data.length} public diagnostic questions successfully.`);
