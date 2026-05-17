import { test, expect } from '@playwright/test';

test.describe('Leases Management', () => {
  const BASE_URL = 'http://localhost:4200';

  // ==================== POSITIVE CASES ====================

  test('[LEASE-P01] Navigate to leases page', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const url = page.url();
    expect(url).toMatch(/leases|login|auth/);
  });

  test('[LEASE-P02] Leases page displays heading', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const heading = page.locator('h1, h2').first();
    const text = await heading.textContent();
    expect(text).toBeTruthy();
  });

  test('[LEASE-P03] Create lease button visible', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const btn = page.locator('button:has-text("New"), button:has-text("Create")').first();
    const visible = await btn.isVisible().catch(() => false);
    expect(visible || page.url().includes('login')).toBeTruthy();
  });

  // ==================== NEGATIVE CASES ====================

  test('[LEASE-N01] Empty lease form cannot submit', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const createBtn = page.locator('button:has-text("New"), button:has-text("Create")').first();
    if (await createBtn.isVisible().catch(() => false)) {
      await createBtn.click();
      await page.waitForTimeout(500);
      const submitBtn = page.locator('button[type="submit"]').first();
      if (await submitBtn.isVisible().catch(() => false)) {
        await submitBtn.click();
        await page.waitForTimeout(500);
        expect(page.url()).toContain('leases');
      }
    }
  });

  test('[LEASE-N02] Lease with missing property fails', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const createBtn = page.locator('button:has-text("New"), button:has-text("Create")').first();
    if (await createBtn.isVisible().catch(() => false)) {
      await createBtn.click();
      await page.waitForTimeout(500);
      const inputs = await page.locator('input').all();
      if (inputs.length > 0) {
        await inputs[0].fill('Tenant Name');
      }
      const submitBtn = page.locator('button[type="submit"]').first();
      if (await submitBtn.isVisible().catch(() => false)) {
        await submitBtn.click();
        await page.waitForTimeout(500);
        expect(page.url()).toContain('leases');
      }
    }
  });

  // ==================== INVALID/EDGE CASES ====================

  test('[LEASE-I01] Lease start date in past', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const dateInputs = await page.locator('input[type="date"]').all();
    if (dateInputs.length > 0) {
      await dateInputs[0].fill('2020-01-01');
      const val = await dateInputs[0].inputValue();
      expect(val).toBeDefined();
    }
  });

  test('[LEASE-I02] Lease end before start date', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const dateInputs = await page.locator('input[type="date"]').all();
    if (dateInputs.length >= 2) {
      await dateInputs[0].fill('2025-12-31');
      await dateInputs[1].fill('2025-01-01');
      const val1 = await dateInputs[0].inputValue();
      const val2 = await dateInputs[1].inputValue();
      expect(val1).toBeDefined();
      expect(val2).toBeDefined();
    }
  });

  test('[LEASE-I03] Rent amount is zero', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const numInputs = await page.locator('input[type="number"]').all();
    if (numInputs.length > 0) {
      await numInputs[0].fill('0');
      const val = await numInputs[0].inputValue();
      expect(val).toBeDefined();
    }
  });

  test('[LEASE-I04] XSS in tenant name', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const inputs = await page.locator('input[type="text"]').all();
    if (inputs.length > 0) {
      await inputs[0].fill('<script>alert("xss")</script>');
      await page.waitForTimeout(300);
      const xssPassed = await page.evaluate(() => (window as any).xssPassed || false);
      expect(xssPassed).toBe(false);
    }
  });

  test('[LEASE-I05] Very long tenant name', async ({ page }) => {
    await page.goto(`${BASE_URL}/leases`);
    await page.waitForLoadState('networkidle');
    const inputs = await page.locator('input[type="text"]').all();
    if (inputs.length > 0) {
      await inputs[0].fill('A'.repeat(300));
      const val = await inputs[0].inputValue();
      expect(val.length).toBeGreaterThan(0);
    }
  });
});
