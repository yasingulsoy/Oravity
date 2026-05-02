import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Search, Building2, BadgePercent, Landmark, ChevronDown, ChevronUp, ExternalLink, Zap } from 'lucide-react';
import { toast } from 'sonner';
import { institutionsApi, type InstitutionItem, type InstitutionPaymentModel } from '@/api/institutions';
import { pricingApi, type PricingRule } from '@/api/pricing';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

// ── Sabitler ──────────────────────────────────────────────────────────────────

const INSTITUTION_TYPES = [
  { value: 'sigorta',        label: 'Sigorta' },
  { value: 'kurumsal',       label: 'Kurumsal' },
  { value: 'kamu',           label: 'Kamu' },
  { value: 'uluslararası',   label: 'Uluslararası' },
];

const MARKET_SEGMENTS = [
  { value: 'domestic',       label: 'Yurtiçi' },
  { value: 'international',  label: 'Yurtdışı' },
];

const PAYMENT_MODEL_CONFIG: Record<InstitutionPaymentModel, { label: string; desc: string; className: string }> = {
  Discount:  { label: 'İndirim',   desc: 'Hasta indirimli fiyatı öder, kuruma fatura kesilmez.',          className: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300' },
  Provision: { label: 'Provizyon', desc: 'Kurum tedavi başına sabit tutar öder, kalan hastadan alınır.',  className: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300' },
};

// ── Form tipi ─────────────────────────────────────────────────────────────────

interface FormState {
  name: string;
  code: string;
  type: string;
  paymentModel: InstitutionPaymentModel;
  marketSegment: string;
  phone: string;
  email: string;
  website: string;
  country: string;
  city: string;
  district: string;
  address: string;
  contactPerson: string;
  contactPhone: string;
  taxNumber: string;
  taxOffice: string;
  discountRate: string;
  paymentDays: string;
  paymentTerms: string;
  notes: string;
  isActive: boolean;
  // E-Fatura & Tevkifat
  isEInvoiceTaxpayer: boolean;
  withholdingApplies: boolean;
  withholdingCode: string;
  withholdingNumerator: string;
  withholdingDenominator: string;
}

const emptyForm = (): FormState => ({
  name: '', code: '', type: '', paymentModel: 'Discount' as InstitutionPaymentModel, marketSegment: '',
  phone: '', email: '', website: '',
  country: '', city: '', district: '', address: '',
  contactPerson: '', contactPhone: '',
  taxNumber: '', taxOffice: '',
  discountRate: '', paymentDays: '30', paymentTerms: '',
  notes: '', isActive: true,
  isEInvoiceTaxpayer: false,
  withholdingApplies: false,
  withholdingCode: '',
  withholdingNumerator: '5',
  withholdingDenominator: '10',
});

function formFromItem(item: InstitutionItem): FormState {
  return {
    name:          item.name,
    code:          item.code ?? '',
    type:          item.type ?? '',
    paymentModel:  item.paymentModel,
    marketSegment: item.marketSegment ?? '',
    phone:         item.phone ?? '',
    email:         item.email ?? '',
    website:       item.website ?? '',
    country:       item.country ?? '',
    city:          item.city ?? '',
    district:      item.district ?? '',
    address:       item.address ?? '',
    contactPerson: item.contactPerson ?? '',
    contactPhone:  item.contactPhone ?? '',
    taxNumber:     item.taxNumber ?? '',
    taxOffice:     item.taxOffice ?? '',
    discountRate:  item.discountRate != null ? String(item.discountRate) : '',
    paymentDays:   String(item.paymentDays),
    paymentTerms:  item.paymentTerms ?? '',
    notes:         item.notes ?? '',
    isActive:      item.isActive,
    isEInvoiceTaxpayer:    item.isEInvoiceTaxpayer,
    withholdingApplies:    item.withholdingApplies,
    withholdingCode:       item.withholdingCode ?? '',
    withholdingNumerator:  String(item.withholdingNumerator),
    withholdingDenominator: String(item.withholdingDenominator),
  };
}

// ── Form diyaloğu ─────────────────────────────────────────────────────────────

interface InstitutionFormDialogProps {
  open: boolean;
  editing: InstitutionItem | null;
  onClose: () => void;
  onSuccess: () => void;
}

function InstitutionFormDialog({ open, editing, onClose, onSuccess }: InstitutionFormDialogProps) {
  const [form, setForm]           = useState<FormState>(emptyForm);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const isEdit = !!editing;

  // open veya editing değişince formu sıfırla/doldur
  useEffect(() => {
    if (open) {
      setForm(editing ? formFromItem(editing) : emptyForm());
      setShowAdvanced(false);
    }
  }, [open, editing]);

  const handleOpenChange = (o: boolean) => {
    if (!o) onClose();
  };

  const set = (patch: Partial<FormState>) => setForm(prev => ({ ...prev, ...patch }));

  const buildPayload = () => ({
    name:                 form.name.trim(),
    code:                 form.code.trim() || undefined,
    type:                 form.type || undefined,
    paymentModel:         form.paymentModel,
    marketSegment:        (form.marketSegment as 'domestic' | 'international') || undefined,
    phone:                form.phone.trim() || undefined,
    email:                form.email.trim() || undefined,
    website:              form.website.trim() || undefined,
    country:              form.country.trim() || undefined,
    city:                 form.city.trim() || undefined,
    district:             form.district.trim() || undefined,
    address:              form.address.trim() || undefined,
    contactPerson:        form.contactPerson.trim() || undefined,
    contactPhone:         form.contactPhone.trim() || undefined,
    taxNumber:            form.taxNumber.trim() || undefined,
    taxOffice:            form.taxOffice.trim() || undefined,
    discountRate:         form.discountRate !== '' ? Number(form.discountRate) : undefined,
    paymentDays:          form.paymentDays !== '' ? Number(form.paymentDays) : undefined,
    paymentTerms:         form.paymentTerms.trim() || undefined,
    notes:                form.notes.trim() || undefined,
    isEInvoiceTaxpayer:   form.isEInvoiceTaxpayer,
    withholdingApplies:   form.withholdingApplies,
    withholdingCode:      form.withholdingCode.trim() || undefined,
    withholdingNumerator: form.withholdingNumerator !== '' ? Number(form.withholdingNumerator) : 5,
    withholdingDenominator: form.withholdingDenominator !== '' ? Number(form.withholdingDenominator) : 10,
  });

  const createMut = useMutation({
    mutationFn: () => institutionsApi.create(buildPayload()),
    onSuccess: () => { toast.success('Kurum oluşturuldu.'); onSuccess(); onClose(); },
    onError:   () => toast.error('Kurum oluşturulamadı.'),
  });

  const updateMut = useMutation({
    mutationFn: () => institutionsApi.update(editing!.publicId, { ...buildPayload(), isActive: form.isActive }),
    onSuccess: () => { toast.success('Kurum güncellendi.'); onSuccess(); onClose(); },
    onError:   () => toast.error('Kurum güncellenemedi.'),
  });

  const isPending = createMut.isPending || updateMut.isPending;
  const canSave   = form.name.trim().length >= 2;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Landmark className="size-4" />
            {isEdit ? 'Kurumu Düzenle' : 'Yeni Anlaşmalı Kurum'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-5 py-1">

          {/* ── Temel bilgiler ── */}
          <section className="space-y-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Temel Bilgiler</h3>

            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2 space-y-1.5">
                <Label htmlFor="inst-name">Kurum Adı <span className="text-destructive">*</span></Label>
                <Input id="inst-name" value={form.name} onChange={e => set({ name: e.target.value })} placeholder="Ör. TZH Sağlık Sigortası" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="inst-code">Kod</Label>
                <Input id="inst-code" value={form.code} onChange={e => set({ code: e.target.value })} placeholder="Ör. TZH" className="uppercase" />
              </div>
              <div className="space-y-1.5">
                <Label>Kurum Tipi</Label>
                <Select value={form.type} onValueChange={v => set({ type: v })}>
                  <SelectTrigger>
                    <SelectValue placeholder="Seçin…" />
                  </SelectTrigger>
                  <SelectContent>
                    {INSTITUTION_TYPES.map(t => (
                      <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </section>

          <Separator />

          {/* ── Ödeme modeli ── */}
          <section className="space-y-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ödeme Modeli</h3>

            <div className="grid grid-cols-2 gap-3">
              {(['Discount', 'Provision'] as InstitutionPaymentModel[]).map(model => {
                const cfg = PAYMENT_MODEL_CONFIG[model];
                const icon = model === 1 ? <BadgePercent className="size-4 shrink-0" /> : <Landmark className="size-4 shrink-0" />;
                return (
                  <button
                    key={model}
                    type="button"
                    onClick={() => set({ paymentModel: model })}
                    className={cn(
                      'flex items-start gap-3 rounded-lg border-2 p-3 text-left transition-all',
                      form.paymentModel === model
                        ? 'border-primary bg-primary/5'
                        : 'border-muted hover:border-muted-foreground/30',
                    )}
                  >
                    <div className={cn('mt-0.5 rounded-full p-1.5', form.paymentModel === model ? 'bg-primary/10 text-primary' : 'bg-muted text-muted-foreground')}>
                      {icon}
                    </div>
                    <div>
                      <p className="font-medium text-sm">{cfg.label}</p>
                      <p className="text-[11px] text-muted-foreground mt-0.5 leading-snug">{cfg.desc}</p>
                    </div>
                  </button>
                );
              })}
            </div>

            {/* İndirim oranı — sadece Discount modelde */}
            {form.paymentModel === 'Discount' && (
              <div className="grid grid-cols-2 gap-3 pt-1">
                <div className="space-y-1.5">
                  <Label htmlFor="inst-dr">İndirim Oranı (%)</Label>
                  <div className="relative">
                    <Input
                      id="inst-dr"
                      type="number"
                      min={0} max={100} step={0.1}
                      value={form.discountRate}
                      onChange={e => set({ discountRate: e.target.value })}
                      placeholder="Ör. 20"
                      className="pr-8"
                    />
                    <span className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm">%</span>
                  </div>
                </div>
                <div className="space-y-1.5">
                  <Label>Pazar Segmenti</Label>
                  <Select value={form.marketSegment} onValueChange={v => set({ marketSegment: v })}>
                    <SelectTrigger>
                      <SelectValue placeholder="Seçin…" />
                    </SelectTrigger>
                    <SelectContent>
                      {MARKET_SEGMENTS.map(s => (
                        <SelectItem key={s.value} value={s.value}>{s.label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            )}
            {form.paymentModel === 'Provision' && (
              <div className="rounded-md bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 px-3 py-2.5 text-xs text-blue-700 dark:text-blue-300">
                <strong>Provizyon Akışı:</strong> Tedavi tamamlandıktan sonra resepsiyoncu, kurumun onayladığı tutarları her kalem için tedavi planından girer. Hasta kalan farkı öder.
              </div>
            )}
          </section>

          <Separator />

          {/* ── Ödeme koşulları ── */}
          <section className="space-y-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ödeme Koşulları</h3>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="inst-pd">Ödeme Vadesi (gün)</Label>
                <Input
                  id="inst-pd"
                  type="number" min={0}
                  value={form.paymentDays}
                  onChange={e => set({ paymentDays: e.target.value })}
                  placeholder="30"
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="inst-pt">Ödeme Koşulları</Label>
                <Input id="inst-pt" value={form.paymentTerms} onChange={e => set({ paymentTerms: e.target.value })} placeholder="Ör. Ay sonu ödeme" />
              </div>
            </div>
          </section>

          <Separator />

          {/* ── E-Fatura & Tevkifat ── */}
          <section className="space-y-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">E-Fatura & Tevkifat</h3>

            <div className="flex items-center gap-6">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="inst-einvoice"
                  checked={form.isEInvoiceTaxpayer}
                  onCheckedChange={v => set({ isEInvoiceTaxpayer: !!v })}
                />
                <Label htmlFor="inst-einvoice" className="cursor-pointer">E-Fatura Mükellefi</Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="inst-withholding"
                  checked={form.withholdingApplies}
                  onCheckedChange={v => set({ withholdingApplies: !!v, withholdingCode: v ? (form.withholdingCode || '') : '' })}
                />
                <Label htmlFor="inst-withholding" className="cursor-pointer">Tevkifat Uygulanır</Label>
              </div>
            </div>

            {form.withholdingApplies && (
              <div className="grid grid-cols-5 gap-3 rounded-lg border bg-muted/30 p-3">
                <div className="col-span-3 space-y-1.5">
                  <Label htmlFor="inst-wcode" className="text-xs">Tevkifat Kodu</Label>
                  <Input
                    id="inst-wcode"
                    value={form.withholdingCode}
                    onChange={e => set({ withholdingCode: e.target.value })}
                    placeholder="Ör. 616 - Diğer Hizmetler"
                  />
                </div>
                <div className="col-span-2 space-y-1.5">
                  <Label className="text-xs">Oran (Pay / Payda)</Label>
                  <div className="flex items-center gap-2">
                    <Input
                      type="number" min={1} max={10}
                      value={form.withholdingNumerator}
                      onChange={e => set({ withholdingNumerator: e.target.value })}
                      className="text-center"
                    />
                    <span className="text-muted-foreground font-semibold">/</span>
                    <Input
                      type="number" min={1} max={10}
                      value={form.withholdingDenominator}
                      onChange={e => set({ withholdingDenominator: e.target.value })}
                      className="text-center"
                    />
                  </div>
                  <p className="text-[11px] text-muted-foreground">
                    KDV'nin {form.withholdingNumerator}/{form.withholdingDenominator}'i kurum tarafından vergi dairesine ödenir.
                  </p>
                </div>
              </div>
            )}
          </section>

          <Separator />

          {/* ── İletişim & Adres (katlanabilir) ── */}
          <button
            type="button"
            className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:text-foreground transition-colors w-full"
            onClick={() => setShowAdvanced(v => !v)}
          >
            {showAdvanced ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
            İletişim, Adres & Vergi Bilgileri
          </button>

          {showAdvanced && (
            <section className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="inst-phone">Telefon</Label>
                  <Input id="inst-phone" value={form.phone} onChange={e => set({ phone: e.target.value })} placeholder="+90 212 555 0000" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="inst-email">E-posta</Label>
                  <Input id="inst-email" type="email" value={form.email} onChange={e => set({ email: e.target.value })} placeholder="info@kurum.com" />
                </div>
                <div className="col-span-2 space-y-1.5">
                  <Label htmlFor="inst-website">Web Sitesi</Label>
                  <Input id="inst-website" value={form.website} onChange={e => set({ website: e.target.value })} placeholder="https://www.kurum.com" />
                </div>
              </div>

              <Separator className="my-1" />

              <div className="grid grid-cols-3 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="inst-country">Ülke</Label>
                  <Input id="inst-country" value={form.country} onChange={e => set({ country: e.target.value })} placeholder="Türkiye" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="inst-city">Şehir</Label>
                  <Input id="inst-city" value={form.city} onChange={e => set({ city: e.target.value })} placeholder="İstanbul" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="inst-district">İlçe</Label>
                  <Input id="inst-district" value={form.district} onChange={e => set({ district: e.target.value })} placeholder="Kadıköy" />
                </div>
                <div className="col-span-3 space-y-1.5">
                  <Label htmlFor="inst-address">Açık Adres</Label>
                  <Textarea id="inst-address" rows={2} value={form.address} onChange={e => set({ address: e.target.value })} placeholder="Mahalle, sokak, no…" />
                </div>
              </div>

              <Separator className="my-1" />

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="inst-cp">Yetkili Kişi</Label>
                  <Input id="inst-cp" value={form.contactPerson} onChange={e => set({ contactPerson: e.target.value })} placeholder="Ad Soyad" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="inst-cpphone">Yetkili Telefonu</Label>
                  <Input id="inst-cpphone" value={form.contactPhone} onChange={e => set({ contactPhone: e.target.value })} placeholder="+90 5xx xxx xx xx" />
                </div>
              </div>

              <Separator className="my-1" />

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="inst-tn">Vergi Numarası</Label>
                  <Input id="inst-tn" value={form.taxNumber} onChange={e => set({ taxNumber: e.target.value })} placeholder="1234567890" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="inst-to">Vergi Dairesi</Label>
                  <Input id="inst-to" value={form.taxOffice} onChange={e => set({ taxOffice: e.target.value })} placeholder="Kadıköy VD" />
                </div>
              </div>
            </section>
          )}

          {/* ── Notlar ── */}
          <div className="space-y-1.5">
            <Label htmlFor="inst-notes">Notlar</Label>
            <Textarea id="inst-notes" rows={2} value={form.notes} onChange={e => set({ notes: e.target.value })} placeholder="Opsiyonel notlar…" />
          </div>

          {/* Durum (sadece düzenleme) */}
          {isEdit && (
            <div className="flex items-center gap-2">
              <input
                id="inst-active"
                type="checkbox"
                className="size-4 rounded accent-primary"
                checked={form.isActive}
                onChange={e => set({ isActive: e.target.checked })}
              />
              <Label htmlFor="inst-active" className="cursor-pointer">Aktif</Label>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isPending}>İptal</Button>
          <Button onClick={() => isEdit ? updateMut.mutate() : createMut.mutate()} disabled={isPending || !canSave}>
            {isPending ? 'Kaydediliyor…' : isEdit ? 'Güncelle' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Kurum kartı ───────────────────────────────────────────────────────────────

interface InstitutionCardProps {
  item: InstitutionItem;
  onEdit: () => void;
  onDelete: () => void;
}

function InstitutionPricingRules({ item }: { item: InstitutionItem }) {
  const navigate = useNavigate();

  const { data: allRules, isLoading } = useQuery({
    queryKey: ['pricing', 'rules'],
    queryFn: () => pricingApi.getRules().then(r => r.data),
    staleTime: 60_000,
  });

  const rules: PricingRule[] = (allRules ?? []).filter(r => {
    if (!r.includeFilters) return false;
    try {
      const f = JSON.parse(r.includeFilters) as { institutionIds?: number[] };
      return Array.isArray(f.institutionIds) && f.institutionIds.includes(item.id);
    } catch { return false; }
  });

  const goToPricing = () => navigate('/pricing?tab=rules');

  return (
    <div className="mt-3 pt-3 border-t">
      <div className="flex items-center justify-between mb-2">
        <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-1">
          <Zap className="size-3" /> Fiyat Kuralları
        </span>
        <button
          onClick={goToPricing}
          className="flex items-center gap-1 text-[11px] text-primary hover:underline"
        >
          <Plus className="size-3" />
          Yeni Kural
          <ExternalLink className="size-2.5" />
        </button>
      </div>

      {isLoading && (
        <div className="space-y-1.5">
          <div className="h-6 rounded bg-muted animate-pulse" />
          <div className="h-6 rounded bg-muted animate-pulse w-3/4" />
        </div>
      )}

      {!isLoading && rules.length === 0 && (
        <p className="text-[11px] text-muted-foreground italic">
          Bu kuruma özel kural tanımlanmamış —{' '}
          <button onClick={goToPricing} className="text-primary hover:underline">
            Pricing sayfasından ekleyin
          </button>
        </p>
      )}

      {rules.length > 0 && (
        <div className="space-y-1">
          {rules.map(rule => (
            <div key={rule.publicId} className="flex items-center justify-between gap-2 rounded-md bg-muted/50 px-2.5 py-1.5 text-xs">
              <div className="flex items-center gap-2 min-w-0">
                <span className={cn(
                  'shrink-0 w-1.5 h-1.5 rounded-full',
                  rule.isActive ? 'bg-emerald-500' : 'bg-muted-foreground/40',
                )} />
                <span className="font-medium truncate">{rule.name}</span>
                {rule.formula && (
                  <code className="shrink-0 text-[10px] text-muted-foreground bg-muted px-1.5 py-0.5 rounded font-mono">
                    {rule.formula}
                  </code>
                )}
              </div>
              <div className="flex items-center gap-1.5 shrink-0 text-[10px] text-muted-foreground">
                <span>Öncelik: {rule.priority}</span>
                <button onClick={goToPricing} className="hover:text-primary">
                  <ExternalLink className="size-3" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function InstitutionCard({ item, onEdit, onDelete }: InstitutionCardProps) {
  const pmCfg     = PAYMENT_MODEL_CONFIG[item.paymentModel] ?? PAYMENT_MODEL_CONFIG['Discount'];
  const typeCfg   = INSTITUTION_TYPES.find(t => t.value === item.type);
  const segCfg    = MARKET_SEGMENTS.find(s => s.value === item.marketSegment);

  return (
    <div className={cn('rounded-lg border bg-card p-4 transition-opacity', !item.isActive && 'opacity-60')}>
      <div className="flex items-start justify-between gap-3">
        {/* Sol: ikon + isim */}
        <div className="flex items-start gap-3 min-w-0">
          <div className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
            {item.paymentModel === 'Provision' ? <Landmark className="size-4" /> : <BadgePercent className="size-4" />}
          </div>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-1.5">
              <span className="font-semibold text-sm truncate">{item.name}</span>
              {item.code && (
                <Badge variant="outline" className="text-[10px] px-1.5 py-0 font-mono">{item.code}</Badge>
              )}
              {!item.isActive && (
                <Badge variant="outline" className="text-[10px] px-1.5 py-0 text-muted-foreground">Pasif</Badge>
              )}
            </div>
            <div className="flex flex-wrap gap-1.5 mt-1.5">
              <span className={cn('text-[11px] font-medium px-2 py-0.5 rounded-full', pmCfg.className)}>
                {pmCfg.label}
              </span>
              {typeCfg && (
                <span className="text-[11px] px-2 py-0.5 rounded-full bg-muted text-muted-foreground">
                  {typeCfg.label}
                </span>
              )}
              {segCfg && (
                <span className="text-[11px] px-2 py-0.5 rounded-full bg-muted text-muted-foreground">
                  {segCfg.label}
                </span>
              )}
              {item.isGlobal && (
                <span className="text-[11px] px-2 py-0.5 rounded-full bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300">
                  Platform geneli
                </span>
              )}
              {item.isEInvoiceTaxpayer && (
                <span className="text-[11px] px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300">
                  E-Fatura
                </span>
              )}
              {item.withholdingApplies && (
                <span className="text-[11px] px-2 py-0.5 rounded-full bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300">
                  Tevkifat {item.withholdingNumerator}/{item.withholdingDenominator}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Sağ: aksiyonlar */}
        <div className="flex items-center gap-1 shrink-0">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onEdit} title="Düzenle">
            <Pencil className="size-3.5" />
          </Button>
          {!item.isGlobal && (
            <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={onDelete} title="Sil">
              <Trash2 className="size-3.5" />
            </Button>
          )}
        </div>
      </div>

      {/* Detay satırı */}
      <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-muted-foreground">
        {item.paymentModel === 'Discount' && item.discountRate != null && (
          <div className="flex items-center gap-1">
            <BadgePercent className="size-3 shrink-0" />
            <span>İndirim: <strong className="text-foreground">%{item.discountRate}</strong></span>
          </div>
        )}
        {item.paymentDays > 0 && (
          <div className="flex items-center gap-1">
            <span>Vade: <strong className="text-foreground">{item.paymentDays} gün</strong></span>
          </div>
        )}
        {item.phone && (
          <div className="truncate">{item.phone}</div>
        )}
        {item.email && (
          <div className="truncate">{item.email}</div>
        )}
        {item.contactPerson && (
          <div className="col-span-2 truncate">Yetkili: {item.contactPerson}{item.contactPhone ? ` · ${item.contactPhone}` : ''}</div>
        )}
        {item.city && (
          <div className="truncate">{[item.city, item.district].filter(Boolean).join(', ')}</div>
        )}
        {item.notes && (
          <div className="col-span-2 italic truncate">{item.notes}</div>
        )}
      </div>

      {/* Fiyat kuralları bölümü */}
      <InstitutionPricingRules item={item} />
    </div>
  );
}

// ── Ana sekme bileşeni ────────────────────────────────────────────────────────

export function InstitutionsTab() {
  const qc = useQueryClient();

  const [search, setSearch]         = useState('');
  const [filterModel, setFilterModel] = useState<'all' | 'Discount' | 'Provision'>('all');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing]       = useState<InstitutionItem | null>(null);
  const [deleting, setDeleting]     = useState<InstitutionItem | null>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['settings', 'institutions'],
    queryFn: () => institutionsApi.list().then(r => r.data),
    staleTime: 30_000,
  });

  const deleteMut = useMutation({
    mutationFn: (publicId: string) => institutionsApi.delete(publicId),
    onSuccess: () => { toast.success('Kurum silindi.'); setDeleting(null); invalidate(); },
    onError:   () => toast.error('Kurum silinemedi.'),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['settings', 'institutions'] });

  const institutions = (data ?? []).filter(inst => {
    const matchesSearch = !search || inst.name.toLowerCase().includes(search.toLowerCase()) || (inst.code ?? '').toLowerCase().includes(search.toLowerCase());
    const matchesModel  = filterModel === 'all' || inst.paymentModel === filterModel;
    return matchesSearch && matchesModel;
  });

  const discountCount   = (data ?? []).filter(i => i.paymentModel === 'Discount').length;
  const provisionCount  = (data ?? []).filter(i => i.paymentModel === 'Provision').length;

  return (
    <div className="space-y-4">
      {/* Başlık + Yeni Ekle */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Anlaşmalı Kurumlar</h2>
          <p className="text-sm text-muted-foreground">
            Sigorta şirketleri, kurumsal müşteriler ve kamu kurumları
          </p>
        </div>
        <Button onClick={() => { setEditing(null); setDialogOpen(true); }}>
          <Plus className="size-4 mr-1.5" />
          Yeni Kurum
        </Button>
      </div>

      {/* İstatistik kartları */}
      {data && data.length > 0 && (
        <div className="grid grid-cols-3 gap-3">
          {[
            { label: 'Toplam',    value: data.length,     className: 'bg-muted/60' },
            { label: 'İndirim',   value: discountCount,   className: 'bg-amber-50 dark:bg-amber-950/20' },
            { label: 'Provizyon', value: provisionCount,  className: 'bg-blue-50 dark:bg-blue-950/20' },
          ].map(stat => (
            <div key={stat.label} className={cn('rounded-lg border p-3 text-center', stat.className)}>
              <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
              <div className="text-xs text-muted-foreground mt-0.5">{stat.label}</div>
            </div>
          ))}
        </div>
      )}

      {/* Arama + Model filtresi */}
      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
          <Input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Kurum adı veya kod ara…"
            className="pl-9"
          />
        </div>
        <Select value={filterModel} onValueChange={v => setFilterModel(v as typeof filterModel)}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm modeller</SelectItem>
            <SelectItem value="Discount">İndirim</SelectItem>
            <SelectItem value="Provision">Provizyon</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Yükleniyor */}
      {isLoading && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28 w-full" />)}
        </div>
      )}

      {/* Hata */}
      {isError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive text-center">
          Kurumlar yüklenirken hata oluştu.
        </div>
      )}

      {/* Boş */}
      {!isLoading && !isError && institutions.length === 0 && (
        <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed py-12 text-muted-foreground">
          <Building2 className="size-10 opacity-40" />
          <p className="text-sm">
            {search || filterModel !== 'all' ? 'Arama kriterlerine uygun kurum bulunamadı.' : 'Henüz anlaşmalı kurum eklenmemiş.'}
          </p>
          {!search && filterModel === 'all' && (
            <Button variant="outline" size="sm" onClick={() => { setEditing(null); setDialogOpen(true); }}>
              <Plus className="size-3.5 mr-1.5" />
              İlk Kurumu Ekle
            </Button>
          )}
        </div>
      )}

      {/* Kurum listesi */}
      {institutions.length > 0 && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {institutions.map(inst => (
            <InstitutionCard
              key={inst.publicId}
              item={inst}
              onEdit={() => { setEditing(inst); setDialogOpen(true); }}
              onDelete={() => setDeleting(inst)}
            />
          ))}
        </div>
      )}

      {/* Form diyaloğu */}
      <InstitutionFormDialog
        open={dialogOpen}
        editing={editing}
        onClose={() => { setDialogOpen(false); setEditing(null); }}
        onSuccess={invalidate}
      />

      {/* Silme onayı */}
      <AlertDialog open={!!deleting} onOpenChange={o => !o && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Kurumu Sil</AlertDialogTitle>
            <AlertDialogDescription>
              <strong>{deleting?.name}</strong> kurumu silinecek. Bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteMut.isPending}>Vazgeç</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={deleteMut.isPending}
              onClick={() => deleting && deleteMut.mutate(deleting.publicId)}
            >
              Evet, Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
