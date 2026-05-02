import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  Calendar, CheckCircle2, Clock, Lock, LockOpen,
  ChevronLeft, ChevronRight, Banknote, CreditCard,
  Building2, Hash, AlertCircle, PiggyBank,
} from 'lucide-react';
import {
  cashReportApi,
  type CashMethodTotal, type CashCurrencyTotal,
  type PosTotalLine, type BankTotalLine, type KasaSection,
  type CashReportState,
} from '@/api/cashReport';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog';
import { toast } from 'sonner';
import { usePermissions } from '@/hooks/usePermissions';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';

// ─── Constants ────────────────────────────────────────────────────────────

// Sütun sırası: ödeme yöntemleri (method id → başlık)
const METHODS: Array<{ id: number; label: string; icon: React.ReactNode }> = [
  { id: 1, label: 'Nakit',      icon: <Banknote className="h-3 w-3" /> },
  { id: 2, label: 'Kredi Kartı', icon: <CreditCard className="h-3 w-3" /> },
  { id: 3, label: 'Havale/EFT', icon: <Building2 className="h-3 w-3" /> },
  { id: 4, label: 'Taksit',     icon: <Hash className="h-3 w-3" /> },
  { id: 5, label: 'Çek',        icon: <Hash className="h-3 w-3" /> },
];

// Satır sırası: para birimleri
const CURRENCIES = ['TRY', 'EUR', 'USD', 'GBP'];

// ─── Helpers ──────────────────────────────────────────────────────────────

function fmtTry(v: number) {
  return v.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
}

function fmtAmt(v: number, currency: string) {
  if (currency === 'TRY') return fmtTry(v);
  const sym: Record<string, string> = { EUR: '€', USD: '$', GBP: '£' };
  return v.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + (sym[currency] ?? currency);
}

