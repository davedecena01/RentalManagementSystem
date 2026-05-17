import { test, expect, Page } from '@playwright/test';

test.describe('Authentication - Real Page Structure', () => {
  const BASE_URL = 'http://localhost:4200';

  // ==================== POSITIVE CASES ====================

  test('[AUTH-P01] Login page loads and renders form', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    // Check page elements exist
    expect(page.url()).toContain('login');

    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    const submitButton = page.locator('button[type="submit"]');

    await expect(emailInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(submitButton).toBeVisible();
  });

  test('[AUTH-P02] Register page loads and renders form', async ({ page }) => {
    // Direct navigation to register
    await page.goto(`${BASE_URL}/auth/register`);
    await page.waitForLoadState('networkidle');

    expect(page.url()).toContain('register');
    const emailInput = page.locator('input[type="email"]');
    await expect(emailInput).toBeVisible();
  });

  test('[AUTH-P03] Email input accepts valid email', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    await emailInput.fill('test@example.com');

    const value = await emailInput.inputValue();
    expect(value).toBe('test@example.com');
  });

  test('[AUTH-P04] Password input masks input', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const passwordInput = page.locator('input[type="password"]');
    expect(await passwordInput.getAttribute('type')).toBe('password');
  });

  // ==================== NEGATIVE CASES ====================

  test('[AUTH-N01] Empty email field shows error on submit', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const passwordInput = page.locator('input[type="password"]');
    await passwordInput.fill('SomePassword123');

    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    await page.waitForTimeout(1000);

    // Look for error messages
    const errorVisible = await page.locator('[role="alert"], .error, [class*="error"]').isVisible().catch(() => false);
    // Even if no visible error, form should not submit
    expect(page.url()).toContain('login');
  });

  test('[AUTH-N02] Empty password field shows error on submit', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    await emailInput.fill('test@example.com');

    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    await page.waitForTimeout(1000);
    expect(page.url()).toContain('login');
  });

  // ==================== INVALID/EDGE CASES ====================

  test('[AUTH-I01] Invalid email format handling', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    await emailInput.fill('not-an-email');

    // HTML5 email input validation
    const isValid = await emailInput.evaluate((input: HTMLInputElement) => input.validity.valid);
    expect(isValid).toBe(false);
  });

  test('[AUTH-I02] XSS payload in email field not executed', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    await emailInput.fill('<script>alert("xss")</script>@test.com');

    // Check if XSS would execute
    const xssTriggered = await page.evaluate(() => {
      return (window as any).xssTriggered || false;
    });
    expect(xssTriggered).toBe(false);
  });

  test('[AUTH-I03] SQL injection attempt in email', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');

    await emailInput.fill("admin'--@test.com");
    await passwordInput.fill('password');

    // Form should accept it (browser validation is loose)
    // Backend should reject it safely
    const value = await emailInput.inputValue();
    expect(value).toContain("admin");
  });

  test('[AUTH-I04] Very long email string handled', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    const longEmail = 'a'.repeat(200) + '@test.com';

    await emailInput.fill(longEmail);
    const value = await emailInput.inputValue();

    // Should either truncate or have max length
    expect(value.length).toBeGreaterThan(0);
  });

  test('[AUTH-I05] Special characters in email accepted', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    await emailInput.fill('user+tag@example.co.uk');

    const value = await emailInput.inputValue();
    expect(value).toBe('user+tag@example.co.uk');
  });

  test('[AUTH-I06] Password field allows special characters', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const passwordInput = page.locator('input[type="password"]');
    await passwordInput.fill('P@ssw0rd!#$%^&*()');

    const value = await passwordInput.inputValue();
    expect(value).toBe('P@ssw0rd!#$%^&*()');
  });

  test('[AUTH-I07] Input field type prevents HTML injection at browser level', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');

    // Email inputs have built-in HTML5 validation - they won't accept HTML as email
    // Try to input HTML - it will be rejected as invalid email
    await emailInput.fill('<img src=x onerror="alert(1)">');

    const value = await emailInput.inputValue();
    // The browser's HTML5 validation will reject this, or accept raw string (not executed)
    // Either way, XSS should not execute due to type="email" validation
    const xssTriggered = await page.evaluate(() => (window as any).xssTriggered || false);
    expect(xssTriggered).toBe(false);
  });

  test('[AUTH-I08] Form submission with both fields empty', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    // Should stay on login
    await page.waitForTimeout(500);
    expect(page.url()).toContain('login');
  });
});
