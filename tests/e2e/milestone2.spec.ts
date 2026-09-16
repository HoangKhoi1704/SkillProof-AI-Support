import { test, expect } from '@playwright/test';

test.describe('Milestone 2: Diagnostic Evaluation & Skill Profile', () => {
    test('Happy Path: Select Backend Developer, answer, submit, view skill profile and top gaps', async ({ page }) => {
        await page.goto('/');

        // Select Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Fill Question 1 (REST API)
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        const answerInput = page.locator('textarea');
        await answerInput.fill('PUT replaces the entire resource representation while PATCH performs a partial update with idempotency guarantees.');

        // Fill Question 2 (SQL / Database)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('I would inspect the execution plan using EXPLAIN and add a composite index on CustomerId and OrderDate.');

        // Fill Question 3 (Testing)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('I would use mock interfaces to isolate payment gateway calls and test success and error status codes.');

        // Fill Question 4 (System Design)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('I would use Redis with a sliding window counter and return HTTP 429 Too Many Requests.');

        // Fill Question 5 (Authentication)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Stateless JWT verifies cryptographic signature on the header and payload claims using HMAC or RSA.');

        // Fill Question 6 (Programming Fundamentals)
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('I would stream records with IAsyncEnumerable and pass a CancellationToken to allow graceful cancellation.');

        // Navigate to Review Screen
        await page.getByRole('button', { name: /Review/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();

        // Submit Assessment
        const submitBtn = page.getByRole('button', { name: /Submit Diagnostic Assessment/i });
        await expect(submitBtn).toBeVisible();
        await submitBtn.click();

        // Verify Skill Profile View renders
        await expect(page.getByRole('heading', { name: 'Career Readiness Skill Profile' })).toBeVisible();
        await expect(page.getByText('Milestone 2 — Skill Profile')).toBeVisible();

        // Verify Top Gaps Banner
        await expect(page.getByText('Top Prioritized Skill Gaps (Max 3)')).toBeVisible();
        const topGapsList = page.getByTestId('top-gaps-list');
        await expect(topGapsList).toBeVisible();
        await expect(topGapsList.getByText('SQL / Database')).toBeVisible();
        await expect(topGapsList.getByText('Testing')).toBeVisible();
        await expect(topGapsList.getByText('System Design')).toBeVisible();

        // Verify Competency breakdown cards render
        await expect(page.getByRole('heading', { name: 'REST API' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'SQL / Database' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Testing' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'System Design' })).toBeVisible();

        // Verify 4-tier qualitative level badges render
        await expect(page.getByText('Intermediate', { exact: true }).first()).toBeVisible();
        await expect(page.getByText('Beginner', { exact: true }).first()).toBeVisible();

        // Verify Milestone 2 Exit Card
        await expect(page.getByText('Milestone 2 Exit Criteria Satisfied')).toBeVisible();
    });

    test('Empty Answers Path: Select Financial Analyst, submit blank answers, produces Insufficient Evidence', async ({ page }) => {
        await page.goto('/');

        // Select Financial Analyst
        await expect(page.getByRole('heading', { name: 'Financial Analyst' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).nth(1).click();

        // Skip to end without entering text
        for (let i = 1; i <= 5; i++) {
            await page.getByRole('button', { name: /Next Question/i }).click();
        }

        // Navigate to Review
        await page.getByRole('button', { name: /Review/i }).click();
        await expect(page.getByText('Financial Analyst Assessment Review')).toBeVisible();

        // Submit blank assessment
        await page.getByRole('button', { name: /Submit Diagnostic Assessment/i }).click();

        // Verify Skill Profile renders
        await expect(page.getByRole('heading', { name: 'Career Readiness Skill Profile' })).toBeVisible();

        // Verify Insufficient Evidence badges render
        const insufficientBadges = page.locator('text=Insufficient Evidence');
        await expect(insufficientBadges.first()).toBeVisible();

        // Verify Top Gaps are populated
        await expect(page.getByText('Top Prioritized Skill Gaps (Max 3)')).toBeVisible();
    });
});
