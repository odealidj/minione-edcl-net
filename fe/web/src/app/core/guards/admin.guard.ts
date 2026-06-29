import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.currentUserValue;

  if (user && user.role === 'ADMIN') {
    return true;
  }

  // Not admin, redirect to root or forbidden
  router.navigate(['/']);
  return false;
};
