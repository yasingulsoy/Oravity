import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Clock, UserCheck, LogOut, Stethoscope, WifiOff, User, Bell } from 'lucide-react';
import { getVisitStep } from '@/lib/appointmentJourney';
import {
  Tooltip, TooltipContent, TooltipProvider, TooltipTrigger,
} from '@/components/ui/tooltip';

function ProtocolRow({ protocol: p }: { protocol: WaitingProtocolItem }) {
  const isOpen = p.status === 1;
  return (
    <li className="flex items-center gap-1.5 py-0.5">
      <span
        className="size-2 rounded-full shrink-0"
        style={{ backgroundColor: p.typeColor }}
      />
      <span className="text-[11px] text-muted-foreground font-mono leading-none">
        {p.protocolNo}
      </span>
      <span className="text-[11px] leading-none truncate" style={{ color: p.typeColor }}>
        {p.typeName}
      </span>
      <span className="text-[10px] leading-none text-muted-foreground truncate ml-auto">
        {p.doctorName.split(' ').pop()}
      </span>
      <span
        className={cn(
          'text-[9px] px-1 py-0.5 rounded leading-none font-medium shrink-0',
          isOpen
            ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-400'
            : 'bg-green-100 dark:bg-green-900/50 text-green-700 dark:text-green-400',
        )}
      >
        {isOpen ? 'Açık' : 'Tamam'}
      </span>
    </li>
  );
}

function calcAge(birthDate?: string | null): number | null {
  if (!birthDate) return null;
  const today = new Date();
  const bd = new Date(birthDate);
  let age = today.getFullYear() - bd.getFullYear();
  if (
    today.getMonth() < bd.getMonth() ||
    (today.getMonth() === bd.getMonth() && today.getDate() < bd.getDate())
  ) age--;
  return age;
}

function genderLabel(gender?: string | null): string {
  if (!gender) return '';
  const g = gender.toLowerCase();
  if (g === 'male' || g === 'erkek' || g === 'm') return 'E';
  if (g === 'female' || g === 'kadın' || g === 'kadin' || g === 'f') return 'K';
  return '';
}
import { format } from 'date-fns';
import { visitsApi } from '@/api/visits';
import { VisitStatus, type WaitingListItem, type WaitingProtocolItem } from '@/types/visit';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { CreateProtocolDialog } from './CreateProtocolDialog';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';

type ProtocolGuardState =
  | { type: 'confirm'; item: WaitingListItem; openProtocol: WaitingProtocolItem }
  | { type: 'block';   item: WaitingListItem; openProtocol: WaitingProtocolItem };

