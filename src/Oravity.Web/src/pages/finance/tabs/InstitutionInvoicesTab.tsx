import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Plus, CreditCard, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import {
  institutionInvoicesApi,
  type InstitutionInvoiceStatus,
} from '@/api/institutionInvoices';
import { institutionsApi } from '@/api/institutions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
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

const STATUS_OPTIONS: { value: InstitutionInvoiceStatus; label: string; color: 'default' | 'secondary' | 'destructive' | 'outline' }[] = [
  { value: 'Draft',          label: 'Taslak',       color: 'outline' },
  { value: 'Issued',         label: 'Oluşturuldu',  color: 'secondary' },
  { value: 'Submitted',      label: 'Gönderildi',   color: 'secondary' },
  { value: 'PartiallyPaid',  label: 'Kısmi Ödendi', color: 'secondary' },
  { value: 'Paid',           label: 'Ödendi',       color: 'default' },
  { value: 'Rejected',       label: 'Reddedildi',   color: 'destructive' },
  { value: 'Cancelled',      label: 'İptal',        color: 'outline' },
];

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function InstitutionInvoicesTab() {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [createOpen, setCreateOpen] = useState(false);
  const [paymentTarget, setPaymentTarget] = useState<string | null>(null);

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

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Kurum Fatura Takibi</CardTitle>
            <p className="text-sm text-muted-foreground">
              Toplam: {items.length}
              {items.some(i => i.followUpScheduled) && (
                <span className="ml-2 inline-flex items-center gap-1 text-amber-600">
                  <AlertTriangle className="h-3 w-3" />
                  {items.filter(i => i.followUpScheduled).length} takipte
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
                  return (
                    <TableRow key={inv.id}>
                      <TableCell className="font-mono font-medium">{inv.invoiceNumber}</TableCell>
                      <TableCell>{inv.institutionName}</TableCell>
                      <TableCell>{inv.patientName ?? '—'}</TableCell>
                      <TableCell>{format(new Date(inv.invoiceDate), 'dd.MM.yyyy')}</TableCell>
                      <TableCell>
                        {inv.dueDate ? format(new Date(inv.dueDate), 'dd.MM.yyyy') : '—'}
                        {inv.followUpScheduled && (
                          <Badge variant="outline" className="ml-1">Takip</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">{formatCurrency(inv.totalAmount)}</TableCell>
                      <TableCell className="text-right">{formatCurrency(inv.paidAmount)}</TableCell>
                      <TableCell className="text-right font-semibold">
                        {formatCurrency(inv.remainingAmount)}
                      </TableCell>
                      <TableCell>
                        <Badge variant={opt?.color ?? 'secondary'}>{inv.statusLabel}</Badge>
                      </TableCell>
                      <TableCell>
                        {inv.remainingAmount > 0 && (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => setPaymentTarget(inv.publicId)}
                          >
                            <CreditCard className="h-4 w-4" />
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
          open={createOpen}
          onClose={() => setCreateOpen(false)}
          institutions={insts}
          onSuccess={() => {
            setCreateOpen(false);
            qc.invalidateQueries({ queryKey: ['institution-invoices'] });
          }}
        />
      )}
      {paymentTarget && (
        <RegisterPaymentDialog
          publicId={paymentTarget}
          invoice={items.find(i => i.publicId === paymentTarget)!}
          onClose={() => setPaymentTarget(null)}
          onSuccess={() => {
            setPaymentTarget(null);
            qc.invalidateQueries({ queryKey: ['institution-invoices'] });
          }}
        />
      )}
    </div>
  );
}

// ── Create dialog ────────────────────────────────────────────────────────

function CreateInvoiceDialog({
  open, onClose, onSuccess, institutions,
}: {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  institutions: { id: number; name: string }[];
}) {
  const [institutionId, setInstitutionId] = useState<string>('');
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [invoiceDate, setInvoiceDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [dueDate, setDueDate] = useState('');
  const [totalAmount, setTotalAmount] = useState<string>('');
  const [notes, setNotes] = useState('');

  const create = useMutation({
    mutationFn: () => institutionInvoicesApi.create({
      institutionId: parseInt(institutionId, 10),
      invoiceNumber,
      invoiceDate,
      dueDate: dueDate || undefined,
      totalAmount: parseFloat(totalAmount),
      notes: notes || undefined,
    }),
    onSuccess: () => {
      toast.success('Kurum faturası oluşturuldu');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const canSubmit = institutionId && invoiceNumber && totalAmount && parseFloat(totalAmount) > 0;

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>Yeni Kurum Faturası</DialogTitle></DialogHeader>
        <div className="space-y-3">
          <div>
            <label className="text-sm mb-1 block">Kurum</label>
            <Select value={institutionId} onValueChange={setInstitutionId}>
              <SelectTrigger><SelectValue placeholder="Seçiniz" /></SelectTrigger>
              <SelectContent>
                {institutions.map(i => (
                  <SelectItem key={i.id} value={String(i.id)}>{i.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm mb-1 block">Fatura No</label>
              <Input value={invoiceNumber} onChange={e => setInvoiceNumber(e.target.value)} />
            </div>
            <div>
              <label className="text-sm mb-1 block">Toplam (₺)</label>
              <Input
                type="number" step="0.01" min="0"
                value={totalAmount}
                onChange={e => setTotalAmount(e.target.value)}
              />
            </div>
            <div>
              <label className="text-sm mb-1 block">Tarih</label>
              <Input type="date" value={invoiceDate} onChange={e => setInvoiceDate(e.target.value)} />
            </div>
            <div>
              <label className="text-sm mb-1 block">Vade (opsiyonel)</label>
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
  publicId, invoice, onClose, onSuccess,
}: {
  publicId: string;
  invoice: { invoiceNumber: string; remainingAmount: number };
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [amount, setAmount] = useState(String(invoice.remainingAmount));
  const [paymentDate, setPaymentDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [method, setMethod] = useState('BankTransfer');
  const [reference, setReference] = useState('');

  const mut = useMutation({
    mutationFn: () => institutionInvoicesApi.registerPayment(publicId, {
      amount: parseFloat(amount),
      paymentDate,
      method,
      reference: reference || undefined,
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
          <DialogTitle>Ödeme Kaydet — {invoice.invoiceNumber}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div>
            <label className="text-sm mb-1 block">
              Tutar (kalan: {formatCurrency(invoice.remainingAmount)})
            </label>
            <Input
              type="number" step="0.01" min="0"
              value={amount}
              onChange={e => setAmount(e.target.value)}
            />
          </div>
          <div>
            <label className="text-sm mb-1 block">Tarih</label>
            <Input type="date" value={paymentDate} onChange={e => setPaymentDate(e.target.value)} />
          </div>
          <div>
            <label className="text-sm mb-1 block">Yöntem</label>
            <Select value={method} onValueChange={setMethod}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="Cash">Nakit</SelectItem>
                <SelectItem value="BankTransfer">Havale/EFT</SelectItem>
                <SelectItem value="CreditCard">Kredi Kartı</SelectItem>
                <SelectItem value="Cheque">Çek</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <label className="text-sm mb-1 block">Referans</label>
            <Input value={reference} onChange={e => setReference(e.target.value)} />
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
