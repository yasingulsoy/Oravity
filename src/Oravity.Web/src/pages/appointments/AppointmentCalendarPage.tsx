import { useState, useCallback, useMemo, useEffect } from 'react';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { format, addDays, subDays } from 'date-fns';
import { tr } from 'date-fns/locale';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { appointmentsApi } from '@/api/appointments';
import { useCalendarSocket } from '@/hooks/useCalendarSocket';
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

export function AppointmentCalendarPage() {
  const queryClient = useQueryClient();

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

  useCalendarSocket(handleCalendarEvent);

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
      dayEndHour: settings.dayEndHour,
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
              dayEndHour={settings.dayEndHour}
              viewDate={currentDate}
              onRangeSelect={handleRangeSelect}
              onAppointmentClick={handleAppointmentClick}
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

          {selectedAppointment && (() => {
            const currentStatus = statuses.find(s => s.id === selectedAppointment.statusId);
            const allowedNextIds: number[] = currentStatus
              ? JSON.parse(currentStatus.allowedNextStatusIds ?? '[]')
              : [];
            const allowedNextStatuses = statuses.filter(s => allowedNextIds.includes(s.id));
            const activeStatusId = pendingStatusId ?? selectedAppointment.statusId;
            const activeStatus = statuses.find(s => s.id === activeStatusId);

            return (
              <div className="space-y-4">
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
                      {activeStatus?.name ?? selectedAppointment.statusLabel}
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
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Hasta</dt>
                    <dd className="font-medium">{selectedAppointment.patientName}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Doktor</dt>
                    <dd className="font-medium">{selectedAppointment.doctorName}</dd>
                  </div>
                  {selectedAppointment.appointmentTypeName && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Randevu Tipi</dt>
                      <dd className="font-medium">{selectedAppointment.appointmentTypeName}</dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Tarih</dt>
                    <dd className="font-medium">
                      {format(new Date(selectedAppointment.startTime), 'dd.MM.yyyy')}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Saat</dt>
                    <dd className="font-medium">
                      {format(new Date(selectedAppointment.startTime), 'HH:mm')} -{' '}
                      {format(new Date(selectedAppointment.endTime), 'HH:mm')}
                    </dd>
                  </div>
                  {selectedAppointment.notes && (
                    <>
                      <Separator />
                      <div>
                        <dt className="text-muted-foreground mb-1">Notlar</dt>
                        <dd>{selectedAppointment.notes}</dd>
                      </div>
                    </>
                  )}
                </dl>
              </div>
            );
          })()}

          <DialogFooter>
            <Button variant="outline" onClick={() => { setDetailOpen(false); setPendingStatusId(null); }}>
              Kapat
            </Button>
            {pendingStatusId !== null && (
              <Button
                onClick={() => statusMutation.mutate({
                  publicId: selectedAppointment!.publicId,
                  statusId: pendingStatusId,
                })}
                disabled={statusMutation.isPending}
              >
                {statusMutation.isPending ? 'Kaydediliyor...' : 'Durumu Güncelle'}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
