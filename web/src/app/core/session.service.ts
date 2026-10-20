import { Injectable, computed, inject, signal } from '@angular/core';
import { LoginResponse, OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable, tap } from 'rxjs';
import { realmRoles } from './jwt';

/** Holds who is signed in. Login itself is delegated to Keycloak (authorization code + PKCE). */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly oidc = inject(OidcSecurityService);

  readonly isAuthenticated = signal(false);
  readonly userName = signal('');
  readonly roles = signal<string[]>([]);

  readonly isAdmin = computed(() => this.roles().includes('admin'));
  readonly isManager = computed(() => this.isAdmin() || this.roles().includes('manager'));

  /** Called once at startup: finishes a login redirect if there is one, or restores the session. */
  init(): Observable<LoginResponse> {
    return this.oidc.checkAuth().pipe(tap((response) => this.apply(response)));
  }

  login(): void {
    this.oidc.authorize();
  }

  logout(): void {
    this.oidc.logoff().subscribe();
  }

  private apply(response: LoginResponse): void {
    this.isAuthenticated.set(response.isAuthenticated);
    const user = response.userData as { preferred_username?: string } | null;
    this.userName.set(user?.preferred_username ?? '');
    this.roles.set(response.isAuthenticated ? realmRoles(response.accessToken) : []);
  }
}
