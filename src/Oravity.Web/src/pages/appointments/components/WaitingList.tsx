import { useState, useMemo, useEffect } from 'react';
import { toast } from 'sonner';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Clock, UserCheck, LogOut, Stethoscope, WifiOff, User,
  BellRing, CheckCheck, DoorOpen, Timer, Building2, UserPen,
} from 'lucide-react';
import { getVisitStep } from '@/lib/appointmentJourney';


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
import { appointmentsApi } from '@/api/appointments';
import { DoctorSpecialDayType } from '@/types/appointment';
import { VisitStatus, type WaitingListItem } from '@/types/visit';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { CreateProtocolDialog } from './CreateProtocolDialog';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';

// ─── Priority for sorting ─────────────────────────────────────────────────────
function cardPriority(item: WaitingListItem): number {
  const s = getSection(item);
  if (s === 'calling')      return 0;
  if (s === 'protocolOpen') return 1;
  if (s === 'inRoom')       return 2;
  if (s === 'readyOut')     return 3;
  return 4;
}

// ─── Section label helper ─────────────────────────────────────────────────────
type Section = 'calling' | 'protocolOpen' | 'inRoom' | 'readyOut' | 'waiting';

function getSection(item: WaitingListItem): Section {
  if (item.isBeingCalled) return 'calling';
  if (item.status === VisitStatus.ProtocolOpened && item.hasOpenProtocol) {
    const openProtocol = item.protocols.find((p) => p.status === 1);
    return openProtocol?.startedAt ? 'inRoom' : 'protocolOpen';
  }
  if (item.status === VisitStatus.ProtocolOpened && !item.hasOpenProtocol) return 'readyOut';
  return 'waiting';
}

const SECTION_META: Record<Section, { label: string; color: string; icon: React.ElementType }> = {
  calling:      { label: 'Çağrıldı',       color: 'text-amber-600',       icon: BellRing   },
  protocolOpen: { label: 'Protokol Açık',  color: 'text-violet-600',      icon: Stethoscope },
  inRoom:       { label: 'Odada',          color: 'text-blue-600',        icon: DoorOpen   },
  readyOut:     { label: 'Çıkış Hazır',   color: 'text-emerald-600',     icon: CheckCheck },
  waiting:      { label: 'Bekliyor',       color: 'text-muted-foreground', icon: Timer      },
};

