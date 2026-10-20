import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { AccessRequest, RequestStatus } from '../core/models';
import { SessionService } from '../core/session.service';

@Component({
  selector: 'app-requests-page',
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <section class="card">
      <h2>Request access</h2>
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label for="system">System</label>
        <input id="system" formControlName="system" placeholder="e.g. GitHub, Azure, Jira" />
        @if (form.controls.system.touched && form.controls.system.invalid) {
          <p class="error">Enter the system name (at least 2 characters).</p>
        }

        <label for="justification">Why do you need it?</label>
        <textarea id="justification" formControlName="justification" rows="3"></textarea>
        @if (form.controls.justification.touched && form.controls.justification.invalid) {
          <p class="error">Explain the reason in at least 10 characters.</p>
        }

        <button type="submit" [disabled]="submitting()">Submit request</button>
      </form>
    </section>

    <section class="card">
      <div class="row">
        <h2>{{ session.isManager() ? 'All requests' : 'My requests' }}</h2>
        <label class="inline">
          Status
          <select (change)="onFilter($any($event.target).value)">
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
          </select>
        </label>
      </div>

      @if (message()) {
        <p class="notice" role="status">{{ message() }}</p>
      }
      @if (errorText()) {
        <p class="error" role="alert">{{ errorText() }}</p>
      }

      @if (loading()) {
        <p>Loading…</p>
      } @else if (requests().length === 0) {
        <p class="muted">No requests yet.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>System</th>
              <th>Requester</th>
              <th>Reason</th>
              <th>Status</th>
              <th>Created</th>
              @if (session.isManager()) {
                <th>Decision</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (r of requests(); track r.id) {
              <tr>
                <td>{{ r.system }}</td>
                <td>{{ r.requesterName }}</td>
                <td>{{ r.justification }}</td>
                <td>
                  <span class="badge" [class]="'badge ' + r.status.toLowerCase()">{{ r.status }}</span>
                  @if (r.decidedBy) {
                    <div class="muted small">by {{ r.decidedBy }}</div>
                  }
                  @if (r.decisionComment) {
                    <div class="muted small">“{{ r.decisionComment }}”</div>
                  }
                </td>
                <td>{{ r.createdAtUtc | date: 'medium' }}</td>
                @if (session.isManager()) {
                  <td>
                    @if (r.status === 'Pending') {
                      <input
                        class="comment"
                        #comment
                        placeholder="Comment (optional)"
                        [attr.aria-label]="'Comment for request ' + r.id"
                      />
                      <button type="button" (click)="decide(r, true, comment.value)">Approve</button>
                      <button type="button" class="secondary" (click)="decide(r, false, comment.value)">Reject</button>
                    }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class RequestsPage implements OnInit {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  protected readonly session = inject(SessionService);

  protected readonly requests = signal<AccessRequest[]>([]);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly message = signal('');
  protected readonly errorText = signal('');
  private filter: RequestStatus | undefined;

  protected readonly form = this.fb.nonNullable.group({
    system: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    justification: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
  });

  ngOnInit(): void {
    this.load();
  }

  protected onFilter(value: string): void {
    this.filter = (value || undefined) as RequestStatus | undefined;
    this.load();
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    this.submitting.set(true);
    this.clearMessages();
    this.api.createRequest(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.form.reset();
        this.message.set('Request submitted.');
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorText.set(this.describe(err));
      },
    });
  }

  protected decide(request: AccessRequest, approve: boolean, comment: string): void {
    this.clearMessages();
    const call = approve ? this.api.approve(request.id, comment) : this.api.reject(request.id, comment);
    call.subscribe({
      next: () => {
        this.message.set(`Request for ${request.system} ${approve ? 'approved' : 'rejected'}.`);
        this.load();
      },
      error: (err: HttpErrorResponse) => this.errorText.set(this.describe(err)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.listRequests(this.filter).subscribe({
      next: (items) => {
        this.requests.set(items);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorText.set(this.describe(err));
      },
    });
  }

  private clearMessages(): void {
    this.message.set('');
    this.errorText.set('');
  }

  private describe(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Is it running on port 5080?';
    }
    const apiMessage = (err.error as { error?: string } | null)?.error;
    if (apiMessage) {
      return apiMessage;
    }
    if (err.status === 403) {
      return 'You do not have permission to do that.';
    }
    if (err.status === 400) {
      return 'The request was not valid. Check the fields and try again.';
    }
    return `Something went wrong (${err.status}).`;
  }
}
