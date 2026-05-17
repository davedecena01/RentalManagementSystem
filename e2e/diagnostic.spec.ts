import { test } from '@playwright/test';

test.describe('Diagnostic - Page Structure Analysis', () => {
  test('[DIAG-01] Inspect login page structure', async ({ page }) => {
    await page.goto('http://localhost:4200/login');
    await page.waitForLoadState('networkidle');

    // Get all form inputs
    const inputs = await page.locator('input').all();
    console.log('\n=== Login Page Inputs ===');
    for (const input of inputs) {
      const name = await input.getAttribute('name');
      const type = await input.getAttribute('type');
      const placeholder = await input.getAttribute('placeholder');
      console.log(`  ${name || 'unnamed'} (type: ${type}, placeholder: ${placeholder})`);
    }

    // Get all buttons
    const buttons = await page.locator('button').all();
    console.log('\n=== Login Page Buttons ===');
    for (const button of buttons) {
      const text = await button.textContent();
      const type = await button.getAttribute('type');
      console.log(`  "${text?.trim()}" (type: ${type})`);
    }

    // Get all text content
    const pageText = await page.locator('body').textContent();
    console.log('\n=== Page contains: ===');
    const lines = (pageText || '').split('\n').filter(l => l.trim().length > 0).slice(0, 20);
    lines.forEach(l => console.log(`  ${l.trim()}`));
  });

  test('[DIAG-02] Inspect register page structure', async ({ page }) => {
    await page.goto('http://localhost:4200/register');
    await page.waitForLoadState('networkidle');

    const inputs = await page.locator('input').all();
    console.log('\n=== Register Page Inputs ===');
    for (const input of inputs) {
      const name = await input.getAttribute('name');
      const type = await input.getAttribute('type');
      console.log(`  ${name || 'unnamed'} (${type})`);
    }

    const buttons = await page.locator('button').all();
    console.log('\n=== Register Page Buttons ===');
    for (const button of buttons) {
      const text = await button.textContent();
      console.log(`  "${text?.trim()}"`);
    }
  });

  test('[DIAG-03] Inspect dashboard page structure', async ({ page }) => {
    await page.goto('http://localhost:4200/dashboard');
    await page.waitForLoadState('networkidle');

    console.log('\n=== Dashboard URL Final ===');
    console.log(`  ${page.url()}`);

    const headings = await page.locator('h1, h2, h3').all();
    console.log('\n=== Dashboard Headings ===');
    for (const heading of headings.slice(0, 10)) {
      const text = await heading.textContent();
      console.log(`  ${text?.trim()}`);
    }

    const cards = await page.locator('[role="article"], .card, [class*="card"]').all();
    console.log(`\n=== Dashboard Cards/Articles: ${cards.length} found ===`);
  });

  test('[DIAG-04] Check API connectivity', async ({ page }) => {
    console.log('\n=== API Connectivity Test ===');

    const responses: { url: string; status: number }[] = [];
    page.on('response', (response) => {
      responses.push({ url: response.url(), status: response.status() });
    });

    await page.goto('http://localhost:4200/login');
    await page.waitForLoadState('networkidle');

    console.log('Network requests:');
    responses.forEach((r) => {
      if (r.url.includes('api') || r.url.includes('localhost')) {
        console.log(`  ${r.status} - ${r.url}`);
      }
    });
  });
});
