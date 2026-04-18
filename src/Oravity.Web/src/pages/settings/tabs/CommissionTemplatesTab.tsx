import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  commissionsApi,
  type CommissionTemplate,
  type CommissionTemplateInput,
  type PaymentType,
  type WorkingStyle,
} from '@/api/commissions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

const emptyInput: CommissionTemplateInput = {
  name: '',
  workingStyle: 'Accrual',
  paymentType: 'Prim',
  fixedFee: 0,
  primRate: 30,
  institutionPayOnInvoice: false,
  jobStartCalculation: null,

  clinicTargetEnabled: false,
  clinicTargetBonusRate: null,
  doctorTargetEnabled: false,
  doctorTargetBonusRate: null,

  deductTreatmentPlanCommission: false,
  deductLabCost: true,
  deductTreatmentCost: false,

  kdvEnabled: false,
  kdvRate: null,
  kdvAppliedPaymentTypes: null,

  extraExpenseEnabled: false,
  extraExpenseRate: null,

  withholdingTaxEnabled: false,
  withholdingTaxRate: null,

  jobStartPrices: [],
};

function toInput(t: CommissionTemplate): CommissionTemplateInput {
  return {
    name: t.name,
    workingStyle: t.workingStyle,
    paymentType: t.paymentType,
    fixedFee: t.fixedFee,
    primRate: t.primRate,
    institutionPayOnInvoice: t.institutionPayOnInvoice,
    jobStartCalculation: t.jobStartCalculation,

    clinicTargetEnabled: t.clinicTargetEnabled,
    clinicTargetBonusRate: t.clinicTargetBonusRate,
    doctorTargetEnabled: t.doctorTargetEnabled,
    doctorTargetBonusRate: t.doctorTargetBonusRate,

    deductTreatmentPlanCommission: t.deductTreatmentPlanCommission,
    deductLabCost: t.deductLabCost,
    deductTreatmentCost: t.deductTreatmentCost,

    kdvEnabled: t.kdvEnabled,
    kdvRate: t.kdvRate,
    kdvAppliedPaymentTypes: t.kdvAppliedPaymentTypes,

    extraExpenseEnabled: t.extraExpenseEnabled,
    extraExpenseRate: t.extraExpenseRate,

    withholdingTaxEnabled: t.withholdingTaxEnabled,
    withholdingTaxRate: t.withholdingTaxRate,

    jobStartPrices: t.jobStartPrices.map(jp => ({
      treatmentId: jp.treatmentId,
      priceType: jp.priceType,
      value: jp.value,
    })),
  };
}

