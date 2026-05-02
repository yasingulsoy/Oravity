import { useState, useRef, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import {
  ClipboardList, Search, Info, Pencil, Trash2, X, Check, RotateCcw, User, Building2,
} from 'lucide-react';
import { PdfDownloadButton } from '@/components/PdfDownloadButton';
import { treatmentPlansApi } from '@/api/treatments';
import { patientsApi } from '@/api/patients';
import { buildPatientListRequest } from '@/lib/patientListSearch';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import { Button } from '@/components/ui/button';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import {
  Tooltip, TooltipContent, TooltipProvider, TooltipTrigger,
} from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import { usePermissions } from '@/hooks/usePermissions';
import type { TreatmentPlan, TreatmentPlanItem } from '@/types/treatment';
import type { Patient } from '@/types/patient';

// ── Debounce ──────────────────────────────────────────────────────────────────

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

// ── Status config ─────────────────────────────────────────────────────────────

const statusConfig: Record<string, { label: string; className: string }> = {
  Draft:     { label: 'Taslak',     className: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200' },
  Approved:  { label: 'Onaylandı',  className: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
  Completed: { label: 'Tamamlandı', className: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
  Cancelled: { label: 'İptal',      className: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
};

function fmt(n: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, maximumFractionDigits: 2 }).format(n);
}

// ── Hasta arama kutusu ────────────────────────────────────────────────────────

interface PatientPickerProps {
  selected: Patient | null;
  onSelect: (p: Patient) => void;
  onClear: () => void;
}

function PatientPicker({ selected, onSelect, onClear }: PatientPickerProps) {
  const [query, setQuery]         = useState('');
  const [open, setOpen]           = useState(false);
  const [activeIdx, setActiveIdx] = useState(-1);
  const [listParams, setListParams] = useState<Awaited<ReturnType<typeof buildPatientListRequest>>>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const debouncedQuery = useDebounce(query.trim(), 350);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      const next = await buildPatientListRequest(debouncedQuery, 1, 8);
      if (!cancelled) setListParams(next);
    })();
    return () => { cancelled = true; };
  }, [debouncedQuery]);

  const { data, isFetching } = useQuery({
    queryKey: ['patient-search', listParams],
    queryFn: () => patientsApi.list(listParams!),
    enabled: listParams !== null,
    select: (res) => res.data?.items ?? [],
    staleTime: 30_000,
  });

  const results: Patient[] = data ?? [];
  const showDropdown = open && debouncedQuery.length >= 3;

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  useEffect(() => setActiveIdx(-1), [debouncedQuery]);

  const pick = useCallback((p: Patient) => {
    onSelect(p);
    setQuery('');
    setOpen(false);
  }, [onSelect]);

  if (selected) {
    return (
      <div className="flex items-center gap-2 h-9 px-3 rounded-md border bg-muted/40 text-sm">
        <User className="size-4 text-muted-foreground shrink-0" />
        <span className="font-medium flex-1">{selected.firstName} {selected.lastName}</span>
        {selected.phone && (
          <span className="text-xs text-muted-foreground">{selected.phone}</span>
        )}
        <button
          onClick={onClear}
          className="ml-1 text-muted-foreground hover:text-foreground"
          aria-label="Seçimi temizle"
        >
          <X className="size-3.5" />
        </button>
      </div>
    );
  }

  return (
    <div ref={containerRef} className="relative flex-1">
      <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
      <Input
        value={query}
        onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)}
        onKeyDown={(e) => {
          if (!showDropdown || results.length === 0) return;
          if (e.key === 'ArrowDown') { e.preventDefault(); setActiveIdx((i) => Math.min(i + 1, results.length - 1)); }
          else if (e.key === 'ArrowUp') { e.preventDefault(); setActiveIdx((i) => Math.max(i - 1, -1)); }
          else if (e.key === 'Enter' && activeIdx >= 0) { e.preventDefault(); pick(results[activeIdx]); }
          else if (e.key === 'Escape') setOpen(false);
        }}
        placeholder="Ad soyad, telefon veya TC ile hasta ara…"
        className="pl-9"
        autoComplete="off"
        spellCheck={false}
      />

      {showDropdown && (
        <div className="absolute top-full mt-1 left-0 right-0 z-50 rounded-md border bg-popover shadow-md overflow-hidden">
          {isFetching && results.length === 0 ? (
            <div className="px-3 py-2.5 text-sm text-muted-foreground">Aranıyor…</div>
          ) : results.length === 0 ? (
            <div className="px-3 py-2.5 text-sm text-muted-foreground">Sonuç bulunamadı</div>
          ) : (
            <ul>
              {results.map((p, i) => (
                <li key={p.publicId}>
                  <button
                    type="button"
                    className={cn(
                      'w-full flex items-center gap-2.5 px-3 py-2 text-left text-sm transition-colors',
                      i === activeIdx ? 'bg-accent text-accent-foreground' : 'hover:bg-accent/60',
                    )}
                    onMouseEnter={() => setActiveIdx(i)}
                    onMouseDown={(e) => { e.preventDefault(); pick(p); }}
                  >
                    <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-muted">
                      <User className="size-3.5 text-muted-foreground" />
                    </span>
                    <span className="flex-1 min-w-0">
                      <span className="block font-medium truncate">{p.firstName} {p.lastName}</span>
                      {p.phone && (
                        <span className="block text-[11px] text-muted-foreground">{p.phone}</span>
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

// ── Kurum katkısı inline input ────────────────────────────────────────────────

interface ContribInputProps {
  initialValue: number | null;
  refValue: number;
  isPending: boolean;
  onSave: (amount: number | null) => void;
}

function ContribInput({ initialValue, refValue, onSave, isPending }: ContribInputProps) {
  const [raw, setRaw]       = useState(initialValue != null ? String(initialValue) : '');
  const [editing, setEditing] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const commit = () => {
    const n = raw === '' ? null : Number(raw);
    if (n !== null && (isNaN(n) || n < 0)) { setRaw(initialValue != null ? String(initialValue) : ''); setEditing(false); return; }
    onSave(n);
    setEditing(false);
  };
  const revert = () => { setRaw(initialValue != null ? String(initialValue) : ''); setEditing(false); };

  const isOverRef = raw !== '' && Number(raw) > refValue;

  if (!editing) {
    return (
      <button
        className="min-w-[60px] text-right text-xs tabular-nums px-2 py-1 rounded hover:bg-muted/60 transition-colors"
        onClick={() => { setEditing(true); setTimeout(() => inputRef.current?.select(), 0); }}
        disabled={isPending}
      >
        {initialValue != null && initialValue > 0
          ? <span className="font-medium text-foreground">{initialValue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} ₺</span>
          : <span className="text-muted-foreground">—</span>}
      </button>
    );
  }

  return (
    <input
      ref={inputRef}
      autoFocus
      type="number"
      min={0}
      step="0.01"
      value={raw}
      onChange={(e) => setRaw(e.target.value)}
      onBlur={commit}
      onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commit(); } if (e.key === 'Escape') revert(); }}
      className={cn(
        'w-24 text-right text-xs tabular-nums px-2 py-1 rounded border bg-background outline-none',
        isOverRef ? 'border-amber-400 text-amber-600' : 'border-border focus:border-primary',
      )}
      placeholder="0.00"
    />
  );
}

// ── Fiyat tooltip ─────────────────────────────────────────────────────────────

function PriceTooltip({ item }: { item: TreatmentPlanItem }) {
  const discountAmount = item.unitPrice - item.finalPrice;
  const hasDiscount = item.discountRate > 0;
  const hasKdv = item.kdvRate > 0;

  const hasContribution = item.institutionContributionAmount != null && item.institutionContributionAmount > 0;

  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <button
            type="button"
            className="inline-flex shrink-0 items-center justify-center rounded-full text-muted-foreground hover:text-foreground transition-colors"
            aria-label="Fiyat hesaplama detayı"
          />
        }
      >
        <Info className="size-3.5" />
      </TooltipTrigger>
      <TooltipContent side="top" align="end" className="w-56 p-0 overflow-hidden">
        <div className="bg-popover text-popover-foreground text-xs">
          <div className="px-3 py-2 font-semibold border-b text-[11px] uppercase tracking-wider text-muted-foreground">
            Fiyat Hesaplama
          </div>
          <div className="px-3 py-2 space-y-1.5">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Birim Fiyat</span>
              <span className="font-medium tabular-nums">{fmt(item.unitPrice)}</span>
            </div>
            {hasDiscount && (
              <div className="flex justify-between text-amber-600 dark:text-amber-400">
                <span>İndirim (%{item.discountRate})</span>
                <span className="font-medium tabular-nums">−{fmt(discountAmount)}</span>
              </div>
            )}
            {hasDiscount && (
              <div className="flex justify-between border-t pt-1.5">
                <span className="text-muted-foreground">Net Fiyat</span>
                <span className="font-semibold tabular-nums">{fmt(item.finalPrice)}</span>
              </div>
            )}
            {hasKdv && (
              <div className="flex justify-between text-muted-foreground">
                <span>KDV (%{item.kdvRate})</span>
                <span className="tabular-nums">+{fmt(item.kdvAmount)}</span>
              </div>
            )}
            {(hasKdv || hasDiscount) && (
              <div className="flex justify-between border-t pt-1.5 font-semibold">
                <span>Toplam</span>
                <span className="tabular-nums">{fmt(item.totalAmount)}</span>
              </div>
            )}
            {!hasDiscount && !hasKdv && (
              <p className="text-muted-foreground text-[11px]">İndirim veya KDV uygulanmadı.</p>
            )}
            {hasContribution && (
              <>
                <div className="flex justify-between border-t pt-1.5 text-blue-600 dark:text-blue-400">
                  <span>Kurum Katkısı</span>
                  <span className="tabular-nums">−{fmt(item.institutionContributionAmount!)}</span>
                </div>
                <div className="flex justify-between font-semibold text-emerald-600 dark:text-emerald-400">
                  <span>Hasta Payı</span>
                  <span className="tabular-nums">{fmt(item.patientAmount)}</span>
                </div>
              </>
            )}
          </div>
        </div>
      </TooltipContent>
    </Tooltip>
  );
}

// ── Kalem satır içi fiyat düzenleme ──────────────────────────────────────────

interface ItemRowProps {
  plan: TreatmentPlan;
  item: TreatmentPlanItem;
  onItemUpdated: () => void;
}

function ItemRow({ plan, item, onItemUpdated }: ItemRowProps) {
  const [editing, setEditing]           = useState(false);
  const [unitPrice, setUnitPrice]       = useState(String(item.unitPrice));
  const [discountRate, setDiscountRate] = useState(String(item.discountRate));

  const mutation = useMutation({
    mutationFn: () =>
      treatmentPlansApi.updateItem(plan.publicId, item.publicId, {
        unitPrice: Number(unitPrice),
        discountRate: Number(discountRate),
        toothNumber: item.toothNumber ?? null,
      }),
    onSuccess: () => { setEditing(false); onItemUpdated(); toast.success('Kalem güncellendi.'); },
    onError: () => toast.error('Kalem güncellenemedi.'),
  });

  const contribMutation = useMutation({
    mutationFn: (amount: number | null) =>
      treatmentPlansApi.setContribution(plan.publicId, item.publicId, amount),
    onSuccess: () => { onItemUpdated(); },
    onError: () => toast.error('Kurum katkısı kaydedilemedi.'),
  });

  const canEdit = plan.statusLabel === 'Draft' || plan.statusLabel === 'Approved';
  // Katkı girilebilir durumlar: Onaylandı veya Tamamlandı; sadece provizyon veya bilinmeyen kurum tipi
  const canSetContrib = (item.status === 'Approved' || item.status === 'Completed') && plan.institutionPaymentModel !== 1;
  const hasContrib = item.institutionContributionAmount != null && item.institutionContributionAmount > 0;
  const isForeign  = item.priceCurrency !== 'TRY';
  const contribRef = isForeign ? item.priceBaseAmount : item.totalAmount;

  if (editing) {
    return (
      <div className="flex items-center gap-2 rounded-md bg-muted/50 px-3 py-2 text-sm">
        <span className="flex-1 truncate font-medium">{item.treatmentName}</span>
        {item.toothNumber && (
          <Badge variant="outline" className="shrink-0 text-[10px] px-1.5 py-0">Diş {item.toothNumber}</Badge>
        )}
        <Input
          className="h-7 w-28 text-xs tabular-nums"
          value={unitPrice}
          onChange={(e) => setUnitPrice(e.target.value)}
          type="number" min={0}
          placeholder="Fiyat"
        />
        <div className="flex items-center gap-1">
          <Input
            className="h-7 w-20 text-xs tabular-nums"
            value={discountRate}
            onChange={(e) => setDiscountRate(e.target.value)}
            type="number" min={0} max={100}
            placeholder="İsk %"
          />
          <span className="text-xs text-muted-foreground">%</span>
        </div>
        <button className="text-green-600 hover:text-green-700" onClick={() => mutation.mutate()} disabled={mutation.isPending} aria-label="Kaydet">
          <Check className="size-4" />
        </button>
        <button className="text-muted-foreground hover:text-foreground" onClick={() => { setUnitPrice(String(item.unitPrice)); setDiscountRate(String(item.discountRate)); setEditing(false); }} aria-label="İptal">
          <X className="size-4" />
        </button>
      </div>
    );
  }

  return (
    <div className={cn('flex items-center gap-2 rounded-md px-3 py-2 text-sm group', item.completedAt ? 'bg-muted/30' : 'bg-muted/50')}>
      {/* Sol: isim + etiketler */}
      <div className="flex min-w-0 flex-1 flex-wrap items-center gap-1.5">
        <span className={cn('truncate', item.completedAt && 'line-through text-muted-foreground')}>
          {item.treatmentName}
        </span>
        {item.toothNumber && (
          <Badge variant="outline" className="shrink-0 text-[10px] px-1.5 py-0">Diş {item.toothNumber}</Badge>
        )}
        <span className="shrink-0 flex flex-col items-end leading-tight">
          {item.listPrice != null && item.listPrice > item.totalAmount && (
            <span className="line-through text-[10px] text-muted-foreground tabular-nums">
              {fmt(item.listPrice, item.priceCurrency)}
            </span>
          )}
          <span className="rounded-full bg-primary/10 px-2 py-0.5 text-[11px] font-medium text-primary tabular-nums">
            {fmt(item.totalAmount, item.priceCurrency)}
          </span>
        </span>
        <PriceTooltip item={item} />
        {hasContrib && (
          <span className="shrink-0 rounded-full bg-emerald-100 dark:bg-emerald-900/40 px-2 py-0.5 text-[11px] font-medium text-emerald-700 dark:text-emerald-300 tabular-nums">
            Hasta: {fmt(item.patientAmount)}
          </span>
        )}
      </div>

      {/* Kurum Payı inline — sadece provizyon/bilinmeyen ve katkı girilebilir durumda */}
      {canSetContrib && (
        <div className="shrink-0 flex items-center gap-1 text-xs text-muted-foreground">
          <span className="hidden sm:inline">Kurum:</span>
          <ContribInput
            key={`${item.publicId}:${item.institutionContributionAmount}`}
            initialValue={item.institutionContributionAmount}
            refValue={contribRef}
            isPending={contribMutation.isPending}
            onSave={(amount) => contribMutation.mutate(amount)}
          />
        </div>
      )}

      {/* Sağ: toplam + düzenle */}
      <div className="ml-1 flex items-center gap-1.5 shrink-0">
        <div className="text-right">
          <div className="font-semibold tabular-nums">{fmt(item.totalAmount)}</div>
          {item.kdvRate > 0 && <div className="text-[10px] text-muted-foreground">KDV dahil</div>}
        </div>
        {canEdit && !item.completedAt && (
          <button
            className="opacity-0 group-hover:opacity-100 text-muted-foreground hover:text-foreground transition-opacity"
            onClick={() => setEditing(true)}
            aria-label="Kalemi düzenle"
          >
            <Pencil className="size-3.5" />
          </button>
        )}
      </div>
    </div>
  );
}

// ── Ana sayfa ─────────────────────────────────────────────────────────────────

export function TreatmentPlansPage() {
  const qc = useQueryClient();
  const { hasPermission } = usePermissions();

  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [editingPlan, setEditingPlan]         = useState<TreatmentPlan | null>(null);
  const [deletingPlan, setDeletingPlan]       = useState<TreatmentPlan | null>(null);
  const [editName, setEditName]   = useState('');
  const [editNotes, setEditNotes] = useState('');

  const patientPublicId = selectedPatient?.publicId ?? '';

  const { data, isLoading, isError } = useQuery({
    queryKey: ['treatment-plans', patientPublicId],
    queryFn: () => treatmentPlansApi.getByPatient(patientPublicId),
    enabled: !!patientPublicId,
  });

  const plans: TreatmentPlan[] = data?.data ?? [];

  const invalidate = () => qc.invalidateQueries({ queryKey: ['treatment-plans', patientPublicId] });

  const updatePlanMutation = useMutation({
    mutationFn: ({ id, name, notes }: { id: string; name: string; notes: string | null }) =>
      treatmentPlansApi.update(id, { name, notes }),
    onSuccess: () => { toast.success('Plan güncellendi.'); setEditingPlan(null); invalidate(); },
    onError: (e: any) => { if (!e._403handled) toast.error('Plan güncellenemedi.'); },
  });

  const deletePlanMutation = useMutation({
    mutationFn: (id: string) => treatmentPlansApi.deletePlan(id),
    onSuccess: () => {
      toast.success(deletingPlan?.statusLabel === 'Draft' ? 'Plan silindi.' : 'Plan iptal edildi.');
      setDeletingPlan(null);
      invalidate();
    },
    onError: (e: any) => { if (!e._403handled) toast.error('Plan silinemedi.'); },
  });

  return (
    <TooltipProvider delayDuration={200}>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Tedavi Planları</h1>
          <p className="text-muted-foreground">Hasta tedavi planlarını görüntüleyin ve yönetin</p>
        </div>

        {/* Hasta seçici */}
        <Card>
          <CardContent className="pt-4 pb-4">
            <PatientPicker
              selected={selectedPatient}
              onSelect={setSelectedPatient}
              onClear={() => setSelectedPatient(null)}
            />
          </CardContent>
        </Card>

        {/* Yükleniyor */}
        {isLoading && (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-32 w-full" />)}
          </div>
        )}

        {/* Hata */}
        {isError && (
          <Card>
            <CardContent className="py-8 text-center text-sm text-muted-foreground">
              Tedavi planları yüklenirken hata oluştu.
            </CardContent>
          </Card>
        )}

        {/* Boş */}
        {!isLoading && patientPublicId && plans.length === 0 && (
          <Card>
            <CardContent className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
              <ClipboardList className="h-10 w-10" />
              <p>Bu hasta için tedavi planı bulunamadı.</p>
            </CardContent>
          </Card>
        )}

        {/* Plan listesi */}
        {plans.map((plan) => {
          const cfg            = statusConfig[plan.statusLabel] ?? statusConfig.Draft;
          const completed      = plan.items.filter((i) => i.completedAt).length;
          const planTotal      = plan.items.reduce((s, i) => s + i.totalAmount, 0);
          const canEdit        = (plan.statusLabel === 'Draft' || plan.statusLabel === 'Approved') && hasPermission('treatment_plan:edit');
          const canDelete      = plan.statusLabel !== 'Completed' && hasPermission('treatment_plan:delete');
          const hasListDisc    = plan.items.some(i => i.listPrice != null && i.listPrice > i.totalAmount);
          const catalogTotal   = hasListDisc
            ? plan.items.reduce((s, i) => s + (i.listPrice != null && i.listPrice > i.totalAmount ? i.listPrice : i.totalAmount), 0)
            : 0;
          const savedAmount    = hasListDisc
            ? plan.items.reduce((s, i) => s + ((i.listPrice ?? i.totalAmount) - i.totalAmount), 0)
            : 0;

          return (
            <Card key={plan.publicId}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="flex items-center gap-2">
                    <ClipboardList className="h-5 w-5" />
                    {plan.name}
                  </CardTitle>
                  <div className="flex items-center gap-1">
                    <Badge className={cfg.className}>{cfg.label}</Badge>
                    {canEdit && (
                      <Button variant="ghost" size="icon" className="h-8 w-8"
                        title="Planı düzenle"
                        onClick={() => { setEditName(plan.name); setEditNotes(plan.notes ?? ''); setEditingPlan(plan); }}
                      >
                        <Pencil className="size-4" />
                      </Button>
                    )}
                    {canDelete && (
                      <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive"
                        title={plan.statusLabel === 'Draft' ? 'Planı sil' : 'Planı iptal et'}
                        onClick={() => setDeletingPlan(plan)}
                      >
                        {plan.statusLabel === 'Draft' ? <Trash2 className="size-4" /> : <RotateCcw className="size-4" />}
                      </Button>
                    )}
                    <PdfDownloadButton planPublicId={plan.publicId} />
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-wrap">
                  <p className="text-sm text-muted-foreground">
                    {format(new Date(plan.createdAt), 'dd.MM.yyyy')}
                  </p>
                  {plan.institutionName && (
                    <span className={cn(
                      'inline-flex items-center gap-1 text-[11px] px-2 py-0.5 rounded-full font-medium',
                      plan.institutionPaymentModel === 2
                        ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300'
                        : 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
                    )}>
                      <Building2 className="size-3" />
                      {plan.institutionName}
                      {plan.institutionPaymentModel === 2 ? ' · Provizyon' : ' · İndirim'}
                    </span>
                  )}
                </div>
              </CardHeader>

              <CardContent className="space-y-4">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{completed}/{plan.items.length} kalem tamamlandı</span>
                  <span className="font-semibold">{fmt(planTotal)}</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full bg-primary transition-all"
                    style={{ width: plan.items.length ? `${(completed / plan.items.length) * 100}%` : '0%' }}
                  />
                </div>
                {plan.items.length > 0 && (
                  <>
                    <Separator />
                    <div className="space-y-1.5">
                      {plan.items.map((item) => (
                        <ItemRow key={item.publicId} plan={plan} item={item} onItemUpdated={invalidate} />
                      ))}
                    </div>
                    {hasListDisc && (
                      <div className="flex items-center justify-end gap-5 rounded-md border border-dashed border-emerald-300 dark:border-emerald-700 bg-emerald-50 dark:bg-emerald-950/30 px-4 py-2.5 text-sm">
                        <div className="text-right text-muted-foreground">
                          <div className="text-[10px] uppercase tracking-wide">Liste Fiyatı</div>
                          <div className="line-through tabular-nums">{fmt(catalogTotal)}</div>
                        </div>
                        <div className="text-right text-emerald-600 dark:text-emerald-400">
                          <div className="text-[10px] uppercase tracking-wide">Tasarruf</div>
                          <div className="font-medium tabular-nums">−{fmt(savedAmount)}</div>
                        </div>
                        <div className="text-right">
                          <div className="text-[10px] uppercase tracking-wide text-muted-foreground">Planın Tutarı</div>
                          <div className="font-bold tabular-nums">{fmt(planTotal)}</div>
                        </div>
                      </div>
                    )}
                  </>
                )}
                {plan.notes && (
                  <>
                    <Separator />
                    <p className="text-sm italic text-muted-foreground">{plan.notes}</p>
                  </>
                )}
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Plan düzenleme dialog'u */}
      <Dialog open={!!editingPlan} onOpenChange={(o) => !o && setEditingPlan(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Planı Düzenle</DialogTitle></DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1.5">
              <Label htmlFor="ep-name">Plan Adı</Label>
              <Input id="ep-name" value={editName} onChange={(e) => setEditName(e.target.value)} placeholder="Plan adı" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="ep-notes">Notlar</Label>
              <Textarea id="ep-notes" rows={3} value={editNotes} onChange={(e) => setEditNotes(e.target.value)} placeholder="Opsiyonel notlar…" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingPlan(null)} disabled={updatePlanMutation.isPending}>İptal</Button>
            <Button
              onClick={() => editingPlan && updatePlanMutation.mutate({ id: editingPlan.publicId, name: editName.trim(), notes: editNotes.trim() || null })}
              disabled={!editName.trim() || updatePlanMutation.isPending}
            >
              {updatePlanMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Plan silme / iptal onayı */}
      <AlertDialog open={!!deletingPlan} onOpenChange={(o) => !o && setDeletingPlan(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{deletingPlan?.statusLabel === 'Draft' ? 'Planı Sil' : 'Planı İptal Et'}</AlertDialogTitle>
            <AlertDialogDescription>
              {deletingPlan?.statusLabel === 'Draft'
                ? `"${deletingPlan?.name}" planı kalıcı olarak silinecek.`
                : `"${deletingPlan?.name}" planı iptal edilecek. Yeniden açılamaz.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletePlanMutation.isPending}>Vazgeç</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={deletePlanMutation.isPending}
              onClick={() => deletingPlan && deletePlanMutation.mutate(deletingPlan.publicId)}
            >
              {deletingPlan?.statusLabel === 'Draft' ? 'Evet, Sil' : 'Evet, İptal Et'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

    </TooltipProvider>
  );
}
