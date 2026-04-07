import { useMemo } from 'react';
import type { Appointment, AppointmentStatus } from '@/types/appointment';
import { cn } from '@/lib/utils';

interface AppointmentBlockProps {
  appointment: Appointment;
  statuses: AppointmentStatus[];
  slotHeight: number;
  slotMinutes: number;
  dayStart: number; // minutes from midnight, e.g. 480 for 08:00
  onClick: (appointment: Appointment) => void;
}

export function AppointmentBlock({
  appointment,
  statuses,
  slotHeight,
  slotMinutes,
  dayStart,
  onClick,
}: AppointmentBlockProps) {
  const status = statuses.find((s) => s.id === appointment.statusId);

  const { top, height } = useMemo(() => {
    const start = new Date(appointment.startTime);
    const end = new Date(appointment.endTime);
    const startMinutes = start.getHours() * 60 + start.getMinutes();
    const endMinutes = end.getHours() * 60 + end.getMinutes();
    const pixelsPerMinute = slotHeight / slotMinutes;
    return {
      top: (startMinutes - dayStart) * pixelsPerMinute,
      height: Math.max((endMinutes - startMinutes) * pixelsPerMinute, slotHeight * 0.5),
    };
  }, [appointment.startTime, appointment.endTime, slotHeight, dayStart]);

  const startTime = new Date(appointment.startTime);
  const endTime = new Date(appointment.endTime);
  const timeStr = `${pad(startTime.getHours())}:${pad(startTime.getMinutes())} - ${pad(endTime.getHours())}:${pad(endTime.getMinutes())}`;

  return (
    <button
      type="button"
      onClick={() => onClick(appointment)}
      className={cn(
        'absolute inset-x-1 z-10 overflow-hidden rounded-md border px-1.5 py-0.5 text-left text-xs',
        'cursor-pointer hover:opacity-90 transition-opacity'
      )}
      style={{
        top: `${top}px`,
        height: `${height}px`,
        backgroundColor: status?.containerColor ?? '#e5e7eb',
        borderColor: status?.borderColor ?? '#d1d5db',
        color: status?.textColor ?? '#374151',
      }}
      title={`${appointment.patientName ?? 'Hasta'} - ${timeStr}`}
    >
      <div className="font-medium truncate leading-tight">
        {timeStr}
      </div>
      <div className="truncate leading-tight">
        {appointment.patientName ?? 'Isimsiz Hasta'}
      </div>
      {appointment.notes && height > slotHeight && (
        <div className="truncate text-[10px] opacity-75 leading-tight">
          {appointment.notes}
        </div>
      )}
    </button>
  );
}

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}
