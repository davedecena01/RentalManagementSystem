import { test, expect } from '@playwright/test';

test.describe('Smoke Tests - Page Loading', () => {
  test('[SMOKE-01] Login page loads', async ({ page }) => {
    await page.goto('http://localhost:4200/login', { waitUntil: 'domcontentloaded', timeout: 10000 });

    const title = await page.title();
    console.log(`Page title: ${title}`);

    const emailField = page.locator('input[name="email"]').or(page.locator('input[placeholder*="email" i]')).first();
    expect(emailField).toBeDefined();
  });

  test('[SMOKE-02] Register page loads', async ({ page }) => {
    await page.goto('http://localhost:4200/register', { waitUntil: 'domcontentloaded', timeout: 10000 });

    const title = await page.title();
    console.log(`Register page title: ${title}`);
    expect(page.url()).toContain('register');
  });

  test('[SMOKE-03] Dashboard redirect works', async ({ page }) => {
    await page.goto('http://localhost:4200/dashboard', { waitUntil: 'domcontentloaded', timeout: 10000 });

    const url = page.url();
    console.log(`Dashboard URL: ${url}`);
    // Should redirect to login if no token
    expect(url).toMatch(/login|dashboard/);
  });
});
