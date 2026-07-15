import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  // Endpoints that handle errors themselves (no toast needed)
  const skipToast = ['/auth/me', '/auth/login', '/auth/register'];
  const isSilent = skipToast.some(u => req.url.includes(u));

  return next(req).pipe(
    catchError((err) => {
      if (!isSilent) {
        let msg = 'Something went wrong. Please try again.';

        if (err.status === 0 || err.statusText === 'Unknown Error') {
          msg = 'Cannot reach the server. Check your internet connection.';
        } else if (err.status === 401) {
          msg = 'Session expired. Please log in again.';
          setTimeout(() => router.navigate(['/login']), 1500);
        } else if (err.status === 403) {
          msg = 'You do not have permission to do that.';
        } else if (err.status >= 500) {
          msg = 'Server error. Please try again later.';
        } else if (err.error?.error) {
          msg = err.error.error;
        }

        document.dispatchEvent(new CustomEvent('app-toast', { detail: { message: msg, type: 'error' } }));
      }
      return throwError(() => err);
    })
  );
};
