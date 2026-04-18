import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format, subDays } from 'date-fns';
import {
  BarChart3,
  TrendingUp,
  Users,
  CalendarCheck,
  Clock,
  AlertTriangle,
} from 'lucide-react';
import { reportsApi } from '@/api/reports';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';

function formatCurrency(n: number) {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(n);
}

export function ReportsPage() {
  const today = format(new Date(), 'yyyy-MM-dd');
  const weekAgo = format(subDays(new Date(), 7), 'yyyy-MM-dd');
  const monthAgo = format(subDays(new Date(), 30), 'yyyy-MM-dd');

  const [range] = useState({ start: monthAgo, end: today });

  const { data: dashboard, isLoading: dashLoading } = useQuery({
    queryKey: ['reports', 'dashboard'],
    queryFn: () => reportsApi.dashboard(),
  });

  const { data: revenueData, isLoading: revLoading } = useQuery({
    queryKey: ['reports', 'revenue', range],
    queryFn: () => reportsApi.dailyRevenue(range.start, range.end),
  });

  const { data: doctorData, isLoading: docLoading } = useQuery({
    queryKey: ['reports', 'doctor-performance', range],
    queryFn: () => reportsApi.doctorPerformance(range.start, range.end),
  });

  const { data: aptData, isLoading: aptLoading } = useQuery({
    queryKey: ['reports', 'appointment-stats', range],
    queryFn: () => reportsApi.appointmentStats(range.start, range.end),
  });

  const { data: patData, isLoading: patLoading } = useQuery({
    queryKey: ['reports', 'patient-stats', range],
    queryFn: () => reportsApi.patientStats(range.start, range.end),
  });

  const dash = dashboard?.data;
  const revenue = revenueData?.data;
  const doctors = doctorData?.data;
  const aptStats = aptData?.data;
  const patStats = patData?.data;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Raporlar</h1>
        <p className="text-muted-foreground">Klinik performansını izleyin</p>
      </div>

      {/* Dashboard KPI */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Bugünkü Randevular</CardTitle>
            <CalendarCheck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {dashLoading ? <Skeleton className="h-8 w-20" /> : (
              <>
                <div className="text-2xl font-bold">{dash?.appointments.total ?? 0}</div>
                <p className="text-xs text-muted-foreground">
                  {dash?.appointments.completed ?? 0} tamamlandı, {dash?.appointments.pending ?? 0} bekliyor
                </p>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Bugünkü Gelir</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {dashLoading ? <Skeleton className="h-8 w-24" /> : (
              <div className="text-2xl font-bold">{formatCurrency(dash?.revenue.total ?? 0)}</div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Bekleyen Talepler</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {dashLoading ? <Skeleton className="h-8 w-12" /> : (
              <div className="text-2xl font-bold">{dash?.pendingBookingRequests ?? 0}</div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Okunmamış Bildirim</CardTitle>
            <AlertTriangle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {dashLoading ? <Skeleton className="h-8 w-12" /> : (
              <div className="text-2xl font-bold">{dash?.unreadNotifications ?? 0}</div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Detaylı Raporlar */}
      <Tabs defaultValue="revenue">
        <TabsList>
          <TabsTrigger value="revenue">Gelir</TabsTrigger>
          <TabsTrigger value="doctors">Doktor Performansı</TabsTrigger>
          <TabsTrigger value="appointments">Randevu İstatistikleri</TabsTrigger>
          <TabsTrigger value="patients">Hasta İstatistikleri</TabsTrigger>
        </TabsList>

        {/* Gelir Tab */}
        <TabsContent value="revenue">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="h-5 w-5" />
                Günlük Gelir Raporu
              </CardTitle>
            </CardHeader>
            <CardContent>
              {revLoading ? (
                <div className="space-y-2">
                  {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
                </div>
              ) : revenue ? (
                <div className="space-y-4">
                  <div className="text-xl font-bold">{formatCurrency(revenue.grandTotal)}</div>

                  <Separator />

                  <div>
                    <h4 className="mb-2 text-sm font-medium text-muted-foreground">Ödeme Yöntemine Göre</h4>
                    <div className="space-y-2">
                      {revenue.byMethod.map((m) => (
                        <div key={m.method} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2">
                          <span className="text-sm">{m.method}</span>
                          <span className="text-sm font-medium">{formatCurrency(m.amount)} ({m.count} adet)</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  <Separator />

                  <div>
                    <h4 className="mb-2 text-sm font-medium text-muted-foreground">Doktora Göre</h4>
                    <div className="space-y-2">
                      {revenue.byDoctor.map((d) => (
                        <div key={d.doctorId} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2">
                          <span className="text-sm">{d.doctorName}</span>
                          <span className="text-sm font-medium">{formatCurrency(d.total)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Veri bulunamadı.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Doktor Performansı Tab */}
        <TabsContent value="doctors">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Doktor Performansı
              </CardTitle>
            </CardHeader>
            <CardContent>
              {docLoading ? (
                <div className="space-y-2">
                  {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
                </div>
              ) : doctors?.doctors.length ? (
                <div className="space-y-3">
                  {doctors.doctors.map((d) => (
                    <div key={d.doctorId} className="rounded-lg border p-4">
                      <div className="flex items-center justify-between">
                        <h4 className="font-medium">{d.doctorName}</h4>
                        <Badge variant="secondary">{formatCurrency(d.totalRevenue)}</Badge>
                      </div>
                      <div className="mt-2 grid grid-cols-3 gap-2 text-sm text-muted-foreground">
                        <span>{d.completedAppointments} randevu</span>
                        <span>{d.completedTreatmentItems} tedavi</span>
                        <span>%{d.commissionRate.toFixed(0)} komisyon</span>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Veri bulunamadı.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Randevu İstatistikleri Tab */}
        <TabsContent value="appointments">
          <Card>
            <CardHeader>
              <CardTitle>Randevu İstatistikleri</CardTitle>
            </CardHeader>
            <CardContent>
              {aptLoading ? (
                <Skeleton className="h-40 w-full" />
              ) : aptStats ? (
                <div className="space-y-4">
                  <div className="grid grid-cols-3 gap-4">
                    <div className="rounded-lg bg-muted/50 p-3 text-center">
                      <div className="text-2xl font-bold">{aptStats.total}</div>
                      <div className="text-xs text-muted-foreground">Toplam</div>
                    </div>
                    <div className="rounded-lg bg-muted/50 p-3 text-center">
                      <div className="text-2xl font-bold">%{(aptStats.noShowRate * 100).toFixed(1)}</div>
                      <div className="text-xs text-muted-foreground">Gelmedi Oranı</div>
                    </div>
                    <div className="rounded-lg bg-muted/50 p-3 text-center">
                      <div className="text-2xl font-bold">{aptStats.avgDurationMinutes} dk</div>
                      <div className="text-xs text-muted-foreground">Ort. Süre</div>
                    </div>
                  </div>

                  <Separator />

                  <div className="space-y-2">
                    {aptStats.byStatus.map((s) => (
                      <div key={s.status} className="flex items-center justify-between text-sm">
                        <span>{s.label}</span>
                        <span className="font-medium">{s.count} (%{s.percentage.toFixed(1)})</span>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Veri bulunamadı.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Hasta İstatistikleri Tab */}
        <TabsContent value="patients">
          <Card>
            <CardHeader>
              <CardTitle>Hasta İstatistikleri</CardTitle>
            </CardHeader>
            <CardContent>
              {patLoading ? (
                <Skeleton className="h-40 w-full" />
              ) : patStats ? (
                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="rounded-lg bg-muted/50 p-3 text-center">
                      <div className="text-2xl font-bold">{patStats.newPatients}</div>
                      <div className="text-xs text-muted-foreground">Yeni Hasta</div>
                    </div>
                    <div className="rounded-lg bg-muted/50 p-3 text-center">
                      <div className="text-2xl font-bold">{patStats.totalActivePatients}</div>
                      <div className="text-xs text-muted-foreground">Aktif Hasta</div>
                    </div>
                  </div>

                  {patStats.topPatients.length > 0 && (
                    <>
                      <Separator />
                      <h4 className="text-sm font-medium text-muted-foreground">En Çok Tedavi Görenler</h4>
                      <div className="space-y-2">
                        {patStats.topPatients.map((p, i) => (
                          <div key={p.publicId} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2 text-sm">
                            <span>
                              <span className="mr-2 text-muted-foreground">#{i + 1}</span>
                              {p.fullName}
                            </span>
                            <span className="font-medium">{p.treatmentItemCount} tedavi · {formatCurrency(p.totalPaid)}</span>
                          </div>
                        ))}
                      </div>
                    </>
                  )}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Veri bulunamadı.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
