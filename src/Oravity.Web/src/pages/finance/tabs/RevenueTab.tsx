import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format, startOfMonth, endOfMonth } from 'date-fns';
import { CreditCard, TrendingUp, Clock, CheckCircle } from 'lucide-react';
import { financeApi } from '@/api/finance';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

const statusVariants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Paid: 'default',
  Pending: 'secondary',
  Overdue: 'destructive',
  Cancelled: 'outline',
};

const statusLabels: Record<string, string> = {
  Paid: 'Ödendi',
  Pending: 'Bekliyor',
  Overdue: 'Gecikmiş',
  Cancelled: 'İptal',
};

export function RevenueTab() {
  const now = new Date();
  const [page] = useState(1);

  const startDate = format(startOfMonth(now), 'yyyy-MM-dd');
  const endDate = format(endOfMonth(now), 'yyyy-MM-dd');

  const { data: summaryData, isLoading: summaryLoading } = useQuery({
    queryKey: ['finance', 'summary', startDate, endDate],
    queryFn: () => financeApi.getSummary(startDate, endDate),
  });

  const { data: invoicesData, isLoading: invoicesLoading } = useQuery({
    queryKey: ['finance', 'invoices', page],
    queryFn: () => financeApi.getInvoices({ page, pageSize: 20 }),
  });

  const summary = summaryData?.data;
  const invoices = invoicesData?.data.items ?? [];

  const summaryCards = [
    { title: 'Toplam Gelir', value: summary?.totalRevenue, icon: CreditCard, color: 'text-green-600' },
    { title: 'Tahsil Edilen', value: summary?.totalPaid, icon: CheckCircle, color: 'text-blue-600' },
    { title: 'Bekleyen', value: summary?.totalPending, icon: Clock, color: 'text-yellow-600' },
    { title: 'Fatura Sayısı', value: summary?.invoiceCount, icon: TrendingUp, color: 'text-purple-600', isCurrency: false },
  ];

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {summaryCards.map(({ title, value, icon: Icon, color, isCurrency = true }) => (
          <Card key={title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {title}
              </CardTitle>
              <Icon className={`h-5 w-5 ${color}`} />
            </CardHeader>
            <CardContent>
              {summaryLoading ? (
                <Skeleton className="h-8 w-20" />
              ) : (
                <div className="text-2xl font-bold">
                  {value != null
                    ? isCurrency !== false
                      ? `₺${value.toLocaleString('tr-TR')}`
                      : value.toLocaleString('tr-TR')
                    : '—'}
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Son Faturalar</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Hasta</TableHead>
                <TableHead>Tutar</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead>Vade Tarihi</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoicesLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 4 }).map((_, j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-4 w-24" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : invoices.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground">
                    Fatura bulunamadı.
                  </TableCell>
                </TableRow>
              ) : (
                invoices.map((invoice) => (
                  <TableRow key={invoice.id}>
                    <TableCell className="font-medium">{invoice.patientName}</TableCell>
                    <TableCell>₺{invoice.amount.toLocaleString('tr-TR')}</TableCell>
                    <TableCell>
                      <Badge variant={statusVariants[invoice.status] ?? 'secondary'}>
                        {statusLabels[invoice.status] ?? invoice.status}
                      </Badge>
                    </TableCell>
                    <TableCell>{invoice.dueDate}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
