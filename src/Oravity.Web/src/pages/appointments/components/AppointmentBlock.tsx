import { useMemo } from 'react';
import { Stethoscope, BellRing } from 'lucide-react';
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
  /** Override times used during drag preview */
  overrideStartTime?: string;
  overrideEndTime?: string;
  onResizeStart?: (edge: 'top' | 'bottom', apt: Appointment, e: React.MouseEvent) => void;
  onMoveStart?: (apt: Appointment, e: React.MouseEvent) => void;
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
  overrideStartTime,
  overrideEndTime,
  onResizeStart,
  onMoveStart,
}: AppointmentBlockProps) {
  const status = statuses.find((s) => s.id === appointment.statusId);
  const journeyStep = getAppointmentStep(appointment.statusId);
  const JourneyIcon = journeyStep.icon;

  const effectiveStart = overrideStartTime ?? appointment.startTime;
  const effectiveEnd   = overrideEndTime   ?? appointment.endTime;

  const { top, height } = useMemo(() => {
    const start = new Date(effectiveStart);
    const end   = new Date(effectiveEnd);
    const startMinutes = start.getHours() * 60 + start.getMinutes();
    const endMinutes   = end.getHours()   * 60 + end.getMinutes();
    const pixelsPerMinute = slotHeight / slotMinutes;
    return {
      top:    (startMinutes - dayStart) * pixelsPerMinute,
      height: Math.max((endMinutes - startMinutes) * pixelsPerMinute, slotHeight * 0.5),
    };
  }, [effectiveStart, effectiveEnd, slotHeight, dayStart, slotMinutes]);

  const startTime = new Date(effectiveStart);
  const endTime   = new Date(effectiveEnd);
  const timeStr = `${pad(startTime.getHours())}:${pad(startTime.getMinutes())} - ${pad(endTime.getHours())}:${pad(endTime.getMinutes())}`;

  const isCalling = !!appointment.isBeingCalled;

  const borderStyle = appointment.isUrgent
    ? '2px solid #ef4444'
    : isCalling
      ? '2px solid #f59e0b'
      : appointment.isEarlierRequest
        ? '2px dashed #f97316'
        : `1px solid ${status?.borderColor ?? '#d1d5db'}`;

  return (
    <button
      type="button"
      onClick={() => onClick(appointment)}
      className={cn(
        'absolute z-10 overflow-hidden rounded-md px-1.5 py-0.5 text-left text-xs',
        'cursor-pointer hover:opacity-90 transition-opacity',
        (overrideStartTime || overrideEndTime) && 'ring-2 ring-blue-400 ring-offset-1 opacity-90',
      )}
      style={{
        top: `${top}px`,
        height: `${height}px`,
        left: `calc(${(lane / totalLanes) * 100}% + 2px)`,
        width: `calc(${(1 / totalLanes) * 100}% - 4px)`,
        backgroundColor: appointment.isUrgent
          ? '#fef2f2'
          : isCalling
            ? '#fffbeb'
            : status?.containerColor ?? '#e5e7eb',
        border: borderStyle,
        color: status?.textColor ?? '#374151',
      }}
      title={`${appointment.patientName ?? 'Hasta'} - ${timeStr}${appointment.isUrgent ? ' 🚨 ACİL' : ''}${appointment.isEarlierRequest ? ' ⏰ Erken saat talep' : ''}`}
    >
      {/* Top resize handle */}
      {onResizeStart && (
        <div
          className="absolute top-0 left-0 right-0 h-1.5 cursor-ns-resize z-20"
          onMouseDown={(e) => { e.stopPropagation(); e.preventDefault(); onResizeStart('top', appointment, e); }}
          onClick={(e) => { e.stopPropagation(); e.preventDefault(); }}
        />
      )}

      {/* Move handle — orta alan, sürükleyerek hekime/saate taşı */}
      {onMoveStart && (
        <div
          className="absolute inset-x-0 top-1.5 bottom-2 cursor-grab z-20 active:cursor-grabbing"
          onMouseDown={(e) => { e.stopPropagation(); e.preventDefault(); onMoveStart(appointment, e); }}
        />
      )}

      {isCalling && (
        <div className="flex items-center gap-0.5 text-[9px] font-bold text-amber-700 bg-amber-200 px-1 py-0.5 rounded leading-none mb-0.5 -mx-0.5">
          <BellRing className="size-2.5 animate-bounce shrink-0" />
          Hekim Çağırdı
        </div>
      )}
      <div className="flex items-center gap-1 leading-tight">
        {appointment.isUrgent && (
          <span className="shrink-0 text-[9px] font-bold text-red-600 dark:text-red-400 bg-red-100 dark:bg-red-900/50 px-0.5 rounded leading-none">ACİL</span>
        )}
        {appointment.isEarlierRequest && !appointment.isUrgent && (
          <span className="shrink-0 text-[9px] font-bold text-orange-600 dark:text-orange-400 bg-orange-100 dark:bg-orange-900/50 px-0.5 rounded leading-none">ERKEN</span>
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

      {/* Bottom resize handle */}
      {onResizeStart && (
        <div
          className="absolute bottom-0 left-0 right-0 h-2 cursor-ns-resize z-20 flex items-center justify-center"
          onMouseDown={(e) => { e.stopPropagation(); e.preventDefault(); onResizeStart('bottom', appointment, e); }}
          onClick={(e) => { e.stopPropagation(); e.preventDefault(); }}
        >
          <div className="w-6 h-0.5 rounded-full bg-current opacity-40" />
        </div>
      )}
    </button>
  );
}

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}
