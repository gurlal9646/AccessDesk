import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { API_BASE_URL, AUTHORITY, CLIENT_ID } from './core/app-config';
import { SessionService } from './core/session.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // The interceptor adds the access token only to calls to the API (secureRoutes below).
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideAuth({
      config: {
        authority: AUTHORITY,
        redirectUrl: window.location.origin,
        postLogoutRedirectUri: window.location.origin,
        clientId: CLIENT_ID,
        scope: 'openid profile',
        responseType: 'code', // authorization code flow; the library adds PKCE
        silentRenew: true,
        useRefreshToken: true,
        renewTimeBeforeTokenExpiresInSeconds: 30,
        secureRoutes: [API_BASE_URL],
      },
    }),
    // Finish a login redirect (or restore the session) before the first screen renders.
    provideAppInitializer(() => firstValueFrom(inject(SessionService).init())),
  ],
};
