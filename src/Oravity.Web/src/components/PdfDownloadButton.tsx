import { useState, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { FileDown, ChevronDown } from 'lucide-react';
import { toast } from 'sonner';
import { treatmentPlansApi } from '@/api/treatments';

const CURRENCIES = [
  { code: null,  label: 'Orijinal para birimi' },
  { code: 'TRY', label: 'Türk Lirası (TRY)' },
  { code: 'USD', label: 'Amerikan Doları (USD)' },
  { code: 'EUR', label: 'Euro (EUR)' },
  { code: 'GBP', label: 'İngiliz Sterlini (GBP)' },
  { code: 'CHF', label: 'İsviçre Frankı (CHF)' },
];

interface Props {
  planPublicId: string;
  className?: string;
}

export function PdfDownloadButton({ planPublicId, className }: Props) {
  const [open, setOpen]       = useState(false);
  const [loading, setLoading] = useState(false);
  const [pos, setPos]         = useState<{ top: number; right: number } | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);

  const toggle = (e: React.MouseEvent) => {
    e.stopPropagation();
    e.preventDefault();
    if (!open && btnRef.current) {
      const rect = btnRef.current.getBoundingClientRect();
      setPos({ top: rect.bottom + 4, right: window.innerWidth - rect.right });
    }
    setOpen(v => !v);
  };

  /* Close on outside click */
  useEffect(() => {
    if (!open) return;
    const close = (e: PointerEvent) => {
      if (btnRef.current && btnRef.current.contains(e.target as Node)) return;
      setOpen(false);
    };
    document.addEventListener('pointerdown', close);
    return () => document.removeEventListener('pointerdown', close);
  }, [open]);

  const download = async (currencyCode: string | null) => {
    setOpen(false);
    setLoading(true);
    try {
      const res = await treatmentPlansApi.downloadPdf(planPublicId, currencyCode ?? undefined);
      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a   = document.createElement('a');
      a.href     = url;
      a.download = `tedavi-plani-${planPublicId}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('PDF oluşturulamadı');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        disabled={loading}
        title="PDF İndir"
        onClick={toggle}
        className={`flex items-center gap-0.5 text-muted-foreground hover:text-foreground transition-colors disabled:opacity-50 ${className ?? ''}`}
      >
        <FileDown className="size-4" />
        <ChevronDown className="size-3" />
      </button>

      {open && pos && createPortal(
        <div
          style={{ position: 'fixed', top: pos.top, right: pos.right, zIndex: 9999 }}
          className="w-52 bg-popover border rounded-md shadow-md py-1"
          onPointerDown={(e) => e.stopPropagation()}
        >
          <p className="px-2 py-1.5 text-[10px] uppercase tracking-wider text-muted-foreground font-medium">
            Para birimi seç
          </p>
          <div className="border-t mb-1" />
          {CURRENCIES.map(({ code, label }) => (
            <button
              key={code ?? 'orig'}
              type="button"
              onClick={() => download(code)}
              className="w-full flex items-center gap-2 px-2 py-1.5 text-left hover:bg-accent transition-colors"
            >
              {code
                ? <span className="font-mono text-xs w-8 text-primary">{code}</span>
                : <span className="font-mono text-xs w-8 text-muted-foreground">—</span>}
              <span className="text-xs text-muted-foreground">{label}</span>
            </button>
          ))}
        </div>,
        document.body,
      )}
    </>
  );
}
