import { useState, useRef, useEffect, useCallback } from 'react';
import { ChevronDown, Search, X, Check } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface MultiSelectOption {
  value: number;
  label: string;
}

interface MultiSelectProps {
  label: string;
  options: MultiSelectOption[];
  selected: number[];
  onChange: (selected: number[]) => void;
  allLabel?: string;
  loading?: boolean;
  className?: string;
}

export function MultiSelect({
  label,
  options,
  selected,
  onChange,
  allLabel,
  loading = false,
  className,
}: MultiSelectProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  const handleClickOutside = useCallback((e: MouseEvent) => {
    if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
      setOpen(false);
    }
  }, []);

  useEffect(() => {
    if (open) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open, handleClickOutside]);

  const allSelected = options.length > 0 && selected.length === options.length;
  const someSelected = selected.length > 0 && !allSelected;

  const filtered = options.filter((o) =>
    o.label.toLowerCase().includes(search.toLowerCase())
  );

  function toggleItem(value: number) {
    if (selected.includes(value)) {
      onChange(selected.filter((v) => v !== value));
    } else {
      onChange([...selected, value]);
    }
  }

  function toggleAll() {
    onChange(allSelected ? [] : options.map((o) => o.value));
  }

  const displayText = loading
    ? 'Yukleniyor...'
    : allSelected
      ? (allLabel ?? 'Tumu')
      : selected.length === 0
        ? label
        : `${selected.length} secili`;

  return (
    <div ref={containerRef} className={cn('relative', className)}>
      <button
        type="button"
        onClick={() => !loading && setOpen((v) => !v)}
        disabled={loading}
        className={cn(
          'flex h-9 items-center gap-1.5 rounded-md border border-input bg-background px-3 text-sm',
          'hover:bg-accent hover:text-accent-foreground transition-colors',
          'min-w-[140px] justify-between',
          open && 'ring-2 ring-ring',
          loading && 'opacity-50 cursor-not-allowed',
        )}
      >
        <span className="truncate">{displayText}</span>
        <ChevronDown className={cn('size-4 shrink-0 opacity-50 transition-transform', open && 'rotate-180')} />
      </button>

      {open && (
        <div
          className={cn(
            'absolute top-full left-0 z-50 mt-1 w-64 rounded-md border bg-popover shadow-md',
            'animate-in fade-in-0 zoom-in-95',
          )}
        >
          {/* Arama */}
          <div className="flex items-center gap-2 border-b px-3 py-2">
            <Search className="size-4 shrink-0 text-muted-foreground" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Ara..."
              className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
              autoFocus
            />
            {search && (
              <button
                type="button"
                onClick={() => setSearch('')}
                className="text-muted-foreground hover:text-foreground"
              >
                <X className="size-3.5" />
              </button>
            )}
          </div>

          <div className="max-h-60 overflow-y-auto p-1">
            {/* Tümünü seç */}
            <div
              role="option"
              aria-selected={allSelected}
              onClick={toggleAll}
              className="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent select-none"
            >
              <Checkmark checked={allSelected} indeterminate={someSelected} />
              <span className="font-medium">{allLabel ?? 'Tumu'}</span>
            </div>

            <div className="my-1 h-px bg-border" />

            {filtered.length === 0 && (
              <p className="px-2 py-4 text-center text-sm text-muted-foreground">
                Sonuc bulunamadi
              </p>
            )}

            {filtered.map((option) => {
              const isChecked = selected.includes(option.value);
              return (
                <div
                  key={option.value}
                  role="option"
                  aria-selected={isChecked}
                  onClick={() => toggleItem(option.value)}
                  className="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent select-none"
                >
                  <Checkmark checked={isChecked} />
                  <span className="truncate">{option.label}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

function Checkmark({ checked, indeterminate = false }: { checked: boolean; indeterminate?: boolean }) {
  return (
    <div
      className={cn(
        'flex size-4 shrink-0 items-center justify-center rounded-sm border transition-colors',
        checked || indeterminate
          ? 'bg-primary border-primary text-primary-foreground'
          : 'border-input bg-background',
      )}
    >
      {indeterminate && <span className="h-0.5 w-2 rounded bg-current" />}
      {checked && !indeterminate && <Check className="size-3" strokeWidth={3} />}
    </div>
  );
}
