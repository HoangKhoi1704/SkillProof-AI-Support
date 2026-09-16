import { test, expect } from '@playwright/test';

test.describe('Milestone I2: Dynamic Skill Selection UI', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);
    });

    test('Backend Developer opens Assessment Setup and loads catalog from API', async ({ page }) => {
        // Select Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // Verify Assessment Setup screen renders
        const setupScreen = page.getByTestId('assessment-setup-screen');
        await expect(setupScreen).toBeVisible();
        await expect(page.getByText('Backend Developer Assessment Setup')).toBeVisible();

        // Verify Recommended Assessment is active by default
        await expect(page.getByTestId('mode-recommended-tab')).toHaveClass(/bg-indigo-600/);
        await expect(page.getByTestId('recommended-view')).toBeVisible();

        // Verify Core competencies loaded from backend API (all 6)
        const coreGrid = page.getByTestId('core-skills-grid');
        await expect(coreGrid).toBeVisible();
        await expect(coreGrid.getByText('Programming Fundamentals')).toBeVisible();
        await expect(coreGrid.getByText('REST API Design & HTTP')).toBeVisible();
        await expect(coreGrid.getByText('SQL & Relational Databases')).toBeVisible();
        await expect(coreGrid.getByText('Automated Testing & Quality')).toBeVisible();
        await expect(coreGrid.getByText('Authentication & API Security')).toBeVisible();
        await expect(coreGrid.getByText('System Design & Architecture')).toBeVisible();

        // Verify Language options loaded from backend API (all 8)
        const langGrid = page.getByTestId('languages-grid');
        await expect(langGrid).toBeVisible();
        await expect(langGrid.getByText('C# / .NET')).toBeVisible();
        await expect(langGrid.getByText('Java', { exact: true })).toBeVisible();
        await expect(langGrid.getByText('Python')).toBeVisible();
        await expect(langGrid.getByText('C++')).toBeVisible();
        await expect(langGrid.getByText('JavaScript (Node.js)')).toBeVisible();
        await expect(langGrid.getByText('TypeScript')).toBeVisible();
        await expect(langGrid.getByText('Go (Golang)')).toBeVisible();
        await expect(langGrid.getByText('Rust', { exact: true })).toBeVisible();
    });

    test('Recommended mode rejects continuing without a primary language', async ({ page }) => {
        // Navigate to Backend Developer Setup
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Click Continue without selecting a language
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify validation error
        const errorAlert = page.getByTestId('setup-validation-error');
        await expect(errorAlert).toBeVisible();
        await expect(errorAlert).toContainText('Please select a primary programming language to continue.');

        // Diagnostic questions should NOT be loaded
        await expect(page.getByText(/Question 1 of/i)).not.toBeVisible();
    });

    test('Selecting primary language allows continuing to Diagnostic and preserves selection state', async ({ page }) => {
        // Navigate to Backend Developer Setup
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Select C# / .NET
        const csharpCard = page.getByTestId('language-card-csharp');
        await expect(csharpCard).toBeVisible();
        await csharpCard.click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify Diagnostic Questions view loaded
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 7')).toBeVisible();

        // Verify selection badge in header reflects Recommended + CSHARP
        await expect(page.getByText(/Recommended • CSHARP/i)).toBeVisible();

        // Verify Setup button exists and clicking it returns to Setup preserving state
        const backToSetupBtn = page.getByTestId('back-to-setup-button');
        await expect(backToSetupBtn).toBeVisible();
        await backToSetupBtn.click();

        // Verify we are back on setup screen with C# still active
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();
        await expect(csharpCard).toHaveClass(/bg-indigo-600\/20/);
    });

    test('Customize mode exposes Core, Recommended, Optional, and Languages with toggle functionality', async ({ page }) => {
        // Navigate to Backend Developer Setup
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();
        await expect(page.getByTestId('custom-view')).toBeVisible();

        // Verify Core competencies group
        await expect(page.getByTestId('custom-core-skills-grid')).toBeVisible();
        await expect(page.getByText('Core Competencies')).toBeVisible();

        // Verify Recommended competencies group (NoSQL, Caching)
        const recGrid = page.getByTestId('custom-recommended-skills-grid');
        await expect(recGrid).toBeVisible();
        await expect(recGrid.getByText('NoSQL Data Stores')).toBeVisible();
        await expect(recGrid.getByText('Distributed Caching')).toBeVisible();

        // Verify Optional competencies group (Git, Docker, CI/CD, Concurrency, Messaging, Observability)
        const optGrid = page.getByTestId('custom-optional-skills-grid');
        await expect(optGrid).toBeVisible();
        await expect(optGrid.getByText('Version Control (Git)')).toBeVisible();
        await expect(optGrid.getByText('Containerization (Docker)')).toBeVisible();
        await expect(optGrid.getByText('CI/CD Pipelines')).toBeVisible();

        // Verify Programming Languages section is separate
        const customLangGrid = page.getByTestId('custom-languages-grid');
        await expect(customLangGrid).toBeVisible();
        await expect(customLangGrid.getByText('Python')).toBeVisible();

        // Toggle a recommended skill (Distributed Caching)
        const cachingCard = page.getByTestId('skill-toggle-caching');
        await cachingCard.click();
        await expect(cachingCard).toHaveClass(/bg-indigo-600\/10/);

        // Select Python as primary language
        const pythonCard = page.getByTestId('custom-language-card-python');
        await pythonCard.click();
        await expect(pythonCard).toHaveClass(/bg-violet-600\/20/);

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText(/Custom • PYTHON/i)).toBeVisible();
    });

    test('Customize mode rejects empty skill selection', async ({ page }) => {
        // Navigate to Backend Developer Setup
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Deselect all core competencies using Toggle All Core button
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Attempt to continue with zero skills and no language
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify validation error
        const errorAlert = page.getByTestId('setup-validation-error');
        await expect(errorAlert).toBeVisible();
        await expect(errorAlert).toContainText('Please select at least one skill or language to evaluate.');
    });

    test('Financial Analyst preserves existing flow directly without setup screen', async ({ page }) => {
        // Select Financial Analyst
        await expect(page.getByRole('heading', { name: 'Financial Analyst' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).nth(1).click();

        // Setup screen should NOT appear
        await expect(page.getByTestId('assessment-setup-screen')).not.toBeVisible();

        // Questions load directly
        await expect(page.getByText('Financial Analyst Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 6')).toBeVisible();
        await expect(page.getByText('Financial Statements', { exact: true })).toBeVisible();

        // Change role returns to home
        await page.getByRole('button', { name: /Change Role/i }).click();
        await expect(page.getByText('Validate Your Job Readiness')).toBeVisible();
    });
});
