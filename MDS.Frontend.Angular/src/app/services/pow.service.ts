import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PowService {

  private async sha256Hex(s: string): Promise<string> {
    const enc = new TextEncoder().encode(s);
    const hash = await crypto.subtle.digest('SHA-256', enc);
    const b = new Uint8Array(hash);
    let out = '';
    for (let i = 0; i < b.length; i++) out += b[i].toString(16).padStart(2, '0');
    return out;
  }

  private leadingZeroBits(hex: string): number {
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

  public async mintPow(difficulty: number): Promise<string> {
    const ts = Math.floor(Date.now() / 1000).toString();
    for (; ;) {
      const nonce = crypto.getRandomValues(new Uint8Array(12));
      let n = '';
      for (let i = 0; i < nonce.length; i++) n += nonce[i].toString(16).padStart(2, '0');
      const token = `v1:${ts}:${n}`;
      const h = await this.sha256Hex(token);
      if (this.leadingZeroBits(h) >= difficulty) return token;
    }
  }
}
