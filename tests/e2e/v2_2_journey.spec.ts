import { test, expect } from '@playwright/test';

test.describe('SkillProof V2.2 Multi-Page Journey & Session State', () => {

  test('root route / redirects to /roles', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/roles$/);
    await expect(page.locator('h1')).toContainText(/Select.*Target.*Role/i);
  });

  test('/roles displays exactly the 3 primary demo roles from V3 API', async ({ page }) => {
    await page.goto('/roles');

    // Verify exactly 3 primary demo roles
    const roleCards = page.locator('[data-testid^="role-card-"]');
    await expect(roleCards).toHaveCount(3);

    await expect(page.locator('[data-testid="role-card-frontend-developer"]')).toBeVisible();
    await expect(page.locator('[data-testid="role-card-backend-developer"]')).toBeVisible();
    await expect(page.locator('[data-testid="role-card-data-analyst"]')).toBeVisible();

    // Financial Analyst must NOT be displayed in primary V2 UI
    await expect(page.locator('[data-testid="role-card-financial-analyst"]')).toHaveCount(0);
    await expect(page.getByText('Financial Analyst')).toHaveCount(0);
  });

  test('role selection transitions to /assessment/skills with NO preselected skills', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');

    await expect(page).toHaveURL(/\/assessment\/skills$/);
    await expect(page.locator('h1')).toContainText(/Select Skills to Assess/i);

    // Verify 0 skills preselected
    const selectedCheckboxes = page.locator('input[type="checkbox"]:checked');
    await expect(selectedCheckboxes).toHaveCount(0);

    // Selected count badge should show 0 selected
    await expect(page.getByText(/0 of \d+ skills selected/i)).toBeVisible();

    // Begin assessment button should be disabled when 0 skills selected
    const beginButton = page.locator('[data-testid="start-interview-button"]');
    await expect(beginButton).toBeDisabled();
  });

  test('Frontend Developer / Data Analyst honesty: zero question coverage warning', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select an uncovered skill (e.g. CSS Architecture or last skill)
    const uncoveredCheckbox = page.locator('input[type="checkbox"][value="frontend.css-architecture"]');
    if (await uncoveredCheckbox.count() > 0) {
      await uncoveredCheckbox.check();
    } else {
      await page.locator('input[type="checkbox"]').last().check();
    }

    // Attempt to proceed -> triggers honest coverage check
    await page.click('[data-testid="start-interview-button"]');

    // Verify honest zero coverage warning
    const coverageWarning = page.locator('[data-testid="coverage-notice"]');
    await expect(coverageWarning).toBeVisible();
    await expect(coverageWarning).toContainText(/Assessment questions for these skills are not available yet/i);
  });

  test('route guards: unauthorized deep links redirect to valid earlier step', async ({ page }) => {
    // Clear any previous session
    await page.goto('/roles');
    await page.evaluate(() => window.sessionStorage.clear());

    // Try accessing /assessment/skills without role
    await page.goto('/assessment/skills');
    await expect(page).toHaveURL(/\/roles$/);

    // Try accessing /assessment/interview without role/skills
    await page.goto('/assessment/interview');
    await expect(page).toHaveURL(/\/roles$/);

    // Try accessing /assessment/result without evaluation
    await page.goto('/assessment/result');
    await expect(page).toHaveURL(/\/roles$/);

    // Try accessing /roadmap without evaluation
    await page.goto('/roadmap');
    await expect(page).toHaveURL(/\/roles$/);

    // Try accessing /projects without evaluation
    await page.goto('/projects');
    await expect(page).toHaveURL(/\/roles$/);

    // Try accessing /portfolio without evaluation
    await page.goto('/portfolio');
    await expect(page).toHaveURL(/\/roles$/);
  });

  test('/dev/ai-inspector remains accessible directly but unlinked in journey navigation', async ({ page }) => {
    await page.goto('/roles');
    // Navigation bar should not contain link to ai-inspector
    const navLinks = page.locator('header a, nav a');
    const hrefs = await navLinks.evaluateAll(elements => elements.map(el => el.getAttribute('href')));
    expect(hrefs.some(h => h && h.includes('ai-inspector'))).toBe(false);

    // But direct navigation works
    await page.goto('/dev/ai-inspector');
    await expect(page).toHaveURL(/\/dev\/ai-inspector$/);
  });

  test('back/forward navigation and refresh preserves journey state', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select a skill
    const skillCheckbox = page.locator('input[type="checkbox"]').first();
    await skillCheckbox.check();
    await expect(page.getByText(/1 of \d+ skills selected/i)).toBeVisible();

    // Refresh page -> session state should persist
    await page.reload();
    await expect(page).toHaveURL(/\/assessment\/skills$/);
    await expect(page.getByText(/1 of \d+ skills selected/i)).toBeVisible();

    // Browser back to /roles
    await page.goBack();
    await expect(page).toHaveURL(/\/roles$/);

    // Browser forward to /assessment/skills -> selection should still be there
    await page.goForward();
    await expect(page).toHaveURL(/\/assessment\/skills$/);
    await expect(page.getByText(/1 of \d+ skills selected/i)).toBeVisible();
  });

  test('role change invalidates downstream skills and assessment state', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Select skills
    const firstCheckbox = page.locator('input[type="checkbox"]').first();
    await firstCheckbox.check();
    await expect(page.getByText(/1 of \d+ skills selected/i)).toBeVisible();

    // Navigate back and select Frontend Developer instead
    await page.goto('/roles');
    await page.click('[data-testid="role-card-frontend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Downstream skill selections must be cleared for new role
    const selectedCheckboxes = page.locator('input[type="checkbox"]:checked');
    await expect(selectedCheckboxes).toHaveCount(0);
    await expect(page.getByText(/0 of \d+ skills selected/i)).toBeVisible();
  });

  test('journey reset button clears state and returns to /roles', async ({ page }) => {
    await page.goto('/roles');
    await page.click('[data-testid="role-card-backend-developer"]');
    await expect(page).toHaveURL(/\/assessment\/skills$/);

    // Accept dialog when clicking start over
    page.once('dialog', dialog => dialog.accept());

    // Click "Start over"
    const startOverBtn = page.locator('[data-testid="start-over-button"]');
    await expect(startOverBtn).toBeVisible();
    await startOverBtn.click();

    await expect(page).toHaveURL(/\/roles$/);

    // Verify storage cleared
    const rawStorage = await page.evaluate(() => window.sessionStorage.getItem('skillproof_journey_v2'));
    if (rawStorage) {
      const parsed = JSON.parse(rawStorage);
      expect(parsed.selectedRoleId).toBeNull();
      expect(parsed.userSelectedSkillIds).toEqual([]);
    }
  });

});
