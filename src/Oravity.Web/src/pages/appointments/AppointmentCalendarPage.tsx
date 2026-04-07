import { useState, useCallback, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { format, addDays, subDays } from 'date-fns';
import { tr } from 'date-fns/locale';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { appointmentsApi } from '@/api/appointments';
import { useCalendarSocket } from '@/hooks/useCalendarSocket';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import type { Appointment, DoctorCalendarInfo, CalendarSettings } from '@/types/appointment';

// -- Form schema ---------------------------------------------------------------

const createAppointmentSchema = z.object({
  patientId: z.string().min(1, 'Hasta ID gerekli'),
  notes: z.string().optional(),
});

type CreateAppointmentForm = z.infer<typeof createAppointmentSchema>;

interface SelectedSlot {
  doctorId: number;
  doctorName: string;
  start: Date;
  end: Date;
}

// -- Page component ------------------------------------------------------------

export function AppointmentCalendarPage() {
  const queryClient = useQueryClient();

  // --- Date navigation ---
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const dateStr = format(currentDate, 'yyyy-MM-dd');

  // --- Filters ---
  const [selectedBranchIds, setSelectedBranchIds] = useState<number[]>([]);
  const [selectedSpecIds, setSelectedSpecIds] = useState<number[]>([]);
  const [selectedDoctorIds, setSelectedDoctorIds] = useState<number[]>([]);
  const [branchesInitialized, setBranchesInitialized] = useState(false);
  const [specsInitialized, setSpecsInitialized] = useState(false);
  const [doctorsInitialized, setDoctorsInitialized] = useState(false);

  // --- Dialogs ---
  const [selectedSlot, setSelectedSlot] = useState<SelectedSlot | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState<Appointment | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  // --- Queries ---

  const { data: branchesData } = useQuery({
    queryKey: ['appointments', 'accessible-branches'],
    queryFn: () => appointmentsApi.getAccessibleBranches(),
    staleTime: 5 * 60 * 1000,
    select: (res) => res.data ?? [],
  });
  const branches = branchesData ?? [];

  // Initialize branches once loaded
  if (branches.length > 0 && !branchesInitialized) {
    setSelectedBranchIds(branches.map((b) => b.id));
    setBranchesInitialized(true);
  }

  const { data: specsData } = useQuery({
    queryKey: ['appointments', 'specializations'],
    queryFn: () => appointmentsApi.getSpecializations(),
    staleTime: 5 * 60 * 1000,
    select: (res) => res.data ?? [],
  });
  const specializations = specsData ?? [];

  if (specializations.length > 0 && !specsInitialized) {
    setSelectedSpecIds(specializations.map((s) => s.id));
    setSpecsInitialized(true);
  }

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
    queryFn: async () => {
      setDoctorsInitialized(false); // yeni sorgu → re-init
      return appointmentsApi.getCalendarDoctors({
        date: dateStr,
        branchIds: selectedBranchIds.length > 0 ? selectedBranchIds : undefined,
        specializationIds: selectedSpecIds.length > 0 ? selectedSpecIds : undefined,
      });
    },
    enabled: branchesInitialized,
    select: (res) => res.data ?? [],
  });
  const allDoctors: DoctorCalendarInfo[] = doctorsData ?? [];

  const doctorOptions = useMemo(
    () => allDoctors.map((d) => ({ value: d.doctorId, label: d.fullName })),
    [allDoctors]
  );

  // Hekimler ilk yüklendiğinde hepsini seç; filtre/tarih değişince geçersiz ID'leri temizle
  if (allDoctors.length > 0 && !doctorsInitialized) {
    setSelectedDoctorIds(allDoctors.map((d) => d.doctorId));
    setDoctorsInitialized(true);
  }

  // Filtre/tarih değişip yeni hekim listesi gelince seçili olmayan ID'leri düşür,
  // tamamı geçersizse yeniden hepsini seç
  const visibleDoctorIds = useMemo(() => {
    if (selectedDoctorIds.length === 0) return [];
    const validIds = new Set(allDoctors.map((d) => d.doctorId));
    return selectedDoctorIds.filter((id) => validIds.has(id));
  }, [selectedDoctorIds, allDoctors]);

  const visibleDoctors = useMemo(
    () => allDoctors.filter((d) => visibleDoctorIds.includes(d.doctorId)),
    [allDoctors, visibleDoctorIds]
  );

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
  }, [queryClient]);

  useCalendarSocket(handleCalendarEvent);

  // --- Handlers ---

  function handleSlotClick(doctorId: number, time: string) {
    const doctor = allDoctors.find((d) => d.doctorId === doctorId);
    if (!doctor) return;

    const [h, m] = time.split(':').map(Number);
    const start = new Date(currentDate);
    start.setHours(h, m, 0, 0);
    const end = new Date(start);
    end.setMinutes(end.getMinutes() + 30);

    setSelectedSlot({
      doctorId,
      doctorName: `${doctor.title ? doctor.title + ' ' : ''}${doctor.fullName}`,
      start,
      end,
    });
    setCreateOpen(true);
  }

  function handleAppointmentClick(apt: Appointment) {
    setSelectedAppointment(apt);
    setDetailOpen(true);
  }

  // --- Create mutation ---

  const createMutation = useMutation({
    mutationFn: (data: { patientId: string; doctorId: number; startTime: string; endTime: string; notes?: string }) =>
      appointmentsApi.create({
        patientId: Number(data.patientId),
        doctorId: data.doctorId,
        startTime: data.startTime,
        endTime: data.endTime,
        notes: data.notes,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      setCreateOpen(false);
      reset();
    },
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateAppointmentForm>({
    resolver: zodResolver(createAppointmentSchema),
  });

  function onSubmit(formData: CreateAppointmentForm) {
    if (!selectedSlot) return;
    createMutation.mutate({
      patientId: formData.patientId,
      doctorId: selectedSlot.doctorId,
      startTime: selectedSlot.start.toISOString(),
      endTime: selectedSlot.end.toISOString(),
      notes: formData.notes || undefined,
    });
  }

  // --- Date helpers ---

  const displayDate = format(currentDate, "dd MMMM yyyy - EEEE", { locale: tr });

  function goToday() {
    setCurrentDate(new Date());
  }

  // --- Render ---

  return (
    <div className="space-y-4">
      {/* Header row: title + status legend + filters */}
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

      {/* Calendar grid */}
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
          onSlotClick={handleSlotClick}
          onAppointmentClick={handleAppointmentClick}
        />
      )}

      {/* Create appointment dialog */}
      <Dialog
        open={createOpen}
        onOpenChange={(open) => {
          setCreateOpen(open);
          if (!open) reset();
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Yeni Randevu Olustur</DialogTitle>
          </DialogHeader>

          {selectedSlot && (
            <div className="rounded-md bg-muted p-3 text-sm space-y-1">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Hekim</span>
                <span className="font-medium">{selectedSlot.doctorName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tarih</span>
                <span className="font-medium">{format(selectedSlot.start, 'dd.MM.yyyy')}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Saat</span>
                <span className="font-medium">
                  {format(selectedSlot.start, 'HH:mm')} - {format(selectedSlot.end, 'HH:mm')}
                </span>
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="patientId">Hasta ID</Label>
              <Input id="patientId" placeholder="Hasta ID giriniz" {...register('patientId')} />
              {errors.patientId && (
                <p className="text-sm text-destructive">{errors.patientId.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Notlar</Label>
              <Input id="notes" placeholder="Randevu notu (opsiyonel)" {...register('notes')} />
            </div>

            {createMutation.isError && (
              <p className="text-sm text-destructive">
                Randevu olusturulamadi. Lutfen bilgileri kontrol edin.
              </p>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
                Iptal
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? 'Olusturuluyor...' : 'Randevu Olustur'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Appointment detail dialog */}
      <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Randevu Detayi</DialogTitle>
          </DialogHeader>

          {selectedAppointment && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Durum</span>
                <Badge className={APPOINTMENT_STATUS_COLORS[selectedAppointment.statusId]}>
                  {selectedAppointment.statusLabel}
                </Badge>
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
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setDetailOpen(false)}>
              Kapat
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
