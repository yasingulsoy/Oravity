import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Search, X, User } from 'lucide-react';
import { patientsApi } from '@/api/patients';
import { appointmentsApi } from '@/api/appointments';
import type { AppointmentType } from '@/types/appointment';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { Patient } from '@/types/patient';

interface SelectedRange {
  doctorId: number;
  doctorName: string;
  branchId: number;
  startTime: string; // "HH:mm"
  endTime: string;   // "HH:mm"
  date: Date;
}

interface PatientSearchModalProps {
  open: boolean;
  range: SelectedRange | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function PatientSearchModal({ open, range, onClose, onSuccess }: PatientSearchModalProps) {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [selectedTypeId, setSelectedTypeId] = useState<number | null>(null);
  const [notes, setNotes] = useState('');

  // Debounce search input 300ms
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset on open/close
  useEffect(() => {
    if (!open) {
      setSearch('');
      setDebouncedSearch('');
      setSelectedPatient(null);
      setSelectedTypeId(null);
      setNotes('');
    }
  }, [open]);

  const { data: appointmentTypes } = useQuery({
    queryKey: ['appointments', 'types'],
    queryFn: () => appointmentsApi.getTypes(),
    staleTime: Infinity,
    select: (res) => (res.data ?? []).filter((t: AppointmentType) => t.isPatientAppointment),
  });

  const { data: patients, isFetching } = useQuery({
    queryKey: ['patients', 'quick-search', debouncedSearch],
    queryFn: () =>
      patientsApi.list({
        page: 1,
        pageSize: 10,
        firstName: debouncedSearch || undefined,
      }),
    enabled: open && debouncedSearch.length >= 2,
    select: (res) => res.data?.items ?? [],
  });

  const createMutation = useMutation({
    mutationFn: () => {
      if (!range || !selectedPatient) throw new Error('Eksik bilgi');

      const startDate = new Date(range.date);
      const [sh, sm] = range.startTime.split(':').map(Number);
      startDate.setHours(sh, sm, 0, 0);

      const endDate = new Date(range.date);
      const [eh, em] = range.endTime.split(':').map(Number);
      endDate.setHours(eh, em, 0, 0);

      return appointmentsApi.create({
        patientId: selectedPatient.id,
        doctorId: range.doctorId,
        branchId: range.branchId,
        appointmentTypeId: selectedTypeId ?? undefined,
        startTime: startDate.toISOString(),
        endTime: endDate.toISOString(),
        notes: notes.trim() || undefined,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      onSuccess();
      onClose();
    },
  });

  const handlePatientSelect = useCallback((patient: Patient) => {
    setSelectedPatient(patient);
    setSearch('');
    setDebouncedSearch('');
  }, []);

  const handleClearPatient = useCallback(() => {
    setSelectedPatient(null);
  }, []);

  if (!range) return null;

  const dateLabel = format(range.date, 'dd.MM.yyyy');

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Yeni Randevu</DialogTitle>
        </DialogHeader>

        {/* Slot summary */}
        <div className="rounded-md bg-muted p-3 text-sm space-y-1">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Hekim</span>
            <span className="font-medium">{range.doctorName}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Tarih</span>
            <span className="font-medium">{dateLabel}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Saat</span>
            <span className="font-medium">{range.startTime} - {range.endTime}</span>
          </div>
        </div>

        {/* Patient search / selected */}
        <div className="space-y-2">
          <Label>Hasta</Label>

          {selectedPatient ? (
            <div className="flex items-center justify-between rounded-md border px-3 py-2 bg-blue-50">
              <div className="flex items-center gap-2">
                <User className="size-4 text-blue-600 shrink-0" />
                <div>
                  <p className="text-sm font-medium">{selectedPatient.firstName} {selectedPatient.lastName}</p>
                  {selectedPatient.phone && (
                    <p className="text-xs text-muted-foreground">{selectedPatient.phone}</p>
                  )}
                </div>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-7 shrink-0"
                onClick={handleClearPatient}
              >
                <X className="size-4" />
              </Button>
            </div>
          ) : (
            <div className="space-y-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <Input
                  placeholder="Ad ile hasta ara..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-9"
                  autoFocus
                />
              </div>

              {debouncedSearch.length >= 2 && (
                <div className="border rounded-md divide-y max-h-48 overflow-y-auto">
                  {isFetching ? (
                    <div className="px-3 py-2 text-sm text-muted-foreground">Aranıyor...</div>
                  ) : patients && patients.length > 0 ? (
                    patients.map((p) => (
                      <button
                        key={p.publicId}
                        type="button"
                        className="w-full text-left px-3 py-2 hover:bg-muted text-sm transition-colors"
                        onClick={() => handlePatientSelect(p)}
                      >
                        <span className="font-medium">{p.firstName} {p.lastName}</span>
                        {p.phone && (
                          <span className="ml-2 text-xs text-muted-foreground">{p.phone}</span>
                        )}
                      </button>
                    ))
                  ) : (
                    <div className="px-3 py-2 text-sm text-muted-foreground">
                      Hasta bulunamadı
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Appointment type */}
        {appointmentTypes && appointmentTypes.length > 0 && (
          <div className="space-y-2">
            <Label>Randevu Tipi</Label>
            <Select
              value={selectedTypeId?.toString() ?? ''}
              onValueChange={(v) => setSelectedTypeId(v ? Number(v) : null)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Tip seçin (opsiyonel)" />
              </SelectTrigger>
              <SelectContent>
                {appointmentTypes.map((t: AppointmentType) => (
                  <SelectItem key={t.id} value={t.id.toString()}>
                    <span className="flex items-center gap-2">
                      {t.color && (
                        <span
                          className="inline-block size-2.5 rounded-full shrink-0"
                          style={{ backgroundColor: t.color }}
                        />
                      )}
                      {t.name}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {/* Notes */}
        <div className="space-y-2">
          <Label htmlFor="apt-notes">Notlar</Label>
          <Input
            id="apt-notes"
            placeholder="Randevu notu (opsiyonel)"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </div>

        {createMutation.isError && (
          <p className="text-sm text-destructive">
            Randevu oluşturulamadı. Slot dolu olabilir veya bağlantı hatası oluştu.
          </p>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose}>
            İptal
          </Button>
          <Button
            type="button"
            disabled={!selectedPatient || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? 'Kaydediliyor...' : 'Randevu Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
