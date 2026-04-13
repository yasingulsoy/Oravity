import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  ArrowLeft, User, ClipboardList,
  CheckCheck, AlertTriangle, Cigarette, Wine,
  Phone, Mail, MapPin, Calendar, Heart, Save,
  FileText, Search, Trash2, History, Lock, X,
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
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { protocolsApi } from '@/api/visits';
import { patientsApi } from '@/api/patients';
import { dentalApi } from '@/api/dental';
import { ToothStatus, STATUS_META } from '@/types/dental';
import type { ToothRecord } from '@/types/dental';
import type { DoctorProtocol, ProtocolDetail, IcdCode, ProtocolDiagnosis, ProtocolHistoryItem } from '@/types/visit';
import type { Patient, PatientAnamnesis, AnamnesisHistoryItem } from '@/types/patient';

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

// ─── Anamnez helpers ──────────────────────────────────────────────────────────

const CRITICAL_FLAGS: Array<{ key: keyof PatientAnamnesis; label: string }> = [
  { key: 'localAnesthesiaAllergy', label: 'Lokal Anestezi Alerjisi' },
  { key: 'hasPenicillinAllergy',   label: 'Penisilin Alerjisi' },
  { key: 'onAnticoagulant',        label: 'Kan Sulandırıcı İlaç' },
  { key: 'bleedingTendency',       label: 'Kanama Eğilimi' },
  { key: 'hasPacemaker',           label: 'Kalp Pili' },
  { key: 'bisphosphonateUse',      label: 'Bifosfonat Kullanımı' },
  { key: 'hasHiv',                 label: 'HIV+' },
  { key: 'hasHepatitisB',          label: 'Hepatit B' },
  { key: 'hasHepatitisC',          label: 'Hepatit C' },
];

const SYSTEMIC_FLAGS: Array<{ key: keyof PatientAnamnesis; label: string }> = [
  { key: 'hasDiabetes',      label: 'Diyabet' },
  { key: 'hasHypertension',  label: 'Hipertansiyon' },
  { key: 'hasHeartDisease',  label: 'Kalp Hastalığı' },
  { key: 'hasAsthma',        label: 'Astım' },
  { key: 'hasEpilepsy',      label: 'Epilepsi' },
  { key: 'hasKidneyDisease', label: 'Böbrek Hastalığı' },
  { key: 'hasLiverDisease',  label: 'Karaciğer Hastalığı' },
];

const ALLERGY_FLAGS: Array<{ key: keyof PatientAnamnesis; label: string }> = [
  { key: 'hasPenicillinAllergy',  label: 'Penisilin' },
  { key: 'hasAspirinAllergy',     label: 'Aspirin' },
  { key: 'hasLatexAllergy',       label: 'Lateks' },
  { key: 'localAnesthesiaAllergy',label: 'Lokal Anestezi' },
];

function AnamnesisSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">{title}</p>
      <div className="rounded-lg border p-3 space-y-2">{children}</div>
    </div>
  );
}

function BoolRow({ label, value }: { label: string; value: boolean }) {
  return (
    <div className="flex items-center justify-between py-0.5">
      <span className="text-sm">{label}</span>
      <span className={cn(
        'text-xs px-2 py-0.5 rounded-full font-medium',
        value ? 'bg-red-100 text-red-700' : 'bg-muted text-muted-foreground',
      )}>
        {value ? 'Evet' : 'Hayır'}
      </span>
    </div>
  );
}

