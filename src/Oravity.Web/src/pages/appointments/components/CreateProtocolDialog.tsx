import { useState, useMemo } from 'react';
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

// Backend Protocol enum'u ile birebir eşleşir (değiştirme)
const PROTOCOL_TYPES = [
  { value: 1, label: 'Muayene' },
  { value: 2, label: 'Tedavi' },
  { value: 3, label: 'Konsültasyon' },
  { value: 4, label: 'Kontrol' },
  { value: 5, label: 'Acil' },
];

interface Props {
  open: boolean;
  visitPublicId: string;
  patientName: string;
  checkInAt: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function CreateProtocolDialog({
  open,
  visitPublicId,
  patientName,
  checkInAt,
  onClose,
  onSuccess,
}: Props) {
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  const [protocolType, setProtocolType] = useState(1);
  const [selectedSpecId, setSelectedSpecId] = useState<number | null>(null);
  const [doctorId, setDoctorId] = useState<number | null>(null);

  const today = format(new Date(), 'yyyy-MM-dd');

  // Bugün çalışma saati olan tüm hekimler (izinliler hariç)
  const { data: doctorsRaw, isLoading: doctorsLoading } = useQuery({
    queryKey: ['appointments', 'calendar-doctors', today],
    queryFn: () => appointmentsApi.getCalendarDoctors({ date: today }),
    select: (res) => {
      const seen = new Set<number>();
      return (res.data ?? []).filter((d) => {
        // İzinli doktorları dışla
        if (d.isSpecialDay && d.specialDayType === DoctorSpecialDayType.DayOff) return false;
        // Çalışma saati tanımlı olmalı
        if (!d.workStart) return false;
        // Aynı doktor birden fazla şubede varsa bir kez göster
        if (seen.has(d.doctorId)) return false;
        seen.add(d.doctorId);
        return true;
      });
    },
    staleTime: 5 * 60 * 1000,
    enabled: open,
  });
  const doctors = doctorsRaw ?? [];

  // Uzmanlik listesi (mevcut doktorlardan türet)
  const specializations = useMemo(() => {
    const map = new Map<number, string>();
    for (const d of doctors) {
      if (d.specializationId && d.specializationName && !map.has(d.specializationId)) {
        map.set(d.specializationId, d.specializationName);
      }
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [doctors]);

  // Seçili uzmanlığa göre filtrelenmiş hekimler
  const filteredDoctors = useMemo(() => {
    if (!selectedSpecId) return doctors;
    return doctors.filter((d) => d.specializationId === selectedSpecId);
  }, [doctors, selectedSpecId]);

  // Giriş yapan kullanıcı hekimse otomatik seç
  const currentUserId = user?.id ? Number(user.id) : null;
  const effectiveDoctorId = doctorId ?? (() => {
    const found = filteredDoctors.find((d) => d.doctorId === currentUserId);
    return found?.doctorId ?? null;
  })();

  const mutation = useMutation({
    mutationFn: () => protocolsApi.create(visitPublicId, effectiveDoctorId!, protocolType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      onSuccess();
    },
  });

  function handleSpecChange(specId: number | null) {
    setSelectedSpecId(specId);
    setDoctorId(null); // uzmanlık değişince hekim sıfırla
  }

  function handleClose() {
    mutation.reset();
    setProtocolType(1);
    setSelectedSpecId(null);
    setDoctorId(null);
    onClose();
  }

  const canSubmit = !!effectiveDoctorId && !mutation.isPending;

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
            <p className="text-muted-foreground text-xs">
              Giriş: {format(new Date(checkInAt), 'HH:mm')}
            </p>
          </div>

          {/* Protokol tipi */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">Protokol Tipi</label>
            <div className="grid grid-cols-3 gap-1.5">
              {PROTOCOL_TYPES.map((pt) => (
                <button
                  key={pt.value}
                  type="button"
                  onClick={() => setProtocolType(pt.value)}
                  className={`rounded-md border px-2 py-1.5 text-xs transition-colors ${
                    protocolType === pt.value
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'border-border hover:bg-muted'
                  }`}
                >
                  {pt.label}
                </button>
              ))}
            </div>
          </div>

          {/* Uzmanlık */}
          {specializations.length > 1 && (
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Uzmanlık</label>
              <div className="flex flex-wrap gap-1.5">
                <button
                  type="button"
                  onClick={() => handleSpecChange(null)}
                  className={`rounded-md border px-2 py-1 text-xs transition-colors ${
                    !selectedSpecId
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'border-border hover:bg-muted'
                  }`}
                >
                  Tümü
                </button>
                {specializations.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => handleSpecChange(s.id)}
                    className={`rounded-md border px-2 py-1 text-xs transition-colors ${
                      selectedSpecId === s.id
                        ? 'border-primary bg-primary text-primary-foreground'
                        : 'border-border hover:bg-muted'
                    }`}
                  >
                    {s.name}
                  </button>
                ))}
              </div>
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
                    {d.workStart && d.workEnd ? ` (${d.workStart.slice(0, 5)}–${d.workEnd.slice(0, 5)})` : ''}
                  </option>
                ))}
              </select>
            )}
          </div>

          {mutation.isError && (
            <p className="text-xs text-destructive">Protokol açılamadı. Tekrar deneyin.</p>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={!canSubmit}>
              {mutation.isPending ? 'Açılıyor...' : 'Protokol Aç'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
