import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './app-config';
import { AccessRequest, AuditEntry, CreateAccessRequest, RequestStatus } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  listRequests(status?: RequestStatus): Observable<AccessRequest[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<AccessRequest[]>(`${API_BASE_URL}/api/requests`, { params });
  }

  createRequest(body: CreateAccessRequest): Observable<AccessRequest> {
    return this.http.post<AccessRequest>(`${API_BASE_URL}/api/requests`, body);
  }

  approve(id: number, comment?: string): Observable<AccessRequest> {
    return this.http.post<AccessRequest>(`${API_BASE_URL}/api/requests/${id}/approve`, { comment: comment || null });
  }

  reject(id: number, comment?: string): Observable<AccessRequest> {
    return this.http.post<AccessRequest>(`${API_BASE_URL}/api/requests/${id}/reject`, { comment: comment || null });
  }

  audit(take = 100): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(`${API_BASE_URL}/api/audit`, { params: { take } });
  }
}
