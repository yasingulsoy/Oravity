import { useMemo } from 'react';
import { Stethoscope } from 'lucide-react';
import type { Appointment, AppointmentStatus } from '@/types/appointment';
import { cn } from '@/lib/utils';
import { getAppointmentStep } from '@/lib/appointmentJourney';

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

interface AppointmentBlockProps {
  appointment: Appointment;
  statuses: AppointmentStatus[];
  slotHeight: number;
  slotMinutes: number;
  dayStart: number; // minutes from midnight, e.g. 480 for 08:00
  lane?: number;
  totalLanes?: number;
  onClick: (appointment: Appointment) => void;
}

export function AppointmentBlock({
  appointment,
  statuses,
  slotHeight,
  slotMinutes,
  dayStart,
  lane = 0,
  totalLanes = 1,
  onClick,
}: AppointmentBlockProps) {
  const status = statuses.find((s) => s.id === appointment.statusId);
  const journeyStep = getAppointmentStep(appointment.statusId);
  const JourneyIcon = journeyStep.icon;

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

  const borderStyle = appointment.isUrgent
    ? '2px solid #ef4444'
    : appointment.isEarlierRequest
      ? '2px dashed #f97316'
      : `1px solid ${status?.borderColor ?? '#d1d5db'}`;

  return (
    <button
      type="button"
      onClick={() => onClick(appointment)}
      className={cn(
        'absolute z-10 overflow-hidden rounded-md px-1.5 py-0.5 text-left text-xs',
        'cursor-pointer hover:opacity-90 transition-opacity'
      )}
      style={{
        top: `${top}px`,
        height: `${height}px`,
        left: `calc(${(lane / totalLanes) * 100}% + 2px)`,
        width: `calc(${(1 / totalLanes) * 100}% - 4px)`,
        backgroundColor: appointment.isUrgent
          ? '#fef2f2'
          : status?.containerColor ?? '#e5e7eb',
        border: borderStyle,
        color: status?.textColor ?? '#374151',
      }}
      title={`${appointment.patientName ?? 'Hasta'} - ${timeStr}${appointment.isUrgent ? ' 🚨 ACİL' : ''}${appointment.isEarlierRequest ? ' ⏰ Erken saat talep' : ''}`}
    >
      <div className="flex items-center gap-1 leading-tight">
        {appointment.isUrgent && (
          <span className="shrink-0 text-[9px] font-bold text-red-600 bg-red-100 px-0.5 rounded leading-none">ACİL</span>
        )}
        {appointment.isEarlierRequest && !appointment.isUrgent && (
          <span className="shrink-0 text-[9px] font-bold text-orange-600 bg-orange-100 px-0.5 rounded leading-none">ERKEN</span>
        )}
        <span className="font-medium truncate">{timeStr}</span>
      </div>
      <div className="flex items-center gap-1 leading-tight">
        <span className="truncate">{appointment.patientName ?? 'İsimsiz Hasta'}</span>
        <div className="flex items-center gap-0.5 shrink-0 ml-auto">
          {/* Protokol açık ama zaten journey icon'u bunu göstermiyorsa (InRoom=5 hariç) */}
          {appointment.hasOpenProtocol && appointment.statusId !== 5 && (
            <Stethoscope className="size-2.5 opacity-80" />
          )}
          {/* title attribute kullanıyoruz — button içinde button olamaz */}
          <span title={journeyStep.label}>
            <JourneyIcon className={cn('size-2.5', journeyStep.color)} />
          </span>
        </div>
      </div>
      {(() => {
        const age = calcAge(appointment.patientBirthDate);
        const gender = genderLabel(appointment.patientGender);
        const info = [age !== null ? `${age}y` : null, gender].filter(Boolean).join(' ');
        return info ? (
          <div className="text-[10px] opacity-60 leading-tight">{info}</div>
        ) : null;
      })()}
      {appointment.appointmentTypeName && (
        <div className="truncate text-[10px] opacity-70 leading-tight">
          {appointment.appointmentTypeName}
        </div>
      )}
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
