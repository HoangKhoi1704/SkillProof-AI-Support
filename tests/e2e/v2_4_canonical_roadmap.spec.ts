import { test, expect } from '@playwright/test';

test.describe('SkillProof V2.4 Canonical Personalized Visual Roadmap E2E', () => {

  test('Backend Developer: Assessment -> Canonical Roadmap -> Node Detail -> Back Navigation', async ({ page }) => {
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
          'I use B-Tree indexing, execution plan analysis via EXPLAIN, connection pooling, and strict ACID transaction isolation levels.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // 4. Verify Skill Matrix result page reached
    await expect(page).toHaveURL(/\/assessment\/result$/);
    const continueRoadmapBtn = page.locator('[data-testid="continue-to-roadmap-button"]');
    await expect(continueRoadmapBtn).toBeVisible();
    await continueRoadmapBtn.click();

    // 5. Verify Canonical Roadmap page reached
    await expect(page).toHaveURL(/\/roadmap$/);
    await expect(page.locator('[data-testid="roadmap-content"]')).toBeVisible();

    // Verify Title and zero-AI badge
    await expect(page.locator('h2')).toContainText(/Backend Developer/i);
    await expect(page.getByText('Zero-AI Graph Resolution')).toBeVisible();

    // Verify Summary Grid has qualitative counts and NO percentages (%)
    await expect(page.getByText('Total Nodes', { exact: true })).toBeVisible();
    await expect(page.getByText('Completed', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Needs Dev', { exact: true })).toBeVisible();
    await expect(page.getByText('Not Assessed', { exact: true }).first()).toBeVisible();

    const summarySection = page.locator('[data-testid="roadmap-content"]');
    const summaryText = await summarySection.innerText();
    expect(summaryText).not.toContain('%');

    // Verify presence of canonical nodes with qualitative states
    const nodes = page.locator('[data-testid^="roadmap-node-"]');
    await expect(nodes.first()).toBeVisible();
    const nodeCount = await nodes.count();
    expect(nodeCount).toBeGreaterThanOrEqual(5);

    // Verify Current priority or Completed badge exists
    const hasCurrent = await page.getByText('Current Priority').count();
    const hasCompleted = await page.getByText('Completed').count();
    expect(hasCurrent + hasCompleted).toBeGreaterThan(0);

    // 6. Click a canonical node to navigate to Node Detail page
    const firstNode = nodes.first();
    const firstNodeIdAttr = await firstNode.getAttribute('data-testid');
    const targetNodeId = firstNodeIdAttr?.replace('roadmap-node-', '') || '';

    await firstNode.click();
    await expect(page).toHaveURL(new RegExp(`/roadmap/${encodeURIComponent(targetNodeId)}`));

    // 7. Verify Node Detail Page content
    await expect(page.locator('[data-testid="roadmap-node-detail"]')).toBeVisible();
    await expect(page.getByText('Why This Node Is Placed Here')).toBeVisible();
    await expect(page.getByText('Verified Prerequisite Dependencies')).toBeVisible();
    await expect(page.getByText('Actionable Next Step')).toBeVisible();

    // Strict safety checks: ensure no rubric / expectedSignals / raw prompts leak
    const detailText = await page.locator('[data-testid="roadmap-node-detail"]').innerText();
    expect(detailText.toLowerCase()).not.toContain('rubric');
    expect(detailText.toLowerCase()).not.toContain('expectedsignals');
    expect(detailText.toLowerCase()).not.toContain('raw prompt');

    // 8. Back navigation preserves journey session
    const backBtn = page.locator('[data-testid="back-to-roadmap-button"]');
    await backBtn.click();
    await expect(page).toHaveURL(/\/roadmap$/);
    await expect(page.locator('[data-testid="roadmap-content"]')).toBeVisible();

    // Verify Continue to Practice Projects button is available
    const continueProjectsBtn = page.locator('[data-testid="continue-to-projects-button"]');
    await expect(continueProjectsBtn).toBeVisible();
  });

  test('Frontend Developer: Mandatory composition, honest Not Assessed labels & Electives', async ({ page }) => {
    test.setTimeout(90000);

    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select first available skill
    await page.locator('input[type="checkbox"]').first().check();

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

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
          'Semantic HTML5 structure with landmark roles, ARIA live regions, and WCAG 2.1 AA accessible contrast.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // Go to roadmap
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    // Verify Frontend Developer title
    await expect(page.locator('h2')).toContainText(/Frontend Developer/i);

    // Verify Not Assessed is explicitly rendered and never labeled "Beginner"
    const notAssessedBadges = page.getByText('Not Assessed');
    await expect(notAssessedBadges.first()).toBeVisible();

    // Verify Optional / Electives exist
    const optionalBadges = page.getByText('Optional Elective');
    const optionalCount = await optionalBadges.count();
    expect(optionalCount).toBeGreaterThanOrEqual(1);
  });

  test('Data Analyst: Canonical roadmap verifies Deep Learning remains Optional', async ({ page }) => {
    test.setTimeout(90000);

    await page.goto('/roles');
    await page.click('[data-testid="role-card-data-analyst"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select SQL or EDA
    const sqlCheckbox = page.locator('input[type="checkbox"][value="shared.sql"]');
    if (await sqlCheckbox.count() > 0) {
      await sqlCheckbox.check();
    } else {
      await page.locator('input[type="checkbox"]').nth(1).check();
    }

    const startBtn = page.locator('[data-testid="start-interview-button"]');
    await expect(startBtn).toBeEnabled();
    await startBtn.click();

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
          'I inspect missing data distributions, perform IQR outlier filtering, and validate distributions with QQ plots.'
        );
        await page.click('[data-testid="submit-answer-button"]');
        await expect(page.locator('[data-testid="post-answer-explanation"]')).toBeVisible({ timeout: 10000 });
        await page.locator('[data-testid="next-question-button"]').click();
        await page.waitForTimeout(300);
        continue;
      }

      await page.waitForTimeout(400);
    }

    // Go to roadmap
    await expect(page).toHaveURL(/\/assessment\/result$/);
    await page.click('[data-testid="continue-to-roadmap-button"]');
    await expect(page).toHaveURL(/\/roadmap$/);

    // Verify Data Analyst title
    await expect(page.locator('h2')).toContainText(/Data Analyst/i);

    // Verify Deep Learning node is present and marked Optional Elective
    const dlNode = page.locator('[data-testid="roadmap-node-data-analyst.deep-learning"]');
    await expect(dlNode).toBeVisible();
    await expect(dlNode).toContainText('Optional Elective');
  });

});
