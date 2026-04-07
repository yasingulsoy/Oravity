import { useMemo } from 'react';
import type { Appointment, AppointmentStatus, DoctorCalendarInfo } from '@/types/appointment';
import { AppointmentBlock } from './AppointmentBlock';
import { cn } from '@/lib/utils';

const PX_PER_MINUTE = 40 / 30; // sabit piksel/dakika oranı (~1.33)

interface CalendarGridProps {
  doctors: DoctorCalendarInfo[];
  appointments: Appointment[];
  statuses: AppointmentStatus[];
  slotIntervalMinutes?: number;
  dayStartHour?: number;
  dayEndHour?: number;
  onSlotClick: (doctorId: number, time: string) => void;
  onAppointmentClick: (appointment: Appointment) => void;
}

function timeToMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}

function minutesToTime(minutes: number): string {
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`;
}

type SlotType = 'open' | 'closed' | 'break';

function getSlotType(
  slotStart: number,
  doctor: DoctorCalendarInfo
): SlotType {
  if (!doctor.workStart || !doctor.workEnd) return 'closed';

  const workStart = timeToMinutes(doctor.workStart);
  const workEnd = timeToMinutes(doctor.workEnd);

  if (slotStart < workStart || slotStart >= workEnd) return 'closed';

  if (doctor.breakStart && doctor.breakEnd) {
    const breakStart = timeToMinutes(doctor.breakStart);
    const breakEnd = timeToMinutes(doctor.breakEnd);
    if (slotStart >= breakStart && slotStart < breakEnd) return 'break';
  }

  return 'open';
}

export function CalendarGrid({
  doctors,
  appointments,
  statuses,
  slotIntervalMinutes = 30,
  dayStartHour = 8,
  dayEndHour = 20,
  onSlotClick,
  onAppointmentClick,
}: CalendarGridProps) {
  const dayStart = dayStartHour * 60;
  const dayEnd = dayEndHour * 60;
  const slotHeight = Math.round(slotIntervalMinutes * PX_PER_MINUTE);

  const timeSlots = useMemo(() => {
    const slots: number[] = [];
    for (let m = dayStart; m < dayEnd; m += slotIntervalMinutes) {
      slots.push(m);
    }
    return slots;
  }, [dayStart, dayEnd, slotIntervalMinutes]);

  const appointmentsByDoctor = useMemo(() => {
    const map = new Map<number, Appointment[]>();
    for (const apt of appointments) {
      const list = map.get(apt.doctorId) ?? [];
      list.push(apt);
      map.set(apt.doctorId, list);
    }
    return map;
  }, [appointments]);

  if (doctors.length === 0) {
    return (
      <div className="flex items-center justify-center py-20 text-muted-foreground">
        Secilen filtrelere uygun hekim bulunamadi
      </div>
    );
  }

  const colWidth = doctors.length <= 4
    ? 'min-w-[220px]'
    : doctors.length <= 8
      ? 'min-w-[180px]'
      : 'min-w-[150px]';

  return (
    <div className="overflow-x-auto border rounded-lg">
      <div className="inline-flex min-w-full">
        {/* Time column */}
        <div className="sticky left-0 z-20 bg-background border-r w-16 shrink-0">
          {/* Header spacer */}
          <div className="h-16 border-b" />
          {/* Time labels */}
          {timeSlots.map((minutes) => (
            <div
              key={minutes}
              className="border-b text-xs text-muted-foreground flex items-start justify-end pr-2 pt-0.5"
              style={{ height: `${slotHeight}px` }}
            >
              {minutesToTime(minutes)}
            </div>
          ))}
        </div>

        {/* Doctor columns */}
        {doctors.map((doctor) => {
          const doctorApts = appointmentsByDoctor.get(doctor.doctorId) ?? [];
          const headerColor = doctor.calendarColor ?? '#0ea5e9';

          return (
            <div
              key={doctor.doctorId}
              className={cn('flex-1 border-r last:border-r-0', colWidth)}
            >
              {/* Doctor header */}
              <div
                className="h-16 border-b px-2 py-1.5 flex flex-col justify-center text-xs"
                style={{ backgroundColor: headerColor + '18' }}
              >
                <span
                  className="font-semibold truncate leading-tight"
                  style={{ color: headerColor }}
                  title={`${doctor.title ? doctor.title + ' ' : ''}${doctor.fullName}`}
                >
                  {doctor.title ? `${doctor.title} ` : ''}{doctor.fullName}
                </span>
                <span className="text-muted-foreground truncate leading-tight">
                  {doctor.workStart && doctor.workEnd
                    ? `(${doctor.workStart} - ${doctor.workEnd})`
                    : 'Calismiyor'}
                </span>
                {doctor.specializationName && (
                  <span className="text-[10px] text-muted-foreground truncate leading-tight">
                    {doctor.specializationName}
                  </span>
                )}
              </div>

              {/* Slots */}
              <div className="relative">
                {timeSlots.map((minutes) => {
                  const slotType = getSlotType(minutes, doctor);
                  return (
                    <div
                      key={minutes}
                      className={cn(
                        'border-b',
                        slotType === 'closed' && 'bg-gray-100',
                        slotType === 'break' && 'bg-gray-50',
                        slotType === 'open' && 'bg-white hover:bg-blue-50/50 cursor-pointer'
                      )}
                      style={{ height: `${slotHeight}px` }}
                      onClick={() => {
                        if (slotType === 'open') {
                          onSlotClick(doctor.doctorId, minutesToTime(minutes));
                        }
                      }}
                      role={slotType === 'open' ? 'button' : undefined}
                      tabIndex={slotType === 'open' ? 0 : undefined}
                      onKeyDown={(e) => {
                        if (slotType === 'open' && (e.key === 'Enter' || e.key === ' ')) {
                          e.preventDefault();
                          onSlotClick(doctor.doctorId, minutesToTime(minutes));
                        }
                      }}
                      aria-label={
                        slotType === 'open'
                          ? `Yeni randevu: ${doctor.fullName}, ${minutesToTime(minutes)}`
                          : undefined
                      }
                    />
                  );
                })}

                {/* Appointment overlays */}
                {doctorApts.map((apt) => (
                  <AppointmentBlock
                    key={apt.publicId}
                    appointment={apt}
                    statuses={statuses}
                    slotHeight={slotHeight}
                    slotMinutes={slotIntervalMinutes}
                    dayStart={dayStart}
                    onClick={onAppointmentClick}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
