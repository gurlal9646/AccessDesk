import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';

/** UI convenience only. The API still enforces the admin policy on /api/audit. */
export const adminGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  return session.isAdmin() ? true : inject(Router).createUrlTree(['/']);
};
