import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Pencil, X, Check } from 'lucide-react';
import { useForm, Controller } from 'react-hook-form';
import { patientsApi } from '@/api/patients';
import { lookupsApi } from '@/api/lookups';
import type { UpdatePatientRequest } from '@/types/patient';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';

const genderLabel: Record<string, string> = {
  male: 'Erkek', female: 'Kadın', other: 'Diğer',
};

const maritalStatusLabel: Record<string, string> = {
  single: 'Bekar', married: 'Evli', divorced: 'Boşanmış', widowed: 'Dul',
};

function formatDate(date: string | null | undefined) {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('tr-TR');
}

function Field({ label, value }: { label: string; value: string | number | null | undefined }) {
  return (
    <div>
      <dt className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{label}</dt>
      <dd className="text-sm mt-0.5">{value ?? '—'}</dd>
    </div>
  );
}

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['patient', id],
    queryFn: () => patientsApi.getById(id!),
    enabled: !!id,
  });

  const { data: referralSources } = useQuery({
    queryKey: ['lookups', 'referral-sources'],
    queryFn: () => lookupsApi.getReferralSources(),
  });

  const { data: citizenshipTypes } = useQuery({
    queryKey: ['lookups', 'citizenship-types'],
    queryFn: () => lookupsApi.getCitizenshipTypes(),
  });

  const patient = data?.data;

  const { register, handleSubmit, setValue, reset, control, formState: { errors } } =
    useForm<UpdatePatientRequest>();

  const updateMutation = useMutation({
    mutationFn: (req: UpdatePatientRequest) => patientsApi.update(id!, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient', id] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!patient) return;
    reset({
      firstName: patient.firstName,
      lastName: patient.lastName,
      motherName: patient.motherName ?? '',
      fatherName: patient.fatherName ?? '',
      gender: patient.gender ?? '',
      maritalStatus: patient.maritalStatus ?? '',
      nationality: patient.nationality ?? '',
      citizenshipTypeId: patient.citizenshipTypeId ?? undefined,
      occupation: patient.occupation ?? '',
      smokingType: patient.smokingType ?? '',
      pregnancyStatus: patient.pregnancyStatus ?? undefined,
      birthDate: patient.birthDate ?? '',
      phone: patient.phone ?? '',
      homePhone: patient.homePhone ?? '',
      workPhone: patient.workPhone ?? '',
      email: patient.email ?? '',
      country: patient.country ?? '',
      city: patient.city ?? '',
      district: patient.district ?? '',
      neighborhood: patient.neighborhood ?? '',
      address: patient.address ?? '',
      bloodType: patient.bloodType ?? '',
      referralSourceId: patient.referralSourceId ?? undefined,
      referralPerson: patient.referralPerson ?? '',
      notes: patient.notes ?? '',
      smsOptIn: patient.smsOptIn,
      campaignOptIn: patient.campaignOptIn,
    });
    setEditing(true);
  }

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
      <div className="text-center py-16">
        <p className="text-muted-foreground">Hasta bulunamadı.</p>
        <Button variant="link" asChild>
          <Link to="/patients">Hasta listesine dön</Link>
        </Button>
      </div>
    );
  }

  const fullName = `${patient.firstName} ${patient.lastName}`;

  return (
    <div className="space-y-6">
      {/* Başlık */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/patients">
            <ArrowLeft className="h-4 w-4" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold tracking-tight">{fullName}</h1>
          <div className="flex items-center gap-2 mt-1">
            {patient.gender && (
              <Badge variant="secondary">{genderLabel[patient.gender] ?? patient.gender}</Badge>
            )}
            {patient.bloodType && (
              <Badge variant="outline">{patient.bloodType}</Badge>
            )}
            {!patient.isActive && (
              <Badge variant="destructive">Pasif</Badge>
            )}
          </div>
        </div>
        {!editing && (
          <Button variant="outline" size="sm" onClick={startEdit}>
            <Pencil className="h-4 w-4 mr-2" />
            Düzenle
          </Button>
        )}
      </div>

      <Tabs defaultValue="info">
        <TabsList>
          <TabsTrigger value="info">Bilgiler</TabsTrigger>
          <TabsTrigger value="appointments">Randevular</TabsTrigger>
          <TabsTrigger value="treatments">Tedaviler</TabsTrigger>
        </TabsList>

        {/* ── Bilgiler tab ── */}
        <TabsContent value="info" className="mt-4">
          {editing ? (
            <form onSubmit={handleSubmit((d) => updateMutation.mutate(d))} className="space-y-4">
              {/* Kişisel */}
              <Card>
                <CardHeader><CardTitle className="text-base">Kişisel Bilgiler</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Ad *</Label>
                    <Input {...register('firstName', { required: true })} />
                    {errors.firstName && <p className="text-xs text-destructive">Zorunlu alan</p>}
                  </div>
                  <div className="space-y-1.5">
                    <Label>Soyad *</Label>
                    <Input {...register('lastName', { required: true })} />
                    {errors.lastName && <p className="text-xs text-destructive">Zorunlu alan</p>}
                  </div>
                  <div className="space-y-1.5">
                    <Label>Ana Adı</Label>
                    <Input {...register('motherName')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Baba Adı</Label>
                    <Input {...register('fatherName')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Doğum Tarihi</Label>
                    <Input {...register('birthDate')} type="date" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Cinsiyet</Label>
                    <Controller
                      name="gender"
                      control={control}
                      render={({ field }) => (
                        <Select onValueChange={field.onChange} defaultValue={field.value ?? ''}>
                          <SelectTrigger><SelectValue placeholder="Seçin…" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="male">Erkek</SelectItem>
                            <SelectItem value="female">Kadın</SelectItem>
                            <SelectItem value="other">Diğer</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Medeni Durum</Label>
                    <Controller
                      name="maritalStatus"
                      control={control}
                      render={({ field }) => (
                        <Select onValueChange={field.onChange} defaultValue={field.value ?? ''}>
                          <SelectTrigger><SelectValue placeholder="Seçin…" /></SelectTrigger>
                          <SelectContent>
                            <SelectItem value="single">Bekar</SelectItem>
                            <SelectItem value="married">Evli</SelectItem>
                            <SelectItem value="divorced">Boşanmış</SelectItem>
                            <SelectItem value="widowed">Dul</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Meslek</Label>
                    <Input {...register('occupation')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Uyruk</Label>
                    <Input {...register('nationality')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Vatandaşlık Tipi</Label>
                    <Controller
                      name="citizenshipTypeId"
                      control={control}
                      render={({ field }) => (
                        <Select
                          onValueChange={(v) => field.onChange(Number(v))}
                          defaultValue={field.value ? String(field.value) : ''}
                        >
                          <SelectTrigger><SelectValue placeholder="Seçin…" /></SelectTrigger>
                          <SelectContent>
                            {citizenshipTypes?.data?.map((ct) => (
                              <SelectItem key={ct.id} value={String(ct.id)}>{ct.name}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Kan Grubu</Label>
                    <Controller
                      name="bloodType"
                      control={control}
                      render={({ field }) => (
                        <Select onValueChange={field.onChange} defaultValue={field.value ?? ''}>
                          <SelectTrigger><SelectValue placeholder="Seçin…" /></SelectTrigger>
                          <SelectContent>
                            {['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', '0+', '0-'].map((bg) => (
                              <SelectItem key={bg} value={bg}>{bg}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                </CardContent>
              </Card>

              {/* İletişim */}
              <Card>
                <CardHeader><CardTitle className="text-base">İletişim</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Cep Telefonu</Label>
                    <Input {...register('phone')} placeholder="05XX XXX XX XX" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Ev Telefonu</Label>
                    <Input {...register('homePhone')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>İş Telefonu</Label>
                    <Input {...register('workPhone')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>E-posta</Label>
                    <Input {...register('email')} type="email" />
                  </div>
                </CardContent>
              </Card>

              {/* Adres */}
              <Card>
                <CardHeader><CardTitle className="text-base">Adres</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Ülke</Label>
                    <Input {...register('country')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>İl</Label>
                    <Input {...register('city')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>İlçe</Label>
                    <Input {...register('district')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Mahalle</Label>
                    <Input {...register('neighborhood')} />
                  </div>
                  <div className="space-y-1.5 sm:col-span-2">
                    <Label>Açık Adres</Label>
                    <Input {...register('address')} />
                  </div>
                </CardContent>
              </Card>

              {/* Geliş / Kurum */}
              <Card>
                <CardHeader><CardTitle className="text-base">Geliş Bilgileri</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Geliş Şekli</Label>
                    <Controller
                      name="referralSourceId"
                      control={control}
                      render={({ field }) => (
                        <Select
                          onValueChange={(v) => field.onChange(Number(v))}
                          defaultValue={field.value ? String(field.value) : ''}
                        >
                          <SelectTrigger><SelectValue placeholder="Seçin…" /></SelectTrigger>
                          <SelectContent>
                            {referralSources?.data?.map((rs) => (
                              <SelectItem key={rs.id} value={String(rs.id)}>{rs.name}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Referans Kişi</Label>
                    <Input {...register('referralPerson')} />
                  </div>
                </CardContent>
              </Card>

              {/* Notlar & Tercihler */}
              <Card>
                <CardHeader><CardTitle className="text-base">Notlar &amp; Tercihler</CardTitle></CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-1.5">
                    <Label>Notlar</Label>
                    <Textarea {...register('notes')} rows={3} />
                  </div>
                  <div className="flex items-center gap-6">
                    <div className="flex items-center gap-2">
                      <Controller
                        name="smsOptIn"
                        control={control}
                        render={({ field }) => (
                          <Checkbox
                            id="smsOptIn"
                            checked={field.value ?? false}
                            onCheckedChange={field.onChange}
                          />
                        )}
                      />
                      <Label htmlFor="smsOptIn" className="font-normal">SMS bildirimleri</Label>
                    </div>
                    <div className="flex items-center gap-2">
                      <Controller
                        name="campaignOptIn"
                        control={control}
                        render={({ field }) => (
                          <Checkbox
                            id="campaignOptIn"
                            checked={field.value ?? false}
                            onCheckedChange={field.onChange}
                          />
                        )}
                      />
                      <Label htmlFor="campaignOptIn" className="font-normal">Kampanya bildirimleri</Label>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {updateMutation.isError && (
                <p className="text-sm text-destructive">Güncelleme başarısız, tekrar deneyin.</p>
              )}

              <div className="flex gap-2 justify-end">
                <Button type="button" variant="outline" onClick={() => setEditing(false)}>
                  <X className="h-4 w-4 mr-1" /> İptal
                </Button>
                <Button type="submit" disabled={updateMutation.isPending}>
                  <Check className="h-4 w-4 mr-1" />
                  {updateMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
                </Button>
              </div>
            </form>
          ) : (
            <div className="space-y-4">
              {/* Kişisel */}
              <Card>
                <CardHeader><CardTitle className="text-base">Kişisel Bilgiler</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Ad Soyad" value={fullName} />
                    <Field label="Ana Adı" value={patient.motherName} />
                    <Field label="Baba Adı" value={patient.fatherName} />
                    <Field label="Doğum Tarihi" value={formatDate(patient.birthDate)} />
                    <Field label="Cinsiyet" value={patient.gender ? (genderLabel[patient.gender] ?? patient.gender) : null} />
                    <Field label="Medeni Durum" value={patient.maritalStatus ? (maritalStatusLabel[patient.maritalStatus] ?? patient.maritalStatus) : null} />
                    <Field label="Meslek" value={patient.occupation} />
                    <Field label="Uyruk" value={patient.nationality} />
                    <Field label="Vatandaşlık Tipi" value={patient.citizenshipTypeName} />
                    <Field label="Kan Grubu" value={patient.bloodType} />
                    <Field label="Kayıt Tarihi" value={formatDate(patient.createdAt)} />
                  </dl>
                </CardContent>
              </Card>

              {/* İletişim */}
              <Card>
                <CardHeader><CardTitle className="text-base">İletişim</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Cep Telefonu" value={patient.phone} />
                    <Field label="Ev Telefonu" value={patient.homePhone} />
                    <Field label="İş Telefonu" value={patient.workPhone} />
                    <Field label="E-posta" value={patient.email} />
                  </dl>
                </CardContent>
              </Card>

              {/* Adres */}
              <Card>
                <CardHeader><CardTitle className="text-base">Adres</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Ülke" value={patient.country} />
                    <Field label="İl" value={patient.city} />
                    <Field label="İlçe" value={patient.district} />
                    <Field label="Mahalle" value={patient.neighborhood} />
                    {patient.address && (
                      <div className="sm:col-span-3">
                        <dt className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Açık Adres</dt>
                        <dd className="text-sm mt-0.5">{patient.address}</dd>
                      </div>
                    )}
                  </dl>
                </CardContent>
              </Card>

              {/* Geliş / Kurum */}
              <Card>
                <CardHeader><CardTitle className="text-base">Geliş Bilgileri</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Geliş Şekli" value={patient.referralSourceName} />
                    <Field label="Referans Kişi" value={patient.referralPerson} />
                  </dl>
                </CardContent>
              </Card>

              {/* Notlar */}
              {(patient.notes || !patient.smsOptIn || !patient.campaignOptIn) && (
                <Card>
                  <CardHeader><CardTitle className="text-base">Notlar &amp; Tercihler</CardTitle></CardHeader>
                  <CardContent className="space-y-3">
                    {patient.notes && (
                      <p className="text-sm whitespace-pre-wrap">{patient.notes}</p>
                    )}
                    <div className="flex gap-4 text-sm text-muted-foreground">
                      <span>{patient.smsOptIn ? '✓ SMS izni var' : '✗ SMS izni yok'}</span>
                      <span>{patient.campaignOptIn ? '✓ Kampanya izni var' : '✗ Kampanya izni yok'}</span>
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </TabsContent>

        {/* ── Randevular tab ── */}
        <TabsContent value="appointments" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Randevu Geçmişi</CardTitle></CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">Yakında eklenecek.</p>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ── Tedaviler tab ── */}
        <TabsContent value="treatments" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Tedavi Geçmişi</CardTitle></CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">Yakında eklenecek.</p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
