import { test, expect } from '@playwright/test';

test.describe('SkillProof V2.6 Project Evidence Verification & Evidence-Gated Portfolio Proof E2E', () => {

  test('Frontend Developer: Curated Project -> Deployed Web + Repo Evidence -> Evidence Matrix & Gated Proof', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Roles -> Frontend Developer
    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select Skill with question coverage
    await page.locator('input[type="checkbox"]').first().check();

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();
    await expect(page).toHaveURL(/\/assessment\/interview$/);

    // 3. Complete Interview
    for (let step = 0; step < 25; step++) {
      if (page.url().includes('/assessment/result')) break;

      const isNextVisible = await page.locator('[data-testid="next-question-button"]').isVisible().catch(() => false);
      if (isNextVisible) {
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      const isInputVisible = await page.locator('[data-testid="answer-input"]').isVisible().catch(() => false);
      if (isInputVisible) {
        await page.locator('[data-testid="answer-input"]').fill(
          'I use semantic HTML5 elements like main, nav, and article, ARIA landmarks, WCAG accessibility standards, and modern CSS Grid layout.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await page.waitForTimeout(1000);
        const nextBtn = page.locator('[data-testid="next-question-button"]');
        if (await nextBtn.isVisible()) {
          await nextBtn.click();
        }
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Result -> Roadmap -> Projects
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    await page.goto('/projects');
    await expect(page).toHaveURL(/\/projects$/);

    // 5. Select Portfolio Project
    const selectBtn = page.locator('[data-testid="select-portfolio-project-button"]').first();
    await expect(selectBtn).toBeVisible();
    await selectBtn.click();

    // 6. Land on /portfolio
    await expect(page).toHaveURL(/\/portfolio$/, { timeout: 15000 });
    await expect(page.locator('[data-testid="evidence-form"]')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('[data-testid="repo-url-input"]')).toBeVisible();

    // 7. Verify Frontend Fields: repo + deployed web
    await expect(page.locator('[data-testid="deployed-url-input"]')).toBeVisible();

    // 8. Submit Evidence Fixture (verified repo + deployed web app fixture)
    await page.locator('[data-testid="repo-url-input"]').fill('https://github.com/skillproof-fixtures/fe-todo-app');
    await page.locator('[data-testid="deployed-url-input"]').fill('https://demo-app.skillproof.dev/todo');
    await page.locator('[data-testid="evidence-summary-input"]').fill(
      'Implemented accessible design system with WCAG AAA color contrast, keyboard navigation, and responsive CSS tokens.'
    );
    await page.locator('[data-testid="evidence-architecture-input"]').fill(
      'Used CSS custom properties for token hierarchy and zero-runtime styled components.'
    );
    await page.locator('[data-testid="evidence-testing-input"]').fill(
      'Verified with automated accessibility tests and interaction test suites.'
    );

    await page.click('[data-testid="submit-evidence-button"]');

    // 9. Verify Verified Evidence View
    await expect(page.locator('[data-testid="verified-portfolio-view"]')).toBeVisible({ timeout: 15000 });
    const overallStatus = page.locator('[data-testid="evaluation-overall-status"]');
    await expect(overallStatus).toBeVisible();
    await expect(overallStatus).toContainText('Demonstrated');

    // 10. Verify Requirement Evidence Matrix is rendered
    await expect(page.getByText('Requirement Evidence Matrix')).toBeVisible();
    await expect(page.locator('table')).toBeVisible();

    // 11. Verify Portfolio Bullets & Gated Proof
    await expect(page.locator('[data-testid="portfolio-proof-view"]')).toBeVisible();
    await expect(page.locator('[data-testid="cv-bullets-list"]')).toBeVisible();
    const cvText = await page.locator('[data-testid="cv-bullets-list"]').innerText();
    expect(cvText.length).toBeGreaterThan(20);

    // 12. Strict Assertions: NO numeric scores, percentages, or score leaks
    const pageContent = await page.content();
    expect(pageContent).not.toContain('% match');
    expect(pageContent).not.toContain('/100');
    expect(pageContent).not.toContain('expectedSignals');
    expect(pageContent).not.toContain('systemInstructions');
  });

  test('Backend Developer: Curated Project -> Repo + API Evidence -> Traceability & Re-assessment CTA', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Roles -> Backend Developer
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select SQL
    const sqlCard = page.locator('[data-testid="skill-card-shared.sql"]');
    if (await sqlCard.count() > 0) {
      await sqlCard.click();
    } else {
      await page.locator('[data-testid^="skill-card-"]').first().click();
    }

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();
    await expect(page).toHaveURL(/\/assessment\/interview$/);

    // 3. Complete Interview
    for (let step = 0; step < 25; step++) {
      if (page.url().includes('/assessment/result')) break;

      const isNextVisible = await page.locator('[data-testid="next-question-button"]').isVisible().catch(() => false);
      if (isNextVisible) {
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      const isInputVisible = await page.locator('[data-testid="answer-input"]').isVisible().catch(() => false);
      if (isInputVisible) {
        await page.locator('[data-testid="answer-input"]').fill(
          'I use composite B-Tree indexes on frequently filtered columns, analyze execution plans with EXPLAIN ANALYZE, and prevent N+1 queries.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await page.waitForTimeout(1000);
        const nextBtn = page.locator('[data-testid="next-question-button"]');
        if (await nextBtn.isVisible()) {
          await nextBtn.click();
        }
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Navigate from Result -> Roadmap -> Projects
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    await page.goto('/projects');
    await expect(page).toHaveURL(/\/projects$/);

    // 5. Select Backend Portfolio Project
    const selectBtn = page.locator('[data-testid="select-portfolio-project-button"]').first();
    await expect(selectBtn).toBeVisible();
    await selectBtn.click();

    // 6. Verify transition to /portfolio
    await expect(page).toHaveURL(/\/portfolio$/, { timeout: 15000 });
    await expect(page.locator('[data-testid="evidence-form"]')).toBeVisible();

    // 7. Submit Backend Evidence Fixture
    await page.locator('[data-testid="repo-url-input"]').fill('https://github.com/skillproof-fixtures/be-order-service');
    await page.locator('[data-testid="deployed-url-input"]').fill('https://api.skillproof.dev/orders');
    await page.locator('[data-testid="evidence-summary-input"]').fill(
      'Built a production-grade ordering API in C# with database transaction isolation and automated test suites.'
    );
    await page.locator('[data-testid="evidence-architecture-input"]').fill(
      'Implemented explicit database transaction management using Repeatable Read isolation. Added composite index on (OrderId, Status).'
    );
    await page.locator('[data-testid="evidence-testing-input"]').fill(
      'Implemented automated test suite with unit tests and test doubles to verify boundary conditions.'
    );

    await page.click('[data-testid="submit-evidence-button"]');

    // 8. Verify Evaluation View
    await expect(page.locator('[data-testid="verified-portfolio-view"]')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('[data-testid="evaluation-overall-status"]')).toContainText('Demonstrated');

    // 9. Verify Requirement Evidence Matrix
    await expect(page.getByText('Requirement Evidence Matrix')).toBeVisible();

    // 10. Verify Evidence-Backed CV Bullet
    const cvBullets = page.locator('[data-testid="cv-bullets-list"]');
    await expect(cvBullets).toBeVisible();
    const cvText = await cvBullets.innerText();
    expect(cvText).toContain('supported by verified repository artifacts and observable technical deliverables');

    // 11. Verify Re-assessment CTA
    const reassessBtn = page.locator('[data-testid="reassess-skills-btn"]').first();
    await expect(reassessBtn).toBeVisible();
    await reassessBtn.click();
    await expect(page).toHaveURL(/\/assessment\/skills$/);
  });

  test('Data Analyst: Appropriate Projects & Evidence Flexibility (No Website Required)', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Roles -> Data Analyst
    await page.goto('/roles');
    await page.click('[data-testid="role-card-data-analyst"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select SQL (covered competency for Data Analyst)
    await page.locator('input[type="checkbox"]').nth(2).check();

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();
    await expect(page).toHaveURL(/\/assessment\/interview$/);

    // 3. Complete Interview
    for (let step = 0; step < 25; step++) {
      if (page.url().includes('/assessment/result')) break;

      const isNextVisible = await page.locator('[data-testid="next-question-button"]').isVisible().catch(() => false);
      if (isNextVisible) {
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      const isInputVisible = await page.locator('[data-testid="answer-input"]').isVisible().catch(() => false);
      if (isInputVisible) {
        await page.locator('[data-testid="answer-input"]').fill(
          'I use window functions like ROW_NUMBER() and RANK() partitioned by customer_id, along with CTEs for clean data aggregation.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await page.waitForTimeout(1000);
        const nextBtn = page.locator('[data-testid="next-question-button"]');
        if (await nextBtn.isVisible()) {
          await nextBtn.click();
        }
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Result -> Roadmap -> Projects
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    await page.goto('/projects');
    await expect(page).toHaveURL(/\/projects$/);

    // 5. Select Data Analyst Portfolio Project
    const selectBtn = page.locator('[data-testid="select-portfolio-project-button"]').first();
    await expect(selectBtn).toBeVisible();
    await selectBtn.click();

    // 6. Verify transition to /portfolio
    await expect(page).toHaveURL(/\/portfolio$/, { timeout: 15000 });
    await expect(page.locator('[data-testid="evidence-form"]')).toBeVisible();

    // 7. Verify Data Analyst Notice: Website Not Required!
    await expect(page.getByText('A live website deployment is not required')).toBeVisible();

    // 8. Verify Data Analyst inputs: Notebook, Dashboard, Dataset
    await expect(page.locator('[data-testid="notebook-url-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="dashboard-url-input"]')).toBeVisible();

    // 9. Submit Data Analyst Evidence Fixture
    await page.locator('[data-testid="repo-url-input"]').fill('https://github.com/skillproof-fixtures/da-revenue-intelligence');
    await page.locator('[data-testid="notebook-url-input"]').fill('https://github.com/skillproof-fixtures/da-revenue-intelligence/blob/main/notebooks/revenue_statistical_modeling.ipynb');
    await page.locator('[data-testid="dashboard-url-input"]').fill('https://analytics.skillproof.dev/dashboards/revenue-executive');
    await page.locator('[data-testid="evidence-summary-input"]').fill(
      'Executed customer revenue cohort analysis and built interactive executive dashboard.'
    );

    await page.click('[data-testid="submit-evidence-button"]');

    // 10. Verify Evaluation View
    await expect(page.locator('[data-testid="verified-portfolio-view"]')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('[data-testid="evaluation-overall-status"]')).toContainText('Demonstrated');
    await expect(page.locator('[data-testid="portfolio-proof-view"]')).toBeVisible();
  });

  test('Invalid / Insufficient Evidence: Neutral Handling & No Overclaiming', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Roles -> Backend Developer
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select skill
    await page.locator('[data-testid^="skill-card-"]').first().click();
    await page.click('[data-testid="start-interview-button"]');
    await expect(page).toHaveURL(/\/assessment\/interview$/);

    // Complete interview
    for (let step = 0; step < 25; step++) {
      if (page.url().includes('/assessment/result')) break;
      const isNext = await page.locator('[data-testid="next-question-button"]').isVisible().catch(() => false);
      if (isNext) {
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }
      const isInput = await page.locator('[data-testid="answer-input"]').isVisible().catch(() => false);
      if (isInput) {
        await page.locator('[data-testid="answer-input"]').fill('Basic answer.');
        await page.click('[data-testid="submit-answer-button"]');
        await page.waitForTimeout(1000);
        const nextBtn = page.locator('[data-testid="next-question-button"]');
        if (await nextBtn.isVisible()) {
          await nextBtn.click();
        }
        await page.waitForTimeout(300);
        continue;
      }
      await page.waitForTimeout(400);
    }

    await page.goto('/projects');
    const selectBtn = page.locator('[data-testid="select-portfolio-project-button"]').first();
    await selectBtn.click();
    await expect(page).toHaveURL(/\/portfolio$/, { timeout: 15000 });

    // Submit invalid / missing evidence fixture
    await page.locator('[data-testid="repo-url-input"]').fill('https://github.com/candidate/empty-or-missing-repo');
    await page.locator('[data-testid="evidence-summary-input"]').fill('No actual implementation provided.');

    await page.click('[data-testid="submit-evidence-button"]');

    // Verify Evaluation View handles insufficient evidence neutrally
    await expect(page.locator('[data-testid="verified-portfolio-view"]')).toBeVisible({ timeout: 15000 });
    const overallStatus = page.locator('[data-testid="evaluation-overall-status"]');
    await expect(overallStatus).toBeVisible();
    const statusText = await overallStatus.innerText();
    expect(['Insufficient Evidence', 'Partially Demonstrated']).toContain(statusText);

    // Verify neutral language in results
    const viewContent = await page.locator('[data-testid="verified-portfolio-view"]').innerText();
    expect(viewContent).not.toContain('You failed');
    expect(viewContent).not.toContain('Your project is bad');
  });

});
