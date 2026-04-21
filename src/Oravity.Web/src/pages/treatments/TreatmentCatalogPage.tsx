import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search, Plus, Pencil, Trash2, ChevronsUpDown, Link2Off, Lock,
} from 'lucide-react';
import { DentalSymbolPicker } from '@/components/DentalSymbolPicker';
import { getSymbol } from '@/types/dentalSymbols';
import { useAuthStore } from '@/store/authStore';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { toast } from 'sonner';
import { treatmentsApi, treatmentCategoriesApi, treatmentMappingsApi, type TreatmentCatalogItem, type TreatmentDetail, type TreatmentMapping, type TreatmentCategory } from '@/api/treatments';
import { pricingApi, type ReferencePriceList, type ReferencePriceItem } from '@/api/pricing';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

// ─── Mapping Row ─────────────────────────────────────────────────────────────
// One row per reference list in the edit sheet.
// Shows current mapping (if any) or "Eşleştirilmemiş" + search combobox.

interface MappingRowProps {
  list: ReferencePriceList;
  mapping: TreatmentMapping | undefined;
  treatmentPublicId: string;
}

function MappingRow({ list, mapping, treatmentPublicId }: MappingRowProps) {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  const { data: itemsPage } = useQuery({
    queryKey: ['pricing', 'ref-items-inline', list.id, search],
    queryFn: () => pricingApi.getReferenceItems(list.id, { search, pageSize: 15 }).then(r => r.data),
    enabled: open,
  });

  const createMapping = useMutation({
    mutationFn: (item: ReferencePriceItem) =>
      treatmentMappingsApi.createMapping(treatmentPublicId, {
        referenceListId: list.id,
        referenceCode: item.treatmentCode,
        mappingQuality: 'exact',
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatment-mappings', treatmentPublicId] });
      setOpen(false);
      setSearch('');
      toast.success(`${list.code} eşleştirmesi eklendi`);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Eşleştirme eklenemedi');
    },
  });

  const deleteMapping = useMutation({
    mutationFn: () => treatmentMappingsApi.deleteMapping(treatmentPublicId, mapping!.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatment-mappings', treatmentPublicId] });
      toast.success(`${list.code} eşleştirmesi kaldırıldı`);
    },
  });

  return (
    <div className="flex items-center gap-3 py-2 border-b last:border-b-0">
      <div className="w-28 shrink-0">
        <Badge variant="secondary" className="font-mono text-xs">{list.code}</Badge>
      </div>

      <div className="flex-1 min-w-0">
        {mapping ? (
          <div className="flex items-center gap-1.5">
            <span className="font-mono text-xs text-muted-foreground">{mapping.referenceCode}</span>
            {mapping.referenceItemName && (
              <span className="text-sm truncate">{mapping.referenceItemName}</span>
            )}
          </div>
        ) : (
          <span className="text-sm text-muted-foreground italic">Eşleştirilmemiş</span>
        )}
      </div>

      <div className="flex items-center gap-1 shrink-0">
        {mapping && (
          <Button
            size="icon"
            variant="ghost"
            className="h-7 w-7 text-muted-foreground hover:text-destructive"
            onClick={() => deleteMapping.mutate()}
            disabled={deleteMapping.isPending}
            title="Eşleştirmeyi kaldır"
          >
            <Link2Off className="h-3.5 w-3.5" />
          </Button>
        )}

        <Button size="sm" variant="outline" className="h-7 text-xs gap-1.5" onClick={() => setOpen(true)}>
          <ChevronsUpDown className="h-3 w-3" />
          {mapping ? 'Değiştir' : 'Eşleştir'}
        </Button>

        <Dialog open={open} onOpenChange={v => { if (!v) { setOpen(false); setSearch(''); } }}>
          <DialogContent className="max-w-md p-0">
            <DialogHeader className="px-4 pt-4 pb-2">
              <DialogTitle className="text-base">
                {list.code} — Eşleştirme Seç
              </DialogTitle>
            </DialogHeader>
            <div className="px-4 pb-2 border-b">
              <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  className="pl-8 h-8 text-sm"
                  placeholder={`${list.code} içinde ara...`}
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                  autoFocus
                />
              </div>
            </div>
            <div className="max-h-72 overflow-y-auto">
              {!itemsPage ? (
                <div className="p-3 space-y-2">
                  {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-8 w-full" />)}
                </div>
              ) : itemsPage.items.length === 0 ? (
                <p className="p-6 text-sm text-center text-muted-foreground">Sonuç bulunamadı</p>
              ) : (
                itemsPage.items.map(item => (
                  <button
                    key={item.id}
                    className="w-full text-left px-4 py-2.5 hover:bg-muted/50 flex items-center gap-2 transition-colors border-b last:border-b-0"
                    onClick={() => createMapping.mutate(item)}
                    disabled={createMapping.isPending}
                  >
                    <span className="font-mono text-xs text-muted-foreground w-20 shrink-0">{item.treatmentCode}</span>
                    <span className="text-sm flex-1 truncate">{item.treatmentName}</span>
                    {item.price > 0 && (
                      <span className="text-xs text-muted-foreground shrink-0">
                        {item.price.toLocaleString('tr-TR')} ₺
                      </span>
                    )}
                  </button>
                ))
              )}
            </div>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}

// ─── Category tree helpers ────────────────────────────────────────────────────

interface FlatCategory extends TreatmentCategory {
  depth: number;
  label: string; // indented name
}

function flattenCategoryTree(categories: TreatmentCategory[]): FlatCategory[] {
  const result: FlatCategory[] = [];

  function walk(parentId: string | null, depth: number) {
    const children = categories
      .filter(c => c.parentPublicId === parentId)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'tr'));

    for (const c of children) {
      result.push({
        ...c,
        depth,
        label: '\u00A0\u00A0\u00A0\u00A0'.repeat(depth) + (depth > 0 ? '└ ' : '') + c.name,
      });
      walk(c.publicId, depth + 1);
    }
  }

  walk(null, 0);
  return result;
}

// ─── Treatment Edit Sheet ─────────────────────────────────────────────────────

interface TreatmentSheetProps {
  open: boolean;
  onClose: () => void;
  treatment: TreatmentCatalogItem | null; // null = new
}

function TreatmentSheet({ open, onClose, treatment }: TreatmentSheetProps) {
  const qc = useQueryClient();
  const isNew = !treatment;

  // Fetch categories from API
  const { data: allCategories } = useQuery({
    queryKey: ['treatment-categories'],
    queryFn: () => treatmentCategoriesApi.list().then(r => r.data),
  });
  const flatCategories = allCategories ? flattenCategoryTree(allCategories) : [];

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [kdvRate, setKdvRate] = useState('20');
  const [requiresSurface, setRequiresSurface] = useState(false);
  const [requiresLab, setRequiresLab] = useState(false);
  const [tags, setTags] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [chartSymbolCode, setChartSymbolCode] = useState<string | null>(null);
  const [symbolPickerOpen, setSymbolPickerOpen] = useState(false);

  // Reset form when treatment changes
  useEffect(() => {
    if (open) {
      setCode(treatment?.code ?? '');
      setName(treatment?.name ?? '');
      setCategoryId(treatment?.category?.publicId ?? '');
      setKdvRate(String(treatment?.kdvRate ?? 20));
      setRequiresSurface(treatment?.requiresSurfaceSelection ?? false);
      setRequiresLab(treatment?.requiresLaboratory ?? false);
      setTags('');
      setIsActive(treatment?.isActive ?? true);
      setChartSymbolCode(treatment?.chartSymbolCode ?? null);
    }
  }, [open, treatment]);

  // Fetch full detail for tags (TreatmentCatalogItem doesn't have tags)
  const { data: detail } = useQuery({
    queryKey: ['treatment-detail', treatment?.publicId],
    queryFn: () => treatmentsApi.getById(treatment!.publicId).then(r => r.data),
    enabled: open && !!treatment,
  });

  useEffect(() => {
    if (detail) {
      setTags(detail.tags ?? '');
      setChartSymbolCode(detail.chartSymbolCode ?? null);
    }
  }, [detail]);

  // Mappings
  const { data: mappings, isLoading: mappingsLoading } = useQuery({
    queryKey: ['treatment-mappings', treatment?.publicId],
    queryFn: () => treatmentMappingsApi.getMappings(treatment!.publicId).then(r => r.data),
    enabled: open && !!treatment,
  });

  const { data: referenceLists } = useQuery({
    queryKey: ['pricing', 'reference-lists'],
    queryFn: () => pricingApi.getReferenceLists().then(r => r.data),
  });

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = {
        code, name,
        categoryPublicId: categoryId || null,
        kdvRate: parseFloat(kdvRate) || 0,
        requiresSurfaceSelection: requiresSurface,
        requiresLaboratory: requiresLab,
        tags: tags || null,
        isActive,
        chartSymbolCode: chartSymbolCode ?? null,
      };
      return isNew
        ? treatmentsApi.create(payload)
        : treatmentsApi.update(treatment!.publicId, payload);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatments'] });
      toast.success(isNew ? 'Tedavi oluşturuldu' : 'Tedavi güncellendi');
      if (isNew) onClose();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Kaydetme başarısız');
    },
  });

  const mappingsByList = new Map(mappings?.map(m => [m.referenceListId, m]) ?? []);

  return (
    <Sheet open={open} onOpenChange={v => !v && onClose()}>
      <SheetContent className="w-[520px] sm:max-w-[520px] flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 py-4 border-b">
          <SheetTitle>{isNew ? 'Yeni Tedavi' : 'Tedavi Düzenle'}</SheetTitle>
          {treatment && (
            <p className="text-sm text-muted-foreground font-mono">{treatment.code}</p>
          )}
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {/* Basic info */}
          <div className="px-6 py-4 space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Kod</Label>
                <Input value={code} onChange={e => setCode(e.target.value)} placeholder="örn. D001" />
              </div>
              <div className="space-y-1.5">
                <Label>KDV Oranı (%)</Label>
                <Input value={kdvRate} onChange={e => setKdvRate(e.target.value)} type="number" min={0} max={100} />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Tedavi Adı</Label>
              <Input value={name} onChange={e => setName(e.target.value)} placeholder="Tedavi adı" />
            </div>

            <div className="space-y-1.5">
              <Label>Kategori</Label>
              <select
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                value={categoryId}
                onChange={e => setCategoryId(e.target.value)}
              >
                <option value="">— Kategori seçin —</option>
                {flatCategories.map(c => (
                  <option key={c.publicId} value={c.publicId}>{c.label}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <Label>Etiketler <span className="text-muted-foreground text-xs">(virgülle ayırın)</span></Label>
              <Input value={tags} onChange={e => setTags(e.target.value)} placeholder="implant, cerrahi, ..." />
            </div>

            {/* Chart Symbol */}
            <div className="space-y-1.5">
              <Label>Diş Şeması Simgesi</Label>
              {requiresSurface ? (
                <div className="flex items-center gap-2 rounded-md border bg-muted/40 px-3 py-2">
                  <div className="flex gap-1">
                    {(['M','D','O','V','L'] as const).map(s => (
                      <span key={s} className="text-[10px] font-mono font-bold w-5 h-5 rounded border border-blue-400 bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400 flex items-center justify-center">{s}</span>
                    ))}
                  </div>
                  <span className="text-xs text-muted-foreground">
                    Yüzey gerektiren tedavi — plan ekranında seçilen yüzeylerden otomatik türetilir.
                  </span>
                </div>
              ) : (
                <>
                  <div className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() => setSymbolPickerOpen(true)}
                      className="flex items-center gap-2.5 h-9 px-3 rounded-md border border-input bg-background text-sm shadow-sm hover:bg-muted/50 transition-colors"
                    >
                      {chartSymbolCode ? (() => {
                        const sym = getSymbol(chartSymbolCode);
                        return sym ? (
                          <>
                            <svg viewBox="0 0 36 44" width={20} height={24} className="shrink-0 overflow-visible">
                              <rect x="0" y="0" width="36" height="44" fill="#ffffff" />
                              <polygon points="0,0 36,0 28,12 8,12"   fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
                              <polygon points="8,32 28,32 36,44 0,44"  fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
                              <polygon points="0,0 8,12 8,32 0,44"     fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
                              <polygon points="28,12 36,0 36,44 28,32" fill="#f0f9ff" stroke="#94a3b8" strokeWidth="1" />
                              <rect x="8" y="12" width="20" height="20" fill="#e0f2fe" stroke="#94a3b8" strokeWidth="1" />
                              {sym.overlay()}
                            </svg>
                            <span>{sym.label}</span>
                            <span className="text-[10px] font-mono text-muted-foreground">{sym.code}</span>
                          </>
                        ) : <span className="text-muted-foreground">Simge seç…</span>;
                      })() : <span className="text-muted-foreground">Simge seç…</span>}
                    </button>
                    {chartSymbolCode && (
                      <button
                        type="button"
                        onClick={() => setChartSymbolCode(null)}
                        className="text-xs text-muted-foreground hover:text-destructive transition-colors"
                      >
                        Kaldır
                      </button>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Bu tedavi bir diş üzerine atandığında şemada gösterilecek simge.
                  </p>
                </>
              )}
            </div>

            <DentalSymbolPicker
              open={symbolPickerOpen}
              value={chartSymbolCode}
              onChange={setChartSymbolCode}
              onClose={() => setSymbolPickerOpen(false)}
            />

            <div className="flex items-center gap-6">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <Checkbox checked={requiresSurface} onCheckedChange={v => setRequiresSurface(!!v)} />
                Diş yüzeyi seçimi
              </label>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <Checkbox checked={requiresLab} onCheckedChange={v => setRequiresLab(!!v)} />
                Laboratuvar gerektirir
              </label>
              {!isNew && (
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <Checkbox checked={isActive} onCheckedChange={v => setIsActive(!!v)} />
                  Aktif
                </label>
              )}
            </div>
          </div>

          {/* Reference mappings — only for existing treatments */}
          {!isNew && (
            <div className="px-6 py-4 border-t">
              <div className="mb-3">
                <p className="text-sm font-medium">Referans Eşleştirmeleri</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Bu tedavinin TDB, SUT ve diğer listelerdeki karşılıklarını seçin.
                  Fiyatlandırma kuralları bu eşleştirmeleri kullanır.
                </p>
              </div>

              {mappingsLoading || !referenceLists ? (
                <div className="space-y-2">
                  {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
                </div>
              ) : referenceLists.length === 0 ? (
                <p className="text-sm text-muted-foreground">Henüz referans listesi yok.</p>
              ) : (
                <div className="rounded-md border">
                  {referenceLists.map(list => (
                    <MappingRow
                      key={list.id}
                      list={list}
                      mapping={mappingsByList.get(list.id)}
                      treatmentPublicId={treatment!.publicId}
                    />
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t flex items-center justify-end gap-2 bg-background shrink-0">
          <Button variant="outline" onClick={onClose} disabled={saveMutation.isPending}>
            İptal
          </Button>
          <Button
            onClick={() => saveMutation.mutate()}
            disabled={saveMutation.isPending || !code || !name}
          >
            {saveMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export function TreatmentCatalogPage() {
  const qc = useQueryClient();
  const isPlatformAdmin = useAuthStore(s => s.user?.isPlatformAdmin ?? false);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [showInactive, setShowInactive] = useState(false);
  const [editingTreatment, setEditingTreatment] = useState<TreatmentCatalogItem | null | undefined>(undefined);
  // undefined = closed, null = new, TreatmentCatalogItem = edit

  const { data: allCategories } = useQuery({
    queryKey: ['treatment-categories'],
    queryFn: () => treatmentCategoriesApi.list().then(r => r.data),
  });
  const flatCategories = allCategories ? flattenCategoryTree(allCategories) : [];

  const { data: treatmentsPage, isLoading } = useQuery({
    queryKey: ['treatments', 'catalog', search, categoryId, showInactive],
    queryFn: () => treatmentsApi.list({
      search,
      categoryId: categoryId || undefined,
      activeOnly: !showInactive,
      pageSize: 200,
    }).then(r => r.data),
  });

  const treatments = treatmentsPage?.items ?? [];

  const deleteMutation = useMutation({
    mutationFn: (t: TreatmentCatalogItem) =>
      treatmentsApi.update(t.publicId, {
        code: t.code,
        name: t.name,
        categoryPublicId: t.category?.publicId ?? null,
        kdvRate: t.kdvRate,
        requiresSurfaceSelection: t.requiresSurfaceSelection,
        requiresLaboratory: t.requiresLaboratory,
        isActive: false,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatments'] });
      toast.success('Tedavi deaktif edildi');
    },
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Tedavi Kataloğu</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Klinik tedavi listesi ve referans fiyat eşleştirmeleri
          </p>
        </div>
        <Button size="sm" onClick={() => setEditingTreatment(null)}>
          <Plus className="h-4 w-4 mr-1.5" />
          Yeni Tedavi
        </Button>
      </div>

      {/* Info note */}
      <div className="rounded-md border bg-muted/30 px-4 py-2.5 text-sm text-muted-foreground">
        Bu tablo kliniğinizin tedavi kataloğunu gösterir. <strong className="text-foreground">TDB, SUT</strong> gibi referans listelerindeki karşılıkları her tedavinin
        {' '}<strong className="text-foreground">Düzenle</strong> ekranında eşleştirebilirsiniz.
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="relative w-64">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-8"
            placeholder="Kod veya adı ara..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <select
          className="h-9 rounded-md border border-input bg-background px-3 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          value={categoryId}
          onChange={e => setCategoryId(e.target.value)}
        >
          <option value="">Tüm kategoriler</option>
          {flatCategories.map(c => (
            <option key={c.publicId} value={c.publicId}>{c.label}</option>
          ))}
        </select>
        <label className="flex items-center gap-2 text-sm cursor-pointer text-muted-foreground">
          <Checkbox checked={showInactive} onCheckedChange={v => setShowInactive(!!v)} />
          Pasif tedavileri göster
        </label>
      </div>

      {/* Table */}
      <div className="border rounded-lg overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-28">Kod</TableHead>
              <TableHead>Tedavi Adı</TableHead>
              <TableHead className="w-40">Kategori</TableHead>
              <TableHead className="w-20 text-right">KDV</TableHead>
              <TableHead className="w-20">Durum</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 6 }).map((_, i) => (
                <TableRow key={i}>
                  {[1, 2, 3, 4, 5, 6].map(j => (
                    <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
              : treatments.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-10 text-muted-foreground text-sm">
                    Tedavi bulunamadı
                  </TableCell>
                </TableRow>
              )
              : treatments.map(t => (
                <TableRow key={t.publicId} className={!t.isActive ? 'opacity-50' : ''}>
                  <TableCell className="font-mono text-xs">{t.code}</TableCell>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-1.5">
                      {t.name}
                      {t.isGlobal && (
                        <span className="inline-flex items-center gap-0.5 text-[10px] text-muted-foreground border rounded px-1 py-0.5 font-normal">
                          <Lock className="size-2.5" /> Global
                        </span>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {t.category?.name ?? '—'}
                  </TableCell>
                  <TableCell className="text-right text-sm">%{t.kdvRate}</TableCell>
                  <TableCell>
                    {t.isActive
                      ? <Badge variant="secondary" className="text-xs">Aktif</Badge>
                      : <Badge variant="outline" className="text-xs text-muted-foreground">Pasif</Badge>
                    }
                  </TableCell>
                  <TableCell>
                    {t.isGlobal && !isPlatformAdmin ? (
                      <div className="flex justify-end pr-1">
                        <Lock className="size-3.5 text-muted-foreground/40" />
                      </div>
                    ) : (
                      <div className="flex items-center gap-1 justify-end">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={() => setEditingTreatment(t)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        {t.isActive && (
                          <Button
                            size="icon"
                            variant="ghost"
                            className="h-7 w-7 text-muted-foreground hover:text-destructive"
                            onClick={() => deleteMutation.mutate(t)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        )}
                      </div>
                    )}
                  </TableCell>
                </TableRow>
              ))
            }
          </TableBody>
        </Table>
      </div>

      {editingTreatment !== undefined && (
        <TreatmentSheet
          open
          onClose={() => setEditingTreatment(undefined)}
          treatment={editingTreatment}
        />
      )}
    </div>
  );
}
