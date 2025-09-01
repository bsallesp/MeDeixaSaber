import { HttpInterceptorFn } from '@angular/common/http';
import { from } from 'rxjs';
import { switchMap } from 'rxjs/operators';

function toHex(bytes: ArrayBuffer): string {
  const b = new Uint8Array(bytes);
  let s = '';
  for (let i = 0; i < b.length; i++) s += b[i].toString(16).padStart(2, '0');
  return s.toUpperCase();
}

const API_KEY = 'demo-key';
const SECRET = 'demo-secret';

async function sha256(data: Uint8Array): Promise<string> {
  const hash = await crypto.subtle.digest('SHA-256', data.buffer as ArrayBuffer);
  return toHex(hash);
}

async function hmacSha256(key: Uint8Array, msg: string): Promise<string> {
  const k = await crypto.subtle.importKey('raw', key.buffer as ArrayBuffer, { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']);
  const sig = await crypto.subtle.sign('HMAC', k, new TextEncoder().encode(msg).buffer as ArrayBuffer);
  return toHex(sig);
}

function isClassifieds(url: string): boolean {
  return url.includes('/api/classifieds');
}

export const hmacInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isClassifieds(req.url)) return next(req);
  const work = async () => {
    const enc = new TextEncoder();
    const method = req.method.toUpperCase();
    const url = new URL(req.url, location.origin);
    const path = url.pathname;
    const query = url.search || '';
    const ts = Math.floor(Date.now() / 1000).toString();
    const nonce = crypto.getRandomValues(new Uint8Array(12));
    let nonceHex = '';
    for (let i = 0; i < nonce.length; i++) nonceHex += nonce[i].toString(16).padStart(2, '0').toUpperCase();
    let bodyBytes = new Uint8Array(0);
    if (req.body instanceof ArrayBuffer) bodyBytes = new Uint8Array(req.body);
    else if (typeof req.body === 'string') bodyBytes = enc.encode(req.body);
    else if (req.body) bodyBytes = enc.encode(JSON.stringify(req.body));
    const bodyHash = await sha256(bodyBytes);
    const canonical = `${method}\n${path}\n${query}\n${ts}\n${nonceHex}\n${bodyHash}`;
    const signature = await hmacSha256(enc.encode(SECRET), canonical);
    return req.clone({
      setHeaders: {
        'X-Api-Key': API_KEY,
        'X-Timestamp': ts,
        'X-Nonce': nonceHex,
        'X-Signature': signature
      }
    });
  };
  return from(work()).pipe(switchMap(clone => next(clone)));
}
