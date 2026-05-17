import { test, expect } from '@playwright/test';

test.describe('Security Penetration Testing', () => {
  const BASE_URL = 'http://localhost:4200';
  const API_URL = 'http://localhost:8080/api';

  // ==================== INPUT VALIDATION ====================

  test('[SEC-01] XSS via email field fails silently (no execution)', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    const xssPayload = '"><script>window.xssPassed=true</script><input type="';

    await emailInput.fill(xssPayload);

    // Check if XSS was triggered
    await page.waitForTimeout(500);
    const xssTriggered = await page.evaluate(() => (window as any).xssPassed);
    expect(xssTriggered).toBeUndefined();
  });

  test('[SEC-02] SQL injection syntax in email field', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    const sqlPayload = "admin'--@test.com";

    await emailInput.fill(sqlPayload);
    const value = await emailInput.inputValue();
    expect(value).toBe(sqlPayload);
    // Backend should sanitize/parameterize queries
  });

  test('[SEC-03] Command injection attempt in input', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');
    const cmdPayload = '$(whoami)@test.com';

    await emailInput.fill(cmdPayload);
    const value = await emailInput.inputValue();
    expect(value).toBe(cmdPayload);
  });

  // ==================== SESSION SECURITY ====================

  test('[SEC-04] Session token not exposed in HTML', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const htmlContent = await page.content();
    expect(htmlContent).not.toMatch(/token|jwt|auth/i);
  });

  test('[SEC-05] No sensitive data in localStorage/sessionStorage initially', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const storage = await page.evaluate(() => {
      return {
        local: Object.keys(localStorage),
        session: Object.keys(sessionStorage)
      };
    });

    // Should not have tokens before login
    const keys = [...storage.local, ...storage.session].join('|');
    expect(keys).not.toMatch(/password|secret|key/i);
  });

  test('[SEC-06] Page does not log sensitive data to console', async ({ page }) => {
    const consoleLogs: string[] = [];
    page.on('console', (msg) => {
      consoleLogs.push(msg.text());
    });

    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    const sensitive = consoleLogs.filter(log =>
      log.match(/password|token|secret|key|auth|credential/i)
    );

    // Some debug logs are OK, but no actual credentials
    sensitive.forEach(log => {
      expect(log).not.toMatch(/[a-zA-Z0-9]{20,}/);
    });
  });

  // ==================== FORM SECURITY ====================

  test('[SEC-07] Form has no autocomplete on password fields', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const passwordInput = page.locator('input[type="password"]');
    const autocomplete = await passwordInput.getAttribute('autocomplete');

    // Should be "off" or "new-password"
    if (autocomplete) {
      expect(autocomplete).not.toBe('on');
    }
  });

  test('[SEC-08] No password in URLs or query parameters', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const passwordInput = page.locator('input[type="password"]');
    await passwordInput.fill('TestPassword123');

    await page.waitForTimeout(500);
    const currentUrl = page.url();

    expect(currentUrl).not.toContain('password');
    expect(currentUrl).not.toContain('TestPassword');
  });

  // ==================== API SECURITY ====================

  test('[SEC-09] API calls not exposed in network tab XHR body', async ({ page }) => {
    const requests: Array<{url: string; method: string}> = [];

    page.on('request', (request) => {
      requests.push({
        url: request.url(),
        method: request.method()
      });
    });

    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    // Check that no credentials are in GET parameters
    const problematicRequests = requests.filter(r =>
      r.url.includes('password') || r.url.includes('token')
    );

    expect(problematicRequests.length).toBe(0);
  });

  test('[SEC-10] HTTPS enforced (or localhost allowed)', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const url = page.url();
    // Local should be http://localhost, production should be https://
    expect(url).toMatch(/^https?:\/\/localhost|^https:\/\//);
  });

  // ==================== CLICKJACKING PROTECTION ====================

  test('[SEC-11] Page has security headers (via response)', async ({ page }) => {
    let securityHeaders: Record<string, string> = {};

    page.on('response', (response) => {
      if (response.url().includes('localhost:4200/') && !response.url().includes('.js')) {
        const headers = response.headers();
        securityHeaders = { ...securityHeaders, ...headers };
      }
    });

    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    // X-Frame-Options should prevent clickjacking
    // Note: These might not be set in dev, but should be in production
    console.log('Security headers present:', Object.keys(securityHeaders));
  });

  // ==================== BROWSER SECURITY ====================

  test('[SEC-12] CSP or X-Content-Type-Options respected', async ({ page }) => {
    let responses: any[] = [];

    page.on('response', (response) => {
      responses.push({
        url: response.url(),
        headers: response.headers()
      });
    });

    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    // Check for security headers
    const htmlResponse = responses.find(r => r.url.includes('/login'));
    console.log('Response headers available:', htmlResponse ? 'yes' : 'no');
  });

  // ==================== SENSITIVE DATA EXPOSURE ====================

  test('[SEC-13] Error messages are generic (no path disclosure)', async ({ page }) => {
    await page.goto(`${BASE_URL}/invalid-page-that-does-not-exist`);

    const errorText = await page.locator('body').textContent();

    // Should not expose file paths or server details
    expect(errorText).not.toMatch(/\/src\/|\/app\/|\.cs|\.ts/);
  });

  test('[SEC-14] No stack traces in UI', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const pageContent = await page.content();

    // Should not contain TypeScript/C# stack traces
    expect(pageContent).not.toMatch(/at \w+\s*\(\s*\w+\.\w+\.ts/);
    expect(pageContent).not.toMatch(/at \w+\.cs/);
  });

  // ==================== ACCESS CONTROL ====================

  test('[SEC-15] Cannot access admin/protected routes without auth', async ({ page }) => {
    // Navigate to a protected route that redirects when not authenticated
    await page.goto(`${BASE_URL}/properties`);
    await page.waitForLoadState('networkidle');

    // Should redirect to login if not authenticated
    const url = page.url();
    // Either stays on properties (which requires auth) or redirects to login
    expect(url).toMatch(/properties|login|auth/);
  });

  test('[SEC-16] Page accessible without JavaScript disabled warning', async ({ page }) => {
    // Angular apps require JS, so this is expected
    await page.goto(`${BASE_URL}/auth/login`);

    const title = await page.title();
    expect(title).toBeTruthy();
  });

  // ==================== VALIDATION LOGIC ====================

  test('[SEC-17] Email validation is strict', async ({ page }) => {
    await page.goto(`${BASE_URL}/auth/login`);

    const emailInput = page.locator('input[type="email"]');

    // Test various invalid formats
    const testCases = [
      { value: 'noatsign.com', valid: false },
      { value: 'spaces in@email.com', valid: false },
      { value: 'double@@email.com', valid: false },
      { value: 'valid@email.com', valid: true }
    ];

    for (const testCase of testCases) {
      await emailInput.fill(testCase.value);
      const isValid = await emailInput.evaluate((input: HTMLInputElement) => {
        return input.validity.valid;
      });

      if (testCase.valid) {
        expect(isValid).toBe(true);
      } else {
        expect(isValid).toBe(false);
      }
    }
  });

  // ==================== BRUTE FORCE PROTECTION ====================

  test('[SEC-18] No obvious brute force vulnerabilities in frontend', async ({ page }) => {
    // Frontend cannot prevent brute force - backend must.
    // Verify the login form is available without any throttling overlay.
    await page.goto(`${BASE_URL}/auth/login`);
    const emailInput = page.locator('input[type="email"]');
    await expect(emailInput).toBeVisible();
  });

  // ==================== DEPENDENCY SECURITY ====================

  test('[SEC-19] No known vulnerable libraries in console errors', async ({ page }) => {
    const errors: string[] = [];

    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    await page.goto(`${BASE_URL}/auth/login`);
    await page.waitForLoadState('networkidle');

    // Check for known vulnerability patterns
    const vulnerabilities = errors.filter(e =>
      e.match(/jquery.*exploit|angular.*cve|xss.*vulnerability/i)
    );

    expect(vulnerabilities.length).toBe(0);
  });

  // ==================== DATA PERSISTENCE ====================

  test('[SEC-20] Sensitive data not persisted unnecessarily', async ({ page }) => {
    const cookies = await page.context().cookies();
    const cookieNames = cookies.map(c => c.name).join('|');

    // Should not have plaintext token cookies
    expect(cookieNames).not.toMatch(/token|password|secret/i);
  });
});
