import { useState } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  ArrowLeft, User, ClipboardList, ScanLine,
  CheckCheck,
  Phone, Mail, MapPin, Calendar, Heart,
} from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { protocolsApi } from '@/api/visits';
import { patientsApi } from '@/api/patients';
import type { DoctorProtocol } from '@/types/visit';
import type { Patient } from '@/types/patient';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function calcAge(birthDate?: string | null): number | null {
  if (!birthDate) return null;
  const birth = new Date(birthDate);
  const now = new Date();
  let age = now.getFullYear() - birth.getFullYear();
  const m = now.getMonth() - birth.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < birth.getDate())) age--;
  return age;
}

function InfoRow({ label, value, icon: Icon }: {
  label: string;
  value?: string | null;
  icon?: React.ElementType;
}) {
  if (!value) return null;
  return (
    <div className="flex items-start gap-3 py-2 border-b last:border-0">
      {Icon && <Icon className="size-4 text-muted-foreground mt-0.5 shrink-0" />}
      <div className="flex-1 min-w-0">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-sm font-medium break-words">{value}</p>
      </div>
    </div>
  );
}

// ─── Tab: Hasta Bilgileri ─────────────────────────────────────────────────────

function PatientInfoTab({ patientPublicId }: { patientPublicId: string }) {
  const { data: patient, isLoading } = useQuery<Patient>({
    queryKey: ['patient-detail', patientPublicId],
    queryFn: () => patientsApi.getById(patientPublicId).then((r) => r.data),
    staleTime: 5 * 60 * 1000,
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(5)].map((_, i) => (
          <Skeleton key={i} className="h-10 w-full" />
        ))}
      </div>
    );
  }

  if (!patient) {
    return (
      <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
        <User className="size-10 opacity-40" />
        <p className="text-sm">Hasta bilgisi yüklenemedi</p>
      </div>
    );
  }

  const fullName = `${patient.firstName} ${patient.lastName}`.trim();
  const age = calcAge(patient.birthDate?.toString() ?? null);
  const gender = patient.gender === 'Male' || patient.gender === 'M' ? 'Erkek'
    : patient.gender === 'Female' || patient.gender === 'F' ? 'Kadın' : patient.gender;

  return (
    <div className="space-y-4">
      {/* Kişisel bilgiler */}
      <div>
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Kişisel Bilgiler</p>
        <div className="rounded-lg border divide-y">
          <InfoRow label="Ad Soyad" value={fullName} icon={User} />
          <InfoRow
            label="Yaş / Cinsiyet"
            value={[age != null ? `${age} yaş` : null, gender].filter(Boolean).join(' · ')}
            icon={User}
          />
          {patient.birthDate && (
            <InfoRow
              label="Doğum Tarihi"
              value={format(new Date(patient.birthDate.toString()), 'd MMMM yyyy', { locale: tr })}
              icon={Calendar}
            />
          )}
          <InfoRow label="Kan Grubu" value={patient.bloodType} icon={Heart} />
          <InfoRow label="Meslek" value={patient.occupation} icon={User} />
        </div>
      </div>

      {/* İletişim */}
      <div>
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">İletişim</p>
        <div className="rounded-lg border divide-y">
          <InfoRow label="Telefon" value={patient.phone} icon={Phone} />
          <InfoRow label="Ev Telefonu" value={patient.homePhone} icon={Phone} />
          <InfoRow label="E-posta" value={patient.email} icon={Mail} />
          <InfoRow label="Adres" value={patient.address} icon={MapPin} />
        </div>
      </div>

      {patient.notes && (
        <div>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Notlar</p>
          <div className="rounded-lg border p-3 text-sm text-muted-foreground">{patient.notes}</div>
        </div>
      )}
    </div>
  );
}

// ─── Tab: Anamnez ─────────────────────────────────────────────────────────────

function AnamnezTab({ patientId }: { patientId: number }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">Hastanın tıbbi öyküsü</p>
        <Button size="sm" variant="outline" className="h-7 text-xs" disabled>
          Düzenle
        </Button>
      </div>

      {/* Placeholder sections */}
      {[
        { label: 'Genel Sağlık Durumu', placeholder: 'Genel sağlık değerlendirmesi...' },
        { label: 'Sistemik Hastalıklar', placeholder: 'Kalp, diyabet, tansiyon...' },
        { label: 'Geçirilmiş Ameliyatlar', placeholder: 'Operasyon geçmişi...' },
        { label: 'Aile Hastalık Öyküsü', placeholder: 'Kalıtsal hastalıklar...' },
        { label: 'Sigara / Alkol Kullanımı', placeholder: 'Alışkanlıklar...' },
        { label: 'Diş Hekimi Korkusu', placeholder: 'Anksiyete durumu...' },
      ].map((section) => (
        <div key={section.label} className="rounded-lg border p-3 space-y-1">
          <p className="text-xs font-semibold text-muted-foreground">{section.label}</p>
          <p className="text-sm text-muted-foreground/60 italic">{section.placeholder}</p>
        </div>
      ))}

      <div className="flex items-center gap-2 rounded-lg border border-dashed p-4 text-center justify-center">
        <ClipboardList className="size-4 text-muted-foreground/40" />
        <p className="text-sm text-muted-foreground">Anamnez formu yakında aktif olacak</p>
      </div>
    </div>
  );
}

