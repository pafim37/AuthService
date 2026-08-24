import { HttpInterceptorFn } from '@angular/common/http';

export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const csrfToken = getCookieValue('auth_csrf_token');
  const headers =
    csrfToken && !['GET', 'HEAD', 'OPTIONS'].includes(request.method.toUpperCase())
      ? request.headers.set('X-CSRF-TOKEN', csrfToken)
      : request.headers;

  return next(
    request.clone({
      headers,
      withCredentials: true,
    }),
  );
};

function getCookieValue(name: string): string | null {
  const cookie = document.cookie
    .split('; ')
    .find((value) => value.startsWith(`${name}=`));

  return cookie ? decodeURIComponent(cookie.slice(name.length + 1)) : null;
}
