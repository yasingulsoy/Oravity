import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import { patientsApi } from '@/api/patients';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>();

  const { data, isLoading } = useQuery({
    queryKey: ['patient', id],
    queryFn: () => patientsApi.getById(id!),
    enabled: !!id,
  });

  const patient = data?.data;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!patient) {
    return (
      <div className="text-center">
        <p className="text-muted-foreground">Hasta bulunamadı.</p>
        <Button variant="link" asChild>
          <Link to="/patients">Hasta listesine dön</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/patients">
            <ArrowLeft className="h-4 w-4" />
          </Link>
        </Button>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{patient.fullName}</h1>
          <Badge variant="secondary">{patient.gender}</Badge>
        </div>
      </div>

      <Tabs defaultValue="info">
        <TabsList>
          <TabsTrigger value="info">Bilgiler</TabsTrigger>
          <TabsTrigger value="appointments">Randevular</TabsTrigger>
          <TabsTrigger value="treatments">Tedaviler</TabsTrigger>
        </TabsList>

        <TabsContent value="info" className="mt-4">
          <Card>
            <CardHeader>
              <CardTitle>Hasta Bilgileri</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid gap-4 sm:grid-cols-2">
                <div>
                  <dt className="text-sm font-medium text-muted-foreground">E-posta</dt>
                  <dd className="text-sm">{patient.email}</dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-muted-foreground">Telefon</dt>
                  <dd className="text-sm">{patient.phone}</dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-muted-foreground">Doğum Tarihi</dt>
                  <dd className="text-sm">{patient.dateOfBirth}</dd>
                </div>
                {patient.nationalId && (
                  <div>
                    <dt className="text-sm font-medium text-muted-foreground">TC Kimlik No</dt>
                    <dd className="text-sm">{patient.nationalId}</dd>
                  </div>
                )}
                {patient.address && (
                  <div className="sm:col-span-2">
                    <dt className="text-sm font-medium text-muted-foreground">Adres</dt>
                    <dd className="text-sm">{patient.address}</dd>
                  </div>
                )}
                {patient.notes && (
                  <div className="sm:col-span-2">
                    <dt className="text-sm font-medium text-muted-foreground">Notlar</dt>
                    <dd className="text-sm">{patient.notes}</dd>
                  </div>
                )}
              </dl>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="appointments" className="mt-4">
          <Card>
            <CardHeader>
              <CardTitle>Randevu Geçmişi</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                API bağlantısı kurulduktan sonra veriler burada görünecek.
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="treatments" className="mt-4">
          <Card>
            <CardHeader>
              <CardTitle>Tedavi Geçmişi</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                API bağlantısı kurulduktan sonra veriler burada görünecek.
              </p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