function fmtTime(dt: string) {
  return new Date(dt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

// ─── Status badge ─────────────────────────────────────────────────────────

function StatusBadge({ status, label }: { status: number; label: string }) {
  if (status === 1)
    return <Badge variant="secondary" className="gap-1"><Clock className="h-3 w-3" />{label}</Badge>;
  if (status === 2)
    return <Badge variant="outline" className="gap-1 border-amber-300 text-amber-700 bg-amber-50"><Lock className="h-3 w-3" />{label}</Badge>;
  return <Badge className="gap-1 bg-green-600 hover:bg-green-600"><CheckCircle2 className="h-3 w-3" />{label}</Badge>;
}

// ─── Para birimi × yöntem matrisi ────────────────────────────────────────

function CashMatrix({ byMethod }: { byMethod: CashMethodTotal[] }) {
  // byMethod[method].byCurrency[currency] → amount (orijinal) ve baseTry
  const methodMap = new Map(byMethod.map(m => [m.method, m]));

  // Aktif olan para birimleri (gerçek veri olan)
  const activeCurrencies = CURRENCIES.filter(cur =>
    byMethod.some(m => m.byCurrency.some(c => c.currency === cur))
  );
  if (activeCurrencies.length === 0) return null;

  function getCellAmount(methodId: number, currency: string): number {
    const m = methodMap.get(methodId);
    if (!m) return 0;
    const c = m.byCurrency.find(x => x.currency === currency);
    return c ? c.amount : 0;
  }

  function getCellBaseTry(methodId: number, currency: string): number {
    const m = methodMap.get(methodId);
    if (!m) return 0;
    const c = m.byCurrency.find(x => x.currency === currency);
    return c ? c.baseTry : 0;
  }

  function getRowTotal(currency: string): number {
    return METHODS.reduce((sum, m) => sum + getCellBaseTry(m.id, currency), 0);
  }

  function getColTotal(methodId: number): number {
    const m = methodMap.get(methodId);
    return m ? m.totalTry : 0;
  }

  const grandTotal = byMethod.reduce((s, m) => s + m.totalTry, 0);

  return (
    <div className="rounded-lg border overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/50">
            <TableHead className="font-semibold min-w-[80px]">Para Birimi</TableHead>
            {METHODS.map(m => (
              <TableHead key={m.id} className="text-center min-w-[110px]">
                <div className="flex items-center justify-center gap-1">
                  {m.icon}
                  <span className="text-xs">{m.label}</span>
                </div>
              </TableHead>
            ))}
            <TableHead className="text-right font-semibold min-w-[110px]">Toplam</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {activeCurrencies.map(cur => (
            <TableRow key={cur}>
              <TableCell className="font-medium">{cur}</TableCell>
              {METHODS.map(m => {
                const amt = getCellAmount(m.id, cur);
                const tryVal = getCellBaseTry(m.id, cur);
                return (
                  <TableCell key={m.id} className="text-center">
                    {amt > 0 ? (
                      <div>
                        <div className="font-semibold text-sm">{fmtAmt(amt, cur)}</div>
                        {cur !== 'TRY' && (
                          <div className="text-xs text-muted-foreground">{fmtTry(tryVal)}</div>
                        )}
                      </div>
                    ) : (
                      <span className="text-muted-foreground text-xs">—</span>
                    )}
                  </TableCell>
                );
              })}
              <TableCell className="text-right font-bold">
                {getRowTotal(cur) > 0 ? fmtTry(getRowTotal(cur)) : '—'}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
        {/* Toplam satırı */}
        <tfoot>
          <tr className="border-t-2 bg-muted/30">
            <td className="px-4 py-2 font-bold text-sm">Toplam</td>
            {METHODS.map(m => (
              <td key={m.id} className="px-4 py-2 text-center font-bold text-sm">
                {getColTotal(m.id) > 0 ? fmtTry(getColTotal(m.id)) : '—'}
              </td>
            ))}
            <td className="px-4 py-2 text-right font-bold text-base text-primary">
              {fmtTry(grandTotal)}
            </td>
          </tr>
        </tfoot>
      </Table>
    </div>
  );
}

// ─── POS toplamı ──────────────────────────────────────────────────────────

function PosTotalsCard({ posTotals }: { posTotals: PosTotalLine[] }) {
  if (posTotals.length === 0) return null;
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <CreditCard className="h-4 w-4" />
          POS Toplamı
        </CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/30">
              <TableHead>Cihaz</TableHead>
              <TableHead>Banka</TableHead>
              <TableHead className="text-right">İşlem</TableHead>
              <TableHead className="text-right">Toplam</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {posTotals.map((pos, i) => (
              <TableRow key={i}>
                <TableCell className="font-medium text-sm">{pos.terminalName}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{pos.bankName}</TableCell>
                <TableCell className="text-right text-sm">{pos.count}</TableCell>
                <TableCell className="text-right font-semibold">{fmtTry(pos.totalTry)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
          {posTotals.length > 1 && (
            <tfoot>
              <tr className="border-t bg-muted/20">
                <td colSpan={2} className="px-4 py-2 font-bold text-sm">Toplam POS</td>
                <td className="px-4 py-2 text-right font-bold text-sm">
                  {posTotals.reduce((s, p) => s + p.count, 0)}
                </td>
                <td className="px-4 py-2 text-right font-bold">
                  {fmtTry(posTotals.reduce((s, p) => s + p.totalTry, 0))}
                </td>
              </tr>
            </tfoot>
          )}
        </Table>
      </CardContent>
    </Card>
  );
}

// ─── Bankaya yatan ────────────────────────────────────────────────────────

function BankTotalsCard({ bankTotals }: { bankTotals: BankTotalLine[] }) {
  if (bankTotals.length === 0) return null;
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Building2 className="h-4 w-4" />
          Toplam Bankaya Yatan
        </CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/30">
              <TableHead>Hesap</TableHead>
              <TableHead>Banka</TableHead>
              <TableHead className="text-right">İşlem</TableHead>
              <TableHead className="text-right">Toplam</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {bankTotals.map((bank, i) => (
              <TableRow key={i}>
                <TableCell className="font-medium text-sm">{bank.accountName}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{bank.bankName}</TableCell>
                <TableCell className="text-right text-sm">{bank.count}</TableCell>
                <TableCell className="text-right font-semibold">{fmtTry(bank.totalTry)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
          {bankTotals.length > 1 && (
            <tfoot>
              <tr className="border-t bg-muted/20">
                <td colSpan={2} className="px-4 py-2 font-bold text-sm">Toplam Havale</td>
                <td className="px-4 py-2 text-right font-bold text-sm">
                  {bankTotals.reduce((s, b) => s + b.count, 0)}
                </td>
                <td className="px-4 py-2 text-right font-bold">
                  {fmtTry(bankTotals.reduce((s, b) => s + b.totalTry, 0))}
                </td>
              </tr>
            </tfoot>
          )}
        </Table>
      </CardContent>
    </Card>
  );
}

// ─── Kasa bölümü ──────────────────────────────────────────────────────────

function CurrencyLines({ items }: { items: CashCurrencyTotal[] }) {
  if (items.length === 0)
    return <span className="text-muted-foreground text-sm">—</span>;
  return (
    <div className="space-y-0.5">
      {items.map(c => (
        <div key={c.currency} className="flex justify-between text-sm">
          <span className="text-muted-foreground">{c.currency}</span>
          <span className="font-medium">{fmtAmt(c.amount, c.currency)}</span>
        </div>
      ))}
    </div>
  );
}

function KasaCard({ kasa }: { kasa: KasaSection }) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <PiggyBank className="h-4 w-4" />
          Kasa
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 divide-y sm:divide-y-0 sm:divide-x">
          <div className="pb-3 sm:pb-0 sm:pr-4">
            <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
              Önceki Günden Devir
            </div>
            <CurrencyLines items={kasa.oncekiGunDevir} />
          </div>
          <div className="py-3 sm:py-0 sm:px-4">
            <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
              Bugün Nakit
            </div>
            <CurrencyLines items={kasa.bugunNakit} />
          </div>
          <div className="pt-3 sm:pt-0 sm:pl-4">
            <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
              Mevcut Kasa
            </div>
            {kasa.toplamKasa.length === 0 ? (
              <span className="text-muted-foreground text-sm">—</span>
            ) : (
              <div className="space-y-0.5">
                {kasa.toplamKasa.map(c => (
                  <div key={c.currency} className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{c.currency}</span>
                    <span className="font-bold text-primary">{fmtAmt(c.amount, c.currency)}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// ─── Close dialog ─────────────────────────────────────────────────────────

function CloseDialog({
  open, onClose, onConfirm, loading, totalTry,
}: {
  open: boolean; onClose: () => void;
  onConfirm: (notes: string) => void;
  loading: boolean; totalTry: number;
}) {
  const [notes, setNotes] = useState('');
  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader><DialogTitle>Kasayı Kapat</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <div className="rounded-lg bg-muted p-4 text-center">
            <div className="text-sm text-muted-foreground">Bugünkü toplam</div>
            <div className="text-3xl font-bold mt-1">{fmtTry(totalTry)}</div>
          </div>
          <div className="space-y-1">
            <Label>Not (opsiyonel)</Label>
            <Textarea placeholder="Kasa kapanış notu..." value={notes}
              onChange={e => setNotes(e.target.value)} rows={2} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => onConfirm(notes)} disabled={loading}>
            <Lock className="h-4 w-4 mr-2" />Kasayı Kapat
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Approve dialog ────────────────────────────────────────────────────────

function ApproveDialog({
  open, onClose, onConfirm, loading, report,
}: {
  open: boolean; onClose: () => void;
  onConfirm: (notes: string) => void;
  loading: boolean; report: CashReportState;
}) {
  const [notes, setNotes] = useState('');
  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader><DialogTitle>Kasayı Onayla</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          {report.closingNotes && (
            <div className="rounded-md border bg-muted p-3 text-sm">
              <span className="font-medium">Kapanış notu: </span>{report.closingNotes}
            </div>
          )}
          <div className="flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
            <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
            <span>Onaylanan kasa raporuna yeni ödeme eklemek için raporu yeniden açmanız gerekir.</span>
          </div>
          <div className="space-y-1">
            <Label>Onay notu (opsiyonel)</Label>
            <Textarea placeholder="Onay notu..." value={notes}
              onChange={e => setNotes(e.target.value)} rows={2} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => onConfirm(notes)} disabled={loading} className="bg-green-600 hover:bg-green-700">
            <CheckCircle2 className="h-4 w-4 mr-2" />Onayla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Main tab ─────────────────────────────────────────────────────────────

export function DailyCashTab() {
  const { hasPermission } = usePermissions();
  const accessToken = useAuthStore(s => s.accessToken);
  const user = useAuthStore(s => s.user);
  const qc = useQueryClient();

  const jwtPayload = accessToken ? parseJwt(accessToken) : null;
  const branchId = jwtPayload?.branch_id ? parseInt(jwtPayload.branch_id, 10) : user?.branchId;

  const today = new Date();
  const [selectedDate, setSelectedDate] = useState(today);
  const dateStr = format(selectedDate, 'yyyy-MM-dd');

  const [closeOpen, setCloseOpen]     = useState(false);
  const [approveOpen, setApproveOpen] = useState(false);

  const { data: res, isLoading } = useQuery({
    queryKey: ['cash-report', dateStr, branchId],
    queryFn: () => cashReportApi.getDetail(dateStr, branchId),
    enabled: !!branchId,
  });

  const report = res?.data;
  const invalidate = () => qc.invalidateQueries({ queryKey: ['cash-report', dateStr] });

  const closeMutation = useMutation({
    mutationFn: (notes: string) => cashReportApi.close(dateStr, notes || undefined),
    onSuccess: () => { toast.success('Kasa kapatıldı'); setCloseOpen(false); invalidate(); },
    onError: (e: any) => toast.error(e?.response?.data?.message ?? 'Kasa kapatılamadı'),
  });

  const approveMutation = useMutation({
    mutationFn: (notes: string) =>
      cashReportApi.approve(report!.reportStatus!.publicId, notes || undefined),
    onSuccess: () => { toast.success('Kasa onaylandı'); setApproveOpen(false); invalidate(); },
    onError: (e: any) => toast.error(e?.response?.data?.message ?? 'Onaylanamadı'),
  });

  const reopenMutation = useMutation({
    mutationFn: () => cashReportApi.reopen(report!.reportStatus!.publicId),
    onSuccess: () => { toast.success('Kasa yeniden açıldı'); invalidate(); },
    onError: (e: any) => toast.error(e?.response?.data?.message ?? 'Yeniden açılamadı'),
  });

  const goDay = (delta: number) => {
    const d = new Date(selectedDate);
    d.setDate(d.getDate() + delta);
    setSelectedDate(d);
  };

  const isToday = dateStr === format(today, 'yyyy-MM-dd');
  const rpt = report?.reportStatus;
  const isOpen     = !rpt || rpt.status === 1;
  const isClosed   = rpt?.status === 2;
  const isApproved = rpt?.status === 3;

  const canClose   = hasPermission('report.close')   && isOpen;
  const canApprove = hasPermission('report.approve') && isClosed;
  const canReopen  = hasPermission('report.reopen')  && (isClosed || isApproved);

  return (
    <div className="space-y-6">
      {/* ── Tarih + durum başlığı ── */}
      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={() => goDay(-1)}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <div className="flex items-center gap-2 min-w-[180px] justify-center">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <span className="font-semibold">
              {format(selectedDate, 'd MMMM yyyy', { locale: tr })}
            </span>
            {isToday && <Badge variant="secondary" className="text-xs">Bugün</Badge>}
          </div>
          <Button variant="ghost" size="icon" onClick={() => goDay(1)} disabled={isToday}>
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>

        <div className="flex items-center gap-2 sm:ml-auto">
          {rpt && <StatusBadge status={rpt.status} label={rpt.statusLabel} />}
          {canClose && (
            <Button size="sm" variant="outline" onClick={() => setCloseOpen(true)}>
              <Lock className="h-4 w-4 mr-2" />Kasayı Kapat
            </Button>
          )}
          {canApprove && (
            <Button size="sm" className="bg-green-600 hover:bg-green-700"
              onClick={() => setApproveOpen(true)}>
              <CheckCircle2 className="h-4 w-4 mr-2" />Onayla
            </Button>
          )}
          {canReopen && (
            <Button size="sm" variant="ghost"
              onClick={() => reopenMutation.mutate()}
              disabled={reopenMutation.isPending}>
              <LockOpen className="h-4 w-4 mr-2" />Yeniden Aç
            </Button>
          )}
        </div>
      </div>

      {!branchId ? (
        <div className="rounded-lg border bg-muted/30 py-12 text-center text-muted-foreground">
          Günlük kasa raporu şube bağlamı gerektirir. Lütfen şubenizle giriş yapın.
        </div>
      ) : isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-48 rounded-lg" />
          <Skeleton className="h-32 rounded-lg" />
        </div>
      ) : report && report.byMethod.length > 0 ? (
        <>
          {/* ── Para birimi × yöntem matrisi ── */}
          <div>
            <h3 className="font-semibold mb-3 text-sm text-muted-foreground uppercase tracking-wide">
              Ödeme Özeti
            </h3>
            <CashMatrix byMethod={report.byMethod} />
          </div>

          {/* ── POS + Banka yan yana ── */}
          {(report.posTotals.length > 0 || report.bankTotals.length > 0) && (
            <div className="grid gap-4 md:grid-cols-2">
              <PosTotalsCard posTotals={report.posTotals} />
              <BankTotalsCard bankTotals={report.bankTotals} />
            </div>
          )}

          {/* ── Kasa ── */}
          <KasaCard kasa={report.kasa} />

          {/* ── Onay bilgisi ── */}
          {isApproved && rpt && (
            <div className="rounded-lg border border-green-200 bg-green-50 p-4 flex items-start gap-3">
              <CheckCircle2 className="h-5 w-5 text-green-600 mt-0.5 shrink-0" />
              <div className="text-sm">
                <div className="font-medium text-green-800">Kasa onaylandı</div>
                {rpt.approvedAt && (
                  <div className="text-green-700 mt-0.5">
                    {new Date(rpt.approvedAt).toLocaleString('tr-TR')}
                  </div>
                )}
                {rpt.approvalNotes && (
                  <div className="text-green-700 mt-1 italic">"{rpt.approvalNotes}"</div>
                )}
              </div>
            </div>
          )}

          {/* ── Ödeme detayları tablosu ── */}
          {report.payments.length > 0 && (
            <div>
              <h3 className="font-semibold mb-3 text-sm text-muted-foreground uppercase tracking-wide">
                Ödeme Detayları
              </h3>
              <div className="rounded-lg border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-muted/50">
                      <TableHead className="w-16">Saat</TableHead>
                      <TableHead>Hasta</TableHead>
                      <TableHead>Tutar</TableHead>
                      <TableHead className="hidden md:table-cell">TL Karşılığı</TableHead>
                      <TableHead>Yöntem</TableHead>
                      <TableHead className="hidden lg:table-cell">Not</TableHead>
                      <TableHead className="hidden lg:table-cell">Kaydeden</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {report.payments.map(p => (
                      <TableRow key={p.id}>
                        <TableCell className="text-muted-foreground text-xs font-mono">
                          {fmtTime(p.createdAt)}
                        </TableCell>
                        <TableCell className="font-medium">{p.patientName}</TableCell>
                        <TableCell>
                          <div className="font-semibold">{fmtAmt(p.amount, p.currency)}</div>
                          {p.currency !== 'TRY' && (
                            <div className="text-xs text-muted-foreground">
                              × {p.exchangeRate.toFixed(2)}
                            </div>
                          )}
                        </TableCell>
                        <TableCell className="hidden md:table-cell text-muted-foreground">
                          {fmtTry(p.baseAmount)}
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className="gap-1 text-xs">
                            {p.methodLabel}
                          </Badge>
                        </TableCell>
                        <TableCell className="hidden lg:table-cell text-muted-foreground text-sm max-w-[160px] truncate">
                          {p.notes ?? '—'}
                        </TableCell>
                        <TableCell className="hidden lg:table-cell text-muted-foreground text-xs">
                          {p.recordedByName}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          )}
        </>
      ) : (
        <div className="rounded-lg border bg-muted/30 py-12 text-center text-muted-foreground">
          Bu tarihte ödeme kaydı bulunamadı.
        </div>
      )}

      {/* ── Dialoglar ── */}
      <CloseDialog
        open={closeOpen}
        onClose={() => setCloseOpen(false)}
        onConfirm={(notes) => closeMutation.mutate(notes)}
        loading={closeMutation.isPending}
        totalTry={report?.totalTry ?? 0}
      />

      {rpt && (
        <ApproveDialog
          open={approveOpen}
          onClose={() => setApproveOpen(false)}
          onConfirm={(notes) => approveMutation.mutate(notes)}
          loading={approveMutation.isPending}
          report={rpt}
        />
      )}
    </div>
  );
}
