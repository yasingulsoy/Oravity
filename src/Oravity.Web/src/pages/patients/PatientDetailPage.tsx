import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Pencil, X, Check } from 'lucide-react';
import { useForm, Controller, useWatch } from 'react-hook-form';
import { patientsApi } from '@/api/patients';
import { lookupsApi } from '@/api/lookups';
import { geoApi } from '@/api/geo';
import { institutionsApi } from '@/api/institutions';
import type { UpdatePatientRequest } from '@/types/patient';
import { OCCUPATIONS } from '@/data/occupations';

import { Button, buttonVariants } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { PatientAccountTab } from './tabs/PatientAccountTab';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const GENDER_LABELS: Record<string, string> = { male: 'Erkek', female: 'Kadın', other: 'Diğer' };
const MARITAL_LABELS: Record<string, string> = {
  single: 'Bekar', married: 'Evli', divorced: 'Boşanmış', widowed: 'Dul',
};

function formatDate(d: string | null | undefined) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('tr-TR');
}

function Field({ label, value }: { label: string; value: string | number | boolean | null | undefined }) {
  const display = value === true ? 'Evet' : value === false ? 'Hayır' : value;
  return (
    <div>
      <dt className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{label}</dt>
      <dd className="text-sm mt-0.5">{display ?? '—'}</dd>
    </div>
  );
}

