import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { commissionsApi } from '@/api/commissions';
import { financeApi } from '@/api/finance';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

const MONTHS = [
  'Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz',
  'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara',
];

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function DoctorAccountsTab() {
  const [doctorId, setDoctorId] = useState<string>('');

  const { data: pending } = useQuery({
    queryKey: ['commissions', 'pending'],
    queryFn: () => commissionsApi.getPending(),
  });
  const { data: distributed } = useQuery({
    queryKey: ['commissions', 'distributed'],
    queryFn: () => financeApi.getCommissions({ status: 'Distributed', pageSize: 500 }),
  });

  const doctors = useMemo(() => {
    const map = new Map<number, string>();
    (pending?.data.items ?? []).forEach(p => map.set(p.doctorId, p.doctorName));
    (distributed?.data.items ?? []).forEach(c => {
      if (c.doctorName) map.set(c.doctorId, c.doctorName);
    });
    return [...map.entries()].sort((a, b) => a[1].localeCompare(b[1], 'tr'));
  }, [pending, distributed]);

  const numericId = doctorId ? parseInt(doctorId, 10) : undefined;

  const { data: acc, isLoading } = useQuery({
    enabled: !!numericId,
    queryKey: ['doctor-account', numericId],
    queryFn: () => commissionsApi.getDoctorAccount(numericId!),
  });

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Hekim Cari Hesap</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="max-w-sm">
            <label className="text-sm mb-1 block text-muted-foreground">Hekim seçin</label>
            <Select value={doctorId} onValueChange={setDoctorId}>
              <SelectTrigger>
                <SelectValue placeholder={doctors.length === 0 ? 'Hekim bulunamadı' : 'Seçiniz'} />
              </SelectTrigger>
              <SelectContent>
                {doctors.map(([id, name]) => (
                  <SelectItem key={id} value={String(id)}>{name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {numericId && (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-muted-foreground">Bekleyen</CardTitle>
              </CardHeader>
              <CardContent>
                {isLoading ? <Skeleton className="h-7 w-24" /> : (
                  <div className="text-2xl font-bold text-yellow-600">
                    {formatCurrency(acc?.data.totalPending ?? 0)}
                  </div>
                )}
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-muted-foreground">Dağıtılmış</CardTitle>
              </CardHeader>
              <CardContent>
                {isLoading ? <Skeleton className="h-7 w-24" /> : (
                  <div className="text-2xl font-bold text-green-600">
                    {formatCurrency(acc?.data.totalDistributed ?? 0)}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Aylık Özet — {acc?.data.doctorName ?? ''}</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Dönem</TableHead>
                    <TableHead className="text-right">Brüt</TableHead>
                    <TableHead className="text-right">Kesintiler</TableHead>
                    <TableHead className="text-right">Hakediş</TableHead>
                    <TableHead className="text-right">Net</TableHead>
                    <TableHead className="text-center">İşlem</TableHead>
                    <TableHead>Hedef</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    Array.from({ length: 3 }).map((_, i) => (
                      <TableRow key={i}>
                        {Array.from({ length: 7 }).map((_, j) => (
                          <TableCell key={j}><Skeleton className="h-4 w-20" /></TableCell>
                        ))}
                      </TableRow>
                    ))
                  ) : (acc?.data.monthly ?? []).length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} className="text-center text-muted-foreground py-6">
                        Kayıt yok.
                      </TableCell>
                    </TableRow>
                  ) : (
                    acc!.data.monthly.map(m => (
                      <TableRow key={`${m.year}-${m.month}`}>
                        <TableCell className="font-medium">
                          {MONTHS[m.month - 1]} {m.year}
                          {m.bonusApplied && <Badge className="ml-2" variant="default">Bonus</Badge>}
                        </TableCell>
                        <TableCell className="text-right">{formatCurrency(m.totalGross)}</TableCell>
                        <TableCell className="text-right text-destructive">
                          −{formatCurrency(m.totalDeductions)}
                        </TableCell>
                        <TableCell className="text-right">{formatCurrency(m.totalCommission)}</TableCell>
                        <TableCell className="text-right font-semibold">{formatCurrency(m.totalNet)}</TableCell>
                        <TableCell className="text-center">{m.completedCount}</TableCell>
                        <TableCell>
                          {m.targetAmount != null ? (
                            <Badge variant={m.targetReached ? 'default' : 'secondary'}>
                              {formatCurrency(m.targetAmount)} {m.targetReached ? '✓' : ''}
                            </Badge>
                          ) : <span className="text-muted-foreground text-sm">—</span>}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
