import { useState, useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { CreditCard, Wallet, AlertCircle, CheckCircle2, Clock, ArrowRight, ChevronDown, ChevronRight, RotateCcw } from 'lucide-react';
import { toast } from 'sonner';
import { patientAccountApi } from '@/api/patientAccount';
import type { PatientAccountPayment, PatientAccountItem } from '@/api/patientAccount';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

const PAYMENT_METHODS = [
  { value: '1', label: 'Nakit' },
  { value: '2', label: 'Kredi Kartı' },
  { value: '3', label: 'Havale/EFT' },
  { value: '4', label: 'Taksit' },
  { value: '5', label: 'Çek' },
];

const CURRENCIES = [
  { value: 'TRY', label: '₺ TRY', sym: '₺' },
  { value: 'EUR', label: '€ EUR', sym: '€' },
  { value: 'USD', label: '$ USD', sym: '$' },
  { value: 'GBP', label: '£ GBP', sym: '£' },
  { value: 'CHF', label: 'Fr CHF', sym: 'Fr' },
];

function fmt(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function PatientAccountTab({ patientId }: { patientId: number }) {
  const qc = useQueryClient();
  const [collectOpen,    setCollectOpen]    = useState(false);
  const [allocateTarget, setAllocateTarget] = useState<PatientAccountPayment | null>(null);
  const [refundTarget,   setRefundTarget]   = useState<PatientAccountPayment | null>(null);
  const [expandedItems,  setExpandedItems]  = useState<Set<number>>(new Set());

  function toggleExpand(id: number) {
    setExpandedItems(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  const { data, isLoading } = useQuery({
    queryKey: ['patient-account', patientId],
    queryFn:  () => patientAccountApi.getAccount(patientId).then(r => r.data),
  });

  const completedItems = (data?.items ?? []).filter(i => i.status === 'Completed');
  const plannedItems   = (data?.items ?? []).filter(i => i.status !== 'Completed');
  const unpaidItems    = completedItems.filter(i => i.remainingAmount > 0.005);

  const onRefresh = () => qc.invalidateQueries({ queryKey: ['patient-account', patientId] });

  return (
    <div className="space-y-4">
      {/* ── Özet kartları ─────────────────────────────────────────────── */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <SummaryCard
          label="Tamamlanan Tedavi"
          value={data?.totalCompleted}
          loading={isLoading}
          icon={<CheckCircle2 className="size-4 text-blue-500" />}
          color="text-blue-600"
          subtitle="hasta payı"
        />
        <SummaryCard
          label="Tahsil Edilen"
          value={data?.totalPaid}
          loading={isLoading}
          icon={<CreditCard className="size-4 text-green-500" />}
          color="text-green-600"
        />
        <SummaryCard
          label="Açıkta Bekleyen"
          value={data?.unallocatedAmount}
          loading={isLoading}
          icon={<Wallet className="size-4 text-amber-500" />}
          color={(data?.unallocatedAmount ?? 0) > 0.005 ? 'text-amber-600' : 'text-muted-foreground'}
          subtitle={(data?.unallocatedAmount ?? 0) > 0.005 ? 'tedaviye dağıtılmadı' : undefined}
        />
        <SummaryCard
          label="Kalan Borç"
          value={data?.totalRemaining}
          loading={isLoading}
          icon={<AlertCircle className="size-4 text-destructive" />}
          color={(data?.totalRemaining ?? 0) > 0 ? 'text-destructive' : 'text-green-600'}
        />
        <SummaryCard
          label="Planlanan"
          value={data?.totalPlanned}
          loading={isLoading}
          icon={<Clock className="size-4 text-muted-foreground" />}
          color="text-muted-foreground"
          subtitle="henüz yapılmadı"
        />
      </div>

      {/* ── Ödeme Al butonu ────────────────────────────────────────────── */}
      <div className="flex items-center justify-between">
        <h3 className="font-medium text-sm text-muted-foreground">Tedavi Kalemleri</h3>
        <Button size="sm" className="gap-1.5" onClick={() => setCollectOpen(true)}>
          <CreditCard className="size-4" />
          Ödeme Al
        </Button>
      </div>

      {/* ── Tamamlanan kalemler ────────────────────────────────────────── */}
      <Card>
        <CardHeader className="py-3 px-4">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <CheckCircle2 className="size-4 text-blue-500" />
            Tamamlanan Tedaviler
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tedavi</TableHead>
                <TableHead>Diş</TableHead>
                <TableHead>Hekim</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead className="text-right">Tutar</TableHead>
                <TableHead className="text-right">Kurum</TableHead>
                <TableHead className="text-right">Hasta Payı</TableHead>
                <TableHead className="text-right">Ödenen</TableHead>
                <TableHead className="text-right">Kalan</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 9 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-16" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : completedItems.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9} className="text-center text-muted-foreground py-6 text-sm">
                    Tamamlanmış tedavi kalemi yok.
                  </TableCell>
                </TableRow>
              ) : (
                completedItems.flatMap(i => {
                  const isExpanded = expandedItems.has(i.treatmentPlanItemId);
                  const hasAllocs  = i.allocationDetails?.length > 0;
                  return [
                    <TableRow
                      key={i.treatmentPlanItemId}
                      className={cn(hasAllocs && 'cursor-pointer hover:bg-muted/40')}
                      onClick={() => hasAllocs && toggleExpand(i.treatmentPlanItemId)}
                    >
                      {/* Expand chevron */}
                      <TableCell className="font-medium">
                        <div className="flex items-center gap-1.5">
                          {hasAllocs ? (
                            isExpanded
                              ? <ChevronDown className="size-3.5 shrink-0 text-muted-foreground" />
                              : <ChevronRight className="size-3.5 shrink-0 text-muted-foreground" />
                          ) : (
                            <span className="size-3.5 shrink-0 inline-block" />
                          )}
                          {i.treatmentName}
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{i.toothNumber ?? '—'}</TableCell>
                      <TableCell className="text-muted-foreground">{i.doctorName ?? '—'}</TableCell>
                      <TableCell className="text-muted-foreground text-xs">
                        {i.completedAt ? format(new Date(i.completedAt), 'dd MMM yyyy', { locale: tr }) : '—'}
                      </TableCell>
                      {/* Tutar */}
                      <TableCell className="text-right font-medium">
                        {i.priceCurrency !== 'TRY' ? (
                          <span>
                            {i.totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} {i.priceCurrency}
                            <span className="text-xs text-muted-foreground ml-1">≈ {fmt(i.totalAmountTry)}</span>
                          </span>
                        ) : fmt(i.totalAmount)}
                      </TableCell>
                      {/* Kurum payı */}
                      <TableCell className="text-right text-muted-foreground">
                        {i.totalAmountTry - i.patientAmount > 0.005
                          ? fmt(i.totalAmountTry - i.patientAmount)
                          : <span className="text-xs">—</span>}
                      </TableCell>
                      {/* Hasta payı */}
                      <TableCell className="text-right font-medium">{fmt(i.patientAmount)}</TableCell>
                      {/* Ödenen */}
                      <TableCell className="text-right text-green-600">{fmt(i.allocatedAmount)}</TableCell>
                      {/* Kalan */}
                      <TableCell className="text-right">
                        {i.remainingAmount > 0.005 ? (
                          <span className="text-destructive font-semibold">{fmt(i.remainingAmount)}</span>
                        ) : (
                          <span className="text-green-600 text-sm">✓</span>
                        )}
                      </TableCell>
                    </TableRow>,

                    /* ── Allocation detay sub-row ── */
                    isExpanded && hasAllocs && (() => {
                      const hasFx = i.allocationDetails.some(a => a.paymentCurrency !== 'TRY');
                      return (
                        <TableRow key={`alloc-${i.treatmentPlanItemId}`} className="bg-muted/20 hover:bg-muted/20">
                          <TableCell colSpan={9} className="py-0 pl-8 pr-4">
                            <table className="w-full text-xs my-2">
                              <thead>
                                <tr className="text-muted-foreground border-b border-border/50">
                                  <th className="py-1 pr-4 text-left font-medium w-28">Tarih</th>
                                  <th className="py-1 pr-4 text-left font-medium w-24">Yöntem</th>
                                  {hasFx && <th className="py-1 pr-4 text-right font-medium">Tutar</th>}
                                  {hasFx && <th className="py-1 pr-4 text-right font-medium">Kur</th>}
                                  <th className="py-1 text-right font-medium">Dağıtılan (₺)</th>
                                </tr>
                              </thead>
                              <tbody className="divide-y divide-border/30">
                                {i.allocationDetails.map((a, idx) => (
                                  <tr key={idx} className="text-muted-foreground">
                                    <td className="py-1.5 pr-4 tabular-nums">
                                      {format(new Date(a.paymentDate), 'dd MMM yyyy', { locale: tr })}
                                    </td>
                                    <td className="py-1.5 pr-4">{a.methodLabel}</td>
                                    {hasFx && (
                                      <td className="py-1.5 pr-4 text-right tabular-nums">
                                        {a.paymentCurrency !== 'TRY' && (a.paymentAmount ?? 0) > 0 ? (
                                          <span>
                                            {(a.paymentAmount ?? 0).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                                            {' '}<span className="font-medium">{a.paymentCurrency}</span>
                                          </span>
                                        ) : '—'}
                                      </td>
                                    )}
                                    {hasFx && (
                                      <td className="py-1.5 pr-4 text-right tabular-nums">
                                        {a.paymentCurrency !== 'TRY' && (a.exchangeRate ?? 0) > 0 ? (
                                          <span>
                                            {(a.exchangeRate ?? 1).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}
                                            {' ₺/'}
                                            {a.paymentCurrency}
                                          </span>
                                        ) : '—'}
                                      </td>
                                    )}
                                    <td className="py-1.5 text-right font-medium text-foreground tabular-nums">
                                      {fmt(a.allocatedAmount)}
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </TableCell>
                        </TableRow>
                      );
                    })(),
                  ].filter(Boolean);
                })
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* ── Planlanan kalemler (bilgi amaçlı) ─────────────────────────── */}
      {plannedItems.length > 0 && (
        <Card>
          <CardHeader className="py-3 px-4">
            <CardTitle className="text-sm font-medium flex items-center gap-2 text-muted-foreground">
              <Clock className="size-4" />
              Planlanan / Onaylanan Tedaviler
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tedavi</TableHead>
                  <TableHead>Diş</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">Planlanan Tutar</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {plannedItems.map(i => (
                  <TableRow key={i.treatmentPlanItemId} className="text-muted-foreground">
                    <TableCell>{i.treatmentName}</TableCell>
                    <TableCell>{i.toothNumber ?? '—'}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">{i.statusLabel}</Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {i.priceCurrency !== 'TRY' ? (
                        <span>
                          {i.totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} {i.priceCurrency}
                          <span className="text-xs text-muted-foreground ml-1">≈ {fmt(i.totalAmountTry)}</span>
                        </span>
                      ) : fmt(i.totalAmount)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* ── Ödemeler ───────────────────────────────────────────────────── */}
      <Card>
        <CardHeader className="py-3 px-4">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Wallet className="size-4 text-green-500" />
            Ödemeler
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tarih</TableHead>
                <TableHead>Yöntem</TableHead>
                <TableHead className="text-right">Tutar</TableHead>
                <TableHead className="text-right">Dağıtılan</TableHead>
                <TableHead className="text-right">Açıkta</TableHead>
                <TableHead className="w-[120px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 2 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 6 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-16" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (data?.payments ?? []).length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground py-6 text-sm">
                    Henüz ödeme alınmadı.
                  </TableCell>
                </TableRow>
              ) : (
                data!.payments.map(p => (
                  <TableRow key={p.id} className={cn(p.isRefunded && 'opacity-50 line-through')}>
                    <TableCell>{format(new Date(p.paymentDate), 'dd MMM yyyy', { locale: tr })}</TableCell>
                    <TableCell>{p.methodLabel}</TableCell>
                    <TableCell className="text-right font-medium">
                      {p.currency !== 'TRY' ? (
                        <span>
                          {p.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} {p.currency}
                          <span className="text-xs text-muted-foreground ml-1">≈ {fmt(p.baseAmount)}</span>
                        </span>
                      ) : fmt(p.amount)}
                    </TableCell>
                    <TableCell className="text-right text-green-600">{fmt(p.allocatedAmount)}</TableCell>
                    <TableCell className="text-right">
                      {p.unallocatedAmount > 0.005 ? (
                        <span className="text-amber-600 font-semibold">{fmt(p.unallocatedAmount)}</span>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </TableCell>
                    {/* Aksiyonlar */}
                    <TableCell className="px-2">
                      <div className="flex items-center gap-1 justify-end">
                        {!p.isRefunded && p.unallocatedAmount > 0.005 && unpaidItems.length > 0 && (
                          <Button
                            size="sm"
                            variant="outline"
                            className="h-7 px-2 text-xs gap-1"
                            onClick={() => setAllocateTarget(p)}
                          >
                            <ArrowRight className="size-3" />
                            Dağıt
                          </Button>
                        )}
                        {!p.isRefunded && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 px-2 text-xs gap-1 text-destructive hover:text-destructive hover:bg-destructive/10"
                            onClick={() => setRefundTarget(p)}
                          >
                            <RotateCcw className="size-3" />
                            İade
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* ── Ödeme Al Dialog ────────────────────────────────────────────── */}
      {collectOpen && (
        <CollectPaymentDialog
          patientId={patientId}
          remainingDebt={data?.totalRemaining ?? 0}
          onClose={() => setCollectOpen(false)}
          onSuccess={() => { setCollectOpen(false); onRefresh(); }}
          onSuccessManual={(payment) => {
            setCollectOpen(false);
            onRefresh();
            setAllocateTarget(payment);
          }}
        />
      )}

      {/* ── Manuel Dağıtım Dialog ──────────────────────────────────────── */}
      {allocateTarget && (
        <AllocateDialog
          payment={allocateTarget}
          items={unpaidItems}
          onClose={() => setAllocateTarget(null)}
          onSuccess={() => { setAllocateTarget(null); onRefresh(); }}
        />
      )}

      {/* ── İade Dialog ───────────────────────────────────────────────── */}
      {refundTarget && (
        <RefundDialog
          payment={refundTarget}
          onClose={() => setRefundTarget(null)}
          onSuccess={() => { setRefundTarget(null); onRefresh(); }}
        />
      )}
    </div>
  );
}

// ── Özet kart ─────────────────────────────────────────────────────────────────

function SummaryCard({
  label, value, loading, color = '', icon, subtitle,
}: {
  label: string;
  value?: number;
  loading: boolean;
  color?: string;
  icon?: React.ReactNode;
  subtitle?: string;
}) {
  return (
    <Card>
      <CardHeader className="pb-1 pt-3 px-4">
        <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
          {icon}
          {label}
        </CardTitle>
      </CardHeader>
      <CardContent className="pb-3 px-4">
        {loading ? (
          <Skeleton className="h-7 w-24" />
        ) : (
          <>
            <div className={cn('text-xl font-bold', color)}>
              {fmt(value ?? 0)}
            </div>
            {subtitle && <p className="text-xs text-muted-foreground mt-0.5">{subtitle}</p>}
          </>
        )}
      </CardContent>
    </Card>
  );
}

// ── Manuel Dağıtım Dialog ─────────────────────────────────────────────────────

function AllocateDialog({
  payment, items, onClose, onSuccess,
}: {
  payment: PatientAccountPayment;
  items: PatientAccountItem[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  // treatmentPlanItemId → entered amount string
  const [amounts, setAmounts] = useState<Record<number, string>>({});

  const isFxPayment = payment.currency !== 'TRY';

  // Is this item priced in the same currency as the payment?
  const isSameCurrency = (item: PatientAccountItem) =>
    isFxPayment && item.priceCurrency === payment.currency;

  const parsed = useMemo(() =>
    items.map(i => ({
      item: i,
      entered: parseFloat(amounts[i.treatmentPlanItemId] ?? '') || 0,
    })),
  [items, amounts]);

  const totalEntered   = parsed.reduce((s, x) => s + x.entered, 0);
  // Non-exact items (TRY or cross-currency) must stay within TRY budget
  const totalNonExact  = parsed.filter(x => !isSameCurrency(x.item)).reduce((s, x) => s + x.entered, 0);
  const regularOverBudget = totalNonExact > payment.unallocatedAmount + 0.005;
  // FX overrun: total > TRY budget but all overflow is from exact-currency items (clinic absorbs)
  const hasFxOverrun   = isFxPayment && totalEntered > payment.unallocatedAmount + 0.005 && !regularOverBudget;
  const anyOverItem    = parsed.some(x => x.entered > x.item.remainingAmount + 0.005);
  const isValid        = totalEntered > 0.005 && !regularOverBudget && !anyOverItem;

  // "Max" — for same-currency FX items: full close (no TRY budget cap); otherwise: budget-capped
  function fillMax(itemId: number, item: PatientAccountItem) {
    if (isSameCurrency(item)) {
      // Same currency → fully close the item; backend allows FX overrun
      setAmounts(prev => ({ ...prev, [itemId]: item.remainingAmount.toFixed(2) }));
    } else {
      const alreadyOthers = parsed
        .filter(x => x.item.treatmentPlanItemId !== itemId)
        .reduce((s, x) => s + x.entered, 0);
      const available = payment.unallocatedAmount - alreadyOthers;
      const val = Math.min(item.remainingAmount, Math.max(0, available));
      setAmounts(prev => ({ ...prev, [itemId]: val.toFixed(2) }));
    }
  }

  const mut = useMutation({
    mutationFn: () => patientAccountApi.allocatePayment(
      payment.publicId,
      parsed
        .filter(x => x.entered > 0.005)
        .map(x => ({ treatmentPlanItemId: x.item.treatmentPlanItemId, amount: x.entered }))
    ),
    onSuccess: () => {
      toast.success(`${fmt(totalEntered)} tedavilere dağıtıldı.`);
      onSuccess();
    },
    onError: (err: unknown) => {
      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(detail ?? 'Dağıtım yapılamadı.');
    },
  });

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ArrowRight className="size-5" />
            Ödemeyi Tedaviye Yönlendir
          </DialogTitle>
        </DialogHeader>

        {/* Ödeme özeti */}
        <div className="rounded-lg bg-muted/50 border px-4 py-3 text-sm flex items-center justify-between gap-4">
          <div className="space-y-0.5">
            <div className="font-medium">
              {payment.currency !== 'TRY'
                ? `${payment.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} ${payment.currency} ≈ ${fmt(payment.baseAmount)}`
                : fmt(payment.amount)
              }
            </div>
            <div className="text-muted-foreground text-xs">
              {format(new Date(payment.paymentDate), 'dd MMM yyyy', { locale: tr })} · {payment.methodLabel}
            </div>
          </div>
          <div className="text-right shrink-0">
            <div className="text-xs text-muted-foreground">Dağıtılabilir</div>
            <div className="font-semibold text-amber-600">{fmt(payment.unallocatedAmount)}</div>
          </div>
        </div>

        {/* Tedavi listesi */}
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-6">
            Dağıtılacak ödenmemiş tedavi kalemi yok.
          </p>
        ) : (
          <div className="border rounded-lg overflow-auto max-h-72">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/40 text-muted-foreground text-xs">
                  <th className="px-3 py-2 text-left font-medium">Tedavi</th>
                  <th className="px-3 py-2 text-left font-medium">Diş</th>
                  <th className="px-3 py-2 text-right font-medium">Hasta Payı</th>
                  <th className="px-3 py-2 text-right font-medium">Kalan Borç</th>
                  <th className="px-3 py-2 text-right font-medium">Bu Ödemeden (₺)</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {parsed.map(({ item, entered }) => {
                  const overItem = entered > item.remainingAmount + 0.005;
                  return (
                    <tr key={item.treatmentPlanItemId} className="hover:bg-muted/20">
                      <td className="px-3 py-2 font-medium max-w-[180px]">
                        <span className="truncate block">{item.treatmentName}</span>
                      </td>
                      <td className="px-3 py-2 text-muted-foreground">{item.toothNumber ?? '—'}</td>
                      <td className="px-3 py-2 text-right text-muted-foreground">
                        {fmt(item.patientAmount)}
                      </td>
                      <td className="px-3 py-2 text-right text-destructive font-semibold">
                        {fmt(item.remainingAmount)}
                      </td>
                      <td className="px-2 py-1.5">
                        <div className="flex items-center gap-1.5 justify-end">
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            max={item.remainingAmount}
                            className={cn(
                              'text-right h-7 text-xs w-28 tabular-nums',
                              overItem && 'border-destructive focus-visible:ring-destructive'
                            )}
                            placeholder={item.remainingAmount.toFixed(2)}
                            value={amounts[item.treatmentPlanItemId] ?? ''}
                            onChange={e => setAmounts(prev => ({
                              ...prev,
                              [item.treatmentPlanItemId]: e.target.value,
                            }))}
                          />
                          <button
                            type="button"
                            className="text-[10px] text-blue-500 hover:text-blue-700 whitespace-nowrap underline underline-offset-2"
                            onClick={() => fillMax(item.treatmentPlanItemId, item)}
                          >
                            Max
                          </button>
                        </div>
                        {overItem && (
                          <p className="text-[10px] text-destructive text-right mt-0.5">
                            Kalan borcunu aşıyor
                          </p>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Toplam */}
        <div className={cn(
          'flex items-center justify-between text-sm rounded-lg border px-4 py-2.5',
          regularOverBudget && 'border-destructive bg-destructive/5',
          hasFxOverrun && 'border-amber-300 bg-amber-50 dark:bg-amber-950/20'
        )}>
          <span className="text-muted-foreground">Toplam dağıtılacak</span>
          <div className="flex items-center gap-4">
            <span className={cn('font-semibold text-base tabular-nums', regularOverBudget && 'text-destructive')}>
              {fmt(totalEntered)}
            </span>
            <span className="text-muted-foreground text-xs">
              / {fmt(payment.unallocatedAmount)} mevcut
            </span>
            {!regularOverBudget && !hasFxOverrun && payment.unallocatedAmount - totalEntered > 0.005 && (
              <span className="text-xs text-amber-600">
                {fmt(payment.unallocatedAmount - totalEntered)} açıkta kalacak
              </span>
            )}
            {hasFxOverrun && (
              <span className="text-xs text-amber-700 font-medium">
                Kur farkı ({fmt(totalEntered - payment.unallocatedAmount)}) klinik üstlenir
              </span>
            )}
            {regularOverBudget && (
              <span className="text-xs text-destructive font-medium">
                {fmt(totalEntered - payment.unallocatedAmount)} fazla
              </span>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={mut.isPending}>
            Vazgeç
          </Button>
          <Button
            onClick={() => mut.mutate()}
            disabled={!isValid || mut.isPending}
            className="gap-1.5"
          >
            <ArrowRight className="size-4" />
            {mut.isPending ? 'Kaydediliyor...' : 'Dağıt'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── İade Dialog ───────────────────────────────────────────────────────────────

function RefundDialog({
  payment, onClose, onSuccess,
}: {
  payment: PatientAccountPayment;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState('');

  const hasAllocations = payment.allocatedAmount > 0.005;

  const mut = useMutation({
    mutationFn: () => patientAccountApi.refundPayment(payment.publicId, reason || undefined),
    onSuccess: () => {
      toast.success('Ödeme iade edildi.');
      onSuccess();
    },
    onError: (err: unknown) => {
      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(detail ?? 'İade yapılamadı.');
    },
  });

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-destructive">
            <RotateCcw className="size-5" />
            Ödeme İadesi
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-1">
          {/* Ödeme özeti */}
          <div className="rounded-lg border px-4 py-3 text-sm space-y-1">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Tutar</span>
              <span className="font-semibold">
                {payment.currency !== 'TRY'
                  ? `${payment.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} ${payment.currency} ≈ ${fmt(payment.baseAmount)}`
                  : fmt(payment.amount)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Tarih</span>
              <span>{format(new Date(payment.paymentDate), 'dd MMM yyyy', { locale: tr })}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Yöntem</span>
              <span>{payment.methodLabel}</span>
            </div>
          </div>

          {/* Uyarı — tedavilere dağıtıldıysa */}
          {hasAllocations && (
            <div className="rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 px-4 py-3 text-sm space-y-1">
              <p className="font-medium text-amber-800 dark:text-amber-300">
                Bu ödeme tedavilere dağıtıldı
              </p>
              <p className="text-amber-700 dark:text-amber-400 text-xs">
                {fmt(payment.allocatedAmount)} tedavilere dağıtılmış. İade edilirse bu dağıtımlar
                geri alınır ve hasta borcu yeniden açılır.
              </p>
              <p className="text-amber-700 dark:text-amber-400 text-xs font-medium">
                Hekimlerin <span className="underline">dağıtılmamış (Bekliyor)</span> hakediş
                kayıtları otomatik iptal edilir. Dağıtılmış (ödenmiş) hakediş varsa
                sistem iade işlemini reddeder — önce hekimden geri alınmalıdır.
              </p>
            </div>
          )}

          {/* İade nedeni */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">
              İade Nedeni <span className="text-muted-foreground font-normal">(isteğe bağlı)</span>
            </label>
            <Input
              placeholder="Müşteri talebi, hatalı ödeme..."
              value={reason}
              onChange={e => setReason(e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={mut.isPending}>
            Vazgeç
          </Button>
          <Button
            variant="destructive"
            onClick={() => mut.mutate()}
            disabled={mut.isPending}
            className="gap-1.5"
          >
            <RotateCcw className="size-4" />
            {mut.isPending ? 'İade ediliyor...' : 'Ödemeyi İade Et'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Ödeme Al Dialog ───────────────────────────────────────────────────────────

function CollectPaymentDialog({
  patientId, remainingDebt, onClose, onSuccess, onSuccessManual,
}: {
  patientId: number;
  remainingDebt: number;
  onClose: () => void;
  onSuccess: () => void;
  onSuccessManual: (payment: PatientAccountPayment) => void;
}) {
  const today = format(new Date(), 'yyyy-MM-dd');
  const [amount,       setAmount]       = useState(remainingDebt > 0 ? String(remainingDebt.toFixed(2)) : '');
  const [currency,     setCurrency]     = useState('TRY');
  const [exchangeRate, setExchangeRate] = useState('1');
  const [method,       setMethod]       = useState('1');
  const [date,         setDate]         = useState(today);
  const [notes,        setNotes]        = useState('');

  const isFx      = currency !== 'TRY';
  const amountNum = parseFloat(amount)       || 0;
  const rateNum   = parseFloat(exchangeRate) || 1;
  const tryEquiv  = isFx ? amountNum * rateNum : amountNum;
  const curSym    = CURRENCIES.find(c => c.value === currency)?.sym ?? currency;

  // Otomatik FIFO dağıtım
  const mutAuto = useMutation({
    mutationFn: () => patientAccountApi.collectPayment(patientId, {
      amount:       amountNum,
      method:       parseInt(method, 10),
      paymentDate:  date,
      currency,
      exchangeRate: isFx ? rateNum : 1,
      notes:        notes || undefined,
    }),
    onSuccess: (res) => {
      const r   = res.data;
      const msg = r.unallocatedAmount > 0
        ? `Ödeme alındı. ${fmt(r.totalAllocated)} dağıtıldı, ${fmt(r.unallocatedAmount)} açıkta.`
        : `Ödeme alındı. ${fmt(r.totalAllocated)} tedavilere dağıtıldı.`;
      toast.success(msg);
      onSuccess();
    },
    onError: (err: unknown) => {
      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(detail ?? 'Ödeme alınamadı.');
    },
  });

  // Manuel dağıtım — ödemeyi FIFO olmadan kaydeder, sonra dağıtım dialogu açılır
  const mutManual = useMutation({
    mutationFn: () => patientAccountApi.createPayment({
      patientId,
      amount:       amountNum,
      method:       parseInt(method, 10),
      paymentDate:  date,
      currency,
      exchangeRate: isFx ? rateNum : 1,
      notes:        notes || undefined,
    }),
    onSuccess: (res) => {
      const p = res.data;
      toast.success('Ödeme kaydedildi. Şimdi dağıtın.');
      // PatientAccountPayment şekline çevir
      const payment: PatientAccountPayment = {
        id:                p.id,
        publicId:          p.publicId,
        amount:            p.amount,
        currency:          p.currency,
        exchangeRate:      p.exchangeRate,
        baseAmount:        p.baseAmount,
        paymentDate:       p.paymentDate,
        method:            p.method,
        methodLabel:       p.methodLabel,
        notes:             p.notes,
        allocatedAmount:   0,
        unallocatedAmount: p.baseAmount,
        isRefunded:        false,
      };
      onSuccessManual(payment);
    },
    onError: (err: unknown) => {
      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(detail ?? 'Ödeme alınamadı.');
    },
  });

  const isPending = mutAuto.isPending || mutManual.isPending;
  const isValid   = amountNum > 0 && (!isFx || rateNum > 0) && !!method && !!date;

  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CreditCard className="size-5" />
            Ödeme Al
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {remainingDebt > 0 && (
            <div className="rounded-lg bg-destructive/10 border border-destructive/20 px-3 py-2 text-sm">
              <span className="text-muted-foreground">Kalan borç: </span>
              <span className="font-semibold text-destructive">{fmt(remainingDebt)}</span>
            </div>
          )}

          {/* Tutar + Para Birimi */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">Tutar</label>
            <div className="flex gap-2">
              <div className="relative flex-1">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm">
                  {curSym}
                </span>
                <Input
                  type="number"
                  step="0.01"
                  min="0.01"
                  placeholder="0,00"
                  className="pl-7"
                  value={amount}
                  onChange={e => setAmount(e.target.value)}
                  autoFocus
                />
              </div>
              <Select value={currency} onValueChange={v => {
                setCurrency(v);
                if (v === 'TRY') setExchangeRate('1');
              }}>
                <SelectTrigger className="w-28">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CURRENCIES.map(c => (
                    <SelectItem key={c.value} value={c.value}>{c.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Kur (sadece döviz seçilince) */}
          {isFx && (
            <div className="space-y-1.5">
              <label className="text-sm font-medium">
                Kur <span className="text-muted-foreground font-normal">({currency}/TRY)</span>
              </label>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  step="0.0001"
                  min="0.0001"
                  placeholder="38.50"
                  value={exchangeRate}
                  onChange={e => setExchangeRate(e.target.value)}
                  className="flex-1"
                />
                {tryEquiv > 0 && (
                  <div className="text-sm text-muted-foreground whitespace-nowrap">
                    = <span className="font-semibold text-foreground">{fmt(tryEquiv)}</span>
                  </div>
                )}
              </div>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Yöntem</label>
              <Select value={method} onValueChange={setMethod}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {PAYMENT_METHODS.map(m => (
                    <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Tarih</label>
              <Input type="date" value={date} onChange={e => setDate(e.target.value)} />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium">
              Not <span className="text-muted-foreground font-normal">(isteğe bağlı)</span>
            </label>
            <Input placeholder="Ödeme notu..." value={notes} onChange={e => setNotes(e.target.value)} />
          </div>

        </div>

        <DialogFooter className="flex-col sm:flex-row gap-2">
          <Button variant="outline" onClick={onClose} disabled={isPending} className="sm:mr-auto">
            Vazgeç
          </Button>
          {/* Manuel dağıtım — ödemeyi kaydeder, dağıtım dialogu açılır */}
          <Button
            variant="outline"
            onClick={() => mutManual.mutate()}
            disabled={!isValid || isPending}
            className="gap-1.5"
          >
            <ArrowRight className="size-4" />
            {mutManual.isPending ? 'Kaydediliyor...' : 'Kaydet ve Manuel Dağıt'}
          </Button>
          {/* Otomatik FIFO dağıtım */}
          <Button
            onClick={() => mutAuto.mutate()}
            disabled={!isValid || isPending}
            className="gap-1.5"
          >
            <CreditCard className="size-4" />
            {mutAuto.isPending ? 'Kaydediliyor...' : 'Kaydet (Otomatik)'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