// ─── Patient card ─────────────────────────────────────────────────────────────
function PatientCard({
  item,
  onProtocolOpen,
  onCheckOut,
  onReassignDoctor,
  isCheckingOut,
}: {
  item: WaitingListItem;
  onProtocolOpen: (item: WaitingListItem) => void;
  onCheckOut: (id: string) => void;
  onReassignDoctor: (item: WaitingListItem) => void;
  isCheckingOut: boolean;
}) {
  const section = getSection(item);
  const step = getVisitStep(item.status);
  const StepIcon = step.icon;
  const age = calcAge(item.patientBirthDate);
  const gender = genderLabel(item.patientGender);
  const openProtocol = item.protocols.find((p) => p.status === 1);

  const demoParts = [
    gender === 'E' ? 'Erkek' : gender === 'K' ? 'Kadın' : null,
    age !== null ? `${age} yaş` : null,
  ].filter(Boolean);

  return (
    <div
      className={cn(
        'rounded-xl border overflow-hidden transition-all duration-300',
        section === 'calling'
          ? 'border-amber-400 ring-2 ring-amber-300/60 ring-offset-1 shadow-lg shadow-amber-100 dark:shadow-amber-900/20'
          : section === 'protocolOpen'
            ? 'border-violet-300 dark:border-violet-700 bg-violet-50/60 dark:bg-violet-950/20 shadow-sm'
            : section === 'inRoom'
              ? 'border-blue-300 dark:border-blue-700 bg-blue-50/60 dark:bg-blue-950/20 shadow-sm'
              : section === 'readyOut'
                ? 'border-emerald-300 dark:border-emerald-700 bg-emerald-50/60 dark:bg-emerald-950/20 shadow-sm'
                : 'border-border bg-card',
      )}
    >
      {/* ── "Hekim Çağırdı" banner ──────────────────────────────────────────── */}
      {item.isBeingCalled && (
        <div className="flex items-center gap-2 bg-amber-400 dark:bg-amber-500 px-3 py-1.5">
          <BellRing className="size-3.5 text-white animate-bounce shrink-0" />
          <span className="text-[11px] font-bold text-white tracking-wide flex-1">
            Hekim Çağırdı — Odaya Yönlendirin
          </span>
          <BellRing className="size-3.5 text-white animate-bounce shrink-0" />
        </div>
      )}

      {/* ── Card body ────────────────────────────────────────────────────────── */}
      <div className={cn(
        'flex flex-col gap-1.5 px-3 py-2.5',
        section === 'calling'      && 'bg-amber-50/80 dark:bg-amber-950/20',
        section === 'protocolOpen' && 'bg-violet-50/60 dark:bg-violet-950/20',
      )}>
        {/* Row 1: Name (full width, prominent) */}
        <div className="flex items-center justify-between gap-1.5">
          <span className="text-sm font-semibold leading-tight truncate">
            {item.patientName}
          </span>
          {item.isWalkIn && (
            <Badge variant="outline" className="text-[10px] px-1 py-0 leading-tight shrink-0">
              Direk
            </Badge>
          )}
        </div>

        {/* Row 2: Status pill + demographics */}
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className={cn(
            'inline-flex items-center gap-0.5 text-[10px] font-medium px-1.5 py-0.5 rounded-full shrink-0',
            section === 'calling'
              ? 'bg-amber-100 dark:bg-amber-900/50 text-amber-700 dark:text-amber-400'
              : section === 'protocolOpen'
                ? 'bg-violet-100 dark:bg-violet-900/50 text-violet-700 dark:text-violet-400'
                : section === 'inRoom'
                  ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-400'
                  : section === 'readyOut'
                    ? 'bg-emerald-100 dark:bg-emerald-900/50 text-emerald-700 dark:text-emerald-400'
                    : 'bg-muted text-muted-foreground',
          )}>
            <StepIcon className={cn('size-2.5', section === 'calling' && 'animate-pulse')} />
            {step.label}
          </span>
          {demoParts.length > 0 && (
            <span className="text-[11px] text-muted-foreground flex items-center gap-1">
              <User className="size-2.5" />
              {demoParts.join(', ')}
            </span>
          )}
          {item.branchName && (
            <span className="text-[11px] text-muted-foreground flex items-center gap-1 min-w-0">
              <Building2 className="size-2.5 shrink-0" />
              <span className="truncate">{item.branchName}</span>
            </span>
          )}
        </div>

        {/* Row 3: Times — grid layout, no overlap */}
        <div className="grid grid-cols-[auto_1fr] gap-x-2 gap-y-0.5 text-[11px] mt-0.5">
          <span className="text-muted-foreground flex items-center gap-1">
            <Clock className="size-3" />Giriş
          </span>
          <span className="font-medium tabular-nums">{format(new Date(item.checkInAt), 'HH:mm')}</span>

          <span className="text-muted-foreground">Bekleme</span>
          <span className={cn(
            'font-semibold tabular-nums flex items-center gap-0.5',
            item.waitingMinutes > 30
              ? 'text-red-500'
              : item.waitingMinutes > 15
                ? 'text-amber-500'
                : '',
          )}>
            {item.waitingMinutes > 30 && <span className="animate-pulse">●</span>}
            {item.waitingMinutes} dk
          </span>

          {item.appointmentTime && (
            <>
              <span className="text-muted-foreground">Randevu</span>
              <span className="font-medium tabular-nums">{item.appointmentTime}</span>
            </>
          )}
        </div>

        {/* Row 4: Open protocol info */}
        {openProtocol && (
          <div className="flex items-center gap-1.5 rounded-md bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 px-2 py-1 text-[11px]">
            <Stethoscope className="size-3 text-blue-500 shrink-0" />
            <span className="font-medium text-blue-700 dark:text-blue-300 truncate">
              {openProtocol.typeName}
            </span>
            <span className="text-muted-foreground truncate ml-auto text-[10px]">
              {openProtocol.doctorName}
            </span>
          </div>
        )}

        {/* Row 5: Actions */}
        <div className="flex gap-1 mt-0.5">
          {(item.status === VisitStatus.Waiting ||
            (item.status === VisitStatus.ProtocolOpened && !item.hasOpenProtocol)) && (
            <Button
              size="sm"
              variant={section === 'calling' ? 'default' : 'secondary'}
              className={cn(
                'h-6 text-[11px] px-2 flex-1',
                section === 'calling' && 'bg-amber-500 hover:bg-amber-600 text-white',
              )}
              onClick={() => onProtocolOpen(item)}
            >
              <Stethoscope className="size-3 mr-1" />
              Protokol Aç
            </Button>
          )}
          {!item.isWalkIn && !!item.appointmentDoctorId && item.status === VisitStatus.Waiting && (
            <Button
              size="sm"
              variant="outline"
              className="h-6 text-[11px] px-2 shrink-0"
              title="Randevu hekimini değiştir"
              onClick={() => onReassignDoctor(item)}
            >
              <UserPen className="size-3" />
            </Button>
          )}
          {item.status === VisitStatus.ProtocolOpened && (
            <Button
              size="sm"
              variant="outline"
              className={cn(
                'h-6 text-[11px] px-2 flex-1 disabled:opacity-50',
                section === 'readyOut'
                  ? 'border-emerald-400 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-950/30'
                  : 'border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/30',
              )}
              disabled={isCheckingOut || item.hasOpenProtocol}
              title={item.hasOpenProtocol ? 'Açık protokol tamamlanmadan taburcu edilemez' : undefined}
              onClick={() => onCheckOut(item.publicId)}
            >
              <LogOut className="size-3 mr-1" />
              Klinikten Çıktı
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Section header ───────────────────────────────────────────────────────────
function SectionHeader({ section, count }: { section: Section; count: number }) {
  const meta = SECTION_META[section];
  const Icon = meta.icon;
  return (
    <div className="flex items-center gap-1.5 px-1 pt-1 pb-0.5">
      <Icon className={cn('size-3', meta.color)} />
      <span className={cn('text-[10px] font-bold uppercase tracking-widest', meta.color)}>
        {meta.label}
      </span>
      <span className="ml-auto text-[10px] text-muted-foreground tabular-nums">{count}</span>
    </div>
  );
}

// ─── Main component ───────────────────────────────────────────────────────────
export function WaitingList() {
  const queryClient = useQueryClient();
  const [protocolTarget, setProtocolTarget] = useState<WaitingListItem | null>(null);
  const [reassignTarget, setReassignTarget] = useState<WaitingListItem | null>(null);

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
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      toast.error(msg ?? 'Taburcu işlemi başarısız.');
    },
  });

  const reassignMutation = useMutation({
    mutationFn: ({ visitId, doctorId }: { visitId: string; doctorId: number }) =>
      visitsApi.reassignDoctor(visitId, doctorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      setReassignTarget(null);
    },
  });

  const handleProtocolOpen = (item: WaitingListItem) => {
    setProtocolTarget(item);
  };

  const items = data ?? [];
  const sorted = [...items].sort((a, b) => cardPriority(a) - cardPriority(b));

  // Group by section (preserving sorted order within section)
  const SECTION_ORDER: Section[] = ['calling', 'protocolOpen', 'inRoom', 'readyOut', 'waiting'];
  const groups = SECTION_ORDER.reduce<Record<Section, WaitingListItem[]>>(
    (acc, s) => ({ ...acc, [s]: sorted.filter((i) => getSection(i) === s) }),
    { calling: [], protocolOpen: [], inRoom: [], readyOut: [], waiting: [] },
  );
  const activeGroups = SECTION_ORDER.filter((s) => groups[s].length > 0);

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
            <div className="flex items-center gap-1.5">
              {groups.calling.length > 0 && (
                <span className="inline-flex items-center gap-0.5 text-[10px] font-bold text-amber-600 bg-amber-100 dark:bg-amber-900/40 px-1.5 py-0.5 rounded-full animate-pulse">
                  <BellRing className="size-2.5" />
                  {groups.calling.length}
                </span>
              )}
              <Badge variant="secondary" className="text-xs tabular-nums">
                {items.length}
              </Badge>
            </div>
          )}
        </div>

        {/* List */}
        <div className="flex-1 overflow-y-auto min-h-0">
          {isLoading ? (
            <div className="space-y-2 p-2">
              {[...Array(4)].map((_, i) => (
                <Skeleton key={i} className="h-24 w-full rounded-xl" />
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
            <div className="space-y-1 p-2">
              {activeGroups.map((section) => (
                <div key={section} className="space-y-1">
                  <SectionHeader section={section} count={groups[section].length} />
                  {groups[section].map((item) => (
                    <PatientCard
                      key={item.publicId}
                      item={item}
                      onProtocolOpen={handleProtocolOpen}
                      onCheckOut={(id) => checkOutMutation.mutate(id)}
                      onReassignDoctor={setReassignTarget}
                      isCheckingOut={checkOutMutation.isPending}
                    />
                  ))}
                </div>
              ))}
            </div>
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

      {/* Hekimi Değiştir dialog'u */}
      {reassignTarget && (
        <ReassignDoctorDialog
          item={reassignTarget}
          isPending={reassignMutation.isPending}
          error={reassignMutation.isError ? ((reassignMutation.error as Error)?.message ?? 'Hata oluştu.') : null}
          onClose={() => { reassignMutation.reset(); setReassignTarget(null); }}
          onConfirm={(newDoctorId) => reassignMutation.mutate({ visitId: reassignTarget.publicId, doctorId: newDoctorId })}
        />
      )}
    </>
  );
}

// ─── Hekim Değiştir Dialog ────────────────────────────────────────────────────
function ReassignDoctorDialog({
  item,
  isPending,
  error,
  onClose,
  onConfirm,
}: {
  item: WaitingListItem;
  isPending: boolean;
  error: string | null;
  onClose: () => void;
  onConfirm: (newDoctorId: number) => void;
}) {
  const today = useMemo(() => new Date().toISOString().slice(0, 10), []);
  const [selectedDoctorId, setSelectedDoctorId] = useState<number | ''>('');

  const { data: doctorsRaw, isLoading } = useQuery({
    queryKey: ['appointments', 'calendar-doctors', today],
    queryFn: () => appointmentsApi.getCalendarDoctors({ date: today }),
    select: (res) => {
      const seen = new Set<number>();
      return (res.data ?? []).filter((d) => {
        if (d.isSpecialDay && d.specialDayType === DoctorSpecialDayType.DayOff) return false;
        if (!d.workStart) return false;
        if (seen.has(d.doctorId)) return false;
        seen.add(d.doctorId);
        return true;
      });
    },
    staleTime: 5 * 60 * 1000,
  });

  const doctors = useMemo(
    () => (doctorsRaw ?? []).filter((d) => d.doctorId !== item.appointmentDoctorId),
    [doctorsRaw, item.appointmentDoctorId],
  );

  useEffect(() => {
    setSelectedDoctorId('');
  }, [item.publicId]);

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Hekimi Değiştir</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="rounded-md bg-muted px-3 py-2 text-sm">
            <p className="font-medium">{item.patientName}</p>
            <p className="text-xs text-muted-foreground">
              Randevu saati: {item.appointmentTime ?? '—'}
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium">Yeni Hekim</label>
            {isLoading ? (
              <p className="text-xs text-muted-foreground">Yükleniyor...</p>
            ) : doctors.length === 0 ? (
              <p className="text-xs text-destructive">Bugün başka çalışan hekim yok.</p>
            ) : (
              <select
                value={selectedDoctorId}
                onChange={(e) => setSelectedDoctorId(e.target.value ? Number(e.target.value) : '')}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
              >
                <option value="">— Hekim seçin —</option>
                {doctors.map((d) => (
                  <option key={d.doctorId} value={d.doctorId}>
                    {d.title ? `${d.title} ` : ''}{d.fullName}
                    {d.workStart && d.workEnd
                      ? ` (${d.workStart.slice(0, 5)}–${d.workEnd.slice(0, 5)})`
                      : ''}
                  </option>
                ))}
              </select>
            )}
          </div>

          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isPending}>Vazgeç</Button>
          <Button
            disabled={!selectedDoctorId || isPending}
            onClick={() => selectedDoctorId && onConfirm(Number(selectedDoctorId))}
          >
            {isPending ? 'Değiştiriliyor...' : 'Hekimi Değiştir'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
