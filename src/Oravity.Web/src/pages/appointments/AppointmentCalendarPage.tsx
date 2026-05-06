import { useState, useCallback, useMemo, useEffect } from 'react';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { format, addDays, subDays } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  ChevronLeft, ChevronRight,
  CalendarDays, PhoneCall, UserCheck, Stethoscope, CheckCheck,
  XCircle, Ban, LogOut, Trash2, ExternalLink,
} from 'lucide-react';
import { appointmentsApi } from '@/api/appointments';
import { useCalendarSocket } from '@/hooks/useCalendarSocket';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { APPOINTMENT_STATUS_COLORS } from '@/lib/constants';
import { cn } from '@/lib/utils';
import { MultiSelect } from './components/MultiSelect';
import { CalendarGrid } from './components/CalendarGrid';
import { PatientSearchModal } from './components/PatientSearchModal';
import { WaitingList } from './components/WaitingList';
import type { Appointment, DoctorCalendarInfo, CalendarSettings } from '@/types/appointment';

interface SelectedRange {
  doctorId: number;
  doctorName: string;
  branchId: number;
  startTime: string;
  endTime: string;
  date: Date;
  dayStartHour: number;
  dayEndHour: number;
}

// ─── Appointment Journey Tracker ──────────────────────────────────────────

const JOURNEY_STEPS = [
  { id: 1, label: 'Planlandı',   icon: CalendarDays  },
  { id: 2, label: 'Onaylandı',   icon: PhoneCall     },
  { id: 3, label: 'Geldi',       icon: UserCheck     },
  { id: 5, label: 'Odada',       icon: Stethoscope   },
  { id: 7, label: 'Tamamlandı',  icon: CheckCheck    },
] as const;

// Terminal durumlar: adım sırası yok, özel badge gösterilir
const TERMINAL: Record<number, { label: string; icon: typeof XCircle; color: string }> = {
  6: { label: 'İptal',   icon: Ban,     color: 'text-red-500'    },
  8: { label: 'Gelmedi', icon: XCircle, color: 'text-orange-500' },
  4: { label: 'Ayrıldı', icon: LogOut,  color: 'text-slate-500'  },
};

