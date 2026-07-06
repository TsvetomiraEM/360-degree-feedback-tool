import { test, expect } from '@playwright/test';
import { credentials, login } from '../helpers/auth';
import { cleanupE2eEntities } from '../helpers/cleanup';

test.describe('Feedback360 smoke tests', () => {
  test('manager can log in and reach dashboard', async ({ page }) => {
    await login(page, credentials.manager);
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { name: 'Team Dashboard' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'New 360 Review' })).toBeVisible();
  });

  test('admin is forbidden from results route', async ({ page }) => {
    await login(page, credentials.admin);
    await expect(page).toHaveURL('/admin/users');
    await page.goto('/results');
    await expect(page).toHaveURL('/admin/users');
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  });

  test.describe('template creation', () => {
    let templateName = '';

    test.afterEach(async ({ request }) => {
      if (templateName) {
        await cleanupE2eEntities(request, { templateName });
        templateName = '';
      }
    });

    test('manager can create a survey template', async ({ page }) => {
      templateName = `E2E Template ${Date.now()}`;

      await login(page, credentials.manager);
      await page.goto('/templates');
      await expect(page.getByRole('heading', { name: 'Survey Templates' })).toBeVisible();

      await page.getByRole('button', { name: 'New Template' }).click();
      const dialog = page.getByRole('dialog');
      await expect(dialog).toBeVisible();
      await dialog.getByRole('textbox', { name: 'Name' }).fill(templateName);
      await dialog.getByLabel('Question').first().fill('Demonstrates clear communication');
      await dialog.getByRole('button', { name: 'Save' }).click();

      await expect(page.getByRole('dialog')).not.toBeVisible();
      await expect(page.getByText(templateName)).toBeVisible();
    });
  });
});
