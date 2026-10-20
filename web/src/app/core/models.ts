export type RequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface AccessRequest {
  id: number;
  requesterName: string;
  system: string;
  justification: string;
  status: RequestStatus;
  createdAtUtc: string;
  decidedBy: string | null;
  decidedAtUtc: string | null;
  decisionComment: string | null;
}

export interface AuditEntry {
  id: number;
  timestampUtc: string;
  actor: string;
  action: string;
  details: string;
}

export interface CreateAccessRequest {
  system: string;
  justification: string;
}
