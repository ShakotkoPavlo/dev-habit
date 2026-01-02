import { HateoasResponse } from '../../types/api';

export enum EntrySource {
  Manual = 0,
  Automation = 1,
}

export interface HabitReference {
  id: string;
  name: string;
}

export interface Entry extends HateoasResponse {
  id: string;
  habit: HabitReference;
  value: number;
  notes: string | null;
  source: EntrySource;
  externalId: string | null;
  isArchived: boolean;
  date: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface CreateEntry {
  habitId: string;
  value: number;
  notes?: string;
  date: string;
}

export interface CreateBatchEntries {
  entries: CreateEntry[];
}

export interface UpdateEntry {
  value: number;
  notes?: string;
  date: string;
}

export interface EntriesResponse extends HateoasResponse {
  items: Entry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
