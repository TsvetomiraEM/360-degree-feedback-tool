import { test, expect, type Page, type Locator } from '@playwright/test';
import { credentials, login, logout, type TestCredential } from '../helpers/auth';
import { suffix, uniqueEmail } from '../helpers/unique';

async function selectMuiOption(field: Locator, optionName: string) {
  await field.click();
  await field.page().getByRole('option', { name: optionName, exact: true }).click();
}

async function fillUserDialog(
  page: Page,
  input: { name: string; email: string; password: string; managerName: string }
) {
  await page.getByRole('button', { name: 'Add User' }).click();

  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await dialog.getByLabel('Name').fill(input.name);
  await dialog.getByLabel('Email').fill(input.email);
  await selectMuiOption(dialog.getByLabel('Role'), 'Employee');
  await selectMuiOption(dialog.getByLabel('Manager'), input.managerName);
  await dialog.getByLabel('Password').fill(input.password);
  await dialog.getByRole('button', { name: 'Save' }).click();
  await expect(dialog).not.toBeVisible();
}

async function createTemplate(page: Page, templateName: string) {
  await page.goto('/templates');
  await expect(page.getByRole('heading', { name: 'Survey Templates' })).toBeVisible();

  await page.getByRole('button', { name: 'New Template' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await dialog.getByLabel('Name').fill(templateName);
  await dialog.getByLabel('Question').first().fill('Collaborates effectively with the team');
  await dialog.getByRole('button', { name: 'Save' }).click();

  await expect(dialog).not.toBeVisible();
  await expect(page.getByText(templateName)).toBeVisible();
}

async function launchSurvey(
  page: Page,
  input: { templateName: string; employeeName: string; peerName: string }
) {
  await page.goto('/surveys/new');
  await expect(page.getByRole('heading', { name: 'Create 360 Review' })).toBeVisible();

  await selectMuiOption(page.getByRole('combobox', { name: 'Template' }), input.templateName);
  await page.getByRole('button', { name: 'Next' }).click();

  await selectMuiOption(page.getByRole('combobox', { name: 'Employee' }), input.employeeName);
  await page.getByLabel('Due Date').fill('2030-12-31');
  await page.getByRole('button', { name: 'Next' }).click();

  await page.getByRole('checkbox', { name: input.peerName }).check();
  await page.getByRole('button', { name: 'Launch' }).click();

  await expect(page.getByText('360 Review launched successfully!')).toBeVisible();
  await expect(page.getByText('Peers, self, and manager have been assigned.')).toBeVisible();
  await page.getByRole('button', { name: 'Go to Dashboard' }).click();

  await expect(page.getByRole('heading', { name: 'Team Dashboard' })).toBeVisible();
  const surveyRow = page.locator('[role="row"]').filter({ hasText: input.employeeName }).first();
  await expect(surveyRow).toBeVisible();
  await expect(surveyRow).toContainText('Active');
}

async function expectAssignmentVisible(
  page: Page,
  credential: TestCredential,
  input: { surveyTitle: string; employeeName: string; role: string }
) {
  await login(page, credential);
  await page.goto('/assignments');
  await expect(page.getByRole('heading', { name: 'My Surveys' })).toBeVisible();
  const assignmentRow = page.locator('[role="row"]').filter({ hasText: input.surveyTitle }).first();
  await expect(assignmentRow).toBeVisible();
  await expect(assignmentRow).toContainText(input.employeeName);
  await expect(assignmentRow).toContainText(input.role);
  await expect(page.getByRole('button', { name: 'Respond' }).first()).toBeVisible();
}

test.describe.serial('Feedback360 happy path 360 journey', () => {
  test('admin creates user, manager launches 360, assignees can see it', async ({ page }) => {
    test.setTimeout(90_000);

    const runSuffix = suffix();
    const employeeName = `E2E Employee ${runSuffix}`;
    const employeeEmail = uniqueEmail('e2e-employee');
    const employeePassword = 'Employee123!';
    const peerName = `E2E Peer ${runSuffix}`;
    const peerEmail = uniqueEmail('e2e-peer');
    const peerPassword = 'Employee123!';
    const templateName = `E2E Template ${runSuffix}`;
    const surveyTitle = `${templateName} - 360 Review`;

    await login(page, credentials.admin);
    await expect(page).toHaveURL('/admin/users');

    await fillUserDialog(page, {
      name: employeeName,
      email: employeeEmail,
      password: employeePassword,
      managerName: 'Jane Manager',
    });

    await expect(page.getByText(employeeName)).toBeVisible();
    await expect(page.getByText(employeeEmail)).toBeVisible();

    await fillUserDialog(page, {
      name: peerName,
      email: peerEmail,
      password: peerPassword,
      managerName: 'Jane Manager',
    });

    await expect(page.getByText(peerName)).toBeVisible();
    await expect(page.getByText(peerEmail)).toBeVisible();

    await logout(page);
    await login(page, credentials.manager);

    await createTemplate(page, templateName);
    await launchSurvey(page, {
      templateName,
      employeeName,
      peerName,
    });

    await logout(page);
    await expectAssignmentVisible(page, {
      email: employeeEmail,
      password: employeePassword,
    }, {
      surveyTitle,
      employeeName,
      role: 'Self',
    });

    await logout(page);
    await expectAssignmentVisible(page, {
      email: peerEmail,
      password: peerPassword,
    }, {
      surveyTitle,
      employeeName,
      role: 'Peer',
    });
  });
});
