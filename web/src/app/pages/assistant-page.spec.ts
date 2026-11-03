import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../core/app-config';
import { AssistantPage } from './assistant-page';

describe('AssistantPage', () => {
  let fixture: ComponentFixture<AssistantPage>;
  let http: HttpTestingController;
  const askUrl = `${API_BASE_URL}/api/assistant/ask`;

  function root(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function type(question: string): void {
    const textarea = root().querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = question;
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    (root().querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AssistantPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(AssistantPage);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('shows the answer and its sources', () => {
    type('Who can approve an access request?');
    submit();

    const req = http.expectOne(askUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ question: 'Who can approve an access request?' });
    req.flush({
      answer: 'Managers and admins can approve requests [1].',
      sources: [{ document: 'access-policy', section: 'Who can approve access requests' }],
    });
    fixture.detectChanges();

    expect(root().textContent).toContain('Managers and admins can approve requests [1].');
    expect(root().textContent).toContain('Who can approve access requests');
    expect(root().textContent).toContain('access-policy');
  });

  it('does not call the API for a question that is too short', () => {
    type('hi');
    submit();

    http.expectNone(askUrl);
    expect(root().textContent).toContain('at least 5 characters');
  });

  it('shows the message from the API when the assistant is unavailable', () => {
    type('Who can approve an access request?');
    submit();

    http
      .expectOne(askUrl)
      .flush({ error: 'The assistant is not configured.' }, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(root().querySelector('[role="alert"]')?.textContent).toContain('The assistant is not configured.');
  });

  it('asks the question of a suggestion chip when it is clicked', () => {
    const chip = root().querySelector('.chips button') as HTMLButtonElement;
    const text = chip.textContent?.trim();

    chip.click();
    fixture.detectChanges();

    expect(http.expectOne(askUrl).request.body).toEqual({ question: text });
  });

  it('renders the answer as plain text, not as HTML', () => {
    type('Who can approve an access request?');
    submit();

    http.expectOne(askUrl).flush({ answer: '<img src=x onerror=alert(1)> done', sources: [] });
    fixture.detectChanges();

    expect(root().querySelector('.answer img')).toBeNull();
    expect(root().querySelector('.answer')?.textContent).toContain('<img src=x onerror=alert(1)> done');
  });
});
