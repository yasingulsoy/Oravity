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
  onAppointmentResize?: (apt: Appointment, newStartISO: string, newEndISO: string) => void;
  onAppointmentMove?: (apt: Appointment, newDoctorId: number, newBranchId: number, newStartISO: string, newEndISO: string) => void;
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

interface ResizeState {
  appointment: Appointment;
  edge: 'top' | 'bottom';
  startMouseY: number;
  deltaMinutes: number;
}

interface MoveState {
  appointment: Appointment;
  startMouseY: number;
  deltaMinutes: number;
  targetDoctorId: number;
  targetBranchId: number;
}

const SNAP_MINUTES = 5;
const MIN_DURATION_MINUTES = 5;

function snapToGrid(minutes: number): number {
  return Math.round(minutes / SNAP_MINUTES) * SNAP_MINUTES;
}

/** Local minutes → UTC ISO string using a reference Date to preserve the date. */
function localMinutesToISO(ref: Date, localMinutes: number): string {
  const d = new Date(ref);
  d.setHours(Math.floor(localMinutes / 60), localMinutes % 60, 0, 0);
  return d.toISOString();
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
  onAppointmentResize,
  onAppointmentMove,
}: CalendarGridProps) {
  const dayStart = dayStartHour * 60;
  const dayEnd = dayEndHour * 60;
  const slotHeight = Math.round(slotIntervalMinutes * PX_PER_MINUTE);

  const [dragState, setDragState] = useState<DragState | null>(null);
  const isDragging = dragState !== null;

  const [resizeState, setResizeState] = useState<ResizeState | null>(null);
  const resizeRef = useRef<ResizeState | null>(null);
  resizeRef.current = resizeState;
  const isResizing = resizeState !== null;

  const [moveState, setMoveState] = useState<MoveState | null>(null);
  const moveRef = useRef<MoveState | null>(null);
  moveRef.current = moveState;
  const isMoving = moveState !== null;

  // Tracks whether mouse actually moved during the current drag session.
  // Reset by a native mousedown listener on the container (fires regardless of
  // React stopPropagation), set to true in mousemove handlers.
  const dragMovedRef = useRef(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const onDown = () => { dragMovedRef.current = false; };
    el.addEventListener('mousedown', onDown);
    return () => el.removeEventListener('mousedown', onDown);
  }, []);

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

  // Resize drag handlers
  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      dragMovedRef.current = true;
      const state = resizeRef.current;
      if (!state) return;
      const rawDeltaMin = (e.clientY - state.startMouseY) / PX_PER_MINUTE;
      const delta = snapToGrid(rawDeltaMin);

      const origStart = new Date(state.appointment.startTime);
      const origEnd   = new Date(state.appointment.endTime);
      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
      const origEndMin   = origEnd.getHours()   * 60 + origEnd.getMinutes();

      let newDelta: number;
      if (state.edge === 'bottom') {
        const clamped = Math.min(Math.max(origEndMin + delta, origStartMin + MIN_DURATION_MINUTES), dayEnd);
        newDelta = clamped - origEndMin;
      } else {
        const clamped = Math.max(Math.min(origStartMin + delta, origEndMin - MIN_DURATION_MINUTES), dayStart);
        newDelta = clamped - origStartMin;
      }

      setResizeState((prev) => prev ? { ...prev, deltaMinutes: newDelta } : null);
    };

    const handleMouseUp = () => {
      const state = resizeRef.current;
      if (!state || state.deltaMinutes === 0) { setResizeState(null); return; }

      const origStart = new Date(state.appointment.startTime);
      const origEnd   = new Date(state.appointment.endTime);
      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
      const origEndMin   = origEnd.getHours()   * 60 + origEnd.getMinutes();

      // localMinutesToISO handles timezone correctly: setHours (local) → toISOString (UTC)
      onAppointmentResize?.(
        state.appointment,
        state.edge === 'top'
          ? localMinutesToISO(origStart, origStartMin + state.deltaMinutes)
          : origStart.toISOString(),
        state.edge === 'bottom'
          ? localMinutesToISO(origEnd, origEndMin + state.deltaMinutes)
          : origEnd.toISOString(),
      );

      setResizeState(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isResizing, dayStart, dayEnd, onAppointmentResize]);

  // Move drag handlers (sürükle → başka hekim / saat)
  useEffect(() => {
    if (!isMoving) return;

    const handleMouseMove = (e: MouseEvent) => {
      dragMovedRef.current = true;
      const state = moveRef.current;
      if (!state) return;
      const rawDeltaMin = (e.clientY - state.startMouseY) / PX_PER_MINUTE;
      const delta = snapToGrid(rawDeltaMin);

      const origStart = new Date(state.appointment.startTime);
      const origEnd   = new Date(state.appointment.endTime);
      const durationMin = (origEnd.getTime() - origStart.getTime()) / 60000;
      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();

      const clampedStart = Math.min(Math.max(origStartMin + delta, dayStart), dayEnd - durationMin);
      const clampedDelta = clampedStart - origStartMin;

      setMoveState((prev) => prev ? { ...prev, deltaMinutes: clampedDelta } : null);
    };

    const handleMouseUp = () => {
      const state = moveRef.current;
      if (!state) { setMoveState(null); return; }

      if (state.deltaMinutes === 0 && state.targetDoctorId === state.appointment.doctorId && state.targetBranchId === state.appointment.branchId) {
        setMoveState(null);
        return;
      }

      const origStart = new Date(state.appointment.startTime);
      const origEnd   = new Date(state.appointment.endTime);
      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
      const origEndMin   = origEnd.getHours()   * 60 + origEnd.getMinutes();
      const durationMin  = origEndMin - origStartMin;

      const newStartMin = origStartMin + state.deltaMinutes;
      const newEndMin   = newStartMin + durationMin;

      onAppointmentMove?.(
        state.appointment,
        state.targetDoctorId,
        state.targetBranchId,
        localMinutesToISO(origStart, newStartMin),
        localMinutesToISO(origStart, newEndMin),
      );

      setMoveState(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isMoving, dayStart, dayEnd, onAppointmentMove]);

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
      ref={containerRef}
      className="overflow-x-auto border rounded-lg"
      style={{
        userSelect: isDragging || isResizing || isMoving ? 'none' : undefined,
        cursor: isResizing ? 'ns-resize' : isMoving ? 'grabbing' : undefined,
      }}
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
              onMouseEnter={() => {
                if (isMoving) {
                  setMoveState((prev) => prev
                    ? { ...prev, targetDoctorId: doctor.doctorId, targetBranchId: doctor.branchId }
                    : null);
                }
              }}
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
                    <span className="shrink-0 text-[9px] font-bold bg-yellow-100 dark:bg-yellow-900/50 text-yellow-700 dark:text-yellow-400 px-0.5 py-px rounded leading-none">BŞ</span>
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
                        doctor.specialDayType === DoctorSpecialDayType.ExtraWork  && 'bg-green-100 dark:bg-green-900/50 text-green-700 dark:text-green-400',
                        doctor.specialDayType === DoctorSpecialDayType.HourChange && 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-400',
                        doctor.specialDayType === DoctorSpecialDayType.DayOff     && 'bg-red-100 dark:bg-red-900/50 text-red-700 dark:text-red-400',
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
                        slotType === 'closed' && 'bg-gray-100 dark:bg-gray-800/60',
                        slotType === 'break' && 'bg-amber-50 dark:bg-amber-950/30',
                        isPast && slotType === 'open' && 'bg-gray-50 dark:bg-gray-800/30',
                        isBookable && !isDragging && 'bg-white dark:bg-gray-900 hover:bg-blue-50/50 dark:hover:bg-blue-950/30 cursor-crosshair',
                        isBookable && isDragging && isCurrentDragCol && 'bg-white dark:bg-gray-900 cursor-crosshair',
                        isBookable && isDragging && !isCurrentDragCol && 'bg-white dark:bg-gray-900',
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
                        <span className="absolute inset-x-1 top-0.5 text-[10px] text-amber-700 dark:text-amber-400 font-medium truncate leading-none pointer-events-none">
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

                    // Compute preview overrides during resize or move
                    let overrideStart: string | undefined;
                    let overrideEnd: string | undefined;

                    if (resizeState?.appointment.publicId === apt.publicId && resizeState.deltaMinutes !== 0) {
                      const origStart = new Date(apt.startTime);
                      const origEnd   = new Date(apt.endTime);
                      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
                      const origEndMin   = origEnd.getHours()   * 60 + origEnd.getMinutes();
                      // Use localMinutesToISO for preview too (consistent with API call)
                      if (resizeState.edge === 'bottom') {
                        overrideEnd = localMinutesToISO(origEnd, origEndMin + resizeState.deltaMinutes);
                      } else {
                        overrideStart = localMinutesToISO(origStart, origStartMin + resizeState.deltaMinutes);
                      }
                    }

                    if (moveState?.appointment.publicId === apt.publicId && moveState.deltaMinutes !== 0) {
                      const origStart = new Date(apt.startTime);
                      const origEnd   = new Date(apt.endTime);
                      const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
                      const origEndMin   = origEnd.getHours()   * 60 + origEnd.getMinutes();
                      const dur = origEndMin - origStartMin;
                      overrideStart = localMinutesToISO(origStart, origStartMin + moveState.deltaMinutes);
                      overrideEnd   = localMinutesToISO(origStart, origStartMin + moveState.deltaMinutes + dur);
                    }

                    // Hide from original column when dragged to a different doctor
                    const isBeingMovedElsewhere =
                      moveState?.appointment.publicId === apt.publicId &&
                      (moveState.targetDoctorId !== doctor.doctorId || moveState.targetBranchId !== doctor.branchId);

                    if (isBeingMovedElsewhere) return null;

                    // Full-width during same-doctor move
                    const isMovingHere =
                      moveState !== null &&
                      moveState.targetDoctorId === doctor.doctorId &&
                      moveState.targetBranchId === doctor.branchId &&
                      moveState.appointment.publicId === apt.publicId;

                    const isBusy = isResizing || isMoving;

                    return (
                      <AppointmentBlock
                        key={apt.publicId}
                        appointment={apt}
                        statuses={statuses}
                        slotHeight={slotHeight}
                        slotMinutes={slotIntervalMinutes}
                        dayStart={dayStart}
                        lane={isMovingHere ? 0 : laneInfo.lane}
                        totalLanes={isMovingHere ? 1 : laneInfo.totalLanes}
                        onClick={(a) => { if (!dragMovedRef.current) onAppointmentClick(a); }}
                        overrideStartTime={overrideStart}
                        overrideEndTime={overrideEnd}
                        onResizeStart={onAppointmentResize ? (edge, a, e) => {
                          setResizeState({ appointment: a, edge, startMouseY: e.clientY, deltaMinutes: 0 });
                        } : undefined}
                        onMoveStart={onAppointmentMove && [1, 2, 3].includes(apt.statusId) && !apt.isBeingCalled && !(apt.statusId === 3 && apt.hasOpenProtocol) ? (a, e) => {
                          setMoveState({
                            appointment: a,
                            startMouseY: e.clientY,
                            deltaMinutes: 0,
                            targetDoctorId: doctor.doctorId,
                            targetBranchId: doctor.branchId,
                          });
                        } : undefined}
                      />
                    );
                  });
                })()}

                {/* Ghost block when this column is the drag-move target for a different doctor's appointment */}
                {moveState !== null &&
                  moveState.targetDoctorId === doctor.doctorId &&
                  moveState.targetBranchId === doctor.branchId &&
                  (moveState.appointment.doctorId !== doctor.doctorId || moveState.appointment.branchId !== doctor.branchId) &&
                  (() => {
                    const apt = moveState.appointment;
                    const origStart    = new Date(apt.startTime);
                    const origEnd      = new Date(apt.endTime);
                    const origStartMin = origStart.getHours() * 60 + origStart.getMinutes();
                    const dur          = (origEnd.getTime() - origStart.getTime()) / 60000;
                    const ghostStartMin = origStartMin + moveState.deltaMinutes;
                    return (
                      <AppointmentBlock
                        appointment={apt}
                        statuses={statuses}
                        slotHeight={slotHeight}
                        slotMinutes={slotIntervalMinutes}
                        dayStart={dayStart}
                        lane={0}
                        totalLanes={1}
                        onClick={() => {}}
                        overrideStartTime={localMinutesToISO(origStart, ghostStartMin)}
                        overrideEndTime={localMinutesToISO(origStart, ghostStartMin + dur)}
                      />
                    );
                  })()
                }
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
