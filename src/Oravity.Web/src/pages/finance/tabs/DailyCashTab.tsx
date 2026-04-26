import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  Calendar, CheckCircle2, Clock, Lock, LockOpen,
  ChevronLeft, ChevronRight, Banknote, CreditCard,
  Building2, Hash, AlertCircle,
} from 'lucide-react';
import { cashReportApi, type CashMethodTotal, type CashReportState } from '@/api/cashReport';
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

// ─── Helpers ──────────────────────────────────────────────────────────────

function fmtTry(v: number) {
  return v.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
}

function fmtAmt(v: number, currency: string) {
  if (currency === 'TRY') return fmtTry(v);
  return v.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + currency;
}

function fmtTime(dt: string) {
  return new Date(dt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

const METHOD_ICONS: Record<number, React.ReactNode> = {
  1: <Banknote className="h-4 w-4" />,
  2: <CreditCard className="h-4 w-4" />,
  3: <Building2 className="h-4 w-4" />,
  4: <Hash className="h-4 w-4" />,
  5: <Hash className="h-4 w-4" />,
};

const METHOD_COLORS: Record<number, string> = {
  1: 'bg-green-50 border-green-200 text-green-800',
  2: 'bg-blue-50 border-blue-200 text-blue-800',
  3: 'bg-purple-50 border-purple-200 text-purple-800',
  4: 'bg-orange-50 border-orange-200 text-orange-800',
  5: 'bg-slate-50 border-slate-200 text-slate-800',
};

function StatusBadge({ status, label }: { status: number; label: string }) {
  if (status === 1 /* Open */)
    return <Badge variant="secondary" className="gap-1"><Clock className="h-3 w-3" />{label}</Badge>;
  if (status === 2 /* Closed */)
    return <Badge variant="outline" className="gap-1 border-amber-300 text-amber-700 bg-amber-50"><Lock className="h-3 w-3" />{label}</Badge>;
  return <Badge className="gap-1 bg-green-600 hover:bg-green-600"><CheckCircle2 className="h-3 w-3" />{label}</Badge>;
}

// ─── Method card ──────────────────────────────────────────────────────────

function MethodCard({ m }: { m: CashMethodTotal }) {
  const colorClass = METHOD_COLORS[m.method] ?? METHOD_COLORS[1];
  return (
    <div className={`rounded-lg border p-4 ${colorClass}`}>
      <div className="flex items-center gap-2 mb-2">
        {METHOD_ICONS[m.method]}
        <span className="font-semibold text-sm">{m.methodLabel}</span>
        <span className="ml-auto text-xs opacity-70">{m.count} işlem</span>
      </div>
      <div className="text-xl font-bold">{fmtTry(m.totalTry)}</div>
      {m.byCurrency.filter(c => c.currency !== 'TRY').map(c => (
        <div key={c.currency} className="text-xs mt-1 opacity-80">
          {fmtAmt(c.amount, c.currency)} × {(c.baseTry / c.amount).toFixed(2)} = {fmtTry(c.baseTry)}
        </div>
      ))}
    </div>
  );
}

// ─── Close dialog ─────────────────────────────────────────────────────────

function CloseDialog({
  open,
  onClose,
  onConfirm,
  loading,
  totalTry,
}: {
  open: boolean;
  onClose: () => void;
  onConfirm: (notes: string) => void;
  loading: boolean;
  totalTry: number;
}) {
  const [notes, setNotes] = useState('');
  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Kasayı Kapat</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="rounded-lg bg-muted p-4 text-center">
            <div className="text-sm text-muted-foreground">Bugünkü toplam</div>
            <div className="text-3xl font-bold mt-1">{fmtTry(totalTry)}</div>
          </div>
          <div className="space-y-1">
            <Label>Not (opsiyonel)</Label>
            <Textarea
              placeholder="Kasa kapanış notu..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
              rows={2}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => onConfirm(notes)} disabled={loading}>
            <Lock className="h-4 w-4 mr-2" />
            Kasayı Kapat
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Approve dialog ────────────────────────────────────────────────────────

function ApproveDialog({
  open,
  onClose,
  onConfirm,
  loading,
  report,
}: {
  open: boolean;
  onClose: () => void;
  onConfirm: (notes: string) => void;
  loading: boolean;
  report: CashReportState;
}) {
  const [notes, setNotes] = useState('');
  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Kasayı Onayla</DialogTitle>
        </DialogHeader>
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
            <Textarea
              placeholder="Onay notu..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
              rows={2}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => onConfirm(notes)} disabled={loading} className="bg-green-600 hover:bg-green-700">
            <CheckCircle2 className="h-4 w-4 mr-2" />
            Onayla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Main tab ─────────────────────────────────────────────────────────────

export function DailyCashTab() {
  const { hasPermission } = usePermissions();
  const qc = useQueryClient();

  const today = new Date();
  const [selectedDate, setSelectedDate] = useState(today);
  const dateStr = format(selectedDate, 'yyyy-MM-dd');

  const [closeOpen, setCloseOpen]     = useState(false);
  const [approveOpen, setApproveOpen] = useState(false);

  const { data: res, isLoading } = useQuery({
    queryKey: ['cash-report', dateStr],
    queryFn: () => cashReportApi.getDetail(dateStr),
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

  const isToday = format(selectedDate, 'yyyy-MM-dd') === format(today, 'yyyy-MM-dd');

  const rpt = report?.reportStatus;
  const isOpen     = !rpt || rpt.status === 1;
  const isClosed   = rpt?.status === 2;
  const isApproved = rpt?.status === 3;

  const canClose   = hasPermission('report.close')   && isOpen;
  const canApprove = hasPermission('report.approve') && isClosed;
  const canReopen  = hasPermission('report.reopen')  && (isClosed || isApproved);

  return (
    <div className="space-y-6">
      {/* ── Tarih ve durum başlığı ── */}
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
              <Lock className="h-4 w-4 mr-2" />
              Kasayı Kapat
            </Button>
          )}
          {canApprove && (
            <Button size="sm" className="bg-green-600 hover:bg-green-700"
              onClick={() => setApproveOpen(true)}>
              <CheckCircle2 className="h-4 w-4 mr-2" />
              Onayla
            </Button>
          )}
          {canReopen && (
            <Button size="sm" variant="ghost"
              onClick={() => reopenMutation.mutate()}
              disabled={reopenMutation.isPending}>
              <LockOpen className="h-4 w-4 mr-2" />
              Yeniden Aç
            </Button>
          )}
        </div>
      </div>

      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-lg" />
          ))}
        </div>
      ) : (
        <>
          {/* ── Yöntem kartları ── */}
          {report && report.byMethod.length > 0 ? (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              {report.byMethod.map(m => <MethodCard key={m.method} m={m} />)}
              {/* Genel toplam */}
              <div className="rounded-lg border-2 border-primary bg-primary/5 p-4 flex flex-col justify-between">
                <div className="text-sm font-medium text-muted-foreground mb-1">Genel Toplam</div>
                <div className="text-2xl font-bold">{fmtTry(report.totalTry)}</div>
                <div className="text-xs text-muted-foreground mt-1">{report.totalCount} işlem</div>
              </div>
            </div>
          ) : (
            <div className="rounded-lg border bg-muted/30 py-12 text-center text-muted-foreground">
              Bu tarihte ödeme kaydı bulunamadı.
            </div>
          )}

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
          {report && report.payments.length > 0 && (
            <div>
              <h3 className="font-semibold mb-3">Ödeme Detayları</h3>
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
                            {METHOD_ICONS[p.method]}
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
