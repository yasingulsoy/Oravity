import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search, Plus, Pencil, Check, X, ChevronLeft, ChevronRight,
  Tag, Zap, ListChecks, AlertCircle, Building2,
} from 'lucide-react';
import { toast } from 'sonner';
import { pricingApi, type PricingRule, type ReferencePriceList, type BranchPricing } from '@/api/pricing';
import { institutionsApi } from '@/api/institutions';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

// ─── Referans Fiyatlar Tab ──────────────────────────────────────────────────

function ReferencePricesTab() {
  const qc = useQueryClient();
  const [selectedList, setSelectedList] = useState<ReferencePriceList | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editPrice, setEditPrice] = useState('');
  const [editKdv, setEditKdv] = useState('');

  const PAGE_SIZE = 50;

  const { data: lists, isLoading: listsLoading } = useQuery({
    queryKey: ['pricing', 'reference-lists'],
    queryFn: () => pricingApi.getReferenceLists().then(r => r.data),
  });

  // Auto-select first list
  useEffect(() => {
    if (lists && lists.length > 0 && !selectedList) {
      setSelectedList(lists[0]);
    }
  }, [lists, selectedList]);

  const { data: itemsPage, isLoading: itemsLoading } = useQuery({
    queryKey: ['pricing', 'reference-items', selectedList?.id, search, page],
    queryFn: () => pricingApi.getReferenceItems(selectedList!.id, { search, page, pageSize: PAGE_SIZE }).then(r => r.data),
    enabled: !!selectedList,
  });

  const upsertMutation = useMutation({
    mutationFn: ({ code, name, price, priceKdv }: {
      code: string; name: string; price: number; priceKdv: number;
    }) => pricingApi.upsertReferenceItem(selectedList!.id, code, { treatmentName: name, price, priceKdv }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-items'] });
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
      setEditingId(null);
      toast.success('Fiyat güncellendi');
    },
    onError: () => toast.error('Fiyat güncellenemedi'),
  });

  const items = itemsPage?.items ?? [];
  const total = itemsPage?.total ?? 0;
  const totalPages = Math.ceil(total / PAGE_SIZE);

  function startEdit(item: { id: number; price: number; priceKdv: number }) {
    setEditingId(item.id);
    setEditPrice(String(item.price));
    setEditKdv(String(item.priceKdv));
  }

  function saveEdit(item: { id: number; treatmentCode: string; treatmentName: string }) {
    const price = parseFloat(editPrice);
    const priceKdv = parseFloat(editKdv) || 0;
    if (isNaN(price)) return;
    upsertMutation.mutate({ code: item.treatmentCode, name: item.treatmentName, price, priceKdv });
  }

  return (
    <div className="flex gap-4 h-[calc(100vh-200px)]">
      {/* List selector */}
      <div className="w-56 shrink-0 flex flex-col gap-1 border rounded-lg p-2 overflow-y-auto">
        <p className="text-xs font-medium text-muted-foreground px-2 py-1">Listeler</p>
        {listsLoading
          ? Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)
          : lists?.map(list => (
            <button
              key={list.id}
              onClick={() => { setSelectedList(list); setPage(1); setSearch(''); }}
              className={`w-full text-left rounded-md px-3 py-2 text-sm transition-colors ${
                selectedList?.id === list.id
                  ? 'bg-accent text-accent-foreground font-medium'
                  : 'hover:bg-accent/50 text-muted-foreground'
              }`}
            >
              <div className="font-medium">{list.code}</div>
              <div className="text-xs opacity-70">{list.name}</div>
              <div className="text-xs opacity-50 mt-0.5">{list.itemCount} kalem</div>
            </button>
          ))}
      </div>

      {/* Items table */}
      <div className="flex-1 flex flex-col gap-3 min-w-0">
        {selectedList && (
          <>
            <div className="flex items-center gap-2">
              <div className="relative flex-1 max-w-sm">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Kod veya isim ara..."
                  value={search}
                  onChange={e => { setSearch(e.target.value); setPage(1); }}
                  className="pl-8"
                />
              </div>
              <span className="text-sm text-muted-foreground ml-auto">{total} kalem</span>
            </div>

            <div className="border rounded-lg overflow-auto flex-1">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-28">Kod</TableHead>
                    <TableHead>Tedavi Adı</TableHead>
                    <TableHead className="w-32 text-right">Fiyat (₺)</TableHead>
                    <TableHead className="w-32 text-right">KDV'li (₺)</TableHead>
                    <TableHead className="w-20"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {itemsLoading
                    ? Array.from({ length: 10 }).map((_, i) => (
                      <TableRow key={i}>
                        {Array.from({ length: 5 }).map((__, j) => (
                          <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                        ))}
                      </TableRow>
                    ))
                    : items.map(item => (
                      <TableRow key={item.id}>
                        <TableCell className="font-mono text-xs">{item.treatmentCode}</TableCell>
                        <TableCell className="text-sm">{item.treatmentName}</TableCell>
                        <TableCell className="text-right">
                          {editingId === item.id
                            ? <Input
                                autoFocus
                                className="h-7 w-24 text-right ml-auto"
                                value={editPrice}
                                onChange={e => setEditPrice(e.target.value)}
                                onKeyDown={e => {
                                  if (e.key === 'Enter') saveEdit(item);
                                  if (e.key === 'Escape') setEditingId(null);
                                }}
                              />
                            : <span className="tabular-nums">{item.price.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
                          }
                        </TableCell>
                        <TableCell className="text-right">
                          {editingId === item.id
                            ? <Input
                                className="h-7 w-24 text-right ml-auto"
                                value={editKdv}
                                onChange={e => setEditKdv(e.target.value)}
                                onKeyDown={e => {
                                  if (e.key === 'Enter') saveEdit(item);
                                  if (e.key === 'Escape') setEditingId(null);
                                }}
                              />
                            : <span className="tabular-nums">{item.priceKdv.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
                          }
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            {editingId === item.id ? (
                              <>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  className="h-6 w-6 text-green-600"
                                  onClick={() => saveEdit(item)}
                                  disabled={upsertMutation.isPending}
                                >
                                  <Check className="h-3.5 w-3.5" />
                                </Button>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  className="h-6 w-6"
                                  onClick={() => setEditingId(null)}
                                >
                                  <X className="h-3.5 w-3.5" />
                                </Button>
                              </>
                            ) : (
                              <Button
                                size="icon"
                                variant="ghost"
                                className="h-6 w-6"
                                onClick={() => startEdit(item)}
                              >
                                <Pencil className="h-3.5 w-3.5" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  }
                </TableBody>
              </Table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between text-sm text-muted-foreground">
                <span>Sayfa {page} / {totalPages}</span>
                <div className="flex gap-1">
                  <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => setPage(p => p - 1)} disabled={page === 1}>
                    <ChevronLeft className="h-4 w-4" />
                  </Button>
                  <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => setPage(p => p + 1)} disabled={page === totalPages}>
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

// ─── Pricing Rules Tab ──────────────────────────────────────────────────────

const RULE_TYPES = [
  { value: 'formula',    label: 'Formül',     icon: Zap },
  { value: 'percentage', label: 'Yüzde',      icon: Tag },
  { value: 'fixed',      label: 'Sabit Fiyat', icon: ListChecks },
] as const;

const FORMULA_VARS = [
  { key: 'TDB',   desc: 'TDB referans fiyatı (TDB_2026 listesi)' },
  { key: 'SUT',   desc: 'SUT (SGK) fiyatı' },
  { key: 'CARI',  desc: 'Cari liste fiyatı (CARI_2026 listesi)' },
  { key: 'ISAK',  desc: 'Anlaşmalı kurum (1=evet, 0=hayır)' },
  { key: 'MULTI', desc: 'Şube fiyat çarpanı (Şube Ayarları\'ndan)' },
  { key: 'MIN',   desc: 'MIN(A, B) — küçük olanı seç' },
  { key: 'MAX',   desc: 'MAX(A, B) — büyük olanı seç' },
];

const FORMULA_EXAMPLES = [
  'TDB * 2.5',
  'MIN(CARI * 0.80, TDB * 0.80)',
  'MIN(CARI * MULTI * 0.80, TDB * 0.80)',
  'ISAK==1 ? TDB * 0.80 : CARI * MULTI',
];

// Kural koşulunu IncludeFilters JSON'una dönüştür
function buildIncludeFilters(opts: {
  institutionIds: number[];
  ossOnly: boolean;
  campaignCode: string;
}): string | null {
  const obj: Record<string, unknown> = {};
  if (opts.institutionIds.length > 0) obj['institutionIds'] = opts.institutionIds;
  if (opts.ossOnly)                   obj['ossOnly']        = true;
  if (opts.campaignCode.trim())       obj['campaignCodes']  = [opts.campaignCode.trim()];
  return Object.keys(obj).length > 0 ? JSON.stringify(obj) : null;
}

// IncludeFilters JSON'undan koşulları parse et
function parseIncludeFilters(json: string | null) {
  const defaults = { institutionIds: [] as number[], ossOnly: false, campaignCode: '' };
  if (!json) return defaults;
  try {
    const obj = JSON.parse(json);
    return {
      institutionIds: Array.isArray(obj.institutionIds) ? (obj.institutionIds as number[]) : [],
      ossOnly:        !!obj.ossOnly,
      campaignCode:   Array.isArray(obj.campaignCodes) ? (obj.campaignCodes[0] ?? '') : '',
    };
  } catch { return defaults; }
}

// ─── Institution Picker ─────────────────────────────────────────────────────

function InstitutionPicker({ selected, onChange }: {
  selected: number[];
  onChange: (ids: number[]) => void;
}) {
  const { data } = useQuery({
    queryKey: ['institutions'],
    queryFn: () => institutionsApi.getAll().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });

  const institutions = data ?? [];

  function toggle(id: number) {
    onChange(selected.includes(id) ? selected.filter(x => x !== id) : [...selected, id]);
  }

  const selectedNames = institutions
    .filter(i => selected.includes(i.id))
    .map(i => i.name);

  return (
    <div className="space-y-1.5">
      <Label className="text-sm">Anlaşmalı Kurumlar <span className="text-muted-foreground font-normal">(boş = tüm hastalar)</span></Label>
      <div className="flex flex-wrap gap-1.5">
        {institutions.filter(i => i.isActive).map(inst => (
          <button
            key={inst.id}
            type="button"
            onClick={() => toggle(inst.id)}
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium border transition-colors ${
              selected.includes(inst.id)
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-background text-muted-foreground border-border hover:bg-accent'
            }`}
          >
            {inst.name}
          </button>
        ))}
        {institutions.length === 0 && (
          <span className="text-xs text-muted-foreground">Kurum yükleniyor...</span>
        )}
      </div>
      {selectedNames.length > 0 && (
        <p className="text-xs text-muted-foreground">Seçili: {selectedNames.join(', ')}</p>
      )}
    </div>
  );
}

interface RuleDialogProps {
  open: boolean;
  onClose: () => void;
  editing?: PricingRule | null;
}

function RuleDialog({ open, onClose, editing }: RuleDialogProps) {
  const qc = useQueryClient();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [ruleType, setRuleType] = useState<string>('formula');
  const [priority, setPriority] = useState('10');
  const [formula, setFormula] = useState('');
  const [outputCurrency, setOutputCurrency] = useState('TRY');
  const [stopProcessing, setStopProcessing] = useState(true);
  const [isActive, setIsActive] = useState(true);
  // Koşul kriterleri
  const [institutionIds, setInstitutionIds] = useState<number[]>([]);
  const [ossOnly, setOssOnly] = useState(false);
  const [campaignCode, setCampaignCode] = useState('');

  useEffect(() => {
    if (editing) {
      setName(editing.name);
      setDescription(editing.description ?? '');
      setRuleType(editing.ruleType);
      setPriority(String(editing.priority));
      setFormula(editing.formula ?? '');
      setOutputCurrency(editing.outputCurrency);
      setStopProcessing(editing.stopProcessing);
      setIsActive(editing.isActive);
      const parsed = parseIncludeFilters(editing.includeFilters);
      setInstitutionIds(parsed.institutionIds);
      setOssOnly(parsed.ossOnly);
      setCampaignCode(parsed.campaignCode);
    } else {
      setName('');
      setDescription('');
      setRuleType('formula');
      setPriority('10');
      setFormula('');
      setOutputCurrency('TRY');
      setStopProcessing(true);
      setIsActive(true);
      setInstitutionIds([]);
      setOssOnly(false);
      setCampaignCode('');
    }
  }, [editing, open]);

  function buildFilters() {
    return buildIncludeFilters({ institutionIds, ossOnly, campaignCode });
  }

  const createMutation = useMutation({
    mutationFn: () => pricingApi.createRule({
      name, description: description || null,
      ruleType, priority: parseInt(priority) || 10,
      formula: formula || null,
      outputCurrency,
      stopProcessing,
      branchId: null,
      includeFilters: buildFilters(),
      excludeFilters: null,
      validFrom: null, validUntil: null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'rules'] });
      toast.success('Kural oluşturuldu');
      onClose();
    },
    onError: () => toast.error('Kural oluşturulamadı'),
  });

  const updateMutation = useMutation({
    mutationFn: () => pricingApi.updateRule(editing!.publicId, {
      name, description: description || null,
      ruleType, priority: parseInt(priority) || 10,
      formula: formula || null,
      outputCurrency,
      stopProcessing,
      isActive,
      branchId: null,
      includeFilters: buildFilters(),
      excludeFilters: null,
      validFrom: null, validUntil: null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'rules'] });
      toast.success('Kural güncellendi');
      onClose();
    },
    onError: () => toast.error('Kural güncellenemedi'),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  function handleSave() {
    if (!name.trim()) { toast.error('Kural adı zorunlu'); return; }
    if (ruleType === 'formula' && !formula.trim()) { toast.error('Formül zorunlu'); return; }
    editing ? updateMutation.mutate() : createMutation.mutate();
  }

  function insertVar(v: string) {
    setFormula(f => f + (f && !f.endsWith(' ') ? ' ' : '') + v);
  }

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{editing ? 'Kural Düzenle' : 'Yeni Fiyat Kuralı'}</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-2 gap-4">
          {/* Name */}
          <div className="col-span-2 space-y-1">
            <Label>Kural Adı</Label>
            <Input
              placeholder="Örn: TDB x2.5 - Standart"
              value={name}
              onChange={e => setName(e.target.value)}
            />
          </div>

          {/* Description */}
          <div className="col-span-2 space-y-1">
            <Label>Açıklama (opsiyonel)</Label>
            <Input
              placeholder="Bu kural ne zaman uygulanır?"
              value={description}
              onChange={e => setDescription(e.target.value)}
            />
          </div>

          {/* Rule type */}
          <div className="space-y-1">
            <Label>Kural Tipi</Label>
            <Select value={ruleType} onValueChange={setRuleType}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {RULE_TYPES.map(t => (
                  <SelectItem key={t.value} value={t.value}>
                    <span className="flex items-center gap-2">
                      <t.icon className="h-3.5 w-3.5" />
                      {t.label}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Priority */}
          <div className="space-y-1">
            <Label>Öncelik <span className="text-muted-foreground text-xs">(küçük = önce)</span></Label>
            <Input
              type="number"
              min={1}
              value={priority}
              onChange={e => setPriority(e.target.value)}
            />
          </div>

          {/* Output Currency */}
          <div className="space-y-1">
            <Label>Çıktı Para Birimi</Label>
            <Select value={outputCurrency} onValueChange={setOutputCurrency}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="TRY">TRY — Türk Lirası</SelectItem>
                <SelectItem value="USD">USD — Dolar</SelectItem>
                <SelectItem value="EUR">EUR — Euro</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Flags */}
          <div className="flex flex-col justify-end gap-2">
            <div className="flex items-center gap-2">
              <Checkbox
                id="stopProcessing"
                checked={stopProcessing}
                onCheckedChange={v => setStopProcessing(!!v)}
              />
              <Label htmlFor="stopProcessing" className="font-normal cursor-pointer">
                Eşleşince dur (sonraki kuralları atla)
              </Label>
            </div>
            {editing && (
              <div className="flex items-center gap-2">
                <Checkbox
                  id="isActive"
                  checked={isActive}
                  onCheckedChange={v => setIsActive(!!v)}
                />
                <Label htmlFor="isActive" className="font-normal cursor-pointer">Aktif</Label>
              </div>
            )}
          </div>

          {/* Uygulama Koşulları */}
          <div className="col-span-2 space-y-2">
            <Label className="text-sm">Uygulama Koşulları <span className="text-muted-foreground font-normal">(boş = her hastaya)</span></Label>
            <div className="border rounded-lg p-3 space-y-3 bg-muted/30">

              {/* Kurum seçimi */}
              <InstitutionPicker
                selected={institutionIds}
                onChange={setInstitutionIds}
              />

              <div className="flex items-center gap-2">
                <Checkbox
                  id="ossOnly"
                  checked={ossOnly}
                  onCheckedChange={v => setOssOnly(!!v)}
                />
                <Label htmlFor="ossOnly" className="font-normal cursor-pointer text-sm">
                  ÖSS Kapsamı
                </Label>
              </div>

              <div className="flex items-center gap-2">
                <Label className="text-sm whitespace-nowrap shrink-0">Kampanya Kodu:</Label>
                <Input
                  placeholder="Örn: YAZ2026 (boş = kampanyasız da uygula)"
                  value={campaignCode}
                  onChange={e => setCampaignCode(e.target.value.toUpperCase())}
                  className="h-7 text-sm font-mono"
                />
              </div>

              {(institutionIds.length > 0 || ossOnly || campaignCode.trim()) && (
                <p className="text-xs text-amber-600">
                  Bu kural yalnızca:{' '}
                  {[
                    institutionIds.length > 0 && `seçili ${institutionIds.length} kurum hastası`,
                    ossOnly && 'ÖSS kapsamındakiler',
                    campaignCode.trim() && `"${campaignCode.trim()}" kampanyası aktifken`,
                  ].filter(Boolean).join(' + ')}
                  {' '}uygulanır.
                </p>
              )}
            </div>
          </div>

          {/* Formula */}
          {ruleType === 'formula' && (
            <div className="col-span-2 space-y-2">
              <Label>Formül</Label>
              <Textarea
                className="font-mono text-sm h-20 resize-none"
                placeholder="TDB * 2.5"
                value={formula}
                onChange={e => setFormula(e.target.value)}
              />

              {/* Variable hints */}
              <div className="border rounded-lg p-3 bg-muted/40 space-y-2">
                <p className="text-xs font-medium text-muted-foreground">Değişkenler</p>
                <div className="flex flex-wrap gap-1.5">
                  {FORMULA_VARS.map(v => (
                    <button
                      key={v.key}
                      type="button"
                      onClick={() => insertVar(v.key)}
                      className="inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono bg-background border hover:bg-accent transition-colors"
                      title={v.desc}
                    >
                      {v.key}
                    </button>
                  ))}
                </div>
                <div className="flex flex-wrap gap-1.5">
                  <p className="text-xs text-muted-foreground w-full">Örnekler:</p>
                  {FORMULA_EXAMPLES.map(ex => (
                    <button
                      key={ex}
                      type="button"
                      onClick={() => setFormula(ex)}
                      className="inline-flex items-center rounded px-2 py-0.5 text-xs font-mono bg-background border hover:bg-accent transition-colors"
                    >
                      {ex}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Percentage / fixed */}
          {(ruleType === 'percentage' || ruleType === 'fixed') && (
            <div className="col-span-2 space-y-1">
              <Label>
                {ruleType === 'percentage' ? 'Çarpan (örn: 2.5 = TDB x 2.5)' : 'Sabit Fiyat (₺)'}
              </Label>
              <Input
                type="number"
                step="0.01"
                placeholder={ruleType === 'percentage' ? '2.5' : '500'}
                value={formula}
                onChange={e => setFormula(e.target.value)}
              />
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isPending}>İptal</Button>
          <Button onClick={handleSave} disabled={isPending}>
            {isPending ? 'Kaydediliyor...' : (editing ? 'Güncelle' : 'Oluştur')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function PricingRulesTab() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<PricingRule | null>(null);

  const { data: rules, isLoading } = useQuery({
    queryKey: ['pricing', 'rules'],
    queryFn: () => pricingApi.getRules(false).then(r => r.data),
  });

  function openCreate() {
    setEditingRule(null);
    setDialogOpen(true);
  }

  function openEdit(rule: PricingRule) {
    setEditingRule(rule);
    setDialogOpen(true);
  }

  const ruleTypeLabel: Record<string, string> = {
    formula: 'Formül',
    percentage: 'Yüzde',
    fixed: 'Sabit',
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <AlertCircle className="h-4 w-4" />
          Kurallar öncelik sırasıyla değerlendirilir. Küçük numara = daha önce uygulanır.
        </div>
        <Button size="sm" onClick={openCreate}>
          <Plus className="h-4 w-4 mr-1.5" />
          Yeni Kural
        </Button>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-16">Öncelik</TableHead>
              <TableHead>Kural Adı</TableHead>
              <TableHead className="w-24">Tip</TableHead>
              <TableHead>Formül / Değer</TableHead>
              <TableHead className="w-20">Para Birimi</TableHead>
              <TableHead className="w-20">Durum</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 7 }).map((__, j) => (
                    <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
              : (rules ?? []).length === 0
                ? (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center text-muted-foreground py-10">
                      Henüz fiyatlandırma kuralı yok. "Yeni Kural" ile başlayın.
                    </TableCell>
                  </TableRow>
                )
                : (rules ?? [])
                    .sort((a, b) => a.priority - b.priority)
                    .map(rule => (
                      <TableRow key={rule.publicId} className={!rule.isActive ? 'opacity-50' : ''}>
                        <TableCell className="tabular-nums font-medium text-center">{rule.priority}</TableCell>
                        <TableCell>
                          <div className="font-medium text-sm">{rule.name}</div>
                          {rule.description && (
                            <div className="text-xs text-muted-foreground">{rule.description}</div>
                          )}
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className="text-xs">
                            {ruleTypeLabel[rule.ruleType] ?? rule.ruleType}
                          </Badge>
                        </TableCell>
                        <TableCell className="font-mono text-xs max-w-[200px] truncate">
                          {rule.formula ?? '—'}
                        </TableCell>
                        <TableCell className="text-xs">{rule.outputCurrency}</TableCell>
                        <TableCell>
                          <Badge variant={rule.isActive ? 'default' : 'secondary'} className="text-xs">
                            {rule.isActive ? 'Aktif' : 'Pasif'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => openEdit(rule)}>
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
            }
          </TableBody>
        </Table>
      </div>

      <RuleDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        editing={editingRule}
      />
    </div>
  );
}

// ─── New Reference List Dialog ──────────────────────────────────────────────

function NewListDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [sourceType, setSourceType] = useState('private');
  const [year, setYear] = useState(String(new Date().getFullYear()));

  useEffect(() => {
    if (open) { setCode(''); setName(''); setSourceType('private'); setYear(String(new Date().getFullYear())); }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => pricingApi.createReferenceList({ code, name, sourceType, year: parseInt(year) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
      toast.success('Liste oluşturuldu');
      onClose();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Liste oluşturulamadı');
    },
  });

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader><DialogTitle>Yeni Referans Listesi</DialogTitle></DialogHeader>
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Kod <span className="text-xs text-muted-foreground">(büyük harf)</span></Label>
              <Input placeholder="CARI_2026" value={code} onChange={e => setCode(e.target.value.toUpperCase())} />
            </div>
            <div className="space-y-1">
              <Label>Yıl</Label>
              <Input type="number" value={year} onChange={e => setYear(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label>Liste Adı</Label>
            <Input placeholder="Klinik Cari Fiyat Listesi 2026" value={name} onChange={e => setName(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Tip</Label>
            <Select value={sourceType} onValueChange={setSourceType}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="private">Klinik (private)</SelectItem>
                <SelectItem value="insurance">Sigorta</SelectItem>
                <SelectItem value="SUT">SUT (SGK)</SelectItem>
                <SelectItem value="manual">Manuel</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="rounded-lg bg-muted/40 border p-3 text-xs text-muted-foreground space-y-1">
            <p className="font-medium">İpucu:</p>
            <p>• <span className="font-mono">CARI_2026</span> — formüllerde <span className="font-mono">CARI</span> değişkeni bu listeden gelir</p>
            <p>• <span className="font-mono">SUT_2026</span> — formüllerde <span className="font-mono">SUT</span> değişkeni</p>
            <p>• <span className="font-mono">THY_2026</span> — kuruma özel liste</p>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={mutation.isPending}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !code || !name}>
            {mutation.isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Branch Settings Tab ────────────────────────────────────────────────────

function BranchSettingsTab() {
  const qc = useQueryClient();
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editMultiplier, setEditMultiplier] = useState('');

  const { data: branches, isLoading } = useQuery({
    queryKey: ['pricing', 'branches'],
    queryFn: () => pricingApi.getBranchPricing().then(r => r.data),
  });

  const updateMutation = useMutation({
    mutationFn: ({ branchId, multiplier }: { branchId: number; multiplier: number }) =>
      pricingApi.updateBranchMultiplier(branchId, multiplier),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'branches'] });
      setEditingId(null);
      toast.success('Şube fiyat çarpanı güncellendi');
    },
    onError: () => toast.error('Güncelleme başarısız'),
  });

  function saveMultiplier(branch: BranchPricing) {
    const val = parseFloat(editMultiplier);
    if (isNaN(val) || val <= 0) { toast.error('Geçerli bir çarpan girin'); return; }
    updateMutation.mutate({ branchId: branch.branchId, multiplier: val });
  }

  return (
    <div className="space-y-4 max-w-2xl">
      <div className="rounded-lg border bg-muted/30 p-4 text-sm space-y-2">
        <p className="font-medium">Fiyat Çarpanı (MULTI) Nedir?</p>
        <p className="text-muted-foreground">
          Formüllerde <span className="font-mono bg-background border rounded px-1">MULTI</span> değişkeni şubeye özel cari fiyat katsayısını temsil eder.
          Örneğin Bodrum şubesine <span className="font-mono">1.10</span> girilirse,{' '}
          <span className="font-mono">CARI * MULTI</span> formülü Bodrum'da{' '}
          <span className="font-mono">CARI × 1.10</span> olarak hesaplanır.
        </p>
        <p className="text-muted-foreground">
          THY anlaşması için önerilen formül:{' '}
          <span className="font-mono bg-background border rounded px-1">MIN(CARI * MULTI * 0.80, TDB * 0.80)</span>
        </p>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Şube</TableHead>
              <TableHead className="w-48 text-right">Fiyat Çarpanı (MULTI)</TableHead>
              <TableHead className="w-48 text-muted-foreground text-xs">Etki</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 4 }).map((__, j) => <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>)}
                </TableRow>
              ))
              : (branches ?? []).map(branch => (
                <TableRow key={branch.branchId}>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-2">
                      <Building2 className="h-4 w-4 text-muted-foreground" />
                      {branch.branchName}
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    {editingId === branch.branchId
                      ? <div className="flex items-center justify-end gap-1">
                          <Input
                            autoFocus
                            type="number"
                            step="0.01"
                            className="h-7 w-24 text-right"
                            value={editMultiplier}
                            onChange={e => setEditMultiplier(e.target.value)}
                            onKeyDown={e => {
                              if (e.key === 'Enter') saveMultiplier(branch);
                              if (e.key === 'Escape') setEditingId(null);
                            }}
                          />
                        </div>
                      : <span className={`tabular-nums font-mono ${branch.pricingMultiplier !== 1 ? 'text-amber-600 font-medium' : ''}`}>
                          ×{branch.pricingMultiplier.toFixed(2)}
                        </span>
                    }
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {branch.pricingMultiplier === 1
                      ? 'Standart (değişiklik yok)'
                      : `Cari fiyatlar %${((branch.pricingMultiplier - 1) * 100).toFixed(0)} ${branch.pricingMultiplier > 1 ? 'fazla' : 'az'}`
                    }
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      {editingId === branch.branchId ? (
                        <>
                          <Button size="icon" variant="ghost" className="h-6 w-6 text-green-600"
                            onClick={() => saveMultiplier(branch)} disabled={updateMutation.isPending}>
                            <Check className="h-3.5 w-3.5" />
                          </Button>
                          <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => setEditingId(null)}>
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </>
                      ) : (
                        <Button size="icon" variant="ghost" className="h-6 w-6"
                          onClick={() => { setEditingId(branch.branchId); setEditMultiplier(String(branch.pricingMultiplier)); }}>
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            }
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

// ─── Main Page ──────────────────────────────────────────────────────────────

export function PricingPage() {
  const [newListOpen, setNewListOpen] = useState(false);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Fiyatlandırma</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Referans fiyat listeleri, kurallar ve şube ayarları
          </p>
        </div>
      </div>

      <Tabs defaultValue="prices">
        <div className="flex items-center justify-between">
          <TabsList>
            <TabsTrigger value="prices">Referans Fiyatlar</TabsTrigger>
            <TabsTrigger value="rules">Fiyat Kuralları</TabsTrigger>
            <TabsTrigger value="branches">Şube Ayarları</TabsTrigger>
          </TabsList>
          <Button size="sm" variant="outline" onClick={() => setNewListOpen(true)}>
            <Plus className="h-4 w-4 mr-1.5" />
            Yeni Liste
          </Button>
        </div>

        <TabsContent value="prices" className="mt-4">
          <ReferencePricesTab />
        </TabsContent>

        <TabsContent value="rules" className="mt-4">
          <PricingRulesTab />
        </TabsContent>

        <TabsContent value="branches" className="mt-4">
          <BranchSettingsTab />
        </TabsContent>

      </Tabs>

      <NewListDialog open={newListOpen} onClose={() => setNewListOpen(false)} />
    </div>
  );
}
