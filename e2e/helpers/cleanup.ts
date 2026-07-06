import type { APIRequestContext } from '@playwright/test';
import { credentials } from './auth';
import {
  apiLogin,
  deleteSurvey,
  deleteTemplate,
  deleteUser,
  findSurveyIdByTitle,
  findTemplateIdByName,
  findUserIdByEmail,
} from './api';

export type E2eCleanupTargets = {
  surveyTitle?: string;
  templateName?: string;
  userEmails?: string[];
};

export async function cleanupE2eEntities(request: APIRequestContext, targets: E2eCleanupTargets) {
  try {
    if (targets.surveyTitle || targets.templateName) {
      const managerToken = await apiLogin(request, credentials.manager);

      if (targets.surveyTitle) {
        const surveyId = await findSurveyIdByTitle(request, managerToken, targets.surveyTitle);
        if (surveyId) await deleteSurvey(request, managerToken, surveyId);
      }

      if (targets.templateName) {
        const templateId = await findTemplateIdByName(request, managerToken, targets.templateName);
        if (templateId) await deleteTemplate(request, managerToken, templateId);
      }
    }

    if (targets.userEmails?.length) {
      const adminToken = await apiLogin(request, credentials.admin);

      for (const email of targets.userEmails) {
        const userId = await findUserIdByEmail(request, adminToken, email);
        if (userId) await deleteUser(request, adminToken, userId);
      }
    }
  } catch (error) {
    console.warn('E2E cleanup failed:', error);
  }
}
