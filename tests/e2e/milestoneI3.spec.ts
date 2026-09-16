import { test, expect } from '@playwright/test';

test.describe('Milestone I3: Dynamic Question Selection from SQLite', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);
    });

    test('All 6 Core competencies in Recommended mode produces exactly 7 questions with Question 1 of 7', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Select primary language C#
        await page.getByTestId('language-card-csharp').click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify Diagnostic Questions view loaded with dynamic length 7
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 7')).toBeVisible();

        // Verify difficulty badge is "applied"
        await expect(page.getByText('applied', { exact: true })).toBeVisible();

        // Check unsupported notice is NOT present (all 6 core are supported)
        await expect(page.getByTestId('unsupported-skills-notice')).not.toBeVisible();
    });

    test('Selecting a subset in Custom mode (SQL + Testing + TypeScript) produces exactly 3 questions', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Toggle all Core off first
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Turn on only SQL and Testing
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('skill-toggle-testing').click();

        // Select primary language TypeScript
        await page.getByTestId('custom-language-card-typescript').click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify question count is dynamically 3
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 3')).toBeVisible();

        // Verify competency badge reflects one of the selected skills
        const firstBadge = page.locator('span:has-text("SQL")').first();
        await expect(firstBadge).toBeVisible();

        // Fill answer 1 and go to question 2
        await page.locator('textarea').fill('I analyze query execution plans with EXPLAIN and add composite indexes.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Verify question 2 of 3
        await expect(page.getByText('Question 2 of 3')).toBeVisible();
        const secondBadge = page.locator('span:has-text("Automated Testing")').first();
        await expect(secondBadge).toBeVisible();

        // Fill answer 2 and go to question 3 (TypeScript)
        await page.locator('textarea').fill('I write unit and integration tests using mocked interfaces to isolate dependencies.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Verify question 3 of 3 (TypeScript)
        await expect(page.getByText('Question 3 of 3')).toBeVisible();
        const thirdBadge = page.locator('span:has-text("TypeScript")').first();
        await expect(thirdBadge).toBeVisible();

        // Fill answer 3 and advance to review
        await page.locator('textarea').fill('I use strict TypeScript types and generics.');
        await page.getByRole('button', { name: /Review All Answers/i }).click();

        // Verify review screen lists exactly 3 questions
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Q1')).toBeVisible();
        await expect(page.getByText('Q2')).toBeVisible();
        await expect(page.getByText('Q3')).toBeVisible();
        await expect(page.getByText('Q4')).not.toBeVisible();
    });

    test('Selecting unsupported catalog skill displays neutral banner and does not falsely assess it', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Toggle all Core off
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Select SQL (supported) + Docker (unsupported catalog skill)
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('skill-toggle-docker').click();

        // Select primary language Go
        await page.getByTestId('custom-language-card-go').click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify 2 questions returned: SQL (1) + Go (1) = 2 (Docker is unsupported)
        await expect(page.getByText('Question 1 of 2')).toBeVisible();

        // Verify neutral unsupported skills notice is displayed
        const notice = page.getByTestId('unsupported-skills-notice');
        await expect(notice).toBeVisible();
        await expect(notice).toContainText('Catalog-Only Skills Selected');
        await expect(notice).toContainText('docker');
        await expect(notice).toContainText('Not included in the current diagnostic.');
    });

    test('Setup selection survives Back to Setup navigation and updates question bank dynamically', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Switch to Custom mode and select only SQL + Python
        await page.getByTestId('mode-custom-tab').click();
        await page.getByRole('button', { name: /Toggle All Core/i }).click();
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('custom-language-card-python').click();

        await page.getByTestId('continue-to-assessment-button').click();
        // SQL (1) + Python (1) = 2
        await expect(page.getByText('Question 1 of 2')).toBeVisible();

        // Click back to Setup button
        await page.getByTestId('back-to-setup-button').click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Add Testing to selection
        await page.getByTestId('skill-toggle-testing').click();

        // Continue again
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify question count is now updated to 3 (SQL + Testing + Python)
        await expect(page.getByText('Question 1 of 3')).toBeVisible();
    });

    test('Financial Analyst remains on legacy flow without setup screen and with 6 questions', async ({ page }) => {
        // Select Financial Analyst
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).nth(1).click();

        // Setup screen should NOT appear
        await expect(page.getByTestId('assessment-setup-screen')).not.toBeVisible();

        // Legacy questions load directly
        await expect(page.getByText('Financial Analyst Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 6')).toBeVisible();
        await expect(page.getByText('Financial Statements', { exact: true })).toBeVisible();
    });
});
