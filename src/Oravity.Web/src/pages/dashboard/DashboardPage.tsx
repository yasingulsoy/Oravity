import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/store/authStore';
import { reportsApi } from '@/api/reports';
import { appointmentsApi } from '@/api/appointments';
import { cn } from '@/lib/utils';
import type { Appointment, AppointmentStatus } from '@/types/appointment';

// ─── Yardımcılar ─────────────────────────────────────────────────────────

const currencyFmt = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
  maximumFractionDigits: 0,
});

const numberFmt = new Intl.NumberFormat('tr-TR');

function toLocalDateString(d: Date) {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatWeekdayShort(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { weekday: 'short' });
}

function greeting() {
  const h = new Date().getHours();
  if (h < 6) return 'İyi geceler';
  if (h < 12) return 'Günaydın';
  if (h < 18) return 'İyi günler';
  return 'İyi akşamlar';
}

function todayLong() {
  return new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}

// ─── KPI Card (sade, ikonsuz) ────────────────────────────────────────────

interface KpiCardProps {
  title: string;
  value: string;
  hint?: string;
  to?: string;
  loading?: boolean;
}

function KpiCard({ title, value, hint, to, loading }: KpiCardProps) {
  const inner = (
    <Card
      className={cn(
        'border-0 bg-card/80 shadow-sm ring-1 ring-border/60 transition-colors',
        to && 'hover:bg-card hover:ring-border',
      )}
    >
      <CardContent className="space-y-3 p-5">
        <p className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
          {title}
        </p>
        <div className="font-heading text-[2rem] font-semibold tabular-nums leading-none tracking-tight text-foreground">
          {loading ? (
            <span className="inline-block h-7 w-20 animate-pulse rounded-md bg-muted" />
          ) : (
            value
          )}
        </div>
        <p className="min-h-[1rem] text-xs text-muted-foreground">{hint ?? '\u00A0'}</p>
      </CardContent>
    </Card>
  );

  return to ? (
    <Link to={to} className="block">
      {inner}
    </Link>
  ) : (
    inner
  );
}

// ─── Durum chip ──────────────────────────────────────────────────────────

function StatusChip({
  status,
  label,
}: {
  status?: AppointmentStatus;
  label: string;
}) {
  const style = status
    ? {
        backgroundColor: status.containerColor,
        color: status.textColor,
        borderColor: status.borderColor,
      }
    : undefined;

  return (
    <span
      style={style}
      className="inline-block whitespace-nowrap rounded-full border px-2 py-0.5 text-[10.5px] font-medium leading-none"
    >
      {label}
    </span>
  );
}

// ─── Bugünkü Program ─────────────────────────────────────────────────────

