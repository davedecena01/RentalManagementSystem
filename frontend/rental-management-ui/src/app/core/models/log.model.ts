export interface AppLog {
  id: string;
  action: string;
  entityType: string;
  entityId?: string;
  description: string;
  createdAt: string;
}

export interface PagedLogsResult {
  items: AppLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}
