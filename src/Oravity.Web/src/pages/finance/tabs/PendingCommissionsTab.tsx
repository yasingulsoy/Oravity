import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { commissionsApi, type PendingCommission } from '@/api/commissions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

function formatCurrency(n: number) {
  return `₺${n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function PendingCommissionsTab() {
  const qc = useQueryClient();
  const [selected, setSelected] = useState<Set<number>>(new Set());

  const { data, isLoading } = useQuery({
    queryKey: ['commissions', 'pending'],
    queryFn: () => commissionsApi.getPending(),
  });

  const items: PendingCommission[] = data?.data.items ?? [];
  const totalNet = data?.data.totalNet ?? 0;

  const allSelected = items.length > 0 && selected.size === items.length;
  const selectedNet = useMemo(
    () => items.filter(i => selected.has(i.id)).reduce((s, i) => s + i.netCommissionAmount, 0),
    [items, selected]
  );

  const toggleAll = () => {
    if (allSelected) setSelected(new Set());
    else setSelected(new Set(items.map(i => i.id)));
  };

  const toggleOne = (id: number) => {
    const next = new Set(selected);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    setSelected(next);
  };

  const distribute = useMutation({
    mutationFn: (ids: number[]) => commissionsApi.distributeBatch(ids),
    onSuccess: (res) => {
      const r = res.data;
      toast.success(`${r.distributed} hakediş dağıtıldı — ${formatCurrency(r.totalAmount)}`);
      if (r.warnings.length > 0) {
        r.warnings.forEach(w => toast.warning(w));
      }
      setSelected(new Set());
      qc.invalidateQueries({ queryKey: ['commissions'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Bekleyen Hakedişler</CardTitle>
          <p className="text-sm text-muted-foreground">
            Seçili: {selected.size} — {formatCurrency(selectedNet)} · Toplam: {items.length} — {formatCurrency(totalNet)}
          </p>
        </div>
        <Button
          disabled={selected.size === 0 || distribute.isPending}
          onClick={() => distribute.mutate([...selected])}
        >
          Seçilileri Dağıt
        </Button>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">
                <Checkbox checked={allSelected} onCheckedChange={toggleAll} />
              </TableHead>
              <TableHead>Hekim</TableHead>
              <TableHead>Tedavi</TableHead>
              <TableHead>Dönem</TableHead>
              <TableHead className="text-right">Brüt</TableHead>
              <TableHead className="text-right">Net Baz</TableHead>
              <TableHead className="text-right">Oran</TableHead>
              <TableHead className="text-right">Net Hakediş</TableHead>
              <TableHead>Bonus</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 9 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-20" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={9} className="text-center text-muted-foreground py-6">
                  Bekleyen hakediş yok.
                </TableCell>
              </TableRow>
            ) : (
              items.map((i) => (
                <TableRow key={i.id}>
                  <TableCell>
                    <Checkbox
                      checked={selected.has(i.id)}
                      onCheckedChange={() => toggleOne(i.id)}
                    />
                  </TableCell>
                  <TableCell className="font-medium">{i.doctorName}</TableCell>
                  <TableCell>{i.treatmentName ?? '—'}</TableCell>
                  <TableCell>{String(i.periodMonth).padStart(2, '0')}/{i.periodYear}</TableCell>
                  <TableCell className="text-right">{formatCurrency(i.grossAmount)}</TableCell>
                  <TableCell className="text-right">{formatCurrency(i.netBaseAmount)}</TableCell>
                  <TableCell className="text-right">%{(i.commissionRate * 100).toFixed(1)}</TableCell>
                  <TableCell className="text-right font-semibold">{formatCurrency(i.netCommissionAmount)}</TableCell>
                  <TableCell>
                    {i.bonusApplied ? <Badge variant="default">Bonus</Badge> : <span className="text-muted-foreground">—</span>}
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
