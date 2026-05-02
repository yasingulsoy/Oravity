import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  User, Search, Clock, CalendarDays, CalendarX2, ChevronRight, Plus,
  Trash2, Pencil, Loader2, Save, AlertCircle, Globe, Settings, BellRing, Ban,
} from 'lucide-react';
import { toast } from 'sonner';
import { settingsApi, type UserItem, type UserDetail, type SpecializationItem, type BranchItem } from '@/api/settings';
import {
  doctorSchedulesApi,
  type DoctorScheduleItem, type SpecialDayItem, type SpecialDayType,
  type OnlineScheduleItem, type OnlineBookingSettingsItem,
  type OnCallSettingsItem, type OnlineBlockItem,
} from '@/api/doctorSchedules';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { cn } from '@/lib/utils';

// ─── Constants ────────────────────────────────────────────────────────────────

const DAY_NAMES = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar'];
const TITLES = ['Dr.', 'Dt.', 'Prof. Dr.', 'Uzm. Dr.', 'Doç. Dr.'];
const SPECIAL_DAY_LABELS: Record<number, string> = {
  1: 'Ekstra Mesai',
  2: 'Saat Değişikliği',
  3: 'İzin',
};
const SPECIAL_DAY_COLORS: Record<number, string> = {
  1: 'bg-green-100 text-green-800',
  2: 'bg-blue-100 text-blue-800',
  3: 'bg-red-100 text-red-800',
};

// ─── Types ────────────────────────────────────────────────────────────────────

interface DayRow {
  id?: number;
  dayOfWeek: number;
  isWorking: boolean;
  startTime: string;
  endTime: string;
  hasBreak: boolean;
  breakStart: string;
  breakEnd: string;
  breakLabel: string;
}

function defaultDay(dayOfWeek: number): DayRow {
  const isWeekday = dayOfWeek <= 5;
  return {
    dayOfWeek,
    isWorking: isWeekday,
    startTime: '09:00',
    endTime: '18:00',
    hasBreak: false,
    breakStart: '12:00',
    breakEnd: '13:00',
    breakLabel: 'Mola',
  };
}

function scheduleToDayRow(s: DoctorScheduleItem): DayRow {
  return {
    id: s.id,
    dayOfWeek: s.dayOfWeek,
    isWorking: s.isWorking,
    startTime: s.startTime,
    endTime: s.endTime,
    hasBreak: !!s.breakStart,
    breakStart: s.breakStart ?? '12:00',
    breakEnd: s.breakEnd ?? '13:00',
    breakLabel: s.breakLabel ?? 'Mola',
  };
}

function buildDays(schedules: DoctorScheduleItem[], branchPublicId: string): DayRow[] {
  return Array.from({ length: 7 }, (_, i) => {
    const dow = i + 1;
    const existing = schedules.find(s => s.branchPublicId === branchPublicId && s.dayOfWeek === dow);
    return existing ? scheduleToDayRow(existing) : defaultDay(dow);
  });
}

// ─── Main Tab ─────────────────────────────────────────────────────────────────

export function DoctorSchedulesTab() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['settings', 'users'],
    queryFn: () => settingsApi.listUsers().then(r => r.data),
  });

  const doctors = users.filter(u => u.roles.some(r => r.roleCode === 'DOCTOR'));
  const filtered = doctors.filter(d =>
    d.fullName.toLowerCase().includes(search.toLowerCase()) ||
    (d.title ?? '').toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="flex gap-4" style={{ height: 'calc(100vh - 220px)' }}>
      {/* Doctor list */}
      <div className="w-64 border rounded-lg flex flex-col flex-shrink-0">
        <div className="p-3 border-b">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Hekim ara..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="pl-8 h-8 text-sm"
            />
          </div>
        </div>

        <div className="flex-1 overflow-auto">
          {isLoading ? (
            <div className="p-3 space-y-2">
              {[1, 2, 3].map(i => <Skeleton key={i} className="h-12" />)}
            </div>
          ) : filtered.length === 0 ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              <User className="h-8 w-8 mx-auto mb-2 opacity-40" />
              DOCTOR rolüne sahip kullanıcı bulunamadı
            </div>
          ) : (
            filtered.map(doc => (
              <button
                key={doc.publicId}
                onClick={() => setSelectedId(doc.publicId)}
                className={cn(
                  'w-full text-left px-3 py-2.5 border-b last:border-0 hover:bg-muted/60 transition-colors',
                  selectedId === doc.publicId && 'bg-muted'
                )}
              >
                <div className="flex items-center gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-sm truncate">
                      {doc.title ? `${doc.title} ` : ''}{doc.fullName}
                    </div>
                    <div className="text-xs text-muted-foreground truncate">
                      {doc.roles.find(r => r.roleCode === 'DOCTOR')?.branchName ?? 'Şube atanmamış'}
                    </div>
                  </div>
                  {selectedId === doc.publicId && (
                    <ChevronRight className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                  )}
                </div>
              </button>
            ))
          )}
        </div>
      </div>

      {/* Right panel */}
      {selectedId ? (
        <DoctorDetailPanel
          key={selectedId}
          doctorPublicId={selectedId}
          doctorName={(() => {
            const d = doctors.find(d => d.publicId === selectedId);
            return d ? `${d.title ? d.title + ' ' : ''}${d.fullName}` : '';
          })()}
        />
      ) : (
        <div className="flex-1 flex flex-col items-center justify-center text-muted-foreground border rounded-lg">
          <User className="h-12 w-12 mb-3 opacity-30" />
          <p className="text-sm">Sol taraftan bir hekim seçin</p>
        </div>
      )}
    </div>
  );
}

// ─── Doctor Detail Panel ───────────────────────────────────────────────────────

