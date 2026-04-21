import type { ReactNode } from 'react';

// ── Sembol tanımı ─────────────────────────────────────────────────────────────
// Her sembol 36×44 viewBox'a çizilir (mevcut ToothSvg ile aynı koordinat sistemi)
// Crown area: y 0–32  |  Root area: y 32–44

export interface DentalSymbol {
  code:     string;
  label:    string;
  category: string;
  color:    string;         // preview badge rengi
  overlay:  () => ReactNode; // SVG elementleri (36×44 viewBox içinde)
}

const g = (children: ReactNode, opacity = 1) => (
  <g opacity={opacity}>{children}</g>
);

export const DENTAL_SYMBOLS: DentalSymbol[] = [
  // ── Cerrahi ────────────────────────────────────────────────────────────────
  {
    code: 'extraction', label: 'Çekim', category: 'Cerrahi', color: '#ef4444',
    overlay: () => g(<>
      <line x1="8"  y1="6"  x2="28" y2="38" stroke="#ef4444" strokeWidth="3" strokeLinecap="round" />
      <line x1="28" y1="6"  x2="8"  y2="38" stroke="#ef4444" strokeWidth="3" strokeLinecap="round" />
    </>),
  },
  {
    code: 'implant', label: 'İmplant', category: 'Cerrahi', color: '#7c3aed',
    overlay: () => g(<>
      <line x1="18" y1="6"  x2="18" y2="38" stroke="#7c3aed" strokeWidth="2.5" strokeLinecap="round" />
      {[11, 17, 23, 29, 35].map(y => (
        <line key={y} x1="12" y1={y} x2="24" y2={y} stroke="#7c3aed" strokeWidth="1.3" />
      ))}
      <rect x="11" y="4" width="14" height="5" rx="2" fill="#7c3aed" opacity="0.9" />
    </>),
  },
  {
    code: 'sinus-lift', label: 'Sinüs Lifting', category: 'Cerrahi', color: '#0284c7',
    overlay: () => g(<>
      <path d="M4,10 Q10,2 18,6 Q26,2 32,10" fill="none" stroke="#0284c7" strokeWidth="2" strokeLinecap="round" />
      <path d="M4,16 Q10,8 18,12 Q26,8 32,16" fill="none" stroke="#0284c7" strokeWidth="1.5" strokeLinecap="round" opacity="0.6" />
    </>),
  },
  {
    code: 'graft', label: 'Kemik Grefti', category: 'Cerrahi', color: '#b45309',
    overlay: () => g(<>
      <rect x="8" y="28" width="20" height="12" rx="2" fill="none" stroke="#b45309" strokeWidth="1.8" strokeDasharray="3,2" />
      <text x="18" y="37" textAnchor="middle" fontSize="7" fill="#b45309" fontFamily="Arial" fontWeight="bold">G</text>
    </>),
  },
  {
    code: 'broken', label: 'Kırık Diş', category: 'Cerrahi', color: '#ea580c',
    overlay: () => g(<>
      <path d="M21,2 L17,14 L24,18 L14,42" fill="none" stroke="#ea580c" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
    </>),
  },
  {
    code: 'root', label: 'Kök Kalıntısı', category: 'Cerrahi', color: '#78716c',
    overlay: () => g(<>
      <line x1="8"  y1="14" x2="28" y2="14" stroke="#78716c" strokeWidth="2" strokeLinecap="round" />
      <line x1="13" y1="14" x2="11" y2="40" stroke="#78716c" strokeWidth="2.2" strokeLinecap="round" />
      <line x1="23" y1="14" x2="25" y2="40" stroke="#78716c" strokeWidth="2.2" strokeLinecap="round" />
    </>),
  },

  // ── Protetik ───────────────────────────────────────────────────────────────
  {
    code: 'crown', label: 'Kron', category: 'Protetik', color: '#92400e',
    overlay: () => g(<>
      <path d="M6,38 L6,16 L12,24 L18,8 L24,24 L30,16 L30,38 Z"
        fill="none" stroke="#92400e" strokeWidth="2" strokeLinejoin="round" />
    </>),
  },
  {
    code: 'bridge', label: 'Köprü', category: 'Protetik', color: '#0e7490',
    overlay: () => g(<>
      <rect x="0" y="1" width="36" height="6" fill="#0e7490" opacity="0.7" rx="1" />
      <line x1="0" y1="7" x2="36" y2="7" stroke="#0e7490" strokeWidth="1" />
    </>),
  },
  {
    code: 'veneer', label: 'Veneer / Laminate', category: 'Protetik', color: '#db2777',
    overlay: () => g(<>
      <polygon points="0,0 36,0 28,12 8,12" fill="#fbcfe8" stroke="#db2777" strokeWidth="1.8" opacity="0.85" />
    </>),
  },
  {
    code: 'inlay', label: 'İnley / Onley', category: 'Protetik', color: '#059669',
    overlay: () => g(<>
      <rect x="10" y="14" width="16" height="16" rx="2" fill="#a7f3d0" stroke="#059669" strokeWidth="1.8" />
    </>),
  },
  {
    code: 'protez', label: 'Hareketli Protez', category: 'Protetik', color: '#6d28d9',
    overlay: () => g(<>
      <rect x="3" y="4" width="30" height="36" rx="4" fill="none" stroke="#6d28d9" strokeWidth="1.8" strokeDasharray="4,3" />
    </>),
  },

  // ── Endodonti ──────────────────────────────────────────────────────────────
  {
    code: 'root-canal', label: 'Kanal Tedavisi', category: 'Endodonti', color: '#c2410c',
    overlay: () => g(<>
      <line x1="15" y1="8"  x2="13" y2="40" stroke="#c2410c" strokeWidth="2.2" strokeLinecap="round" />
      <line x1="21" y1="8"  x2="23" y2="40" stroke="#c2410c" strokeWidth="2.2" strokeLinecap="round" />
    </>),
  },
  {
    code: 'apikal', label: 'Apikal Apse', category: 'Endodonti', color: '#be123c',
    overlay: () => g(<>
      <circle cx="18" cy="20" r="9" fill="#fda4af" stroke="#e11d48" strokeWidth="1.5" />
      <line x1="18" y1="15" x2="18" y2="22" stroke="#881337" strokeWidth="2.5" strokeLinecap="round" />
      <circle cx="18" cy="25.5" r="1.5" fill="#881337" />
    </>),
  },

  // ── Konservatif / Dolgu ────────────────────────────────────────────────────
  {
    code: 'filling-o', label: 'Dolgu — Oklüzal', category: 'Dolgu', color: '#1d4ed8',
    overlay: () => g(<>
      <rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-m', label: 'Dolgu — Mesial', category: 'Dolgu', color: '#1d4ed8',
    overlay: () => g(<>
      <polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-d', label: 'Dolgu — Distal', category: 'Dolgu', color: '#1d4ed8',
    overlay: () => g(<>
      <polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-v', label: 'Dolgu — Vestibül', category: 'Dolgu', color: '#1d4ed8',
    overlay: () => g(<>
      <polygon points="0,0 36,0 28,12 8,12" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-l', label: 'Dolgu — Lingual', category: 'Dolgu', color: '#1d4ed8',
    overlay: () => g(<>
      <polygon points="8,32 28,32 36,44 0,44" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-mo', label: 'Dolgu — MO', category: 'Dolgu', color: '#2563eb',
    overlay: () => g(<>
      <polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
      <rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-do', label: 'Dolgu — DO', category: 'Dolgu', color: '#2563eb',
    overlay: () => g(<>
      <rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
      <polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'filling-mod', label: 'Dolgu — MOD', category: 'Dolgu', color: '#2563eb',
    overlay: () => g(<>
      <polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
      <rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
      <polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'decay', label: 'Çürük (Karies)', category: 'Dolgu', color: '#92400e',
    overlay: () => g(<>
      <circle cx="14" cy="18" r="4" fill="#a16207" opacity="0.7" />
      <circle cx="22" cy="23" r="3" fill="#a16207" opacity="0.6" />
      <circle cx="17" cy="28" r="2.5" fill="#a16207" opacity="0.5" />
    </>),
  },

  // ── Periodontoloji ─────────────────────────────────────────────────────────
  {
    code: 'perio', label: 'Periodontitis', category: 'Perio', color: '#16a34a',
    overlay: () => g(<>
      <path d="M4,14 Q9,8 14,14 Q19,8 24,14 Q29,8 34,14" fill="none" stroke="#16a34a" strokeWidth="2" strokeLinecap="round" />
      <line x1="6"  y1="14" x2="6"  y2="32" stroke="#16a34a" strokeWidth="1.2" strokeDasharray="2,2" />
      <line x1="30" y1="14" x2="30" y2="32" stroke="#16a34a" strokeWidth="1.2" strokeDasharray="2,2" />
    </>),
  },
  {
    code: 'perio-adv', label: 'İleri Periodontitis', category: 'Perio', color: '#15803d',
    overlay: () => g(<>
      <path d="M4,20 Q9,12 14,20 Q19,12 24,20 Q29,12 34,20" fill="none" stroke="#15803d" strokeWidth="2.2" strokeLinecap="round" />
      <line x1="6"  y1="20" x2="6"  y2="40" stroke="#15803d" strokeWidth="1.5" strokeDasharray="2,2" />
      <line x1="30" y1="20" x2="30" y2="40" stroke="#15803d" strokeWidth="1.5" strokeDasharray="2,2" />
    </>),
  },

  // ── Ortodonti ─────────────────────────────────────────────────────────────
  {
    code: 'braket', label: 'Ortodonti Braket', category: 'Ortodonti', color: '#0369a1',
    overlay: () => g(<>
      <rect x="11" y="16" width="14" height="12" rx="1" fill="#bae6fd" stroke="#0369a1" strokeWidth="1.8" />
      <line x1="4"  y1="22" x2="11" y2="22" stroke="#0369a1" strokeWidth="1.5" />
      <line x1="25" y1="22" x2="32" y2="22" stroke="#0369a1" strokeWidth="1.5" />
    </>),
  },
  {
    code: 'bant', label: 'Ortodonti Bandı', category: 'Ortodonti', color: '#075985',
    overlay: () => g(<>
      <rect x="2" y="12" width="32" height="20" rx="2" fill="none" stroke="#075985" strokeWidth="2" />
      <line x1="2"  y1="18" x2="34" y2="18" stroke="#075985" strokeWidth="1" />
      <line x1="2"  y1="26" x2="34" y2="26" stroke="#075985" strokeWidth="1" />
    </>),
  },

  // ── Genel ─────────────────────────────────────────────────────────────────
  {
    code: 'missing', label: 'Eksik Diş', category: 'Genel', color: '#9ca3af',
    overlay: () => g(<>
      <rect x="2" y="2" width="32" height="40" rx="4" fill="#f9fafb" stroke="#9ca3af" strokeWidth="1.8" strokeDasharray="5,3" />
      <circle cx="18" cy="22" r="3.5" fill="#d1d5db" />
    </>),
  },
  {
    code: 'impacted', label: 'Gömülü Diş', category: 'Genel', color: '#15803d',
    overlay: () => g(<>
      <path d="M18,6 L18,30 M10,22 L18,32 L26,22"
        fill="none" stroke="#15803d" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
    </>),
  },
];

export const SYMBOL_CATEGORIES = [...new Set(DENTAL_SYMBOLS.map(s => s.category))];

export function getSymbol(code: string | null | undefined): DentalSymbol | undefined {
  if (!code) return undefined;
  return DENTAL_SYMBOLS.find(s => s.code === code);
}
