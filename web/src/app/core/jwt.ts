/** Decodes the payload of a JWT (no signature check: the API validates tokens, the UI only reads claims). */
export function decodeJwtPayload(token: string | null | undefined): Record<string, unknown> | null {
  if (!token) {
    return null;
  }
  const parts = token.split('.');
  if (parts.length < 2) {
    return null;
  }
  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const json = decodeURIComponent(
      atob(padded)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/** Keycloak puts realm roles under realm_access.roles. Used only to show or hide UI; the API enforces access. */
export function realmRoles(token: string | null | undefined): string[] {
  const payload = decodeJwtPayload(token);
  const realmAccess = payload?.['realm_access'] as { roles?: unknown } | undefined;
  const roles = realmAccess?.roles;
  return Array.isArray(roles) ? roles.filter((r): r is string => typeof r === 'string') : [];
}
