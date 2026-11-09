// Run only against a disposable development database.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';

const base = process.env.TEST_API_URL || 'http://api:5116/api';
async function request(path, { method = 'GET', body, cookie, grants, csrf = true } = {}) {
  const headers = { ...(csrf ? { 'X-MCN-Request': '1' } : {}),
    ...(body ? { 'Content-Type': 'application/json' } : {}),
    ...(cookie ? { Cookie: cookie } : {}), ...(grants ? { 'X-Folder-Grants': grants } : {}) };
  const res = await fetch(base + path, { method, headers, body: body ? JSON.stringify(body) : undefined });
  const text = await res.text();
  return { status: res.status, headers: res.headers, body: text ? JSON.parse(text) : null };
}
const post = (path, body, cookie, grants) => request(path, { method: 'POST', body, cookie, grants });
const register = async () => {
  const email = `${randomUUID()}@example.test`;
  const result = await post('/auth/register', { email, password: 'Testing123!' });
  assert.equal(result.status, 200);
  const setCookie = result.headers.get('set-cookie');
  assert.match(setCookie, /httponly/i); assert.match(setCookie, /samesite=strict/i);
  assert.ok(!result.body.token, 'session token must not appear in JSON');
  return { cookie: setCookie.split(';')[0], email };
};

const first = await register();
const owner = first.cookie;
const other = (await register()).cookie;
assert.equal((await request('/folders', { method: 'POST', cookie: owner, body: { name: 'blocked', password: 'abcd' }, csrf: false })).status, 403);
const folder = await post('/folders', { name: 'Protected', password: 'folder-password' }, owner);
assert.equal(folder.status, 201);
const folderId = folder.body.id;
assert.equal((await post(`/folders/${folderId}/unlock`, { password: 'incorrect' }, owner)).status, 401);
assert.equal((await post('/notes', { title: 'Blocked', content: 'x', password: 'pass', folderId }, owner)).status, 403);
const grant = await post(`/folders/${folderId}/unlock`, { password: 'folder-password' }, owner);
assert.equal(grant.status, 200);
const note = await post('/notes', { title: 'Secret', content: 'test content', password: 'note-password', folderId }, owner, grant.body.grant);
assert.equal(note.status, 201);
assert.equal((await request('/notes', { cookie: owner })).body.some(n => n.id === note.body.id), false);
assert.equal((await request('/notes', { cookie: owner, grants: grant.body.grant })).body.some(n => n.id === note.body.id), true);
assert.equal((await post(`/notes/${note.body.id}/unlock`, { password: 'note-password' }, owner)).status, 403);
assert.equal((await post(`/notes/${note.body.id}/unlock`, { password: 'note-password' }, owner, grant.body.grant)).body.content, 'test content');
assert.equal((await post(`/notes/${note.body.id}/unlock`, { password: 'note-password' }, other, grant.body.grant)).status, 403);
const second = await post('/auth/login', { email: first.email, password: 'Testing123!' });
assert.equal(second.status, 200);
const secondCookie = second.headers.get('set-cookie').split(';')[0];
assert.equal((await post(`/notes/${note.body.id}/unlock`, { password: 'note-password' }, secondCookie, grant.body.grant)).status, 403, 'grant must not work in another session');
const login = await post('/auth/login', { email: 'not-found@example.test', password: 'Testing123!' });
assert.equal(login.status, 401);
assert.equal((await post('/auth/google', { credential: 'invalid-token' })).status, 400);
assert.equal((await post('/auth/logout', {}, owner)).status, 204);
assert.equal((await request('/auth/me', { cookie: owner })).status, 401, 'revoked cookie must fail');
console.log('Security integration checks passed');
