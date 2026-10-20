import { decodeJwtPayload, realmRoles } from './jwt';

function makeToken(payload: object): string {
  // Real JWTs are base64url of UTF-8 bytes, so encode the JSON as UTF-8 first.
  const encode = (value: object) => {
    const bytes = new TextEncoder().encode(JSON.stringify(value));
    const binary = Array.from(bytes, (b) => String.fromCharCode(b)).join('');
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  };
  return `${encode({ alg: 'none' })}.${encode(payload)}.signature`;
}

describe('jwt helpers', () => {
  it('decodes the payload of a token', () => {
    const token = makeToken({ preferred_username: 'mia', sub: 'u1' });

    expect(decodeJwtPayload(token)).toEqual({ preferred_username: 'mia', sub: 'u1' });
  });

  it('returns null for empty or malformed tokens', () => {
    expect(decodeJwtPayload(null)).toBeNull();
    expect(decodeJwtPayload('')).toBeNull();
    expect(decodeJwtPayload('not-a-token')).toBeNull();
    expect(decodeJwtPayload('a.%%%.c')).toBeNull();
  });

  it('reads Keycloak realm roles', () => {
    const token = makeToken({ realm_access: { roles: ['employee', 'manager'] } });

    expect(realmRoles(token)).toEqual(['employee', 'manager']);
  });

  it('ignores missing or odd role shapes', () => {
    expect(realmRoles(makeToken({}))).toEqual([]);
    expect(realmRoles(makeToken({ realm_access: { roles: 'manager' } }))).toEqual([]);
    expect(realmRoles(makeToken({ realm_access: { roles: ['ok', 5, null] } }))).toEqual(['ok']);
  });

  it('handles non-ASCII characters in claims', () => {
    const token = makeToken({ preferred_username: 'josé' });

    expect(decodeJwtPayload(token)?.['preferred_username']).toBe('josé');
  });
});
