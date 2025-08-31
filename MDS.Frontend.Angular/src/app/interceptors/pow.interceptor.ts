import { HttpInterceptorFn } from '@angular/common/http';
import { from } from 'rxjs';
import { switchMap } from 'rxjs/operators';

async function sha256Hex(s: string): Promise<string> {
  const enc = new TextEncoder().encode(s);
  const hash = await crypto.subtle.digest('SHA-256', enc);
  const b = new Uint8Array(hash);
  let out = '';
  for (let i = 0; i < b.length; i++) out += b[i].toString(16).padStart(2, '0');
  return out;
}

function leadingZeroBits(hex: string): number {
  let bits = 0;
  for (let i = 0; i < hex.length; i++) {
    const nibble = parseInt(hex[i], 16);
    if (nibble === 0) {
      bits += 4;
      continue;
    }
    for (let k = 3; k >= 0; k--) {
      if (((nibble >> k) & 1) === 0) bits++; else return bits;
    }
  }
  return bits;
}

async function mintPow(difficulty: number): Promise<string> {
  const ts = Math.floor(Date.now() / 1000).toString();
  for (;;) {
    const nonce = crypto.getRandomValues(new Uint8Array(12));
    let n = '';
    for (let i = 0; i < nonce.length; i++) n += nonce[i].toString(16).padStart(2, '0');
    const token = `v1:${ts}:${n}`;
    const h = await sha256Hex(token);
    if (leadingZeroBits(h) >= difficulty) return token;
  }
}

function needsPow(url: string, method: string): boolean {
  return method.toUpperCase() === 'GET' && url.includes('/api/news/top');
}

export const powInterceptor: HttpInterceptorFn = (req, next) => {
  if (!needsPow(req.url, req.method)) return next(req);
  const work = async () => {
    const token = await mintPow(20);
    return req.clone({ setHeaders: { 'X-PoW': token } });
  };
  return from(work()).pipe(switchMap(clone => next(clone)));
}