export function WaitingList() {
  const queryClient = useQueryClient();
  const [protocolTarget, setProtocolTarget] = useState<WaitingListItem | null>(null);
  const [guard, setGuard] = useState<ProtocolGuardState | null>(null);

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
                    {/* Left: journey icon + name + age */}
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-1.5">
                        {(() => {
                          const step = getVisitStep(item.status);
                          const StepIcon = step.icon;
                          return (
                            <TooltipProvider delayDuration={200}>
                              <Tooltip>
                                <TooltipTrigger>
                                  <span className="shrink-0">
                                    <StepIcon className={cn('size-3.5', step.color)} />
                                  </span>
                                </TooltipTrigger>
                                <TooltipContent side="right" className="text-xs">
                                  {step.label}
                                </TooltipContent>
                              </Tooltip>
                            </TooltipProvider>
                          );
                        })()}
                        <span className="text-sm font-medium leading-tight line-clamp-1">
                          {item.patientName}
                        </span>
                        {item.isBeingCalled && (
                          <Bell className="size-3.5 text-emerald-500 shrink-0 animate-pulse" />
                        )}
                      </div>
                      {(() => {
                        const age = calcAge(item.patientBirthDate);
                        const gender = genderLabel(item.patientGender);
                        const parts = [gender, age !== null ? `${age} yaş` : null].filter(Boolean);
                        return parts.length > 0 ? (
                          <div className="flex items-center gap-1 text-[11px] text-muted-foreground mt-0.5">
                            <User className="size-2.5" />
                            {parts.join(' · ')}
                          </div>
                        ) : null;
                      })()}
                    </div>
                    {/* Right: badges */}
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
                    {/* Protokol Aç — visit aktifken her zaman görünür */}
                    {(item.status === VisitStatus.Waiting || item.status === VisitStatus.ProtocolOpened) && (
                      <Button
                        size="sm"
                        variant="secondary"
                        className="h-6 text-[11px] px-2 flex-1"
                        onClick={() => {
                          const openProtocol = item.protocols.find((p) => p.status === 1);
                          if (!openProtocol) {
                            setProtocolTarget(item);
                          } else if (openProtocol.diagnosis?.trim()) {
                            setGuard({ type: 'confirm', item, openProtocol });
                          } else {
                            setGuard({ type: 'block', item, openProtocol });
                          }
                        }}
                      >
                        <Stethoscope className="size-3 mr-1" />
                        Protokol Aç
                      </Button>
                    )}
                    {/* Taburcu — open protokol yoksa aktif */}
                    {item.status === VisitStatus.ProtocolOpened && (
                      <Button
                        size="sm"
                        variant="outline"
                        className="h-6 text-[11px] px-2 flex-1 border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/30 disabled:opacity-50"
                        disabled={item.hasOpenProtocol || checkOutMutation.isPending}
                        title={item.hasOpenProtocol ? 'Önce açık protokolleri tamamlayın' : undefined}
                        onClick={() => checkOutMutation.mutate(item.publicId)}
                      >
                        <LogOut className="size-3 mr-1" />
                        Taburcu
                      </Button>
                    )}
                  </div>

                  {/* Protokol tree */}
                  {item.protocols.length > 0 && (
                    <ul className="mt-1 space-y-0.5 border-l-2 border-muted ml-1 pl-2">
                      {item.protocols.map((p) => (
                        <ProtocolRow key={p.publicId} protocol={p} />
                      ))}
                    </ul>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Guard: onay */}
      {guard?.type === 'confirm' && (
        <Dialog open onOpenChange={() => setGuard(null)}>
          <DialogContent className="sm:max-w-sm">
            <DialogHeader>
              <DialogTitle>Protokol Aç</DialogTitle>
            </DialogHeader>
            <p className="text-sm text-muted-foreground">
              Hastanın{' '}
              <span className="font-medium text-foreground">
                {guard.openProtocol.doctorName}
              </span>{' '}
              hekiminde{' '}
              <span className="font-medium text-foreground">
                {guard.openProtocol.protocolNo}
              </span>{' '}
              numaralı açık bir protokolü bulunmaktadır. Yeni protokol açmak istediğinize emin misiniz?
            </p>
            <DialogFooter className="gap-2">
              <Button variant="outline" onClick={() => setGuard(null)}>Vazgeç</Button>
              <Button onClick={() => { setProtocolTarget(guard.item); setGuard(null); }}>
                Evet, Aç
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Guard: engelle */}
      {guard?.type === 'block' && (
        <Dialog open onOpenChange={() => setGuard(null)}>
          <DialogContent className="sm:max-w-sm">
            <DialogHeader>
              <DialogTitle>Protokol Açılamaz</DialogTitle>
            </DialogHeader>
            <p className="text-sm text-muted-foreground">
              Lütfen{' '}
              <span className="font-medium text-foreground">
                {guard.openProtocol.doctorName}
              </span>{' '}
              hekiminde açık olan{' '}
              <span className="font-medium text-foreground">
                {guard.openProtocol.protocolNo}
              </span>{' '}
              numaralı protokolün kapatılmasını sağlayınız. Bu şekilde yeni bir protokol açabilirsiniz.
            </p>
            <DialogFooter>
              <Button onClick={() => setGuard(null)}>Tamam</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Protokol açma dialog'u — dışarıda render edilmeli (portal için) */}
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
