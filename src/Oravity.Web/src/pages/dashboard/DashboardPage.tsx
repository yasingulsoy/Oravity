import type { ComponentType } from 'react';
import {
  ArrowUpRight,
  Calendar,
  CalendarClock,
  CreditCard,
  History,
  Sparkles,
  TrendingUp,
  Users,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuthStore } from '@/store/authStore';
import { cn } from '@/lib/utils';

type Stat = {
  title: string;
  value: string;
  hint: string;
  icon: ComponentType<{ className?: string }>;
  accent: string;
};

const stats: Stat[] = [
  {
    title: 'Toplam Hasta',
    value: '—',
    hint: 'Kayıtlı hasta sayısı',
    icon: Users,
    accent:
      'bg-sky-500/10 text-sky-700 ring-sky-500/20 dark:text-sky-300 dark:ring-sky-500/30',
  },
  {
    title: 'Bugünkü randevular',
    value: '—',
    hint: 'Bugün için planlanan',
    icon: Calendar,
    accent:
      'bg-emerald-500/10 text-emerald-700 ring-emerald-500/20 dark:text-emerald-300 dark:ring-emerald-500/30',
  },
  {
    title: 'Aylık gelir',
    value: '—',
    hint: 'Bu ay tahmini',
    icon: CreditCard,
    accent:
      'bg-violet-500/10 text-violet-700 ring-violet-500/20 dark:text-violet-300 dark:ring-violet-500/30',
  },
  {
    title: 'Büyüme',
    value: '—',
    hint: 'Önceki döneme göre',
    icon: TrendingUp,
    accent:
      'bg-amber-500/10 text-amber-800 ring-amber-500/20 dark:text-amber-300 dark:ring-amber-500/30',
  },
];

function StatCard({ stat }: { stat: Stat }) {
  const Icon = stat.icon;
  return (
    <Card
      className={cn(
        'group relative overflow-hidden border-0 bg-card/80 shadow-sm ring-1 ring-border/60',
        'transition-[box-shadow,transform] duration-200 hover:-translate-y-0.5 hover:shadow-md hover:ring-border',
      )}
    >
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.03] dark:opacity-[0.06]"
        style={{
          backgroundImage: `radial-gradient(circle at 100% 0%, currentColor 0%, transparent 55%)`,
        }}
      />
      <CardHeader className="relative flex flex-row items-start justify-between space-y-0 pb-2">
        <div className="space-y-1">
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            {stat.title}
          </p>
          <CardTitle className="font-heading text-3xl font-semibold tabular-nums tracking-tight text-foreground">
            {stat.value}
          </CardTitle>
        </div>
        <div
          className={cn(
            'flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ring-1',
            stat.accent,
          )}
          aria-hidden
        >
          <Icon className="h-[1.125rem] w-[1.125rem] stroke-[1.75]" />
        </div>
      </CardHeader>
      <CardContent className="relative pt-0">
        <p className="flex items-center gap-1 text-xs text-muted-foreground">
          <Sparkles className="h-3 w-3 opacity-60" aria-hidden />
          {stat.hint}
        </p>
      </CardContent>
    </Card>
  );
}

function EmptyPanel({
  title,
  icon: Icon,
  description,
}: {
  title: string;
  icon: ComponentType<{ className?: string }>;
  description: string;
}) {
  return (
    <Card className="border-0 bg-card/80 shadow-sm ring-1 ring-border/60">
      <CardHeader className="flex flex-row items-center gap-3 space-y-0 border-b border-border/80 pb-4">
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-muted/80 text-muted-foreground ring-1 ring-border/50">
          <Icon className="h-4 w-4 stroke-[1.75]" aria-hidden />
        </div>
        <CardTitle className="font-heading text-base font-semibold">{title}</CardTitle>
      </CardHeader>
      <CardContent className="pt-6">
        <div className="flex min-h-[140px] flex-col items-center justify-center rounded-xl border border-dashed border-border/80 bg-muted/20 px-6 py-8 text-center">
          <div className="mb-3 flex h-11 w-11 items-center justify-center rounded-full bg-muted/60 text-muted-foreground ring-1 ring-border/40">
            <ArrowUpRight className="h-5 w-5 stroke-[1.5] opacity-50" aria-hidden />
          </div>
          <p className="max-w-[260px] text-sm leading-relaxed text-muted-foreground">
            {description}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const displayName = user?.name ?? 'Kullanıcı';

  return (
    <div className="mx-auto max-w-6xl space-y-10">
      <header className="space-y-1">
        <p className="text-xs font-medium uppercase tracking-[0.2em] text-muted-foreground">
          Genel bakış
        </p>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="font-heading text-3xl font-semibold tracking-tight text-foreground">
              Dashboard
            </h1>
            <p className="mt-1 text-muted-foreground">
              Hoş geldiniz,{' '}
              <span className="font-medium text-foreground">{displayName}</span>
            </p>
          </div>
        </div>
      </header>

      <section aria-labelledby="stats-heading" className="space-y-4">
        <h2 id="stats-heading" className="sr-only">
          Özet metrikler
        </h2>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {stats.map((stat) => (
            <StatCard key={stat.title} stat={stat} />
          ))}
        </div>
      </section>

      <section aria-labelledby="activity-heading" className="space-y-4">
        <h2
          id="activity-heading"
          className="font-heading text-sm font-semibold text-foreground"
        >
          Aktivite
        </h2>
        <div className="grid gap-6 lg:grid-cols-2">
          <EmptyPanel
            title="Yaklaşan randevular"
            icon={CalendarClock}
            description="API bağlantısı kurulduktan sonra yaklaşan randevular burada listelenecek."
          />
          <EmptyPanel
            title="Son işlemler"
            icon={History}
            description="API bağlantısı kurulduktan sonra son finansal işlemler burada görünecek."
          />
        </div>
      </section>
    </div>
  );
}
