import { useState, useMemo, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { CreditCard, Wallet, AlertCircle, CheckCircle2, Clock, ArrowRight, ChevronDown, ChevronRight, RotateCcw, FileText, Building2, Download } from 'lucide-react';
import { toast } from 'sonner';
import { patientAccountApi } from '@/api/patientAccount';
import type { PatientAccountPayment, PatientAccountItem } from '@/api/patientAccount';
import { treatmentPlansApi } from '@/api/treatments';
import { exchangeRatesApi } from '@/api/exchangeRates';
import { patientInvoicesApi } from '@/api/patientInvoices';
import type { InvoiceRecipientType, PatientInvoice } from '@/api/patientInvoices';
import { settingsApi } from '@/api/settings';
import { institutionInvoicesApi } from '@/api/institutionInvoices';
import type { InstitutionInvoice, InstitutionPaymentMethod } from '@/api/institutionInvoices';
import { institutionsApi } from '@/api/institutions';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
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
import { usePermissions } from '@/hooks/usePermissions';

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

function ContribInput({
  initialValue, refValue, onSave, isPending,
}: {
  initialValue: number | null;
  refValue: number;
  onSave: (amount: number | null) => void;
  isPending?: boolean;
}) {
  const [val, setVal] = useState(initialValue != null ? String(initialValue) : '');

  useEffect(() => {
    setVal(initialValue != null ? String(initialValue) : '');
  }, [initialValue]);

  const parsed = val.trim() === '' ? null : parseFloat(val.replace(',', '.'));
  const isInvalid = parsed !== null && (isNaN(parsed) || parsed < 0);
  const isOverRef = parsed !== null && !isNaN(parsed) && parsed > refValue;

  const commit = () => {
    const trimmed = val.trim();
    if (trimmed === '') { onSave(null); return; }
    const n = parseFloat(trimmed.replace(',', '.'));
    if (isNaN(n) || n < 0) { setVal(initialValue != null ? String(initialValue) : ''); return; }
    onSave(n);
  };

  const borderClass = isInvalid
    ? 'border-destructive text-destructive'
    : isOverRef
      ? 'border-amber-400 text-amber-700'
      : parsed !== null && parsed > 0
        ? 'border-blue-300 text-blue-700'
        : 'text-muted-foreground';

  return (
    <input
      type="number"
      min={0}
      step="0.01"
      className={`w-[72px] text-right text-xs border rounded px-1.5 py-0.5 bg-background tabular-nums
        focus:outline-none focus:ring-1 focus:ring-blue-400
        ${isPending ? 'opacity-50' : ''} ${borderClass}`}
      value={val}
      onChange={e => setVal(e.target.value)}
      onBlur={commit}
      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); commit(); } }}
      onClick={e => e.stopPropagation()}
    />
  );
}

