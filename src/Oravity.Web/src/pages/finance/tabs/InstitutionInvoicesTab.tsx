import { useState, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Plus, CreditCard, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import {
  institutionInvoicesApi,
  type InstitutionInvoice,
  type InstitutionInvoiceStatus,
  type InstitutionPaymentMethod,
} from '@/api/institutionInvoices';
import { institutionsApi } from '@/api/institutions';
import { settingsApi } from '@/api/settings';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

const STATUS_OPTIONS: {
  value: InstitutionInvoiceStatus;
  label: string;
  color: 'default' | 'secondary' | 'destructive' | 'outline';
}[] = [
  { value: 'Issued',         label: 'Kesildi',        color: 'secondary'   },
  { value: 'PartiallyPaid',  label: 'Kısmi Ödendi',   color: 'secondary'   },
  { value: 'Paid',           label: 'Ödendi',          color: 'default'     },
  { value: 'Overdue',        label: 'Vadesi Geçti',    color: 'destructive' },
  { value: 'InFollowUp',     label: 'Takipte',         color: 'destructive' },
  { value: 'Rejected',       label: 'Reddedildi',      color: 'outline'     },
  { value: 'Cancelled',      label: 'İptal Edildi',    color: 'outline'     },
];

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

// Kurum faturası değişince tüm ilgili cache'leri temizle:
// hem global Finance listesi hem hasta bazlı Cari Hesap verileri
function invalidateInvoiceQueries(qc: QueryClient) {
  qc.invalidateQueries({ queryKey: ['institution-invoices'] });
  qc.invalidateQueries({ queryKey: ['institution-invoices-for-patient'] });
  qc.invalidateQueries({ queryKey: ['patient-account'] });
  qc.invalidateQueries({ queryKey: ['billable-items'] });
}

function hasActiveFollowUp(inv: InstitutionInvoice) {
  return inv.followUpStatus !== 'None';
}

export function InstitutionInvoicesTab() {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [createOpen, setCreateOpen] = useState(false);
  const [paymentTarget, setPaymentTarget] = useState<InstitutionInvoice | null>(null);
  const [cancelTarget, setCancelTarget] = useState<InstitutionInvoice | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['institution-invoices', statusFilter],
    queryFn: () => institutionInvoicesApi.list({
      status: statusFilter === 'all' ? undefined : (statusFilter as InstitutionInvoiceStatus),
      pageSize: 100,
    }),
  });

  const { data: institutions } = useQuery({
    queryKey: ['institutions'],
    queryFn: () => institutionsApi.list(),
  });

  const items = data?.data.items ?? [];
  const insts = institutions?.data ?? [];
  const followUpCount = items.filter(hasActiveFollowUp).length;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Kurum Fatura Takibi</CardTitle>
            <p className="text-sm text-muted-foreground">
              Toplam: {items.length}
              {followUpCount > 0 && (
                <span className="ml-2 inline-flex items-center gap-1 text-amber-600">
                  <AlertTriangle className="h-3 w-3" />
                  {followUpCount} takipte
                </span>
              )}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-44">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tümü</SelectItem>
                {STATUS_OPTIONS.map(o => (
                  <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="mr-1 h-4 w-4" /> Yeni Fatura
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Fatura No</TableHead>
                <TableHead>Kurum</TableHead>
                <TableHead>Hasta</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead>Vade</TableHead>
                <TableHead className="text-right">Tutar</TableHead>
                <TableHead className="text-right">Ödenen</TableHead>
                <TableHead className="text-right">Kalan</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 4 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 10 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-20" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={10} className="text-center text-muted-foreground py-6">
                    Fatura yok.
                  </TableCell>
                </TableRow>
              ) : (
                items.map(inv => {
                  const opt = STATUS_OPTIONS.find(o => o.value === inv.status);
                  const followUp = hasActiveFollowUp(inv);
                  return (
                    <TableRow key={inv.id}>
                      <TableCell className="font-mono font-medium">{inv.invoiceNo}</TableCell>
                      <TableCell>{inv.institutionName}</TableCell>
                      <TableCell>{inv.patientName ?? '—'}</TableCell>
                      <TableCell>{format(new Date(inv.invoiceDate), 'dd.MM.yyyy')}</TableCell>
                      <TableCell>
                        {format(new Date(inv.dueDate), 'dd.MM.yyyy')}
                        {followUp && (
                          <Badge variant="outline" className="ml-1">
                            {inv.followUpStatusLabel}
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">{formatCurrency(inv.amount)}</TableCell>
                      <TableCell className="text-right">{formatCurrency(inv.paidAmount)}</TableCell>
                      <TableCell className="text-right font-semibold">
                        {formatCurrency(inv.remainingAmount)}
                      </TableCell>
                      <TableCell>
                        <Badge variant={opt?.color ?? 'secondary'}>{inv.statusLabel}</Badge>
                      </TableCell>
                      <TableCell className="flex items-center gap-1">
                        {inv.remainingAmount > 0 && inv.status !== 'Rejected' && inv.status !== 'Cancelled' && (
                          <Button size="sm" variant="ghost" onClick={() => setPaymentTarget(inv)} title="Ödeme Al">
                            <CreditCard className="h-4 w-4" />
                          </Button>
                        )}
                        {!['Paid', 'PartiallyPaid', 'Cancelled'].includes(inv.status) && (
                          <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setCancelTarget(inv)} title="İptal Et">
                            ✕
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {createOpen && (
        <CreateInvoiceDialog
          onClose={() => setCreateOpen(false)}
          institutions={insts}
          onSuccess={() => {
            setCreateOpen(false);
            invalidateInvoiceQueries(qc);
          }}
        />
      )}
      {paymentTarget && (
        <RegisterPaymentDialog
          invoice={paymentTarget}
          onClose={() => setPaymentTarget(null)}
          onSuccess={() => {
            setPaymentTarget(null);
            invalidateInvoiceQueries(qc);
          }}
        />
      )}
      {cancelTarget && (
        <CancelInvoiceDialog
          invoice={cancelTarget}
          onClose={() => setCancelTarget(null)}
          onSuccess={() => {
            setCancelTarget(null);
            invalidateInvoiceQueries(qc);
          }}
        />
      )}
    </div>
  );
}

function CancelInvoiceDialog({
  invoice, onClose, onSuccess,
}: {
  invoice: InstitutionInvoice;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState('');

  const mut = useMutation({
    mutationFn: () => institutionInvoicesApi.cancel(invoice.publicId, reason),
    onSuccess: () => { toast.success('Fatura iptal edildi'); onSuccess(); },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="text-destructive">Faturayı İptal Et</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-1">
          <p className="text-sm text-muted-foreground">
            <span className="font-mono font-medium text-foreground">{invoice.invoiceNo}</span> nolu fatura iptal edilecek.
            Bu işlem geri alınamaz.
          </p>
          <div>
            <Label className="mb-1 block">İptal Gerekçesi</Label>
            <Input
              placeholder="Zorunlu"
              value={reason}
              onChange={e => setReason(e.target.value)}
              autoFocus
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Vazgeç</Button>
          <Button
            variant="destructive"
            disabled={!reason.trim() || mut.isPending}
            onClick={() => mut.mutate()}
          >
            {mut.isPending ? 'İptal ediliyor...' : 'İptal Et'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Create dialog ────────────────────────────────────────────────────────

function CreateInvoiceDialog({
  onClose, onSuccess, institutions,
}: {
  onClose: () => void;
  onSuccess: () => void;
  institutions: { id: number; name: string }[];
}) {
  const [institutionId, setInstitutionId] = useState<string>('');
  const [patientId, setPatientId] = useState<string>('');
  const [invoiceNo, setInvoiceNo] = useState('');
  const [invoiceDate, setInvoiceDate] = useState(format(new Date(), 'yyyy-MM-dd'));

  const { data: nextNumber } = useQuery({
    queryKey: ['institution-invoices-next-number'],
    queryFn: () => institutionInvoicesApi.nextNumber().then(r => r.data.number),
    staleTime: 0,
    gcTime: 0,
    retry: false, // 403 (şirket düzey kullanıcı) durumunda tekrar deneme yapma
  });
  useEffect(() => {
    if (nextNumber && !invoiceNo) setInvoiceNo(nextNumber);
  }, [nextNumber]); // eslint-disable-line react-hooks/exhaustive-deps
  const [dueDate, setDueDate] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return format(d, 'yyyy-MM-dd');
  });
  const [amount, setAmount] = useState<string>('');
  const [notes, setNotes] = useState('');

  const create = useMutation({
    mutationFn: () => institutionInvoicesApi.create({
      patientId: parseInt(patientId, 10),
      institutionId: parseInt(institutionId, 10),
      invoiceNo,
      invoiceDate,
      dueDate,
      amount: parseFloat(amount),
      notes: notes || undefined,
    }),
    onSuccess: () => {
      toast.success('Kurum faturası oluşturuldu');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const canSubmit =
    institutionId &&
    patientId &&
    invoiceNo &&
    amount &&
    parseFloat(amount) > 0 &&
    invoiceDate &&
    dueDate;

  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>Yeni Kurum Faturası</DialogTitle></DialogHeader>
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm mb-1 block">Kurum</label>
              <Select value={institutionId} onValueChange={setInstitutionId}>
                <SelectTrigger>
                  <SelectValue placeholder="Seçiniz" />
                </SelectTrigger>
                <SelectContent>
                  {institutions.map(i => (
                    <SelectItem key={i.id} value={String(i.id)}>{i.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="text-sm mb-1 block">Hasta ID</label>
              <Input
                type="number"
                value={patientId}
                onChange={e => setPatientId(e.target.value)}
                placeholder="Hasta ID"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm mb-1 block">Fatura No</label>
              <Input
                value={invoiceNo}
                onChange={e => setInvoiceNo(e.target.value)}
                placeholder="Yükleniyor..."
                className="font-mono"
              />
            </div>
            <div>
              <label className="text-sm mb-1 block">Tutar (₺)</label>
              <Input
                type="number" step="0.01" min="0"
                value={amount}
                onChange={e => setAmount(e.target.value)}
              />
            </div>
            <div>
              <label className="text-sm mb-1 block">Fatura Tarihi</label>
              <Input type="date" value={invoiceDate} onChange={e => setInvoiceDate(e.target.value)} />
            </div>
            <div>
              <label className="text-sm mb-1 block">Vade</label>
              <Input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} />
            </div>
          </div>
          <div>
            <label className="text-sm mb-1 block">Notlar</label>
            <Input value={notes} onChange={e => setNotes(e.target.value)} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Vazgeç</Button>
          <Button onClick={() => create.mutate()} disabled={!canSubmit || create.isPending}>
            Oluştur
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Register payment dialog ──────────────────────────────────────────────

function RegisterPaymentDialog({
  invoice, onClose, onSuccess,
}: {
  invoice: InstitutionInvoice;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [amount, setAmount] = useState(String(invoice.remainingAmount));
  const [paymentDate, setPaymentDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [method, setMethod] = useState<InstitutionPaymentMethod>('BankTransfer');
  const [referenceNo, setReferenceNo] = useState('');
  const [bankAccountPublicId, setBankAccountPublicId] = useState('');

  const { data: bankAccountsRaw } = useQuery({
    queryKey: ['bank-accounts'],
    queryFn: () => settingsApi.listBankAccounts().then(r => r.data),
  });
  const bankAccounts = (Array.isArray(bankAccountsRaw) ? bankAccountsRaw : []).filter(b => b.isActive);

  const mut = useMutation({
    mutationFn: () => institutionInvoicesApi.registerPayment(invoice.publicId, {
      amount: parseFloat(amount),
      paymentDate,
      method,
      referenceNo: referenceNo || undefined,
      bankAccountPublicId: bankAccountPublicId || undefined,
    }),
    onSuccess: () => {
      toast.success('Ödeme kaydedildi');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Ödeme Kaydet — {invoice.invoiceNo}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div>
            <Label className="mb-1 block">
              Tutar <span className="text-muted-foreground">(kalan: {formatCurrency(invoice.remainingAmount)})</span>
            </Label>
            <Input
              type="number" step="0.01" min="0"
              value={amount}
              onChange={e => setAmount(e.target.value)}
            />
          </div>
          <div>
            <Label className="mb-1 block">Tarih</Label>
            <Input type="date" value={paymentDate} onChange={e => setPaymentDate(e.target.value)} />
          </div>
          <div>
            <Label className="mb-1 block">Yöntem</Label>
            <Select
              value={method}
              onValueChange={v => setMethod(v as InstitutionPaymentMethod)}
            >
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
              <Select value={bankAccountPublicId} onValueChange={setBankAccountPublicId}>
                <SelectTrigger>
                  <SelectValue placeholder="Hesap seçin (opsiyonel)">
                    {(val: string | null) => {
                      if (!val) return undefined;
                      const acc = bankAccounts.find(b => b.publicId === val);
                      return acc ? `${acc.bankShortName ? acc.bankShortName + ' — ' : ''}${acc.accountName}` : val;
                    }}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Belirtme</SelectItem>
                  {bankAccounts.map(b => (
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
            Kaydet
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