function TodaySchedule({
  appointments,
  statuses,
  isLoading,
}: {
  appointments: Appointment[] | undefined;
  statuses: AppointmentStatus[] | undefined;
  isLoading: boolean;
}) {
  const sorted = useMemo(
    () => (appointments ?? []).slice().sort((a, b) => a.startTime.localeCompare(b.startTime)),
    [appointments],
  );

  const statusMap = useMemo(() => {
    const m = new Map<number, AppointmentStatus>();
    (statuses ?? []).forEach((s) => m.set(s.id, s));
    return m;
  }, [statuses]);

  const now = new Date();
  const upcoming = sorted.filter((a) => new Date(a.startTime) >= now);
  const nextId = upcoming[0]?.publicId;

  return (
    <Card className="border-0 bg-card/80 shadow-sm ring-1 ring-border/60">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 border-b border-border/60 px-5 py-4">
        <div>
          <CardTitle className="font-heading text-base font-semibold">
            Bugünkü Randevular
          </CardTitle>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {isLoading
              ? 'Yükleniyor…'
              : `${sorted.length} randevu · ${upcoming.length} yaklaşan`}
          </p>
        </div>
        <Link
          to="/appointments"
          className="text-xs font-medium text-muted-foreground underline-offset-4 transition-colors hover:text-foreground hover:underline"
        >
          Takvimi aç
        </Link>
      </CardHeader>

      <CardContent className="p-0">
        {isLoading ? (
          <div className="flex h-[240px] items-center justify-center text-xs text-muted-foreground">
            Yükleniyor…
          </div>
        ) : sorted.length === 0 ? (
          <div className="flex h-[240px] flex-col items-center justify-center gap-3 px-6 text-center">
            <p className="text-sm font-medium text-foreground">
              Bugün için randevu bulunmuyor
            </p>
            <p className="max-w-[280px] text-xs leading-relaxed text-muted-foreground">
              Takvime yeni randevu eklemek için aşağıdaki butonu kullanabilirsiniz.
            </p>
            <Button asChild size="sm" variant="outline" className="mt-1">
              <Link to="/appointments">Yeni Randevu</Link>
            </Button>
          </div>
        ) : (
          <ul className="divide-y divide-border/50">
            {sorted.slice(0, 10).map((a) => {
              const status = statusMap.get(a.statusId);
              const isPast = new Date(a.startTime) < now;
              const isNext = nextId === a.publicId;

              return (
                <li
                  key={a.publicId}
                  className={cn(
                    'flex items-center gap-4 px-5 py-3 transition-colors hover:bg-muted/40',
                    isNext && 'bg-primary/[0.04]',
                  )}
                >
                  {/* Saat */}
                  <div className="flex w-[52px] shrink-0 flex-col">
                    <span className="font-heading text-[13px] font-semibold tabular-nums leading-none text-foreground">
                      {formatTime(a.startTime)}
                    </span>
                    <span className="mt-1 text-[10px] leading-none text-muted-foreground">
                      {formatTime(a.endTime)}
                    </span>
                  </div>

                  {/* Dikey çizgi */}
                  <span
                    aria-hidden
                    className={cn(
                      'h-8 w-[2px] shrink-0 rounded-full',
                      isNext ? 'bg-primary' : isPast ? 'bg-border/60' : 'bg-border',
                    )}
                  />

                  {/* Hasta + doktor */}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p
                        className={cn(
                          'truncate text-[13.5px] font-medium',
                          isPast ? 'text-muted-foreground' : 'text-foreground',
                        )}
                      >
                        {a.patientName ?? 'Bilinmeyen hasta'}
                      </p>
                      {isNext && (
                        <span className="rounded-full bg-primary/10 px-1.5 py-0.5 text-[9.5px] font-semibold uppercase tracking-wide text-primary">
                          Sıradaki
                        </span>
                      )}
                      {a.isUrgent && (
                        <span className="rounded bg-destructive/10 px-1.5 py-0.5 text-[9.5px] font-semibold uppercase text-destructive">
                          Acil
                        </span>
                      )}
                    </div>
                    <p className="mt-0.5 truncate text-[11.5px] text-muted-foreground">
                      {a.doctorName ?? '—'}
                      {a.appointmentTypeName && (
                        <>
                          <span className="mx-1.5 opacity-50">·</span>
                          {a.appointmentTypeName}
                        </>
                      )}
                    </p>
                  </div>

                  <StatusChip status={status} label={a.statusLabel} />
                </li>
              );
            })}
            {sorted.length > 10 && (
              <li className="px-5 py-3 text-center text-xs text-muted-foreground">
                +{sorted.length - 10} randevu daha ·{' '}
                <Link
                  to="/appointments"
                  className="font-medium text-foreground underline-offset-2 hover:underline"
                >
                  tümünü gör
                </Link>
              </li>
            )}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Gelir Grafiği (sade bar) ────────────────────────────────────────────

function RevenueChart({
  days,
  isLoading,
}: {
  days: { date: string; total: number }[];
  isLoading: boolean;
}) {
  const max = Math.max(1, ...days.map((d) => d.total));
  const sum = days.reduce((acc, d) => acc + d.total, 0);

  const trend = useMemo(() => {
    if (days.length < 6) return 0;
    const recent = days.slice(-3).reduce((s, d) => s + d.total, 0);
    const past = days.slice(-6, -3).reduce((s, d) => s + d.total, 0);
    if (past === 0) return recent > 0 ? 100 : 0;
    return Math.round(((recent - past) / past) * 100);
  }, [days]);

  return (
    <Card className="border-0 bg-card/80 shadow-sm ring-1 ring-border/60">
      <CardHeader className="flex flex-row items-end justify-between space-y-0 px-5 pb-2 pt-4">
        <div>
          <CardTitle className="font-heading text-sm font-semibold">
            Son 7 Gün Gelir
          </CardTitle>
          <p className="mt-1 font-heading text-xl font-semibold tabular-nums leading-none">
            {isLoading ? '—' : currencyFmt.format(sum)}
          </p>
        </div>
        {!isLoading && sum > 0 && (
          <span
            className={cn(
              'text-[11px] font-medium tabular-nums',
              trend > 0 && 'text-emerald-600 dark:text-emerald-400',
              trend < 0 && 'text-rose-600 dark:text-rose-400',
              trend === 0 && 'text-muted-foreground',
            )}
          >
            {trend > 0 && '+'}
            {trend}%
          </span>
        )}
      </CardHeader>

      <CardContent className="px-5 pb-5 pt-3">
        {isLoading ? (
          <div className="h-[120px] animate-pulse rounded-md bg-muted/40" />
        ) : sum === 0 ? (
          <div className="flex h-[120px] items-center justify-center text-xs text-muted-foreground">
            Son 7 gün için tahsilat bulunmuyor
          </div>
        ) : (
          <div className="flex h-[120px] items-end gap-1.5">
            {days.map((d) => {
              const pct = (d.total / max) * 100;
              const isToday = d.date === toLocalDateString(new Date());
              return (
                <div key={d.date} className="group flex flex-1 flex-col items-center gap-2">
                  <div className="relative flex w-full flex-1 items-end">
                    <div
                      className={cn(
                        'w-full rounded-sm transition-all duration-200',
                        isToday ? 'bg-foreground' : 'bg-foreground/25 group-hover:bg-foreground/50',
                      )}
                      style={{ height: `${Math.max(pct, 2)}%` }}
                    />
                    <span className="pointer-events-none absolute -top-7 left-1/2 -translate-x-1/2 whitespace-nowrap rounded-md border border-border bg-popover px-1.5 py-0.5 text-[10px] font-medium tabular-nums text-popover-foreground opacity-0 shadow-sm transition-opacity group-hover:opacity-100">
                      {currencyFmt.format(d.total)}
                    </span>
                  </div>
                  <span
                    className={cn(
                      'text-[10px] leading-none',
                      isToday ? 'font-semibold text-foreground' : 'text-muted-foreground',
                    )}
                  >
                    {formatWeekdayShort(d.date)}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Durum Dağılımı ──────────────────────────────────────────────────────

function StatusBreakdown({
  summary,
  isLoading,
}: {
  summary:
    | { total: number; completed: number; pending: number; noShow: number; cancelled: number }
    | undefined;
  isLoading: boolean;
}) {
  const segments = summary
    ? [
        { label: 'Tamamlandı', count: summary.completed, color: 'bg-emerald-500' },
        { label: 'Bekliyor',   count: summary.pending,   color: 'bg-sky-500' },
        { label: 'Gelmedi',    count: summary.noShow,    color: 'bg-rose-500' },
        { label: 'İptal',      count: summary.cancelled, color: 'bg-muted-foreground/50' },
      ]
    : [];

  const total = summary?.total ?? 0;

  return (
    <Card className="border-0 bg-card/80 shadow-sm ring-1 ring-border/60">
      <CardHeader className="px-5 pb-2 pt-4">
        <CardTitle className="font-heading text-sm font-semibold">
          Randevu Durumu
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 px-5 pb-5">
        {isLoading ? (
          <div className="h-2 animate-pulse rounded-full bg-muted" />
        ) : total === 0 ? (
          <p className="text-xs text-muted-foreground">
            Bugün için henüz randevu yok.
          </p>
        ) : (
          <>
            <div className="flex h-1.5 overflow-hidden rounded-full bg-muted">
              {segments.map((s) =>
                s.count > 0 ? (
                  <div
                    key={s.label}
                    className={cn('h-full', s.color)}
                    style={{ width: `${(s.count / total) * 100}%` }}
                    title={`${s.label}: ${s.count}`}
                  />
                ) : null,
              )}
            </div>
            <ul className="space-y-1.5">
              {segments.map((s) => (
                <li
                  key={s.label}
                  className="flex items-center justify-between text-xs text-muted-foreground"
                >
                  <span className="flex items-center gap-2">
                    <span className={cn('h-1.5 w-1.5 rounded-full', s.color)} />
                    {s.label}
                  </span>
                  <span className="font-semibold tabular-nums text-foreground">
                    {s.count}
                  </span>
                </li>
              ))}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Sayfa ───────────────────────────────────────────────────────────────

export function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const firstName = user?.name?.split(' ')[0] ?? 'Kullanıcı';

  const today = useMemo(() => toLocalDateString(new Date()), []);
  const weekAgo = useMemo(() => {
    const d = new Date();
    d.setDate(d.getDate() - 6);
    return toLocalDateString(d);
  }, []);

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => reportsApi.dashboard().then((r) => r.data),
    staleTime: 60 * 1000,
  });

  const { data: todayAppts, isLoading: apptsLoading } = useQuery({
    queryKey: ['dashboard', 'appts', today],
    queryFn: () => appointmentsApi.getByDate(today).then((r) => r.data),
    staleTime: 60 * 1000,
  });

  const { data: statuses } = useQuery({
    queryKey: ['appointment-statuses'],
    queryFn: () => appointmentsApi.getStatuses().then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

  const { data: weekly, isLoading: revenueLoading } = useQuery({
    queryKey: ['dashboard', 'revenue', weekAgo, today],
    queryFn: () => reportsApi.dailyRevenue(weekAgo, today).then((r) => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const dailyRevenue = useMemo(() => {
    const map = new Map<string, number>();
    (weekly?.byDay ?? []).forEach((d) => map.set(d.date.slice(0, 10), d.total));
    const out: { date: string; total: number }[] = [];
    for (let i = 6; i >= 0; i--) {
      const d = new Date();
      d.setDate(d.getDate() - i);
      const key = toLocalDateString(d);
      out.push({ date: key, total: map.get(key) ?? 0 });
    }
    return out;
  }, [weekly]);

  const appts = summary?.appointments;
  const rev = summary?.revenue;

  const paymentCount = rev?.byMethod?.reduce((s, m) => s + m.count, 0) ?? 0;

  return (
    <div className="mx-auto max-w-[1280px] space-y-8">
      {/* Header */}
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
            {todayLong()}
          </p>
          <h1 className="mt-1.5 font-heading text-2xl font-semibold tracking-tight text-foreground sm:text-[1.75rem]">
            {greeting()}, {firstName}
          </h1>
        </div>
        <div className="flex items-center gap-2">
          <Button asChild variant="outline" size="sm">
            <Link to="/doctor">Hekim Ekranı</Link>
          </Button>
          <Button asChild size="sm">
            <Link to="/appointments">Yeni Randevu</Link>
          </Button>
        </div>
      </header>

      {/* KPI */}
      <section aria-labelledby="kpi-heading">
        <h2 id="kpi-heading" className="sr-only">
          Özet metrikler
        </h2>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <KpiCard
            title="Bugünkü Randevular"
            value={summaryLoading ? '—' : numberFmt.format(appts?.total ?? 0)}
            loading={summaryLoading}
            to="/appointments"
            hint={
              appts && appts.total > 0
                ? `${appts.completed} tamamlandı · ${appts.pending} bekliyor${
                    appts.noShow > 0 ? ` · ${appts.noShow} gelmedi` : ''
                  }`
                : 'Bugün için randevu bulunmuyor'
            }
          />
          <KpiCard
            title="Bugün Gelir"
            value={summaryLoading ? '—' : currencyFmt.format(rev?.total ?? 0)}
            loading={summaryLoading}
            to="/finance"
            hint={
              paymentCount > 0
                ? `${paymentCount} ödeme alındı`
                : 'Bugün tahsilat kaydı yok'
            }
          />
          <KpiCard
            title="Bekleyen Talep"
            value={summaryLoading ? '—' : numberFmt.format(summary?.pendingBookingRequests ?? 0)}
            loading={summaryLoading}
            to="/booking-requests"
            hint={
              (summary?.pendingBookingRequests ?? 0) > 0
                ? 'Onay bekleyen online başvuru'
                : 'Bekleyen başvuru yok'
            }
          />
          <KpiCard
            title="Bildirimler"
            value={summaryLoading ? '—' : numberFmt.format(summary?.unreadNotifications ?? 0)}
            loading={summaryLoading}
            to="/notifications"
            hint={
              (summary?.unreadNotifications ?? 0) > 0
                ? 'Okunmamış mesaj'
                : 'Tüm bildirimler okundu'
            }
          />
        </div>
      </section>

      {/* Ana içerik: 2 kolon */}
      <section className="grid gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <TodaySchedule
            appointments={todayAppts}
            statuses={statuses}
            isLoading={apptsLoading}
          />
        </div>
        <div className="flex flex-col gap-4">
          <RevenueChart days={dailyRevenue} isLoading={revenueLoading} />
          <StatusBreakdown summary={appts} isLoading={summaryLoading} />
        </div>
      </section>
    </div>
  );
}
