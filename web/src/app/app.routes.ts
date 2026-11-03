import { Routes } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import { adminGuard } from './core/admin.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./pages/requests-page').then((m) => m.RequestsPage),
  },
  {
    path: 'assistant',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./pages/assistant-page').then((m) => m.AssistantPage),
  },
  {
    path: 'audit',
    canActivate: [autoLoginPartialRoutesGuard, adminGuard],
    loadComponent: () => import('./pages/audit-page').then((m) => m.AuditPage),
  },
  { path: '**', redirectTo: '' },
];
