import { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search, User } from 'lucide-react';
import { patientsApi } from '@/api/patients';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

export function PatientSearch() {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const debouncedQuery = useDebounce(query.trim(), 300);

  const { data, isFetching } = useQuery({
    queryKey: ['patient-search', debouncedQuery],
    queryFn: () => patientsApi.getList({ page: 1, pageSize: 8, search: debouncedQuery }),
    enabled: debouncedQuery.length >= 2,
    select: (res) => res.data?.items ?? [],
    staleTime: 30_000,
  });

  const results = data ?? [];
  const showDropdown = open && debouncedQuery.length >= 2;

  // Dışarı tıklanınca kapat
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  // Query değişince aktif indexi sıfırla
  useEffect(() => setActiveIndex(-1), [debouncedQuery]);

  const goToPatient = useCallback((id: number) => {
    navigate(`/patients/${id}`);
    setQuery('');
    setOpen(false);
  }, [navigate]);

  function handleKeyDown(e: React.KeyboardEvent) {
    if (!showDropdown || results.length === 0) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIndex((i) => Math.min(i + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, -1));
    } else if (e.key === 'Enter' && activeIndex >= 0) {
      e.preventDefault();
      goToPatient(results[activeIndex].id);
    } else if (e.key === 'Escape') {
      setOpen(false);
    }
  }

  return (
    <div ref={containerRef} className="relative w-56">
      <div className="relative">
        <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
        <Input
          ref={inputRef}
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder="Hasta ara..."
          className="h-8 pl-8 pr-3 text-sm"
        />
      </div>

      {showDropdown && (
        <div className="absolute top-full mt-1 left-0 right-0 z-50 rounded-md border bg-popover shadow-md overflow-hidden">
          {isFetching && results.length === 0 ? (
            <div className="px-3 py-2 text-xs text-muted-foreground">Aranıyor...</div>
          ) : results.length === 0 ? (
            <div className="px-3 py-2 text-xs text-muted-foreground">Sonuç bulunamadı</div>
          ) : (
            <ul>
              {results.map((p, i) => (
                <li key={p.id}>
                  <button
                    type="button"
                    className={cn(
                      'w-full flex items-center gap-2.5 px-3 py-2 text-left text-sm transition-colors',
                      i === activeIndex
                        ? 'bg-accent text-accent-foreground'
                        : 'hover:bg-accent/60',
                    )}
                    onMouseEnter={() => setActiveIndex(i)}
                    onMouseDown={(e) => { e.preventDefault(); goToPatient(p.id); }}
                  >
                    <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-muted">
                      <User className="h-3.5 w-3.5 text-muted-foreground" />
                    </span>
                    <span className="flex-1 min-w-0">
                      <span className="block font-medium truncate">
                        {p.firstName} {p.lastName}
                      </span>
                      {p.phone && (
                        <span className="block text-[11px] text-muted-foreground truncate">
                          {p.phone}
                        </span>
                      )}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
