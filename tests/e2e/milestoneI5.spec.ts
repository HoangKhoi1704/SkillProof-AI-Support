import { test, expect } from '@playwright/test';

test.describe('Milestone I5: Developer AI Inspector & Diagnostic Observability', () => {

    test('Normal User Experience: Does NOT expose AI Inspector links, rubrics, or expected signals', async ({ page }) => {
        // Navigate to public root
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);

        // Verify public page does NOT contain links or navigation to developer inspector
        const inspectorLinks = page.locator('a[href*="/dev/ai-inspector"], a[href*="ai-inspector"]');
        await expect(inspectorLinks).toHaveCount(0);

        // Verify text does not reveal internal rubric / prompt inspector
        await expect(page.getByText('AI Diagnostic Inspector')).not.toBeVisible();
        await expect(page.getByText('Server Evaluation Context')).not.toBeVisible();
        await expect(page.getByText('Expected Signals')).not.toBeVisible();

        // Normal assessment flow still starts properly
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();
    });

    test('Dev AI Inspector: Loads successfully and displays safe runtime configuration', async ({ page }) => {
        await page.goto('/dev/ai-inspector');

        // Check header and developer tool badge
        await expect(page.getByText('AI Diagnostic Inspector')).toBeVisible();
        await expect(page.getByText('DEVELOPER TOOL')).toBeVisible();
        await expect(page.getByText('DEV ONLY')).toBeVisible();

        // Privacy warning banner
        await expect(page.getByText(/Developer diagnostic tool. Avoid entering real personal or sensitive candidate information/i)).toBeVisible();

        // Runtime configuration panel
        await expect(page.getByTestId('runtime-config-title')).toBeVisible();
        await expect(page.getByTestId('runtime-provider')).toHaveText('OpenAI');
        await expect(page.getByTestId('runtime-model')).toHaveText('gpt-5.4-mini');
        await expect(page.getByTestId('runtime-environment')).toHaveText('ENV: Development');
        await expect(page.getByTestId('runtime-evaluator')).toHaveText(/DeterministicDiagnosticEvaluator|OpenAiDiagnosticEvaluator/);

        // Verify NO API keys or connection strings are displayed in the DOM
        const pageContent = await page.content();
        expect(pageContent).not.toContain('sk-');
        expect(pageContent).not.toContain('Data Source=');
        expect(pageContent).not.toContain('Password=');
    });

    test('Dev AI Inspector: Allows question selection and inspects server-side rubric & signals', async ({ page }) => {
        await page.goto('/dev/ai-inspector');

        // Wait for question inspector to load
        await expect(page.getByText('Question Inspector')).toBeVisible();

        // Check question dropdown exists and has questions
        const questionSelect = page.getByTestId('question-selector');
        await expect(questionSelect).toBeVisible();

        // Select q-be-sql-02
        await questionSelect.selectOption('q-be-sql-02');

        // Verify Question details
        await expect(page.getByTestId('question-id-badge')).toHaveText('q-be-sql-02');
        await expect(page.getByTestId('question-detail-card').getByText('sql', { exact: true })).toBeVisible();
        await expect(page.getByTestId('question-detail-card').getByText('applied', { exact: true })).toBeVisible();

        // Verify Server Evaluation Context (Server-Side Rubric & Expected Signals)
        await expect(page.getByTestId('server-evaluation-context')).toBeVisible();
        await expect(page.getByTestId('expected-signals-list')).toBeVisible();
        await expect(page.getByTestId('server-rubric-container')).toBeVisible();
        await expect(page.getByText('Insufficient Evidence:')).toBeVisible();
        await expect(page.getByText('Beginner:')).toBeVisible();
        await expect(page.getByText('Intermediate:')).toBeVisible();
        await expect(page.getByText('Advanced:')).toBeVisible();
    });

    test('Dev AI Inspector: Runs Deterministic Preview, displays full pipeline trace, and updates history', async ({ page }) => {
        await page.goto('/dev/ai-inspector');

        // Select q-be-sql-02
        const questionSelect = page.getByTestId('question-selector');
        await expect(questionSelect).toBeVisible();
        await questionSelect.selectOption('q-be-sql-02');

        // Populate simulated candidate answer using Strong Sample
        const strongSampleBtn = page.getByTestId('strong-sample-btn');
        await expect(strongSampleBtn).toBeVisible();
        await strongSampleBtn.click();

        const answerTextarea = page.getByTestId('candidate-answer-input');
        await expect(answerTextarea).not.toBeEmpty();

        // Check Live AI button is disabled when LiveEvaluationEnabled is false
        const liveAiBtn = page.getByTestId('live-evaluation-btn');
        await expect(liveAiBtn).toBeDisabled();

        // Run Deterministic Preview
        const deterministicBtn = page.getByTestId('deterministic-preview-btn');
        await expect(deterministicBtn).toBeEnabled();
        await deterministicBtn.click();

        // Wait for Trace results to appear
        const traceCard = page.getByTestId('active-trace-card');
        await expect(traceCard).toBeVisible({ timeout: 10_000 });
        await expect(page.getByTestId('active-trace-id')).toBeVisible();
        await expect(traceCard.getByText('DETERMINISTIC', { exact: true })).toBeVisible();
        await expect(traceCard.getByText('DeterministicDiagnosticEvaluator', { exact: true })).toBeVisible();

        // Evidence Grounding Debug View
        await expect(traceCard.getByText('Evidence Grounding Debug View')).toBeVisible();
        await expect(traceCard.getByText('AI Evidence')).toBeVisible();
        await expect(traceCard.getByText('AI Reasoning')).toBeVisible();

        // Assigned Level badge (e.g. Intermediate, Beginner, etc.)
        await expect(page.getByTestId('assigned-level-badge')).toBeVisible();

        // Backend Validation Stages
        await expect(traceCard.getByText('Backend Validation Stages')).toBeVisible();
        await expect(traceCard.getByText('Schema Valid')).toBeVisible();
        await expect(traceCard.getByText('Level Valid')).toBeVisible();
        await expect(traceCard.getByText('Evidence Present')).toBeVisible();
        await expect(traceCard.getByText('Question ID Match')).toBeVisible();
        await expect(traceCard.getByText('Rubric Resolved')).toBeVisible();

        // Shared Prompt Visibility
        const promptToggle = traceCard.getByRole('button', { name: /Shared Evaluation Prompt & Instructions/i });
        await expect(promptToggle).toBeVisible();
        await promptToggle.click();
        await expect(traceCard.getByText('System Instructions')).toBeVisible();
        await expect(traceCard.getByText('Evaluation Instructions (User Prompt)')).toBeVisible();

        // Verify Recent Development Traces table has updated with the new trace
        await expect(page.getByTestId('trace-history-table')).toBeVisible();
        const traceRow = page.getByTestId('trace-history-table').locator('tbody tr').first();
        await expect(traceRow).toBeVisible();
        await expect(traceRow.getByText('q-be-sql-02')).toBeVisible();
        await expect(traceRow.getByText('DeterministicDiagnosticEvaluator')).toBeVisible();
    });

});
