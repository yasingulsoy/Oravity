import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Wallet } from 'lucide-react';
import { toast } from 'sonner';
import { patientAccountApi } from '@/api/patientAccount';
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

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function PatientAccountTab({ patientId }: { patientId: number }) {
  const qc = useQueryClient();
  const [allocPayment, setAllocPayment] = useState<number | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['patient-account', patientId],
    queryFn: () => patientAccountApi.getAccount(patientId),
  });

  const acc = data?.data;
  const openItems = (acc?.items ?? []).filter(i => i.status !== 'Cancelled');
  const unallocPayments = (acc?.payments ?? []).filter(p => p.unallocatedAmount > 0);

  const payment = allocPayment != null
    ? (acc?.payments ?? []).find(p => p.id === allocPayment)
    : null;

  return (
    <div className="space-y-4">
      {/* Summary */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard label="Planlanan" value={acc?.totalPlanned} loading={isLoading} />
        <SummaryCard label="Tamamlanan" value={acc?.totalCompleted} loading={isLoading} color="text-blue-600" />
        <SummaryCard label="Tahsil Edilen" value={acc?.totalPaid} loading={isLoading} color="text-green-600" />
        <SummaryCard
          label="Bakiye"
          value={acc?.balance}
          loading={isLoading}
          color={(acc?.balance ?? 0) > 0 ? 'text-destructive' : 'text-green-600'}
        />
      </div>

      {/* Items */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Tedavi Kalemleri</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tedavi</TableHead>
                <TableHead>Diş</TableHead>
                <TableHead>Hekim</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="text-right">Planlanan</TableHead>
                <TableHead className="text-right">Net</TableHead>
                <TableHead>Tarih</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-16" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : openItems.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground py-4">
                    Tedavi kalemi yok.
                  </TableCell>
                </TableRow>
              ) : (
                openItems.map(i => (
                  <TableRow key={i.treatmentPlanItemId}>
                    <TableCell className="font-medium">{i.treatmentName}</TableCell>
                    <TableCell>{i.tooth ?? '—'}</TableCell>
                    <TableCell>{i.doctorName ?? '—'}</TableCell>
                    <TableCell><Badge variant="outline">{i.statusLabel}</Badge></TableCell>
                    <TableCell className="text-right">{formatCurrency(i.plannedPrice)}</TableCell>
                    <TableCell className="text-right">{formatCurrency(i.finalPrice)}</TableCell>
                    <TableCell>{i.completedAt ? format(new Date(i.completedAt), 'dd.MM.yyyy') : '—'}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Payments */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Ödemeler</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tarih</TableHead>
                <TableHead>Yöntem</TableHead>
                <TableHead className="text-right">Tutar</TableHead>
                <TableHead className="text-right">Eşleşen</TableHead>
                <TableHead className="text-right">Açıkta</TableHead>
                <TableHead></TableHead>
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
              ) : (acc?.payments ?? []).length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground py-4">
                    Ödeme yok.
                  </TableCell>
                </TableRow>
              ) : (
                acc!.payments.map(p => (
                  <TableRow key={p.id}>
                    <TableCell>{format(new Date(p.paymentDate), 'dd.MM.yyyy')}</TableCell>
                    <TableCell>{p.methodLabel}</TableCell>
                    <TableCell className="text-right font-medium">{formatCurrency(p.amount)}</TableCell>
                    <TableCell className="text-right">{formatCurrency(p.allocatedTotal)}</TableCell>
                    <TableCell className="text-right">
                      {p.unallocatedAmount > 0 ? (
                        <span className="text-amber-600 font-semibold">
                          {formatCurrency(p.unallocatedAmount)}
                        </span>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {p.unallocatedAmount > 0 && (
                        <Button size="sm" variant="ghost" onClick={() => setAllocPayment(p.id)}>
                          <Wallet className="h-4 w-4 mr-1" />
                          Eşle
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
          {unallocPayments.length > 0 && (
            <p className="mt-2 text-sm text-amber-600">
              {unallocPayments.length} ödemede açık tutar var — eşleme talebi oluşturabilirsiniz.
            </p>
          )}
        </CardContent>
      </Card>

      {payment && (
        <AllocationRequestDialog
          payment={payment}
          items={openItems.map(i => ({
            id: i.treatmentPlanItemId,
            label: `${i.treatmentName}${i.tooth ? ` (${i.tooth})` : ''} — ${formatCurrency(i.finalPrice)}`,
          }))}
          onClose={() => setAllocPayment(null)}
          onSuccess={() => {
            setAllocPayment(null);
            qc.invalidateQueries({ queryKey: ['patient-account', patientId] });
          }}
        />
      )}
    </div>
  );
}

function SummaryCard({
  label, value, loading, color = '',
}: { label: string; value?: number; loading: boolean; color?: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-xs font-medium text-muted-foreground">{label}</CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Skeleton className="h-7 w-20" />
        ) : (
          <div className={`text-xl font-bold ${color}`}>
            {formatCurrency(value ?? 0)}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function AllocationRequestDialog({
  payment, items, onClose, onSuccess,
}: {
  payment: { id: number; unallocatedAmount: number };
  items: { id: number; label: string }[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [itemId, setItemId] = useState<string>('');
  const [amount, setAmount] = useState<string>(String(payment.unallocatedAmount));
  const [notes, setNotes] = useState('');

  const mut = useMutation({
    mutationFn: () => patientAccountApi.requestManualAllocation({
      paymentId: payment.id,
      treatmentPlanItemId: itemId ? parseInt(itemId, 10) : undefined,
      amount: parseFloat(amount),
      notes: notes || undefined,
    }),
    onSuccess: () => {
      toast.success('Eşleme talebi oluşturuldu — onay bekliyor');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Manuel Eşleme Talebi</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div>
            <label className="text-sm mb-1 block">Tedavi Kalemi</label>
            <Select value={itemId} onValueChange={setItemId}>
              <SelectTrigger><SelectValue placeholder="Seçiniz" /></SelectTrigger>
              <SelectContent>
                {items.map(i => (
                  <SelectItem key={i.id} value={String(i.id)}>{i.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <label className="text-sm mb-1 block">
              Tutar (açıkta: {formatCurrency(payment.unallocatedAmount)})
            </label>
            <Input
              type="number" step="0.01" min="0" max={payment.unallocatedAmount}
              value={amount}
              onChange={e => setAmount(e.target.value)}
            />
          </div>
          <div>
            <label className="text-sm mb-1 block">Not</label>
            <Input value={notes} onChange={e => setNotes(e.target.value)} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Vazgeç</Button>
          <Button
            onClick={() => mut.mutate()}
            disabled={!amount || parseFloat(amount) <= 0 || mut.isPending}
          >
            Talep Oluştur
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
