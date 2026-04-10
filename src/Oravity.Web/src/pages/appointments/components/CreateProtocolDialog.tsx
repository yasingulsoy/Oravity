import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { protocolsApi } from '@/api/visits';
import { appointmentsApi } from '@/api/appointments';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/store/authStore';

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
  const [doctorId, setDoctorId] = useState<number | ''>('');

  // Bugünkü doktorları çek (branch filtresi yok — tüm erişilebilir)
  const today = format(new Date(), 'yyyy-MM-dd');
  const { data: doctorsData } = useQuery({
    queryKey: ['appointments', 'calendar-doctors', today],
    queryFn: () => appointmentsApi.getCalendarDoctors({ date: today }),
    select: (res) => {
      const seen = new Set<number>();
      return (res.data ?? []).filter((d) => {
        if (seen.has(d.doctorId)) return false;
        seen.add(d.doctorId);
        return true;
      });
    },
    staleTime: 5 * 60 * 1000,
    enabled: open,
  });
  const doctors = doctorsData ?? [];

  // Giriş yapan kullanıcı doktorsa onu varsayılan seç
  const currentUserId = user?.id ? Number(user.id) : null;
  const effectiveDoctorId =
    doctorId !== '' ? doctorId : (doctors.find((d) => d.doctorId === currentUserId)?.doctorId ?? '');

  const mutation = useMutation({
    mutationFn: () =>
      protocolsApi.create(visitPublicId, Number(effectiveDoctorId), protocolType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['visits', 'waiting'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      onSuccess();
    },
  });

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!effectiveDoctorId) return;
    mutation.mutate();
  }

  function handleClose() {
    mutation.reset();
    setProtocolType(1);
    setDoctorId('');
    onClose();
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Protokol Aç</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Hasta bilgisi */}
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

          {/* Hekim seçimi */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium">Hekim</label>
            <select
              value={doctorId !== '' ? doctorId : (effectiveDoctorId !== '' ? effectiveDoctorId : '')}
              onChange={(e) => setDoctorId(e.target.value ? Number(e.target.value) : '')}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
            >
              <option value="">— Hekim seçin —</option>
              {doctors.map((d) => (
                <option key={d.doctorId} value={d.doctorId}>
                  {d.title ? `${d.title} ` : ''}{d.fullName}
                </option>
              ))}
            </select>
          </div>

          {mutation.isError && (
            <p className="text-xs text-destructive">Protokol açılamadı. Tekrar deneyin.</p>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              Vazgeç
            </Button>
            <Button
              type="submit"
              disabled={!effectiveDoctorId || mutation.isPending}
            >
              {mutation.isPending ? 'Açılıyor...' : 'Protokol Aç'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
