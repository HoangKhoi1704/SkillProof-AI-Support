import { test, expect } from '@playwright/test';

test.describe('Milestone 4: Personalized Learning Roadmap', () => {
    test('Skill Profile to Personalized Roadmap Flow: Backend Developer', async ({ page }) => {
        await page.goto('/');

        // Select Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Fill Question 1 (REST API)
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        const answerInput = page.locator('textarea');
        await answerInput.fill('PUT replaces resource representation entirely and is idempotent, while PATCH applies partial updates.');

        // Fill Question 2 (SQL / Database)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Inspect EXPLAIN plan and analyze composite indexes on CustomerId and OrderDate.');

        // Fill Question 3 (Testing)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Mock payment gateway interface to isolate transient network failure and test edge cases.');

        // Fill Question 4 (System Design)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Redis sliding window counter rate limiter returning HTTP 429.');

        // Fill Question 5 (Authentication)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Stateless JWT cryptographic signature validation using public keys.');

        // Fill Question 6 (Programming Fundamentals)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Asynchronous streaming with IAsyncEnumerable and CancellationToken propagation.');

        // Review & Submit Diagnostic
        await page.getByRole('button', { name: /Review/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();

        const submitBtn = page.getByRole('button', { name: /Submit Diagnostic Assessment/i });
        await expect(submitBtn).toBeVisible();
        await submitBtn.click();

        // Verify Skill Profile renders
        await expect(page.getByRole('heading', { name: 'Career Readiness Skill Profile' })).toBeVisible();
        await expect(page.getByText('Top Prioritized Skill Gaps (Max 3)')).toBeVisible();

        // Milestone 4: Generate Roadmap
        const generateRoadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(generateRoadmapBtn).toBeVisible();
        await generateRoadmapBtn.click();

        // Verify Personalized Roadmap renders
        await expect(page.getByRole('heading', { name: 'Personalized Career Readiness Roadmap' })).toBeVisible();
        await expect(page.getByText('Milestone 4 — Personalized Roadmap')).toBeVisible();

        // Verify Roadmap Items List container
        const roadmapItems = page.getByTestId('roadmap-items-list');
        await expect(roadmapItems).toBeVisible();

        // Verify 3 priorities render
        await expect(roadmapItems.getByText('Priority 1')).toBeVisible();
        await expect(roadmapItems.getByText('Priority 2')).toBeVisible();
        await expect(roadmapItems.getByText('Priority 3')).toBeVisible();

        // Verify learning goals and actionable practice tasks
        await expect(roadmapItems.getByText('Target Learning Goal').first()).toBeVisible();
        await expect(roadmapItems.getByText('Actionable Practice Task').first()).toBeVisible();

        // Verify Milestone 4 Exit Card
        await expect(page.getByText('Milestone 4 Exit Criteria Satisfied')).toBeVisible();
    });
});
