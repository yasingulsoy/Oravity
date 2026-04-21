import { useState } from 'react';
import { X, Search } from 'lucide-react';
import { DENTAL_SYMBOLS, SYMBOL_CATEGORIES, getSymbol } from '@/types/dentalSymbols';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle,
} from '@/components/ui/dialog';
import { cn } from '@/lib/utils';

// Tek bir diş önizlemesi (36×44 viewBox, sabit referans dişi göster)
function ToothPreview({ symbolCode }: { symbolCode: string | null }) {
  const sym = getSymbol(symbolCode);
  return (
    <svg viewBox="0 0 36 44" width={72} height={88} className="overflow-visible drop-shadow-sm">
      {/* Diş arka plan */}
      <rect x="0" y="0" width="36" height="44" fill="#ffffff" />
      {/* Yüzeyler */}
      <polygon points="0,0 36,0 28,12 8,12"   fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="8,32 28,32 36,44 0,44"  fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="0,0 8,12 8,32 0,44"     fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="28,12 36,0 36,44 28,32" fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <rect x="8" y="12" width="20" height="20" fill="#e0f2fe" stroke="#94a3b8" strokeWidth="1" />
      {/* Sembol overlay */}
      {sym && sym.overlay()}
    </svg>
  );
}

// Küçük önizleme (liste içi)
function ToothMini({ symbolCode }: { symbolCode: string | null }) {
  const sym = getSymbol(symbolCode);
  return (
    <svg viewBox="0 0 36 44" width={32} height={40} className="overflow-visible">
      <rect x="0" y="0" width="36" height="44" fill="#ffffff" />
      <polygon points="0,0 36,0 28,12 8,12"   fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="8,32 28,32 36,44 0,44"  fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="0,0 8,12 8,32 0,44"     fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <polygon points="28,12 36,0 36,44 28,32" fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
      <rect x="8" y="12" width="20" height="20" fill="#e0f2fe" stroke="#94a3b8" strokeWidth="1" />
      {sym && sym.overlay()}
    </svg>
  );
}

interface Props {
  open:     boolean;
  value:    string | null;
  onChange: (code: string | null) => void;
  onClose:  () => void;
}

export function DentalSymbolPicker({ open, value, onChange, onClose }: Props) {
  const [search,   setSearch]   = useState('');
  const [category, setCategory] = useState<string | null>(null);

  const filtered = DENTAL_SYMBOLS.filter(s => {
    if (category && s.category !== category) return false;
    if (search && !s.label.toLowerCase().includes(search.toLowerCase()) &&
        !s.code.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const selected = getSymbol(value);

  return (
    <Dialog open={open} onOpenChange={o => !o && onClose()}>
      <DialogContent className="max-w-3xl p-0 overflow-hidden">
        <DialogHeader className="px-5 pt-4 pb-3 border-b">
          <DialogTitle>Diş Şeması Simgesi Seç</DialogTitle>
        </DialogHeader>

        <div className="flex h-[520px]">
          {/* Sol: liste */}
          <div className="flex-1 flex flex-col border-r">
            {/* Filtre */}
            <div className="flex items-center gap-2 p-3 border-b">
              <div className="relative flex-1">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground" />
                <Input
                  className="pl-8 h-8 text-sm"
                  placeholder="Simge ara..."
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                />
              </div>
            </div>

            {/* Kategori sekmeler */}
            <div className="flex gap-1 px-3 py-2 border-b flex-wrap">
              <button
                onClick={() => setCategory(null)}
                className={cn('text-[11px] px-2 py-0.5 rounded border transition-all',
                  !category ? 'bg-primary text-primary-foreground border-primary' : 'text-muted-foreground hover:text-foreground')}
              >
                Tümü
              </button>
              {SYMBOL_CATEGORIES.map(cat => (
                <button key={cat}
                  onClick={() => setCategory(cat === category ? null : cat)}
                  className={cn('text-[11px] px-2 py-0.5 rounded border transition-all',
                    category === cat ? 'bg-primary text-primary-foreground border-primary' : 'text-muted-foreground hover:text-foreground')}
                >
                  {cat}
                </button>
              ))}
            </div>

            {/* Simge listesi */}
            <div className="flex-1 overflow-y-auto p-2 grid grid-cols-2 gap-1.5 content-start">
              {/* Simge yok seçeneği */}
              <button
                onClick={() => onChange(null)}
                className={cn(
                  'flex items-center gap-2.5 px-3 py-2 rounded-lg border text-left transition-all text-sm',
                  !value
                    ? 'border-primary bg-primary/5 ring-1 ring-primary/30'
                    : 'hover:bg-muted/50'
                )}
              >
                <div className="size-8 flex items-center justify-center rounded border border-dashed text-muted-foreground">
                  <X className="size-4" />
                </div>
                <span className="text-xs text-muted-foreground">Simge yok</span>
              </button>

              {filtered.map(sym => (
                <button
                  key={sym.code}
                  onClick={() => onChange(sym.code)}
                  className={cn(
                    'flex items-center gap-2.5 px-3 py-2 rounded-lg border text-left transition-all',
                    value === sym.code
                      ? 'border-primary bg-primary/5 ring-1 ring-primary/30'
                      : 'hover:bg-muted/50'
                  )}
                >
                  <ToothMini symbolCode={sym.code} />
                  <div className="min-w-0">
                    <p className="text-xs font-medium truncate">{sym.label}</p>
                    <p className="text-[10px] text-muted-foreground">{sym.category}</p>
                  </div>
                </button>
              ))}

              {filtered.length === 0 && (
                <div className="col-span-2 text-center py-8 text-sm text-muted-foreground">
                  Simge bulunamadı.
                </div>
              )}
            </div>
          </div>

          {/* Sağ: önizleme */}
          <div className="w-52 flex flex-col items-center justify-center gap-4 p-6">
            <p className="text-xs text-muted-foreground uppercase tracking-wider font-medium">Önizleme</p>
            <ToothPreview symbolCode={value} />
            {selected ? (
              <div className="text-center">
                <p className="text-sm font-medium">{selected.label}</p>
                <p className="text-xs text-muted-foreground">{selected.category}</p>
                <span className="inline-block mt-1 px-2 py-0.5 rounded text-[10px] font-mono"
                  style={{ background: selected.color + '20', color: selected.color }}>
                  {selected.code}
                </span>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground italic">Simge seçilmedi</p>
            )}

            <Button size="sm" className="w-full mt-2" onClick={onClose}>
              Tamam
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