export function PatientAccountTab({ patientId, hasPassportNo = false }: { patientId: number; hasPassportNo?: boolean }) {
  const qc = useQueryClient();
  const [collectOpen,    setCollectOpen]    = useState(false);
  const [allocateTarget, setAllocateTarget] = useState<PatientAccountPayment | null>(null);
  const [refundTarget,   setRefundTarget]   = useState<PatientAccountPayment | null>(null);
  const [invoiceOpen,         setInvoiceOpen]         = useState(false);
  const [patientInvoiceOpen,  setPatientInvoiceOpen]  = useState(false);
  const [invoiceItemIds, setInvoiceItemIds] = useState<Set<number>>(new Set());
  const [expandedItems,  setExpandedItems]  = useState<Set<number>>(new Set());
  const [previewInvoice, setPreviewInvoice] = useState<
    | { type: 'patient';     data: PatientInvoice }
    | { type: 'institution'; data: InstitutionInvoice }
    | null
  >(null);
  const [payingInstInvoice, setPayingInstInvoice] = useState<InstitutionInvoice | null>(null);

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

  const { data: patientInvoicesData } = useQuery({
    queryKey: ['patient-invoices', patientId],
    queryFn: () => patientInvoicesApi.list({ patientId, pageSize: 100 }).then(r => r.data),
  });

  const { data: institutionInvoicesData } = useQuery({
    queryKey: ['institution-invoices-for-patient', patientId],
    queryFn: () => institutionInvoicesApi.list({ patientId, pageSize: 100 }).then(r => r.data),
  });

  // itemId → faturalar haritaları
  const patientInvoicesByItemId = useMemo(() => {
    const map = new Map<number, PatientInvoice[]>();
    for (const inv of patientInvoicesData?.items ?? []) {
      if (!inv.treatmentItemIdsJson) continue;
      try {
        const ids: number[] = JSON.parse(inv.treatmentItemIdsJson);
        ids.forEach(id => {
          const arr = map.get(id) ?? [];
          arr.push(inv);
          map.set(id, arr);
        });
      } catch { /* ignore */ }
    }
    return map;
  }, [patientInvoicesData]);

  const institutionInvoicesByItemId = useMemo(() => {
    const map = new Map<number, InstitutionInvoice[]>();
    for (const inv of institutionInvoicesData?.items ?? []) {
      if (!inv.treatmentItemIdsJson) continue;
      try {
        const ids: number[] = JSON.parse(inv.treatmentItemIdsJson);
        ids.forEach(id => {
          const arr = map.get(id) ?? [];
          arr.push(inv);
          map.set(id, arr);
        });
      } catch { /* ignore */ }
    }
    return map;
  }, [institutionInvoicesData]);

  const completedItems  = (data?.items ?? []).filter(i => i.status === 'Completed');
  const plannedItems    = (data?.items ?? []).filter(i => i.status !== 'Completed');
  const unpaidItems     = completedItems.filter(i => i.remainingAmount > 0.005);
  const billableItems   = completedItems.filter(i => i.totalAmountTry - i.patientAmount > 0.005);
  const hasProvisionRows = completedItems.some(i => i.institutionPaymentModel === 2);

  const onRefresh = () => {
    qc.invalidateQueries({ queryKey: ['patient-account', patientId] });
    qc.invalidateQueries({ queryKey: ['patient-invoices', patientId] });
    qc.invalidateQueries({ queryKey: ['institution-invoices-for-patient', patientId] });
  };

  const contribMutation = useMutation({
    mutationFn: ({ planId, itemId, amount }: { planId: string; itemId: string; amount: number | null }) =>
      treatmentPlansApi.setContribution(planId, itemId, amount),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['patient-account', patientId] }),
    onError: () => toast.error('Kurum katkısı kaydedilemedi.'),
  });

  return (
    <div className="space-y-4">
      {/* ── Özet kartları ─────────────────────────────────────────────── */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-6">
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
          label="Hasta Borcu"
          value={data?.totalRemaining}
          loading={isLoading}
          icon={<AlertCircle className="size-4 text-destructive" />}
          color={(data?.totalRemaining ?? 0) > 0.005 ? 'text-destructive' : 'text-green-600'}
          subtitle="kalan hasta payı"
        />
        <SummaryCard
          label="Kurum Borcu"
          value={institutionInvoicesData?.items
            .filter(i => i.status !== 'Rejected' && i.status !== 'Cancelled')
            .reduce((s, i) => s + i.remainingAmount, 0)}
          loading={isLoading}
          icon={<Building2 className="size-4 text-purple-500" />}
          color={
            (institutionInvoicesData?.items
              .filter(i => i.status !== 'Rejected' && i.status !== 'Cancelled')
              .reduce((s, i) => s + i.remainingAmount, 0) ?? 0) > 0.005
              ? 'text-purple-600'
              : 'text-green-600'
          }
          subtitle="kalan kurum payı"
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
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-1.5" onClick={() => setPatientInvoiceOpen(true)}>
            <FileText className="size-4" />
            Hastaya Fatura
          </Button>
          <Button size="sm" variant="outline" className="gap-1.5" onClick={() => setInvoiceOpen(true)}>
            <ArrowRight className="size-4" />
            Kuruma Fatura
            {invoiceItemIds.size > 0 && (
              <span className="ml-0.5 rounded-full bg-primary text-primary-foreground text-[10px] px-1.5 py-0.5 leading-none font-medium">
                {invoiceItemIds.size}
              </span>
            )}
          </Button>
          <Button size="sm" className="gap-1.5" onClick={() => setCollectOpen(true)}>
            <CreditCard className="size-4" />
            Ödeme Al
          </Button>
        </div>
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
                <TableHead className="w-8 px-3">
                  {billableItems.length > 0 && (
                    <Checkbox
                      checked={billableItems.length > 0 && billableItems.every(i => invoiceItemIds.has(i.treatmentPlanItemId))}
                      onCheckedChange={checked => {
                        if (checked) setInvoiceItemIds(new Set(billableItems.map(i => i.treatmentPlanItemId)));
                        else setInvoiceItemIds(new Set());
                      }}
                    />
                  )}
                </TableHead>
                <TableHead>Tedavi</TableHead>
                <TableHead>Diş</TableHead>
                <TableHead>Hekim</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead className="text-right">Tutar</TableHead>
                {hasProvisionRows && <TableHead className="text-right">Kurum</TableHead>}
                {hasProvisionRows && <TableHead className="text-right">Hasta Payı</TableHead>}
                <TableHead className="text-right">Ödenen</TableHead>
                <TableHead className="text-right">Kalan</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 10 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-16" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : completedItems.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={10} className="text-center text-muted-foreground py-6 text-sm">
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
                      {/* Kurum faturası checkbox */}
                      <TableCell className="w-8 px-3" onClick={e => e.stopPropagation()}>
                        {i.totalAmountTry - i.patientAmount > 0.005 && (
                          <Checkbox
                            checked={invoiceItemIds.has(i.treatmentPlanItemId)}
                            onCheckedChange={() => setInvoiceItemIds(prev => {
                              const next = new Set(prev);
                              next.has(i.treatmentPlanItemId) ? next.delete(i.treatmentPlanItemId) : next.add(i.treatmentPlanItemId);
                              return next;
                            })}
                          />
                        )}
                      </TableCell>
                      {/* Expand chevron + tedavi adı + fatura ikonları */}
                      <TableCell className="font-medium">
                        <div className="flex items-center gap-1.5">
                          {hasAllocs ? (
                            isExpanded
                              ? <ChevronDown className="size-3.5 shrink-0 text-muted-foreground" />
                              : <ChevronRight className="size-3.5 shrink-0 text-muted-foreground" />
                          ) : (
                            <span className="size-3.5 shrink-0 inline-block" />
                          )}
                          <span>{i.treatmentName}</span>
                          {/* Hasta fatura ikonları */}
                          {(patientInvoicesByItemId.get(i.treatmentPlanItemId) ?? []).map(inv => (
                            <button
                              key={inv.id}
                              type="button"
                              title={`Hasta Faturası: ${inv.invoiceNo}`}
                              className="shrink-0 text-blue-500 hover:text-blue-700"
                              onClick={e => { e.stopPropagation(); setPreviewInvoice({ type: 'patient', data: inv }); }}
                            >
                              <FileText className="size-3.5" />
                            </button>
                          ))}
                          {/* Kurum fatura ikonları */}
                          {(institutionInvoicesByItemId.get(i.treatmentPlanItemId) ?? []).map(inv => (
                            <button
                              key={inv.id}
                              type="button"
                              title={`Kurum Faturası: ${inv.invoiceNo}`}
                              className="shrink-0 text-purple-500 hover:text-purple-700"
                              onClick={e => { e.stopPropagation(); setPreviewInvoice({ type: 'institution', data: inv }); }}
                            >
                              <Building2 className="size-3.5" />
                            </button>
                          ))}
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
                      {/* Kurum payı — sadece provizyon + değer girilmiş kalemlerde input göster */}
                      {hasProvisionRows && (
                        <TableCell className="text-right">
                          {(() => {
                            if (i.institutionPaymentModel !== 2 || i.institutionContributionAmount == null)
                              return <span className="text-xs text-muted-foreground">—</span>;

                            const invs = (institutionInvoicesByItemId.get(i.treatmentPlanItemId) ?? [])
                              .filter(inv => inv.status !== 'Rejected' && inv.status !== 'Cancelled');
                            const paid     = invs.some(inv => inv.status === 'Paid');
                            const invoiced = invs.length > 0;
                            const statusBadge = paid
                              ? <div className="text-[10px] text-green-600 font-medium">Tahsil ✓</div>
                              : invoiced
                                ? <div className="text-[10px] text-purple-600">Faturalı</div>
                                : null;

                            return (
                              <div className="space-y-0.5 flex flex-col items-end">
                                <ContribInput
                                  key={`${i.itemPublicId}:${i.institutionContributionAmount}`}
                                  initialValue={i.institutionContributionAmount}
                                  refValue={i.totalAmountTry}
                                  isPending={contribMutation.isPending}
                                  onSave={(amount) => contribMutation.mutate({
                                    planId: i.planPublicId,
                                    itemId: i.itemPublicId,
                                    amount,
                                  })}
                                />
                                {statusBadge}
                              </div>
                            );
                          })()}
                        </TableCell>
                      )}
                      {/* Hasta payı */}
                      {hasProvisionRows && (
                        <TableCell className="text-right font-medium">
                          {i.institutionPaymentModel === 2 && i.institutionContributionAmount != null
                            ? fmt(i.patientAmount)
                            : <span className="text-xs text-muted-foreground">—</span>}
                        </TableCell>
                      )}
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
                          <TableCell colSpan={hasProvisionRows ? 10 : 8} className="py-0 pl-8 pr-4">
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

      {/* ── Kurum Borçları ─────────────────────────────────────────────── */}
      {(institutionInvoicesData?.items?.length ?? 0) > 0 && (
        <Card>
          <CardHeader className="py-3 px-4">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Building2 className="size-4 text-purple-500" />
              Kurum Borçları
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Fatura No</TableHead>
                  <TableHead>Kurum</TableHead>
                  <TableHead>Tarih</TableHead>
                  <TableHead>Vade</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">Net Ödenecek</TableHead>
                  <TableHead className="text-right">Tevkifat</TableHead>
                  <TableHead className="text-right">Tahsil Edilen</TableHead>
                  <TableHead className="text-right">Kalan</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {institutionInvoicesData!.items.map(inv => (
                  <TableRow
                    key={inv.id}
                    className="cursor-pointer hover:bg-muted/20"
                    onClick={() => setPreviewInvoice({ type: 'institution', data: inv })}
                  >
                    <TableCell className="font-mono text-xs">{inv.invoiceNo}</TableCell>
                    <TableCell className="text-sm font-medium">{inv.institutionName}</TableCell>
                    <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                      {format(new Date(inv.invoiceDate), 'dd MMM yyyy', { locale: tr })}
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                      {format(new Date(inv.dueDate), 'dd MMM yyyy', { locale: tr })}
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="outline"
                        className={cn(
                          'text-xs',
                          inv.status === 'Paid'          && 'border-green-300 text-green-700 bg-green-50 dark:bg-green-950/30',
                          inv.status === 'PartiallyPaid' && 'border-blue-300 text-blue-700 bg-blue-50 dark:bg-blue-950/30',
                          inv.status === 'Issued'        && 'border-amber-300 text-amber-700 bg-amber-50 dark:bg-amber-950/30',
                          (inv.status === 'Overdue' || inv.status === 'InFollowUp') && 'border-destructive text-destructive',
                          inv.status === 'Rejected'      && 'border-muted text-muted-foreground',
                          inv.status === 'Cancelled'     && 'border-muted text-muted-foreground line-through',
                        )}
                      >
                        {inv.statusLabel}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right font-medium">{fmt(inv.netPayableAmount)}</TableCell>
                    <TableCell className="text-right text-muted-foreground text-xs">
                      {inv.withholdingAmount > 0.005 ? fmt(inv.withholdingAmount) : '—'}
                    </TableCell>
                    <TableCell className="text-right text-green-600">
                      {inv.paidAmount > 0.005 ? fmt(inv.paidAmount) : '—'}
                    </TableCell>
                    <TableCell className="text-right">
                      {inv.remainingAmount > 0.005
                        ? <span className="text-destructive font-semibold">{fmt(inv.remainingAmount)}</span>
                        : <span className="text-green-600 text-sm">✓</span>}
                    </TableCell>
                    <TableCell onClick={e => e.stopPropagation()}>
                      {inv.remainingAmount > 0.005 && inv.status !== 'Rejected' && (
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-7 px-2 whitespace-nowrap"
                          onClick={() => setPayingInstInvoice(inv)}
                        >
                          Ödeme Al
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            {/* Özet satırı */}
            <div className="border-t px-4 py-2.5 flex items-center justify-between text-xs text-muted-foreground">
              <span>{institutionInvoicesData!.items.length} fatura</span>
              <div className="flex gap-5">
                <span>Net Toplam: <span className="font-medium text-foreground">
                  {fmt(institutionInvoicesData!.items.filter(i => i.status !== 'Rejected' && i.status !== 'Cancelled').reduce((s, i) => s + i.netPayableAmount, 0))}
                </span></span>
                <span>Tahsil: <span className="font-medium text-green-600">
                  {fmt(institutionInvoicesData!.items.filter(i => i.status !== 'Cancelled').reduce((s, i) => s + i.paidAmount, 0))}
                </span></span>
                <span>Kalan: <span className="font-medium text-destructive">
                  {fmt(institutionInvoicesData!.items.filter(i => i.status !== 'Rejected' && i.status !== 'Cancelled').reduce((s, i) => s + i.remainingAmount, 0))}
                </span></span>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* ── Kesilen Faturalar ──────────────────────────────────────────── */}
      <PatientInvoiceList patientId={patientId} />

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

      {/* ── Kurum Ödeme Al Dialog ─────────────────────────────────────── */}
      {payingInstInvoice && (
        <InstRegisterPaymentDialog
          invoice={payingInstInvoice}
          onClose={() => setPayingInstInvoice(null)}
          onSuccess={() => {
            setPayingInstInvoice(null);
            qc.invalidateQueries({ queryKey: ['institution-invoices-for-patient', patientId] });
          }}
        />
      )}

      {/* ── Fatura Önizleme Dialog ─────────────────────────────────────── */}
      {previewInvoice && (
        <InvoicePreviewDialog
          invoice={previewInvoice}
          onClose={() => setPreviewInvoice(null)}
          onCancelled={() => {
            setPreviewInvoice(null);
            qc.invalidateQueries({ queryKey: ['institution-invoices-for-patient', patientId] });
          }}
        />
      )}

      {/* ── Kuruma Fatura Kes Dialog ───────────────────────────────────── */}
      {invoiceOpen && (
        <CreateInvoiceDialog
          patientId={patientId}
          preselectedIds={invoiceItemIds}
          onClose={() => { setInvoiceOpen(false); setInvoiceItemIds(new Set()); }}
          onSuccess={() => { setInvoiceOpen(false); setInvoiceItemIds(new Set()); onRefresh(); }}
        />
      )}

      {/* ── Hastaya Fatura Kes Dialog ──────────────────────────────────── */}
      {patientInvoiceOpen && (
        <CreatePatientInvoiceDialog
          patientId={patientId}
          completedItems={completedItems}
          hasPassportNo={hasPassportNo}
          onClose={() => setPatientInvoiceOpen(false)}
          onSuccess={() => { setPatientInvoiceOpen(false); onRefresh(); }}
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
  const { hasPermission } = usePermissions();
  const canBackdate = hasPermission('payment.backdate');
  const today = format(new Date(), 'yyyy-MM-dd');
  const [amount,        setAmount]        = useState(remainingDebt > 0 ? String(remainingDebt.toFixed(2)) : '');
  const [amountEdited,  setAmountEdited]  = useState(false);
  const [currency,      setCurrency]      = useState('TRY');
  const [exchangeRate,  setExchangeRate]  = useState('1');
  const [rateEdited,    setRateEdited]    = useState(false);
  const [method,        setMethod]        = useState('1');
  const [date,          setDate]          = useState(today);
  const [notes,         setNotes]         = useState('');
  const [posTerminalId, setPosTerminalId] = useState<string>('');
  const [bankAccountId, setBankAccountId] = useState<string>('');

  const methodNum = parseInt(method, 10);
  const showPos  = methodNum === 2 || methodNum === 4; // Kredi Kartı veya Taksit
  const showBank = methodNum === 3;                     // Havale/EFT
  const isFx     = currency !== 'TRY';

  // Otomatik kur çekme — döviz seçilince & tarih değişince
  const { data: fxData, isFetching: fxLoading } = useQuery({
    queryKey: ['exchange-rate', currency, date],
    queryFn: () => exchangeRatesApi.getCurrent(currency, date).then(r => r.data),
    enabled: isFx,
    staleTime: 5 * 60 * 1000,
  });

  // Kur gelince: rate + tutar alanını otomatik doldur
  useEffect(() => {
    if (!isFx) {
      setExchangeRate('1');
      setRateEdited(false);
      if (!amountEdited && remainingDebt > 0) setAmount(remainingDebt.toFixed(2));
      return;
    }
    if (fxData) {
      if (!rateEdited)   setExchangeRate(fxData.rate.toFixed(4));
      if (!amountEdited && remainingDebt > 0)
        setAmount((remainingDebt / fxData.rate).toFixed(2));
    }
  }, [fxData, isFx, rateEdited, amountEdited, remainingDebt]);

  const { data: posTerminals } = useQuery({
    queryKey: ['pos-terminals'],
    queryFn: () => settingsApi.listPosTerminals().then(r => r.data),
    enabled: showPos,
  });

  const { data: bankAccounts } = useQuery({
    queryKey: ['bank-accounts'],
    queryFn: () => settingsApi.listBankAccounts().then(r => r.data),
    enabled: showBank,
  });

  const amountNum = parseFloat(amount)       || 0;
  const rateNum   = parseFloat(exchangeRate) || 1;
  const tryEquiv  = isFx ? amountNum * rateNum : amountNum;
  const curSym    = CURRENCIES.find(c => c.value === currency)?.sym ?? currency;

  // Otomatik FIFO dağıtım
  const mutAuto = useMutation({
    mutationFn: () => patientAccountApi.collectPayment(patientId, {
      amount:        amountNum,
      method:        methodNum,
      paymentDate:   date,
      currency,
      exchangeRate:  isFx ? rateNum : 1,
      notes:         notes || undefined,
      posTerminalId: showPos && posTerminalId ? posTerminalId : undefined,
      bankAccountId: showBank && bankAccountId ? bankAccountId : undefined,
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
      amount:        amountNum,
      method:        methodNum,
      paymentDate:   date,
      currency,
      exchangeRate:  isFx ? rateNum : 1,
      notes:         notes || undefined,
      posTerminalId: showPos && posTerminalId ? posTerminalId : undefined,
      bankAccountId: showBank && bankAccountId ? bankAccountId : undefined,
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
      <DialogContent className="sm:max-w-lg">
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
                  onChange={e => { setAmount(e.target.value); setAmountEdited(true); }}
                  autoFocus
                />
              </div>
              <Select value={currency} onValueChange={v => {
                setCurrency(v);
                setRateEdited(false);
                setAmountEdited(false);
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
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium">
                  Kur <span className="text-muted-foreground font-normal">({currency}/TRY)</span>
                </label>
                {fxLoading ? (
                  <span className="text-xs text-muted-foreground animate-pulse">TCMB kuru alınıyor…</span>
                ) : fxData && !rateEdited ? (
                  <span className="text-xs text-muted-foreground">TCMB · {fxData.rateDate}</span>
                ) : rateEdited ? (
                  <button
                    type="button"
                    className="text-xs text-primary underline-offset-2 hover:underline"
                    onClick={() => { setRateEdited(false); if (fxData) setExchangeRate(fxData.rate.toFixed(4)); }}
                  >
                    TCMB kuruna dön
                  </button>
                ) : null}
              </div>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  step="0.0001"
                  min="0.0001"
                  placeholder="38.50"
                  value={exchangeRate}
                  onChange={e => { setExchangeRate(e.target.value); setRateEdited(true); }}
                  className="flex-1"
                  disabled={fxLoading}
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
                <SelectTrigger>
                  <SelectValue>
                    {PAYMENT_METHODS.find(m => m.value === method)?.label ?? 'Seçin'}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {PAYMENT_METHODS.map(m => (
                    <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Tarih</label>
              <Input
                type="date"
                value={date}
                max={today}
                min={canBackdate ? undefined : today}
                onChange={e => setDate(e.target.value)}
              />
            </div>
          </div>

          {/* POS cihazı (Kredi Kartı / Taksit) */}
          {showPos && (
            <div className="space-y-1.5">
              <label className="text-sm font-medium">
                POS Cihazı <span className="text-muted-foreground font-normal">(isteğe bağlı)</span>
              </label>
              <Select value={posTerminalId} onValueChange={setPosTerminalId}>
                <SelectTrigger>
                  <SelectValue placeholder="Cihaz seçin..." />
                </SelectTrigger>
                <SelectContent>
                  {(posTerminals ?? []).filter(p => p.isActive).map(p => (
                    <SelectItem key={p.publicId} value={p.publicId}>
                      {p.name}{p.bankShortName ? ` – ${p.bankShortName}` : ''}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Banka hesabı (Havale/EFT) */}
          {showBank && (
            <div className="space-y-1.5">
              <label className="text-sm font-medium">
                Banka Hesabı <span className="text-muted-foreground font-normal">(isteğe bağlı)</span>
              </label>
              <Select value={bankAccountId} onValueChange={setBankAccountId}>
                <SelectTrigger>
                  <SelectValue placeholder="Hesap seçin..." />
                </SelectTrigger>
                <SelectContent>
                  {(bankAccounts ?? []).filter(b => b.isActive).map(b => (
                    <SelectItem key={b.publicId} value={b.publicId}>
                      {b.bankShortName ? `${b.bankShortName} – ` : ''}{b.accountName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

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

// ── Kurum Ödeme Al Dialog ────────────────────────────────────────────────────

function InstRegisterPaymentDialog({
  invoice,
  onClose,
  onSuccess,
}: {
  invoice: InstitutionInvoice;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [amount,     setAmount]     = useState(String(invoice.remainingAmount));
  const [payDate,    setPayDate]    = useState(format(new Date(), 'yyyy-MM-dd'));
  const [method,     setMethod]     = useState<InstitutionPaymentMethod>('BankTransfer');
  const [referenceNo, setReferenceNo] = useState('');
  const [bankAccountId, setBankAccountId] = useState('');

  const { data: bankAccountsRaw } = useQuery({
    queryKey: ['bank-accounts'],
    queryFn: () => settingsApi.listBankAccounts().then(r => r.data),
  });
  const bankAccounts = (Array.isArray(bankAccountsRaw) ? bankAccountsRaw : []).filter((b: { isActive: boolean }) => b.isActive);

  const mut = useMutation({
    mutationFn: () => institutionInvoicesApi.registerPayment(invoice.publicId, {
      amount: parseFloat(amount),
      paymentDate: payDate,
      method,
      referenceNo: referenceNo || undefined,
      bankAccountPublicId: bankAccountId || undefined,
    }),
    onSuccess: () => { toast.success('Ödeme kaydedildi'); onSuccess(); },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Building2 className="size-4 text-purple-500" />
            Ödeme Al — {invoice.invoiceNo}
          </DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-1">
          <div>
            <Label className="mb-1 block">
              Tutar <span className="text-muted-foreground text-xs">(kalan: {fmt(invoice.remainingAmount)})</span>
            </Label>
            <Input type="number" step="0.01" min="0" value={amount} onChange={e => setAmount(e.target.value)} />
          </div>
          <div>
            <Label className="mb-1 block">Tarih</Label>
            <Input type="date" value={payDate} onChange={e => setPayDate(e.target.value)} />
          </div>
          <div>
            <Label className="mb-1 block">Yöntem</Label>
            <Select value={method} onValueChange={v => setMethod(v as InstitutionPaymentMethod)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="BankTransfer">Havale/EFT</SelectItem>
                <SelectItem value="Check">Çek</SelectItem>
                <SelectItem value="Other">Diğer</SelectItem>
              </SelectContent>
            </Select>
          </div>
          {(method === 'BankTransfer' || method === 'Check') && bankAccounts.length > 0 && (
            <div>
              <Label className="mb-1 block">Hesap</Label>
              <Select value={bankAccountId} onValueChange={setBankAccountId}>
                <SelectTrigger>
                  <span className={bankAccountId ? 'text-sm' : 'text-muted-foreground text-sm'}>
                    {bankAccountId
                      ? (() => {
                          const acc = bankAccounts.find((b: { publicId: string }) => b.publicId === bankAccountId);
                          return acc ? `${acc.bankShortName ? acc.bankShortName + ' — ' : ''}${acc.accountName}` : bankAccountId;
                        })()
                      : 'Hesap seçin (opsiyonel)'}
                  </span>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Belirtme</SelectItem>
                  {bankAccounts.map((b: { publicId: string; bankShortName?: string; accountName: string; iban?: string }) => (
                    <SelectItem key={b.publicId} value={b.publicId}>
                      {b.bankShortName ? `${b.bankShortName} — ` : ''}{b.accountName}
                      {b.iban ? ` (${b.iban.slice(-4)})` : ''}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}
          <div>
            <Label className="mb-1 block">Referans No</Label>
            <Input value={referenceNo} onChange={e => setReferenceNo(e.target.value)} placeholder="IBAN, çek no vb." />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} disabled={!amount || mut.isPending}>
            {mut.isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Fatura Önizleme Dialog ────────────────────────────────────────────────────

function InvoicePreviewDialog({
  invoice,
  onClose,
  onCancelled,
}: {
  invoice: { type: 'patient'; data: PatientInvoice } | { type: 'institution'; data: InstitutionInvoice };
  onClose: () => void;
  onCancelled?: () => void;
}) {
  const [downloading, setDownloading] = useState(false);
  const [cancelMode, setCancelMode]   = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  async function handleDownload() {
    setDownloading(true);
    try {
      const res = invoice.type === 'patient'
        ? await patientInvoicesApi.downloadPdf(invoice.data.publicId)
        : await institutionInvoicesApi.downloadPdf(invoice.data.publicId);

      const filename = invoice.type === 'patient'
        ? `fatura-${invoice.data.invoiceNo}.pdf`
        : `kurum-fatura-${invoice.data.invoiceNo}.pdf`;

      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('PDF indirilemedi');
    } finally {
      setDownloading(false);
    }
  }

  const isPatient = invoice.type === 'patient';
  const d = invoice.data;

  // Ortak alanlar
  const invoiceNo   = d.invoiceNo;
  const invoiceDate = d.invoiceDate;
  const statusLabel = d.statusLabel;
  const currency    = d.currency;
  const paidAmount  = d.paidAmount;

  // Tip bazlı alanlar
  const recipientName = isPatient
    ? (d as PatientInvoice).recipientName
    : (d as InstitutionInvoice).institutionName;

  const recipientId = isPatient
    ? ((d as PatientInvoice).recipientTcNo
        ? `TC: ${(d as PatientInvoice).recipientTcNo}`
        : `VKN: ${(d as PatientInvoice).recipientVkn ?? '—'}`)
    : null;

  const instData = !isPatient ? (d as InstitutionInvoice) : null;

  const matrah = isPatient
    ? (d as PatientInvoice).amount
    : (d as InstitutionInvoice).amount;

  const kdvAmount = isPatient
    ? (d as PatientInvoice).kdvAmount
    : (d as InstitutionInvoice).kdvAmount;

  const total = isPatient
    ? (d as PatientInvoice).totalAmount
    : (d as InstitutionInvoice).netPayableAmount;

  const totalLabel = isPatient ? 'Toplam (KDV Dahil)' : 'Net Ödenecek';

  const withholdingAmount = !isPatient
    ? (d as InstitutionInvoice).withholdingAmount
    : undefined;

  const cancelMut = useMutation({
    mutationFn: () => institutionInvoicesApi.cancel(
      (invoice.data as InstitutionInvoice).publicId, cancelReason
    ),
    onSuccess: () => {
      toast.success('Fatura iptal edildi');
      onCancelled?.();
      onClose();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const cancellable = !isPatient &&
    !['Paid', 'PartiallyPaid', 'Cancelled'].includes((d as InstitutionInvoice).status);

  function fmtAmt(n: number) {
    return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {isPatient
              ? <FileText className="size-4 text-blue-500" />
              : <Building2 className="size-4 text-purple-500" />
            }
            {isPatient ? 'Hasta Faturası' : 'Kurum Faturası'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-1">
          {/* Fatura başlık */}
          <div className="rounded-md bg-muted/40 px-4 py-3 grid grid-cols-2 gap-2 text-sm">
            <div>
              <div className="text-xs text-muted-foreground mb-0.5">Fatura No</div>
              <div className="font-mono font-medium">{invoiceNo}</div>
            </div>
            <div>
              <div className="text-xs text-muted-foreground mb-0.5">Fatura Tarihi</div>
              <div>{invoiceDate}</div>
            </div>
            <div>
              <div className="text-xs text-muted-foreground mb-0.5">Durum</div>
              <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                statusLabel === 'Ödendi' ? 'bg-green-50 text-green-700' :
                statusLabel === 'Kısmi Ödendi' ? 'bg-amber-50 text-amber-700' :
                statusLabel === 'İptal Edildi' || statusLabel === 'Reddedildi' ? 'bg-gray-100 text-gray-500 line-through' :
                'bg-blue-50 text-blue-700'
              }`}>{statusLabel}</span>
            </div>
            <div>
              <div className="text-xs text-muted-foreground mb-0.5">Para Birimi</div>
              <div>{currency}</div>
            </div>
          </div>

          {/* Alıcı */}
          <div>
            <div className="text-xs font-medium text-muted-foreground mb-1">Alıcı</div>
            <div className="text-sm font-medium">{recipientName}</div>
            {recipientId && <div className="text-xs text-muted-foreground">{recipientId}</div>}
            {instData && (
              <div className="text-xs text-muted-foreground space-y-0.5 mt-0.5">
                {(instData.institutionTaxNumber || instData.institutionTaxOffice) && (
                  <div>
                    {instData.institutionTaxOffice && <span>{instData.institutionTaxOffice} V.D. </span>}
                    {instData.institutionTaxNumber && <span>VKN: {instData.institutionTaxNumber}</span>}
                  </div>
                )}
                {(instData.institutionAddress || instData.institutionCity) && (
                  <div>
                    {[instData.institutionAddress, instData.institutionCity].filter(Boolean).join(', ')}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Hasta (kurum faturasında) */}
          {instData && (instData.patientName || instData.patientTcNumber) && (
            <div>
              <div className="text-xs font-medium text-muted-foreground mb-1">Hasta (Açıklama)</div>
              {instData.patientTcNumber && (
                <div className="text-xs text-muted-foreground">TC: {instData.patientTcNumber}</div>
              )}
              {instData.patientName && (
                <div className="text-sm">{instData.patientName}</div>
              )}
            </div>
          )}

          {/* Tutarlar */}
          <div className="border rounded-md divide-y text-sm">
            <div className="flex justify-between px-3 py-2">
              <span className="text-muted-foreground">Matrah (KDV Hariç)</span>
              <span>{fmtAmt(matrah)}</span>
            </div>
            <div className="flex justify-between px-3 py-2">
              <span className="text-muted-foreground">KDV</span>
              <span>{fmtAmt(kdvAmount)}</span>
            </div>
            {withholdingAmount != null && withholdingAmount > 0 && (
              <div className="flex justify-between px-3 py-2 text-red-600">
                <span>Tevkifat (−)</span>
                <span>−{fmtAmt(withholdingAmount)}</span>
              </div>
            )}
            <div className="flex justify-between px-3 py-2 font-semibold bg-muted/30">
              <span>{totalLabel}</span>
              <span>{fmtAmt(total)}</span>
            </div>
            {paidAmount > 0 && (
              <div className="flex justify-between px-3 py-2 text-green-700">
                <span>Ödenen</span>
                <span>{fmtAmt(paidAmount)}</span>
              </div>
            )}
          </div>
        </div>

        {cancelMode && (
          <div className="border-t pt-3 space-y-2">
            <p className="text-sm font-medium text-destructive">Faturayı iptal et</p>
            <Input
              placeholder="İptal gerekçesi (zorunlu)"
              value={cancelReason}
              onChange={e => setCancelReason(e.target.value)}
            />
            <div className="flex gap-2 justify-end">
              <Button variant="outline" size="sm" onClick={() => { setCancelMode(false); setCancelReason(''); }}>
                Vazgeç
              </Button>
              <Button
                size="sm"
                variant="destructive"
                disabled={!cancelReason.trim() || cancelMut.isPending}
                onClick={() => cancelMut.mutate()}
              >
                {cancelMut.isPending ? 'İptal ediliyor...' : 'Evet, İptal Et'}
              </Button>
            </div>
          </div>
        )}

        <DialogFooter className="gap-2">
          {cancellable && !cancelMode && (
            <Button
              variant="ghost"
              size="sm"
              className="text-destructive hover:text-destructive mr-auto"
              onClick={() => setCancelMode(true)}
            >
              Faturayı İptal Et
            </Button>
          )}
          <Button variant="outline" onClick={onClose}>Kapat</Button>
          <Button onClick={handleDownload} disabled={downloading} className="gap-1.5">
            <Download className="size-4" />
            {downloading ? 'İndiriliyor...' : 'PDF İndir'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Kesilen Faturalar Listesi ─────────────────────────────────────────────────

function PatientInvoiceList({ patientId }: { patientId: number }) {
  const [downloading, setDownloading] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['patient-invoices', patientId],
    queryFn: () => patientInvoicesApi.list({ patientId, pageSize: 50 }).then(r => r.data),
  });

  const invoices = data?.items ?? [];

  if (!isLoading && invoices.length === 0) return null;

  async function handleDownload(publicId: string, invoiceNo: string) {
    setDownloading(publicId);
    try {
      const res = await patientInvoicesApi.downloadPdf(publicId);
      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url;
      a.download = `fatura-${invoiceNo}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('PDF oluşturulurken hata oluştu');
    } finally {
      setDownloading(null);
    }
  }

  const statusColor = (s: string) => ({
    Issued: 'text-blue-600 bg-blue-50',
    Paid: 'text-green-600 bg-green-50',
    PartiallyPaid: 'text-amber-600 bg-amber-50',
    Cancelled: 'text-gray-400 bg-gray-50',
  }[s] ?? 'text-muted-foreground bg-muted');

  return (
    <Card>
      <CardHeader className="py-3 px-4">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <FileText className="size-4 text-blue-500" />
          Kesilen Faturalar
        </CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Fatura No</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead>Alıcı</TableHead>
              <TableHead className="text-right">Tutar</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="w-[100px]" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 2 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-20" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              invoices.map(inv => (
                <TableRow key={inv.id}>
                  <TableCell className="font-mono text-xs">{inv.invoiceNo}</TableCell>
                  <TableCell className="text-sm">{inv.invoiceDate}</TableCell>
                  <TableCell className="text-sm">{inv.recipientName}</TableCell>
                  <TableCell className="text-right font-medium">
                    {fmt(inv.totalAmount)}
                  </TableCell>
                  <TableCell>
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${statusColor(inv.status)}`}>
                      {inv.statusLabel}
                    </span>
                  </TableCell>
                  <TableCell>
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 px-2 text-xs gap-1"
                      disabled={downloading === inv.publicId}
                      onClick={() => handleDownload(inv.publicId, inv.invoiceNo)}
                    >
                      <FileText className="size-3" />
                      {downloading === inv.publicId ? '...' : 'PDF'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

// ── Hastaya Fatura Kes Dialog ─────────────────────────────────────────────────

function CreatePatientInvoiceDialog({
  patientId, completedItems, hasPassportNo = false, onClose, onSuccess,
}: {
  patientId: number;
  completedItems: PatientAccountItem[];
  hasPassportNo?: boolean;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [recipientType, setRecipientType] = useState<InvoiceRecipientType>('IndividualTc');
  const [recipientName, setRecipientName] = useState('');
  const [recipientTcNo, setRecipientTcNo] = useState('');
  const [recipientVkn,  setRecipientVkn]  = useState('');
  const [recipientTaxOffice, setRecipientTaxOffice] = useState('');
  const [invoiceType,   setInvoiceType]   = useState('EARCHIVE');
  const [selectedIds,   setSelectedIds]   = useState<Set<number>>(new Set());
  const [invoiceNo,     setInvoiceNo]     = useState('');
  const [invoiceDate,   setInvoiceDate]   = useState(() => new Date().toISOString().slice(0, 10));
  const [dueDate,       setDueDate]       = useState(() => {
    const d = new Date(); d.setDate(d.getDate() + 30);
    return d.toISOString().slice(0, 10);
  });
  // Pasaport numarası kayıtlı yabancı uyruklu hastalara sıfır KDV uygulanır
  const [kdvRate, setKdvRate] = useState(hasPassportNo ? '0' : '10');
  const [notes,   setNotes]   = useState('');

  const billableItems = completedItems.filter(i => i.patientAmount > 0.005);

  const { data: nextNumber } = useQuery({
    queryKey: ['patient-invoices-next-number', invoiceType],
    queryFn: () => patientInvoicesApi.nextNumber(undefined, invoiceType).then(r => r.data.number),
    staleTime: 0,
    gcTime: 0,
    retry: false,
  });
  useEffect(() => {
    if (nextNumber && !invoiceNo) setInvoiceNo(nextNumber);
  }, [nextNumber]); // eslint-disable-line react-hooks/exhaustive-deps

  const kdvRateNum = (parseFloat(kdvRate) || 0) / 100;
  const selectedItems = billableItems.filter(i => selectedIds.has(i.treatmentPlanItemId));
  // patientAmount = KDV dahil; matrahı içinden çıkarıyoruz
  const grossTotal = selectedItems.reduce((s, i) => s + i.patientAmount, 0);
  const matrah     = Math.round(grossTotal / (1 + kdvRateNum) * 100) / 100;
  const kdvAmount  = Math.round((grossTotal - matrah) * 100) / 100;

  function toggleItem(id: number) {
    setSelectedIds(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  function toggleAll() {
    const allIds = billableItems.map(i => i.treatmentPlanItemId);
    const allSelected = allIds.every(id => selectedIds.has(id));
    setSelectedIds(allSelected ? new Set() : new Set(allIds));
  }

  const mutation = useMutation({
    mutationFn: () => patientInvoicesApi.create({
      patientId,
      invoiceNo: invoiceNo.trim(),
      invoiceType,
      invoiceDate,
      dueDate,
      amount: grossTotal, // KDV dahil; backend içinde matrahı hesaplar
      kdvRate: kdvRateNum,
      currency: 'TRY',
      recipientType,
      recipientName: recipientName.trim(),
      recipientTcNo:     recipientType === 'IndividualTc' ? recipientTcNo.trim() : undefined,
      recipientVkn:      recipientType === 'CompanyVkn'   ? recipientVkn.trim()  : undefined,
      recipientTaxOffice: recipientType === 'CompanyVkn'  ? recipientTaxOffice.trim() || undefined : undefined,
      treatmentItemIds:  selectedIds.size > 0 ? [...selectedIds] : undefined,
      notes: notes.trim() || undefined,
    }),
    onSuccess: async (res) => {
      onSuccess();
      // Otomatik PDF indir
      try {
        const pdf = await patientInvoicesApi.downloadPdf(res.data.publicId);
        const url = URL.createObjectURL(new Blob([pdf.data], { type: 'application/pdf' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = `fatura-${res.data.invoiceNo}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        toast.success('Hasta faturası oluşturuldu ve indirildi');
      } catch {
        toast.success('Hasta faturası oluşturuldu');
      }
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Fatura oluşturulamadı');
    },
  });

  const canSubmit =
    invoiceNo.trim().length > 0
    && recipientName.trim().length > 0
    && (recipientType === 'IndividualTc' ? recipientTcNo.trim().length > 0 : recipientVkn.trim().length > 0)
    && grossTotal > 0;

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FileText className="size-4" />
            Hastaya Fatura Kes
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-1">
          {/* Fatura tipi + No */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Fatura Tipi</Label>
              <Select value={invoiceType} onValueChange={v => { setInvoiceType(v); setInvoiceNo(''); }}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="EARCHIVE">e-Arşiv</SelectItem>
                  <SelectItem value="EINVOICE">e-Fatura</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Fatura No <span className="text-destructive">*</span></Label>
              <Input
                value={invoiceNo}
                onChange={e => setInvoiceNo(e.target.value)}
                placeholder="Yükleniyor..."
                className="font-mono"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Fatura Tarihi</Label>
              <Input type="date" value={invoiceDate} onChange={e => setInvoiceDate(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label>Vade Tarihi</Label>
              <Input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} />
            </div>
          </div>

          {/* Alıcı bilgileri */}
          <div className="rounded-lg border p-4 space-y-3">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium">Alıcı Bilgileri</Label>
              <div className="flex rounded-md border overflow-hidden text-xs">
                <button
                  type="button"
                  className={cn('px-3 py-1.5 transition-colors', recipientType === 'IndividualTc' ? 'bg-primary text-primary-foreground' : 'hover:bg-muted')}
                  onClick={() => setRecipientType('IndividualTc')}
                >
                  TC Kimlik
                </button>
                <button
                  type="button"
                  className={cn('px-3 py-1.5 border-l transition-colors', recipientType === 'CompanyVkn' ? 'bg-primary text-primary-foreground' : 'hover:bg-muted')}
                  onClick={() => setRecipientType('CompanyVkn')}
                >
                  Şirket (VKN)
                </button>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label className="text-xs">{recipientType === 'IndividualTc' ? 'Ad Soyad' : 'Şirket Adı'} <span className="text-destructive">*</span></Label>
                <Input
                  value={recipientName}
                  onChange={e => setRecipientName(e.target.value)}
                  placeholder={recipientType === 'IndividualTc' ? 'Ad Soyad...' : 'Şirket Adı...'}
                />
              </div>
              {recipientType === 'IndividualTc' ? (
                <div className="space-y-1.5">
                  <Label className="text-xs">TC Kimlik No <span className="text-destructive">*</span></Label>
                  <Input
                    value={recipientTcNo}
                    onChange={e => setRecipientTcNo(e.target.value)}
                    placeholder="11 haneli TC kimlik..."
                    maxLength={11}
                    className="font-mono"
                  />
                </div>
              ) : (
                <>
                  <div className="space-y-1.5">
                    <Label className="text-xs">VKN <span className="text-destructive">*</span></Label>
                    <Input
                      value={recipientVkn}
                      onChange={e => setRecipientVkn(e.target.value)}
                      placeholder="10 haneli VKN..."
                      maxLength={11}
                      className="font-mono"
                    />
                  </div>
                  <div className="space-y-1.5 col-span-2">
                    <Label className="text-xs">Vergi Dairesi</Label>
                    <Input
                      value={recipientTaxOffice}
                      onChange={e => setRecipientTaxOffice(e.target.value)}
                      placeholder="Vergi dairesi adı..."
                    />
                  </div>
                </>
              )}
            </div>
          </div>

          {/* KDV oranı */}
          <div className="space-y-1.5">
            <Label className="text-sm font-medium">KDV Oranı (%)</Label>
            {hasPassportNo && (
              <p className="text-xs text-emerald-600 bg-emerald-50 border border-emerald-200 rounded px-2 py-1">
                Yabancı uyruklu hasta (pasaport kayıtlı) — KDV %0 uygulanır.
              </p>
            )}
            <div className="flex gap-2">
              {['0', '10', '20'].map(v => (
                <button
                  key={v}
                  type="button"
                  className={cn(
                    'px-4 py-1.5 rounded-md border text-sm transition-colors',
                    kdvRate === v ? 'bg-primary text-primary-foreground border-primary' : 'hover:bg-muted'
                  )}
                  onClick={() => setKdvRate(v)}
                >
                  %{v}
                </button>
              ))}
              <Input
                type="number"
                min="0"
                max="100"
                step="1"
                value={kdvRate}
                onChange={e => setKdvRate(e.target.value)}
                className="w-20 font-mono"
                placeholder="%"
              />
            </div>
          </div>

          {/* Tedavi kalemleri */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium">Faturaya Eklenecek Tedaviler</Label>
              {billableItems.length > 0 && (
                <button
                  type="button"
                  className="text-xs text-primary hover:underline"
                  onClick={toggleAll}
                >
                  {billableItems.every(i => selectedIds.has(i.treatmentPlanItemId)) ? 'Tümünü kaldır' : 'Tümünü seç'}
                </button>
              )}
            </div>

            {billableItems.length === 0 ? (
              <div className="rounded-md border bg-muted/40 px-4 py-6 text-center text-sm text-muted-foreground">
                Faturaya girebilecek tamamlanmış hasta payı kalem bulunamadı.
              </div>
            ) : (
              <div className="rounded-md border divide-y">
                {billableItems.map(item => (
                  <label
                    key={item.treatmentPlanItemId}
                    className={cn(
                      'flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-muted/40 transition-colors',
                      selectedIds.has(item.treatmentPlanItemId) && 'bg-primary/5'
                    )}
                  >
                    <Checkbox
                      checked={selectedIds.has(item.treatmentPlanItemId)}
                      onCheckedChange={() => toggleItem(item.treatmentPlanItemId)}
                    />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">
                        {item.treatmentName}
                        {item.toothNumber && <span className="ml-1.5 text-xs text-muted-foreground">Diş {item.toothNumber}</span>}
                      </p>
                      {item.completedAt && (
                        <p className="text-xs text-muted-foreground">
                          {format(new Date(item.completedAt), 'd MMM yyyy', { locale: tr })}
                        </p>
                      )}
                    </div>
                    <div className="text-right shrink-0">
                      <p className="text-sm font-semibold">{fmt(item.patientAmount)}</p>
                      <p className="text-xs text-muted-foreground">hasta payı</p>
                    </div>
                  </label>
                ))}
              </div>
            )}
          </div>

          {/* Notlar */}
          <div className="space-y-1.5">
            <Label>Not</Label>
            <Input value={notes} onChange={e => setNotes(e.target.value)} placeholder="Opsiyonel fatura notu..." />
          </div>
        </div>

        {/* Toplam */}
        {selectedIds.size > 0 && (
          <div className="border-t pt-3 space-y-1 text-sm">
            <div className="flex justify-between text-muted-foreground">
              <span>Matrah (KDV hariç, {selectedIds.size} kalem)</span>
              <span>{fmt(matrah)}</span>
            </div>
            {kdvRateNum > 0 && (
              <div className="flex justify-between text-muted-foreground">
                <span>KDV (%{kdvRate})</span>
                <span>{fmt(kdvAmount)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-base pt-1 border-t">
              <span>Toplam (KDV dahil)</span>
              <span>{fmt(grossTotal)}</span>
            </div>
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={!canSubmit || mutation.isPending}>
            {mutation.isPending ? 'Oluşturuluyor...' : 'Fatura Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Kuruma Fatura Kes Dialog ──────────────────────────────────────────────────

function CreateInvoiceDialog({
  patientId, preselectedIds = new Set(), onClose, onSuccess,
}: {
  patientId: number;
  preselectedIds?: Set<number>;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [institutionId, setInstitutionId] = useState<number | null>(null);
  const [selectedIds,   setSelectedIds]   = useState<Set<number>>(new Set());
  const [invoiceNo,     setInvoiceNo]     = useState('');
  const [invoiceDate,   setInvoiceDate]   = useState(() => new Date().toISOString().slice(0, 10));
  const [dueDate,       setDueDate]       = useState(() => {
    const d = new Date(); d.setDate(d.getDate() + 30);
    return d.toISOString().slice(0, 10);
  });
  const [notes, setNotes] = useState('');

  const { data: institutions } = useQuery({
    queryKey: ['institutions'],
    queryFn: () => institutionsApi.list().then(r => r.data),
  });
  const provisionInstitutions = (Array.isArray(institutions) ? institutions : [])
    .filter(i => i.isActive && (i.paymentModel === 'Provision' || (i.paymentModel as unknown as number) === 2));

  const { data: billableItems, isLoading: itemsLoading } = useQuery({
    queryKey: ['billable-items', patientId, institutionId],
    queryFn: () => institutionInvoicesApi.getBillableItems(patientId, institutionId!).then(r => r.data),
    enabled: institutionId != null,
  });

  // branchId: şube kullanıcıları için backend token'dan çözer (undefined geçmek yeterli).
  // Şirket düzeyindeki kullanıcılar için kalem verilerinden alır.
  const itemsBranchId = billableItems?.[0]?.branchId;
  const { data: nextNumber } = useQuery({
    queryKey: ['institution-invoices-next-number', itemsBranchId],
    queryFn: () => institutionInvoicesApi.nextNumber(itemsBranchId).then(r => r.data.number),
    enabled: itemsBranchId != null,
    staleTime: 0,
    gcTime: 0,
    retry: false,
  });
  useEffect(() => {
    if (nextNumber && !invoiceNo) setInvoiceNo(nextNumber);
  }, [nextNumber]); // eslint-disable-line react-hooks/exhaustive-deps

  // Dışarıdan seçili gelen kalemler varsa otomatik işaretle
  useEffect(() => {
    if (!billableItems?.length) return;
    if (preselectedIds.size > 0) {
      const matching = billableItems.filter(item => preselectedIds.has(item.id)).map(i => i.id);
      if (matching.length > 0) setSelectedIds(new Set(matching));
    }
  }, [billableItems]); // eslint-disable-line react-hooks/exhaustive-deps

  // institutionAmount = KDV dahil brüt tutar; backend matrahı içinden hesaplar
  const grossInstitutionAmount = (billableItems ?? [])
    .filter(i => selectedIds.has(i.id))
    .reduce((sum, i) => sum + i.institutionAmount, 0);
  const institutionMatrah = Math.round(grossInstitutionAmount / 1.10 * 100) / 100;
  const institutionKdv    = Math.round((grossInstitutionAmount - institutionMatrah) * 100) / 100;

  function toggleItem(id: number) {
    setSelectedIds(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  function toggleAll() {
    if (!billableItems) return;
    const allIds = billableItems.map(i => i.id);
    const allSelected = allIds.every(id => selectedIds.has(id));
    setSelectedIds(allSelected ? new Set() : new Set(allIds));
  }

  const institution = provisionInstitutions.find(i => i.id === institutionId);

  const mutation = useMutation({
    mutationFn: () => institutionInvoicesApi.create({
      patientId,
      institutionId: institutionId!,
      invoiceNo: invoiceNo.trim(),
      invoiceDate,
      dueDate,
      amount: grossInstitutionAmount, // KDV dahil; backend matrahı hesaplar
      currency: 'TRY',
      treatmentItemIds: [...selectedIds],
      notes: notes.trim() || undefined,
    }),
    onSuccess: () => { toast.success('Fatura oluşturuldu'); onSuccess(); },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Fatura oluşturulamadı');
    },
  });

  const canSubmit = institutionId != null
    && invoiceNo.trim().length > 0
    && selectedIds.size > 0
    && grossInstitutionAmount > 0;

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ArrowRight className="size-4" />
            Kuruma Fatura Kes
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-1">
          {/* Kurum seçimi */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Kurum <span className="text-destructive">*</span></Label>
              <Select
                value={institutionId?.toString() ?? ''}
                onValueChange={v => {
                  const newId = Number(v);
                  if (newId !== institutionId) {
                    setInstitutionId(newId);
                    setSelectedIds(new Set());
                  }
                }}
              >
                <SelectTrigger>
                  <span className={institutionId == null ? 'text-muted-foreground text-sm' : 'text-sm'}>
                    {institutionId != null
                      ? (provisionInstitutions.find(i => i.id === institutionId)?.name ?? String(institutionId))
                      : 'Provizyon kurumu seçin...'}
                  </span>
                </SelectTrigger>
                <SelectContent>
                  {provisionInstitutions.map(i => (
                    <SelectItem key={i.id} value={String(i.id)}>{i.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Fatura No <span className="text-destructive">*</span></Label>
              <Input
                value={invoiceNo}
                onChange={e => setInvoiceNo(e.target.value)}
                placeholder={institutionId == null ? 'Kurum seçince otomatik dolacak' : itemsBranchId == null ? 'Yükleniyor...' : 'Fatura No'}
                className="font-mono"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Fatura Tarihi</Label>
              <Input type="date" value={invoiceDate} onChange={e => setInvoiceDate(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label>Vade Tarihi</Label>
              <Input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} />
            </div>
          </div>

          {/* Tevkifat uyarısı */}
          {institution?.withholdingApplies && (
            <div className="rounded-md bg-orange-50 border border-orange-200 px-3 py-2 text-xs text-orange-800 dark:bg-orange-950/30 dark:border-orange-800 dark:text-orange-300">
              Bu kuruma <strong>tevkifatlı fatura</strong> kesilecek.
              KDV'nin {institution.withholdingNumerator}/{institution.withholdingDenominator}'i kurum tarafından vergi dairesine ödenir.
              {institution.withholdingCode && <> Tevkifat kodu: <strong>{institution.withholdingCode}</strong></>}
            </div>
          )}
          {institution?.isEInvoiceTaxpayer && (
            <div className="rounded-md bg-emerald-50 border border-emerald-200 px-3 py-2 text-xs text-emerald-800 dark:bg-emerald-950/30 dark:border-emerald-800 dark:text-emerald-300">
              Bu kurum <strong>e-fatura mükellefidir</strong>. E-fatura olarak kesilmesi gerekmektedir.
            </div>
          )}

          {/* Tedavi kalemleri */}
          {institutionId != null && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-sm font-medium">Faturaya Eklenecek Tedaviler</Label>
                {billableItems && billableItems.length > 0 && (
                  <button
                    type="button"
                    className="text-xs text-primary hover:underline"
                    onClick={toggleAll}
                  >
                    {billableItems.every(i => selectedIds.has(i.id)) ? 'Tümünü kaldır' : 'Tümünü seç'}
                  </button>
                )}
              </div>

              {itemsLoading ? (
                <div className="space-y-2">
                  {[1,2,3].map(i => <div key={i} className="h-10 rounded-md bg-muted animate-pulse" />)}
                </div>
              ) : !billableItems?.length ? (
                <div className="rounded-md border bg-muted/40 px-4 py-6 text-center text-sm text-muted-foreground">
                  Bu kuruma ait faturaya girebilecek tamamlanmış tedavi bulunamadı.
                </div>
              ) : (
                <div className="rounded-md border divide-y">
                  {billableItems.map(item => (
                    <label
                      key={item.id}
                      className={cn(
                        'flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-muted/40 transition-colors',
                        selectedIds.has(item.id) && 'bg-primary/5'
                      )}
                    >
                      <Checkbox
                        checked={selectedIds.has(item.id)}
                        onCheckedChange={() => toggleItem(item.id)}
                      />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">
                          {item.treatmentName}
                          {item.toothNumber && <span className="ml-1.5 text-xs text-muted-foreground">Diş {item.toothNumber}</span>}
                        </p>
                        {item.completedAt && (
                          <p className="text-xs text-muted-foreground">
                            {format(new Date(item.completedAt), 'd MMM yyyy', { locale: tr })}
                          </p>
                        )}
                      </div>
                      <span className="text-sm font-semibold shrink-0">
                        {fmt(item.institutionAmount)}
                      </span>
                    </label>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Notlar */}
          <div className="space-y-1.5">
            <Label>Not</Label>
            <Input
              value={notes}
              onChange={e => setNotes(e.target.value)}
              placeholder="Opsiyonel fatura notu..."
            />
          </div>
        </div>

        {/* Toplam */}
        {selectedIds.size > 0 && (
          <div className="border-t pt-3 space-y-1 text-sm">
            <div className="flex justify-between text-muted-foreground">
              <span>Matrah (KDV hariç, {selectedIds.size} kalem)</span>
              <span>{fmt(institutionMatrah)}</span>
            </div>
            <div className="flex justify-between text-muted-foreground">
              <span>KDV (%10)</span>
              <span>{fmt(institutionKdv)}</span>
            </div>
            {institution?.withholdingApplies && (
              <div className="flex justify-between text-orange-600">
                <span>Tevkifat ({institution.withholdingNumerator}/{institution.withholdingDenominator} KDV)</span>
                <span>-{fmt(Math.round(institutionKdv * institution.withholdingNumerator / institution.withholdingDenominator * 100) / 100)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-base pt-1 border-t">
              <span>Toplam (KDV dahil)</span>
              <span>{fmt(grossInstitutionAmount)}</span>
            </div>
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={!canSubmit || mutation.isPending}>
            {mutation.isPending ? 'Oluşturuluyor...' : 'Fatura Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
