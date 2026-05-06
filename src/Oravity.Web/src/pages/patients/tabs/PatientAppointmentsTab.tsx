import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { CalendarDays, Clock, UserRound, AlertCircle, Building2, FileText } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { appointmentsApi } from '@/api/appointments';

const STATUS_COLORS: Record<number, string> = {
  1: 'bg-blue-100 text-blue-700 border-blue-200',
  2: 'bg-indigo-100 text-indigo-700 border-indigo-200',
  3: 'bg-amber-100 text-amber-700 border-amber-200',
  5: 'bg-sky-100 text-sky-700 border-sky-200',
  4: 'bg-slate-100 text-slate-600 border-slate-200',
  6: 'bg-red-100 text-red-600 border-red-200',
  7: 'bg-green-100 text-green-700 border-green-200',
  8: 'bg-rose-100 text-rose-700 border-rose-200',
};

const STATUS_DOT: Record<number, string> = {
  1: 'bg-blue-400',
  2: 'bg-indigo-400',
  3: 'bg-amber-400',
  5: 'bg-sky-400',
  4: 'bg-slate-400',
  6: 'bg-red-400',
  7: 'bg-green-500',
  8: 'bg-rose-400',
};

export function PatientAppointmentsTab({ patientPublicId }: { patientPublicId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['patient-appointments', patientPublicId],
    queryFn: () => appointmentsApi.getByPatient(patientPublicId).then(r => r.data),
    staleTime: 30_000,
    enabled: !!patientPublicId,
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }

  const appointments = data?.items ?? [];

  if (appointments.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-12 text-muted-foreground">
        <CalendarDays className="size-8 opacity-30" />
        <p className="text-sm">Henüz randevu kaydı yok</p>
      </div>
    );
  }

  // Group by year
  const byYear = appointments.reduce<Record<string, typeof appointments>>((acc, apt) => {
    const year = new Date(apt.startTime).getFullYear().toString();
    if (!acc[year]) acc[year] = [];
    acc[year].push(apt);
    return acc;
  }, {});

  const years = Object.keys(byYear).sort((a, b) => Number(b) - Number(a));

  return (
    <div className="space-y-6">
      <p className="text-sm text-muted-foreground">{data?.total ?? appointments.length} randevu</p>

      {years.map(year => (
        <div key={year}>
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">{year}</p>
          <div className="relative pl-4">
            <div className="absolute left-1.5 top-0 bottom-0 w-px bg-border" />
            <div className="space-y-3">
              {byYear[year].map(apt => (
                <div key={apt.publicId} className="relative">
                  <div className={cn('absolute -left-[11px] top-3 size-2.5 rounded-full border-2 border-background', STATUS_DOT[apt.statusId] ?? 'bg-slate-400')} />
                  <div className="rounded-lg border bg-background p-3 space-y-2 ml-2">
                    {/* Header row */}
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-semibold">
                        {format(new Date(apt.startTime), 'd MMMM yyyy', { locale: tr })}
                      </span>
                      <span className="flex items-center gap-1 text-xs text-muted-foreground">
                        <Clock className="size-3" />
                        {format(new Date(apt.startTime), 'HH:mm')}–{format(new Date(apt.endTime), 'HH:mm')}
                      </span>
                      <span className={cn(
                        'text-[10px] px-1.5 py-0.5 rounded-full font-medium border ml-auto',
                        STATUS_COLORS[apt.statusId] ?? 'bg-slate-100 text-slate-600 border-slate-200'
                      )}>
                        {apt.statusLabel}
                      </span>
                    </div>

                    {/* Detail rows */}
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-muted-foreground">
                      {apt.doctorName && (
                        <span className="flex items-center gap-1">
                          <UserRound className="size-3 shrink-0" />
                          {apt.doctorName}
                        </span>
                      )}
                      {apt.branchName && (
                        <span className="flex items-center gap-1">
                          <Building2 className="size-3 shrink-0" />
                          {apt.branchName}
                        </span>
                      )}
                      {apt.appointmentTypeName && (
                        <span className="flex items-center gap-1 col-span-2">
                          <CalendarDays className="size-3 shrink-0" />
                          {apt.appointmentTypeName}
                        </span>
                      )}
                      {apt.isUrgent && (
                        <span className="flex items-center gap-1 text-red-600 font-medium">
                          <AlertCircle className="size-3 shrink-0" />Acil randevu
                        </span>
                      )}
                    </div>

                    {apt.notes && (
                      <div className="flex items-start gap-1 text-xs text-muted-foreground border-t pt-1.5">
                        <FileText className="size-3 shrink-0 mt-0.5" />
                        <span className="line-clamp-2">{apt.notes}</span>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