export function CommissionTemplatesTab() {
  const qc = useQueryClient();
  const [editing, setEditing] = useState<CommissionTemplate | null>(null);
  const [creating, setCreating] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['commission-templates'],
    queryFn: () => commissionsApi.listTemplates(),
  });

  const del = useMutation({
    mutationFn: (publicId: string) => commissionsApi.deleteTemplate(publicId),
    onSuccess: () => {
      toast.success('Şablon silindi');
      qc.invalidateQueries({ queryKey: ['commission-templates'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const templates = data?.data ?? [];

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Hakediş Şablonları</CardTitle>
          <Button onClick={() => setCreating(true)}>
            <Plus className="mr-1 h-4 w-4" /> Yeni Şablon
          </Button>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ad</TableHead>
                <TableHead>Çalışma Şekli</TableHead>
                <TableHead>Ödeme Tipi</TableHead>
                <TableHead>Hedef</TableHead>
                <TableHead>Kesintiler</TableHead>
                <TableHead>Kurum</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 8 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-24" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : templates.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center text-muted-foreground py-6">
                    Şablon bulunamadı.
                  </TableCell>
                </TableRow>
              ) : (
                templates.map(t => (
                  <TableRow key={t.id}>
                    <TableCell className="font-medium">{t.name}</TableCell>
                    <TableCell>{t.workingStyleLabel}</TableCell>
                    <TableCell>
                      {t.paymentTypeLabel}
                      {(t.paymentType === 'Prim' || t.paymentType === 'FixPlusPrim') && (
                        <span className="text-muted-foreground ml-1">
                          (%{t.primRate.toFixed(1)})
                        </span>
                      )}
                      {(t.paymentType === 'Fix' || t.paymentType === 'FixPlusPrim') && t.fixedFee > 0 && (
                        <span className="text-muted-foreground ml-1">
                          ₺{t.fixedFee.toLocaleString('tr-TR')}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-col text-xs">
                        {t.doctorTargetEnabled && (
                          <span>Hekim +%{t.doctorTargetBonusRate?.toFixed(1) ?? '-'}</span>
                        )}
                        {t.clinicTargetEnabled && (
                          <span>Klinik +%{t.clinicTargetBonusRate?.toFixed(1) ?? '-'}</span>
                        )}
                        {!t.doctorTargetEnabled && !t.clinicTargetEnabled && (
                          <span className="text-muted-foreground">Yok</span>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {t.deductLabCost && <Badge variant="outline">Lab</Badge>}
                        {t.deductTreatmentCost && <Badge variant="outline">Maliyet</Badge>}
                        {t.deductTreatmentPlanCommission && <Badge variant="outline">Plan</Badge>}
                        {t.deductCreditCardCommission && <Badge variant="outline">POS</Badge>}
                        {t.kdvEnabled && <Badge variant="outline">KDV</Badge>}
                        {t.withholdingTaxEnabled && <Badge variant="outline">Stopaj</Badge>}
                        {t.extraExpenseEnabled && <Badge variant="outline">Ekstra</Badge>}
                      </div>
                    </TableCell>
                    <TableCell className="text-xs">
                      {t.institutionPayOnInvoice ? 'Fatura Kesilince' : 'Kurum Ödeyince'}
                    </TableCell>
                    <TableCell>
                      <Badge variant={t.isActive ? 'default' : 'outline'}>
                        {t.isActive ? 'Aktif' : 'Pasif'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="sm" variant="ghost" onClick={() => setEditing(t)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            if (confirm(`"${t.name}" silinsin mi?`)) del.mutate(t.publicId);
                          }}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {(creating || editing) && (
        <TemplateDialog
          template={editing}
          onClose={() => { setCreating(false); setEditing(null); }}
          onSuccess={() => {
            setCreating(false);
            setEditing(null);
            qc.invalidateQueries({ queryKey: ['commission-templates'] });
          }}
        />
      )}
    </div>
  );
}

// ── Template create/edit dialog ──────────────────────────────────────────

function TemplateDialog({
  template, onClose, onSuccess,
}: {
  template: CommissionTemplate | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [form, setForm] = useState<CommissionTemplateInput>(
    template ? toInput(template) : emptyInput,
  );

  const save = useMutation({
    mutationFn: () => template
      ? commissionsApi.updateTemplate(template.publicId, form)
      : commissionsApi.createTemplate(form),
    onSuccess: () => {
      toast.success(template ? 'Şablon güncellendi' : 'Şablon oluşturuldu');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const update = <K extends keyof CommissionTemplateInput>(key: K, val: CommissionTemplateInput[K]) =>
    setForm(f => ({ ...f, [key]: val }));

  const showPrimRate = form.paymentType === 'Prim' || form.paymentType === 'FixPlusPrim';
  const showFixedFee = form.paymentType === 'Fix' || form.paymentType === 'FixPlusPrim';

  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{template ? 'Şablon Düzenle' : 'Yeni Hakediş Şablonu'}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-2">
          <div>
            <label className="text-sm mb-1 block">Ad</label>
            <Input value={form.name} onChange={e => update('name', e.target.value)} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm mb-1 block">Çalışma Şekli</label>
              <Select value={form.workingStyle} onValueChange={v => update('workingStyle', v as WorkingStyle)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Accrual">Tahakkuk</SelectItem>
                  <SelectItem value="Collection">Tahsilat</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="text-sm mb-1 block">Ödeme Tipi</label>
              <Select value={form.paymentType} onValueChange={v => update('paymentType', v as PaymentType)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Fix">Sabit</SelectItem>
                  <SelectItem value="Prim">Prim (%)</SelectItem>
                  <SelectItem value="FixPlusPrim">Sabit + Prim</SelectItem>
                  <SelectItem value="PerJob">İş Başı</SelectItem>
                  <SelectItem value="PerJobSelectedPlusFixPrim">Seçili İş Başı + Fix/Prim</SelectItem>
                  <SelectItem value="PriceRange">Fiyat Aralığı</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            {showPrimRate && (
              <div>
                <label className="text-sm mb-1 block">Prim Oranı (%)</label>
                <Input
                  type="number" step="0.01" min="0" max="100"
                  value={form.primRate}
                  onChange={e => update('primRate', parseFloat(e.target.value) || 0)}
                />
              </div>
            )}
            {showFixedFee && (
              <div>
                <label className="text-sm mb-1 block">Sabit Tutar (₺)</label>
                <Input
                  type="number" step="0.01" min="0"
                  value={form.fixedFee}
                  onChange={e => update('fixedFee', parseFloat(e.target.value) || 0)}
                />
              </div>
            )}
          </div>

          {/* Hedef Sistemi */}
          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Hedef Sistemi</h4>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="flex items-center gap-2 text-sm">
                  <Checkbox
                    checked={form.doctorTargetEnabled}
                    onCheckedChange={v => update('doctorTargetEnabled', !!v)}
                  />
                  Hekim hedefi aktif
                </label>
                {form.doctorTargetEnabled && (
                  <Input
                    type="number" step="0.01" min="0" max="100"
                    placeholder="Bonus prim (%)"
                    value={form.doctorTargetBonusRate ?? ''}
                    onChange={e => update('doctorTargetBonusRate',
                      e.target.value ? parseFloat(e.target.value) : null)}
                  />
                )}
              </div>
              <div className="space-y-1">
                <label className="flex items-center gap-2 text-sm">
                  <Checkbox
                    checked={form.clinicTargetEnabled}
                    onCheckedChange={v => update('clinicTargetEnabled', !!v)}
                  />
                  Klinik hedefi aktif
                </label>
                {form.clinicTargetEnabled && (
                  <Input
                    type="number" step="0.01" min="0" max="100"
                    placeholder="Bonus prim (%)"
                    value={form.clinicTargetBonusRate ?? ''}
                    onChange={e => update('clinicTargetBonusRate',
                      e.target.value ? parseFloat(e.target.value) : null)}
                  />
                )}
              </div>
            </div>
          </div>

          <div>
            <label className="text-sm mb-1 block">Kurum Faturası Hakediş Zamanlaması</label>
            <Select
              value={form.institutionPayOnInvoice ? 'invoice' : 'payment'}
              onValueChange={v => update('institutionPayOnInvoice', v === 'invoice')}
            >
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="payment">Kurum Ödeyince (Güvenli)</SelectItem>
                <SelectItem value="invoice">Fatura Kesilince (Riskli)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Kesintiler</h4>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductLabCost} onCheckedChange={v => update('deductLabCost', !!v)} />
              Laboratuvar masrafı düş
            </label>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductTreatmentCost} onCheckedChange={v => update('deductTreatmentCost', !!v)} />
              Tedavi maliyeti düş
            </label>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductTreatmentPlanCommission} onCheckedChange={v => update('deductTreatmentPlanCommission', !!v)} />
              Tedavi planı komisyonu düş
            </label>
            <p className="text-xs text-muted-foreground">
              POS/Kredi Kartı komisyonu kural gereği her zaman düşülür.
            </p>
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Vergiler</h4>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.kdvEnabled} onCheckedChange={v => update('kdvEnabled', !!v)} />
              KDV uygula
            </label>
            {form.kdvEnabled && (
              <div className="space-y-1 pl-6">
                <Input
                  type="number" step="0.1" min="0"
                  placeholder="KDV oranı (%)"
                  value={form.kdvRate ?? ''}
                  onChange={e => update('kdvRate',
                    e.target.value ? parseFloat(e.target.value) : null)}
                />
                <Input
                  placeholder='Uygulanan ödeme tipleri (JSON, örn: [1,2,3])'
                  value={form.kdvAppliedPaymentTypes ?? ''}
                  onChange={e => update('kdvAppliedPaymentTypes', e.target.value || null)}
                />
                <p className="text-xs text-muted-foreground">
                  1=Nakit, 2=Kredi Kartı, 3=Havale, 4=Taksit, 5=Çek. Boş bırakılırsa hepsi uygulanır.
                </p>
              </div>
            )}
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.withholdingTaxEnabled} onCheckedChange={v => update('withholdingTaxEnabled', !!v)} />
              Stopaj uygula
            </label>
            {form.withholdingTaxEnabled && (
              <Input
                type="number" step="0.1" min="0"
                className="ml-6"
                placeholder="Stopaj oranı (%)"
                value={form.withholdingTaxRate ?? ''}
                onChange={e => update('withholdingTaxRate',
                  e.target.value ? parseFloat(e.target.value) : null)}
              />
            )}
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Ekstra Gider</h4>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={form.extraExpenseEnabled}
                onCheckedChange={v => update('extraExpenseEnabled', !!v)}
              />
              Ekstra gider yüzdesi uygula
            </label>
            {form.extraExpenseEnabled && (
              <Input
                type="number" step="0.01" min="0"
                className="ml-6"
                placeholder="Oran (%)"
                value={form.extraExpenseRate ?? ''}
                onChange={e => update('extraExpenseRate',
                  e.target.value ? parseFloat(e.target.value) : null)}
              />
            )}
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Vazgeç</Button>
          <Button onClick={() => save.mutate()} disabled={!form.name || save.isPending}>
            {template ? 'Güncelle' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
