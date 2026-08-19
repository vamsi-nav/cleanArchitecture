import { Inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../web-api-client';

export interface ArchivedTodoItem {
  id: number;
  listId: number;
  title: string | null;
  note: string | null;
  archivedAt: string;
}

export interface TodoArchivePurgeState {
  todoItemId: number;
  approved: boolean;
}

/**
 * Hand-written client for the todo archive module. Every call below targets a route
 * template declared by TodoArchiveEndpoints or TodoOperations on the server; route
 * parameters are interpolated into the same position the server template declares them.
 */
@Injectable({ providedIn: 'root' })
export class TodoArchiveService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  /** GET /api/TodoArchive */
  getArchivedItems(): Observable<ArchivedTodoItem[]> {
    return this.http.get<ArchivedTodoItem[]>(this.baseUrl + '/api/TodoArchive');
  }

  /** GET /api/TodoArchive/approval */
  getPurgeApprovals(): Observable<TodoArchivePurgeState[]> {
    return this.http.get<TodoArchivePurgeState[]>(this.baseUrl + '/api/TodoArchive/approval');
  }

  /** POST /api/TodoArchive/requests */
  requestPurge(todoItemId: number): Observable<void> {
    return this.http.post<void>(this.baseUrl + '/api/TodoArchive/requests', { todoItemId });
  }

  /** POST /api/TodoArchive/approval */
  approvePurge(todoItemId: number, approved: boolean): Observable<void> {
    return this.http.post<void>(this.baseUrl + '/api/TodoArchive/approval', { todoItemId, approved });
  }

  /** PUT /api/TodoArchive/{id}/restore */
  restoreItem(id: number): Observable<void> {
    return this.http.put<void>(this.baseUrl + '/api/TodoArchive/' + id + '/restore', {});
  }

  /** DELETE /api/TodoArchive/{id} — 409 until the purge is approved. */
  purgeItem(id: number): Observable<void> {
    return this.http.delete<void>(this.baseUrl + '/api/TodoArchive/' + id);
  }

  /** GET /api/TodoOperations/backlog */
  getBacklog(): Observable<string> {
    return this.http.get(this.baseUrl + '/api/TodoOperations/backlog', { responseType: 'text' });
  }

  /** POST /api/TodoOperations/reindex */
  reindex(): Observable<string> {
    return this.http.post(this.baseUrl + '/api/TodoOperations/reindex', {}, { responseType: 'text' });
  }
}
