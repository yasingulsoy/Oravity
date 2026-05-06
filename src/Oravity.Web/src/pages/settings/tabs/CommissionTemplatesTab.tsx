import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, X, ArrowLeft, ChevronRight, Users, FileText } from 'lucide-react';
import { toast } from 'sonner';
import {
  commissionsApi,
  type CommissionTemplate,
  type CommissionTemplateInput,
  type JobStartPriceRequest,
  type PriceRangeRequest,
  type PaymentType,
  type WorkingStyle,
} from '@/api/commissions';
import { treatmentsApi } from '@/api/treatments';
import { settingsApi } from '@/api/settings';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';

// ── Styling helpers ──────────────────────────────────────────────────────────

const SELECT_CLS = 'h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2';

// ── Defaults ─────────────────────────────────────────────────────────────────

const emptyInput: CommissionTemplateInput = {
  name: '',
  workingStyle: 'Collection',
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
  requireLabApproval: true,

  kdvEnabled: false,
  kdvRate: null,
  kdvAppliedPaymentTypes: null,

  extraExpenseEnabled: false,
  extraExpenseRate: null,

  withholdingTaxEnabled: false,
  withholdingTaxRate: null,

  jobStartPrices: [],
  priceRanges: [],
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
    requireLabApproval: t.requireLabApproval,

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
    priceRanges: t.priceRanges.map(pr => ({
      minAmount: pr.minAmount,
      maxAmount: pr.maxAmount,
      rate: pr.rate,
    })),
  };
}

// ── Main tab ─────────────────────────────────────────────────────────────────

type TabId = 'templates' | 'assignments';

type View =
  | { type: 'list' }
  | { type: 'form'; template: CommissionTemplate | null };

