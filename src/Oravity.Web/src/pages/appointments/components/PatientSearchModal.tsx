import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Search, X, User, UserPlus, ChevronLeft, AlertTriangle, Clock } from 'lucide-react';
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
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
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

type ModalMode = 'search' | 'quick-register';

export function PatientSearchModal({ open, range, onClose, onSuccess }: PatientSearchModalProps) {
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<ModalMode>('search');

  // --- Search state ---
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);

  // --- Appointment details ---
  const [selectedTypeId, setSelectedTypeId] = useState<number | null>(null);
  const [notes, setNotes] = useState('');
  const [isUrgent, setIsUrgent] = useState(false);
  const [isEarlierRequest, setIsEarlierRequest] = useState(false);

  // --- Quick register state ---
  const [quickFirstName, setQuickFirstName] = useState('');
  const [quickLastName, setQuickLastName] = useState('');
  const [quickPhone, setQuickPhone] = useState('');

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
      setIsUrgent(false);
      setIsEarlierRequest(false);
      setMode('search');
      setQuickFirstName('');
      setQuickLastName('');
      setQuickPhone('');
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
        search: debouncedSearch || undefined,
      }),
    enabled: open && debouncedSearch.length >= 2,
    select: (res) => res.data?.items ?? [],
  });

  // Quick patient registration
  const registerMutation = useMutation({
    mutationFn: () =>
      patientsApi.create({
        firstName: quickFirstName.trim(),
        lastName: quickLastName.trim(),
        phone: quickPhone.trim() || undefined,
      }),
    onSuccess: (res) => {
      const newPatient = res.data;
      setSelectedPatient(newPatient);
      setMode('search');
      queryClient.invalidateQueries({ queryKey: ['patients'] });
    },
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
        isUrgent,
        isEarlierRequest,
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
  const showNoResults = debouncedSearch.length >= 2 && !isFetching && (!patients || patients.length === 0);

  // ─── Quick register form ───────────────────────────────────────────────────
  if (mode === 'quick-register') {
    return (
      <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="icon"
                className="size-7 -ml-1"
                onClick={() => setMode('search')}
              >
                <ChevronLeft className="size-4" />
              </Button>
              Hızlı Hasta Kaydı
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="qr-first">Ad <span className="text-destructive">*</span></Label>
              <Input
                id="qr-first"
                placeholder="Ad"
                value={quickFirstName}
                onChange={(e) => setQuickFirstName(e.target.value)}
                autoFocus
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="qr-last">Soyad <span className="text-destructive">*</span></Label>
              <Input
                id="qr-last"
                placeholder="Soyad"
                value={quickLastName}
                onChange={(e) => setQuickLastName(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="qr-phone">Telefon</Label>
              <Input
                id="qr-phone"
                placeholder="05XX XXX XX XX"
                value={quickPhone}
                onChange={(e) => setQuickPhone(e.target.value)}
                type="tel"
              />
            </div>
          </div>

          {registerMutation.isError && (
            <p className="text-sm text-destructive">Hasta kaydedilemedi. Lütfen tekrar deneyin.</p>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setMode('search')}>İptal</Button>
            <Button
              disabled={!quickFirstName.trim() || !quickLastName.trim() || registerMutation.isPending}
              onClick={() => registerMutation.mutate()}
            >
              {registerMutation.isPending ? 'Kaydediliyor...' : 'Kaydet ve Seç'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  // ─── Main search + create form ─────────────────────────────────────────────
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

        {/* Urgent / Earlier request flags */}
        <div className="flex gap-4">
          <label className="flex items-center gap-2 cursor-pointer select-none">
            <Checkbox
              checked={isUrgent}
              onCheckedChange={(v) => setIsUrgent(!!v)}
            />
            <span className="flex items-center gap-1 text-sm font-medium text-red-600">
              <AlertTriangle className="size-3.5" />
              Acil randevu
            </span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer select-none">
            <Checkbox
              checked={isEarlierRequest}
              onCheckedChange={(v) => setIsEarlierRequest(!!v)}
            />
            <span className="flex items-center gap-1 text-sm font-medium text-orange-600">
              <Clock className="size-3.5" />
              Erken saat talep
            </span>
          </label>
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
                  ) : null}

                  {showNoResults && (
                    <div className="px-3 py-2.5 flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Hasta bulunamadı</span>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 gap-1 text-xs"
                        onClick={() => {
                          // Pre-fill first name from search
                          setQuickFirstName(search);
                          setMode('quick-register');
                        }}
                      >
                        <UserPlus className="size-3.5" />
                        Yeni Hasta Kaydet
                      </Button>
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
            {(() => {
              const selectedType = appointmentTypes.find((t: AppointmentType) => t.id === selectedTypeId);
              return (
                <Select
                  value={selectedTypeId?.toString() ?? ''}
                  onValueChange={(v) => setSelectedTypeId(v ? Number(v) : null)}
                >
                  <SelectTrigger className="w-full">
                    {selectedType ? (
                      <span className="flex flex-1 items-center gap-2 text-sm">
                        {selectedType.color && (
                          <span
                            className="inline-block size-2.5 rounded-full shrink-0"
                            style={{ backgroundColor: selectedType.color }}
                          />
                        )}
                        {selectedType.name}
                      </span>
                    ) : (
                      <span className="flex-1 text-sm text-muted-foreground">Tip seçin (opsiyonel)</span>
                    )}
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
              );
            })()}
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
