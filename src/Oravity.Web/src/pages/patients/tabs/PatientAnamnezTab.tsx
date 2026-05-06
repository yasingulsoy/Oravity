import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { AlertTriangle, ClipboardList, Info } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { patientsApi } from '@/api/patients';
import type { PatientAnamnesis } from '@/types/patient';

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
  { key: 'hasAspirinAllergy', label: 'Aspirin Alerjisi' },
  { key: 'hasLatexAllergy',   label: 'Lateks Alerjisi' },
];

function FlagRow({ label, active }: { label: string; active: boolean }) {
  if (!active) return null;
  return (
    <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-red-100 text-red-700 border border-red-200">
      <AlertTriangle className="size-3" />
      {label}
    </span>
  );
}

export function PatientAnamnezTab({ patientPublicId }: { patientPublicId: string }) {
  const { data: anamnesis, isLoading } = useQuery<PatientAnamnesis | null>({
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

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
      </div>
    );
  }

  if (!anamnesis) {
    return (
      <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
        <ClipboardList className="size-8 opacity-30" />
        <p className="text-sm">Anamnez formu henüz doldurulmamış</p>
      </div>
    );
  }

  const criticalActive = CRITICAL_FLAGS.filter(f => anamnesis[f.key] === true);
  const systemicActive = SYSTEMIC_FLAGS.filter(f => anamnesis[f.key] === true);
  const allergyActive  = ALLERGY_FLAGS.filter(f => anamnesis[f.key] === true);

  return (
    <div className="space-y-5">
      {/* Read-only uyarısı */}
      <div className="flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 dark:bg-blue-950/20 px-3 py-2 text-xs text-blue-700">
        <Info className="size-3.5 mt-0.5 shrink-0" />
        Anamnezi güncellemek için muayene protokolü açmanız gerekmektedir.
      </div>

      {/* Kritik uyarılar */}
      {criticalActive.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Kritik Uyarılar</p>
          <div className="flex flex-wrap gap-1.5">
            {criticalActive.map(f => <FlagRow key={f.key} label={f.label} active />)}
          </div>
        </div>
      )}

      {/* Sistemik hastalıklar */}
      {systemicActive.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Sistemik Hastalıklar</p>
          <div className="flex flex-wrap gap-1.5">
            {systemicActive.map(f => (
              <span key={f.key} className="text-xs px-2 py-0.5 rounded-full bg-amber-100 text-amber-700 border border-amber-200">
                {f.label}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Alerjiler */}
      {allergyActive.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Alerjiler</p>
          <div className="flex flex-wrap gap-1.5">
            {allergyActive.map(f => (
              <span key={f.key} className="text-xs px-2 py-0.5 rounded-full bg-orange-100 text-orange-700 border border-orange-200">
                {f.label}
              </span>
            ))}
            {anamnesis.otherAllergies && (
              <span className="text-xs px-2 py-0.5 rounded-full bg-orange-100 text-orange-700 border border-orange-200">
                {anamnesis.otherAllergies}
              </span>
            )}
          </div>
        </div>
      )}

      {/* Genel */}
      <div>
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Genel</p>
        <div className="rounded-lg border divide-y text-sm">
          {anamnesis.bloodType && (
            <div className="flex items-center justify-between px-3 py-2">
              <span className="text-muted-foreground">Kan Grubu</span>
              <span className="font-medium">{anamnesis.bloodType}</span>
            </div>
          )}
          {anamnesis.smokingStatus != null && (
            <div className="flex items-center justify-between px-3 py-2">
              <span className="text-muted-foreground">Sigara</span>
              <span className={cn('font-medium', anamnesis.smokingStatus === 1 && 'text-orange-600')}>
                {anamnesis.smokingStatus === 0 ? 'Kullanmıyor' : anamnesis.smokingStatus === 1 ? `Kullanıyor${anamnesis.smokingAmount ? ` (${anamnesis.smokingAmount})` : ''}` : 'Bıraktı'}
              </span>
            </div>
          )}
          {anamnesis.isPregnant && (
            <div className="flex items-center justify-between px-3 py-2">
              <span className="text-muted-foreground">Gebelik</span>
              <span className="font-medium text-amber-600">Gebe</span>
            </div>
          )}
          {anamnesis.isBreastfeeding && (
            <div className="flex items-center justify-between px-3 py-2">
              <span className="text-muted-foreground">Emzirme</span>
              <span className="font-medium text-amber-600">Emziriyor</span>
            </div>
          )}
        </div>
      </div>

      {anamnesis.additionalNotes && (
        <div>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">Ek Notlar</p>
          <div className="rounded-lg border p-3 text-sm text-muted-foreground">{anamnesis.additionalNotes}</div>
        </div>
      )}

      <p className="text-[11px] text-muted-foreground">
        Son güncelleme: {format(new Date(anamnesis.filledAt), 'd MMM yyyy HH:mm', { locale: tr })} · {anamnesis.filledByName}
      </p>
    </div>
  );
}
