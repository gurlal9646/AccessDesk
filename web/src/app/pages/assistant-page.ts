import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { AssistantAnswer } from '../core/models';

const SUGGESTIONS = [
  'Who can approve my access request?',
  'How do I get access to the production database?',
  'How long does Azure production access last?',
  'What happens to my access when I change teams?',
];

interface Exchange extends AssistantAnswer {
  question: string;
}

@Component({
  selector: 'app-assistant-page',
  imports: [ReactiveFormsModule],
  styles: `
    .chips {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0.5rem 0 1rem;
    }
    .answer {
      white-space: pre-wrap;
      margin: 0.25rem 0 1rem;
    }
    .sources {
      margin: 0.25rem 0 0;
      padding-left: 1.25rem;
    }
    .counter {
      text-align: right;
      margin: 0.25rem 0 0.75rem;
    }
  `,
  template: `
    <section class="card">
      <h2>Ask about the access policy</h2>
      <p class="muted">
        Answers come only from the company access policy, with the sections they were taken from. It is a
        demo with fictional policy text.
      </p>

      <div class="chips" aria-label="Example questions">
        @for (s of suggestions; track s) {
          <button type="button" class="secondary" [disabled]="asking()" (click)="useSuggestion(s)">{{ s }}</button>
        }
      </div>

      <form [formGroup]="form" (ngSubmit)="ask()" novalidate>
        <label for="question">Your question</label>
        <textarea id="question" formControlName="question" rows="3" maxlength="500"></textarea>
        <p class="muted small counter">{{ form.controls.question.value.length }}/500</p>
        @if (form.controls.question.touched && form.controls.question.invalid) {
          <p class="error">Ask a question of at least 5 characters.</p>
        }
        <button type="submit" [disabled]="asking()">{{ asking() ? 'Asking…' : 'Ask' }}</button>
      </form>
    </section>

    @if (errorText()) {
      <section class="card">
        <p class="error" role="alert">{{ errorText() }}</p>
      </section>
    }

    @if (exchange(); as e) {
      <section class="card" aria-live="polite">
        <h2>{{ e.question }}</h2>
        <p class="answer">{{ e.answer }}</p>
        @if (e.sources.length > 0) {
          <p class="muted small">Sources</p>
          <ol class="sources small">
            @for (s of e.sources; track s.section) {
              <li>{{ s.section }} <span class="muted">({{ s.document }})</span></li>
            }
          </ol>
        } @else {
          <p class="muted small">No policy section matched this question.</p>
        }
      </section>
    }
  `,
})
export class AssistantPage {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly suggestions = SUGGESTIONS;
  protected readonly asking = signal(false);
  protected readonly exchange = signal<Exchange | null>(null);
  protected readonly errorText = signal('');

  protected readonly form = this.fb.nonNullable.group({
    question: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
  });

  protected useSuggestion(text: string): void {
    this.form.controls.question.setValue(text);
    this.ask();
  }

  protected ask(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.asking()) {
      return;
    }
    const question = this.form.getRawValue().question.trim();
    this.asking.set(true);
    this.errorText.set('');
    this.exchange.set(null);
    this.api.askAssistant(question).subscribe({
      next: (result) => {
        this.asking.set(false);
        this.exchange.set({ question, ...result });
      },
      error: (err: HttpErrorResponse) => {
        this.asking.set(false);
        this.errorText.set(this.describe(err));
      },
    });
  }

  private describe(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Is it running on port 5080?';
    }
    const apiMessage = (err.error as { error?: string } | null)?.error;
    if (apiMessage) {
      return apiMessage;
    }
    if (err.status === 400) {
      return 'Questions must be between 5 and 500 characters.';
    }
    return `Something went wrong (${err.status}).`;
  }
}