// ─── Tab: Oral Diagnoz ────────────────────────────────────────────────────────

function OralDiagnozTab({ patientId }: { patientId: number }) {
  // 32 diş — üst: 18-11, 21-28 | alt: 48-41, 31-38
  const upperRight = [18, 17, 16, 15, 14, 13, 12, 11];
  const upperLeft  = [21, 22, 23, 24, 25, 26, 27, 28];
  const lowerRight = [48, 47, 46, 45, 44, 43, 42, 41];
  const lowerLeft  = [31, 32, 33, 34, 35, 36, 37, 38];

  function ToothCell({ number }: { number: number }) {
    return (
      <div className="flex flex-col items-center gap-0.5">
        <div className="w-8 h-8 rounded border-2 border-muted hover:border-primary cursor-pointer transition-colors flex items-center justify-center text-[10px] text-muted-foreground hover:text-foreground hover:bg-primary/5">
        </div>
        <span className="text-[9px] text-muted-foreground">{number}</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">Dişlere tıklayarak durum ekleyebilirsiniz</p>
        <Button size="sm" variant="outline" className="h-7 text-xs" disabled>
          Tam Ekran
        </Button>
      </div>

      {/* Diş şeması */}
      <div className="rounded-xl border bg-muted/20 p-4 space-y-3">
        {/* Üst çene */}
        <div className="space-y-1">
          <p className="text-[10px] text-center text-muted-foreground uppercase tracking-wider">Üst Çene</p>
          <div className="flex justify-center gap-1">
            {upperRight.map((n) => <ToothCell key={n} number={n} />)}
            <div className="w-px bg-border mx-1" />
            {upperLeft.map((n) => <ToothCell key={n} number={n} />)}
          </div>
        </div>

        {/* Orta çizgi */}
        <div className="h-px bg-border" />

        {/* Alt çene */}
        <div className="space-y-1">
          <div className="flex justify-center gap-1">
            {lowerRight.map((n) => <ToothCell key={n} number={n} />)}
            <div className="w-px bg-border mx-1" />
            {lowerLeft.map((n) => <ToothCell key={n} number={n} />)}
          </div>
          <p className="text-[10px] text-center text-muted-foreground uppercase tracking-wider">Alt Çene</p>
        </div>
      </div>

      {/* Renk skalası */}
      <div className="flex flex-wrap gap-2">
        {[
          { label: 'Sağlıklı',      color: 'bg-emerald-100 border-emerald-300 text-emerald-700' },
          { label: 'Çürük',         color: 'bg-red-100 border-red-300 text-red-700' },
          { label: 'Dolgu',         color: 'bg-blue-100 border-blue-300 text-blue-700' },
          { label: 'Kanal Tedavisi',color: 'bg-purple-100 border-purple-300 text-purple-700' },
          { label: 'Eksik',         color: 'bg-slate-100 border-slate-300 text-slate-700' },
          { label: 'Köprü',         color: 'bg-amber-100 border-amber-300 text-amber-700' },
        ].map((item) => (
          <span key={item.label} className={cn('text-[10px] px-2 py-0.5 rounded border', item.color)}>
            {item.label}
          </span>
        ))}
      </div>

      <div className="flex items-center gap-2 rounded-lg border border-dashed p-4 text-center justify-center">
        <ScanLine className="size-4 text-muted-foreground/40" />
        <p className="text-sm text-muted-foreground">İnteraktif diş şeması yakında aktif olacak</p>
      </div>
    </div>
  );
}

// ─── Close Protocol Dialog ────────────────────────────────────────────────────

function CloseProtocolDialog({
  protocol,
  onConfirm,
  onCancel,
  isPending,
}: {
  protocol: DoctorProtocol;
  onConfirm: (diagnosis: string, notes: string) => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const [diagnosis, setDiagnosis] = useState('');
  const [notes, setNotes] = useState('');

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

// ─── Main Page ────────────────────────────────────────────────────────────────

export function ExaminationPage() {
  const { publicId } = useParams<{ publicId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [closeOpen, setCloseOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('hasta-bilgileri');

  // Protocol info passed via query params for instant display
  const patientName      = searchParams.get('patient') ?? 'Hasta';
  const protocolNo       = searchParams.get('no') ?? '';
  const patientPublicId  = searchParams.get('patientPublicId') ?? '';

  // Placeholder protocol object from URL params
  const protocol: DoctorProtocol | null = patientPublicId ? {
    publicId: publicId ?? '',
    protocolNo: protocolNo,
    patientId: 0,
    patientPublicId,
    patientName,
    phone: null,
    protocolType: 1,
    protocolTypeName: searchParams.get('type') ?? '',
    status: 1,
    statusName: 'Açık',
    startedAt: null,
  } : null;

  const completeMutation = useMutation({
    mutationFn: ({ diagnosis, notes }: { diagnosis: string; notes: string }) =>
      protocolsApi.complete(publicId!, diagnosis || undefined, notes || undefined),
    onSuccess: () => {
      toast.success('Protokol kapatıldı.');
      qc.invalidateQueries({ queryKey: ['doctor-protocols'] });
      qc.invalidateQueries({ queryKey: ['doctor-appointments'] });
      navigate('/doctor');
    },
    onError: () => {
      toast.error('Protokol kapatılamadı.');
    },
  });

  const TAB_ITEMS = [
    { value: 'hasta-bilgileri', label: 'Hasta Bilgileri', icon: User },
    { value: 'anamnez',         label: 'Anamnez',         icon: ClipboardList },
    { value: 'oral-diagnoz',    label: 'Oral Diagnoz',    icon: ScanLine },
  ];

  return (
    <div className="flex flex-col h-full">
      {/* ── Header ─────────────────────────────────────────────── */}
      <div className="flex items-center gap-3 px-4 py-3 border-b bg-background/95 backdrop-blur shrink-0">
        <Button
          variant="ghost"
          size="sm"
          className="gap-1.5 text-muted-foreground"
          onClick={() => navigate('/doctor')}
        >
          <ArrowLeft className="size-4" />
          Geri
        </Button>

        <Separator orientation="vertical" className="h-5" />

        {/* Protocol info */}
        <div className="flex items-center gap-2 flex-1 min-w-0">
          <div className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
            <Badge className="bg-emerald-500 hover:bg-emerald-500 text-white text-xs h-5 px-1.5">
              Odada
            </Badge>
          </div>
          <span className="font-semibold truncate">{patientName}</span>
          {protocolNo && (
            <span className="text-xs text-muted-foreground font-mono shrink-0">{protocolNo}</span>
          )}
        </div>

        {/* Close protocol */}
        <Button
          size="sm"
          variant="outline"
          className="gap-1.5 border-emerald-300 text-emerald-700 hover:bg-emerald-50 shrink-0"
          onClick={() => setCloseOpen(true)}
        >
          <CheckCheck className="size-4" />
          Protokolü Kapat
        </Button>
      </div>

      {/* ── Tab bar ────────────────────────────────────────────── */}
      <Tabs
        value={activeTab}
        onValueChange={setActiveTab}
        className="flex flex-col flex-1 min-h-0"
      >
        <div className="border-b px-4 bg-background shrink-0">
          <TabsList className="h-10 bg-transparent gap-0 p-0 rounded-none">
            {TAB_ITEMS.map((tab) => {
              const Icon = tab.icon;
              return (
                <TabsTrigger
                  key={tab.value}
                  value={tab.value}
                  className={cn(
                    'gap-1.5 rounded-none border-b-2 border-transparent px-4 h-10 text-sm',
                    'data-[state=active]:border-primary data-[state=active]:bg-transparent',
                    'data-[state=active]:shadow-none',
                  )}
                >
                  <Icon className="size-3.5" />
                  {tab.label}
                </TabsTrigger>
              );
            })}
          </TabsList>
        </div>

        {/* ── Tab panels ─────────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto">
          <div className="max-w-2xl mx-auto p-4">
            <TabsContent value="hasta-bilgileri" className="mt-0">
              {protocol?.patientPublicId ? (
                <PatientInfoTab patientPublicId={protocol.patientPublicId} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </TabsContent>

            <TabsContent value="anamnez" className="mt-0">
              {protocol?.patientPublicId ? (
                <AnamnezTab patientId={0} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </TabsContent>

            <TabsContent value="oral-diagnoz" className="mt-0">
              {protocol?.patientPublicId ? (
                <OralDiagnozTab patientId={0} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </TabsContent>
          </div>
        </div>
      </Tabs>

      {/* ── Close dialog ────────────────────────────────────────── */}
      {closeOpen && protocol && (
        <CloseProtocolDialog
          protocol={protocol}
          onConfirm={(diagnosis, notes) => completeMutation.mutate({ diagnosis, notes })}
          onCancel={() => setCloseOpen(false)}
          isPending={completeMutation.isPending}
        />
      )}
    </div>
  );
}
