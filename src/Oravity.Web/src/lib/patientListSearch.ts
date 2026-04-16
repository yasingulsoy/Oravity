import type { PatientListRequest } from '@/types/patient';

export type SearchMode = 'name' | 'phone' | 'tc' | 'empty';

export async function sha256hex(text: string): Promise<string> {
  const buf = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return Array.from(new Uint8Array(buf))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

/**
 * TC Kimlik No: tam olarak 11 hane, asla 0 ile başlamaz.
 * Türk GSM: 05XXXXXXXXX (11 hane, 0 ile başlar) veya 5XXXXXXXXX (10 hane).
 */
export function detectMode(raw: string): SearchMode {
  const t = raw.trim();
  if (!t) return 'empty';

  const digits = t.replace(/[\s\-]/g, '');

  if (/^[1-9]\d{10}$/.test(digits)) return 'tc';

  if (
    /^(\+90|0090)\d{10}$/.test(digits) ||
    /^05\d{9}$/.test(digits) ||
    /^5\d{9}$/.test(digits)
  ) return 'phone';

  return 'name';
}

/** /patients sayfası ve navbar araması ile aynı istek gövdesi (min 3 karakter). */
export async function buildPatientListRequest(
  raw: string,
  page: number,
  pageSize: number,
): Promise<PatientListRequest | null> {
  const t = raw.trim();
  if (t.length < 3) return null;

  const mode = detectMode(t);
  const next: PatientListRequest = { page, pageSize };

  if (mode === 'tc') {
    next.tcHash = await sha256hex(t);
  } else if (mode === 'phone') {
    next.phone = t.replace(/[\s\-]/g, '');
  } else {
    const parts = t.split(/\s+/);
    next.firstName = parts[0];
    if (parts.length > 1) next.lastName = parts.slice(1).join(' ');
  }

  return next;
}
