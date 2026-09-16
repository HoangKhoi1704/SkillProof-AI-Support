import { test, expect } from '@playwright/test';

test.describe('Milestone I4: Data Foundation v2 SQLite Dynamic Assessment E2E', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await expect(page).toHaveTitle(/SkillProof/i);
    });

    test('Recommended Backend Developer with C# produces exactly 7 questions (6 Core + 1 Language)', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Verify Recommended setup notice mentions 7 questions
        await expect(page.getByText('6 Core Skills + 1 Language (7 Questions)')).toBeVisible();

        // Select primary language C#
        await page.getByTestId('language-card-csharp').click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify Diagnostic Questions view loaded with dynamic length 7
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 7')).toBeVisible();

        // Verify difficulty badge is "applied"
        await expect(page.getByText('applied', { exact: true })).toBeVisible();

        // Check unsupported notice is NOT visible
        await expect(page.getByTestId('unsupported-skills-notice')).not.toBeVisible();

        // Navigate through all 7 questions
        const answerInput = page.locator('textarea');
        for (let i = 1; i <= 6; i++) {
            await expect(page.getByText(`Question ${i} of 7`)).toBeVisible();
            await answerInput.fill(`Applied answer for question ${i} demonstrating competency.`);
            await page.getByRole('button', { name: /Next Question/i }).click();
        }

        // On Question 7 (C# / .NET applied question)
        await expect(page.getByText('Question 7 of 7')).toBeVisible();
        await expect(page.getByText('C# / .NET', { exact: true })).toBeVisible();
        await answerInput.fill('In C#, I handle async streams using IAsyncEnumerable with await foreach and CancellationTokens.');

        // Review button should now appear on the 7th question
        const reviewBtn = page.getByRole('button', { name: /Review All Answers/i });
        await expect(reviewBtn).toBeVisible();
        await reviewBtn.click();

        // Verify review screen lists exactly 7 questions
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Q1')).toBeVisible();
        await expect(page.getByText('Q7')).toBeVisible();
        await expect(page.getByText('Q8')).not.toBeVisible();
    });

    test('Custom Mode: SQL + Testing + Python produces exactly 3 applied questions', async ({ page }) => {
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

        // Select primary language Python
        await page.getByTestId('custom-language-card-python').click();

        // Verify custom setup notice says: 3 Questions to Assess (2 Competencies + 1 Language)
        await expect(page.getByText('3 Questions to Assess (2 Competencies + 1 Language)')).toBeVisible();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify question count is dynamically 3
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 3')).toBeVisible();

        const answerInput = page.locator('textarea');

        // Q1: SQL
        await expect(page.getByText('SQL & Relational Databases')).toBeVisible();
        await answerInput.fill('EXPLAIN ANALYZE to inspect query plan, add composite index on user_id and created_at.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q2: Testing
        await expect(page.getByText('Question 2 of 3')).toBeVisible();
        await expect(page.getByText('Automated Testing & Quality')).toBeVisible();
        await answerInput.fill('Use pytest with fixture mocks to isolate database and third-party APIs.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q3: Python
        await expect(page.getByText('Question 3 of 3')).toBeVisible();
        await expect(page.getByText('Python', { exact: true })).toBeVisible();
        await answerInput.fill('Python GIL impacts CPU-bound threads; use multiprocessing or asyncio for I/O concurrency.');
        
        // Review Answers
        await page.getByRole('button', { name: /Review All Answers/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Q1')).toBeVisible();
        await expect(page.getByText('Q2')).toBeVisible();
        await expect(page.getByText('Q3')).toBeVisible();
        await expect(page.getByText('Q4')).not.toBeVisible();
    });

    test('Custom Mode: NoSQL + Caching + Go produces exactly 3 applied questions', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Toggle all Core off
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Select Recommended skills: NoSQL and Distributed Caching
        await page.getByTestId('skill-toggle-nosql').click();
        await page.getByTestId('skill-toggle-caching').click();

        // Select primary language Go
        await page.getByTestId('custom-language-card-go').click();

        // Verify setup notice says 3 Questions to Assess
        await expect(page.getByText('3 Questions to Assess (2 Competencies + 1 Language)')).toBeVisible();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Verify question count is dynamically 3
        await expect(page.getByText('Backend Developer Diagnostic')).toBeVisible();
        await expect(page.getByText('Question 1 of 3')).toBeVisible();

        const answerInput = page.locator('textarea');

        // Q1: NoSQL
        await expect(page.getByText('NoSQL Data Stores')).toBeVisible();
        await answerInput.fill('Design document model with embedded comments for fast single-read lookup.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q2: Caching
        await expect(page.getByText('Question 2 of 3')).toBeVisible();
        await expect(page.getByText('Distributed Caching')).toBeVisible();
        await answerInput.fill('Cache-aside pattern with Redis and mutex locking to prevent cache stampede.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Q3: Go
        await expect(page.getByText('Question 3 of 3')).toBeVisible();
        await expect(page.getByText('Go (Golang)', { exact: true })).toBeVisible();
        await answerInput.fill('Use sync.Mutex or channels with select timeouts to prevent goroutine leaks.');

        // Review Answers
        await page.getByRole('button', { name: /Review All Answers/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Q1')).toBeVisible();
        await expect(page.getByText('Q2')).toBeVisible();
        await expect(page.getByText('Q3')).toBeVisible();
        await expect(page.getByText('Q4')).not.toBeVisible();
    });

    test('Optional Docker competency remains catalog-only and produces no fake question', async ({ page }) => {
        // Select Backend Developer
        await page.getByRole('button', { name: /Start Career Diagnostic/i }).first().click();
        await expect(page.getByTestId('assessment-setup-screen')).toBeVisible();

        // Switch to Customize tab
        await page.getByTestId('mode-custom-tab').click();

        // Toggle all Core off
        await page.getByRole('button', { name: /Toggle All Core/i }).click();

        // Select SQL (assessable) + Docker (catalog-only optional)
        await page.getByTestId('skill-toggle-sql').click();
        await page.getByTestId('skill-toggle-docker').click();

        // Select primary language Rust
        await page.getByTestId('custom-language-card-rust').click();

        // Continue to Diagnostic
        await page.getByTestId('continue-to-assessment-button').click();

        // Expected: SQL (1) + Rust (1) = 2 questions (Docker contributes NO question)
        await expect(page.getByText('Question 1 of 2')).toBeVisible();

        // Verify neutral unsupported skills notice is displayed for docker
        const notice = page.getByTestId('unsupported-skills-notice');
        await expect(notice).toBeVisible();
        await expect(notice).toContainText('Catalog-Only Skills Selected');
        await expect(notice).toContainText('docker');
        await expect(notice).toContainText('Not included in the current diagnostic.');

        // Advance to Question 2
        const answerInput = page.locator('textarea');
        await answerInput.fill('SQL indexing strategy.');
        await page.getByRole('button', { name: /Next Question/i }).click();

        // Question 2 should be Rust, not Docker
        await expect(page.getByText('Question 2 of 2')).toBeVisible();
        await expect(page.getByText('Rust', { exact: true })).toBeVisible();
        
        // Review answers to prove no 3rd question exists
        await page.getByRole('button', { name: /Review All Answers/i }).click();
        await expect(page.getByText('Backend Developer Assessment Review')).toBeVisible();
        await expect(page.getByText('Q1')).toBeVisible();
        await expect(page.getByText('Q2')).toBeVisible();
        await expect(page.getByText('Q3')).not.toBeVisible();
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
        // SQL (1) + Python (1) = 2 questions
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

    test('Financial Analyst remains unchanged on legacy flow without setup screen and with 6 questions', async ({ page }) => {
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