function AppointmentJourney({ statusId }: { statusId: number }) {
  const terminal = TERMINAL[statusId];

  // Terminal durumda son tamamlanan adımı bul
  const currentStepIndex = JOURNEY_STEPS.findIndex((s) => s.id === statusId);

  if (terminal) {
    // Kaçıncı adıma kadar gelindi — 4 (Ayrıldı) = Odada (index 3) tamamlandı, 8 = sadece Planlandı, 6 = hiçbiri
    const lastCompleted = statusId === 4 ? 3 : statusId === 8 ? 0 : -1;
    const TerminalIcon = terminal.icon;
    return (
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          {JOURNEY_STEPS.map((step, i) => {
            const Icon = step.icon;
            const done = i <= lastCompleted;
            return (
              <div key={step.id} className="flex items-center gap-1 flex-1 min-w-0">
                <div className={cn(
                  'h-5 w-5 rounded-full flex items-center justify-center shrink-0',
                  done ? 'bg-emerald-500' : 'bg-muted',
                )}>
                  <Icon className={cn('h-3 w-3', done ? 'text-white' : 'text-muted-foreground')} />
                </div>
                {i < JOURNEY_STEPS.length - 1 && (
                  <div className={cn('h-px flex-1', done ? 'bg-emerald-400' : 'bg-border')} />
                )}
              </div>
            );
          })}
        </div>
        <div className={cn('flex items-center gap-1.5 text-xs font-medium', terminal.color)}>
          <TerminalIcon className="h-3.5 w-3.5" />
          {terminal.label}
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1">
      {JOURNEY_STEPS.map((step, i) => {
        const Icon = step.icon;
        const done    = i < currentStepIndex;
        const current = i === currentStepIndex;
        const future  = i > currentStepIndex;

        return (
          <div key={step.id} className="flex items-center gap-1 flex-1 min-w-0">
            <div className="flex flex-col items-center gap-0.5 shrink-0">
              <div className={cn(
                'h-6 w-6 rounded-full flex items-center justify-center transition-colors',
                done    && 'bg-emerald-500',
                current && 'bg-primary ring-2 ring-primary/30',
                future  && 'bg-muted',
              )}>
                <Icon className={cn(
                  'h-3.5 w-3.5',
                  done    && 'text-white',
                  current && 'text-primary-foreground',
                  future  && 'text-muted-foreground/50',
                )} />
              </div>
              <span className={cn(
                'text-[10px] leading-none text-center whitespace-nowrap',
                done    && 'text-emerald-600 dark:text-emerald-400',
                current && 'text-primary font-semibold',
                future  && 'text-muted-foreground/50',
              )}>
                {step.label}
              </span>
            </div>
            {i < JOURNEY_STEPS.length - 1 && (
              <div className={cn(
                'h-px flex-1 mb-3 transition-colors',
                done ? 'bg-emerald-400' : 'bg-border',
              )} />
            )}
          </div>
        );
      })}
    </div>
  );
}

export function AppointmentCalendarPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  // --- Date navigation ---
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const dateStr = format(currentDate, 'yyyy-MM-dd');

  // --- Filters ---
  const [selectedBranchIds, setSelectedBranchIds] = useState<number[]>([]);
  const [selectedSpecIds, setSelectedSpecIds] = useState<number[]>([]);
  const [selectedDoctorIds, setSelectedDoctorIds] = useState<number[]>([]);

  // --- Dialogs ---
  const [selectedRange, setSelectedRange] = useState<SelectedRange | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState<Appointment | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [pendingStatusId, setPendingStatusId] = useState<number | null>(null);

  // --- Queries ---

  const { data: branchesData } = useQuery({
    queryKey: ['appointments', 'accessible-branches'],
    queryFn: () => appointmentsApi.getAccessibleBranches(),
    staleTime: 5 * 60 * 1000,
    select: (res) => res.data ?? [],
  });
  const branches = branchesData ?? [];

  useEffect(() => {
    if (branches.length > 0) setSelectedBranchIds(branches.map((b) => b.id));
  }, [branches.length]); // eslint-disable-line react-hooks/exhaustive-deps

  const { data: specsData } = useQuery({
    queryKey: ['appointments', 'specializations'],
    queryFn: () => appointmentsApi.getSpecializations(),
    staleTime: 5 * 60 * 1000,
    select: (res) => res.data ?? [],
  });
  const specializations = specsData ?? [];

  useEffect(() => {
    if (specializations.length > 0) setSelectedSpecIds(specializations.map((s) => s.id));
  }, [specializations.length]); // eslint-disable-line react-hooks/exhaustive-deps

  const { data: statusesData } = useQuery({
    queryKey: ['appointments', 'statuses'],
    queryFn: () => appointmentsApi.getStatuses(),
    staleTime: Infinity,
    select: (res) => res.data ?? [],
  });
  const statuses = statusesData ?? [];

  const { data: calendarSettings } = useQuery({
    queryKey: ['appointments', 'calendar-settings'],
    queryFn: () => appointmentsApi.getCalendarSettings(),
    staleTime: 5 * 60 * 1000,
    select: (res) => res.data ?? ({ slotIntervalMinutes: 30, dayStartHour: 8, dayEndHour: 20 } as CalendarSettings),
  });
  const settings: CalendarSettings = calendarSettings ?? { slotIntervalMinutes: 30, dayStartHour: 8, dayEndHour: 20 };

  const { data: doctorsData, isLoading: doctorsLoading } = useQuery({
    queryKey: ['appointments', 'calendar-doctors', dateStr, selectedBranchIds, selectedSpecIds],
    queryFn: () => appointmentsApi.getCalendarDoctors({
      date: dateStr,
      branchIds: selectedBranchIds.length > 0 && selectedBranchIds.length < branches.length
        ? selectedBranchIds
        : undefined,
      specializationIds: selectedSpecIds.length > 0 && selectedSpecIds.length < specializations.length
        ? selectedSpecIds
        : undefined,
    }),
    enabled: selectedBranchIds.length > 0,
    select: (res) => res.data ?? [],
  });
  const allDoctors: DoctorCalendarInfo[] = doctorsData ?? [];

  // Hekim dropdown için deduplicate (aynı doktor birden fazla şubede olabilir)
  const doctorOptions = useMemo(() => {
    const seen = new Set<number>();
    return allDoctors
      .filter((d) => {
        if (seen.has(d.doctorId)) return false;
        seen.add(d.doctorId);
        return true;
      })
      .map((d) => ({ value: d.doctorId, label: d.fullName }));
  }, [allDoctors]);

  useEffect(() => {
    if (allDoctors.length > 0) {
      setSelectedDoctorIds([...new Set(allDoctors.map((d) => d.doctorId))]);
    }
  }, [allDoctors]); // eslint-disable-line react-hooks/exhaustive-deps

  const visibleDoctorIds = useMemo(() => {
    const validIds = new Set(allDoctors.map((d) => d.doctorId));
    return selectedDoctorIds.filter((id) => validIds.has(id));
  }, [selectedDoctorIds, allDoctors]);

  const visibleDoctors = useMemo(() => {
    const collator = new Intl.Collator('tr', { sensitivity: 'base' });
    return allDoctors
      .filter((d) => visibleDoctorIds.includes(d.doctorId))
      .sort((a, b) => {
        if (a.isChiefPhysician !== b.isChiefPhysician)
          return a.isChiefPhysician ? -1 : 1;
        return collator.compare(a.fullName, b.fullName);
      });
  }, [allDoctors, visibleDoctorIds]);

  // Görünür hekimlerin en geç bitiş saatine göre takvim bitişini uzat
  const effectiveDayEndHour = useMemo(() => {
    let maxEnd = settings.dayEndHour;
    for (const doctor of visibleDoctors) {
      if (doctor.workEnd) {
        const [h, m] = doctor.workEnd.split(':').map(Number);
        const endHour = m > 0 ? h + 1 : h;
        if (endHour > maxEnd) maxEnd = endHour;
      }
    }
    return maxEnd;
  }, [visibleDoctors, settings.dayEndHour]);

  const { data: appointmentsData, isLoading: appointmentsLoading } = useQuery({
    queryKey: ['appointments', 'for-doctors', dateStr, visibleDoctorIds],
    queryFn: () => appointmentsApi.getForDoctors(dateStr, visibleDoctorIds),
    enabled: visibleDoctorIds.length > 0,
    select: (res) => res.data ?? [],
  });
  const appointments: Appointment[] = appointmentsData ?? [];

  // --- Socket ---
  const handleCalendarEvent = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
    queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
  }, [queryClient]);

  useCalendarSocket(handleCalendarEvent, ({ patientName, doctorName }) => {
    toast.info(`${doctorName} — ${patientName} hastasını çağırdı`, {
      duration: 6000,
      icon: '🔔',
    });
  });

  // Modal her zaman live query data'sından okur — state snapshot'ı değil
  const liveAppointment = selectedAppointment
    ? (appointments.find((a) => a.publicId === selectedAppointment.publicId) ?? selectedAppointment)
    : null;

  // --- Handlers ---

  function handleRangeSelect(doctorId: number, branchId: number, startTime: string, endTime: string) {
    const doctor = allDoctors.find((d) => d.doctorId === doctorId && d.branchId === branchId);
    if (!doctor) return;

    setSelectedRange({
      doctorId,
      branchId,
      doctorName: `${doctor.title ? doctor.title + ' ' : ''}${doctor.fullName}`,
      startTime,
      endTime,
      date: currentDate,
      dayStartHour: settings.dayStartHour,
      dayEndHour: effectiveDayEndHour,
    });
    setCreateOpen(true);
  }

  function handleAppointmentClick(apt: Appointment) {
    setSelectedAppointment(apt);
    setPendingStatusId(null);
    setDetailOpen(true);
  }

  const statusMutation = useMutation({
    mutationFn: ({ publicId, statusId }: { publicId: string; statusId: number }) =>
      appointmentsApi.updateStatus(publicId, statusId),
    onSuccess: (res) => {
      setSelectedAppointment(res.data as Appointment);
      setPendingStatusId(null);
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
    },
  });

  const [cancelConfirm, setCancelConfirm] = useState(false);
  const cancelMutation = useMutation({
    mutationFn: (publicId: string) => appointmentsApi.cancel(publicId),
    onSuccess: () => {
      setCancelConfirm(false);
      setDetailOpen(false);
      setPendingStatusId(null);
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
    },
  });

  const resizeMutation = useMutation({
    mutationFn: ({ apt, newStart, newEnd }: { apt: Appointment; newStart: string; newEnd: string }) =>
      appointmentsApi.move(apt.publicId, {
        newStartTime: newStart,
        newEndTime: newEnd,
        newDoctorId: apt.doctorId,
        expectedRowVersion: apt.rowVersion,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const moveMutation = useMutation({
    mutationFn: ({ apt, newDoctorId, newStart, newEnd }: {
      apt: Appointment; newDoctorId: number; newStart: string; newEnd: string;
    }) =>
      appointmentsApi.move(apt.publicId, {
        newStartTime: newStart,
        newEndTime: newEnd,
        newDoctorId,
        expectedRowVersion: apt.rowVersion,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
    },
  });

  // --- Date helpers ---
  const displayDate = format(currentDate, "dd MMMM yyyy - EEEE", { locale: tr });

  function goToday() {
    setCurrentDate(new Date());
  }

  // --- Render ---

  return (
    <div className="flex flex-col gap-3">
      {/* Header row */}
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-xl font-bold tracking-tight whitespace-nowrap">RANDEVU</h1>
          <div className="flex gap-1.5 flex-wrap">
            {statuses.filter((s) => s.isPatientStatus).map((s) => (
              <span
                key={s.id}
                className="inline-block size-4 rounded-sm border"
                style={{ backgroundColor: s.containerColor, borderColor: s.borderColor }}
                title={s.name}
              />
            ))}
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <MultiSelect
            label="Sube"
            options={branches.map((b) => ({ value: b.id, label: b.name }))}
            selected={selectedBranchIds}
            onChange={setSelectedBranchIds}
            allLabel="Tum subeler"
          />
          <MultiSelect
            label="Uzmanlik"
            options={specializations.map((s) => ({ value: s.id, label: s.name }))}
            selected={selectedSpecIds}
            onChange={setSelectedSpecIds}
            allLabel="Tum uzmanliklar"
          />
          <MultiSelect
            label="Hekim"
            options={doctorOptions}
            selected={selectedDoctorIds}
            onChange={setSelectedDoctorIds}
            allLabel="Tum hekimler"
            loading={doctorsLoading}
          />
        </div>
      </div>

      {/* Date navigation */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1">
          <Button variant="outline" size="icon" onClick={() => setCurrentDate((d) => subDays(d, 1))}>
            <ChevronLeft className="size-4" />
          </Button>
          <Button variant="outline" size="icon" onClick={() => setCurrentDate((d) => addDays(d, 1))}>
            <ChevronRight className="size-4" />
          </Button>
        </div>

        <h2 className="text-base font-semibold capitalize">{displayDate}</h2>

        <div className="flex items-center gap-1">
          <Button variant="outline" size="sm" onClick={goToday}>
            Bugun
          </Button>
          <Button variant="default" size="sm">
            Gun
          </Button>
          <Button variant="outline" size="sm" disabled>
            Hafta
          </Button>
        </div>
      </div>

      {/* Main area: waiting list + calendar */}
      <div className="flex gap-3 items-start">
        {/* Waiting list panel */}
        <div className="w-52 shrink-0 border rounded-lg overflow-hidden flex flex-col sticky top-0"
             style={{ maxHeight: 'calc(100svh - 11rem)' }}>
          <WaitingList />
        </div>

        {/* Calendar */}
        <div className="flex-1 min-w-0 overflow-x-auto">
          {doctorsLoading || appointmentsLoading ? (
            <div className="space-y-2">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-96 w-full" />
            </div>
          ) : (
            <CalendarGrid
              doctors={visibleDoctors}
              appointments={appointments}
              statuses={statuses}
              slotIntervalMinutes={settings.slotIntervalMinutes}
              dayStartHour={settings.dayStartHour}
              dayEndHour={effectiveDayEndHour}
              viewDate={currentDate}
              onRangeSelect={handleRangeSelect}
              onAppointmentClick={handleAppointmentClick}
              onAppointmentResize={(apt, newStart, newEnd) =>
                resizeMutation.mutate({ apt, newStart, newEnd })
              }
              onAppointmentMove={(apt, newDoctorId, _newBranchId, newStart, newEnd) =>
                moveMutation.mutate({ apt, newDoctorId, newStart, newEnd })
              }
            />
          )}
        </div>
      </div>

      {/* Patient search + create appointment modal */}
      <PatientSearchModal
        open={createOpen}
        range={selectedRange}
        onClose={() => {
          setCreateOpen(false);
          setSelectedRange(null);
        }}
        onSuccess={() => {
          setCreateOpen(false);
          setSelectedRange(null);
        }}
      />

      {/* Appointment detail dialog */}
      <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Randevu Detayi</DialogTitle>
          </DialogHeader>

          {liveAppointment && (() => {
            const currentStatus = statuses.find(s => s.id === liveAppointment.statusId);
            const allowedNextIds: number[] = currentStatus
              ? JSON.parse(currentStatus.allowedNextStatusIds ?? '[]')
              : [];
            // InRoom (5) is set only by the system (doctor starts protocol) — hide from manual UI
            const allowedNextStatuses = statuses.filter(s => allowedNextIds.includes(s.id) && s.id !== 5);
            const activeStatusId = pendingStatusId ?? liveAppointment.statusId;
            const activeStatus = statuses.find(s => s.id === activeStatusId);

            return (
              <div className="space-y-4">
                {/* Yolculuk adımları */}
                <AppointmentJourney statusId={activeStatusId} />

                <Separator />

                {/* Durum + geçiş */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Durum</span>
                    <Badge
                      style={{
                        backgroundColor: activeStatus?.containerColor,
                        borderColor: activeStatus?.borderColor,
                        color: activeStatus?.textColor,
                        borderWidth: '1px',
                        borderStyle: 'solid',
                      }}
                    >
                      {activeStatus?.name ?? liveAppointment.statusLabel}
                    </Badge>
                  </div>

                  {allowedNextStatuses.length > 0 && (
                    <div className="flex flex-wrap gap-1.5">
                      {allowedNextStatuses.map(s => (
                        <button
                          key={s.id}
                          type="button"
                          onClick={() => setPendingStatusId(s.id)}
                          className="text-xs px-2 py-1 rounded-md border transition-all"
                          style={{
                            backgroundColor: pendingStatusId === s.id ? s.containerColor : `${s.containerColor}33`,
                            borderColor: s.borderColor,
                            color: pendingStatusId === s.id ? s.textColor : s.borderColor,
                            fontWeight: pendingStatusId === s.id ? 600 : 400,
                          }}
                        >
                          {s.name}
                        </button>
                      ))}
                    </div>
                  )}

                  {statusMutation.isError && (
                    <p className="text-xs text-destructive">Durum güncellenemedi.</p>
                  )}
                </div>

                <Separator />

                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between items-center">
                    <dt className="text-muted-foreground">Hasta</dt>
                    <dd className="font-medium flex items-center gap-1.5">
                      {liveAppointment.patientName}
                      {liveAppointment.patientPublicId && (
                        <button
                          type="button"
                          onClick={() => navigate(`/patients/${liveAppointment.patientPublicId}`)}
                          className="text-muted-foreground hover:text-primary"
                          title="Hasta kartını aç"
                        >
                          <ExternalLink className="size-3.5" />
                        </button>
                      )}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Doktor</dt>
                    <dd className="font-medium">{liveAppointment.doctorName}</dd>
                  </div>
                  {liveAppointment.appointmentTypeName && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Randevu Tipi</dt>
                      <dd className="font-medium">{liveAppointment.appointmentTypeName}</dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Tarih</dt>
                    <dd className="font-medium">
                      {format(new Date(liveAppointment.startTime), 'dd.MM.yyyy')}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Saat</dt>
                    <dd className="font-medium">
                      {format(new Date(liveAppointment.startTime), 'HH:mm')} -{' '}
                      {format(new Date(liveAppointment.endTime), 'HH:mm')}
                    </dd>
                  </div>
                  {liveAppointment.notes && (
                    <>
                      <Separator />
                      <div>
                        <dt className="text-muted-foreground mb-1">Notlar</dt>
                        <dd>{liveAppointment.notes}</dd>
                      </div>
                    </>
                  )}
                </dl>
              </div>
            );
          })()}

          <DialogFooter className="flex-col gap-2 sm:flex-row sm:justify-between">
            {/* Sol: iptal butonu — terminal durumda gösterme */}
            {liveAppointment && ![4, 6, 7, 8].includes(liveAppointment.statusId) && (
              !cancelConfirm ? (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-destructive hover:text-destructive hover:bg-destructive/10 sm:mr-auto"
                  onClick={() => setCancelConfirm(true)}
                >
                  <Trash2 className="size-3.5 mr-1" />
                  İptal Et
                </Button>
              ) : (
                <div className="flex items-center gap-2 sm:mr-auto">
                  <span className="text-xs text-destructive">Emin misiniz?</span>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => cancelMutation.mutate(liveAppointment.publicId)}
                    disabled={cancelMutation.isPending}
                  >
                    {cancelMutation.isPending ? 'İptal ediliyor...' : 'Evet, İptal Et'}
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setCancelConfirm(false)}>
                    Vazgeç
                  </Button>
                </div>
              )
            )}

            {/* Sağ: kapat + güncelle */}
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => { setDetailOpen(false); setPendingStatusId(null); setCancelConfirm(false); }}>
                Kapat
              </Button>
              {pendingStatusId !== null && (
                <Button
                  onClick={() => statusMutation.mutate({
                    publicId: liveAppointment!.publicId,
                    statusId: pendingStatusId,
                  })}
                  disabled={statusMutation.isPending}
                >
                  {statusMutation.isPending ? 'Kaydediliyor...' : 'Durumu Güncelle'}
                </Button>
              )}
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
