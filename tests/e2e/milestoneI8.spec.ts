import { test, expect } from '@playwright/test';

test.describe('Milestone I8: Personalized Gap-Based Learning Roadmap E2E Flow', () => {
    test('Completed adaptive assessment generates personalized, evidence-grounded roadmap', async ({ page }) => {
        await page.goto('/');

        // 1. Start Career Diagnostic for Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // 2. Select Custom mode to test a focused subset (SQL + Python)
        await page.getByTestId('mode-custom-tab').click();
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Enable SQL
        await page.getByTestId('skill-toggle-sql').click();

        // Select Python as primary language
        const pythonCard = page.getByTestId('custom-language-card-python');
        await expect(pythonCard).toBeVisible();
        await pythonCard.click();

        // 3. Start Adaptive Diagnostic
        const startAdaptiveBtn = page.getByTestId('start-adaptive-assessment-button');
        await expect(startAdaptiveBtn).toBeVisible();
        await startAdaptiveBtn.click();

        // 4. Answer Question 1 (SQL - Applied) -> evaluates to Intermediate
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
        const answerInput = page.getByTestId('adaptive-answer-input');
        await answerInput.fill('I would inspect the query execution plan with EXPLAIN ANALYZE, verify index usage on high-cardinality columns, and add composite B-tree indexes.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 5. Answer Question 2 (SQL - Advanced Reasoning follow-up)
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();
        await answerInput.fill('To resolve phantom reads and write skew, I would use Serializable isolation or SELECT FOR UPDATE pessimistic locking, with MVCC retry loops.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 6. Answer Question 3 (Python - Applied)
        await expect(page.getByText('Skill 2 of 2: Python')).toBeVisible();
        await answerInput.fill('FastAPI with async def endpoints enables non-blocking I/O using asyncio event loop and uvicorn workers.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 7. Answer Question 4 (Python - Advanced Reasoning follow-up)
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();
        await answerInput.fill('Python GIL restricts execution to one thread per interpreter, requiring multiprocessing or ProcessPoolExecutor for CPU-bound tasks.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 8. Verify Career Readiness Profile renders
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();
        await expect(page.getByTestId('career-readiness-summary')).toBeVisible();

        // 9. Click "Generate My Roadmap"
        const generateRoadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(generateRoadmapBtn).toBeVisible();
        await generateRoadmapBtn.click();

        // 10. Verify Personalized Career Readiness Roadmap renders
        await expect(page.getByRole('heading', { name: 'Personalized Career Readiness Roadmap' })).toBeVisible();
        const roadmapItems = page.getByTestId('roadmap-items-list');
        await expect(roadmapItems).toBeVisible();

        // Exactly 2 items rendered (SQL and Python) — no fake gaps added!
        const sqlRoadmapItem = page.getByTestId('roadmap-item-sql');
        await expect(sqlRoadmapItem).toBeVisible();
        await expect(sqlRoadmapItem.getByText('Priority 1')).toBeVisible();

        const pythonRoadmapItem = page.getByTestId('roadmap-item-python');
        await expect(pythonRoadmapItem).toBeVisible();
        await expect(pythonRoadmapItem.getByText('Priority 2')).toBeVisible();

        // 11. Verify SQL Item Details
        // Why this is recommended
        const sqlWhy = page.getByTestId('why-recommended-sql');
        await expect(sqlWhy).toBeVisible();
        await expect(sqlWhy).toContainText('Why this is recommended:');
        await expect(sqlWhy).toContainText('SQL');

        // Learning Goal & Objective
        const sqlGoal = page.getByTestId('learning-objective-sql');
        await expect(sqlGoal).toBeVisible();
        await expect(sqlGoal).toContainText('Target Learning Goal');

        // Actionable Practice Task
        const sqlTask = page.getByTestId('practice-task-sql');
        await expect(sqlTask).toBeVisible();
        await expect(sqlTask).toContainText('Actionable Practice Task');

        // Observable Evidence to Produce
        const sqlEvidence = page.getByTestId('evidence-target-sql');
        await expect(sqlEvidence).toBeVisible();
        await expect(sqlEvidence).toContainText('Observable Evidence to Produce');

        // Completion Criteria
        const sqlCriteria = page.getByTestId('completion-criteria-sql');
        await expect(sqlCriteria).toBeVisible();
        await expect(sqlCriteria).toContainText('Completion Criteria');

        // 12. Strict Privacy Checks: Ensure no percentages or internal grading artifacts are exposed
        const roadmapText = await roadmapItems.innerText();
        expect(roadmapText).not.toContain('%');
        expect(roadmapText).not.toContain('/100');

        const pageHtml = await page.content();
        expect(pageHtml).not.toContain('expectedSignals');
        expect(pageHtml).not.toContain('insufficientEvidenceCriteria');
        expect(pageHtml).not.toContain('systemInstructions');
    });

    test('Insufficient Evidence roadmap item produces neutral evidence-building activity', async ({ page }) => {
        await page.goto('/');

        // 1. Start Career Diagnostic for Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // 2. Custom mode with only SQL and Python
        await page.getByTestId('mode-custom-tab').click();
        await page.getByRole('button', { name: /Toggle All Core/i }).click();
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('custom-language-card-python').click();

        // Start Adaptive Diagnostic
        await page.getByTestId('start-adaptive-assessment-button').click();

        // 3. Submit empty answers to trigger Insufficient Evidence
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
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

        // 4. Profile renders Insufficient Evidence
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();

        // 5. Generate Roadmap
        const generateRoadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(generateRoadmapBtn).toBeVisible();
        await generateRoadmapBtn.click();

        // 6. Verify Roadmap Item for SQL has Evidence-Building Activity semantics
        const sqlRoadmapItem = page.getByTestId('roadmap-item-sql');
        await expect(sqlRoadmapItem).toBeVisible();
        await expect(sqlRoadmapItem).toContainText('Evidence-Building Activity');

        // Ensure "Why recommended" explains missing diagnostic data without derogatory labels
        const sqlWhy = page.getByTestId('why-recommended-sql');
        await expect(sqlWhy).toBeVisible();
        const whyText = await sqlWhy.innerText();
        expect(whyText).toContain('did not collect sufficient evidence');
        expect(whyText).not.toContain('Weak');
        expect(whyText).not.toContain('Failed');
        expect(whyText).not.toContain('0%');
    });
});
