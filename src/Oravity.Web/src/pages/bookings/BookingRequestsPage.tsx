import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { CalendarPlus, Check, X } from 'lucide-react';
import { bookingsApi } from '@/api/bookings';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import type { BookingRequest } from '@/types/booking';

const statusConfig: Record<string, { label: string; className: string }> = {
  Pending: { label: 'Bekliyor', className: 'bg-yellow-100 text-yellow-800' },
  Approved: { label: 'Onaylandı', className: 'bg-green-100 text-green-800' },
  Rejected: { label: 'Reddedildi', className: 'bg-red-100 text-red-800' },
};

export function BookingRequestsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['booking-requests'],
    queryFn: () => bookingsApi.list(),
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => bookingsApi.approve(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['booking-requests'] }),
  });

  const rejectMutation = useMutation({
    mutationFn: (id: string) => bookingsApi.reject(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['booking-requests'] }),
  });

  const items: BookingRequest[] = data?.data?.items ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Online Randevu Talepleri</h1>
        <p className="text-muted-foreground">Web sitesinden gelen randevu taleplerini yönetin</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarPlus className="h-5 w-5" />
            Talepler
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
            </div>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
              <CalendarPlus className="h-10 w-10" />
              <p>Bekleyen randevu talebi yok.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {items.map((req) => {
                const cfg = statusConfig[req.status] ?? statusConfig.Pending;
                return (
                  <div key={req.id} className="flex items-center gap-4 rounded-lg border p-4">
                    <div className="flex-1 space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{req.patientName}</span>
                        <Badge className={cfg.className}>{cfg.label}</Badge>
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {req.doctorName} · {req.requestedDate} {req.requestedTime}
                      </div>
                      <div className="text-sm text-muted-foreground">{req.patientPhone}</div>
                      {req.notes && (
                        <p className="text-sm italic text-muted-foreground">"{req.notes}"</p>
                      )}
                      <p className="text-xs text-muted-foreground">
                        Talep: {format(new Date(req.createdAt), 'dd.MM.yyyy HH:mm')}
                      </p>
                    </div>

                    {req.status === 'Pending' && (
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          onClick={() => approveMutation.mutate(req.id)}
                          disabled={approveMutation.isPending}
                        >
                          <Check className="mr-1 h-4 w-4" />
                          Onayla
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => rejectMutation.mutate(req.id)}
                          disabled={rejectMutation.isPending}
                        >
                          <X className="mr-1 h-4 w-4" />
                          Reddet
                        </Button>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
