import { test, expect } from '@playwright/test';

test.describe('SkillProof V2.3 Production Assessment & Skill Matrix E2E', () => {

  test('Backend Developer: Assessment flow, Explanation panel, explicit Next, and Skill Matrix', async ({ page }) => {
    test.setTimeout(60000);

    // 1. Role Selection
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Skill Selection
    // Check SQL checkbox or first available
    const sqlCheckbox = page.locator('input[type="checkbox"][value="shared.sql"]');
    if (await sqlCheckbox.count() > 0) {
      await sqlCheckbox.check();
    } else {
      await page.locator('input[type="checkbox"]').first().check();
    }

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

    // 3. Interview Page
    await expect(page).toHaveURL(/\/assessment\/interview$/);

    // Loop through interview sequence until completion
    for (let step = 0; step < 25; step++) {
      if (page.url().includes('/assessment/result')) break;

      // If explanation is visible, click Next
      const isNextVisible = await page.locator('[data-testid="next-question-button"]').isVisible().catch(() => false);
      if (isNextVisible) {
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      // If answer input is visible, fill and submit
      const isInputVisible = await page.locator('[data-testid="answer-input"]').isVisible().catch(() => false);
      if (isInputVisible) {
        await page.locator('[data-testid="answer-input"]').fill('I use proper indexes, transaction isolation levels, connection pooling, and query profiling.');
        await page.click('[data-testid="submit-answer-button"]');
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await expect(page.locator('[data-testid="reference-explanation"]')).toBeVisible();
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Skill Matrix Result Page
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await expect(page.locator('[data-testid="skill-matrix-header"]')).toBeVisible();
    await expect(page.locator('[data-testid="skill-matrix-table"]')).toBeVisible();

    // Verify Not Assessed is distinct and mapped to ROLE COVERAGE GAP
    const notAssessedBadge = page.locator('[data-testid="count-not-assessed"]');
    await expect(notAssessedBadge).toBeVisible();

    // Click competency row to inspect detail drawer
    const detailPanel = page.locator('[data-testid="skill-detail-panel"]');
    await expect(detailPanel).toBeVisible();
    await expect(page.locator('[data-testid="selected-why-level"]')).toBeVisible();

    // Verify Continue to Learning Roadmap button is present
    await expect(page.locator('[data-testid="continue-to-roadmap-button"]')).toBeVisible();
  });

  test('Frontend Developer: Mandatory composition & interview initiation', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select a covered mandatory fundamental (HTML core or HTTP)
    const coveredCheckbox = page.locator('input[type="checkbox"]').first();
    await coveredCheckbox.check();

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

    // Must proceed to interview with calibrated question
    await expect(page).toHaveURL(/\/assessment\/interview$/);
    await expect(page.locator('[data-testid="question-card"]')).toBeVisible();
    await expect(page.locator('[data-testid="question-text"]')).toBeVisible();

    // Submit technical answer
    await page.locator('[data-testid="answer-input"]').fill('I use semantic HTML5 elements like nav, main, article, section, and appropriate ARIA attributes for full accessibility.');
    await page.click('[data-testid="submit-answer-button"]');

    // Verify Post-Answer Explanation appears
    await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible();
    await expect(page.locator('[data-testid="reference-explanation"]')).toBeVisible();
  });

  test('Data Analyst: Mandatory composition & interview initiation', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-data-analyst"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select a covered skill: shared.sql or spreadsheets
    const sqlCheckbox = page.locator('input[type="checkbox"][value="shared.sql"]');
    if (await sqlCheckbox.count() > 0) {
      await sqlCheckbox.check();
    } else {
      // Order 2 is spreadsheets, order 3 is sql
      await page.locator('input[type="checkbox"]').nth(1).check();
    }

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

    // Must proceed to interview with calibrated question
    await expect(page).toHaveURL(/\/assessment\/interview$/);
    await expect(page.locator('[data-testid="question-card"]')).toBeVisible();
    await expect(page.locator('[data-testid="question-text"]')).toBeVisible();

    // Submit technical answer
    await page.locator('[data-testid="answer-input"]').fill('I write SQL queries using CTEs, window functions like ROW_NUMBER(), and explicit JOIN conditions while verifying execution plans.');
    await page.click('[data-testid="submit-answer-button"]');

    // Verify Post-Answer Explanation appears
    await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible();
    await expect(page.locator('[data-testid="reference-explanation"]')).toBeVisible();
  });

});
