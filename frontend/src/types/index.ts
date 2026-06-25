export type UserRole = 'Admin' | 'Manager' | 'Employee';
export type QuestionType = 'Rating' | 'OpenText';
export type ReviewerType = 'Self' | 'Peer' | 'Manager';
export type SurveyStatus = 'Draft' | 'Active' | 'Closed';
export type AssignmentStatus = 'Pending' | 'Completed';
export type AuditAction = 'UserCreated' | 'UserUpdated' | 'UserDeleted' | 'UserActivated' | 'UserDeactivated';

export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  managerId?: string;
  managerName?: string;
  isActive: boolean;
  authProvider: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface QuestionCategory {
  id: string;
  name: string;
  createdByName: string;
  createdAt: string;
}

export interface QuestionInput {
  type: QuestionType;
  text: string;
  order: number;
  categoryId: string;
  categoryName?: string;
}

export interface Template {
  id: string;
  name: string;
  description?: string;
  createdById: string;
  createdByName: string;
  createdAt: string;
  questions: QuestionInput[];
  isOwner: boolean;
  isShared: boolean;
}

export interface Survey {
  id: string;
  title: string;
  subjectEmployeeId: string;
  subjectEmployeeName: string;
  createdById: string;
  status: SurveyStatus;
  dueDate?: string;
  resultsPublished: boolean;
  createdAt: string;
  assignmentCount: number;
  completedCount: number;
}

export interface Assignment {
  id: string;
  surveyId: string;
  surveyTitle: string;
  subjectEmployeeName: string;
  reviewerType: ReviewerType;
  status: AssignmentStatus;
  dueDate?: string;
  completedAt?: string;
}

export interface QuestionForResponse {
  id: string;
  type: QuestionType;
  text: string;
  order: number;
  categoryId: string;
  categoryName: string;
}

export interface ResponseInput {
  questionId: string;
  rating?: number;
  comment?: string;
  openText?: string;
}

export interface AssignmentDetail extends Assignment {
  questions: QuestionForResponse[];
  existingResponses?: ResponseInput[];
}

export interface ResultsSeries {
  name: string;
  data: (number | null)[];
}

export interface Results {
  surveyId: string;
  title: string;
  subjectEmployeeName: string;
  resultsPublished: boolean;
  labels: string[];
  series: ResultsSeries[];
  commentGroups: { reviewerType: string; questionText: string; comments: string[] }[];
  openTextGroups: { reviewerType: string; questionText: string; responses: string[] }[];
}

export interface AuditLog {
  id: string;
  actorUserId: string;
  actorName: string;
  action: AuditAction;
  targetUserId?: string;
  targetUserName?: string;
  metadata: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
