import { test, expect } from '@playwright/test';

test.describe('Milestone 5: Gap-Backward Real-World Project Recommendation', () => {
    test('Complete End-to-End Flow: Backend Developer (Diagnostic -> Roadmap -> Gap-Backward Project)', async ({ page }) => {
        await page.goto('/');

        // Select Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Fill questions
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        const answerInput = page.locator('textarea');
        
        // Q1
        await answerInput.fill('PUT replaces resource representation entirely and is idempotent, while PATCH applies partial updates.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q2
        await answerInput.fill('Inspect EXPLAIN plan and analyze composite indexes on CustomerId and OrderDate.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q3
        await answerInput.fill('Mock payment gateway interface to isolate transient network failure and test edge cases.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q4
        await answerInput.fill('Redis sliding window counter rate limiter returning HTTP 429.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q5
        await answerInput.fill('Stateless JWT cryptographic signature validation using public keys.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q6
        await answerInput.fill('Asynchronous streaming with IAsyncEnumerable and CancellationToken propagation.');

        // Review & Submit Diagnostic
        await page.getByRole('button', { name: /Review/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();

        const submitBtn = page.getByRole('button', { name: /Submit Diagnostic Assessment/i });
        await expect(submitBtn).toBeVisible();
        await submitBtn.click();

        // Verify Skill Profile
        await expect(page.getByRole('heading', { name: 'Career Readiness Skill Profile' })).toBeVisible();
        await expect(page.getByText('Top Prioritized Skill Gaps (Max 3)')).toBeVisible();

        // Milestone 4: Generate Roadmap
        const generateRoadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(generateRoadmapBtn).toBeVisible();
        await generateRoadmapBtn.click();

        // Verify Roadmap
        await expect(page.getByRole('heading', { name: 'Personalized Career Readiness Roadmap' })).toBeVisible();
        await expect(page.getByTestId('roadmap-items-list')).toBeVisible();

        // Milestone 5: Click "Build a Project"
        const recommendProjectBtn = page.getByTestId('recommend-project-btn');
        await expect(recommendProjectBtn).toBeVisible();
        await recommendProjectBtn.click();

        // Verify Milestone 5 Recommended Project View
        await expect(page.getByText('Milestone 5 — Gap-Backward Project')).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Recommended Real-World Portfolio Project' })).toBeVisible();

        // Strategic Rationale
        await expect(page.getByText('Strategic Gap-Backward Rationale')).toBeVisible();

        // Project Title & Overview
        await expect(page.getByRole('heading', { name: 'Expense Management API' })).toBeVisible();
        await expect(page.getByText('Observable Deliverables & Evidence Artifacts:')).toBeVisible();

        // Requirements mapped to diagnosed gaps
        const requirementsList = page.getByTestId('project-requirements-list');
        await expect(requirementsList).toBeVisible();
        await expect(requirementsList.getByText('Develops:').first()).toBeVisible();
        await expect(requirementsList.getByText('↳ Deliverable:').first()).toBeVisible();

        // Milestone 5 Exit Criteria
        await expect(page.getByText('Milestone 5 Exit Criteria Satisfied')).toBeVisible();
    });

    test('Complete End-to-End Flow: Financial Analyst (Diagnostic -> Roadmap -> Gap-Backward Project)', async ({ page }) => {
        await page.goto('/');

        // Select Financial Analyst
        await expect(page.getByRole('heading', { name: 'Financial Analyst' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).nth(1).click();

        // Fill Question 1
        await expect(page.getByText('Financial Analyst Diagnostic')).toBeVisible();
        const answerInput = page.locator('textarea');
        await answerInput.fill('Income statement records revenues and expenses, balance sheet records assets and liabilities, and cash flow reconciles cash balance.');

        // Q2
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('INDEX/MATCH or XLOOKUP with dynamic table references and IFERROR handling.');

        // Q3
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Dynamic 3-statement financial model linking revenue assumptions, debt schedules, and working capital.');

        // Q4
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Decompose working capital cash burn and customer payment cycles.');

        // Q5
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('DSO, DIO, and DPO analysis with cash conversion cycle divergence.');

        // Q6
        await page.getByRole('button', { name: /Next Question/i }).click();
        await answerInput.fill('Gross margin variance waterfall decomposing price, volume, and product mix effects.');

        // Review & Submit
        await page.getByRole('button', { name: /Review/i }).click();
        await page.getByRole('button', { name: /Submit Diagnostic Assessment/i }).click();

        // Verify Skill Profile
        await expect(page.getByRole('heading', { name: 'Career Readiness Skill Profile' })).toBeVisible();

        // Generate Roadmap
        await page.getByTestId('generate-roadmap-btn').click();
        await expect(page.getByRole('heading', { name: 'Personalized Career Readiness Roadmap' })).toBeVisible();

        // Recommend Project
        await page.getByTestId('recommend-project-btn').click();

        // Verify Financial Analyst Recommended Project
        await expect(page.getByRole('heading', { name: 'Recommended Real-World Portfolio Project' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Company Financial Health & 3-Year Outlook' })).toBeVisible();

        const reqsList = page.getByTestId('project-requirements-list');
        await expect(reqsList).toBeVisible();
        await expect(reqsList.getByText('Develops:').first()).toBeVisible();

        // Milestone 5 Exit Criteria
        await expect(page.getByText('Milestone 5 Exit Criteria Satisfied')).toBeVisible();
    });
});
