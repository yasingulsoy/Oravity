import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import type { DateSelectArg, EventClickArg } from '@fullcalendar/core';
import { format } from 'date-fns';
import { appointmentsApi } from '@/api/appointments';
import { useCalendarSocket } from '@/hooks/useCalendarSocket';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { APPOINTMENT_STATUS_COLORS, APPOINTMENT_STATUS_LABELS } from '@/lib/constants';
import type { Appointment } from '@/types/appointment';

function getEventColor(status: string) {
  const colorMap: Record<string, string> = {
    Scheduled: '#3b82f6',
    Confirmed: '#22c55e',
    InProgress: '#eab308',
    Completed: '#6b7280',
    Cancelled: '#ef4444',
    NoShow: '#f97316',
  };
  return colorMap[status] ?? '#6b7280';
}

const createAppointmentSchema = z.object({
  patientId: z.string().min(1, 'Hasta ID gerekli'),
  doctorId: z.string().min(1, 'Doktor ID gerekli'),
  notes: z.string().optional(),
});

type CreateAppointmentForm = z.infer<typeof createAppointmentSchema>;

interface SelectedSlot {
  start: Date;
  end: Date;
}

export function AppointmentCalendarPage() {
  const queryClient = useQueryClient();
  const [dateRange, setDateRange] = useState({
    startDate: format(new Date(), 'yyyy-MM-dd'),
    endDate: format(new Date(), 'yyyy-MM-dd'),
  });
  const [selectedSlot, setSelectedSlot] = useState<SelectedSlot | null>(null);
  const [selectedAppointment, setSelectedAppointment] = useState<Appointment | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);

  const { data } = useQuery({
    queryKey: ['appointments', 'calendar', dateRange],
    queryFn: () => appointmentsApi.getCalendar(dateRange),
  });

  const appointments: Appointment[] = data?.data ?? [];

  const createMutation = useMutation({
    mutationFn: (data: { patientId: string; doctorId: string; startTime: string; endTime: string; notes?: string }) =>
      appointmentsApi.create({
        patientId: data.patientId,
        doctorId: data.doctorId,
        startTime: data.startTime,
        endTime: data.endTime,
        notes: data.notes,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments', 'calendar'] });
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

  const handleCalendarEvent = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['appointments', 'calendar'] });
  }, [queryClient]);

  useCalendarSocket(handleCalendarEvent);

  const handleSelect = useCallback((arg: DateSelectArg) => {
    setSelectedSlot({ start: arg.start, end: arg.end });
    setCreateOpen(true);
  }, []);

  const handleEventClick = useCallback((arg: EventClickArg) => {
    const apt = arg.event.extendedProps.appointment as Appointment;
    setSelectedAppointment(apt);
    setDetailOpen(true);
  }, []);

  const onSubmit = (formData: CreateAppointmentForm) => {
    if (!selectedSlot) return;
    createMutation.mutate({
      patientId: formData.patientId,
      doctorId: formData.doctorId,
      startTime: selectedSlot.start.toISOString(),
      endTime: selectedSlot.end.toISOString(),
      notes: formData.notes || undefined,
    });
  };

  const calendarEvents = appointments.map((apt) => ({
    id: apt.id,
    title: `${apt.patientName} - ${apt.treatmentName ?? 'Randevu'}`,
    start: apt.startTime,
    end: apt.endTime,
    backgroundColor: getEventColor(apt.status),
    borderColor: getEventColor(apt.status),
    extendedProps: { appointment: apt },
  }));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Randevu Takvimi</h1>
        <p className="text-muted-foreground">
          Takvimde bir zaman aralığı seçerek yeni randevu oluşturabilirsiniz
        </p>
      </div>

      <div className="flex gap-2 flex-wrap">
        {Object.entries(APPOINTMENT_STATUS_LABELS).map(([status, label]) => (
          <span
            key={status}
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${APPOINTMENT_STATUS_COLORS[status]}`}
          >
            {label}
          </span>
        ))}
      </div>

      <Card>
        <CardContent className="pt-6">
          <FullCalendar
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: 'prev,next today',
              center: 'title',
              right: 'dayGridMonth,timeGridWeek,timeGridDay',
            }}
            locale="tr"
            firstDay={1}
            slotMinTime="08:00:00"
            slotMaxTime="20:00:00"
            slotDuration="00:15:00"
            allDaySlot={false}
            events={calendarEvents}
            editable={false}
            selectable
            selectMirror
            select={handleSelect}
            eventClick={handleEventClick}
            datesSet={(arg) => {
              setDateRange({
                startDate: format(arg.start, 'yyyy-MM-dd'),
                endDate: format(arg.end, 'yyyy-MM-dd'),
              });
            }}
            height="auto"
          />
        </CardContent>
      </Card>

      {/* Yeni Randevu Dialog */}
      <Dialog open={createOpen} onOpenChange={(open) => { setCreateOpen(open); if (!open) reset(); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Yeni Randevu Oluştur</DialogTitle>
          </DialogHeader>

          {selectedSlot && (
            <div className="rounded-md bg-muted p-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tarih</span>
                <span className="font-medium">{format(selectedSlot.start, 'dd.MM.yyyy')}</span>
              </div>
              <div className="flex justify-between mt-1">
                <span className="text-muted-foreground">Saat</span>
                <span className="font-medium">
                  {format(selectedSlot.start, 'HH:mm')} – {format(selectedSlot.end, 'HH:mm')}
                </span>
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="patientId">Hasta ID</Label>
              <Input
                id="patientId"
                placeholder="Hasta ID giriniz"
                {...register('patientId')}
              />
              {errors.patientId && (
                <p className="text-sm text-destructive">{errors.patientId.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="doctorId">Doktor ID</Label>
              <Input
                id="doctorId"
                placeholder="Doktor ID giriniz"
                {...register('doctorId')}
              />
              {errors.doctorId && (
                <p className="text-sm text-destructive">{errors.doctorId.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Notlar</Label>
              <Input
                id="notes"
                placeholder="Randevu notu (opsiyonel)"
                {...register('notes')}
              />
            </div>

            {createMutation.isError && (
              <p className="text-sm text-destructive">
                Randevu oluşturulamadı. Lütfen bilgileri kontrol edin.
              </p>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
                İptal
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? 'Oluşturuluyor...' : 'Randevu Oluştur'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Randevu Detay Dialog */}
      <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Randevu Detayı</DialogTitle>
          </DialogHeader>

          {selectedAppointment && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Durum</span>
                <Badge className={APPOINTMENT_STATUS_COLORS[selectedAppointment.status]}>
                  {APPOINTMENT_STATUS_LABELS[selectedAppointment.status] ?? selectedAppointment.status}
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
                {selectedAppointment.treatmentName && (
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Tedavi</dt>
                    <dd className="font-medium">{selectedAppointment.treatmentName}</dd>
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
                    {format(new Date(selectedAppointment.startTime), 'HH:mm')} –{' '}
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
