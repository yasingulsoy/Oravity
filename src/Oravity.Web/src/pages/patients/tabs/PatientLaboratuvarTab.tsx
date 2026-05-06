import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { FlaskConical } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { laboratoriesApi } from '@/api/laboratories';
import type { LabWorkStatus } from '@/api/laboratories';

const LAB_TERMINAL: LabWorkStatus[] = ['completed', 'approved', 'rejected', 'cancelled'];

const WORK_TYPE_LABEL: Record<string, string> = {
  prosthetic: 'Protetik', orthodontic: 'Ortodontik', implant: 'İmplant', other: 'Diğer',
};

const LAB_STATUS_LABELS: Record<LabWorkStatus, string> = {
  pending:     'Bekliyor',
  sent:        'Gönderildi',
  in_progress: 'Üretimde',
  ready:       'Hazır',
  received:    'Teslim Alındı',
  fitted:      'Denendi',
  completed:   'Tamamlandı',
  approved:    'Onaylandı',
  rejected:    'Reddedildi',
  cancelled:   'İptal',
};

const LAB_STATUS_COLOR: Record<LabWorkStatus, string> = {
  pending:     'bg-slate-100 text-slate-600',
  sent:        'bg-blue-100 text-blue-700',
  in_progress: 'bg-amber-100 text-amber-700',
  ready:       'bg-emerald-100 text-emerald-700',
  received:    'bg-teal-100 text-teal-700',
  fitted:      'bg-purple-100 text-purple-700',
  completed:   'bg-green-100 text-green-700',
  approved:    'bg-green-100 text-green-800',
  rejected:    'bg-red-100 text-red-700',
  cancelled:   'bg-slate-100 text-slate-500',
};

export function PatientLaboratuvarTab({ patientPublicId }: { patientPublicId: string }) {
  const [showAll, setShowAll] = useState(false);

  const { data: labWorksData, isLoading } = useQuery({
    queryKey: ['lab-works-patient', patientPublicId],
    queryFn: () => laboratoriesApi.listWorks({ patientPublicId, pageSize: 200 }).then(r => r.data),
    staleTime: 30_000,
    enabled: !!patientPublicId,
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }

  const allWorks = labWorksData?.items ?? [];
  const activeWorks = allWorks.filter(w => !LAB_TERMINAL.includes(w.status));
  const terminalWorks = allWorks.filter(w => LAB_TERMINAL.includes(w.status));
  const displayed = showAll ? allWorks : activeWorks;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          {activeWorks.length} aktif
          {terminalWorks.length > 0 && `, ${terminalWorks.length} tamamlanan`}
        </p>
        {terminalWorks.length > 0 && (
          <Button size="sm" variant="ghost" className="text-xs" onClick={() => setShowAll(v => !v)}>
            {showAll ? 'Sadece Aktif' : 'Tümünü Göster'}
          </Button>
        )}
      </div>

      {displayed.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
          <FlaskConical className="size-8 opacity-30" />
          <p className="text-sm">Bekleyen lab işlemi yok</p>
        </div>
      ) : (
        <div className="space-y-2">
          {displayed.map(w => (
            <div
              key={w.publicId}
              className={cn(
                'rounded-lg border bg-background p-3',
                LAB_TERMINAL.includes(w.status) && 'opacity-60',
              )}
            >
              <div className="flex items-start gap-3">
                <FlaskConical className="size-4 text-muted-foreground mt-0.5 shrink-0" />
                <div className="flex-1 min-w-0 space-y-0.5">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-semibold">{w.laboratoryName}</span>
                    <span className="text-xs text-muted-foreground font-mono">{w.workNo}</span>
                    <span className={cn('text-[10px] px-1.5 py-0.5 rounded-full font-medium ml-auto', LAB_STATUS_COLOR[w.status])}>
                      {LAB_STATUS_LABELS[w.status]}
                    </span>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground flex-wrap">
                    {w.workType && <span>{WORK_TYPE_LABEL[w.workType] ?? w.workType}</span>}
                    {w.toothNumbers && <span>Diş: {w.toothNumbers}</span>}
                    {w.shadeColor && <span>Renk: {w.shadeColor}</span>}
                  </div>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground flex-wrap">
                    <span>Açıldı: {format(new Date(w.createdAt), 'd MMM yyyy', { locale: tr })}</span>
                    {w.estimatedDeliveryDate && (
                      <span>Tahmini: {format(new Date(w.estimatedDeliveryDate), 'd MMM yyyy', { locale: tr })}</span>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
