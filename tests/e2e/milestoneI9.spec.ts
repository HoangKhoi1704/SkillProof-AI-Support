import { test, expect } from '@playwright/test';

test.describe('Milestone I9: Gap-Based Project, Evaluation & Portfolio Proof E2E Flow', () => {
    test('Full product loop from diagnostic to verified portfolio proof and re-assessment', async ({ page }) => {
        await page.goto('/');

        // 1. Start Career Diagnostic for Backend Developer
        await expect(page.getByRole('heading', { name: 'Backend Developer' })).toBeVisible();
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();

        // 2. Select Custom mode to test a focused subset (SQL + C#)
        await page.getByTestId('mode-custom-tab').click();
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Enable SQL
        await page.getByTestId('skill-toggle-sql').click();

        // Select C# as primary language
        const csharpCard = page.getByTestId('custom-language-card-csharp');
        await expect(csharpCard).toBeVisible();
        await csharpCard.click();

        // 3. Start Adaptive Diagnostic
        const startAdaptiveBtn = page.getByTestId('start-adaptive-assessment-button');
        await expect(startAdaptiveBtn).toBeVisible();
        await startAdaptiveBtn.click();

        // 4. Answer questions (SQL S1, SQL S2, C# S1, C# S2)
        await expect(page.getByTestId('adaptive-diagnostic-view')).toBeVisible();
        const answerInput = page.getByTestId('adaptive-answer-input');

        // Q1: SQL Applied
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();
        await answerInput.fill('I inspect query execution plans with EXPLAIN ANALYZE and implement composite B-tree indexes to eliminate sequential scans.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Q2: SQL Follow-up
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();
        await answerInput.fill('I prevent phantom reads and write skew using Repeatable Read with optimistic concurrency control or SELECT FOR UPDATE locking.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Q3: C# Applied
        await expect(page.getByText('Stage 1: Applied')).toBeVisible();
        await answerInput.fill('In C#, I use async and await with Task and CancellationToken to handle asynchronous workflows without blocking threads.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // Q4: C# Follow-up
        await expect(page.getByText('Stage 2: Follow-up')).toBeVisible();
        await answerInput.fill('I manage concurrency using SemaphoreSlim and ConcurrentDictionary, avoiding lock contention and thread starvation.');
        await page.getByTestId('submit-adaptive-answer-button').click();

        // 5. Verify Career Readiness Profile renders
        await expect(page.getByText('Career Readiness Skill Profile')).toBeVisible();
        await expect(page.getByTestId('career-readiness-summary')).toBeVisible();

        // 6. Generate Roadmap
        const generateRoadmapBtn = page.getByTestId('generate-roadmap-btn');
        await expect(generateRoadmapBtn).toBeVisible();
        await generateRoadmapBtn.click();

        // 7. Verify Roadmap renders, then click "Build a Project"
        await expect(page.getByRole('heading', { name: 'Personalized Career Readiness Roadmap' })).toBeVisible();
        const recommendProjectBtn = page.getByTestId('recommend-project-btn');
        await expect(recommendProjectBtn).toBeVisible();
        await recommendProjectBtn.click();

        // 8. Verify Gap-Based Real-World Project renders
        await expect(page.getByText('Milestone I9 — Gap-Based Project & Portfolio Proof')).toBeVisible();
        const targetedSkillsList = page.getByTestId('project-targeted-skills');
        await expect(targetedSkillsList).toBeVisible();

        // Assert targeted skills trace back to diagnosed gaps (SQL and C#)
        const targetedText = await targetedSkillsList.innerText();
        expect(targetedText.toLowerCase()).toContain('sql');
        expect(targetedText.toLowerCase()).toContain('csharp');

        // Assert requirements are rendered and trace to skills
        const requirementsList = page.getByTestId('project-requirements-list');
        await expect(requirementsList).toBeVisible();
        await expect(requirementsList).toContainText('Develops:');

        // 9. Submit Project Evidence
        await expect(page.getByTestId('project-evidence-submission-form')).toBeVisible();

        await page.getByTestId('evidence-repo-url').fill('https://github.com/candidate/resilient-order-api');
        await page.getByTestId('evidence-summary-input').fill('Built a production-grade ordering API in C# with database transaction isolation and automated test suites.');
        await page.getByTestId('evidence-implementation-input').fill('Implemented explicit database transaction management using Repeatable Read isolation. Added composite index on (OrderId, Status) and verified query execution plan using EXPLAIN ANALYZE. Developed asynchronous handlers with C# async await and cancellation tokens.');
        await page.getByTestId('evidence-architecture-input').fill('Selected Repeatable Read to prevent phantom reads during inventory subtraction without the high lock overhead of full serialization.');
        await page.getByTestId('evidence-testing-input').fill('Implemented an automated test suite with unit tests and test doubles to verify boundary conditions and concurrency contention.');

        const submitEvidenceBtn = page.getByTestId('submit-evidence-btn');
        await expect(submitEvidenceBtn).toBeVisible();
        await submitEvidenceBtn.click();

        // 10. Verify Project Evidence Evaluation
        await expect(page.getByTestId('project-evaluation-view')).toBeVisible();
        const overallStatus = page.getByTestId('evaluation-overall-status');
        await expect(overallStatus).toBeVisible();
        await expect(overallStatus).toContainText('Demonstrated');

        // Verify qualitative breakdown
        await expect(page.getByTestId('skill-evidence-list')).toBeVisible();
        await expect(page.getByTestId('demonstrated-evidence-list')).toBeVisible();

        // 11. Verify Portfolio Proof & CV Bullets
        await expect(page.getByTestId('portfolio-proof-view')).toBeVisible();
        const cvBulletsList = page.getByTestId('cv-bullets-list');
        await expect(cvBulletsList).toBeVisible();
        const cvText = await cvBulletsList.innerText();
        expect(cvText.length).toBeGreaterThan(20);

        const demonstratedBadges = page.getByTestId('demonstrated-skills-badges');
        await expect(demonstratedBadges).toBeVisible();
        const badgesText = await demonstratedBadges.innerText();
        expect(badgesText.toLowerCase()).toContain('sql');

        // 12. Strict Assertions: NO numeric scores, NO percentages, NO grading leaks
        const pageContent = await page.content();
        expect(pageContent).not.toContain('expectedSignals');
        expect(pageContent).not.toContain('systemInstructions');
        expect(pageContent).not.toContain('diagnosticRubric');

        const proofText = await page.getByTestId('portfolio-proof-view').innerText();
        expect(proofText).not.toContain('%');
        expect(proofText).not.toContain('/100');
        expect(proofText).not.toContain('probability');

        // 13. Re-assess Skills entry point completes loop
        const reassessBtn = page.getByTestId('reassess-skills-btn');
        await expect(reassessBtn).toBeVisible();
        await reassessBtn.click();

        // Verify returned to assessment setup screen
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();
        await expect(page.getByText('Backend Developer Assessment Setup')).toBeVisible();
    });
});
