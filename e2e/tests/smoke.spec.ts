import { test, expect } from '@playwright/test';

const manager = { email: 'manager@feedback360.local', password: 'Manager123!' };
const admin = { email: 'admin@feedback360.local', password: 'Admin123!' };

async function login(page: import('@playwright/test').Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
}

test.describe('Feedback360 smoke tests', () => {
  test('manager can log in and reach dashboard', async ({ page }) => {
    await login(page, manager.email, manager.password);
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { name: 'Team Dashboard' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'New 360 Review' })).toBeVisible();
  });

  test('admin is forbidden from results route', async ({ page }) => {
    await login(page, admin.email, admin.password);
    await expect(page).toHaveURL('/admin/users');
    await page.goto('/results');
    await expect(page).toHaveURL('/admin/users');
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  });

  test('manager can create a survey template', async ({ page }) => {
    const templateName = `E2E Template ${Date.now()}`;

    await login(page, manager.email, manager.password);
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
