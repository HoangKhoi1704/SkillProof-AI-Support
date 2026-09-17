import { test, expect } from '@playwright/test';

test.describe('SkillProof V2.5 Curated Learning Resources & Project Matching E2E', () => {

  test('Frontend Developer: Roadmap -> Node Resources -> Curated Projects -> Project Detail', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Start at Roles and choose Frontend Developer
    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select HTML & Web Standards
    const htmlCheckbox = page.locator('input[type="checkbox"][value="frontend.html-core"]');
    if (await htmlCheckbox.count() > 0) {
      await htmlCheckbox.check();
    } else {
      await page.locator('input[type="checkbox"]').first().check();
    }

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

    // 3. Complete Interview
    await expect(page).toHaveURL(/\/assessment\/interview$/);

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
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Navigate from Result to Roadmap
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    // 5. Inspect frontend.html-core node
    const htmlNode = page.locator('[data-testid="roadmap-node-frontend.html-core"]');
    await expect(htmlNode).toBeVisible();
    await htmlNode.click();
    await expect(page).toHaveURL(/\/roadmap\/frontend\.html-core$/);

    // 6. Verify Verified Learning Resources Section
    const resourceSection = page.locator('[data-testid="learning-resources-section"]');
    await expect(resourceSection).toBeVisible();
    await expect(page.locator('[data-testid="resource-card"]').first()).toBeVisible();

    const resourceLink = page.locator('[data-testid="resource-link"]').first();
    const href = await resourceLink.getAttribute('href');
    expect(href).not.toBeNull();
    expect(href).toMatch(/^https:\/\//);
    expect(href).not.toContain('youtube.com');
    expect(href).not.toContain('youtu.be');

    // 7. Navigate to Projects Catalog
    await page.click('[data-testid="continue-to-projects-button"]');
    await expect(page).toHaveURL(/\/projects$/);

    // 8. Verify Curated Projects Page
    await expect(page.locator('[data-testid="practice-projects-section"]')).toBeVisible();
    await expect(page.locator('[data-testid="portfolio-projects-section"]')).toBeVisible();

    // Verify absence of arbitrary match percentages
    const projectsContent = await page.locator('[data-testid="projects-page"]').innerText();
    expect(projectsContent).not.toContain('% match');
    expect(projectsContent).not.toContain('match score');

    // 9. Inspect Practice Project Detail
    const viewDetailBtn = page.locator('[data-testid="view-practice-detail"]').first();
    await expect(viewDetailBtn).toBeVisible();
    await viewDetailBtn.click();
    await expect(page).toHaveURL(/\/projects\/proj-fe-/);

    // Verify Project Detail Elements
    await expect(page.locator('[data-testid="detail-project-title"]')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Required Technical Deliverables' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Verifiable Evidence Standards' })).toBeVisible();

    // Verify external tutorial link if present
    const extLink = page.locator('[data-testid="external-source-link"]');
    if (await extLink.count() > 0) {
      const extHref = await extLink.getAttribute('href');
      expect(extHref).toMatch(/^https:\/\//);
    }

    // 10. Back to Projects
    await page.click('[data-testid="back-to-projects-button"]');
    await expect(page).toHaveURL(/\/projects$/);
  });

  test('Backend Developer: Curated Project Selection -> Portfolio Evidence Handoff', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Start at Roles and choose Backend Developer
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select SQL
    const sqlCheckbox = page.locator('input[type="checkbox"][value="shared.sql"]');
    if (await sqlCheckbox.count() > 0) {
      await sqlCheckbox.check();
    } else {
      await page.locator('input[type="checkbox"]').first().check();
    }

    await page.click('[data-testid="start-interview-button"]');
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
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Navigate from Result to Roadmap -> Projects
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    // 5. Navigate to Projects
    await page.goto('/projects');
    await expect(page).toHaveURL(/\/projects$/);

    // 6. Verify Backend Portfolio Project Card
    const portfolioCard = page.locator('[data-testid="portfolio-project-card"]').first();
    await expect(portfolioCard).toBeVisible();
    await expect(portfolioCard.getByText('Production-Grade Resilient Order & Inventory Service')).toBeVisible();

    // 7. Select Portfolio Project
    const selectBtn = page.locator('[data-testid="select-portfolio-project-button"]').first();
    await expect(selectBtn).toBeVisible();
    await selectBtn.click();

    // 8. Verify seamless transition to Portfolio page with evidence form
    await expect(page).toHaveURL(/\/portfolio$/, { timeout: 15000 });
    await expect(page.locator('[data-testid="evidence-form"]')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('[data-testid="repo-url-input"]')).toBeVisible();
  });

  test('Data Analyst: Appropriate Projects & Evidence Flexibility (No Mandatory Deep Learning)', async ({ page }) => {
    test.setTimeout(90000);

    // 1. Start at Roles and choose Data Analyst
    await page.goto('/roles');
    await page.click('[data-testid="role-card-data-analyst"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // 2. Select SQL (has question coverage)
    const sqlCard = page.locator('[data-testid="skill-card-shared.sql"]');
    if (await sqlCard.count() > 0) {
      await sqlCard.click();
    } else {
      await page.locator('[data-testid^="skill-card-"]').nth(2).click();
    }

    await page.click('[data-testid="start-interview-button"]');
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
          'I use XLOOKUP, INDEX/MATCH for robust bidirectional lookups, and audit formula dependencies with error handling to maintain financial models.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Navigate from Result to Roadmap
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    // 5. Navigate to Projects
    await page.goto('/projects');
    await expect(page).toHaveURL(/\/projects$/);

    // 6. Verify Data Analyst projects
    await expect(page.getByText('Enterprise Revenue Intelligence: Statistical Modeling & Executive BI Dashboard')).toBeVisible();

    // Verify Deep Learning is NOT in the recommended project requirements
    const pageText = await page.innerText('[data-testid="projects-page"]');
    expect(pageText).not.toContain('data-analyst.deep-learning');

    // 7. Inspect Project Detail
    await page.click('[data-testid="view-portfolio-detail"]');
    await expect(page).toHaveURL(/\/projects\/proj-da-bi-portfolio$/);

    // 8. Verify Data Analyst Evidence Flexibility Note
    await expect(page.getByText('Data Analyst Evidence Flexibility')).toBeVisible();
    await expect(page.getByText('Deployed web applications are not required')).toBeVisible();
  });

});