function AnamnezTab({ patientPublicId, protocolPublicId }: { patientPublicId: string; protocolPublicId: string }) {
  const qc = useQueryClient();

  // ── Latest anamnesis (pre-fill) ────────────────────────────────────────────
  const { data: latest, isLoading } = useQuery<PatientAnamnesis | null>({
    queryKey: ['patient-anamnesis', patientPublicId],
    queryFn: async () => {
      try {
        const r = await patientsApi.getAnamnesis(patientPublicId);
        return r.data;
      } catch (e: any) {
        if (e?.response?.status === 204 || e?.response?.status === 404) return null;
        throw e;
      }
    },
    staleTime: 5 * 60 * 1000,
  });

  // ── History ────────────────────────────────────────────────────────────────
  const { data: history = [] } = useQuery<AnamnesisHistoryItem[]>({
    queryKey: ['patient-anamnesis-history', patientPublicId],
    queryFn: () => patientsApi.getAnamnesisHistory(patientPublicId).then((r) => r.data),
    staleTime: 30_000,
  });

  // ── Draft — initialised from latest record ─────────────────────────────────
  const emptyDraft = (): Partial<PatientAnamnesis> => ({
    bloodType: null, isPregnant: false, isBreastfeeding: false,
    hasDiabetes: false, hasHypertension: false, hasHeartDisease: false, hasPacemaker: false,
    hasAsthma: false, hasEpilepsy: false, hasKidneyDisease: false, hasLiverDisease: false,
    hasHiv: false, hasHepatitisB: false, hasHepatitisC: false, otherSystemicDiseases: null,
    localAnesthesiaAllergy: false, localAnesthesiaAllergyNote: null,
    bleedingTendency: false, onAnticoagulant: false, anticoagulantDrug: null, bisphosphonateUse: false,
    hasPenicillinAllergy: false, hasAspirinAllergy: false, hasLatexAllergy: false, otherAllergies: null,
    previousSurgeries: null, brushingFrequency: null, usesFloss: false,
    smokingStatus: 0, smokingAmount: null, alcoholUse: 0, additionalNotes: null,
  });

  const fromRecord = (a: PatientAnamnesis): Partial<PatientAnamnesis> => ({
    bloodType: a.bloodType, isPregnant: a.isPregnant, isBreastfeeding: a.isBreastfeeding,
    hasDiabetes: a.hasDiabetes, hasHypertension: a.hasHypertension, hasHeartDisease: a.hasHeartDisease,
    hasPacemaker: a.hasPacemaker, hasAsthma: a.hasAsthma, hasEpilepsy: a.hasEpilepsy,
    hasKidneyDisease: a.hasKidneyDisease, hasLiverDisease: a.hasLiverDisease,
    hasHiv: a.hasHiv, hasHepatitisB: a.hasHepatitisB, hasHepatitisC: a.hasHepatitisC,
    otherSystemicDiseases: a.otherSystemicDiseases,
    localAnesthesiaAllergy: a.localAnesthesiaAllergy, localAnesthesiaAllergyNote: a.localAnesthesiaAllergyNote,
    bleedingTendency: a.bleedingTendency, onAnticoagulant: a.onAnticoagulant,
    anticoagulantDrug: a.anticoagulantDrug, bisphosphonateUse: a.bisphosphonateUse,
    hasPenicillinAllergy: a.hasPenicillinAllergy, hasAspirinAllergy: a.hasAspirinAllergy,
    hasLatexAllergy: a.hasLatexAllergy, otherAllergies: a.otherAllergies,
    previousSurgeries: a.previousSurgeries, brushingFrequency: a.brushingFrequency,
    usesFloss: a.usesFloss, smokingStatus: a.smokingStatus ?? 0,
    smokingAmount: a.smokingAmount, alcoholUse: a.alcoholUse ?? 0, additionalNotes: a.additionalNotes,
  });

  const [draft, setDraft] = useState<Partial<PatientAnamnesis>>(emptyDraft);
  const [initialized, setInitialized] = useState(false);
  const [selectedHistoryId, setSelectedHistoryId] = useState<string | null>(null);
  const [loadingHistoryId, setLoadingHistoryId] = useState<string | null>(null);

  useEffect(() => {
    if (!initialized && latest !== undefined) {
      setDraft(latest ? fromRecord(latest) : emptyDraft());
      setSelectedHistoryId(latest?.publicId ?? null);
      setInitialized(true);
    }
  }, [latest, initialized]);

  const loadHistoryRecord = async (anamnesisPublicId: string) => {
    if (loadingHistoryId) return;
    setLoadingHistoryId(anamnesisPublicId);
    try {
      const r = await patientsApi.getAnamnesisById(patientPublicId, anamnesisPublicId);
      setDraft(fromRecord(r.data));
      setSelectedHistoryId(anamnesisPublicId);
    } catch {
      toast.error('Kayıt yüklenemedi.');
    } finally {
      setLoadingHistoryId(null);
    }
  };

  const toggle = (key: keyof PatientAnamnesis) =>
    setDraft((prev) => ({ ...prev, [key]: !(prev[key] as boolean) }));

  const setText = (key: keyof PatientAnamnesis, val: string) =>
    setDraft((prev) => ({ ...prev, [key]: val || null }));

  const saveMutation = useMutation({
    mutationFn: () => patientsApi.upsertAnamnesis(patientPublicId, draft as any, protocolPublicId),
    onSuccess: (res) => {
      setSelectedHistoryId(res.data.publicId);
      qc.invalidateQueries({ queryKey: ['patient-anamnesis', patientPublicId] });
      qc.invalidateQueries({ queryKey: ['patient-anamnesis-history', patientPublicId] });
      toast.success('Anamnez kaydedildi.');
    },
    onError: () => toast.error('Kayıt başarısız.'),
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
      </div>
    );
  }

  const d = draft as PatientAnamnesis;

  return (
    <div className="grid grid-cols-[1fr_280px] gap-6 items-start">
      {/* ── Sol: Form (her zaman açık) ───────────────────────────────────────── */}
      <div className="space-y-4">
        {/* Geçmiş kayıt banner */}
        {selectedHistoryId && history.length > 0 && selectedHistoryId !== history[0]?.publicId && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 flex items-center gap-2 text-sm">
            <AlertTriangle className="size-4 text-amber-600 shrink-0" />
            <span className="text-amber-800">
              Geçmiş bir kayıt görüntülüyorsunuz. Kaydet'e basarsanız bu verilerle yeni kayıt oluşturulur.
            </span>
          </div>
        )}
        {/* Kritik uyarı banner */}
        {CRITICAL_FLAGS.some(({ key }) => !!(d[key])) && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 flex items-center gap-2">
            <AlertTriangle className="size-4 text-red-600 shrink-0" />
            <div className="flex flex-wrap gap-1">
              {CRITICAL_FLAGS.filter(({ key }) => !!(d[key])).map(({ label }) => (
                <span key={label} className="text-xs bg-red-100 text-red-700 border border-red-200 px-2 py-0.5 rounded-full font-medium">
                  {label}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Genel */}
        <AnamnesisSection title="Genel">
          <div className="grid grid-cols-2 gap-x-4">
            {([['isPregnant', 'Hamile'], ['isBreastfeeding', 'Emziriyor']] as [keyof PatientAnamnesis, string][]).map(([k, lbl]) => (
              <label key={k as string} className="flex items-center gap-2 cursor-pointer py-1">
                <input type="checkbox" className="h-4 w-4 rounded" checked={!!(d[k])} onChange={() => toggle(k)} />
                <span className="text-sm">{lbl}</span>
              </label>
            ))}
          </div>
        </AnamnesisSection>

        {/* Sistemik */}
        <AnamnesisSection title="Sistemik Hastalıklar">
          <div className="grid grid-cols-2 gap-x-4">
            {SYSTEMIC_FLAGS.concat([
              { key: 'hasPacemaker' as keyof PatientAnamnesis, label: 'Kalp Pili' },
              { key: 'hasHiv' as keyof PatientAnamnesis, label: 'HIV+' },
              { key: 'hasHepatitisB' as keyof PatientAnamnesis, label: 'Hepatit B' },
              { key: 'hasHepatitisC' as keyof PatientAnamnesis, label: 'Hepatit C' },
            ]).map(({ key, label }) => (
              <label key={key as string} className="flex items-center gap-2 cursor-pointer py-1">
                <input type="checkbox" className="h-4 w-4 rounded" checked={!!(d[key])} onChange={() => toggle(key)} />
                <span className="text-sm">{label}</span>
              </label>
            ))}
          </div>
          <div className="pt-1">
            <p className="text-xs text-muted-foreground mb-1">Diğer sistemik hastalıklar</p>
            <Textarea rows={2} className="text-sm" value={d.otherSystemicDiseases ?? ''}
              onChange={(e) => setText('otherSystemicDiseases', e.target.value)} placeholder="Varsa belirtin..." />
          </div>
        </AnamnesisSection>

        {/* Alerjiler */}
        <AnamnesisSection title="Alerjiler">
          <div className="grid grid-cols-2 gap-x-4">
            {ALLERGY_FLAGS.map(({ key, label }) => (
              <label key={key as string} className="flex items-center gap-2 cursor-pointer py-1">
                <input type="checkbox" className="h-4 w-4 rounded" checked={!!(d[key])} onChange={() => toggle(key)} />
                <span className="text-sm">{label}</span>
              </label>
            ))}
          </div>
          {d.localAnesthesiaAllergy && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Lokal anestezi alerjisi notu</p>
              <Textarea rows={1} className="text-sm" value={d.localAnesthesiaAllergyNote ?? ''}
                onChange={(e) => setText('localAnesthesiaAllergyNote', e.target.value)} placeholder="İlaç adı, reaksiyon tipi..." />
            </div>
          )}
          <div>
            <p className="text-xs text-muted-foreground mb-1">Diğer alerjiler</p>
            <Textarea rows={1} className="text-sm" value={d.otherAllergies ?? ''}
              onChange={(e) => setText('otherAllergies', e.target.value)} placeholder="Varsa belirtin..." />
          </div>
        </AnamnesisSection>

        {/* Diş hekimliği spesifik */}
        <AnamnesisSection title="Diş Hekimliği Spesifik">
          <div className="grid grid-cols-2 gap-x-4">
            {([
              ['bleedingTendency', 'Kanama Eğilimi'],
              ['onAnticoagulant', 'Kan Sulandırıcı'],
              ['bisphosphonateUse', 'Bifosfonat'],
            ] as [keyof PatientAnamnesis, string][]).map(([k, lbl]) => (
              <label key={k as string} className="flex items-center gap-2 cursor-pointer py-1">
                <input type="checkbox" className="h-4 w-4 rounded" checked={!!(d[k])} onChange={() => toggle(k)} />
                <span className="text-sm">{lbl}</span>
              </label>
            ))}
          </div>
          {d.onAnticoagulant && (
            <div>
              <p className="text-xs text-muted-foreground mb-1">Kullanılan kan sulandırıcı ilaç</p>
              <Textarea rows={1} className="text-sm" value={d.anticoagulantDrug ?? ''}
                onChange={(e) => setText('anticoagulantDrug', e.target.value)} placeholder="İlaç adı..." />
            </div>
          )}
        </AnamnesisSection>

        {/* Sosyal alışkanlıklar */}
        <AnamnesisSection title="Sosyal Alışkanlıklar">
          <div className="space-y-2">
            <div>
              <p className="text-xs text-muted-foreground mb-1">Sigara</p>
              <div className="flex gap-3">
                {['Hayır', 'Evet', 'Bıraktı'].map((lbl, i) => (
                  <label key={i} className="flex items-center gap-1.5 cursor-pointer">
                    <input type="radio" name={`smoking-${patientPublicId}`} checked={d.smokingStatus === i}
                      onChange={() => setDraft(p => ({ ...p, smokingStatus: i }))} />
                    <span className="text-sm">{lbl}</span>
                  </label>
                ))}
              </div>
            </div>
            {d.smokingStatus === 1 && (
              <Textarea rows={1} className="text-sm" value={d.smokingAmount ?? ''}
                onChange={(e) => setText('smokingAmount', e.target.value)} placeholder="Günde kaç adet..." />
            )}
            <div>
              <p className="text-xs text-muted-foreground mb-1">Alkol</p>
              <div className="flex gap-3">
                {['Hayır', 'Sosyal', 'Düzenli'].map((lbl, i) => (
                  <label key={i} className="flex items-center gap-1.5 cursor-pointer">
                    <input type="radio" name={`alcohol-${patientPublicId}`} checked={d.alcoholUse === i}
                      onChange={() => setDraft(p => ({ ...p, alcoholUse: i }))} />
                    <span className="text-sm">{lbl}</span>
                  </label>
                ))}
              </div>
            </div>
          </div>
        </AnamnesisSection>

        {/* Geçmiş ameliyatlar */}
        <AnamnesisSection title="Geçirilmiş Ameliyatlar">
          <Textarea rows={2} className="text-sm" value={d.previousSurgeries ?? ''}
            onChange={(e) => setText('previousSurgeries', e.target.value)} placeholder="Varsa belirtin..." />
        </AnamnesisSection>

        {/* Ek notlar */}
        <AnamnesisSection title="Ek Notlar">
          <Textarea rows={2} className="text-sm" value={d.additionalNotes ?? ''}
            onChange={(e) => setText('additionalNotes', e.target.value)} placeholder="Diğer önemli bilgiler..." />
        </AnamnesisSection>

        <Button className="w-full gap-1.5" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
          <Save className="size-4" />
          {saveMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
        </Button>
      </div>

      {/* ── Sağ: Anamnez Geçmişi ──────────────────────────────────────────────── */}
      <div className="space-y-2">
        <div className="flex items-center gap-1.5">
          <History className="size-3.5 text-muted-foreground" />
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Anamnez Geçmişi</p>
        </div>

        {history.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-8 text-muted-foreground rounded-lg border border-dashed">
            <ClipboardList className="size-6 opacity-30" />
            <p className="text-xs">Henüz kayıt yok</p>
          </div>
        ) : (
          <div className="space-y-2">
            {[...history].sort((a, b) => new Date(b.filledAt).getTime() - new Date(a.filledAt).getTime()).map((h, idx) => (
              <button
                key={h.publicId}
                className={cn(
                  'w-full text-left rounded-lg border p-3 space-y-1.5 text-xs transition-colors hover:bg-accent',
                  selectedHistoryId === h.publicId
                    ? 'border-primary/50 bg-primary/5 ring-1 ring-primary/20'
                    : 'border-border',
                )}
                disabled={loadingHistoryId === h.publicId}
                onClick={() => loadHistoryRecord(h.publicId)}
              >
                <div className="flex items-center justify-between gap-1">
                  <span className="font-medium text-foreground">
                    {format(new Date(h.filledAt), 'd MMM yyyy HH:mm', { locale: tr })}
                  </span>
                  <div className="flex items-center gap-1 shrink-0">
                    {loadingHistoryId === h.publicId && (
                      <span className="text-[10px] text-muted-foreground">yükleniyor...</span>
                    )}
                    {idx === 0 && (
                      <span className="text-[10px] bg-green-100 text-green-700 px-1.5 py-0.5 rounded-full font-medium">Son</span>
                    )}
                    {selectedHistoryId === h.publicId && (
                      <span className="text-[10px] bg-primary/10 text-primary px-1.5 py-0.5 rounded-full font-medium">Görüntüleniyor</span>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-1 text-muted-foreground">
                  <User className="size-3 shrink-0" />
                  <span className="truncate">{h.filledByName || '—'}</span>
                </div>
                <div className="flex items-center gap-1.5 flex-wrap">
                  {h.hasCriticalAlert && (
                    <span className="text-[10px] bg-red-100 text-red-700 border border-red-200 px-1.5 py-0.5 rounded-full font-medium flex items-center gap-0.5">
                      <AlertTriangle className="size-2.5" /> Kritik
                    </span>
                  )}
                  {h.smokingStatus === 1 && (
                    <span className="text-[10px] bg-muted text-muted-foreground px-1.5 py-0.5 rounded-full flex items-center gap-0.5">
                      <Cigarette className="size-2.5" /> Sigara
                    </span>
                  )}
                  {h.alcoholUse != null && h.alcoholUse > 0 && (
                    <span className="text-[10px] bg-muted text-muted-foreground px-1.5 py-0.5 rounded-full flex items-center gap-0.5">
                      <Wine className="size-2.5" /> Alkol
                    </span>
                  )}
                  {!h.hasCriticalAlert && h.smokingStatus !== 1 && !h.alcoholUse && (
                    <span className="text-[10px] text-emerald-600 flex items-center gap-0.5">
                      <CheckCheck className="size-2.5" /> Temiz
                    </span>
                  )}
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Tab: Oral Diagnoz ────────────────────────────────────────────────────────

// ─── Tooth SVG ───────────────────────────────────────────────────────────────

function ToothSvg({
  tooth,
  selected,
  onClick,
  isUpper,
}: {
  tooth: ToothRecord;
  selected: boolean;
  onClick: () => void;
  isUpper: boolean;
}) {
  const meta  = STATUS_META[tooth.status as ToothStatus];
  const surfs = new Set((tooth.surfaces ?? '').toUpperCase().split(''));

  const SURFACES = isUpper
    ? { top: 'V', bottom: 'L', left: 'M', right: 'D', center: 'O' }
    : { top: 'L', bottom: 'V', left: 'M', right: 'D', center: 'O' };

  const isExtracted = tooth.status === ToothStatus.Extracted || tooth.status === ToothStatus.CongenitallyMissing;

  function surfFill(key: 'top' | 'bottom' | 'left' | 'right' | 'center') {
    if (isExtracted) return '#f3f4f6';
    const code = SURFACES[key];
    return surfs.size === 0 || surfs.has(code) ? meta.fill : '#f9fafb';
  }

  return (
    <button
      onClick={onClick}
      title={`${tooth.toothNumber} — ${meta.label}`}
      className={cn(
        'flex flex-col items-center gap-0.5 group rounded p-0.5 transition-colors',
        selected ? 'bg-primary/10 ring-1 ring-primary/40' : 'hover:bg-muted/60',
      )}
    >
      <svg
        viewBox="0 0 36 44"
        width={32}
        height={40}
        className="overflow-visible"
      >
        {isExtracted ? (
          <>
            <rect x="2" y="2" width="32" height="40" rx="4" fill="#f3f4f6" stroke="#d1d5db" strokeWidth="1.5" strokeDasharray="4 2" />
            <line x1="10" y1="10" x2="26" y2="34" stroke="#9ca3af" strokeWidth="2" strokeLinecap="round" />
            <line x1="26" y1="10" x2="10" y2="34" stroke="#9ca3af" strokeWidth="2" strokeLinecap="round" />
          </>
        ) : (
          <>
            {/* Buccal/Lingual top */}
            <polygon
              points="0,0 36,0 28,12 8,12"
              fill={surfFill('top')}
              stroke={meta.stroke}
              strokeWidth="1"
            />
            {/* Lingual/Buccal bottom */}
            <polygon
              points="8,32 28,32 36,44 0,44"
              fill={surfFill('bottom')}
              stroke={meta.stroke}
              strokeWidth="1"
            />
            {/* Mesial left */}
            <polygon
              points="0,0 8,12 8,32 0,44"
              fill={surfFill('left')}
              stroke={meta.stroke}
              strokeWidth="1"
            />
            {/* Distal right */}
            <polygon
              points="28,12 36,0 36,44 28,32"
              fill={surfFill('right')}
              stroke={meta.stroke}
              strokeWidth="1"
            />
            {/* Occlusal center */}
            <rect
              x="8" y="12" width="20" height="20"
              fill={surfFill('center')}
              stroke={meta.stroke}
              strokeWidth="1"
            />
            {/* Selected ring */}
            {selected && (
              <rect x="0" y="0" width="36" height="44" rx="2" fill="none" stroke="#6366f1" strokeWidth="2" />
            )}
          </>
        )}
      </svg>
      <span className="text-[9px] text-muted-foreground group-hover:text-foreground">{tooth.toothNumber}</span>
    </button>
  );
}

// ─── Tooth Edit Panel ─────────────────────────────────────────────────────────

function ToothEditPanel({
  tooth,
  patientPublicId,
  onSave,
  onClose,
}: {
  tooth: ToothRecord;
  patientPublicId: string;
  onSave: (updated: ToothRecord) => void;
  onClose: () => void;
}) {
  const [status,   setStatus]   = useState<ToothStatus>(tooth.status);
  const [surfaces, setSurfaces] = useState(tooth.surfaces ?? '');
  const [notes,    setNotes]    = useState(tooth.notes ?? '');

  const SURFACE_CODES = ['M', 'D', 'O', 'V', 'L'];

  const toggleSurface = (code: string) => {
    setSurfaces(prev => {
      const set = new Set(prev.toUpperCase().split('').filter(Boolean));
      set.has(code) ? set.delete(code) : set.add(code);
      return [...set].join('');
    });
  };

  const mutation = useMutation({
    mutationFn: () => dentalApi.updateTooth(patientPublicId, tooth.toothNumber, status, surfaces || null, notes || null),
    onSuccess: (res) => {
      toast.success(`${tooth.toothNumber} nolu diş güncellendi.`);
      onSave(res.data);
    },
    onError: () => toast.error('Güncelleme başarısız.'),
  });

  const surfSet = new Set(surfaces.toUpperCase().split('').filter(Boolean));

  return (
    <div className="rounded-xl border bg-card shadow-sm p-4 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <p className="font-semibold text-sm">Diş {tooth.toothNumber}</p>
          <p className="text-xs text-muted-foreground">{tooth.quadrantLabel} · {tooth.toothType}</p>
        </div>
        <button onClick={onClose} className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted">
          <X className="size-4" />
        </button>
      </div>

      {/* Durum seçimi */}
      <div className="space-y-1.5">
        <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Durum</Label>
        <div className="grid grid-cols-3 gap-1.5">
          {(Object.values(ToothStatus).filter(v => typeof v === 'number') as ToothStatus[]).map((s) => {
            const m = STATUS_META[s];
            return (
              <button
                key={s}
                onClick={() => setStatus(s)}
                className={cn(
                  'text-xs px-2 py-1.5 rounded border text-left transition-all',
                  status === s ? 'ring-2 ring-primary border-primary font-medium' : 'hover:bg-muted',
                )}
                style={status === s ? { backgroundColor: m.fill, borderColor: m.stroke, color: m.text } : {}}
              >
                {m.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Yüzey seçimi */}
      {status !== ToothStatus.Extracted && status !== ToothStatus.CongenitallyMissing && (
        <div className="space-y-1.5">
          <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Etkilenen Yüzeyler
          </Label>
          <div className="flex gap-1.5">
            {SURFACE_CODES.map((code) => (
              <button
                key={code}
                onClick={() => toggleSurface(code)}
                className={cn(
                  'w-8 h-8 rounded border text-xs font-mono font-medium transition-all',
                  surfSet.has(code)
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'bg-background hover:bg-muted',
                )}
              >
                {code}
              </button>
            ))}
          </div>
          <p className="text-[10px] text-muted-foreground">M=Mezyal D=Distal O=Oklüzal V=Vestibüler L=Lingual</p>
        </div>
      )}

      {/* Notlar */}
      <div className="space-y-1.5">
        <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Not</Label>
        <Textarea
          rows={2}
          className="text-sm resize-none"
          placeholder="Ek not..."
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />
      </div>

      <div className="flex gap-2">
        <Button variant="outline" size="sm" className="flex-1" onClick={onClose}>İptal</Button>
        <Button size="sm" className="flex-1 gap-1" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          <Save className="size-3.5" />
          {mutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
        </Button>
      </div>
    </div>
  );
}

// ─── Tab: Oral Diagnoz ────────────────────────────────────────────────────────

function OralDiagnozTab({ patientPublicId }: { patientPublicId: string }) {
  const [teethMap, setTeethMap]     = useState<Record<string, ToothRecord>>({});
  const [selected, setSelected]     = useState<string | null>(null);
  const [loading,  setLoading]      = useState(true);

  useEffect(() => {
    dentalApi.getChart(patientPublicId).then(r => {
      const map: Record<string, ToothRecord> = {};
      r.data.teeth.forEach(t => { map[t.toothNumber] = t; });
      setTeethMap(map);
    }).finally(() => setLoading(false));
  }, [patientPublicId]);

  const handleSave = (updated: ToothRecord) => {
    setTeethMap(prev => ({ ...prev, [updated.toothNumber]: updated }));
    setSelected(null);
  };

  const defaultTooth = (num: string): ToothRecord => ({
    publicId: '', toothNumber: num, quadrantLabel: '', toothType: '',
    status: ToothStatus.Healthy, statusLabel: 'Sağlıklı',
    surfaces: null, notes: null, recordedBy: 0,
    recordedAt: '', createdAt: '',
  });

  const rows = {
    upperRight: ['18','17','16','15','14','13','12','11'],
    upperLeft:  ['21','22','23','24','25','26','27','28'],
    lowerRight: ['48','47','46','45','44','43','42','41'],
    lowerLeft:  ['31','32','33','34','35','36','37','38'],
  };

  if (loading) {
    return (
      <div className="space-y-3">
        {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }

  const selectedTooth = selected ? (teethMap[selected] ?? defaultTooth(selected)) : null;

  const summary = Object.values(teethMap).reduce((acc, t) => {
    acc[t.status] = (acc[t.status] ?? 0) + 1;
    return acc;
  }, {} as Record<number, number>);

  return (
    <div className="space-y-4">
      {/* Diş şeması */}
      <div className="rounded-xl border bg-muted/10 p-4 space-y-2">
        {/* Üst çene */}
        <p className="text-[10px] text-center text-muted-foreground uppercase tracking-wider font-medium">Üst Çene</p>
        <div className="flex justify-center gap-0.5 flex-wrap">
          {rows.upperRight.map(n => (
            <ToothSvg key={n} tooth={teethMap[n] ?? defaultTooth(n)} selected={selected === n}
              onClick={() => setSelected(selected === n ? null : n)} isUpper={true} />
          ))}
          <div className="w-px bg-border mx-1 self-stretch" />
          {rows.upperLeft.map(n => (
            <ToothSvg key={n} tooth={teethMap[n] ?? defaultTooth(n)} selected={selected === n}
              onClick={() => setSelected(selected === n ? null : n)} isUpper={true} />
          ))}
        </div>

        <div className="h-px bg-border my-1" />

        {/* Alt çene */}
        <div className="flex justify-center gap-0.5 flex-wrap">
          {rows.lowerRight.map(n => (
            <ToothSvg key={n} tooth={teethMap[n] ?? defaultTooth(n)} selected={selected === n}
              onClick={() => setSelected(selected === n ? null : n)} isUpper={false} />
          ))}
          <div className="w-px bg-border mx-1 self-stretch" />
          {rows.lowerLeft.map(n => (
            <ToothSvg key={n} tooth={teethMap[n] ?? defaultTooth(n)} selected={selected === n}
              onClick={() => setSelected(selected === n ? null : n)} isUpper={false} />
          ))}
        </div>
        <p className="text-[10px] text-center text-muted-foreground uppercase tracking-wider font-medium">Alt Çene</p>
      </div>

      {/* Seçili diş edit paneli */}
      {selectedTooth && (
        <ToothEditPanel
          tooth={selectedTooth}
          patientPublicId={patientPublicId}
          onSave={handleSave}
          onClose={() => setSelected(null)}
        />
      )}

      {/* Legend + Özet */}
      <div className="flex flex-wrap gap-1.5">
        {(Object.values(ToothStatus).filter(v => typeof v === 'number') as ToothStatus[]).map((s) => {
          const m = STATUS_META[s];
          const count = summary[s] ?? 0;
          return (
            <span
              key={s}
              className="text-[10px] px-2 py-0.5 rounded border flex items-center gap-1"
              style={{ backgroundColor: m.fill, borderColor: m.stroke, color: m.text }}
            >
              {m.label}
              {count > 0 && <span className="font-bold">{count}</span>}
            </span>
          );
        })}
      </div>
    </div>
  );
}

// ─── Tab: Protokol ────────────────────────────────────────────────────────────

function ProtokolTab({
  protocolPublicId,
  patientPublicId,
}: {
  protocolPublicId: string;
  patientPublicId: string;
}) {
  const qc = useQueryClient();

  // ── Hangi protokolü görüntülüyoruz ────────────────────────────────────────
  const [viewingId, setViewingId] = useState(protocolPublicId);
  const isCurrentProtocol = viewingId === protocolPublicId;

  // ── Protocol detail (görüntülenen) ────────────────────────────────────────
  const { data: detail, isLoading: detailLoading } = useQuery<ProtocolDetail>({
    queryKey: ['protocol-detail', viewingId],
    queryFn: () => protocolsApi.getDetail(viewingId).then((r) => r.data),
    staleTime: 30_000,
  });

  // Düzenlenebilir mi? Sadece mevcut açık protokol
  const isEditable = isCurrentProtocol && detail != null && detail.status === 1;

  // ── Patient history ────────────────────────────────────────────────────────
  const { data: history = [], isLoading: histLoading } = useQuery<ProtocolHistoryItem[]>({
    queryKey: ['protocol-history', patientPublicId],
    queryFn: () => protocolsApi.getPatientHistory(patientPublicId, 20).then((r) => r.data),
    staleTime: 60_000,
  });

  // ── Draft state (sadece mevcut protokol için) ──────────────────────────────
  const [chiefComplaint, setChiefComplaint]           = useState('');
  const [examinationFindings, setExaminationFindings] = useState('');
  const [treatmentPlan, setTreatmentPlan]             = useState('');
  const [initialized, setInitialized]                 = useState(false);

  useEffect(() => {
    if (detail && !initialized && isCurrentProtocol) {
      setChiefComplaint(detail.chiefComplaint ?? '');
      setExaminationFindings(detail.examinationFindings ?? '');
      setTreatmentPlan(detail.treatmentPlan ?? '');
      setInitialized(true);
    }
    if (!isCurrentProtocol && detail) {
      // Geçmiş protokolde form alanlarını detaydan doldur (read-only gösterim için)
      setChiefComplaint(detail.chiefComplaint ?? '');
      setExaminationFindings(detail.examinationFindings ?? '');
      setTreatmentPlan(detail.treatmentPlan ?? '');
    }
  }, [detail, viewingId]);

  // ── Save details ───────────────────────────────────────────────────────────
  const saveMutation = useMutation({
    mutationFn: () =>
      protocolsApi.updateDetails(protocolPublicId, {
        chiefComplaint:      chiefComplaint || null,
        examinationFindings: examinationFindings || null,
        treatmentPlan:       treatmentPlan || null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['protocol-detail', protocolPublicId] });
      toast.success('Protokol kaydedildi.');
    },
    onError: () => toast.error('Kayıt başarısız.'),
  });

  // ── ICD search ─────────────────────────────────────────────────────────────
  const [icdQuery, setIcdQuery]       = useState('');
  const [debouncedQ, setDebouncedQ]   = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(icdQuery), 300);
    return () => clearTimeout(t);
  }, [icdQuery]);

  const { data: icdResults = [] } = useQuery<IcdCode[]>({
    queryKey: ['icd-search', debouncedQ],
    queryFn: () => protocolsApi.searchIcd(debouncedQ, 1, 20).then((r) => r.data),
    enabled: debouncedQ.length >= 2,
    staleTime: 60_000,
  });

  // click-outside to close dropdown
  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  // ── Add / Remove diagnosis ─────────────────────────────────────────────────
  const addDxMutation = useMutation({
    mutationFn: (icdCode: IcdCode) => {
      const isPrimary = (detail?.diagnoses ?? []).length === 0;
      return protocolsApi.addDiagnosis(protocolPublicId, icdCode.id, isPrimary, null);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['protocol-detail', protocolPublicId] });
      setIcdQuery('');
      setShowDropdown(false);
    },
    onError: () => toast.error('Tanı eklenemedi.'),
  });

  const removeDxMutation = useMutation({
    mutationFn: (entryId: string) =>
      protocolsApi.removeDiagnosis(protocolPublicId, entryId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['protocol-detail', protocolPublicId] }),
    onError: () => toast.error('Tanı silinemedi.'),
  });

  const alreadyAdded = new Set((detail?.diagnoses ?? []).map((d) => d.icdCodeId));

  if (detailLoading) {
    return (
      <div className="space-y-3">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-[1fr_300px] gap-6 items-start">
      {/* ── Sol: Form ───────────────────────────────────────────────────────── */}
      <div className="space-y-4">

        {/* Geçmiş protokol banner */}
        {!isCurrentProtocol && detail && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 flex items-center justify-between gap-2">
            <div className="flex items-center gap-2 text-sm text-amber-800">
              <AlertTriangle className="size-4 text-amber-600 shrink-0" />
              <span>
                <span className="font-medium">{detail.protocolNo}</span> nolu kapalı protokol görüntülüyorsunuz — salt okunur.
              </span>
            </div>
            <Button
              size="sm"
              variant="outline"
              className="shrink-0 text-xs h-7"
              onClick={() => setViewingId(protocolPublicId)}
            >
              Mevcut Protokole Dön
            </Button>
          </div>
        )}

        {/* Şikayet */}
        <div className="space-y-1.5">
          <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Şikayet</Label>
          <Textarea
            rows={3}
            placeholder="Hastanın şikayetini yazın..."
            value={chiefComplaint}
            onChange={(e) => setChiefComplaint(e.target.value)}
            className="text-sm resize-none"
            disabled={!isEditable}
          />
        </div>

        {/* Fiziksel Muayene */}
        <div className="space-y-1.5">
          <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Fiziksel Muayene Bulguları</Label>
          <Textarea
            rows={3}
            placeholder="Muayene bulgularını yazın..."
            value={examinationFindings}
            onChange={(e) => setExaminationFindings(e.target.value)}
            className="text-sm resize-none"
            disabled={!isEditable}
          />
        </div>

        {/* Tanı (ICD) */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Tanı (ICD-10)
            </Label>
            {(detail?.diagnoses?.length ?? 0) > 0 && (
              <span className="text-xs text-muted-foreground">{detail!.diagnoses.length} tanı seçili</span>
            )}
          </div>

          {/* ICD search — sadece düzenlenebilir modda */}
          {isEditable && (
            <div className="relative" ref={dropdownRef}>
              <div className="relative">
                <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground pointer-events-none" />
                <Input
                  className="pl-8 text-sm h-9"
                  placeholder="Kod veya açıklama ara... (min. 2 karakter)"
                  value={icdQuery}
                  onChange={(e) => {
                    setIcdQuery(e.target.value);
                    setShowDropdown(true);
                  }}
                  onFocus={() => icdQuery.length >= 2 && setShowDropdown(true)}
                />
              </div>

              {showDropdown && icdResults.length > 0 && (
                <div className="absolute z-50 w-full mt-1 rounded-md border bg-popover shadow-lg max-h-56 overflow-y-auto">
                  {icdResults.map((icd) => {
                    const added = alreadyAdded.has(icd.id);
                    return (
                      <button
                        key={icd.id}
                        className={cn(
                          'w-full text-left px-3 py-2 text-sm flex items-center gap-3 hover:bg-accent transition-colors',
                          added && 'opacity-50 cursor-not-allowed',
                        )}
                        disabled={added || addDxMutation.isPending}
                        onClick={() => !added && addDxMutation.mutate(icd)}
                      >
                        <span className="font-mono text-xs shrink-0 text-primary">{icd.code}</span>
                        <span className="flex-1 truncate text-foreground">{icd.description}</span>
                        {added && <span className="text-xs text-muted-foreground shrink-0">Eklendi</span>}
                      </button>
                    );
                  })}
                </div>
              )}
              {showDropdown && debouncedQ.length >= 2 && icdResults.length === 0 && (
                <div className="absolute z-50 w-full mt-1 rounded-md border bg-popover shadow-md px-3 py-4 text-center text-sm text-muted-foreground">
                  Sonuç bulunamadı
                </div>
              )}
            </div>
          )}

          {/* Diagnoses table */}
          {(detail?.diagnoses?.length ?? 0) > 0 && (
            <div className="rounded-md border overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/50 text-xs text-muted-foreground">
                    <th className="text-left px-3 py-2 font-medium w-20">Kod</th>
                    <th className="text-left px-3 py-2 font-medium">Açıklama</th>
                    <th className="text-center px-2 py-2 font-medium w-16">Birincil</th>
                    {isEditable && <th className="px-2 py-2 w-10" />}
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {detail!.diagnoses.map((dx: ProtocolDiagnosis) => (
                    <tr key={dx.publicId} className="hover:bg-muted/30">
                      <td className="px-3 py-2 font-mono text-xs text-primary font-medium">{dx.code}</td>
                      <td className="px-3 py-2 text-xs">{dx.description}</td>
                      <td className="px-2 py-2 text-center">
                        {dx.isPrimary && (
                          <span className="text-[10px] bg-primary/10 text-primary px-1.5 py-0.5 rounded-full font-medium">Ana</span>
                        )}
                      </td>
                      {isEditable && (
                        <td className="px-2 py-2 text-right">
                          <button
                            className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors"
                            onClick={() => removeDxMutation.mutate(dx.publicId)}
                            disabled={removeDxMutation.isPending}
                          >
                            <Trash2 className="size-3.5" />
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Tedavi Planı */}
        <div className="space-y-1.5">
          <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Tedavi / Bakım Planı</Label>
          <Textarea
            rows={3}
            placeholder="Tedavi planını yazın..."
            value={treatmentPlan}
            onChange={(e) => setTreatmentPlan(e.target.value)}
            className="text-sm resize-none"
            disabled={!isEditable}
          />
        </div>

        {isEditable && (
          <Button
            className="w-full gap-1.5"
            onClick={() => saveMutation.mutate()}
            disabled={saveMutation.isPending}
          >
            <Save className="size-4" />
            {saveMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </Button>
        )}
      </div>

      {/* ── Sağ: Protokol Geçmişi ──────────────────────────────────────────── */}
      <div className="space-y-2">
        <div className="flex items-center gap-1.5">
          <History className="size-3.5 text-muted-foreground" />
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Protokol Geçmişi</p>
        </div>

        {histLoading ? (
          <div className="space-y-2">
            {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
          </div>
        ) : history.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-8 text-muted-foreground rounded-lg border border-dashed">
            <FileText className="size-6 opacity-30" />
            <p className="text-xs">Geçmiş protokol yok</p>
          </div>
        ) : (
          <div className="space-y-2">
            {[...history].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).map((h) => {
              const isCurrent = h.publicId === protocolPublicId;
              const isViewing = h.publicId === viewingId;
              const isOpen    = h.status === 1 || h.status === 2;
              return (
                <button
                  key={h.publicId}
                  className={cn(
                    'w-full text-left rounded-lg border p-3 space-y-1.5 text-xs transition-colors hover:bg-accent',
                    isViewing
                      ? 'border-primary/50 bg-primary/5 ring-1 ring-primary/20'
                      : 'border-border',
                  )}
                  onClick={() => setViewingId(h.publicId)}
                >
                  <div className="flex items-center justify-between gap-1">
                    <span className="font-mono text-primary font-medium">{h.protocolNo}</span>
                    <div className="flex items-center gap-1 shrink-0">
                      {isCurrent && (
                        <span className="text-[10px] bg-green-100 text-green-700 px-1.5 py-0.5 rounded-full font-medium">Mevcut</span>
                      )}
                      {isViewing && (
                        <span className="text-[10px] bg-primary/10 text-primary px-1.5 py-0.5 rounded-full font-medium">Görüntüleniyor</span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-1 text-muted-foreground">
                    <span>{format(new Date(h.createdAt), 'd MMM yyyy', { locale: tr })}</span>
                    <span>·</span>
                    <span className="truncate">{h.doctorName}</span>
                  </div>
                  <div className="flex items-center gap-1.5 flex-wrap">
                    <span className={cn(
                      'px-1.5 py-0.5 rounded-full text-[10px] font-medium',
                      h.status === 3 ? 'bg-emerald-100 text-emerald-700'
                      : h.status === 4 ? 'bg-red-100 text-red-700'
                      : 'bg-amber-100 text-amber-700',
                    )}>
                      {h.statusName}
                    </span>
                    <span className="text-muted-foreground">{h.protocolTypeName}</span>
                    {!isOpen && (
                      <span className="text-[10px] text-muted-foreground flex items-center gap-0.5">
                        <Lock className="size-2.5" /> Salt okunur
                      </span>
                    )}
                  </div>
                  {h.chiefComplaint && (
                    <p className="text-muted-foreground truncate" title={h.chiefComplaint}>{h.chiefComplaint}</p>
                  )}
                </button>
              );
            })}
          </div>
        )}
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
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  return (
    <Dialog open onOpenChange={(open) => { if (!open) onCancel(); }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Protokolü Kapat</DialogTitle>
        </DialogHeader>
        <div className="py-2">
          <div className="rounded-lg bg-muted/50 px-3 py-2 text-sm">
            <span className="font-medium">{protocol.patientName}</span>
            <span className="text-muted-foreground ml-2 text-xs">{protocol.protocolNo}</span>
          </div>
          <p className="text-sm text-muted-foreground mt-3">
            Protokol kapatılacak. Seçilen ICD tanıları kaydedilmiş durumda, tekrar girmenize gerek yok.
          </p>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isPending}>
            İptal
          </Button>
          <Button onClick={onConfirm} disabled={isPending}>
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
    mutationFn: () => protocolsApi.complete(publicId!),
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
    { value: 'protokol',        label: 'Protokol',        icon: FileText },
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
          <TabsContent value="hasta-bilgileri" className="mt-0">
            <div className="max-w-2xl mx-auto p-4">
              {protocol?.patientPublicId ? (
                <PatientInfoTab patientPublicId={protocol.patientPublicId} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="anamnez" className="mt-0">
            <div className="max-w-5xl mx-auto p-4">
              {protocol?.patientPublicId ? (
                <AnamnezTab patientPublicId={protocol.patientPublicId} protocolPublicId={protocol.publicId} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="oral-diagnoz" className="mt-0">
            <div className="max-w-2xl mx-auto p-4">
              {protocol?.patientPublicId ? (
                <OralDiagnozTab patientPublicId={protocol.patientPublicId} />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="protokol" className="mt-0">
            <div className="max-w-5xl mx-auto p-4">
              {protocol?.patientPublicId && publicId ? (
                <ProtokolTab
                  protocolPublicId={publicId}
                  patientPublicId={protocol.patientPublicId}
                />
              ) : (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
              )}
            </div>
          </TabsContent>
        </div>
      </Tabs>

      {/* ── Close dialog ────────────────────────────────────────── */}
      {closeOpen && protocol && (
        <CloseProtocolDialog
          protocol={protocol}
          onConfirm={() => completeMutation.mutate()}
          onCancel={() => setCloseOpen(false)}
          isPending={completeMutation.isPending}
        />
      )}
    </div>
  );
}
