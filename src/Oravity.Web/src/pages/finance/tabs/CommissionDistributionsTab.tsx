import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { financeApi } from '@/api/finance';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function CommissionDistributionsTab() {
  const { data, isLoading } = useQuery({
    queryKey: ['commissions', 'distributed'],
    queryFn: () => financeApi.getCommissions({ status: 'Distributed', pageSize: 100 }),
  });

  const items = data?.data.items ?? [];
  const total = data?.data.totalCommissionAmount ?? 0;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Dağıtılmış Hakedişler</CardTitle>
        <p className="text-sm text-muted-foreground">
          Toplam: {items.length} — {formatCurrency(total)}
        </p>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Hekim</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead className="text-right">Brüt</TableHead>
              <TableHead className="text-right">Oran</TableHead>
              <TableHead className="text-right">Tutar</TableHead>
              <TableHead>Durum</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-24" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground py-6">
                  Dağıtılmış hakediş yok.
                </TableCell>
              </TableRow>
            ) : (
              items.map((c) => (
                <TableRow key={c.id}>
                  <TableCell className="font-medium">{c.doctorName ?? `#${c.doctorId}`}</TableCell>
                  <TableCell>{c.distributedAt ? format(new Date(c.distributedAt), 'dd.MM.yyyy') : '—'}</TableCell>
                  <TableCell className="text-right">{formatCurrency(c.grossAmount)}</TableCell>
                  <TableCell className="text-right">%{(c.commissionRate * 100).toFixed(1)}</TableCell>
                  <TableCell className="text-right font-semibold">
                    {formatCurrency(c.netCommissionAmount ?? c.commissionAmount)}
                  </TableCell>
                  <TableCell><Badge variant="default">{c.statusLabel}</Badge></TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