/** Controlled Select helper — shows label not raw value */
function FormSelect({
  control,
  name,
  options,
  placeholder = 'Seçin…',
}: {
  control: any;
  name: keyof UpdatePatientRequest;
  options: string[] | { value: string; label: string }[];
  placeholder?: string;
}) {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => {
        const currentVal = (field.value as string) ?? '';
        const matched = (options as any[]).find((opt) =>
          (typeof opt === 'string' ? opt : opt.value) === currentVal
        );
        const displayLabel = matched
          ? typeof matched === 'string' ? matched : matched.label
          : undefined;

        return (
          <Select value={currentVal} onValueChange={field.onChange}>
            <SelectTrigger>
              <SelectValue placeholder={placeholder}>
                {displayLabel}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {(options as any[]).map((opt) => {
                const v = typeof opt === 'string' ? opt : opt.value;
                const l = typeof opt === 'string' ? opt : opt.label;
                return <SelectItem key={v} value={v}>{l}</SelectItem>;
              })}
            </SelectContent>
          </Select>
        );
      }}
    />
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

  const { data: countriesData } = useQuery({
    queryKey: ['geo', 'countries'],
    queryFn: () => geoApi.getCountries(),
  });

  const { data: nationalitiesData } = useQuery({
    queryKey: ['geo', 'nationalities'],
    queryFn: () => geoApi.getNationalities(),
  });

  const { data: institutionsData } = useQuery({
    queryKey: ['institutions'],
    queryFn: () => institutionsApi.list(),
  });

  const patient = data?.data;

  const { register, handleSubmit, control, reset, formState: { errors } } =
    useForm<UpdatePatientRequest>();

  // Watch alanları (koşullu gösterim için)
  const watchedNationality = useWatch({ control, name: 'nationality' });
  const watchedCountry    = useWatch({ control, name: 'country' });
  const watchedCity       = useWatch({ control, name: 'city' });

  const isTurkishNationality = watchedNationality === 'Türkiye Cumhuriyeti';
  const isTurkeyCountry      = watchedCountry === 'Türkiye';

  // Türkiye ID'sini bul → kasabaları önceden yükle
  const turkeyId = countriesData?.data?.find((c) => c.isoCode === 'TR')?.id;

  const { data: citiesData } = useQuery({
    queryKey: ['geo', 'cities', turkeyId],
    queryFn: () => geoApi.getCities(turkeyId!),
    enabled: !!turkeyId,
  });

  // Seçili ilin ID'sini bul → ilçeleri yükle
  const selectedCityId = citiesData?.data?.find((c) => c.name === watchedCity)?.id;

  const { data: districtsData } = useQuery({
    queryKey: ['geo', 'districts', selectedCityId],
    queryFn: () => geoApi.getDistricts(selectedCityId!),
    enabled: !!selectedCityId,
  });

  const updateMutation = useMutation({
    mutationFn: (req: UpdatePatientRequest) => patientsApi.update(id!, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient', id] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!patient) return;
    // birthDate: API'den "2000-01-01" veya "2000-01-01T00:00:00" gelebilir; input type=date için ilk 10 karakter yeterli
    const birthDateValue = patient.birthDate ? patient.birthDate.substring(0, 10) : undefined;
    reset({
      firstName: patient.firstName,
      lastName: patient.lastName,
      motherName: patient.motherName ?? undefined,
      fatherName: patient.fatherName ?? undefined,
      gender: patient.gender ?? undefined,
      maritalStatus: patient.maritalStatus ?? undefined,
      nationality: patient.nationality ?? undefined,
      citizenshipTypeId: patient.citizenshipTypeId ?? undefined,
      occupation: patient.occupation ?? undefined,
      birthDate: birthDateValue,
      phone: patient.phone ?? undefined,
      homePhone: patient.homePhone ?? undefined,
      workPhone: patient.workPhone ?? undefined,
      email: patient.email ?? undefined,
      country: patient.country ?? undefined,
      city: patient.city ?? undefined,
      district: patient.district ?? undefined,
      address: patient.address ?? undefined,
      bloodType: patient.bloodType ?? undefined,
      referralSourceId: patient.referralSourceId ?? undefined,
      referralPerson: patient.referralPerson ?? undefined,
      agreementInstitutionId: patient.agreementInstitutionId ?? undefined,
      insuranceInstitutionId: patient.insuranceInstitutionId ?? undefined,
      notes: patient.notes ?? undefined,
      smsOptIn: patient.smsOptIn,
      campaignOptIn: patient.campaignOptIn,
    });
    setEditing(true);
  }

  function sanitizeForSubmit(data: UpdatePatientRequest): UpdatePatientRequest {
    // Boş stringleri undefined'a çevir (backend DateOnly/number parse hatası önleme)
    return Object.fromEntries(
      Object.entries(data).map(([k, v]) => [k, v === '' ? undefined : v])
    ) as UpdatePatientRequest;
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
        <Link to="/patients" className={buttonVariants({ variant: 'link' })}>Hasta listesine dön</Link>
      </div>
    );
  }

  const fullName = `${patient.firstName} ${patient.lastName}`;

  return (
    <div className="space-y-6">
      {/* Başlık */}
      <div className="flex items-center gap-4">
        <Link to="/patients" className={buttonVariants({ variant: 'ghost', size: 'icon' })}>
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <div className="flex-1">
          <h1 className="text-2xl font-bold tracking-tight">{fullName}</h1>
          <div className="flex items-center gap-2 mt-1">
            {patient.gender && <Badge variant="secondary">{GENDER_LABELS[patient.gender] ?? patient.gender}</Badge>}
            {patient.bloodType && <Badge variant="outline">{patient.bloodType}</Badge>}
            {!patient.isActive && <Badge variant="destructive">Pasif</Badge>}
          </div>
        </div>
        {!editing && (
          <Button variant="outline" size="sm" onClick={startEdit}>
            <Pencil className="h-4 w-4 mr-2" />Düzenle
          </Button>
        )}
      </div>

      <Tabs defaultValue="info">
        <TabsList>
          <TabsTrigger value="info">Bilgiler</TabsTrigger>
          <TabsTrigger value="appointments">Randevular</TabsTrigger>
          <TabsTrigger value="treatments">Tedaviler</TabsTrigger>
          <TabsTrigger value="account">Cari Hesap</TabsTrigger>
        </TabsList>

        <TabsContent value="info" className="mt-4">
          {editing ? (
            <form onSubmit={handleSubmit((d) => updateMutation.mutate(sanitizeForSubmit(d)))} className="space-y-4">

              {/* ── Kişisel ── */}
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
                    <FormSelect control={control} name="gender"
                      options={[{ value: 'male', label: 'Erkek' }, { value: 'female', label: 'Kadın' }, { value: 'other', label: 'Diğer' }]} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Medeni Durum</Label>
                    <FormSelect control={control} name="maritalStatus"
                      options={[
                        { value: 'single', label: 'Bekar' },
                        { value: 'married', label: 'Evli' },
                        { value: 'divorced', label: 'Boşanmış' },
                        { value: 'widowed', label: 'Dul' },
                      ]} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Meslek</Label>
                    <FormSelect control={control} name="occupation" options={OCCUPATIONS} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Uyruk</Label>
                    <FormSelect
                      control={control}
                      name="nationality"
                      options={(nationalitiesData?.data ?? []).map((n) => n.name)}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Vatandaşlık Tipi</Label>
                    <Controller
                      name="citizenshipTypeId"
                      control={control}
                      render={({ field }) => {
                        const label = citizenshipTypes?.data?.find((ct) => ct.id === field.value)?.name;
                        return (
                          <Select
                            value={field.value ? String(field.value) : ''}
                            onValueChange={(v) => field.onChange(Number(v))}
                          >
                            <SelectTrigger><SelectValue placeholder="Seçin…">{label}</SelectValue></SelectTrigger>
                            <SelectContent>
                              {citizenshipTypes?.data?.map((ct) => (
                                <SelectItem key={ct.id} value={String(ct.id)}>{ct.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        );
                      }}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Kan Grubu</Label>
                    <FormSelect control={control} name="bloodType"
                      options={['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', '0+', '0-']} />
                  </div>
                </CardContent>
              </Card>

              {/* ── Kimlik (TC / Pasaport) ── */}
              <Card>
                <CardHeader><CardTitle className="text-base">Kimlik Bilgisi</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  {/* Uyruk seçilmemişse veya Türkiye ise TC göster */}
                  {(!watchedNationality || isTurkishNationality) ? (
                    <div className="space-y-1.5">
                      <Label>TC Kimlik No {isTurkishNationality && <span className="text-destructive">*</span>}</Label>
                      <Input
                        {...register('tcNumber', {
                          validate: (v) => {
                            if (!isTurkishNationality) return true;
                            // TC seçili ve kayıtlı TC yoksa zorunlu
                            if (!v && !patient.hasTcNumber) return 'TC Kimlik No zorunludur';
                            // Girilmişse 11 haneli olmalı
                            if (v && v.length !== 11) return 'TC Kimlik No 11 haneli olmalıdır';
                            if (v && !/^\d{11}$/.test(v)) return 'TC Kimlik No yalnızca rakam içermelidir';
                            return true;
                          },
                        })}
                        placeholder={patient.hasTcNumber ? '••••••••••• (değiştirmek için girin)' : '11 haneli TC No'}
                        maxLength={11}
                      />
                      {errors.tcNumber && <p className="text-xs text-destructive">{errors.tcNumber.message}</p>}
                    </div>
                  ) : (
                    <div className="space-y-1.5">
                      <Label>Pasaport No <span className="text-destructive">*</span></Label>
                      <Input
                        {...register('passportNo', {
                          validate: (v) => {
                            // Yabancı uyruklu: kayıtlı pasaport yoksa zorunlu
                            if (!v && !patient.hasPassportNo) return 'Pasaport No zorunludur';
                            return true;
                          },
                        })}
                        placeholder={patient.hasPassportNo ? '••••••• (değiştirmek için girin)' : 'Pasaport numarası'}
                      />
                      {errors.passportNo && <p className="text-xs text-destructive">{errors.passportNo.message}</p>}
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* ── İletişim ── */}
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

              {/* ── Adres ── */}
              <Card>
                <CardHeader><CardTitle className="text-base">Adres</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Ülke</Label>
                    <FormSelect
                      control={control}
                      name="country"
                      options={(countriesData?.data ?? []).map((c) => c.name)}
                    />
                  </div>

                  {/* İl: Türkiye ise dropdown, değilse text */}
                  <div className="space-y-1.5">
                    <Label>İl / Şehir</Label>
                    {isTurkeyCountry ? (
                      <FormSelect
                        control={control}
                        name="city"
                        options={(citiesData?.data ?? []).map((c) => c.name)}
                        placeholder="İl seçin…"
                      />
                    ) : (
                      <Input {...register('city')} />
                    )}
                  </div>

                  {/* İlçe: Türkiye + il seçili ise dropdown, değilse text */}
                  <div className="space-y-1.5">
                    <Label>İlçe</Label>
                    {isTurkeyCountry && selectedCityId ? (
                      <FormSelect
                        control={control}
                        name="district"
                        options={(districtsData?.data ?? []).map((d) => d.name)}
                        placeholder="İlçe seçin…"
                      />
                    ) : (
                      <Input
                        {...register('district')}
                        disabled={isTurkeyCountry && !watchedCity}
                        placeholder={isTurkeyCountry && !watchedCity ? 'Önce il seçin' : ''}
                      />
                    )}
                  </div>

                  <div className="space-y-1.5 sm:col-span-2">
                    <Label>Açık Adres</Label>
                    <Textarea {...register('address')} rows={2} />
                  </div>
                </CardContent>
              </Card>

              {/* ── Geliş Bilgileri ── */}
              <Card>
                <CardHeader><CardTitle className="text-base">Geliş Bilgileri</CardTitle></CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Geliş Şekli</Label>
                    <Controller
                      name="referralSourceId"
                      control={control}
                      render={({ field }) => {
                        const label = referralSources?.data?.find((rs) => rs.id === field.value)?.name;
                        return (
                          <Select
                            value={field.value ? String(field.value) : ''}
                            onValueChange={(v) => field.onChange(Number(v))}
                          >
                            <SelectTrigger><SelectValue placeholder="Seçin…">{label}</SelectValue></SelectTrigger>
                            <SelectContent>
                              {referralSources?.data?.map((rs) => (
                                <SelectItem key={rs.id} value={String(rs.id)}>{rs.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        );
                      }}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Referans Kişi</Label>
                    <Input {...register('referralPerson')} />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Anlaşmalı Kurum (AK)</Label>
                    <Controller
                      name="agreementInstitutionId"
                      control={control}
                      render={({ field }) => {
                        const akList = institutionsData?.data?.filter(i => i.type === 'kurumsal' || i.type === 'kamu' || i.type === 'uluslararası') ?? [];
                        const label = akList.find((inst) => inst.id === field.value)?.name;
                        return (
                          <Select
                            value={field.value ? String(field.value) : ''}
                            onValueChange={(v) => field.onChange(v ? Number(v) : undefined)}
                          >
                            <SelectTrigger><SelectValue placeholder="Seçin…">{label}</SelectValue></SelectTrigger>
                            <SelectContent>
                              <SelectItem value="">— Yok —</SelectItem>
                              {akList.map((inst) => (
                                <SelectItem key={inst.id} value={String(inst.id)}>{inst.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        );
                      }}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Özel Sağlık Sigortası (ÖSS)</Label>
                    <Controller
                      name="insuranceInstitutionId"
                      control={control}
                      render={({ field }) => {
                        const ossList = institutionsData?.data?.filter(i => i.type === 'sigorta') ?? [];
                        const label = ossList.find((inst) => inst.id === field.value)?.name;
                        return (
                          <Select
                            value={field.value ? String(field.value) : ''}
                            onValueChange={(v) => field.onChange(v ? Number(v) : undefined)}
                          >
                            <SelectTrigger><SelectValue placeholder="Seçin…">{label}</SelectValue></SelectTrigger>
                            <SelectContent>
                              <SelectItem value="">— Yok —</SelectItem>
                              {ossList.map((inst) => (
                                <SelectItem key={inst.id} value={String(inst.id)}>{inst.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        );
                      }}
                    />
                  </div>
                </CardContent>
              </Card>

              {/* ── Notlar & Tercihler ── */}
              <Card>
                <CardHeader><CardTitle className="text-base">Notlar &amp; Tercihler</CardTitle></CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-1.5">
                    <Label>Notlar</Label>
                    <Textarea {...register('notes')} rows={3} />
                  </div>
                  <div className="flex items-center gap-6">
                    <div className="flex items-center gap-2">
                      <Controller name="smsOptIn" control={control} render={({ field }) => (
                        <Checkbox id="smsOptIn" checked={field.value ?? false} onCheckedChange={field.onChange} />
                      )} />
                      <Label htmlFor="smsOptIn" className="font-normal">SMS bildirimleri</Label>
                    </div>
                    <div className="flex items-center gap-2">
                      <Controller name="campaignOptIn" control={control} render={({ field }) => (
                        <Checkbox id="campaignOptIn" checked={field.value ?? false} onCheckedChange={field.onChange} />
                      )} />
                      <Label htmlFor="campaignOptIn" className="font-normal">Kampanya bildirimleri</Label>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {updateMutation.isError && (
                <p className="text-sm text-destructive">
                  Güncelleme başarısız:{' '}
                  {(updateMutation.error as any)?.response?.data?.detail ??
                   (updateMutation.error as any)?.response?.data?.title ??
                   'Tekrar deneyin.'}
                </p>
              )}

              <div className="flex gap-2 justify-end">
                <Button type="button" variant="outline" onClick={() => setEditing(false)}>
                  <X className="h-4 w-4 mr-1" />İptal
                </Button>
                <Button type="submit" disabled={updateMutation.isPending}>
                  <Check className="h-4 w-4 mr-1" />
                  {updateMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
                </Button>
              </div>
            </form>
          ) : (
            /* ── Görüntüleme modu ── */
            <div className="space-y-4">
              <Card>
                <CardHeader><CardTitle className="text-base">Kişisel Bilgiler</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Ad Soyad" value={fullName} />
                    <Field label="Ana Adı" value={patient.motherName} />
                    <Field label="Baba Adı" value={patient.fatherName} />
                    <Field label="Doğum Tarihi" value={formatDate(patient.birthDate)} />
                    <Field label="Cinsiyet" value={patient.gender ? (GENDER_LABELS[patient.gender] ?? patient.gender) : null} />
                    <Field label="Medeni Durum" value={patient.maritalStatus ? (MARITAL_LABELS[patient.maritalStatus] ?? patient.maritalStatus) : null} />
                    <Field label="Meslek" value={patient.occupation} />
                    <Field label="Uyruk" value={patient.nationality} />
                    <Field label="Vatandaşlık Tipi" value={patient.citizenshipTypeName} />
                    <Field label="Kan Grubu" value={patient.bloodType} />
                  </dl>
                </CardContent>
              </Card>

              <Card>
                <CardHeader><CardTitle className="text-base">Kimlik</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="TC Kimlik No" value={patient.hasTcNumber ? 'Kayıtlı ✓' : null} />
                    <Field label="Pasaport No" value={patient.hasPassportNo ? 'Kayıtlı ✓' : null} />
                  </dl>
                </CardContent>
              </Card>

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

              <Card>
                <CardHeader><CardTitle className="text-base">Adres</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Ülke" value={patient.country} />
                    <Field label="İl" value={patient.city} />
                    <Field label="İlçe" value={patient.district} />
                    {patient.address && (
                      <div className="sm:col-span-3">
                        <dt className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Açık Adres</dt>
                        <dd className="text-sm mt-0.5">{patient.address}</dd>
                      </div>
                    )}
                  </dl>
                </CardContent>
              </Card>

              <Card>
                <CardHeader><CardTitle className="text-base">Geliş Bilgileri</CardTitle></CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    <Field label="Geliş Şekli" value={patient.referralSourceName} />
                    <Field label="Referans Kişi" value={patient.referralPerson} />
                    <Field label="Anlaşmalı Kurum (AK)" value={patient.agreementInstitutionName} />
                    <Field label="Özel Sağlık Sigortası (ÖSS)" value={patient.insuranceInstitutionName} />
                  </dl>
                </CardContent>
              </Card>

              {(patient.notes || !patient.smsOptIn || !patient.campaignOptIn) && (
                <Card>
                  <CardHeader><CardTitle className="text-base">Notlar &amp; Tercihler</CardTitle></CardHeader>
                  <CardContent className="space-y-3">
                    {patient.notes && <p className="text-sm whitespace-pre-wrap">{patient.notes}</p>}
                    <div className="flex gap-4 text-sm text-muted-foreground">
                      <span>{patient.smsOptIn ? '✓ SMS izni var' : '✗ SMS izni yok'}</span>
                      <span>{patient.campaignOptIn ? '✓ Kampanya izni var' : '✗ Kampanya izni yok'}</span>
                    </div>
                  </CardContent>
                </Card>
              )}

              <div className="text-xs text-muted-foreground text-right">
                Kayıt tarihi: {formatDate(patient.createdAt)}
              </div>
            </div>
          )}
        </TabsContent>

        <TabsContent value="appointments" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Randevu Geçmişi</CardTitle></CardHeader>
            <CardContent><p className="text-sm text-muted-foreground">Yakında eklenecek.</p></CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="treatments" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Tedavi Geçmişi</CardTitle></CardHeader>
            <CardContent><p className="text-sm text-muted-foreground">Yakında eklenecek.</p></CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="account" className="mt-4">
          <PatientAccountTab patientId={patient.id} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
