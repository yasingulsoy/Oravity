import { useMemo, useState, useEffect, useRef } from 'react';
import type { Appointment, AppointmentStatus, DoctorCalendarInfo } from '@/types/appointment';
import { DoctorSpecialDayType } from '@/types/appointment';
import { AppointmentBlock } from './AppointmentBlock';
import { cn } from '@/lib/utils';

function getCurrentMinutes(): number {
  const now = new Date();
  return now.getHours() * 60 + now.getMinutes();
}

const PX_PER_MINUTE = 40 / 30;

/** Assigns lane indices to appointments so overlapping ones render side-by-side. */
function assignLanes(appointments: Appointment[]): Map<string, { lane: number; totalLanes: number }> {
  const sorted = [...appointments].sort(
    (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
  );

  // Each "group" is a set of mutually-overlapping appointments
  const groups: Appointment[][] = [];

  for (const apt of sorted) {
    const aptStart = new Date(apt.startTime).getTime();
    const aptEnd = new Date(apt.endTime).getTime();

    // Find the first group where this apt overlaps at least one member
    const target = groups.find((g) =>
      g.some((a) => {
        const s = new Date(a.startTime).getTime();
        const e = new Date(a.endTime).getTime();
        return aptStart < e && aptEnd > s;
      })
    );

    if (target) {
      target.push(apt);
    } else {
      groups.push([apt]);
    }
  }

  const result = new Map<string, { lane: number; totalLanes: number }>();
  for (const group of groups) {
    const totalLanes = group.length;
    group.forEach((apt, i) => result.set(apt.publicId, { lane: i, totalLanes }));
  }
  return result;
}

interface CalendarGridProps {
  doctors: DoctorCalendarInfo[];
  appointments: Appointment[];
  statuses: AppointmentStatus[];
  slotIntervalMinutes?: number;
  dayStartHour?: number;
  dayEndHour?: number;
  viewDate: Date;
  onRangeSelect: (doctorId: number, branchId: number, startTime: string, endTime: string) => void;
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

function getSlotInfo(
  slotStart: number,
  doctor: DoctorCalendarInfo
): { type: SlotType; breakLabel?: string } {
  if (!doctor.workStart || !doctor.workEnd) return { type: 'closed' };

  const workStart = timeToMinutes(doctor.workStart);
  const workEnd = timeToMinutes(doctor.workEnd);

  if (slotStart < workStart || slotStart >= workEnd) return { type: 'closed' };

  if (doctor.breakStart && doctor.breakEnd) {
    const breakStart = timeToMinutes(doctor.breakStart);
    const breakEnd = timeToMinutes(doctor.breakEnd);
    if (slotStart >= breakStart && slotStart < breakEnd) {
      return { type: 'break', breakLabel: doctor.breakLabel ?? 'Mola' };
    }
  }

  return { type: 'open' };
}

interface DragState {
  doctorId: number;
  branchId: number;
  startMinutes: number;
  currentMinutes: number;
}

export function CalendarGrid({
  doctors,
  appointments,
  statuses,
  slotIntervalMinutes = 30,
  dayStartHour = 8,
  dayEndHour = 20,
  viewDate,
  onRangeSelect,
  onAppointmentClick,
}: CalendarGridProps) {
  const dayStart = dayStartHour * 60;
  const dayEnd = dayEndHour * 60;
  const slotHeight = Math.round(slotIntervalMinutes * PX_PER_MINUTE);

  const [dragState, setDragState] = useState<DragState | null>(null);
  const isDragging = dragState !== null;

  // Current time line
  const [nowMinutes, setNowMinutes] = useState(getCurrentMinutes);
  useEffect(() => {
    const id = setInterval(() => setNowMinutes(getCurrentMinutes()), 60_000);
    return () => clearInterval(id);
  }, []);
  const showNowLine = nowMinutes >= dayStart && nowMinutes <= dayEnd;
  const nowLineTop = showNowLine
    ? ((nowMinutes - dayStart) / slotIntervalMinutes) * slotHeight
    : null;

  // Past-slot cutoff: future day → null (all bookable), today → nowMinutes, past day → Infinity
  const today = new Date();
  const isToday =
    viewDate.getFullYear() === today.getFullYear() &&
    viewDate.getMonth() === today.getMonth() &&
    viewDate.getDate() === today.getDate();
  const isPastDay =
    viewDate < new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const pastCutoffMinutes: number | null = isPastDay ? Infinity : isToday ? nowMinutes : null;

  // Stable ref to avoid stale closure in mouseup handler
  const dragRef = useRef<DragState | null>(null);
  dragRef.current = dragState;

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseUp = () => {
      const drag = dragRef.current;
      if (!drag) return;
      const startMins = Math.min(drag.startMinutes, drag.currentMinutes);
      const endMins = Math.max(drag.startMinutes, drag.currentMinutes) + slotIntervalMinutes;
      onRangeSelect(drag.doctorId, drag.branchId, minutesToTime(startMins), minutesToTime(endMins));
      setDragState(null);
    };

    document.addEventListener('mouseup', handleMouseUp);
    return () => document.removeEventListener('mouseup', handleMouseUp);
  }, [isDragging, slotIntervalMinutes, onRangeSelect]);

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

  const multibranchDoctorIds = useMemo(() => {
    const counts = new Map<number, number>();
    for (const d of doctors) counts.set(d.doctorId, (counts.get(d.doctorId) ?? 0) + 1);
    return new Set([...counts.entries()].filter(([, n]) => n > 1).map(([id]) => id));
  }, [doctors]);

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
    <div
      className="overflow-x-auto border rounded-lg"
      style={{ userSelect: isDragging ? 'none' : undefined }}
    >
      <div className="inline-flex min-w-full relative">
        {/* Time column */}
        <div className="sticky left-0 z-20 bg-background border-r w-16 shrink-0">
          <div className="h-16 border-b" />
          <div className="relative">
            {timeSlots.map((minutes) => (
              <div
                key={minutes}
                className="border-b text-xs text-muted-foreground flex items-start justify-end pr-2 pt-0.5"
                style={{ height: `${slotHeight}px` }}
              >
                {minutesToTime(minutes)}
              </div>
            ))}
            {/* Red dot on time axis */}
            {nowLineTop !== null && (
              <span
                className="absolute right-0 size-2.5 rounded-full bg-red-500 z-30 translate-x-1/2 -translate-y-1/2"
                style={{ top: `${nowLineTop}px` }}
              />
            )}
          </div>
        </div>

        {/* Doctor columns */}
        {doctors.map((doctor) => {
          const doctorApts = appointmentsByDoctor.get(doctor.doctorId) ?? [];
          const headerColor = doctor.calendarColor ?? '#0ea5e9';

          // Drag overlay for this column
          let overlayTop = 0;
          let overlayHeight = 0;
          const showOverlay = dragState?.doctorId === doctor.doctorId && dragState?.branchId === doctor.branchId;
          if (showOverlay && dragState) {
            const startMins = Math.min(dragState.startMinutes, dragState.currentMinutes);
            const endMins = Math.max(dragState.startMinutes, dragState.currentMinutes) + slotIntervalMinutes;
            overlayTop = ((startMins - dayStart) / slotIntervalMinutes) * slotHeight;
            overlayHeight = ((endMins - startMins) / slotIntervalMinutes) * slotHeight;
          }

          return (
            <div
              key={`${doctor.doctorId}-${doctor.branchId}`}
              className={cn('flex-1 border-r last:border-r-0', colWidth)}
            >
              {/* Doctor header */}
              <div
                className="h-16 border-b px-2 py-1.5 flex flex-col justify-center text-xs"
                style={{ backgroundColor: headerColor + '18' }}
              >
                <span
                  className="font-semibold truncate leading-tight flex items-center gap-1"
                  style={{ color: headerColor }}
                  title={`${doctor.title ? doctor.title + ' ' : ''}${doctor.fullName}${doctor.isChiefPhysician ? ' (Başhekim)' : ''}`}
                >
                  {doctor.isChiefPhysician && (
                    <span className="shrink-0 text-[9px] font-bold bg-yellow-100 text-yellow-700 px-0.5 py-px rounded leading-none">BŞ</span>
                  )}
                  {doctor.title ? `${doctor.title} ` : ''}{doctor.fullName}
                </span>
                <span className="text-muted-foreground truncate leading-tight flex items-center gap-1">
                  {doctor.workStart && doctor.workEnd
                    ? `${doctor.workStart} - ${doctor.workEnd}`
                    : 'Calismiyor'}
                  {doctor.isSpecialDay && (
                    <span
                      className={cn(
                        'inline-block shrink-0 text-[9px] font-bold px-0.5 py-px rounded leading-none',
                        doctor.specialDayType === DoctorSpecialDayType.ExtraWork  && 'bg-green-100 text-green-700',
                        doctor.specialDayType === DoctorSpecialDayType.HourChange && 'bg-blue-100 text-blue-700',
                        doctor.specialDayType === DoctorSpecialDayType.DayOff     && 'bg-red-100 text-red-700',
                      )}
                      title={doctor.specialDayReason ?? (
                        doctor.specialDayType === DoctorSpecialDayType.ExtraWork  ? 'Ekstra mesai' :
                        doctor.specialDayType === DoctorSpecialDayType.HourChange ? 'Saat değişikliği' : 'İzin'
                      )}
                    >
                      {doctor.specialDayType === DoctorSpecialDayType.ExtraWork  ? 'EKSTRA' :
                       doctor.specialDayType === DoctorSpecialDayType.HourChange ? 'ÖZEL' : 'İZİN'}
                    </span>
                  )}
                </span>
                <span className="text-[10px] text-muted-foreground truncate leading-tight">
                  {doctor.isSpecialDay && doctor.specialDayReason
                    ? doctor.specialDayReason
                    : multibranchDoctorIds.has(doctor.doctorId)
                      ? doctor.branchName
                      : doctor.specializationName ?? ''}
                </span>
              </div>

              {/* Slots */}
              <div className="relative">
                {/* Current time line */}
                {nowLineTop !== null && (
                  <div
                    className="absolute left-0 right-0 z-20 pointer-events-none"
                    style={{ top: `${nowLineTop}px` }}
                  >
                    <div className="h-0.5 bg-red-500" />
                  </div>
                )}
                {timeSlots.map((minutes) => {
                  const { type: slotType, breakLabel } = getSlotInfo(minutes, doctor);
                  const isPast = pastCutoffMinutes !== null && minutes < pastCutoffMinutes;
                  const isBookable = slotType === 'open' && !isPast;
                  const isCurrentDragCol = isDragging && dragState?.doctorId === doctor.doctorId && dragState?.branchId === doctor.branchId;
                  // Break etiketini sadece mola bloğunun ilk slotunda göster
                  const isBreakStart = slotType === 'break' && (() => {
                    const prev = minutes - slotIntervalMinutes;
                    return prev < 0 || getSlotInfo(prev, doctor).type !== 'break';
                  })();
                  return (
                    <div
                      key={minutes}
                      className={cn(
                        'border-b relative',
                        slotType === 'closed' && 'bg-gray-100',
                        slotType === 'break' && 'bg-amber-50',
                        isPast && slotType === 'open' && 'bg-gray-50',
                        isBookable && !isDragging && 'bg-white hover:bg-blue-50/50 cursor-crosshair',
                        isBookable && isDragging && isCurrentDragCol && 'bg-white cursor-crosshair',
                        isBookable && isDragging && !isCurrentDragCol && 'bg-white',
                        !isBookable && slotType === 'open' && 'cursor-default',
                      )}
                      style={{ height: `${slotHeight}px` }}
                      onMouseDown={() => {
                        if (isBookable) {
                          setDragState({
                            doctorId: doctor.doctorId,
                            branchId: doctor.branchId,
                            startMinutes: minutes,
                            currentMinutes: minutes,
                          });
                        }
                      }}
                      onMouseEnter={() => {
                        if (
                          isDragging &&
                          dragState?.doctorId === doctor.doctorId &&
                          dragState?.branchId === doctor.branchId &&
                          isBookable
                        ) {
                          setDragState((prev) => prev ? { ...prev, currentMinutes: minutes } : null);
                        }
                      }}
                    >
                      {isBreakStart && breakLabel && (
                        <span className="absolute inset-x-1 top-0.5 text-[10px] text-amber-700 font-medium truncate leading-none pointer-events-none">
                          {breakLabel}
                        </span>
                      )}
                    </div>
                  );
                })}

                {/* Drag selection overlay */}
                {showOverlay && overlayHeight > 0 && (
                  <div
                    className="absolute left-0 right-0 bg-blue-400/25 border-2 border-blue-500 pointer-events-none z-10 rounded-sm"
                    style={{ top: overlayTop, height: overlayHeight }}
                  />
                )}

                {/* Appointment blocks */}
                {(() => {
                  const lanes = assignLanes(doctorApts);
                  return doctorApts.map((apt) => {
                    const laneInfo = lanes.get(apt.publicId) ?? { lane: 0, totalLanes: 1 };
                    return (
                      <AppointmentBlock
                        key={apt.publicId}
                        appointment={apt}
                        statuses={statuses}
                        slotHeight={slotHeight}
                        slotMinutes={slotIntervalMinutes}
                        dayStart={dayStart}
                        lane={laneInfo.lane}
                        totalLanes={laneInfo.totalLanes}
                        onClick={onAppointmentClick}
                      />
                    );
                  });
                })()}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
