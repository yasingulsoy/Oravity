import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  commissionsApi,
  type CommissionTemplate,
  type CommissionTemplateInput,
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
  primRate: 0.3,
  jobStartCalculation: 'FromPriceList',
  targetSystem: 'None',
  institutionPayOnInvoice: 'OnPayment',
  deductLabCost: true,
  deductTreatmentCost: false,
  deductTreatmentPlanCommission: false,
  deductCreditCardCommission: true,
  applyKdv: false,
  applyWithholdingTax: false,
  jobStartPrices: [],
};

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
                <TableHead>Durum</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-4 w-24" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : templates.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground py-6">
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
                      {t.paymentType === 'Prim' && t.primRate != null && (
                        <span className="text-muted-foreground ml-1">
                          (%{(t.primRate * 100).toFixed(1)})
                        </span>
                      )}
                      {t.paymentType === 'Fix' && t.fixedAmount != null && (
                        <span className="text-muted-foreground ml-1">
                          (₺{t.fixedAmount.toLocaleString('tr-TR')})
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      {t.targetSystemLabel}
                      {t.targetSystem !== 'None' && t.targetBonusRate != null && (
                        <span className="text-muted-foreground ml-1">
                          (+%{(t.targetBonusRate * 100).toFixed(1)})
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {t.deductLabCost && <Badge variant="outline">Lab</Badge>}
                        {t.deductTreatmentCost && <Badge variant="outline">Maliyet</Badge>}
                        {t.deductCreditCardCommission && <Badge variant="outline">POS</Badge>}
                        {t.applyKdv && <Badge variant="outline">KDV</Badge>}
                        {t.applyWithholdingTax && <Badge variant="outline">Stopaj</Badge>}
                      </div>
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
    template ? {
      name: template.name,
      workingStyle: template.workingStyle,
      paymentType: template.paymentType,
      fixedAmount: template.fixedAmount,
      primRate: template.primRate,
      jobStartCalculation: template.jobStartCalculation,
      targetSystem: template.targetSystem,
      targetBonusRate: template.targetBonusRate,
      institutionPayOnInvoice: template.institutionPayOnInvoice,
      deductLabCost: template.deductLabCost,
      deductTreatmentCost: template.deductTreatmentCost,
      deductTreatmentPlanCommission: template.deductTreatmentPlanCommission,
      deductCreditCardCommission: template.deductCreditCardCommission,
      applyKdv: template.applyKdv,
      kdvRate: template.kdvRate,
      applyWithholdingTax: template.applyWithholdingTax,
      withholdingTaxRate: template.withholdingTaxRate,
      extraExpenseAmount: template.extraExpenseAmount,
      notes: template.notes,
      jobStartPrices: template.jobStartPrices,
    } : emptyInput
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
              <Select value={form.workingStyle} onValueChange={v => update('workingStyle', v as CommissionTemplateInput['workingStyle'])}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Accrual">Tahakkuk</SelectItem>
                  <SelectItem value="Collection">Tahsilat</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="text-sm mb-1 block">Ödeme Tipi</label>
              <Select value={form.paymentType} onValueChange={v => update('paymentType', v as CommissionTemplateInput['paymentType'])}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Fix">Sabit</SelectItem>
                  <SelectItem value="Prim">Prim (%)</SelectItem>
                  <SelectItem value="PerJob">İş Başı</SelectItem>
                  <SelectItem value="PriceRange">Fiyat Aralığı</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {form.paymentType === 'Prim' && (
            <div>
              <label className="text-sm mb-1 block">Prim Oranı (%)</label>
              <Input
                type="number" step="0.01" min="0" max="100"
                value={form.primRate != null ? form.primRate * 100 : ''}
                onChange={e => update('primRate', parseFloat(e.target.value) / 100)}
              />
            </div>
          )}
          {form.paymentType === 'Fix' && (
            <div>
              <label className="text-sm mb-1 block">Sabit Tutar (₺)</label>
              <Input
                type="number" step="0.01" min="0"
                value={form.fixedAmount ?? ''}
                onChange={e => update('fixedAmount', parseFloat(e.target.value))}
              />
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm mb-1 block">Hedef Sistemi</label>
              <Select value={form.targetSystem} onValueChange={v => update('targetSystem', v as CommissionTemplateInput['targetSystem'])}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="None">Yok</SelectItem>
                  <SelectItem value="Clinic">Klinik</SelectItem>
                  <SelectItem value="Doctor">Hekim</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {form.targetSystem !== 'None' && (
              <div>
                <label className="text-sm mb-1 block">Bonus Oranı (%)</label>
                <Input
                  type="number" step="0.01" min="0"
                  value={form.targetBonusRate != null ? form.targetBonusRate * 100 : ''}
                  onChange={e => update('targetBonusRate', parseFloat(e.target.value) / 100)}
                />
              </div>
            )}
          </div>

          <div>
            <label className="text-sm mb-1 block">Kurum Faturası Ödeme</label>
            <Select value={form.institutionPayOnInvoice} onValueChange={v => update('institutionPayOnInvoice', v as CommissionTemplateInput['institutionPayOnInvoice'])}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="OnPayment">Ödemede</SelectItem>
                <SelectItem value="OnCollection">Tahsilatta</SelectItem>
                <SelectItem value="OnInvoice">Fatura Kesimde</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Kesintiler</h4>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductLabCost} onCheckedChange={v => update('deductLabCost', !!v)} />
              Laboratuvar maliyeti düş
            </label>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductTreatmentCost} onCheckedChange={v => update('deductTreatmentCost', !!v)} />
              Tedavi maliyeti düş
            </label>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductTreatmentPlanCommission} onCheckedChange={v => update('deductTreatmentPlanCommission', !!v)} />
              Plan komisyonu düş
            </label>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.deductCreditCardCommission} onCheckedChange={v => update('deductCreditCardCommission', !!v)} />
              POS komisyonu düş
            </label>
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium text-sm">Vergiler</h4>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.applyKdv} onCheckedChange={v => update('applyKdv', !!v)} />
              KDV uygula
            </label>
            {form.applyKdv && (
              <Input
                type="number" step="0.1" min="0"
                placeholder="KDV oranı (%)"
                value={form.kdvRate != null ? form.kdvRate * 100 : ''}
                onChange={e => update('kdvRate', parseFloat(e.target.value) / 100)}
              />
            )}
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={form.applyWithholdingTax} onCheckedChange={v => update('applyWithholdingTax', !!v)} />
              Stopaj uygula
            </label>
            {form.applyWithholdingTax && (
              <Input
                type="number" step="0.1" min="0"
                placeholder="Stopaj oranı (%)"
                value={form.withholdingTaxRate != null ? form.withholdingTaxRate * 100 : ''}
                onChange={e => update('withholdingTaxRate', parseFloat(e.target.value) / 100)}
              />
            )}
          </div>

          <div>
            <label className="text-sm mb-1 block">Ekstra Masraf (₺)</label>
            <Input
              type="number" step="0.01" min="0"
              value={form.extraExpenseAmount ?? ''}
              onChange={e => update('extraExpenseAmount', e.target.value ? parseFloat(e.target.value) : undefined)}
            />
          </div>

          <div>
            <label className="text-sm mb-1 block">Notlar</label>
            <Input value={form.notes ?? ''} onChange={e => update('notes', e.target.value)} />
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
