import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  Stethoscope, Clock, CheckCheck,
  AlertCircle, PlayCircle, BellRing, UserCheck,
  ChevronRight, Check, DoorOpen,
} from 'lucide-react';
import { toast } from 'sonner';
import { appointmentsApi } from '@/api/appointments';
import { protocolsApi, visitsApi } from '@/api/visits';
import { useAuthStore } from '@/store/authStore';
import { useCalendarSocket } from '@/hooks/useCalendarSocket';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { Appointment, DoctorCalendarInfo } from '@/types/appointment';
import type { DoctorProtocol } from '@/types/visit';
import { cn } from '@/lib/utils';

const today = format(new Date(), 'yyyy-MM-dd');

function calcAge(birthDate: string | null | undefined): number | null {
  if (!birthDate) return null;
  const birth = new Date(birthDate);
  const now = new Date();
  let age = now.getFullYear() - birth.getFullYear();
  const m = now.getMonth() - birth.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < birth.getDate())) age--;
  return age;
}

function genderLabel(g: string | null | undefined) {
  if (!g) return null;
  return g === 'M' || g === 'Male' ? 'E' : g === 'F' || g === 'Female' ? 'K' : null;
}

const protocolTypeDot: Record<number, string> = {
  1: 'bg-blue-500',
  2: 'bg-emerald-500',
  3: 'bg-purple-500',
  4: 'bg-amber-500',
  5: 'bg-red-500',
};

// ─── Doctor selector ───────────────────────────────────────────────────────

interface DoctorSelectorProps {
  doctors: DoctorCalendarInfo[];
  value: number | null;
  onChange: (id: number) => void;
  isLoading: boolean;
}

