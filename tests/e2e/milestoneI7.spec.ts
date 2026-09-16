import { test, expect } from '@playwright/test';

test.describe('Milestone I7: Evidence-Based Skill Profile & Gap Analysis E2E Flow', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);
    });

    test('Completed adaptive assessment renders explainable Career Readiness Profile and Gap Analysis', async ({ page }) => {
        // 1. Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // 2. Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Toggle all Core off
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Select only SQL competency
        await page.getByTestId('skill-toggle-sql').click();

        // Select primary language Python
        await page.getByTestId('custom-language-card-python').click();

        // 3. Start Adaptive Diagnostic
        const startAdaptiveBtn = page.getByTestId('start-adaptive-assessment-button');
        await expect(startAdaptiveBtn).toBeVisible();
        await startAdaptiveBtn.click();

        // 4. Verify Stage 1 of Skill 1 (SQL - Applied)
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
        await expect(page.getByText('Skill 1 of 2: SQL & Relational Databases')).toBeVisible();
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();

        // Answer SQL Applied (substantive answer -> branches to advanced-reasoning)
        const answerInput = page.getByTestId('adaptive-answer-input');
        await answerInput.fill('I would inspect the query execution plan with EXPLAIN ANALYZE, verify index usage on high-cardinality columns, and add composite B-tree indexes.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 5. Verify Stage 2 of Skill 1 (SQL - Follow-up: Advanced)
        await expect(page.getByText('Skill 1 of 2: SQL & Relational Databases')).toBeVisible();
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();

        // Answer SQL Follow-up
        await answerInput.fill('To resolve phantom reads and write skew, I would use Serializable isolation or SELECT FOR UPDATE pessimistic locking, with MVCC retry loops.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 6. Verify Stage 1 of Skill 2 (Python - Applied)
        await expect(page.getByText('Skill 2 of 2: Python')).toBeVisible();
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();

        // Answer Python Applied
        await answerInput.fill('FastAPI with async def endpoints enables non-blocking I/O using asyncio event loop and uvicorn workers.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 7. Verify Stage 2 of Skill 2 (Python - Follow-up)
        await expect(page.getByText('Skill 2 of 2: Python')).toBeVisible();
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();

        // Answer Python Follow-up
        await answerInput.fill('Python GIL restricts execution to one thread per interpreter, requiring multiprocessing or ProcessPoolExecutor for CPU-bound tasks.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 8. Transition to Career Readiness Skill Profile
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();

        // 9. Verify Neutral Profile Summary Banner
        const summaryBanner = page.getByTestId('career-readiness-summary');
        await expect(summaryBanner).toBeVisible();
        await expect(summaryBanner).toContainText('Career Readiness Evidence Summary');
        await expect(summaryBanner).toContainText('Demonstrated qualitative evidence across assessed competencies');

        // Verify no arbitrary scores or percentages
        const summaryText = await summaryBanner.innerText();
        expect(summaryText).not.toContain('%');
        expect(summaryText).not.toContain('/100');

        // 10. Verify SQL Card
        const sqlCard = page.getByTestId('skill-card-sql');
        await expect(sqlCard).toBeVisible();
        await expect(sqlCard.getByTestId('skill-level-sql')).toBeVisible();

        // Verify Demonstrated Evidence for SQL
        const sqlStrengths = sqlCard.getByTestId('demonstrated-strengths-sql');
        await expect(sqlStrengths).toBeVisible();
        await expect(sqlStrengths).toContainText('Demonstrated Evidence');

        // Verify Next Development Areas for SQL with catalog concepts
        const sqlNextAreas = sqlCard.getByTestId('next-development-areas-sql');
        await expect(sqlNextAreas).toBeVisible();
        await expect(sqlNextAreas).toContainText('Next Development Areas:');

        // Verify "Why this level?" explainability accordion for SQL
        const sqlExplainToggle = sqlCard.getByTestId('explainability-toggle-sql');
        await expect(sqlExplainToggle).toBeVisible();
        await sqlExplainToggle.click();

        const sqlExplainContent = sqlCard.getByTestId('explainability-content-sql');
        await expect(sqlExplainContent).toBeVisible();
        await expect(sqlExplainContent).toContainText('Candidate Reasoning Observed:');
        await expect(sqlExplainContent).toContainText('Assessed via questions:');
        await expect(sqlExplainContent).toContainText('Evaluator scoring rubrics remain private');

        // 11. Verify Python Card
        const pythonCard = page.getByTestId('skill-card-python');
        await expect(pythonCard).toBeVisible();
        await expect(pythonCard.getByTestId('skill-level-python')).toBeVisible();

        // 12. Verify Top Prioritized Skill Gaps Banner (max 3)
        const topGapsList = page.getByTestId('top-gaps-list');
        await expect(topGapsList).toBeVisible();

        // 13. Verify Roadmap Generation Action Card
        const roadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(roadmapBtn).toBeVisible();

        // 14. Strict Server-Side Privacy: Page must NOT leak internal rubrics or prompts
        const html = await page.content();
        expect(html).not.toContain('expectedSignals');
        expect(html).not.toContain('insufficientEvidenceCriteria');
        expect(html).not.toContain('systemInstructions');
    });

    test('Insufficient Evidence renders neutral wording without negative labeling', async ({ page }) => {
        // Start Career Diagnostic for Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Custom mode with only SQL and Python
        await page.getByTestId('mode-custom-tab').click();
        await page.getByRole('button', { name: /Toggle All Core/i }).click();
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('custom-language-card-python').click();
        await page.getByTestId('start-adaptive-assessment-button').click();

        const answerInput = page.getByTestId('adaptive-answer-input');

        // SQL Stage 1: empty answer
        await answerInput.fill(' ');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // SQL Stage 2: empty answer
        await answerInput.fill(' ');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Python Stage 1: empty answer
        await answerInput.fill(' ');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Python Stage 2: empty answer
        await answerInput.fill(' ');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Verify profile loads
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();

        const sqlCard = page.getByTestId('skill-card-sql');
        await expect(sqlCard).toBeVisible();
        await expect(sqlCard.getByTestId('skill-level-sql')).toHaveText('Insufficient Evidence');

        // Verify neutral callout notice
        const notice = sqlCard.getByTestId('insufficient-evidence-notice-sql');
        await expect(notice).toBeVisible();
        await expect(notice).toContainText('We did not collect enough evidence to determine your current level');
        await expect(notice).toContainText('indicates missing diagnostic data, not a lack of ability or failure');

        // Verify no pejorative labels
        const cardText = await sqlCard.innerText();
        expect(cardText).not.toContain('Failed');
        expect(cardText).not.toContain('Weak');
        expect(cardText).not.toContain('0%');
    });
});
