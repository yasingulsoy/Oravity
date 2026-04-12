import { useState, useMemo, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { protocolsApi } from '@/api/visits';
import { appointmentsApi } from '@/api/appointments';
import { DoctorSpecialDayType } from '@/types/appointment';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/store/authStore';

interface Props {
  open: boolean;
  visitPublicId: string;
  patientName: string;
  checkInAt: string;
  defaultDoctorId?: number | null;
  defaultSpecializationId?: number | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function CreateProtocolDialog({
  open,
  visitPublicId,
  patientName,
  checkInAt,
  defaultDoctorId,
  defaultSpecializationId,
  onClose,
  onSuccess,
}: Props) {
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  const [protocolTypeId, setProtocolTypeId] = useState<number | null>(null);
  const [selectedSpecId, setSelectedSpecId] = useState<number | ''>(defaultSpecializationId ?? '');
  const [doctorId, setDoctorId] = useState<number | null>(defaultDoctorId ?? null);

  const today = format(new Date(), 'yyyy-MM-dd');

  // Protokol tipleri — DB'den
  const { data: typesData } = useQuery({
    queryKey: ['protocol-types'],
    queryFn: () => protocolsApi.getTypes(),
    select: (res) => res.data ?? [],
    staleTime: Infinity,
  });
  const protocolTypes = typesData ?? [];
  const effectiveTypeId = protocolTypeId ?? protocolTypes[0]?.id ?? null;

  // Bugün çalışan hekimler (izinliler ve programsızlar hariç)
  const { data: doctorsRaw, isLoading: doctorsLoading } = useQuery({
    queryKey: ['appointments', 'calendar-doctors', today],
    queryFn: () => appointmentsApi.getCalendarDoctors({ date: today }),
    select: (res) => {
      const seen = new Set<number>();
      return (res.data ?? []).filter((d) => {
        if (d.isSpecialDay && d.specialDayType === DoctorSpecialDayType.DayOff) return false;
        if (!d.workStart) return false;
        if (seen.has(d.doctorId)) return false;
        seen.add(d.doctorId);
        return true;
      });
    },
    staleTime: 5 * 60 * 1000,
    enabled: open,
  });
  const doctors = doctorsRaw ?? [];

  // Dialog her açıldığında default değerleri uygula
  // Uzmanlık: önce randevunun specializationId'si, yoksa hekimin takvimden gelen uzmanlığı
  useEffect(() => {
    if (open) {
      setProtocolTypeId(null);
      setDoctorId(defaultDoctorId ?? null);

      if (defaultSpecializationId) {
        setSelectedSpecId(defaultSpecializationId);
      } else if (defaultDoctorId && doctorsRaw) {
        const doc = doctorsRaw.find((d) => d.doctorId === defaultDoctorId);
        setSelectedSpecId(doc?.specializationId ?? '');
      } else {
        setSelectedSpecId('');
      }
    }
  }, [open, defaultDoctorId, defaultSpecializationId, doctorsRaw]);

  // Uzmanlık listesi
  const specializations = useMemo(() => {
    const map = new Map<number, string>();
    for (const d of doctors) {
      if (d.specializationId && d.specializationName && !map.has(d.specializationId))
        map.set(d.specializationId, d.specializationName);
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [doctors]);

  // Seçili uzmanlığa göre filtrelenmiş hekimler
  // Kullanıcı tarafından seçilmiş hekim filtrede yoksa yine de listeye ekle
  // (defaultDoctorId kullanılmaz — uzmanlık değişince listeyi temizle)
  const filteredDoctors = useMemo(() => {
    const base = selectedSpecId
      ? doctors.filter((d) => d.specializationId === Number(selectedSpecId))
      : doctors;
    if (doctorId && !base.some((d) => d.doctorId === doctorId)) {
      const current = doctors.find((d) => d.doctorId === doctorId);
      if (current) return [...base, current];
    }
    return base;
  }, [doctors, selectedSpecId, doctorId]);

  // Filtrede tek hekim varsa otomatik seç
  useEffect(() => {
    if (filteredDoctors.length === 1 && !doctorId) {
      setDoctorId(filteredDoctors[0].doctorId);
    }
  }, [filteredDoctors, doctorId]);

  // Öncelik: 1) kullanıcı seçimi, 2) randevunun doktoru, 3) giriş yapan kullanıcı
  const currentUserId = user?.id ? Number(user.id) : null;
  const effectiveDoctorId =
    doctorId ??
    (filteredDoctors.find((d) => d.doctorId === (defaultDoctorId ?? currentUserId))?.doctorId ?? null);

  const mutation = useMutation({
    mutationFn: () => protocolsApi.create(visitPublicId, effectiveDoctorId!, effectiveTypeId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      onSuccess();
    },
  });

  function handleClose() {
    mutation.reset();
    setProtocolTypeId(null);
    setSelectedSpecId(defaultSpecializationId ?? '');
    setDoctorId(defaultDoctorId ?? null);
    onClose();
  }

  const canSubmit = !!effectiveDoctorId && !!effectiveTypeId && !mutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Protokol Aç</DialogTitle>
        </DialogHeader>

        <form
          onSubmit={(e) => { e.preventDefault(); if (canSubmit) mutation.mutate(); }}
          className="space-y-4"
        >
          {/* Hasta */}
          <div className="rounded-md bg-muted px-3 py-2 text-sm">
            <p className="font-medium">{patientName}</p>
            <p className="text-xs text-muted-foreground">
              Giriş: {format(new Date(checkInAt), 'HH:mm')}
            </p>
          </div>

          {/* Protokol tipi — DB'den */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">Protokol Tipi</label>
            <div className="grid grid-cols-3 gap-1.5">
              {protocolTypes.map((pt) => (
                <button
                  key={pt.id}
                  type="button"
                  onClick={() => setProtocolTypeId(pt.id)}
                  className="rounded-md border px-2 py-1.5 text-xs transition-colors"
                  style={
                    effectiveTypeId === pt.id
                      ? { backgroundColor: pt.color, borderColor: pt.color, color: '#fff' }
                      : { borderColor: pt.color, color: pt.color }
                  }
                >
                  {pt.name}
                </button>
              ))}
            </div>
          </div>

          {/* Uzmanlık — dropdown */}
          {specializations.length > 1 && (
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Uzmanlık</label>
              <select
                value={selectedSpecId}
                onChange={(e) => {
                  setSelectedSpecId(e.target.value ? Number(e.target.value) : '');
                  setDoctorId(null);
                }}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
              >
                <option value="">Tüm uzmanlıklar</option>
                {specializations.map((s) => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
            </div>
          )}

          {/* Hekim */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">Hekim</label>
            {doctorsLoading ? (
              <p className="text-xs text-muted-foreground">Yükleniyor...</p>
            ) : filteredDoctors.length === 0 ? (
              <p className="text-xs text-destructive">Bu uzmanlıkta bugün çalışan hekim yok.</p>
            ) : (
              <select
                value={effectiveDoctorId ?? ''}
                onChange={(e) => setDoctorId(e.target.value ? Number(e.target.value) : null)}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
              >
                <option value="">— Hekim seçin —</option>
                {filteredDoctors.map((d) => (
                  <option key={d.doctorId} value={d.doctorId}>
                    {d.title ? `${d.title} ` : ''}{d.fullName}
                    {d.workStart && d.workEnd
                      ? ` (${d.workStart.slice(0, 5)}–${d.workEnd.slice(0, 5)})`
                      : ''}
                  </option>
                ))}
              </select>
            )}
          </div>

          {mutation.isError && (
            <p className="text-xs text-destructive">Protokol açılamadı. Tekrar deneyin.</p>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>Vazgeç</Button>
            <Button type="submit" disabled={!canSubmit}>
              {mutation.isPending ? 'Açılıyor...' : 'Protokol Aç'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