export function CommissionTemplatesTab() {
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<TabId>('templates');
  const [view, setView] = useState<View>({ type: 'list' });

  if (view.type === 'form') {
    return (
      <TemplateForm
        template={view.template}
        onBack={() => setView({ type: 'list' })}
        onSuccess={() => {
          qc.invalidateQueries({ queryKey: ['commission-templates'] });
          setView({ type: 'list' });
        }}
      />
    );
  }

  return (
    <div className="space-y-4">
      {/* Tabs */}
      <div className="flex gap-1 border-b">
        <button
          onClick={() => setActiveTab('templates')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'templates'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <FileText className="inline h-4 w-4 mr-1.5 mb-0.5" />
          Şablonlar
        </button>
        <button
          onClick={() => setActiveTab('assignments')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'assignments'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Users className="inline h-4 w-4 mr-1.5 mb-0.5" />
          Hekim Atamaları
        </button>
      </div>

      {activeTab === 'templates' && (
        <TemplatesListView onEdit={t => setView({ type: 'form', template: t })} onNew={() => setView({ type: 'form', template: null })} />
      )}
      {activeTab === 'assignments' && <AssignmentsView />}
    </div>
  );
}

// ── Templates list ────────────────────────────────────────────────────────────

function TemplatesListView({
  onEdit,
  onNew,
}: {
  onEdit: (t: CommissionTemplate) => void;
  onNew: () => void;
}) {
  const qc = useQueryClient();

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
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Hakediş Şablonları</CardTitle>
        <Button onClick={onNew}>
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
                <TableRow
                  key={t.id}
                  className="cursor-pointer"
                  onClick={() => onEdit(t)}
                >
                  <TableCell className="font-medium">
                    <span className="flex items-center gap-1">
                      {t.name}
                      <ChevronRight className="h-3 w-3 text-muted-foreground" />
                    </span>
                  </TableCell>
                  <TableCell>{t.workingStyleLabel}</TableCell>
                  <TableCell>
                    <div>{t.paymentTypeLabel}</div>
                    {(t.paymentType === 'Prim' || t.paymentType === 'FixPlusPrim') && (
                      <div className="text-xs text-muted-foreground">%{t.primRate.toFixed(1)}</div>
                    )}
                    {(t.paymentType === 'Fix' || t.paymentType === 'FixPlusPrim') && t.fixedFee > 0 && (
                      <div className="text-xs text-muted-foreground">₺{t.fixedFee.toLocaleString('tr-TR')}</div>
                    )}
                    {t.paymentType === 'PerJob' && (
                      <div className="text-xs text-muted-foreground">{t.jobStartPrices.length} tedavi</div>
                    )}
                    {t.paymentType === 'PriceRange' && (
                      <div className="text-xs text-muted-foreground">{t.priceRanges.length} bant</div>
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
                  <TableCell onClick={e => e.stopPropagation()}>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        if (confirm(`"${t.name}" silinsin mi?`)) del.mutate(t.publicId);
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

// ── Assignments view ──────────────────────────────────────────────────────────

function AssignmentsView() {
  const qc = useQueryClient();
  const [showDialog, setShowDialog] = useState(false);

  const { data: assignmentsData, isLoading } = useQuery({
    queryKey: ['commission-assignments'],
    queryFn: () => commissionsApi.listAssignments({ activeOnly: false }),
  });

  const unassign = useMutation({
    mutationFn: (publicId: string) => commissionsApi.unassign(publicId),
    onSuccess: () => {
      toast.success('Atama kaldırıldı');
      qc.invalidateQueries({ queryKey: ['commission-assignments'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const assignments = assignmentsData?.data ?? [];

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Hekim — Şablon Atamaları</CardTitle>
          <p className="text-sm text-muted-foreground mt-1">
            Her hekime aktif bir hakediş şablonu atanabilir. Yeni atama, mevcut aktif atamayı otomatik sona erdirir.
          </p>
        </div>
        <Button onClick={() => setShowDialog(true)}>
          <Plus className="mr-1 h-4 w-4" /> Atama Ekle
        </Button>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Hekim</TableHead>
              <TableHead>Şablon</TableHead>
              <TableHead>Başlangıç</TableHead>
              <TableHead>Bitiş</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-24" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : assignments.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground py-6">
                  Henüz atama yok. "Atama Ekle" ile başlayın.
                </TableCell>
              </TableRow>
            ) : (
              assignments.map(a => (
                <TableRow key={a.publicId}>
                  <TableCell className="font-medium">{a.doctorName}</TableCell>
                  <TableCell>{a.templateName}</TableCell>
                  <TableCell className="text-sm">{a.effectiveDate}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {a.expiryDate ?? '—'}
                  </TableCell>
                  <TableCell>
                    <Badge variant={a.isActive ? 'default' : 'outline'}>
                      {a.isActive ? 'Aktif' : 'Sona Erdi'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {a.isActive && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          if (confirm(`${a.doctorName} ataması kaldırılsın mı?`)) {
                            unassign.mutate(a.publicId);
                          }
                        }}
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>

      <AssignDialog
        open={showDialog}
        onClose={() => setShowDialog(false)}
        onSuccess={() => {
          qc.invalidateQueries({ queryKey: ['commission-assignments'] });
          setShowDialog(false);
        }}
      />
    </Card>
  );
}

// ── Assign dialog (geniş: 60% viewport) ──────────────────────────────────────

function AssignDialog({
  open,
  onClose,
  onSuccess,
}: {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [userPublicId, setUserPublicId] = useState('');
  const [templatePublicId, setTemplatePublicId] = useState('');
  const [effectiveDate, setEffectiveDate] = useState(
    new Date().toISOString().split('T')[0],
  );
  const [expiryDate, setExpiryDate] = useState('');

  const { data: usersData } = useQuery({
    queryKey: ['settings-users'],
    queryFn: () => settingsApi.listUsers(),
    enabled: open,
    staleTime: 60_000,
  });

  const { data: templatesData } = useQuery({
    queryKey: ['commission-templates'],
    queryFn: () => commissionsApi.listTemplates(true),
    enabled: open,
    staleTime: 60_000,
  });

  const users = usersData?.data ?? [];
  const templates = templatesData?.data ?? [];

  const assign = useMutation({
    mutationFn: () =>
      commissionsApi.assignTemplate({
        userPublicId,
        templatePublicId,
        effectiveDate,
        expiryDate: expiryDate || undefined,
      }),
    onSuccess: () => {
      toast.success('Atama oluşturuldu');
      onSuccess();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const canSave = userPublicId && templatePublicId && effectiveDate;

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-[60vw] max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Hakediş Şablonu Ata</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-2 gap-6 py-4">
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-1.5 block">Hekim</label>
              <select
                value={userPublicId}
                onChange={e => setUserPublicId(e.target.value)}
                className={SELECT_CLS}
              >
                <option value="">— Hekim seçin —</option>
                {users
                  .filter(u => u.isActive)
                  .sort((a, b) => a.fullName.localeCompare(b.fullName, 'tr'))
                  .map(u => (
                    <option key={u.publicId} value={u.publicId}>
                      {u.fullName}
                      {u.title ? ` (${u.title})` : ''}
                    </option>
                  ))}
              </select>
              <p className="text-xs text-muted-foreground mt-1">
                Aktif kullanıcılar listeleniyor. Yeni atama mevcut aktif atamayı sona erdirir.
              </p>
            </div>

            <div>
              <label className="text-sm font-medium mb-1.5 block">Şablon</label>
              <select
                value={templatePublicId}
                onChange={e => setTemplatePublicId(e.target.value)}
                className={SELECT_CLS}
              >
                <option value="">— Şablon seçin —</option>
                {templates.map(t => (
                  <option key={t.publicId} value={t.publicId}>
                    {t.name} — {t.paymentTypeLabel}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-1.5 block">Başlangıç Tarihi</label>
              <Input
                type="date"
                value={effectiveDate}
                onChange={e => setEffectiveDate(e.target.value)}
              />
            </div>

            <div>
              <label className="text-sm font-medium mb-1.5 block">
                Bitiş Tarihi
                <span className="text-muted-foreground font-normal ml-1">(opsiyonel)</span>
              </label>
              <Input
                type="date"
                value={expiryDate}
                onChange={e => setExpiryDate(e.target.value)}
                min={effectiveDate}
              />
              <p className="text-xs text-muted-foreground mt-1">
                Boş bırakırsanız süresiz devam eder.
              </p>
            </div>

            {userPublicId && templatePublicId && (
              <div className="bg-muted rounded-lg p-3 text-sm space-y-1">
                <p className="font-medium">Özet</p>
                <p>{users.find(u => u.publicId === userPublicId)?.fullName ?? '?'}</p>
                <p className="text-muted-foreground">↳ {templates.find(t => t.publicId === templatePublicId)?.name ?? '?'}</p>
                <p className="text-muted-foreground text-xs">{effectiveDate} — {expiryDate || 'süresiz'}</p>
              </div>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button
            onClick={() => assign.mutate()}
            disabled={!canSave || assign.isPending}
          >
            {assign.isPending ? 'Kaydediliyor…' : 'Ata'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Template full-page form ───────────────────────────────────────────────────

function TemplateForm({
  template, onBack, onSuccess,
}: {
  template: CommissionTemplate | null;
  onBack: () => void;
  onSuccess: () => void;
}) {
  const [form, setForm] = useState<CommissionTemplateInput>(
    template ? toInput(template) : emptyInput,
  );

  const needsTreatments = form.paymentType === 'PerJob' || form.paymentType === 'PerJobSelectedPlusFixPrim';
  const { data: treatmentsData } = useQuery({
    queryKey: ['treatments', 'for-commission'],
    queryFn: () => treatmentsApi.list({ pageSize: 200, activeOnly: true }).then(r => r.data.items),
    enabled: needsTreatments,
    staleTime: 60_000,
  });
  const treatments = treatmentsData ?? [];

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

  const showPrimRate = ['Prim', 'FixPlusPrim', 'PriceRange'].includes(form.paymentType);
  const showFixedFee = ['Fix', 'FixPlusPrim', 'PerJobSelectedPlusFixPrim'].includes(form.paymentType);
  const showJobStartPrices = form.paymentType === 'PerJob' || form.paymentType === 'PerJobSelectedPlusFixPrim';
  const showPriceRanges = form.paymentType === 'PriceRange';

  const addJobStartPrice = () => {
    if (treatments.length === 0) return;
    const firstUnused = treatments.find(t => !form.jobStartPrices.some(jp => jp.treatmentId === t.id));
    if (!firstUnused) return;
    update('jobStartPrices', [...form.jobStartPrices, { treatmentId: firstUnused.id, priceType: 'FixedAmount', value: 0 }]);
  };

  const updateJobStartPrice = (idx: number, patch: Partial<JobStartPriceRequest>) =>
    update('jobStartPrices', form.jobStartPrices.map((jp, i) => i === idx ? { ...jp, ...patch } : jp));

  const removeJobStartPrice = (idx: number) =>
    update('jobStartPrices', form.jobStartPrices.filter((_, i) => i !== idx));

  const addPriceRange = () => {
    const lastMax = form.priceRanges.length > 0
      ? (form.priceRanges[form.priceRanges.length - 1].maxAmount ?? 0)
      : 0;
    update('priceRanges', [...form.priceRanges, { minAmount: lastMax, maxAmount: null, rate: 0 }]);
  };

  const updatePriceRange = (idx: number, patch: Partial<PriceRangeRequest>) =>
    update('priceRanges', form.priceRanges.map((pr, i) => i === idx ? { ...pr, ...patch } : pr));

  const removePriceRange = (idx: number) =>
    update('priceRanges', form.priceRanges.filter((_, i) => i !== idx));

  return (
    <div className="space-y-0">
      {/* Başlık / breadcrumb */}
      <div className="flex items-center gap-3 mb-6">
        <Button variant="ghost" size="sm" onClick={onBack} className="gap-1">
          <ArrowLeft className="h-4 w-4" />
          Şablonlar
        </Button>
        <span className="text-muted-foreground">/</span>
        <span className="font-semibold">
          {template ? template.name : 'Yeni Şablon'}
        </span>
      </div>

      <div className="grid grid-cols-3 gap-6">

        {/* Sol kolon: Temel ayarlar */}
        <div className="col-span-2 space-y-5">

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Temel Bilgiler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-1 block">Şablon Adı</label>
                <Input
                  value={form.name}
                  onChange={e => update('name', e.target.value)}
                  placeholder="Örn: Cerrah %35, Genel Hekim Tahsilat"
                  className="text-base"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-1 block">Çalışma Şekli</label>
                  <select value={form.workingStyle} onChange={e => update('workingStyle', e.target.value as WorkingStyle)} className={SELECT_CLS}>
                    <option value="Collection">Tahsilat — Ödeme alınınca</option>
                    <option value="Accrual">Tahakkuk — Tedavi yapılınca</option>
                  </select>
                  <p className="text-xs text-muted-foreground mt-1">
                    {form.workingStyle === 'Collection'
                      ? 'Ödeme gelmeden hakediş hesaplanamaz.'
                      : 'Tedavi tamamlanır tamamlanmaz hakediş hesaplanır.'}
                  </p>
                </div>
                <div>
                  <label className="text-sm font-medium mb-1 block">Kurum Faturası Zamanlaması</label>
                  <select
                    value={form.institutionPayOnInvoice ? 'invoice' : 'payment'}
                    onChange={e => update('institutionPayOnInvoice', e.target.value === 'invoice')}
                    className={SELECT_CLS}
                  >
                    <option value="payment">Kurum Ödeyince (Güvenli)</option>
                    <option value="invoice">Fatura Kesilince (Riskli)</option>
                  </select>
                  <p className="text-xs text-muted-foreground mt-1">
                    Kurum anlaşmalı hastalarda geçerli. Bireysel hastalarda fark yok.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Ödeme Tipi */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Hakediş Hesaplama Yöntemi</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-1 block">Ödeme Tipi</label>
                <select value={form.paymentType} onChange={e => update('paymentType', e.target.value as PaymentType)} className={SELECT_CLS}>
                  <option value="Prim">Prim (%) — Net bazın yüzdesi</option>
                  <option value="Fix">Sabit — Her tedavide sabit tutar</option>
                  <option value="FixPlusPrim">Sabit + Prim — Her ikisi birden</option>
                  <option value="PerJob">İş Başı — Tedavi bazında sabit veya %</option>
                  <option value="PerJobSelectedPlusFixPrim">Seçili İş Başı + Fix/Prim</option>
                  <option value="PriceRange">Fiyat Bandı — Bedele göre değişen oran</option>
                </select>
              </div>

              {(showPrimRate || showFixedFee) && (
                <div className="grid grid-cols-2 gap-4">
                  {showFixedFee && (
                    <div>
                      <label className="text-sm font-medium mb-1 block">
                        Sabit Tutar (₺)
                        {form.paymentType === 'PerJobSelectedPlusFixPrim' && (
                          <span className="text-muted-foreground font-normal text-xs ml-1">— eşleşme yoksa</span>
                        )}
                      </label>
                      <Input
                        type="number" step="0.01" min="0"
                        value={form.fixedFee}
                        onChange={e => update('fixedFee', parseFloat(e.target.value) || 0)}
                      />
                    </div>
                  )}
                  {showPrimRate && (
                    <div>
                      <label className="text-sm font-medium mb-1 block">
                        Prim Oranı (%)
                        {form.paymentType === 'PriceRange' && (
                          <span className="text-muted-foreground font-normal text-xs ml-1">— bant yoksa fallback</span>
                        )}
                      </label>
                      <Input
                        type="number" step="0.01" min="0" max="100"
                        value={form.primRate}
                        onChange={e => update('primRate', parseFloat(e.target.value) || 0)}
                      />
                    </div>
                  )}
                </div>
              )}

              {/* İş Başı Fiyatları */}
              {showJobStartPrices && (
                <div className="space-y-3 pt-2">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium">Tedavi Bazlı İş Başı Fiyatları</p>
                      <p className="text-xs text-muted-foreground">
                        {form.paymentType === 'PerJobSelectedPlusFixPrim'
                          ? 'Listede olmayan tedaviler Fix+Prim formülüne düşer.'
                          : 'Listede olmayan tedaviler normal prim oranı ile hesaplanır.'}
                      </p>
                    </div>
                    <Button type="button" size="sm" variant="outline" onClick={addJobStartPrice} disabled={treatments.length === 0}>
                      <Plus className="h-3 w-3 mr-1" /> Tedavi Ekle
                    </Button>
                  </div>

                  {form.jobStartPrices.length === 0 ? (
                    <div className="border-2 border-dashed rounded-lg p-6 text-center text-muted-foreground text-sm">
                      Henüz tedavi eklenmedi. "Tedavi Ekle" butonuna tıklayın.
                    </div>
                  ) : (
                    <div className="border rounded-lg overflow-hidden">
                      <div className="grid grid-cols-[1fr_160px_140px_40px] gap-0 bg-muted px-3 py-2 text-xs font-medium text-muted-foreground">
                        <span>Tedavi</span>
                        <span>Tip</span>
                        <span>Değer</span>
                        <span></span>
                      </div>
                      {form.jobStartPrices.map((jp, idx) => (
                        <div key={idx} className="grid grid-cols-[1fr_160px_140px_40px] gap-0 items-center border-t px-3 py-2">
                          <select
                            value={jp.treatmentId}
                            onChange={e => updateJobStartPrice(idx, { treatmentId: parseInt(e.target.value) })}
                            className="h-8 rounded border border-input bg-background px-2 text-sm mr-2"
                          >
                            {treatments.map(t => (
                              <option key={t.id} value={t.id}>{t.code} — {t.name}</option>
                            ))}
                          </select>
                          <select
                            value={jp.priceType}
                            onChange={e => updateJobStartPrice(idx, { priceType: e.target.value as 'FixedAmount' | 'Percentage' })}
                            className="h-8 rounded border border-input bg-background px-2 text-sm mr-2"
                          >
                            <option value="FixedAmount">Sabit (₺)</option>
                            <option value="Percentage">Yüzde (%)</option>
                          </select>
                          <Input
                            type="number" step="0.01" min="0"
                            value={jp.value}
                            onChange={e => updateJobStartPrice(idx, { value: parseFloat(e.target.value) || 0 })}
                            placeholder={jp.priceType === 'Percentage' ? '% oran' : '₺ tutar'}
                            className="h-8 mr-2"
                          />
                          <Button type="button" size="icon" variant="ghost" className="h-8 w-8" onClick={() => removeJobStartPrice(idx)}>
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Fiyat Bantları */}
              {showPriceRanges && (
                <div className="space-y-3 pt-2">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium">Fiyat Bantları</p>
                      <p className="text-xs text-muted-foreground">
                        Brüt tedavi bedeline göre prim oranı. İlk eşleşen bant kullanılır.
                      </p>
                    </div>
                    <Button type="button" size="sm" variant="outline" onClick={addPriceRange}>
                      <Plus className="h-3 w-3 mr-1" /> Bant Ekle
                    </Button>
                  </div>

                  {form.priceRanges.length === 0 ? (
                    <div className="border-2 border-dashed rounded-lg p-6 text-center text-muted-foreground text-sm">
                      Hiç bant tanımlı değil. "Bant Ekle" ile başlayın.
                    </div>
                  ) : (
                    <div className="border rounded-lg overflow-hidden">
                      <div className="grid grid-cols-[1fr_1fr_120px_40px] gap-0 bg-muted px-3 py-2 text-xs font-medium text-muted-foreground">
                        <span>Min tutar (₺, dahil)</span>
                        <span>Max tutar (₺, hariç) — boş = üzeri</span>
                        <span>Oran (%)</span>
                        <span></span>
                      </div>
                      {form.priceRanges.map((pr, idx) => (
                        <div key={idx} className="grid grid-cols-[1fr_1fr_120px_40px] gap-0 items-center border-t px-3 py-2">
                          <Input
                            type="number" step="1" min="0"
                            value={pr.minAmount}
                            onChange={e => updatePriceRange(idx, { minAmount: parseFloat(e.target.value) || 0 })}
                            placeholder="0"
                            className="h-8 mr-2"
                          />
                          <Input
                            type="number" step="1" min="0"
                            value={pr.maxAmount ?? ''}
                            onChange={e => updatePriceRange(idx, { maxAmount: e.target.value ? parseFloat(e.target.value) : null })}
                            placeholder="üst sınır yok"
                            className="h-8 mr-2"
                          />
                          <Input
                            type="number" step="0.01" min="0" max="100"
                            value={pr.rate}
                            onChange={e => updatePriceRange(idx, { rate: parseFloat(e.target.value) || 0 })}
                            placeholder="%"
                            className="h-8 mr-2"
                          />
                          <Button type="button" size="icon" variant="ghost" className="h-8 w-8" onClick={() => removePriceRange(idx)}>
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Kesintiler */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Kesintiler (Net Baz Hesabı)</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <p className="text-xs text-muted-foreground">
                Kesintiler brüt tutardan düşülerek <strong>net baz</strong> hesaplanır. Prim net baz üzerinden uygulanır.
              </p>
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.deductLabCost} onCheckedChange={v => update('deductLabCost', !!v)} />
                Laboratuvar masrafını düş
              </label>
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.deductTreatmentCost} onCheckedChange={v => update('deductTreatmentCost', !!v)} />
                Tedavi maliyetini düş (Treatment.CostPrice)
              </label>
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.deductTreatmentPlanCommission} onCheckedChange={v => update('deductTreatmentPlanCommission', !!v)} />
                Tedavi planı komisyonunu düş
              </label>
              <p className="text-xs text-muted-foreground border-t pt-2">
                POS/Kredi Kartı komisyonu kural gereği her zaman düşülür (%1.75).
              </p>
              <div className="border-t pt-3">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <Checkbox checked={form.requireLabApproval} onCheckedChange={v => update('requireLabApproval', !!v)} />
                  Lab onayı zorunlu (hakediş hesaplaması için)
                </label>
                <p className="text-xs text-muted-foreground mt-1 ml-6">
                  Aktif: Tedaviye bağlı lab işi onaylanmadan hakediş hesaplanamaz.
                </p>
              </div>

              {form.extraExpenseEnabled && (
                <div className="pt-2">
                  <label className="text-xs font-medium mb-1 block">Ekstra Gider Oranı (%)</label>
                  <Input
                    type="number" step="0.01" min="0" className="w-40"
                    placeholder="Oran"
                    value={form.extraExpenseRate ?? ''}
                    onChange={e => update('extraExpenseRate', e.target.value ? parseFloat(e.target.value) : null)}
                  />
                </div>
              )}
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.extraExpenseEnabled} onCheckedChange={v => update('extraExpenseEnabled', !!v)} />
                Ekstra gider yüzdesi uygula (net bazdan düşülür)
              </label>
            </CardContent>
          </Card>
        </div>

        {/* Sağ kolon: Hedefler + Vergiler */}
        <div className="space-y-5">

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Hedef Sistemi</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-xs text-muted-foreground">
                Hedef aşıldığında en yüksek bonus oranı primRate yerine uygulanır.
              </p>
              <div className="space-y-2">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <Checkbox checked={form.doctorTargetEnabled} onCheckedChange={v => update('doctorTargetEnabled', !!v)} />
                  Hekim aylık hedefi
                </label>
                {form.doctorTargetEnabled && (
                  <div className="pl-6">
                    <label className="text-xs mb-1 block">Bonus Prim (%)</label>
                    <Input
                      type="number" step="0.01" min="0" max="100"
                      placeholder="Örn: 35"
                      value={form.doctorTargetBonusRate ?? ''}
                      onChange={e => update('doctorTargetBonusRate', e.target.value ? parseFloat(e.target.value) : null)}
                    />
                  </div>
                )}
              </div>
              <div className="space-y-2">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <Checkbox checked={form.clinicTargetEnabled} onCheckedChange={v => update('clinicTargetEnabled', !!v)} />
                  Klinik aylık hedefi
                </label>
                {form.clinicTargetEnabled && (
                  <div className="pl-6">
                    <label className="text-xs mb-1 block">Bonus Prim (%)</label>
                    <Input
                      type="number" step="0.01" min="0" max="100"
                      placeholder="Örn: 32"
                      value={form.clinicTargetBonusRate ?? ''}
                      onChange={e => update('clinicTargetBonusRate', e.target.value ? parseFloat(e.target.value) : null)}
                    />
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Vergiler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.kdvEnabled} onCheckedChange={v => update('kdvEnabled', !!v)} />
                KDV uygula
              </label>
              {form.kdvEnabled && (
                <div className="space-y-3 pl-6">
                  <div>
                    <label className="text-xs mb-1 block">KDV Oranı (%)</label>
                    <Input
                      type="number" step="0.1" min="0" className="w-32"
                      placeholder="Örn: 20"
                      value={form.kdvRate ?? ''}
                      onChange={e => update('kdvRate', e.target.value ? parseFloat(e.target.value) : null)}
                    />
                  </div>
                  <div>
                    <label className="text-xs mb-1 block">Uygulanan Ödeme Tipleri (JSON)</label>
                    <Input
                      placeholder='[1,2] — boş=hepsi'
                      value={form.kdvAppliedPaymentTypes ?? ''}
                      onChange={e => update('kdvAppliedPaymentTypes', e.target.value || null)}
                    />
                    <p className="text-xs text-muted-foreground mt-1">
                      1=Nakit, 2=K.Kartı, 3=Havale, 4=Taksit, 5=Çek
                    </p>
                  </div>
                </div>
              )}

              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={form.withholdingTaxEnabled} onCheckedChange={v => update('withholdingTaxEnabled', !!v)} />
                Stopaj uygula
              </label>
              {form.withholdingTaxEnabled && (
                <div className="pl-6">
                  <label className="text-xs mb-1 block">Stopaj Oranı (%)</label>
                  <Input
                    type="number" step="0.1" min="0" className="w-32"
                    placeholder="Örn: 10"
                    value={form.withholdingTaxRate ?? ''}
                    onChange={e => update('withholdingTaxRate', e.target.value ? parseFloat(e.target.value) : null)}
                  />
                </div>
              )}
            </CardContent>
          </Card>

          {/* Kaydet */}
          <div className="flex gap-3">
            <Button variant="outline" onClick={onBack} className="flex-1">
              <ArrowLeft className="h-4 w-4 mr-1" /> Vazgeç
            </Button>
            <Button
              onClick={() => save.mutate()}
              disabled={!form.name.trim() || save.isPending}
              className="flex-1"
            >
              {save.isPending ? 'Kaydediliyor…' : (template ? 'Güncelle' : 'Oluştur')}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
