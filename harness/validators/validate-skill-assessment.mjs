import fs from "node:fs";

const file = process.argv[2];

if (!file) {
    console.error(
        "Usage: node harness/validators/validate-skill-assessment.mjs <json-file>"
    );
    process.exit(2);
}

const allowedLevels = new Set([
    "Beginner",
    "Intermediate",
    "Advanced",
    "Insufficient Evidence",
]);

let data;

try {
    data = JSON.parse(fs.readFileSync(file, "utf8"));
} catch {
    console.error("FAIL: invalid JSON");
    process.exit(1);
}

const errors = [];

if (typeof data.competency !== "string" || !data.competency.trim()) {
    errors.push("competency must be a non-empty string");
}

if (!allowedLevels.has(data.level)) {
    errors.push(
        `level must be one of: ${[...allowedLevels].join(", ")}`
    );
}

if (typeof data.reason !== "string" || !data.reason.trim()) {
    errors.push("reason must be a non-empty string");
}

if (!Array.isArray(data.evidence)) {
    errors.push("evidence must be an array");
}

if (errors.length > 0) {
    console.error("FAIL");

    for (const error of errors) {
        console.error(`- ${error}`);
    }

    process.exit(1);
}

console.log("PASS: Skill assessment output is valid.");