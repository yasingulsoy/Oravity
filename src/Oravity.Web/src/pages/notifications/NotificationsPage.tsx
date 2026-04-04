import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Bell, BellOff, CheckCheck } from 'lucide-react';
import { notificationsApi } from '@/api/notifications';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

export function NotificationsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => notificationsApi.list(1, 50),
  });

  const markReadMutation = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const result = data?.data;
  const items = result?.items ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Bildirimler</h1>
          <p className="text-muted-foreground">
            {result ? `${result.unreadCount} okunmamış bildirim` : 'Bildirimleriniz'}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bell className="h-5 w-5" />
            Tüm Bildirimler
            {result && result.unreadCount > 0 && (
              <Badge variant="destructive" className="ml-2">{result.unreadCount}</Badge>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
            </div>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
              <BellOff className="h-10 w-10" />
              <p>Henüz bildirim yok.</p>
            </div>
          ) : (
            <div className="space-y-1">
              {items.map((n) => (
                <div
                  key={n.publicId}
                  className={cn(
                    'flex items-start gap-3 rounded-lg p-3 transition-colors',
                    !n.isRead && 'bg-accent/50',
                  )}
                >
                  <div className={cn(
                    'mt-1 h-2 w-2 shrink-0 rounded-full',
                    n.isRead ? 'bg-transparent' : n.isUrgent ? 'bg-destructive' : 'bg-primary',
                  )} />

                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium">{n.title}</span>
                      <Badge variant="outline" className="text-[10px]">{n.typeLabel}</Badge>
                      {n.isUrgent && <Badge variant="destructive" className="text-[10px]">Acil</Badge>}
                    </div>
                    <p className="text-sm text-muted-foreground">{n.message}</p>
                    <p className="text-xs text-muted-foreground">
                      {format(new Date(n.createdAt), 'dd.MM.yyyy HH:mm')}
                    </p>
                  </div>

                  {!n.isRead && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="shrink-0"
                      onClick={() => markReadMutation.mutate(n.publicId)}
                      disabled={markReadMutation.isPending}
                    >
                      <CheckCheck className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
