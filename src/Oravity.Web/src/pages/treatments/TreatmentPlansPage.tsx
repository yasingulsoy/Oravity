import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { ClipboardList, Search } from 'lucide-react';
import { treatmentPlansApi } from '@/api/treatments';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import type { TreatmentPlan } from '@/types/treatment';

const statusConfig: Record<string, { label: string; className: string }> = {
  Draft: { label: 'Taslak', className: 'bg-gray-100 text-gray-800' },
  Approved: { label: 'Onaylandı', className: 'bg-green-100 text-green-800' },
  Completed: { label: 'Tamamlandı', className: 'bg-blue-100 text-blue-800' },
};

function formatCurrency(n: number) {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(n);
}

export function TreatmentPlansPage() {
  const [patientId, setPatientId] = useState('');
  const [searchId, setSearchId] = useState('');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['treatment-plans', searchId],
    queryFn: () => treatmentPlansApi.getByPatient(searchId),
    enabled: !!searchId,
  });

  const plans: TreatmentPlan[] = data?.data ?? [];

  const handleSearch = () => {
    if (patientId.trim()) setSearchId(patientId.trim());
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Tedavi Planları</h1>
        <p className="text-muted-foreground">Hasta tedavi planlarını görüntüleyin ve yönetin</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Hasta Ara</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Hasta ID giriniz..."
                value={patientId}
                onChange={(e) => setPatientId(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                className="pl-9"
              />
            </div>
            <button
              onClick={handleSearch}
              className="inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Ara
            </button>
          </div>
        </CardContent>
      </Card>

      {isLoading && (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-32 w-full" />)}
        </div>
      )}

      {isError && (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            Tedavi planları yüklenirken hata oluştu.
          </CardContent>
        </Card>
      )}

      {!isLoading && searchId && plans.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
            <ClipboardList className="h-10 w-10" />
            <p>Bu hasta için tedavi planı bulunamadı.</p>
          </CardContent>
        </Card>
      )}

      {plans.map((plan) => {
        const cfg = statusConfig[plan.status] ?? statusConfig.Draft;
        const completedItems = plan.items.filter((i) => i.completedAt).length;
        return (
          <Card key={plan.publicId}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <ClipboardList className="h-5 w-5" />
                  {plan.name}
                </CardTitle>
                <Badge className={cfg.className}>{cfg.label}</Badge>
              </div>
              <p className="text-sm text-muted-foreground">
                {plan.doctorName} · {format(new Date(plan.createdAt), 'dd.MM.yyyy')}
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">
                  {completedItems}/{plan.items.length} kalem tamamlandı
                </span>
                <span className="font-medium">{formatCurrency(plan.totalAmount)}</span>
              </div>

              <div className="h-2 overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all"
                  style={{ width: plan.items.length ? `${(completedItems / plan.items.length) * 100}%` : '0%' }}
                />
              </div>

              {plan.items.length > 0 && (
                <>
                  <Separator />
                  <div className="space-y-2">
                    {plan.items.map((item) => (
                      <div key={item.publicId} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2 text-sm">
                        <div className="flex items-center gap-2">
                          <span className={item.completedAt ? 'line-through text-muted-foreground' : ''}>
                            {item.treatmentName}
                          </span>
                          {item.toothNumber && (
                            <Badge variant="outline" className="text-[10px]">Diş {item.toothNumber}</Badge>
                          )}
                        </div>
                        <span className="font-medium">{formatCurrency(item.netPrice)}</span>
                      </div>
                    ))}
                  </div>
                </>
              )}

              {plan.notes && (
                <>
                  <Separator />
                  <p className="text-sm italic text-muted-foreground">{plan.notes}</p>
                </>
              )}
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
