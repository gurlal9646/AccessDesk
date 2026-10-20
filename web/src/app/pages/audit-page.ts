import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { AuditEntry } from '../core/models';

@Component({
  selector: 'app-audit-page',
  imports: [DatePipe],
  template: `
    <section class="card">
      <h2>Audit log</h2>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
      @if (loading()) {
        <p>Loading…</p>
      } @else if (entries().length === 0) {
        <p class="muted">No audit entries yet.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>Who</th>
              <th>Action</th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            @for (e of entries(); track e.id) {
              <tr>
                <td>{{ e.timestampUtc | date: 'medium' }}</td>
                <td>{{ e.actor }}</td>
                <td>
                  <code>{{ e.action }}</code>
                </td>
                <td>{{ e.details }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class AuditPage implements OnInit {
  private readonly api = inject(ApiService);

  protected readonly entries = signal<AuditEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.api.audit().subscribe({
      next: (items) => {
        this.entries.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load the audit log.');
      },
    });
  }
}
