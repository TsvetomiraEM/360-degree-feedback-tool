import { expect, type Page } from '@playwright/test';

export type TestCredential = {
  email: string;
  password: string;
};

export const credentials = {
  admin: { email: 'admin@feedback360.local', password: 'Admin123!' },
  manager: { email: 'manager@feedback360.local', password: 'Manager123!' },
  bob: { email: 'bob@feedback360.local', password: 'Employee123!' },
} satisfies Record<string, TestCredential>;

export async function login(page: Page, credential: TestCredential) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(credential.email);
  await page.getByLabel('Password').fill(credential.password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
}

export async function logout(page: Page) {
  await page.locator('header button').click();
  await page.getByRole('menuitem', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/\/login$/);
}
