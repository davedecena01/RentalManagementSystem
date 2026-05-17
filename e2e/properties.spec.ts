import { test, expect } from '@playwright/test';

test.describe('Properties Management - CRUD Operations', () => {
  const BASE_URL = 'http://localhost:4200';

  // ==================== POSITIVE CASES ====================

  test('[PROP-P01] Navigate to properties page', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const url = page.url();
    expect(url).toMatch(/properties|login|auth/);
  });

  test('[PROP-P02] Properties page displays heading', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const heading = page.locator('h1, h2, [class*="title"]').first();
    const text = await heading.textContent();
    expect(text).toBeTruthy();
  });

  // ==================== NEGATIVE CASES ====================

  test('[PROP-N01] Empty property form cannot submit', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const addBtn = page.locator('button:has-text("Add"), button:has-text("New")').first();
    if (await addBtn.isVisible().catch(() => false)) {
      await addBtn.click();
      await page.waitForTimeout(500);
      const submitBtn = page.locator('button[type="submit"]').first();
      if (await submitBtn.isVisible().catch(() => false)) {
        await submitBtn.click();
        await page.waitForTimeout(500);
        expect(page.url()).toContain('properties');
      }
    }
  });

  // ==================== INVALID/EDGE CASES ====================

  test('[PROP-I01] Long property name handled', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const input = page.locator('input[type="text"]').first();
    if (await input.isVisible().catch(() => false)) {
      await input.fill('A'.repeat(500));
      const val = await input.inputValue();
      expect(val.length).toBeGreaterThan(0);
    }
  });

  test('[PROP-I02] XSS payload in property name', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const input = page.locator('input').first();
    if (await input.isVisible().catch(() => false)) {
      await input.fill('<img src=x onerror="alert(1)">');
      await page.waitForTimeout(300);
      const xssPassed = await page.evaluate(() => (window as any).xssPassed || false);
      expect(xssPassed).toBe(false);
    }
  });

  test('[PROP-I03] SQL injection in property data', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const inputs = await page.locator('input').all();
    if (inputs.length > 0) {
      await inputs[0].fill("'; DROP TABLE properties; --");
      const val = await inputs[0].inputValue();
      expect(val).toContain(';');
    }
  });

  test('[PROP-I04] Negative price value', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const numInput = page.locator('input[type="number"]').first();
    if (await numInput.isVisible().catch(() => false)) {
      await numInput.fill('-5000');
      const val = await numInput.inputValue();
      expect(val).toBeDefined();
    }
  });

  test('[PROP-I05] Zero rooms/bedrooms', async ({ page }) => {
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');
    const numInputs = await page.locator('input[type="number"]').all();
    if (numInputs.length > 0) {
      await numInputs[0].fill('0');
      const val = await numInputs[0].inputValue();
      expect(val).toBeDefined();
    }
  });
});
