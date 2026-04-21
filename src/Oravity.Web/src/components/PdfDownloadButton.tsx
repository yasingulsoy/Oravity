import { useState, useRef, useEffect } from 'react';
import { FileDown, ChevronDown } from 'lucide-react';
import { toast } from 'sonner';
import { treatmentPlansApi } from '@/api/treatments';
import { cn } from '@/lib/utils';

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
  const ref = useRef<HTMLDivElement>(null);

  // Dışarı tıklayınca kapat
  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
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
    <div ref={ref} className={cn('relative', className)}>
      <button
        disabled={loading}
        title="PDF İndir"
        className="flex items-center gap-0.5 text-muted-foreground hover:text-foreground transition-colors disabled:opacity-50"
        onClick={() => setOpen(v => !v)}
      >
        <FileDown className="size-4" />
        <ChevronDown className="size-3" />
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-1 z-50 w-52 rounded-lg border bg-popover shadow-md py-1 text-sm">
          <p className="px-3 py-1 text-[10px] uppercase tracking-wider text-muted-foreground font-medium">
            Para birimi seç
          </p>
          {CURRENCIES.map(({ code, label }) => (
            <button
              key={code ?? 'orig'}
              onClick={() => download(code)}
              className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors flex items-center gap-2"
            >
              {code
                ? <span className="font-mono text-xs w-8 text-primary">{code}</span>
                : <span className="font-mono text-xs w-8 text-muted-foreground">—</span>}
              <span className="text-xs text-muted-foreground">{label}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
