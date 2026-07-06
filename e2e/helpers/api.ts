import type { APIRequestContext } from '@playwright/test';
import type { TestCredential } from './auth';

type AuthResponse = { accessToken: string };
type Survey = { id: string; title: string };
type Template = { id: string; name: string };
type User = { id: string; email: string };

export function apiBaseUrl() {
  return process.env.E2E_API_URL ?? 'http://localhost:5000';
}

export function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

export async function apiLogin(request: APIRequestContext, credential: TestCredential) {
  const response = await request.post(`${apiBaseUrl()}/api/v1/auth/login`, {
    data: { email: credential.email, password: credential.password },
  });
  if (!response.ok()) {
    throw new Error(`API login failed for ${credential.email}: ${response.status()}`);
  }
  const body = (await response.json()) as AuthResponse;
  return body.accessToken;
}

export async function findSurveyIdByTitle(request: APIRequestContext, token: string, title: string) {
  const response = await request.get(`${apiBaseUrl()}/api/v1/surveys`, {
    headers: authHeaders(token),
  });
  if (!response.ok()) return undefined;
  const surveys = (await response.json()) as Survey[];
  return surveys.find((s) => s.title === title)?.id;
}

export async function findTemplateIdByName(request: APIRequestContext, token: string, name: string) {
  const response = await request.get(`${apiBaseUrl()}/api/v1/templates`, {
    headers: authHeaders(token),
  });
  if (!response.ok()) return undefined;
  const templates = (await response.json()) as Template[];
  return templates.find((t) => t.name === name)?.id;
}

export async function findUserIdByEmail(request: APIRequestContext, token: string, email: string) {
  const response = await request.get(`${apiBaseUrl()}/api/v1/admin/users`, {
    headers: authHeaders(token),
  });
  if (!response.ok()) return undefined;
  const users = (await response.json()) as User[];
  return users.find((u) => u.email.toLowerCase() === email.toLowerCase())?.id;
}

export async function deleteSurvey(request: APIRequestContext, token: string, id: string) {
  const response = await request.delete(`${apiBaseUrl()}/api/v1/surveys/${id}`, {
    headers: authHeaders(token),
  });
  if (response.status() === 404) return;
  if (!response.ok()) {
    throw new Error(`Failed to delete survey ${id}: ${response.status()}`);
  }
}

export async function deleteTemplate(request: APIRequestContext, token: string, id: string) {
  const response = await request.delete(`${apiBaseUrl()}/api/v1/templates/${id}`, {
    headers: authHeaders(token),
  });
  if (response.status() === 404) return;
  if (!response.ok()) {
    throw new Error(`Failed to delete template ${id}: ${response.status()}`);
  }
}

export async function deleteUser(request: APIRequestContext, token: string, id: string) {
  const response = await request.delete(`${apiBaseUrl()}/api/v1/admin/users/${id}`, {
    headers: authHeaders(token),
  });
  if (response.status() === 404) return;
  if (!response.ok()) {
    throw new Error(`Failed to delete user ${id}: ${response.status()}`);
  }
}
