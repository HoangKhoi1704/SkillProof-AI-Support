import { test, expect } from '@playwright/test';

test.describe('Milestone 1: Role Selection & Diagnostic Flow', () => {
    test('User can select Backend Developer, enter answers, and review', async ({ page }) => {
        // Navigate to homepage
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);

        // Verify role selection headline
        await expect(page.getByText('Validate Your Job Readiness')).toBeVisible();

        // Verify role headings
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Financial Analyst' })).toBeVisible();

        // Click Start Career Diagnostic for Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Assessment Setup step (Milestone I2): select primary language and continue
        const langCard = page.getByTestId('language-card-csharp');
        await expect(langCard).toBeVisible();
        await langCard.click();
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify Diagnostic Questions UI loaded
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 7')).toBeVisible();
        await expect(page.getByText(/Programming Fundamentals/)).toBeVisible();

        // Enter answer for Question 1
        const answerInput = page.locator('textarea');
        await expect(answerInput).toBeVisible();
        await answerInput.fill('PUT replaces the entire resource while PATCH applies partial modifications.');
        await expect(page.getByText(/\d+ characters/)).toBeVisible();

        // Navigate to Question 2
        await page.getByRole('button', { name: /Next Question/i }).click();
        await expect(page.getByText('Question 2 of 7')).toBeVisible();
        await expect(page.getByText(/REST API Design/)).toBeVisible();

        // Fill Question 2
        await answerInput.fill('I would inspect the execution plan and create a composite index on (CustomerId, OrderDate).');

        // Navigate through remaining questions to the end (questions 3 to 7)
        for (let i = 3; i <= 7; i++) {
            await page.getByRole('button', { name: /Next Question/i }).click();
            await expect(page.getByText(`Question ${i} of 7`)).toBeVisible();
            await answerInput.fill(`Sample answer for Question ${i} demonstrating competency.`);
        }

        // Click Review button
        await page.getByRole('button', { name: /Review/i }).click();

        // Verify Review View
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Ready for Diagnostic Evaluation')).toBeVisible();

        // Verify answered status badges
        const answeredBadges = page.locator('text=Answered');
        await expect(answeredBadges).toHaveCount(7);
    });

    test('User can select Financial Analyst and load questions', async ({ page }) => {
        await page.goto('/');

        // Verify Financial Analyst card
        await expect(page.getByRole('heading', { name: 'Financial Analyst' })).toBeVisible();

        // Click Start Career Diagnostic for Financial Analyst
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).nth(1).click();

        // Verify Financial Analyst questions loaded
        await expect(page.getByText('Financial Analyst Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 6')).toBeVisible();
        await expect(page.getByText('Financial Statements', { exact: true })).toBeVisible();

        // Verify change role button works
        await page.getByRole('button', { name: /Change Role/i }).click();
        await expect(page.getByText('Validate Your Job Readiness')).toBeVisible();
    });
});