function DoctorDetailPanel({ doctorPublicId, doctorName }: { doctorPublicId: string; doctorName: string }) {
  const { data: doctor, isLoading } = useQuery({
    queryKey: ['settings', 'users', doctorPublicId],
    queryFn: () => settingsApi.getUser(doctorPublicId).then(r => r.data),
  });

  if (isLoading) {
    return (
      <div className="flex-1 border rounded-lg p-6 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  if (!doctor) return null;

  return (
    <div className="flex-1 border rounded-lg flex flex-col overflow-hidden">
      <div className="px-5 py-4 border-b">
        <h3 className="text-base font-semibold">{doctorName}</h3>
        <p className="text-sm text-muted-foreground">{doctor.specializationName ?? 'Uzmanlık belirtilmemiş'}</p>
      </div>

      <Tabs defaultValue="personal" className="flex-1 flex flex-col overflow-hidden">
        <div className="px-4 border-b">
          <TabsList className="h-9 mt-2 mb-0 rounded-none bg-transparent border-none p-0 gap-1 flex-wrap">
            <TabsTrigger value="personal" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <User className="h-3.5 w-3.5 mr-1.5" /> Kişisel
            </TabsTrigger>
            <TabsTrigger value="schedule" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <Clock className="h-3.5 w-3.5 mr-1.5" /> Program
            </TabsTrigger>
            <TabsTrigger value="special" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <CalendarDays className="h-3.5 w-3.5 mr-1.5" /> Özel Günler
            </TabsTrigger>
            <TabsTrigger value="online-schedule" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <Globe className="h-3.5 w-3.5 mr-1.5" /> Online Program
            </TabsTrigger>
            <TabsTrigger value="online-settings" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <Settings className="h-3.5 w-3.5 mr-1.5" /> Online Ayarları
            </TabsTrigger>
            <TabsTrigger value="on-call" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <BellRing className="h-3.5 w-3.5 mr-1.5" /> Nöbet
            </TabsTrigger>
            <TabsTrigger value="online-blocks" className="data-[state=active]:border-b-2 data-[state=active]:border-primary rounded-none h-9 px-3 text-sm">
              <Ban className="h-3.5 w-3.5 mr-1.5" /> Online Bloklar
            </TabsTrigger>
          </TabsList>
        </div>

        <div className="flex-1 overflow-auto">
          <TabsContent value="personal" className="m-0 p-5 h-full">
            <PersonalInfoPanel doctor={doctor} />
          </TabsContent>
          <TabsContent value="schedule" className="m-0 p-5 h-full">
            <WeeklySchedulePanel doctorPublicId={doctorPublicId} doctor={doctor} />
          </TabsContent>
          <TabsContent value="special" className="m-0 p-5 h-full">
            <SpecialDaysPanel doctorPublicId={doctorPublicId} doctor={doctor} />
          </TabsContent>
          <TabsContent value="online-schedule" className="m-0 p-5 h-full">
            <OnlineSchedulePanel doctorPublicId={doctorPublicId} />
          </TabsContent>
          <TabsContent value="online-settings" className="m-0 p-5 h-full">
            <OnlineBookingSettingsPanel doctorPublicId={doctorPublicId} />
          </TabsContent>
          <TabsContent value="on-call" className="m-0 p-5 h-full">
            <OnCallSettingsPanel doctorPublicId={doctorPublicId} />
          </TabsContent>
          <TabsContent value="online-blocks" className="m-0 p-5 h-full">
            <OnlineBlocksPanel doctorPublicId={doctorPublicId} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}

// ─── Personal Info Panel ──────────────────────────────────────────────────────

function PersonalInfoPanel({ doctor }: { doctor: UserDetail }) {
  const qc = useQueryClient();
  const [title, setTitle] = useState(doctor.title ?? '');
  const [specializationId, setSpecializationId] = useState<string>(
    doctor.specializationId ? String(doctor.specializationId) : '0'
  );
  const [calendarColor, setCalendarColor] = useState(doctor.calendarColor ?? '#3b82f6');
  const [duration, setDuration] = useState<string>(
    String(doctor.defaultAppointmentDuration ?? 30)
  );

  useEffect(() => {
    setTitle(doctor.title ?? '');
    setSpecializationId(doctor.specializationId ? String(doctor.specializationId) : '0');
    setCalendarColor(doctor.calendarColor ?? '#3b82f6');
    setDuration(String(doctor.defaultAppointmentDuration ?? 30));
  }, [doctor.publicId]);

  const { data: specializations = [] } = useQuery({
    queryKey: ['settings', 'specializations'],
    queryFn: () => settingsApi.listSpecializations().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const mutation = useMutation({
    mutationFn: () => settingsApi.updateUser(doctor.publicId, {
      title: title || undefined,
      specializationId: parseInt(specializationId),
      calendarColor,
      defaultAppointmentDuration: parseInt(duration) || 30,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['settings', 'users', doctor.publicId] });
      qc.invalidateQueries({ queryKey: ['settings', 'users'] });
      toast.success('Kişisel bilgiler güncellendi.');
    },
    onError: () => toast.error('Güncelleme başarısız.'),
  });

  return (
    <div className="max-w-md space-y-5">
      <div className="space-y-1.5">
        <Label htmlFor="title">Unvan</Label>
        <Select value={title} onValueChange={setTitle}>
          <SelectTrigger id="title">
            <SelectValue placeholder="Unvan seçin" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Unvan yok</SelectItem>
            {TITLES.map(t => <SelectItem key={t} value={t}>{t}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="spec">Uzmanlık Alanı</Label>
        <Select value={specializationId} onValueChange={setSpecializationId}>
          <SelectTrigger id="spec">
            <SelectValue placeholder="Uzmanlık seçin" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="0">Belirtilmemiş</SelectItem>
            {specializations.map(s => (
              <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="color">Takvim Rengi</Label>
        <div className="flex items-center gap-3">
          <input
            id="color"
            type="color"
            value={calendarColor}
            onChange={e => setCalendarColor(e.target.value)}
            className="h-9 w-16 rounded border cursor-pointer p-0.5"
          />
          <span className="text-sm text-muted-foreground font-mono">{calendarColor}</span>
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="duration">Varsayılan Randevu Süresi (dk)</Label>
        <div className="flex items-center gap-2">
          <Input
            id="duration"
            type="number"
            min={5}
            max={240}
            step={5}
            value={duration}
            onChange={e => setDuration(e.target.value)}
            className="w-24"
          />
          <span className="text-sm text-muted-foreground">dakika</span>
        </div>
      </div>

      <Button onClick={() => mutation.mutate()} disabled={mutation.isPending} className="mt-2">
        {mutation.isPending ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : <Save className="h-4 w-4 mr-1.5" />}
        Kaydet
      </Button>
    </div>
  );
}

// ─── Weekly Schedule Panel ────────────────────────────────────────────────────

function WeeklySchedulePanel({ doctorPublicId, doctor: _doctor }: { doctorPublicId: string; doctor: UserDetail }) {
  const qc = useQueryClient();

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const { data: schedules = [], isLoading } = useQuery({
    queryKey: ['doctor-schedules', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getSchedules(doctorPublicId).then(r => r.data),
  });

  // Branches that have existing schedules
  const scheduledBranches = [...new Map(
    schedules.map(s => [s.branchPublicId, { publicId: s.branchPublicId, name: s.branchName }])
  ).values()];

  // All branches for doctor (from role assignments resolved via schedules or allBranches)
  const branchOptions: Array<{ publicId: string; name: string }> = scheduledBranches.length > 0
    ? scheduledBranches
    : allBranches.filter(b => b.isActive).map(b => ({ publicId: b.publicId, name: b.name }));

  const [selectedBranch, setSelectedBranch] = useState<string>('');

  useEffect(() => {
    if (branchOptions.length > 0 && !selectedBranch) {
      setSelectedBranch(branchOptions[0].publicId);
    }
  }, [branchOptions.length]);

  const [days, setDays] = useState<DayRow[]>(() => Array.from({ length: 7 }, (_, i) => defaultDay(i + 1)));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (selectedBranch) {
      setDays(buildDays(schedules, selectedBranch));
    }
  }, [schedules, selectedBranch]);

  const updateDay = useCallback((index: number, patch: Partial<DayRow>) => {
    setDays(prev => prev.map((d, i) => i === index ? { ...d, ...patch } : d));
  }, []);

  const handleSave = async () => {
    if (!selectedBranch) return;
    setSaving(true);
    try {
      for (const day of days) {
        await doctorSchedulesApi.upsertSchedule({
          doctorPublicId,
          branchPublicId: selectedBranch,
          dayOfWeek: day.dayOfWeek,
          isWorking: day.isWorking,
          startTime: day.startTime,
          endTime: day.endTime,
          breakStart: day.hasBreak ? day.breakStart : null,
          breakEnd:   day.hasBreak ? day.breakEnd   : null,
          breakLabel: day.hasBreak ? day.breakLabel  : null,
        });
      }
      qc.invalidateQueries({ queryKey: ['doctor-schedules', doctorPublicId] });
      toast.success('Çalışma saatleri kaydedildi.');
    } catch {
      toast.error('Kaydetme başarısız.');
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  if (branchOptions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-2 py-12 text-muted-foreground">
        <AlertCircle className="h-8 w-8 opacity-40" />
        <p className="text-sm">Bu hekim henüz bir şubeye DOCTOR rolüyle atanmamış.</p>
        <p className="text-xs">Kullanıcılar sekmesinden rol atayın.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {branchOptions.length > 1 && (
        <div className="flex items-center gap-3">
          <Label className="whitespace-nowrap">Şube</Label>
          <Select value={selectedBranch} onValueChange={v => setSelectedBranch(v)}>
            <SelectTrigger className="w-56">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {branchOptions.map(b => (
                <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-3 py-2 font-medium w-28">Gün</th>
              <th className="text-center px-2 py-2 font-medium w-20">Çalışıyor</th>
              <th className="text-left px-2 py-2 font-medium">Başlangıç</th>
              <th className="text-left px-2 py-2 font-medium">Bitiş</th>
              <th className="text-center px-2 py-2 font-medium w-16">Mola</th>
              <th className="text-left px-2 py-2 font-medium">Mola Baş.</th>
              <th className="text-left px-2 py-2 font-medium">Mola Bitiş</th>
              <th className="text-left px-2 py-2 font-medium">Mola Adı</th>
            </tr>
          </thead>
          <tbody>
            {days.map((day, i) => (
              <tr key={day.dayOfWeek} className={cn('border-t', !day.isWorking && 'opacity-50')}>
                <td className="px-3 py-2 font-medium">{DAY_NAMES[i]}</td>
                <td className="px-2 py-2 text-center">
                  <Checkbox
                    checked={day.isWorking}
                    onCheckedChange={v => updateDay(i, { isWorking: !!v })}
                  />
                </td>
                <td className="px-2 py-2">
                  <Input
                    type="time"
                    value={day.startTime}
                    disabled={!day.isWorking}
                    onChange={e => updateDay(i, { startTime: e.target.value })}
                    className="h-7 w-24 text-xs"
                  />
                </td>
                <td className="px-2 py-2">
                  <Input
                    type="time"
                    value={day.endTime}
                    disabled={!day.isWorking}
                    onChange={e => updateDay(i, { endTime: e.target.value })}
                    className="h-7 w-24 text-xs"
                  />
                </td>
                <td className="px-2 py-2 text-center">
                  <Checkbox
                    checked={day.hasBreak}
                    disabled={!day.isWorking}
                    onCheckedChange={v => updateDay(i, { hasBreak: !!v })}
                  />
                </td>
                <td className="px-2 py-2">
                  <Input
                    type="time"
                    value={day.breakStart}
                    disabled={!day.isWorking || !day.hasBreak}
                    onChange={e => updateDay(i, { breakStart: e.target.value })}
                    className="h-7 w-24 text-xs"
                  />
                </td>
                <td className="px-2 py-2">
                  <Input
                    type="time"
                    value={day.breakEnd}
                    disabled={!day.isWorking || !day.hasBreak}
                    onChange={e => updateDay(i, { breakEnd: e.target.value })}
                    className="h-7 w-24 text-xs"
                  />
                </td>
                <td className="px-2 py-2">
                  <Input
                    value={day.breakLabel}
                    disabled={!day.isWorking || !day.hasBreak}
                    onChange={e => updateDay(i, { breakLabel: e.target.value })}
                    placeholder="Mola"
                    className="h-7 w-24 text-xs"
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Button onClick={handleSave} disabled={saving || !selectedBranch}>
        {saving ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : <Save className="h-4 w-4 mr-1.5" />}
        Değişiklikleri Kaydet
      </Button>
    </div>
  );
}

// ─── Special Days Panel ────────────────────────────────────────────────────────

interface SpecialDayForm {
  branchPublicId: string;
  specificDate: string;
  type: number;
  startTime: string;
  endTime: string;
  reason: string;
}

function SpecialDaysPanel({ doctorPublicId, doctor: _doctor }: { doctorPublicId: string; doctor: UserDetail }) {
  const qc = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const branchOptions = allBranches.filter(b => b.isActive);

  const defaultForm = (): SpecialDayForm => ({
    branchPublicId: branchOptions[0]?.publicId ?? '',
    specificDate: new Date().toISOString().slice(0, 10),
    type: 3,
    startTime: '',
    endTime: '',
    reason: '',
  });

  const [form, setForm] = useState<SpecialDayForm>(defaultForm);

  const { data: specialDays = [], isLoading } = useQuery({
    queryKey: ['doctor-special-days', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getSpecialDays(doctorPublicId).then(r => r.data),
  });

  const createMutation = useMutation({
    mutationFn: () => doctorSchedulesApi.createSpecialDay({
      doctorPublicId,
      branchPublicId: form.branchPublicId,
      specificDate: form.specificDate,
      type: form.type,
      startTime: form.startTime || null,
      endTime: form.endTime || null,
      reason: form.reason || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-special-days', doctorPublicId] });
      toast.success('Özel gün eklendi.');
      setDialogOpen(false);
    },
    onError: () => toast.error('Ekleme başarısız.'),
  });

  const updateMutation = useMutation({
    mutationFn: (id: number) => doctorSchedulesApi.updateSpecialDay(id, {
      type: form.type,
      startTime: form.startTime || null,
      endTime: form.endTime || null,
      reason: form.reason || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-special-days', doctorPublicId] });
      toast.success('Özel gün güncellendi.');
      setDialogOpen(false);
      setEditingId(null);
    },
    onError: () => toast.error('Güncelleme başarısız.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => doctorSchedulesApi.deleteSpecialDay(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-special-days', doctorPublicId] });
      toast.success('Özel gün silindi.');
      setDeleteId(null);
    },
    onError: () => toast.error('Silme başarısız.'),
  });

  const openAdd = () => {
    setEditingId(null);
    setForm({ ...defaultForm(), branchPublicId: branchOptions[0]?.publicId ?? '' });
    setDialogOpen(true);
  };

  const openEdit = (day: SpecialDayItem) => {
    setEditingId(day.id);
    setForm({
      branchPublicId: day.branchPublicId,
      specificDate: day.specificDate,
      type: day.type,
      startTime: day.startTime ?? '',
      endTime: day.endTime ?? '',
      reason: day.reason ?? '',
    });
    setDialogOpen(true);
  };

  const handleSubmit = () => {
    if (editingId !== null) {
      updateMutation.mutate(editingId);
    } else {
      createMutation.mutate();
    }
  };

  const isBusy = createMutation.isPending || updateMutation.isPending;
  const isDayOff = form.type === 3;

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button size="sm" onClick={openAdd}>
          <Plus className="h-4 w-4 mr-1.5" /> Özel Gün Ekle
        </Button>
      </div>

      {specialDays.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
          <CalendarX2 className="h-10 w-10 opacity-30" />
          <p className="text-sm">Henüz özel gün tanımlanmamış</p>
        </div>
      ) : (
        <div className="border rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left px-4 py-2 font-medium">Tarih</th>
                <th className="text-left px-4 py-2 font-medium">Tür</th>
                <th className="text-left px-4 py-2 font-medium">Saatler</th>
                <th className="text-left px-4 py-2 font-medium">Şube</th>
                <th className="text-left px-4 py-2 font-medium">Açıklama</th>
                <th className="px-4 py-2 w-20" />
              </tr>
            </thead>
            <tbody>
              {specialDays.map(day => (
                <tr key={day.id} className="border-t">
                  <td className="px-4 py-2 font-medium">
                    {new Date(day.specificDate).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })}
                  </td>
                  <td className="px-4 py-2">
                    <Badge className={cn('text-xs', SPECIAL_DAY_COLORS[day.type])}>
                      {SPECIAL_DAY_LABELS[day.type]}
                    </Badge>
                  </td>
                  <td className="px-4 py-2 text-muted-foreground">
                    {day.type === 3 ? 'Tüm gün' : `${day.startTime ?? '─'} – ${day.endTime ?? '─'}`}
                  </td>
                  <td className="px-4 py-2 text-muted-foreground text-xs">{day.branchName}</td>
                  <td className="px-4 py-2 text-muted-foreground">{day.reason ?? '─'}</td>
                  <td className="px-4 py-2">
                    <div className="flex gap-1">
                      <Button variant="ghost" size="sm" className="h-7 w-7 p-0" onClick={() => openEdit(day)}>
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-7 w-7 p-0 text-destructive hover:text-destructive" onClick={() => setDeleteId(day.id)}>
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Add/Edit dialog */}
      <Dialog open={dialogOpen} onOpenChange={open => { setDialogOpen(open); if (!open) setEditingId(null); }}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{editingId ? 'Özel Günü Düzenle' : 'Özel Gün Ekle'}</DialogTitle>
          </DialogHeader>

          <div className="space-y-4 py-2">
            {!editingId && (
              <div className="space-y-1.5">
                <Label>Şube</Label>
                <Select value={form.branchPublicId} onValueChange={v => setForm(f => ({ ...f, branchPublicId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Şube seçin" /></SelectTrigger>
                  <SelectContent>
                    {branchOptions.map(b => <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="space-y-1.5">
              <Label>Tarih</Label>
              <Input
                type="date"
                value={form.specificDate}
                onChange={e => setForm(f => ({ ...f, specificDate: e.target.value }))}
              />
            </div>

            <div className="space-y-1.5">
              <Label>Tür</Label>
              <Select value={String(form.type)} onValueChange={v => setForm(f => ({ ...f, type: Number(v) }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Ekstra Mesai</SelectItem>
                  <SelectItem value="2">Saat Değişikliği</SelectItem>
                  <SelectItem value="3">İzin (Tüm Gün)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {!isDayOff && (
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label>Başlangıç Saati</Label>
                  <Input type="time" value={form.startTime} onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))} />
                </div>
                <div className="space-y-1.5">
                  <Label>Bitiş Saati</Label>
                  <Input type="time" value={form.endTime} onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))} />
                </div>
              </div>
            )}

            <div className="space-y-1.5">
              <Label>Açıklama</Label>
              <Input
                value={form.reason}
                onChange={e => setForm(f => ({ ...f, reason: e.target.value }))}
                placeholder="Yıllık izin, Kongre, Ekstra vardiya..."
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>İptal</Button>
            <Button onClick={handleSubmit} disabled={isBusy || !form.branchPublicId || !form.specificDate}>
              {isBusy ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : null}
              {editingId ? 'Güncelle' : 'Ekle'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <AlertDialog open={deleteId !== null} onOpenChange={open => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Özel günü sil</AlertDialogTitle>
            <AlertDialogDescription>Bu özel gün kaydı kalıcı olarak kaldırılacak. Emin misiniz?</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => deleteId !== null && deleteMutation.mutate(deleteId)}
            >
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Online Schedule Panel ────────────────────────────────────────────────────

function onlineScheduleToDayRow(s: OnlineScheduleItem): DayRow {
  return {
    id: s.id,
    dayOfWeek: s.dayOfWeek,
    isWorking: s.isWorking,
    startTime: s.startTime,
    endTime: s.endTime,
    hasBreak: !!s.breakStart,
    breakStart: s.breakStart ?? '12:00',
    breakEnd: s.breakEnd ?? '13:00',
    breakLabel: 'Mola',
  };
}

function buildOnlineDays(schedules: OnlineScheduleItem[], branchPublicId: string): DayRow[] {
  return Array.from({ length: 7 }, (_, i) => {
    const dow = i + 1;
    const existing = schedules.find(s => s.branchPublicId === branchPublicId && s.dayOfWeek === dow);
    return existing ? onlineScheduleToDayRow(existing) : defaultDay(dow);
  });
}

function OnlineSchedulePanel({ doctorPublicId }: { doctorPublicId: string }) {
  const qc = useQueryClient();

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const { data: schedules = [], isLoading } = useQuery({
    queryKey: ['doctor-online-schedule', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getOnlineSchedule(doctorPublicId).then(r => r.data),
  });

  const scheduledBranches = [...new Map(
    schedules.map(s => [s.branchPublicId, { publicId: s.branchPublicId, name: s.branchName }])
  ).values()];

  const branchOptions: Array<{ publicId: string; name: string }> = scheduledBranches.length > 0
    ? scheduledBranches
    : allBranches.filter(b => b.isActive).map(b => ({ publicId: b.publicId, name: b.name }));

  const [selectedBranch, setSelectedBranch] = useState<string>('');
  const [days, setDays] = useState<DayRow[]>(() => Array.from({ length: 7 }, (_, i) => defaultDay(i + 1)));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (branchOptions.length > 0 && !selectedBranch) {
      setSelectedBranch(branchOptions[0].publicId);
    }
  }, [branchOptions.length]);

  useEffect(() => {
    if (selectedBranch) {
      setDays(buildOnlineDays(schedules, selectedBranch));
    }
  }, [schedules, selectedBranch]);

  const updateDay = useCallback((index: number, patch: Partial<DayRow>) => {
    setDays(prev => prev.map((d, i) => i === index ? { ...d, ...patch } : d));
  }, []);

  const handleSave = async () => {
    if (!selectedBranch) return;
    setSaving(true);
    try {
      for (const day of days) {
        await doctorSchedulesApi.upsertOnlineSchedule({
          doctorPublicId,
          branchPublicId: selectedBranch,
          dayOfWeek: day.dayOfWeek,
          isWorking: day.isWorking,
          startTime: day.startTime,
          endTime: day.endTime,
          breakStart: day.hasBreak ? day.breakStart : null,
          breakEnd:   day.hasBreak ? day.breakEnd   : null,
        });
      }
      qc.invalidateQueries({ queryKey: ['doctor-online-schedule', doctorPublicId] });
      toast.success('Online program kaydedildi.');
    } catch {
      toast.error('Kaydetme başarısız.');
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  if (branchOptions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-2 py-12 text-muted-foreground">
        <AlertCircle className="h-8 w-8 opacity-40" />
        <p className="text-sm">Bu hekim henüz bir şubeye DOCTOR rolüyle atanmamış.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {branchOptions.length > 1 && (
        <div className="flex items-center gap-3">
          <Label className="whitespace-nowrap">Şube</Label>
          <Select value={selectedBranch} onValueChange={v => setSelectedBranch(v)}>
            <SelectTrigger className="w-56"><SelectValue /></SelectTrigger>
            <SelectContent>
              {branchOptions.map(b => (
                <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-3 py-2 font-medium w-28">Gün</th>
              <th className="text-center px-2 py-2 font-medium w-20">Aktif</th>
              <th className="text-left px-2 py-2 font-medium">Başlangıç</th>
              <th className="text-left px-2 py-2 font-medium">Bitiş</th>
              <th className="text-center px-2 py-2 font-medium w-16">Mola</th>
              <th className="text-left px-2 py-2 font-medium">Mola Baş.</th>
              <th className="text-left px-2 py-2 font-medium">Mola Bitiş</th>
            </tr>
          </thead>
          <tbody>
            {days.map((day, i) => (
              <tr key={day.dayOfWeek} className={cn('border-t', !day.isWorking && 'opacity-50')}>
                <td className="px-3 py-2 font-medium">{DAY_NAMES[i]}</td>
                <td className="px-2 py-2 text-center">
                  <Checkbox checked={day.isWorking} onCheckedChange={v => updateDay(i, { isWorking: !!v })} />
                </td>
                <td className="px-2 py-2">
                  <Input type="time" value={day.startTime} disabled={!day.isWorking}
                    onChange={e => updateDay(i, { startTime: e.target.value })} className="h-7 w-24 text-xs" />
                </td>
                <td className="px-2 py-2">
                  <Input type="time" value={day.endTime} disabled={!day.isWorking}
                    onChange={e => updateDay(i, { endTime: e.target.value })} className="h-7 w-24 text-xs" />
                </td>
                <td className="px-2 py-2 text-center">
                  <Checkbox checked={day.hasBreak} disabled={!day.isWorking}
                    onCheckedChange={v => updateDay(i, { hasBreak: !!v })} />
                </td>
                <td className="px-2 py-2">
                  <Input type="time" value={day.breakStart} disabled={!day.isWorking || !day.hasBreak}
                    onChange={e => updateDay(i, { breakStart: e.target.value })} className="h-7 w-24 text-xs" />
                </td>
                <td className="px-2 py-2">
                  <Input type="time" value={day.breakEnd} disabled={!day.isWorking || !day.hasBreak}
                    onChange={e => updateDay(i, { breakEnd: e.target.value })} className="h-7 w-24 text-xs" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Button onClick={handleSave} disabled={saving || !selectedBranch}>
        {saving ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : <Save className="h-4 w-4 mr-1.5" />}
        Değişiklikleri Kaydet
      </Button>
    </div>
  );
}

// ─── Online Booking Settings Panel ────────────────────────────────────────────

const SLOT_DURATIONS = [10, 15, 20, 30, 45, 60, 90, 120];
const PATIENT_TYPE_FILTER_LABELS = ['Herkese Açık', 'Sadece Yeni Hastalar', 'Sadece Mevcut Hastalar'];

interface BookingSettingsForm {
  isOnlineVisible: boolean;
  slotDurationMinutes: number;
  autoApprove: boolean;
  maxAdvanceDays: number;
  bookingNote: string;
  patientTypeFilter: number;
}

function OnlineBookingSettingsPanel({ doctorPublicId }: { doctorPublicId: string }) {
  const qc = useQueryClient();

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const branchOptions = allBranches.filter((b: BranchItem) => b.isActive);
  const [selectedBranch, setSelectedBranch] = useState<string>('');

  useEffect(() => {
    if (branchOptions.length > 0 && !selectedBranch) {
      setSelectedBranch(branchOptions[0].publicId);
    }
  }, [branchOptions.length]);

  const { data: settingsList = [], isLoading } = useQuery({
    queryKey: ['doctor-online-booking-settings', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getBookingSettings(doctorPublicId).then(r => r.data),
  });

  const defaultBookingForm = (): BookingSettingsForm => ({
    isOnlineVisible: false,
    slotDurationMinutes: 30,
    autoApprove: false,
    maxAdvanceDays: 30,
    bookingNote: '',
    patientTypeFilter: 0,
  });

  const [form, setForm] = useState<BookingSettingsForm>(defaultBookingForm);

  useEffect(() => {
    if (!selectedBranch) return;
    const existing = settingsList.find((s: OnlineBookingSettingsItem) => s.branchPublicId === selectedBranch);
    if (existing) {
      setForm({
        isOnlineVisible: existing.isOnlineVisible,
        slotDurationMinutes: existing.slotDurationMinutes,
        autoApprove: existing.autoApprove,
        maxAdvanceDays: existing.maxAdvanceDays,
        bookingNote: existing.bookingNote ?? '',
        patientTypeFilter: existing.patientTypeFilter,
      });
    } else {
      setForm(defaultBookingForm());
    }
  }, [settingsList, selectedBranch]);

  const mutation = useMutation({
    mutationFn: () => doctorSchedulesApi.upsertBookingSettings({
      doctorPublicId,
      branchPublicId: selectedBranch,
      isOnlineVisible: form.isOnlineVisible,
      slotDurationMinutes: form.slotDurationMinutes,
      autoApprove: form.autoApprove,
      maxAdvanceDays: form.maxAdvanceDays,
      bookingNote: form.bookingNote || null,
      patientTypeFilter: form.patientTypeFilter,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-online-booking-settings', doctorPublicId] });
      toast.success('Online randevu ayarları kaydedildi.');
    },
    onError: () => toast.error('Kaydetme başarısız.'),
  });

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  if (branchOptions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-2 py-12 text-muted-foreground">
        <AlertCircle className="h-8 w-8 opacity-40" />
        <p className="text-sm">Aktif şube bulunamadı.</p>
      </div>
    );
  }

  return (
    <div className="space-y-5 max-w-md">
      {branchOptions.length > 1 && (
        <div className="space-y-1.5">
          <Label>Şube</Label>
          <Select value={selectedBranch} onValueChange={setSelectedBranch}>
            <SelectTrigger className="w-56"><SelectValue /></SelectTrigger>
            <SelectContent>
              {branchOptions.map((b: BranchItem) => (
                <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="flex items-center justify-between rounded-lg border p-3">
        <div>
          <p className="font-medium text-sm">Online Randevuya Açık</p>
          <p className="text-xs text-muted-foreground">Bu hekim online randevu alabilsin mi?</p>
        </div>
        <Checkbox
          checked={form.isOnlineVisible}
          onCheckedChange={v => setForm(f => ({ ...f, isOnlineVisible: !!v }))}
        />
      </div>

      <div className="space-y-1.5">
        <Label>Slot Süresi</Label>
        <Select
          value={String(form.slotDurationMinutes)}
          onValueChange={v => setForm(f => ({ ...f, slotDurationMinutes: Number(v) }))}
        >
          <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
          <SelectContent>
            {SLOT_DURATIONS.map(d => (
              <SelectItem key={d} value={String(d)}>{d} dakika</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center justify-between rounded-lg border p-3">
        <div>
          <p className="font-medium text-sm">Otomatik Onay</p>
          <p className="text-xs text-muted-foreground">Randevular otomatik onaylansın mı?</p>
        </div>
        <Checkbox
          checked={form.autoApprove}
          onCheckedChange={v => setForm(f => ({ ...f, autoApprove: !!v }))}
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="maxAdvanceDays">Maksimum İleri Gün</Label>
        <div className="flex items-center gap-2">
          <Input
            id="maxAdvanceDays"
            type="number"
            min={1}
            max={365}
            value={form.maxAdvanceDays}
            onChange={e => setForm(f => ({ ...f, maxAdvanceDays: Number(e.target.value) }))}
            className="w-24"
          />
          <span className="text-sm text-muted-foreground">gün öncesine kadar</span>
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Hasta Tipi Filtresi</Label>
        <Select
          value={String(form.patientTypeFilter)}
          onValueChange={v => setForm(f => ({ ...f, patientTypeFilter: Number(v) }))}
        >
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent>
            {PATIENT_TYPE_FILTER_LABELS.map((label, i) => (
              <SelectItem key={i} value={String(i)}>{label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="bookingNote">Randevu Notu</Label>
        <textarea
          id="bookingNote"
          value={form.bookingNote}
          onChange={e => setForm(f => ({ ...f, bookingNote: e.target.value }))}
          placeholder="Hastalara gösterilecek ek not (isteğe bağlı)..."
          className="w-full rounded-md border bg-background px-3 py-2 text-sm min-h-[80px] resize-none focus:outline-none focus:ring-1 focus:ring-ring"
        />
      </div>

      <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !selectedBranch}>
        {mutation.isPending ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : <Save className="h-4 w-4 mr-1.5" />}
        Kaydet
      </Button>
    </div>
  );
}

// ─── On-Call Settings Panel ────────────────────────────────────────────────────

const ON_CALL_DAYS = [
  { key: 'monday' as const,    label: 'Pazartesi' },
  { key: 'tuesday' as const,   label: 'Salı' },
  { key: 'wednesday' as const, label: 'Çarşamba' },
  { key: 'thursday' as const,  label: 'Perşembe' },
  { key: 'friday' as const,    label: 'Cuma' },
  { key: 'saturday' as const,  label: 'Cumartesi' },
  { key: 'sunday' as const,    label: 'Pazar' },
];

const PERIOD_TYPE_LABELS: Record<number, string> = {
  1: 'Haftalık',
  2: 'Aylık',
  3: 'Üç Aylık',
  4: 'Altı Aylık',
};

interface OnCallForm {
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
  periodType: number;
  periodStart: string;
  periodEnd: string;
}

function OnCallSettingsPanel({ doctorPublicId }: { doctorPublicId: string }) {
  const qc = useQueryClient();

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const branchOptions = allBranches.filter((b: BranchItem) => b.isActive);
  const [selectedBranch, setSelectedBranch] = useState<string>('');

  useEffect(() => {
    if (branchOptions.length > 0 && !selectedBranch) {
      setSelectedBranch(branchOptions[0].publicId);
    }
  }, [branchOptions.length]);

  const { data: settingsList = [], isLoading } = useQuery({
    queryKey: ['doctor-on-call-settings', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getOnCallSettings(doctorPublicId).then(r => r.data),
  });

  const defaultOnCallForm = (): OnCallForm => ({
    monday: false, tuesday: false, wednesday: false,
    thursday: false, friday: false, saturday: false, sunday: false,
    periodType: 1,
    periodStart: '',
    periodEnd: '',
  });

  const [form, setForm] = useState<OnCallForm>(defaultOnCallForm);

  useEffect(() => {
    if (!selectedBranch) return;
    const existing = settingsList.find((s: OnCallSettingsItem) => s.branchPublicId === selectedBranch);
    if (existing) {
      setForm({
        monday: existing.monday, tuesday: existing.tuesday,
        wednesday: existing.wednesday, thursday: existing.thursday,
        friday: existing.friday, saturday: existing.saturday, sunday: existing.sunday,
        periodType: existing.periodType,
        periodStart: existing.periodStart ?? '',
        periodEnd: existing.periodEnd ?? '',
      });
    } else {
      setForm(defaultOnCallForm());
    }
  }, [settingsList, selectedBranch]);

  const mutation = useMutation({
    mutationFn: () => doctorSchedulesApi.upsertOnCallSettings({
      doctorPublicId,
      branchPublicId: selectedBranch,
      monday: form.monday, tuesday: form.tuesday, wednesday: form.wednesday,
      thursday: form.thursday, friday: form.friday, saturday: form.saturday, sunday: form.sunday,
      periodType: form.periodType,
      periodStart: form.periodStart || null,
      periodEnd: form.periodEnd || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-on-call-settings', doctorPublicId] });
      toast.success('Nöbet ayarları kaydedildi.');
    },
    onError: () => toast.error('Kaydetme başarısız.'),
  });

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  return (
    <div className="space-y-5 max-w-md">
      {branchOptions.length > 1 && (
        <div className="space-y-1.5">
          <Label>Şube</Label>
          <Select value={selectedBranch} onValueChange={setSelectedBranch}>
            <SelectTrigger className="w-56"><SelectValue /></SelectTrigger>
            <SelectContent>
              {branchOptions.map((b: BranchItem) => (
                <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="space-y-2">
        <Label>Nöbet Günleri</Label>
        <div className="grid grid-cols-2 gap-2">
          {ON_CALL_DAYS.map(({ key, label }) => (
            <label key={key} className="flex items-center gap-2 cursor-pointer select-none">
              <Checkbox
                checked={form[key]}
                onCheckedChange={v => setForm(f => ({ ...f, [key]: !!v }))}
              />
              <span className="text-sm">{label}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Periyot Türü</Label>
        <Select value={String(form.periodType)} onValueChange={v => setForm(f => ({ ...f, periodType: Number(v) }))}>
          <SelectTrigger className="w-44"><SelectValue /></SelectTrigger>
          <SelectContent>
            {Object.entries(PERIOD_TYPE_LABELS).map(([k, v]) => (
              <SelectItem key={k} value={k}>{v}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label>Periyot Başlangıcı</Label>
          <Input
            type="date"
            value={form.periodStart}
            onChange={e => setForm(f => ({ ...f, periodStart: e.target.value }))}
          />
        </div>
        <div className="space-y-1.5">
          <Label>Periyot Bitişi</Label>
          <Input
            type="date"
            value={form.periodEnd}
            onChange={e => setForm(f => ({ ...f, periodEnd: e.target.value }))}
          />
        </div>
      </div>

      <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !selectedBranch}>
        {mutation.isPending ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : <Save className="h-4 w-4 mr-1.5" />}
        Kaydet
      </Button>
    </div>
  );
}

// ─── Online Blocks Panel ──────────────────────────────────────────────────────

interface OnlineBlockForm {
  branchPublicId: string;
  startDatetime: string;
  endDatetime: string;
  reason: string;
}

function OnlineBlocksPanel({ doctorPublicId }: { doctorPublicId: string }) {
  const qc = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: allBranches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const branchOptions = allBranches.filter((b: BranchItem) => b.isActive);

  const defaultBlockForm = (): OnlineBlockForm => ({
    branchPublicId: branchOptions[0]?.publicId ?? '',
    startDatetime: '',
    endDatetime: '',
    reason: '',
  });

  const [form, setForm] = useState<OnlineBlockForm>(defaultBlockForm);

  const { data: blocks = [], isLoading } = useQuery({
    queryKey: ['doctor-online-blocks', doctorPublicId],
    queryFn: () => doctorSchedulesApi.getOnlineBlocks(doctorPublicId).then(r => r.data),
  });

  const createMutation = useMutation({
    mutationFn: () => doctorSchedulesApi.createOnlineBlock({
      doctorPublicId,
      branchPublicId: form.branchPublicId,
      startDatetime: new Date(form.startDatetime).toISOString(),
      endDatetime:   new Date(form.endDatetime).toISOString(),
      reason: form.reason || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-online-blocks', doctorPublicId] });
      toast.success('Blok eklendi.');
      setDialogOpen(false);
    },
    onError: () => toast.error('Ekleme başarısız.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => doctorSchedulesApi.deleteOnlineBlock(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['doctor-online-blocks', doctorPublicId] });
      toast.success('Blok silindi.');
      setDeleteId(null);
    },
    onError: () => toast.error('Silme başarısız.'),
  });

  const openAdd = () => {
    setForm({ ...defaultBlockForm(), branchPublicId: branchOptions[0]?.publicId ?? '' });
    setDialogOpen(true);
  };

  const formatDateTime = (iso: string) =>
    new Date(iso).toLocaleString('tr-TR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button size="sm" onClick={openAdd}>
          <Plus className="h-4 w-4 mr-1.5" /> Blok Ekle
        </Button>
      </div>

      {blocks.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
          <Ban className="h-10 w-10 opacity-30" />
          <p className="text-sm">Henüz online blok tanımlanmamış</p>
        </div>
      ) : (
        <div className="border rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left px-4 py-2 font-medium">Başlangıç</th>
                <th className="text-left px-4 py-2 font-medium">Bitiş</th>
                <th className="text-left px-4 py-2 font-medium">Şube</th>
                <th className="text-left px-4 py-2 font-medium">Açıklama</th>
                <th className="px-4 py-2 w-12" />
              </tr>
            </thead>
            <tbody>
              {(blocks as OnlineBlockItem[]).map(block => (
                <tr key={block.id} className="border-t">
                  <td className="px-4 py-2 font-medium">{formatDateTime(block.startDatetime)}</td>
                  <td className="px-4 py-2 text-muted-foreground">{formatDateTime(block.endDatetime)}</td>
                  <td className="px-4 py-2 text-muted-foreground text-xs">{block.branchName}</td>
                  <td className="px-4 py-2 text-muted-foreground">{block.reason ?? '─'}</td>
                  <td className="px-4 py-2">
                    <Button
                      variant="ghost" size="sm" className="h-7 w-7 p-0 text-destructive hover:text-destructive"
                      onClick={() => setDeleteId(block.id)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Add dialog */}
      <Dialog open={dialogOpen} onOpenChange={open => setDialogOpen(open)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Online Blok Ekle</DialogTitle>
          </DialogHeader>

          <div className="space-y-4 py-2">
            {branchOptions.length > 1 && (
              <div className="space-y-1.5">
                <Label>Şube</Label>
                <Select value={form.branchPublicId} onValueChange={v => setForm(f => ({ ...f, branchPublicId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Şube seçin" /></SelectTrigger>
                  <SelectContent>
                    {branchOptions.map((b: BranchItem) => (
                      <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Başlangıç</Label>
                <Input
                  type="datetime-local"
                  value={form.startDatetime}
                  onChange={e => setForm(f => ({ ...f, startDatetime: e.target.value }))}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Bitiş</Label>
                <Input
                  type="datetime-local"
                  value={form.endDatetime}
                  onChange={e => setForm(f => ({ ...f, endDatetime: e.target.value }))}
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Açıklama</Label>
              <Input
                value={form.reason}
                onChange={e => setForm(f => ({ ...f, reason: e.target.value }))}
                placeholder="Tatil, Kişisel, ..."
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>İptal</Button>
            <Button
              onClick={() => createMutation.mutate()}
              disabled={createMutation.isPending || !form.branchPublicId || !form.startDatetime || !form.endDatetime}
            >
              {createMutation.isPending ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> : null}
              Ekle
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <AlertDialog open={deleteId !== null} onOpenChange={open => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Bloğu sil</AlertDialogTitle>
            <AlertDialogDescription>Bu online blok kaydı kalıcı olarak silinecek. Emin misiniz?</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => deleteId !== null && deleteMutation.mutate(deleteId)}
            >
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