function DoctorSelector({ doctors, value, onChange, isLoading }: DoctorSelectorProps) {
  if (isLoading) {
    return <div className="h-8 w-52 rounded-md bg-muted animate-pulse" />;
  }
  if (doctors.length === 0) {
    return <span className="text-xs text-muted-foreground">Bugün takvimde hekim yok</span>;
  }

  const selected = doctors.find((d) => d.doctorId === value);

  // Tek hekim varsa (kendi ekranı) dropdown yerine label göster
  if (doctors.length === 1) {
    const d = doctors[0];
    const label = d.title ? `${d.title} ${d.fullName}` : d.fullName;
    return (
      <span className="flex items-center gap-1.5 text-sm font-medium">
        {d.calendarColor && (
          <span
            className="inline-block h-2 w-2 rounded-full shrink-0"
            style={{ background: d.calendarColor }}
          />
        )}
        {label}
      </span>
    );
  }

  const triggerLabel = selected
    ? (selected.title ? `${selected.title} ${selected.fullName}` : selected.fullName)
    : 'Hekim seç...';

  return (
    <Select
      value={value?.toString() ?? ''}
      onValueChange={(v) => onChange(Number(v))}
    >
      <SelectTrigger className="h-8 w-52 text-sm">
        <span className="flex items-center gap-1.5 truncate">
          {selected?.calendarColor && (
            <span
              className="inline-block h-2 w-2 rounded-full shrink-0"
              style={{ background: selected.calendarColor }}
            />
          )}
          <span className="truncate">{triggerLabel}</span>
        </span>
      </SelectTrigger>
      <SelectContent>
        {doctors.map((d) => (
          <SelectItem key={d.doctorId} value={d.doctorId.toString()}>
            <span className="flex items-center gap-1.5">
              {d.calendarColor && (
                <span
                  className="inline-block h-2 w-2 rounded-full shrink-0"
                  style={{ background: d.calendarColor }}
                />
              )}
              {d.title ? `${d.title} ${d.fullName}` : d.fullName}
            </span>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

// ─── Appointment card ──────────────────────────────────────────────────────

interface AppointmentCardProps {
  apt: Appointment;
  isActive?: boolean;
  onCall?: (apt: Appointment) => void;
  isCallingApt?: boolean;
}

function AppointmentCard({ apt, isActive, onCall, isCallingApt }: AppointmentCardProps) {
  const age = calcAge(apt.patientBirthDate);
  const gender = genderLabel(apt.patientGender);

  // Arrived=3, odaya alınmamış ve açık protokol yok → Çağır butonu göster
  const canCall = apt.statusId === 3 && !isActive && !apt.hasOpenProtocol;

  return (
    <div
      className={cn(
        'flex flex-col gap-2 rounded-lg border border-l-4 bg-background p-3 shadow-sm transition-colors',
        isActive
          ? 'border-l-emerald-500 ring-1 ring-emerald-500/30 bg-emerald-50/30 dark:bg-emerald-950/20'
          : apt.statusId === 3
            ? 'border-l-amber-400'
            : 'border-l-slate-300',
      )}
    >
      <div className="flex items-start gap-3">
        <div className="flex flex-col items-center gap-0.5 text-muted-foreground text-xs w-10 shrink-0">
          <Clock className="h-3 w-3" />
          <span>{new Date(apt.startTime).toTimeString().slice(0, 5)}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium text-sm truncate">{apt.patientName}</p>
          <p className="text-xs text-muted-foreground">
            {[age != null ? `${age}y` : null, gender].filter(Boolean).join(' · ')}
          </p>
          {apt.notes && (
            <p className="text-xs text-muted-foreground mt-0.5 truncate">{apt.notes}</p>
          )}
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          <Badge variant="outline" className="text-xs">{apt.statusLabel}</Badge>
          {isActive && (
            <span className="flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
              <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
              Odada
            </span>
          )}
          {!isActive && apt.hasOpenProtocol && (
            <Stethoscope className="h-3.5 w-3.5 text-blue-500" />
          )}
        </div>
      </div>

      {canCall && onCall && (
        <div className="relative overflow-hidden rounded-md">
          {/* Pulsing ring — clipped by overflow-hidden, no layout side-effects */}
          {!isCallingApt && (
            <span className="absolute inset-0 bg-emerald-400/30 animate-ping pointer-events-none" />
          )}
          <Button
            size="sm"
            variant="default"
            className="relative h-7 w-full text-xs gap-1.5 bg-emerald-500 hover:bg-emerald-600 text-white border-0 shadow-sm"
            disabled={isCallingApt}
            onClick={() => onCall(apt)}
          >
            <BellRing className={cn('h-3.5 w-3.5', !isCallingApt && 'animate-bounce')} />
            {isCallingApt ? 'Çağrılıyor...' : 'Hastayı Çağır'}
          </Button>
        </div>
      )}
    </div>
  );
}

// ─── In-room protocol card (prominent) ────────────────────────────────────

function InRoomCard({
  protocol,
  onExamine,
}: {
  protocol: DoctorProtocol;
  onExamine: (p: DoctorProtocol) => void;
}) {
  const dotColor = protocolTypeDot[protocol.protocolType] ?? 'bg-slate-400';

  return (
    <div className="rounded-xl border-2 border-emerald-500/60 bg-emerald-50/40 dark:bg-emerald-950/20 p-4 shadow-md">
      <div className="flex items-start gap-3">
        {/* Pulse indicator */}
        <div className="relative mt-1 shrink-0">
          <span className={cn('inline-block h-3 w-3 rounded-full', dotColor)} />
          <span
            className={cn(
              'absolute inset-0 rounded-full animate-ping opacity-60',
              dotColor,
            )}
          />
        </div>

        <div className="flex-1 min-w-0">
          {/* Status row */}
          <div className="flex items-center gap-2 mb-1">
            <Badge className="bg-emerald-500 hover:bg-emerald-500 text-white text-xs gap-1 h-5 px-1.5">
              <UserCheck className="h-3 w-3" />
              Odada
            </Badge>
            <span className="text-xs font-mono text-muted-foreground">{protocol.protocolNo}</span>
            <span className="text-xs text-muted-foreground">{protocol.protocolTypeName}</span>
          </div>

          {/* Patient name */}
          <p className="text-base font-semibold truncate">{protocol.patientName}</p>

          {/* Called time */}
          {!!protocol.startedAt && (
            <p className="text-xs text-muted-foreground mt-0.5 flex items-center gap-1">
              <PlayCircle className="h-3 w-3 text-emerald-500" />
              Odaya alındı {format(new Date(protocol.startedAt!), 'HH:mm')}
            </p>
          )}
        </div>

        {/* Action */}
        <Button
          size="sm"
          className="h-8 text-xs gap-1 shrink-0"
          onClick={() => onExamine(protocol)}
        >
          <Stethoscope className="h-3.5 w-3.5" />
          Tedaviye Başla
        </Button>
      </div>
    </div>
  );
}

// ─── Waiting protocol card ─────────────────────────────────────────────────

function WaitingCard({
  protocol,
  onCall,
  isCallingId,
}: {
  protocol: DoctorProtocol;
  onCall: (p: DoctorProtocol) => void;
  isCallingId: string | null;
}) {
  const dotColor = protocolTypeDot[protocol.protocolType] ?? 'bg-slate-400';
  const isCalling = isCallingId === protocol.publicId;

  return (
    <div className="flex items-center gap-3 rounded-lg border bg-background p-3 shadow-sm">
      <span className={cn('inline-block h-2.5 w-2.5 rounded-full shrink-0', dotColor)} />
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5">
          <span className="text-xs font-mono text-muted-foreground">{protocol.protocolNo}</span>
          <span className="text-xs text-muted-foreground">·</span>
          <span className="text-xs text-muted-foreground">{protocol.protocolTypeName}</span>
        </div>
        <p className="font-medium text-sm truncate mt-0.5">{protocol.patientName}</p>
      </div>
      <Button
        size="sm"
        variant="outline"
        className="h-7 text-xs gap-1 shrink-0 border-emerald-300 text-emerald-700 hover:bg-emerald-50"
        disabled={isCalling}
        onClick={() => onCall(protocol)}
      >
        <DoorOpen className="h-3.5 w-3.5" />
        {isCalling ? 'Alınıyor...' : 'Odaya Al'}
      </Button>
    </div>
  );
}

// ─── Completed protocol card ───────────────────────────────────────────────

function CompletedCard({ protocol }: { protocol: DoctorProtocol }) {
  const dotColor = protocolTypeDot[protocol.protocolType] ?? 'bg-slate-400';

  return (
    <div className="flex items-center gap-3 rounded-lg border bg-background/60 p-3 opacity-60">
      <span className={cn('inline-block h-2 w-2 rounded-full shrink-0', dotColor)} />
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5">
          <span className="text-xs font-mono text-muted-foreground">{protocol.protocolNo}</span>
          <span className="text-xs text-muted-foreground">·</span>
          <span className="text-xs text-muted-foreground">{protocol.protocolTypeName}</span>
        </div>
        <p className="text-sm truncate mt-0.5">{protocol.patientName}</p>
      </div>
      <div className="flex items-center gap-1 text-xs text-muted-foreground shrink-0">
        <Check className="h-3 w-3 text-emerald-500" />
        <span>
          {protocol.startedAt
            ? format(new Date(protocol.startedAt), 'HH:mm')
            : 'Tamamlandı'}
        </span>
      </div>
    </div>
  );
}

// ─── Section divider ───────────────────────────────────────────────────────

function SectionDivider({ label }: { label: string }) {
  return (
    <div className="flex items-center gap-2 py-1">
      <div className="h-px flex-1 bg-border" />
      <span className="text-xs text-muted-foreground">{label}</span>
      <div className="h-px flex-1 bg-border" />
    </div>
  );
}

// ─── Complete dialog ───────────────────────────────────────────────────────

interface CompleteDialogProps {
  protocol: DoctorProtocol | null;
  onConfirm: (diagnosis: string, notes: string) => void;
  onCancel: () => void;
  isPending: boolean;
}

function CompleteDialog({ protocol, onConfirm, onCancel, isPending }: CompleteDialogProps) {
  const [diagnosis, setDiagnosis] = useState('');
  const [notes, setNotes] = useState('');

  // Reset fields when dialog opens for a new protocol
  useEffect(() => {
    if (protocol) {
      setDiagnosis('');
      setNotes('');
    }
  }, [protocol?.publicId]);

  if (!protocol) return null;

  return (
    <Dialog open onOpenChange={(open) => { if (!open) onCancel(); }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Protokolü Kapat</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-2">
          <div className="rounded-lg bg-muted/50 px-3 py-2 text-sm">
            <span className="font-medium">{protocol.patientName}</span>
            <span className="text-muted-foreground ml-2 text-xs">{protocol.protocolNo}</span>
          </div>
          <div className="space-y-1">
            <Label htmlFor="diagnosis">Tanı</Label>
            <Textarea
              id="diagnosis"
              placeholder="Tanı / ICD kodu..."
              rows={2}
              value={diagnosis}
              onChange={(e) => setDiagnosis(e.target.value)}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="notes">Notlar</Label>
            <Textarea
              id="notes"
              placeholder="Muayene notları..."
              rows={3}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isPending}>
            İptal
          </Button>
          <Button onClick={() => onConfirm(diagnosis, notes)} disabled={isPending}>
            {isPending ? 'Kaydediliyor...' : 'Protokolü Kapat'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Main page ─────────────────────────────────────────────────────────────

export function DoctorDashboardPage() {
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [selectedDoctorId, setSelectedDoctorId] = useState<number | null>(null);
  const [callingId, setCallingId] = useState<string | null>(null);
  const [callingAptId, setCallingAptId] = useState<string | null>(null);
  const [closeTarget, setCloseTarget] = useState<DoctorProtocol | null>(null);

  // SignalR: protokol veya vizit değişince anlık güncelle
  useCalendarSocket(() => {
    qc.invalidateQueries({ queryKey: ['doctor-protocols'] });
    qc.invalidateQueries({ queryKey: ['doctor-appointments'] });
  });

  const { data: calendarDoctors = [], isLoading: doctorsLoading } = useQuery({
    queryKey: ['calendar-doctors', today],
    queryFn: () => appointmentsApi.getCalendarDoctors({ date: today }).then((r) => r.data),
  });

  const myId = user?.id ? Number(user.id) : null;
  // Giriş yapan kullanıcı takvimde bir hekim olarak varsa yalnızca kendisini göster
  const isOwnDoctor = myId !== null && calendarDoctors.some((d) => d.doctorId === myId);
  const visibleDoctors = isOwnDoctor
    ? calendarDoctors.filter((d) => d.doctorId === myId)
    : calendarDoctors;

  useEffect(() => {
    if (calendarDoctors.length === 0 || selectedDoctorId !== null) return;
    const mine = myId ? calendarDoctors.find((d) => d.doctorId === myId) : null;
    setSelectedDoctorId(mine ? mine.doctorId : calendarDoctors[0].doctorId);
  }, [calendarDoctors, myId, selectedDoctorId]);

  const { data: appointments = [], isLoading: aptsLoading } = useQuery({
    queryKey: ['doctor-appointments', today, selectedDoctorId],
    queryFn: () =>
      appointmentsApi
        .getByDate(today, undefined, selectedDoctorId!.toString())
        .then((r) => r.data),
    enabled: selectedDoctorId !== null,
    refetchInterval: 15_000,
  });

  const { data: protocols = [], isLoading: protoLoading } = useQuery({
    queryKey: ['doctor-protocols', selectedDoctorId],
    queryFn: () =>
      protocolsApi.getMyProtocols(selectedDoctorId!).then((r) => r.data),
    enabled: selectedDoctorId !== null,
    refetchInterval: 15_000,
  });

  const startMutation = useMutation({
    mutationFn: (publicId: string) => protocolsApi.start(publicId),
    onSuccess: () => {
      toast.success('Hasta odaya alındı.');
      qc.invalidateQueries({ queryKey: ['doctor-protocols'] });
      qc.invalidateQueries({ queryKey: ['doctor-appointments'] });
      setCallingId(null);
    },
    onError: () => {
      toast.error('İşlem başarısız.');
      setCallingId(null);
    },
  });

  const completeMutation = useMutation({
    mutationFn: ({
      publicId,
      diagnosis,
      notes,
    }: {
      publicId: string;
      diagnosis: string;
      notes: string;
    }) => protocolsApi.complete(publicId, diagnosis || undefined, notes || undefined),
    onSuccess: () => {
      toast.success('Protokol kapatıldı.');
      qc.invalidateQueries({ queryKey: ['doctor-protocols'] });
      qc.invalidateQueries({ queryKey: ['doctor-appointments'] });
      setCloseTarget(null);
    },
    onError: () => {
      toast.error('Protokol kapatılamadı.');
    },
  });

  const requestCallMutation = useMutation({
    mutationFn: (publicId: string) => visitsApi.requestCall(publicId),
    onSuccess: (res, publicId) => {
      const { protocolStarted, patientName } = res.data;
      if (protocolStarted) {
        toast.success(`${patientName} odaya çağrıldı.`);
        qc.invalidateQueries({ queryKey: ['doctor-protocols'] });
      } else {
        toast.info(`Resepsiyona bildirim gönderildi — ${patientName} için protokol açılması istendi.`);
      }
      // Her iki tarafı da hemen güncelle (SignalR'a ek olarak)
      qc.invalidateQueries({ queryKey: ['doctor-appointments'] });
      qc.invalidateQueries({ queryKey: ['appointments'] });
      qc.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      setCallingAptId(null);
    },
    onError: () => {
      toast.error('İşlem başarısız.');
      setCallingAptId(null);
    },
  });

  const handleExamine = (p: DoctorProtocol) => {
    const params = new URLSearchParams({
      patient: p.patientName,
      no: p.protocolNo,
      patientPublicId: p.patientPublicId,
      type: p.protocolTypeName,
    });
    navigate(`/muayene/${p.publicId}?${params.toString()}`);
  };

  const handleAptCall = (apt: Appointment) => {
    setCallingAptId(apt.publicId);
    requestCallMutation.mutate(apt.publicId);
  };

  const handleCall = (p: DoctorProtocol) => {
    setCallingId(p.publicId);
    startMutation.mutate(p.publicId);
  };

  // Protocol segments — use truthy/falsy to handle both null and undefined from API
  const inRoomProtocols = protocols.filter((p) => p.status === 1 && !!p.startedAt);
  const waitingProtocols = protocols.filter((p) => p.status === 1 && !p.startedAt);
  const completedProtocols = protocols.filter((p) => p.status === 2);

  // Klinikte aktif olan hastalar: Geldi(3), Odada(5)
  const PRESENT_STATUSES = new Set([3, 5]);

  const sortedAppointments = appointments
    .filter((a) => PRESENT_STATUSES.has(a.statusId))
    .slice()
    .sort((a, b) => (a.startTime ?? '').localeCompare(b.startTime ?? ''));

  return (
    <div className="flex h-full flex-col gap-4">
      {/* ── Top bar ──────────────────────────────────────────────── */}
      <div className="flex items-center gap-3">
        <span className="text-xs text-muted-foreground shrink-0">Hekim:</span>
        <DoctorSelector
          doctors={visibleDoctors}
          value={selectedDoctorId}
          onChange={(id) => setSelectedDoctorId(id)}
          isLoading={doctorsLoading}
        />
        <span className="ml-auto text-xs text-muted-foreground">
          {format(new Date(), 'd MMMM yyyy', { locale: tr })}
        </span>
      </div>

      {/* ── Main panels ──────────────────────────────────────────── */}
      <div className="flex flex-1 gap-4 min-h-0">

        {/* ── Left: Today's appointments ───────────────────────── */}
        <div className="flex w-72 shrink-0 flex-col gap-2">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold">Bekleyen Hastalar</h2>
            {!aptsLoading && appointments.length > 0 && (
              <span className="text-xs text-muted-foreground">{appointments.length} randevu</span>
            )}
          </div>

          {aptsLoading ? (
            <div className="space-y-2">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="h-16 rounded-lg bg-muted animate-pulse" />
              ))}
            </div>
          ) : appointments.length === 0 ? (
            <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed p-8 text-center">
              <AlertCircle className="h-8 w-8 text-muted-foreground/40" />
              <p className="text-sm text-muted-foreground">Bugün randevu yok</p>
            </div>
          ) : (
            <div className="space-y-2 overflow-y-auto pr-1">
              {sortedAppointments.map((apt) => (
                <AppointmentCard
                  key={apt.publicId}
                  apt={apt}
                  isActive={apt.statusId === 5}
                  onCall={handleAptCall}
                  isCallingApt={callingAptId === apt.publicId}
                />
              ))}
            </div>
          )}
        </div>

        {/* ── Right: Protocols ──────────────────────────────────── */}
        <div className="flex flex-1 flex-col gap-3 min-w-0">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold">Protokoller</h2>
            <div className="flex items-center gap-3 text-xs text-muted-foreground">
              {inRoomProtocols.length > 0 && (
                <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium">
                  <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
                  {inRoomProtocols.length} odada
                </span>
              )}
              {waitingProtocols.length > 0 && (
                <span>{waitingProtocols.length} bekliyor</span>
              )}
              {completedProtocols.length > 0 && (
                <span>{completedProtocols.length} tamamlandı</span>
              )}
            </div>
          </div>

          {protoLoading ? (
            <div className="space-y-2">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="h-20 rounded-lg bg-muted animate-pulse" />
              ))}
            </div>
          ) : protocols.length === 0 ? (
            <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed p-8 text-center">
              <Stethoscope className="h-8 w-8 text-muted-foreground/40" />
              <p className="text-sm text-muted-foreground">Bugün protokol yok</p>
            </div>
          ) : (
            <div className="space-y-2 overflow-y-auto pr-1">

              {/* ── In-room section ─────────────────────────── */}
              {inRoomProtocols.length > 0 && (
                <>
                  {inRoomProtocols.map((p) => (
                    <InRoomCard
                      key={p.publicId}
                      protocol={p}
                      onExamine={handleExamine}
                    />
                  ))}
                </>
              )}

              {/* ── Waiting section ─────────────────────────── */}
              {waitingProtocols.length > 0 && (
                <>
                  {inRoomProtocols.length > 0 && (
                    <SectionDivider label="Bekleyenler" />
                  )}
                  {waitingProtocols.map((p) => (
                    <WaitingCard
                      key={p.publicId}
                      protocol={p}
                      onCall={handleCall}
                      isCallingId={callingId}
                    />
                  ))}
                </>
              )}

              {/* ── No active patients placeholder ──────────── */}
              {inRoomProtocols.length === 0 && waitingProtocols.length === 0 && completedProtocols.length > 0 && (
                <div className="flex items-center gap-2 rounded-lg border border-dashed p-4 text-center">
                  <CheckCheck className="h-5 w-5 text-emerald-500 mx-auto" />
                  <p className="text-sm text-muted-foreground">Tüm hastalar tamamlandı</p>
                </div>
              )}

              {/* ── Completed section ────────────────────────── */}
              {completedProtocols.length > 0 && (
                <>
                  <SectionDivider label="Tamamlananlar" />
                  {completedProtocols.map((p) => (
                    <CompletedCard key={p.publicId} protocol={p} />
                  ))}
                </>
              )}
            </div>
          )}
        </div>
      </div>

      {/* ── Complete dialog ──────────────────────────────────────── */}
      {closeTarget && (
        <CompleteDialog
          protocol={closeTarget}
          onConfirm={(diagnosis, notes) => {
            completeMutation.mutate({
              publicId: closeTarget.publicId,
              diagnosis,
              notes,
            });
          }}
          onCancel={() => setCloseTarget(null)}
          isPending={completeMutation.isPending}
        />
      )}
    </div>
  );
}
