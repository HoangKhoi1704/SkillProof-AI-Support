import { test, expect } from '@playwright/test';

test.describe('Milestone I6: Adaptive Career Readiness Diagnostic E2E Flow', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);
    });

    test('Custom Mode: Adaptive flow for SQL + Python branches and completes to Skill Profile', async ({ page }) => {
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
        const adaptiveBtn = page.getByTestId('start-adaptive-assessment-button');
        await expect(adaptiveBtn).toBeVisible();
        await adaptiveBtn.click();

        // 4. Verify Adaptive Diagnostic view loaded
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
        await expect(page.getByText('Backend Developer Adaptive Diagnostic')).toBeVisible();
        await expect(page.getByText('⚡ Adaptive Engine')).toBeVisible();

        // Verify neutral intro banner
        await expect(page.getByText(/Targeted Competency Diagnostic/i)).toBeVisible();
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();

        // Skill 1 (SQL): Applied question
        await expect(page.getByText(/Skill 1 of 2: SQL/i)).toBeVisible();
        await expect(page.getByTestId('adaptive-difficulty-badge')).toHaveText('applied');

        // 5. Submit answer for SQL Applied
        const answerInput = page.getByTestId('adaptive-answer-input');
        await answerInput.fill('To diagnose slow database queries, I check query execution plans and index lookups.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 6. Verify SQL follow-up question loads
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();
        await expect(page.getByText(/Skill 1 of 2: SQL/i)).toBeVisible();
        const followUpDifficulty = page.getByTestId('adaptive-difficulty-badge');
        await expect(followUpDifficulty).toHaveText(/foundation|advanced-reasoning/);

        // Submit answer for SQL follow-up
        await answerInput.fill('Primary keys enforce row uniqueness while foreign keys ensure referential integrity.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 7. Verify Skill 2 (Python) Applied question loads
        await expect(page.getByText('Skill 2 of 2: Python')).toBeVisible();
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();
        await expect(page.getByTestId('adaptive-difficulty-badge')).toHaveText('applied');

        // Submit answer for Python Applied
        await answerInput.fill('FastAPI uses asynchronous route handlers with Pydantic for validation and AsyncIO event loops.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 8. Verify Python follow-up loads
        await expect(page.getByText('Skill 2 of 2: Python')).toBeVisible();
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();

        // Submit answer for Python follow-up
        await answerInput.fill('Python GIL restricts pure execution to one thread per interpreter, requiring multiprocessing for CPU-bound tasks.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 9. Verify transition to Qualitative Skill Profile
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();
        await expect(page.getByText(/SQL/i).first()).toBeVisible();
        await expect(page.getByText(/Python/i).first()).toBeVisible();

        // Verify top gaps banner
        await expect(page.getByRole('heading', { name: /Top Prioritized Skill Gaps/i })).toBeVisible();

        // Verify Roadmap generation button is available and works
        const roadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(roadmapBtn).toBeVisible();
    });

    test('Recommended Mode: Adaptive session initializes with 7 skills and allows navigation back to setup', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Select primary language C#
        await page.getByTestId('language-card-csharp').click();

        // Click Start Adaptive Diagnostic
        await page.getByTestId('start-adaptive-assessment-button').click();

        // Verify Adaptive Diagnostic view loads with 7 total skills
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
        await expect(page.getByText(/Skill 1 of 7:/i)).toBeVisible();
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();

        // Click Setup to navigate back
        await page.getByTestId('adaptive-back-to-setup-button').click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();
    });

    test('Preservation: Clicking Standard Assessment preserves existing single-applied question flow', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Select primary language C#
        await page.getByTestId('language-card-csharp').click();

        // Click Standard Assessment button
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify standard question view loads with Question 1 of 7
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 7')).toBeVisible();
        await expect(page.getByText('applied', { exact: true })).toBeVisible();
    });

    test('Privacy: Adaptive Diagnostic UI never exposes internal server rubrics or expected signals', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await page.getByTestId('language-card-csharp').click();
        await page.getByTestId('start-adaptive-assessment-button').click();
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();

        // Verify HTML content does not leak server-side rubric or expected signals
        const html = await page.content();
        expect(html).not.toContain('expectedSignals');
        expect(html).not.toContain('rubric');
        expect(html).not.toContain('insufficientEvidence:');
        expect(html).not.toContain('systemInstructions');
    });
});
