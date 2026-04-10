import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Clock, UserCheck, LogOut, Stethoscope, WifiOff } from 'lucide-react';
import { format } from 'date-fns';
import { visitsApi } from '@/api/visits';
import { VisitStatus, type WaitingListItem } from '@/types/visit';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { CreateProtocolDialog } from './CreateProtocolDialog';

export function WaitingList() {
  const queryClient = useQueryClient();
  const [protocolTarget, setProtocolTarget] = useState<WaitingListItem | null>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['visits', 'waiting'],
    queryFn: () => visitsApi.getWaitingList(),
    select: (res) => res.data ?? [],
    refetchInterval: 30_000,
  });

  const checkOutMutation = useMutation({
    mutationFn: (publicId: string) => visitsApi.checkOut(publicId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const items = data ?? [];

  return (
    <>
      <div className="flex flex-col h-full min-h-0">
        {/* Panel header */}
        <div className="flex items-center justify-between px-3 py-2 border-b bg-muted/40 shrink-0">
          <div className="flex items-center gap-2">
            <UserCheck className="size-4 text-muted-foreground" />
            <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Bekleme
            </span>
          </div>
          {!isLoading && (
            <Badge variant="secondary" className="text-xs tabular-nums">
              {items.length}
            </Badge>
          )}
        </div>

        {/* List */}
        <div className="flex-1 overflow-y-auto min-h-0">
          {isLoading ? (
            <div className="space-y-2 p-2">
              {[...Array(4)].map((_, i) => (
                <Skeleton key={i} className="h-20 w-full rounded-md" />
              ))}
            </div>
          ) : isError ? (
            <div className="flex flex-col items-center justify-center h-full gap-2 text-muted-foreground p-4">
              <WifiOff className="size-8" />
              <span className="text-xs text-center">Bekleme listesi yüklenemedi</span>
            </div>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full gap-2 text-muted-foreground p-4">
              <Clock className="size-8" />
              <span className="text-xs text-center">Bekleyen hasta yok</span>
            </div>
          ) : (
            <ul className="divide-y">
              {items.map((item) => (
                <li
                  key={item.publicId}
                  className={cn(
                    'flex flex-col gap-1.5 px-3 py-2.5',
                    item.status === VisitStatus.ProtocolOpened && 'bg-blue-50 dark:bg-blue-950/20',
                  )}
                >
                  {/* Name + badges */}
                  <div className="flex items-start justify-between gap-1">
                    <span className="text-sm font-medium leading-tight line-clamp-1">
                      {item.patientName}
                    </span>
                    <div className="flex gap-1 shrink-0">
                      {item.isWalkIn && (
                        <Badge variant="outline" className="text-[10px] px-1 py-0 leading-tight">
                          Direk
                        </Badge>
                      )}
                      {item.hasOpenProtocol && (
                        <Badge className="text-[10px] px-1 py-0 leading-tight bg-blue-500">
                          <Stethoscope className="size-2.5 mr-0.5" />
                          Protokol
                        </Badge>
                      )}
                    </div>
                  </div>

                  {/* Meta row */}
                  <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                    <span className="flex items-center gap-0.5">
                      <Clock className="size-3" />
                      {format(new Date(item.checkInAt), 'HH:mm')}
                    </span>
                    <span
                      className={cn(
                        'font-medium',
                        item.waitingMinutes > 30
                          ? 'text-red-500'
                          : item.waitingMinutes > 15
                            ? 'text-amber-500'
                            : 'text-muted-foreground',
                      )}
                    >
                      {item.waitingMinutes}dk
                    </span>
                    {item.appointmentTime && (
                      <span className="ml-auto">R: {item.appointmentTime}</span>
                    )}
                  </div>

                  {/* Actions */}
                  <div className="flex gap-1 pt-0.5">
                    {!item.hasOpenProtocol && item.status === VisitStatus.Waiting && (
                      <Button
                        size="sm"
                        variant="secondary"
                        className="h-6 text-[11px] px-2 flex-1"
                        onClick={() => setProtocolTarget(item)}
                      >
                        <Stethoscope className="size-3 mr-1" />
                        Protokol Aç
                      </Button>
                    )}
                    {item.status === VisitStatus.ProtocolOpened && (
                      <Button
                        size="sm"
                        variant="outline"
                        className="h-6 text-[11px] px-2 flex-1 border-blue-300 text-blue-700 hover:bg-blue-50"
                        disabled={checkOutMutation.isPending}
                        onClick={() => checkOutMutation.mutate(item.publicId)}
                      >
                        <LogOut className="size-3 mr-1" />
                        Taburcu
                      </Button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Protokol açma dialog'u */}
      {protocolTarget && (
        <CreateProtocolDialog
          open={!!protocolTarget}
          visitPublicId={protocolTarget.publicId}
          patientName={protocolTarget.patientName}
          checkInAt={protocolTarget.checkInAt}
          defaultDoctorId={protocolTarget.appointmentDoctorId}
          defaultSpecializationId={protocolTarget.appointmentSpecializationId}
          onClose={() => setProtocolTarget(null)}
          onSuccess={() => setProtocolTarget(null)}
        />
      )}
    </>
  );
}
