import { Calendar, Users, CreditCard, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuthStore } from '@/store/authStore';

const stats = [
  { title: 'Toplam Hasta', value: '—', icon: Users, color: 'text-blue-600' },
  { title: "Bugünkü Randevular", value: '—', icon: Calendar, color: 'text-green-600' },
  { title: 'Aylık Gelir', value: '—', icon: CreditCard, color: 'text-purple-600' },
  { title: 'Büyüme', value: '—', icon: TrendingUp, color: 'text-orange-600' },
];

export function DashboardPage() {
  const user = useAuthStore((s) => s.user);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Hoş geldiniz, {user?.name ?? 'Kullanıcı'}
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map(({ title, value, icon: Icon, color }) => (
          <Card key={title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {title}
              </CardTitle>
              <Icon className={`h-5 w-5 ${color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Yaklaşan Randevular</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              API bağlantısı kurulduktan sonra veriler burada görünecek.
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Son İşlemler</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              API bağlantısı kurulduktan sonra veriler burada görünecek.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
