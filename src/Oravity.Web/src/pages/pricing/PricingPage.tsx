import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search, Plus, Pencil, Check, X, ChevronLeft, ChevronRight,
  Tag, Zap, ListChecks, AlertCircle, Building2, Copy, Loader2,
  Trash2, Upload, FlaskConical,
} from 'lucide-react';
import { toast } from 'sonner';
import { pricingApi, type PricingRule, type ReferencePriceList, type BranchPricing, type TreatmentPriceResponse } from '@/api/pricing';
import { treatmentsApi, treatmentCategoriesApi, type TreatmentCategory } from '@/api/treatments';
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

// ─── Add Item Dialog ────────────────────────────────────────────────────────

function AddItemDialog({ open, onClose, list }: { open: boolean; onClose: () => void; list: ReferencePriceList }) {
  const qc = useQueryClient();
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [price, setPrice] = useState('');
  const [priceKdv, setPriceKdv] = useState('');

  useEffect(() => {
    if (open) { setCode(''); setName(''); setPrice(''); setPriceKdv(''); }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => pricingApi.upsertReferenceItem(list.id, code.trim().toUpperCase(), {
      treatmentName: name.trim(),
      price: parseFloat(price) || 0,
      priceKdv: parseFloat(priceKdv) || 0,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-items'] });
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
      toast.success('Kalem eklendi');
      onClose();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Kalem eklenemedi');
    },
  });

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Yeni Kalem — {list.code}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-1">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Kod</Label>
              <Input
                placeholder="ör. D001"
                value={code}
                onChange={e => setCode(e.target.value)}
                className="uppercase"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Fiyat (₺)</Label>
              <Input
                type="number"
                placeholder="0.00"
                value={price}
                onChange={e => setPrice(e.target.value)}
              />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label>Tedavi Adı</Label>
            <Input
              placeholder="Tedavi adı"
              value={name}
              onChange={e => setName(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label>KDV'li Fiyat (₺) <span className="text-muted-foreground text-xs">(opsiyonel)</span></Label>
            <Input
              type="number"
              placeholder="0.00"
              value={priceKdv}
              onChange={e => setPriceKdv(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={mutation.isPending}>İptal</Button>
          <Button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !code.trim() || !name.trim()}
          >
            {mutation.isPending ? 'Ekleniyor...' : 'Ekle'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Confirm Delete Dialog ───────────────────────────────────────────────────

function ConfirmDialog({ open, title, message, onConfirm, onCancel, loading }: {
  open: boolean; title: string; message: string;
  onConfirm: () => void; onCancel: () => void; loading?: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={v => !v && onCancel()}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>{title}</DialogTitle></DialogHeader>
        <p className="text-sm text-muted-foreground">{message}</p>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={loading}>İptal</Button>
          <Button variant="destructive" onClick={onConfirm} disabled={loading}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Sil'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── CSV Import Dialog ────────────────────────────────────────────────────────

function CsvImportDialog({ open, onClose, list }: { open: boolean; onClose: () => void; list: ReferencePriceList }) {
  const qc = useQueryClient();
  const [csvText, setCsvText] = useState('');
  const [preview, setPreview] = useState<{ code: string; name: string; price: number; currency: string }[]>([]);
  const [parseError, setParseError] = useState('');
  const [importing, setImporting] = useState(false);

  useEffect(() => { if (open) { setCsvText(''); setPreview([]); setParseError(''); } }, [open]);

  function parseCsv(text: string) {
    setParseError('');
    const lines = text.trim().split('\n').map(l => l.trim()).filter(Boolean);
    if (lines.length === 0) { setPreview([]); return; }

    // Skip header if first cell looks like "kod" or "code" (case-insensitive)
    const firstCell = lines[0].split(/[,;\t]/)[0].toLowerCase().replace(/"/g, '').trim();
    const start = (firstCell === 'kod' || firstCell === 'code' || isNaN(Number(firstCell)) && firstCell.length < 10) ? 1 : 0;

    const rows: typeof preview = [];
    for (let i = start; i < lines.length; i++) {
      const cols = lines[i].split(/[,;\t]/).map(c => c.replace(/^"|"$/g, '').trim());
      // Format: kod, ad, fiyat[, para_birimi]  OR  ad, fiyat (no code)
      if (cols.length < 2) continue;
      const price = parseFloat(cols[cols.length >= 3 ? 2 : 1].replace(',', '.'));
      if (isNaN(price)) continue;
      const code = cols.length >= 3 ? cols[0].toUpperCase() : `ROW${i}`;
      const name = cols.length >= 3 ? cols[1] : cols[0];
      const currency = (cols[3] ?? 'TRY').toUpperCase() || 'TRY';
      rows.push({ code, name, price, currency });
    }

    if (rows.length === 0) {
      setParseError('Hiç geçerli satır bulunamadı. Format: Kod, Ad, Fiyat[, Para Birimi]');
    }
    setPreview(rows);
  }

  async function doImport() {
    if (preview.length === 0) return;
    setImporting(true);
    try {
      const res = await pricingApi.bulkUpsertReferenceItems(list.id, preview.map(r => ({
        code: r.code, name: r.name, price: r.price, currency: r.currency,
      })));
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-items'] });
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
      toast.success(`${res.data.count} kalem ${list.code} listesine aktarıldı`);
      onClose();
    } catch {
      toast.error('İçe aktarma başarısız');
    } finally {
      setImporting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={v => { if (!v && !importing) onClose(); }}>
      <DialogContent className="sm:max-w-[700px]">
        <DialogHeader>
          <DialogTitle>CSV/Excel İçe Aktar — {list.code}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 text-sm">
          <div className="bg-muted/50 rounded-md p-3 text-xs text-muted-foreground space-y-1">
            <p className="font-medium text-foreground">Format (virgül, noktalı virgül veya tab):</p>
            <p className="font-mono">Kod, Ad, Fiyat, Para Birimi</p>
            <p className="font-mono text-xs opacity-70">D001, Kompozit Dolgu (1 yüz), 1500, TRY</p>
            <p className="font-mono text-xs opacity-70">IMPL-BEGO, İmplant-Bego, 444, EUR</p>
            <p className="mt-1">Excel'den kopyala-yapıştır çalışır. Başlık satırı otomatik atlanır.</p>
          </div>
          <Textarea
            className="font-mono text-xs h-36 resize-none"
            placeholder="Buraya CSV içeriğini yapıştırın..."
            value={csvText}
            onChange={e => { setCsvText(e.target.value); parseCsv(e.target.value); }}
          />
          {parseError && <p className="text-xs text-destructive">{parseError}</p>}
          {preview.length > 0 && (
            <div className="border rounded-md max-h-40 overflow-y-auto">
              <table className="w-full text-xs">
                <thead className="bg-muted sticky top-0">
                  <tr>
                    <th className="text-left px-2 py-1">Kod</th>
                    <th className="text-left px-2 py-1">Ad</th>
                    <th className="text-right px-2 py-1">Fiyat</th>
                    <th className="text-left px-2 py-1">Birim</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.map((r, i) => (
                    <tr key={i} className="border-t">
                      <td className="px-2 py-1 font-mono">{r.code}</td>
                      <td className="px-2 py-1 truncate max-w-[200px]">{r.name}</td>
                      <td className="px-2 py-1 text-right tabular-nums">{r.price.toLocaleString('tr-TR')}</td>
                      <td className="px-2 py-1">{r.currency}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={importing}>İptal</Button>
          <Button onClick={doImport} disabled={importing || preview.length === 0}>
            {importing
              ? <><Loader2 className="h-4 w-4 mr-1.5 animate-spin" />Aktarılıyor...</>
              : <><Upload className="h-4 w-4 mr-1.5" />{preview.length} Kalem Aktar</>
            }
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Category helpers ─────────────────────────────────────────────────────────

interface FlatCategory extends TreatmentCategory { depth: number; label: string }

function flattenCategoryTree(categories: TreatmentCategory[]): FlatCategory[] {
  const result: FlatCategory[] = [];
  function walk(parentId: string | null, depth: number) {
    const children = categories
      .filter(c => c.parentPublicId === parentId)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'tr'));
    for (const c of children) {
      result.push({ ...c, depth, label: '\u00A0\u00A0\u00A0\u00A0'.repeat(depth) + (depth > 0 ? '└ ' : '') + c.name });
      walk(c.publicId, depth + 1);
    }
  }
  walk(null, 0);
  return result;
}

/** Returns publicIds of `root` and all its descendants */
function subtreeIds(categories: TreatmentCategory[], rootId: string): Set<string> {
  const ids = new Set<string>();
  function walk(id: string) {
    ids.add(id);
    categories.filter(c => c.parentPublicId === id).forEach(c => walk(c.publicId));
  }
  walk(rootId);
  return ids;
}

// ─── Seed From Catalog Dialog ────────────────────────────────────────────────

function SeedFromCatalogDialog({ open, onClose, list }: { open: boolean; onClose: () => void; list: ReferencePriceList }) {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [progress, setProgress] = useState<{ done: number; total: number } | null>(null);
  const [running, setRunning] = useState(false);

  const { data: treatmentsPage } = useQuery({
    queryKey: ['treatments', 'list', 'seed'],
    queryFn: () => treatmentsApi.list({ activeOnly: true, pageSize: 500 }).then(r => r.data),
    enabled: open,
  });

  const { data: existingPage } = useQuery({
    queryKey: ['pricing', 'ref-items-seed', list.id],
    queryFn: () => pricingApi.getReferenceItems(list.id, { pageSize: 1000 }).then(r => r.data),
    enabled: open,
  });

  const { data: allCategories } = useQuery({
    queryKey: ['treatment-categories'],
    queryFn: () => treatmentCategoriesApi.list().then(r => r.data),
    enabled: open,
  });

  const flatCategories = allCategories ? flattenCategoryTree(allCategories) : [];

  const existingCodes = new Set(existingPage?.items.map(i => i.treatmentCode) ?? []);
  const candidates = (treatmentsPage?.items ?? []).filter(t => !existingCodes.has(t.code));

  const allowedCategoryIds = categoryFilter && allCategories
    ? subtreeIds(allCategories, categoryFilter)
    : null;

  const filtered = candidates.filter(t => {
    const matchSearch = !search || t.name.toLowerCase().includes(search.toLowerCase()) || t.code.toLowerCase().includes(search.toLowerCase());
    const matchCategory = !allowedCategoryIds || (t.category != null && allowedCategoryIds.has(t.category.publicId));
    return matchSearch && matchCategory;
  });

  // Init selection when candidates load
  useEffect(() => {
    if (candidates.length > 0 && selected.size === 0) {
      setSelected(new Set(candidates.map(t => t.code)));
    }
  }, [candidates.length]);

  // Reset on open
  useEffect(() => {
    if (open) { setSearch(''); setCategoryFilter(''); setSelected(new Set()); setProgress(null); }
  }, [open]);

  const allFilteredSelected = filtered.length > 0 && filtered.every(t => selected.has(t.code));

  function toggleAll() {
    if (allFilteredSelected) {
      setSelected(prev => { const s = new Set(prev); filtered.forEach(t => s.delete(t.code)); return s; });
    } else {
      setSelected(prev => { const s = new Set(prev); filtered.forEach(t => s.add(t.code)); return s; });
    }
  }

  function toggle(code: string) {
    setSelected(prev => { const s = new Set(prev); s.has(code) ? s.delete(code) : s.add(code); return s; });
  }

  const toAdd = candidates.filter(t => selected.has(t.code));

  async function runSeed() {
    if (toAdd.length === 0) return;
    setRunning(true);
    setProgress({ done: 0, total: toAdd.length });
    let done = 0;
    for (const t of toAdd) {
      try {
        await pricingApi.upsertReferenceItem(list.id, t.code, { treatmentName: t.name, price: 0, priceKdv: 0 });
      } catch { /* skip */ }
      done++;
      setProgress({ done, total: toAdd.length });
    }
    qc.invalidateQueries({ queryKey: ['pricing', 'reference-items'] });
    qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
    toast.success(`${done} kalem ${list.code} listesine eklendi`);
    setRunning(false);
    onClose();
  }

  const ready = !!treatmentsPage && !!existingPage;

  return (
    <Dialog open={open} onOpenChange={v => { if (!v && !running) onClose(); }}>
      <DialogContent className="sm:max-w-[860px]">
        <DialogHeader>
          <DialogTitle>Katalogdan Doldur — {list.code}</DialogTitle>
          {ready && candidates.length > 0 && (
            <p className="text-sm text-muted-foreground mt-1">
              Listede olmayan <strong>{candidates.length}</strong> tedavi gösteriliyor.
              {existingCodes.size > 0 && ` (${existingCodes.size} zaten mevcut, gizlendi)`}
            </p>
          )}
        </DialogHeader>

        {!ready ? (
          <div className="flex items-center justify-center gap-2 py-10 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Yükleniyor...
          </div>
        ) : candidates.length === 0 ? (
          <p className="text-center text-muted-foreground py-8 text-sm">
            Katalogdaki tüm tedaviler zaten bu listede mevcut.
          </p>
        ) : (
          <div className="space-y-3">
            {/* Controls */}
            <div className="flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  className="pl-8 h-8 text-sm"
                  placeholder="Ara..."
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                />
              </div>
              {flatCategories.length > 0 && (
                <Select value={categoryFilter} onValueChange={setCategoryFilter}>
                  <SelectTrigger className="h-8 w-48 text-sm shrink-0">
                    <SelectValue placeholder="Tüm kategoriler" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="">Tüm kategoriler</SelectItem>
                    {flatCategories.map(c => (
                      <SelectItem key={c.publicId} value={c.publicId}>{c.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>

            {/* Select all row */}
            <div className="flex items-center gap-2 px-1">
              <Checkbox
                checked={allFilteredSelected}
                onCheckedChange={toggleAll}
                id="select-all"
              />
              <label htmlFor="select-all" className="text-sm cursor-pointer select-none text-muted-foreground">
                {allFilteredSelected ? 'Tümünü kaldır' : 'Tümünü seç'}
                {search && ` (filtre: ${filtered.length})`}
              </label>
              <span className="ml-auto text-sm text-muted-foreground">
                <strong className="text-foreground">{toAdd.length}</strong> seçili
              </span>
            </div>

            {/* Treatment list */}
            <div className="border rounded-md max-h-64 overflow-y-auto">
              {filtered.length === 0 ? (
                <p className="text-center text-muted-foreground py-6 text-sm">Sonuç yok</p>
              ) : (
                filtered.map(t => (
                  <label
                    key={t.code}
                    className="flex items-center gap-3 px-3 py-2 hover:bg-muted/50 cursor-pointer border-b last:border-b-0"
                  >
                    <Checkbox
                      checked={selected.has(t.code)}
                      onCheckedChange={() => toggle(t.code)}
                    />
                    <span className="font-mono text-xs text-muted-foreground w-20 shrink-0">{t.code}</span>
                    <span className="text-sm flex-1 truncate">{t.name}</span>
                    {t.category && (
                      <span className="text-xs text-muted-foreground shrink-0">{t.category.name}</span>
                    )}
                  </label>
                ))
              )}
            </div>

            {/* Progress */}
            {progress && (
              <div className="space-y-1">
                <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                  <div
                    className="h-full bg-primary transition-all duration-150"
                    style={{ width: `${(progress.done / progress.total) * 100}%` }}
                  />
                </div>
                <p className="text-xs text-center text-muted-foreground">
                  {progress.done} / {progress.total} eklendi
                </p>
              </div>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={running}>İptal</Button>
          {candidates.length > 0 && (
            <Button onClick={runSeed} disabled={running || toAdd.length === 0}>
              {running
                ? <><Loader2 className="h-4 w-4 mr-1.5 animate-spin" />Ekleniyor...</>
                : <><Copy className="h-4 w-4 mr-1.5" />{toAdd.length} Kalem Ekle</>
              }
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Referans Fiyatlar Tab ──────────────────────────────────────────────────

function ReferencePricesTab() {
  const qc = useQueryClient();
  const [selectedList, setSelectedList] = useState<ReferencePriceList | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editPrice, setEditPrice] = useState('');
  const [editKdv, setEditKdv] = useState('');
  const [addItemOpen, setAddItemOpen] = useState(false);
  const [seedOpen, setSeedOpen] = useState(false);
  const [csvImportOpen, setCsvImportOpen] = useState(false);
  const [deletingItem, setDeletingItem] = useState<{ code: string } | null>(null);

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

  const deleteItemMutation = useMutation({
    mutationFn: (code: string) => pricingApi.deleteReferenceItem(selectedList!.id, code),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-items'] });
      qc.invalidateQueries({ queryKey: ['pricing', 'reference-lists'] });
      setDeletingItem(null);
      toast.success('Kalem silindi');
    },
    onError: () => toast.error('Kalem silinemedi'),
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
              <span className="text-sm text-muted-foreground">{total} kalem</span>
              <div className="flex items-center gap-1.5 ml-auto">
                <Button size="sm" variant="outline" onClick={() => setCsvImportOpen(true)}>
                  <Upload className="h-3.5 w-3.5 mr-1.5" />
                  CSV/Excel Aktar
                </Button>
                <Button size="sm" variant="outline" onClick={() => setSeedOpen(true)}>
                  <Copy className="h-3.5 w-3.5 mr-1.5" />
                  Katalogdan Doldur
                </Button>
                <Button size="sm" onClick={() => setAddItemOpen(true)}>
                  <Plus className="h-3.5 w-3.5 mr-1.5" />
                  Yeni Kalem
                </Button>
              </div>
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
                              <>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  className="h-6 w-6"
                                  onClick={() => startEdit(item)}
                                >
                                  <Pencil className="h-3.5 w-3.5" />
                                </Button>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  className="h-6 w-6 text-destructive hover:text-destructive"
                                  onClick={() => setDeletingItem({ code: item.treatmentCode })}
                                >
                                  <Trash2 className="h-3.5 w-3.5" />
                                </Button>
                              </>
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

      {selectedList && addItemOpen && (
        <AddItemDialog open list={selectedList} onClose={() => setAddItemOpen(false)} />
      )}
      {selectedList && seedOpen && (
        <SeedFromCatalogDialog open list={selectedList} onClose={() => setSeedOpen(false)} />
      )}
      {selectedList && csvImportOpen && (
        <CsvImportDialog open list={selectedList} onClose={() => setCsvImportOpen(false)} />
      )}
      <ConfirmDialog
        open={!!deletingItem}
        title="Kalemi Sil"
        message={`"${deletingItem?.code}" kodlu kalemi listeden kaldırmak istediğinizden emin misiniz?`}
        loading={deleteItemMutation.isPending}
        onConfirm={() => deletingItem && deleteItemMutation.mutate(deletingItem.code)}
        onCancel={() => setDeletingItem(null)}
      />
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
  categoryPublicIds: string[];
}): string | null {
  const obj: Record<string, unknown> = {};
  if (opts.institutionIds.length > 0)    obj['institutionIds']    = opts.institutionIds;
  if (opts.ossOnly)                      obj['ossOnly']           = true;
  if (opts.campaignCode.trim())          obj['campaignCodes']     = [opts.campaignCode.trim()];
  if (opts.categoryPublicIds.length > 0) obj['categoryPublicIds'] = opts.categoryPublicIds;
  return Object.keys(obj).length > 0 ? JSON.stringify(obj) : null;
}

// IncludeFilters JSON'undan koşulları parse et
function parseIncludeFilters(json: string | null) {
  const defaults = { institutionIds: [] as number[], ossOnly: false, campaignCode: '', categoryPublicIds: [] as string[] };
  if (!json) return defaults;
  try {
    const obj = JSON.parse(json);
    return {
      institutionIds:    Array.isArray(obj.institutionIds) ? (obj.institutionIds as number[]) : [],
      ossOnly:           !!obj.ossOnly,
      campaignCode:      Array.isArray(obj.campaignCodes) ? (obj.campaignCodes[0] ?? '') : '',
      categoryPublicIds: Array.isArray(obj.categoryPublicIds) ? (obj.categoryPublicIds as string[]) : [],
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
  const [categoryPublicIds, setCategoryPublicIds] = useState<string[]>([]);

  const { data: allCategories } = useQuery({
    queryKey: ['treatment-categories'],
    queryFn: () => treatmentCategoriesApi.list().then(r => r.data),
    staleTime: 5 * 60 * 1000,
  });
  const flatCategories = allCategories ? flattenCategoryTree(allCategories) : [];

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
      setCategoryPublicIds(parsed.categoryPublicIds);
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
      setCategoryPublicIds([]);
    }
  }, [editing, open]);

  function buildFilters() {
    return buildIncludeFilters({ institutionIds, ossOnly, campaignCode, categoryPublicIds });
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

              {/* Kategori filtresi */}
              {flatCategories.length > 0 && (
                <div className="space-y-1.5">
                  <Label className="text-sm">Tedavi Kategorileri <span className="text-muted-foreground font-normal">(boş = tüm tedaviler)</span></Label>
                  <div className="flex flex-wrap gap-1.5">
                    {flatCategories.map(c => (
                      <button
                        key={c.publicId}
                        type="button"
                        onClick={() => setCategoryPublicIds(prev =>
                          prev.includes(c.publicId) ? prev.filter(x => x !== c.publicId) : [...prev, c.publicId]
                        )}
                        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium border transition-colors ${
                          categoryPublicIds.includes(c.publicId)
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'bg-background text-muted-foreground border-border hover:bg-accent'
                        }`}
                        style={{ paddingLeft: `${(c.depth * 8) + 10}px` }}
                      >
                        {c.depth > 0 && <span className="mr-1 opacity-50">└</span>}
                        {c.name}
                      </button>
                    ))}
                  </div>
                </div>
              )}

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

              {(institutionIds.length > 0 || ossOnly || campaignCode.trim() || categoryPublicIds.length > 0) && (
                <p className="text-xs text-amber-600">
                  Bu kural yalnızca:{' '}
                  {[
                    institutionIds.length > 0 && `seçili ${institutionIds.length} kurum hastası`,
                    ossOnly && 'ÖSS kapsamındakiler',
                    campaignCode.trim() && `"${campaignCode.trim()}" kampanyası aktifken`,
                    categoryPublicIds.length > 0 && `seçili ${categoryPublicIds.length} kategori`,
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
  const qc = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<PricingRule | null>(null);
  const [deletingRule, setDeletingRule] = useState<PricingRule | null>(null);

  const deleteRuleMutation = useMutation({
    mutationFn: (publicId: string) => pricingApi.deleteRule(publicId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'rules'] });
      setDeletingRule(null);
      toast.success('Kural silindi');
    },
    onError: () => toast.error('Kural silinemedi'),
  });

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
                          <div className="flex gap-1">
                            <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => openEdit(rule)}>
                              <Pencil className="h-3.5 w-3.5" />
                            </Button>
                            <Button
                              size="icon"
                              variant="ghost"
                              className="h-7 w-7 text-destructive hover:text-destructive"
                              onClick={() => setDeletingRule(rule)}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </div>
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
      <ConfirmDialog
        open={!!deletingRule}
        title="Kuralı Sil"
        message={`"${deletingRule?.name}" kuralını kalıcı olarak silmek istediğinizden emin misiniz?`}
        loading={deleteRuleMutation.isPending}
        onConfirm={() => deletingRule && deleteRuleMutation.mutate(deletingRule.publicId)}
        onCancel={() => setDeletingRule(null)}
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

// ─── Price Test Tab ──────────────────────────────────────────────────────────

function PriceTestTab() {
  const [selectedTreatment, setSelectedTreatment] = useState<{ publicId: string; name: string; code: string } | null>(null);
  const [treatmentSearch, setTreatmentSearch] = useState('');
  const [selectedBranch, setSelectedBranch] = useState<number | null>(null);
  const [selectedInstitution, setSelectedInstitution] = useState<number | null>(null);
  const [isOss, setIsOss] = useState(false);
  const [result, setResult] = useState<TreatmentPriceResponse | null>(null);
  const [loading, setLoading] = useState(false);

  const { data: treatmentsPage } = useQuery({
    queryKey: ['treatments', 'list', 'pricetest', treatmentSearch],
    queryFn: () => treatmentsApi.list({ search: treatmentSearch || undefined, activeOnly: true, pageSize: 20 }).then(r => r.data),
    staleTime: 30_000,
  });

  const { data: branches } = useQuery({
    queryKey: ['pricing', 'branches'],
    queryFn: () => pricingApi.getBranchPricing().then(r => r.data),
    staleTime: 60_000,
  });

  const { data: institutions } = useQuery({
    queryKey: ['institutions'],
    queryFn: () => institutionsApi.getAll().then(r => r.data),
    staleTime: 60_000,
  });

  async function runTest() {
    if (!selectedTreatment) return;
    setLoading(true);
    setResult(null);
    try {
      const res = await pricingApi.getTreatmentPrice(selectedTreatment.publicId, {
        branchId: selectedBranch ?? undefined,
        institutionId: selectedInstitution ?? undefined,
        isOss,
      });
      setResult(res.data);
    } catch {
      toast.error('Fiyat hesaplanamadı');
    } finally {
      setLoading(false);
    }
  }

  const strategyLabel: Record<string, string> = {
    Rule:             'Kural eşleşti',
    ReferencePrice:   'Referans fiyat (kural yok)',
    NoPriceConfigured: 'Fiyat tanımlı değil',
  };

  const strategyColor: Record<string, string> = {
    Rule:             'text-green-600',
    ReferencePrice:   'text-blue-600',
    NoPriceConfigured: 'text-destructive',
  };

  return (
    <div className="max-w-2xl space-y-6">
      <p className="text-sm text-muted-foreground">
        Kural motorunu test edin. Tedaviyi ve koşulları seçin, hangi kuralın uygulandığını görün.
      </p>

      <div className="border rounded-lg p-4 space-y-4">
        {/* Treatment picker */}
        <div className="space-y-1.5">
          <Label>Tedavi</Label>
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              className="pl-8"
              placeholder="Tedavi ara..."
              value={treatmentSearch}
              onChange={e => { setTreatmentSearch(e.target.value); setSelectedTreatment(null); setResult(null); }}
            />
          </div>
          {treatmentsPage && treatmentSearch && !selectedTreatment && (
            <div className="border rounded-md max-h-40 overflow-y-auto">
              {(treatmentsPage.items ?? []).map(t => (
                <button
                  key={t.publicId}
                  type="button"
                  className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-center gap-2 border-b last:border-b-0"
                  onClick={() => { setSelectedTreatment({ publicId: t.publicId, name: t.name, code: t.code }); setTreatmentSearch(t.name); setResult(null); }}
                >
                  <span className="font-mono text-xs text-muted-foreground w-20 shrink-0">{t.code}</span>
                  {t.name}
                </button>
              ))}
              {treatmentsPage.items.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">Sonuç yok</p>
              )}
            </div>
          )}
          {selectedTreatment && (
            <div className="flex items-center gap-2 text-sm">
              <Badge variant="outline" className="font-mono">{selectedTreatment.code}</Badge>
              <span>{selectedTreatment.name}</span>
              <button onClick={() => { setSelectedTreatment(null); setTreatmentSearch(''); setResult(null); }} className="ml-auto text-muted-foreground hover:text-foreground">
                <X className="h-4 w-4" />
              </button>
            </div>
          )}
        </div>

        <div className="grid grid-cols-2 gap-3">
          {/* Branch */}
          <div className="space-y-1.5">
            <Label>Şube <span className="text-muted-foreground font-normal text-xs">(MULTI için)</span></Label>
            <Select
              value={selectedBranch ? String(selectedBranch) : ''}
              onValueChange={v => setSelectedBranch(v ? Number(v) : null)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Şube seç (opsiyonel)" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Şube yok</SelectItem>
                {(branches ?? []).map(b => (
                  <SelectItem key={b.branchId} value={String(b.branchId)}>
                    {b.branchName} <span className="text-muted-foreground text-xs">MULTI={b.pricingMultiplier}</span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Institution */}
          <div className="space-y-1.5">
            <Label>Anlaşmalı Kurum</Label>
            <Select
              value={selectedInstitution ? String(selectedInstitution) : ''}
              onValueChange={v => setSelectedInstitution(v ? Number(v) : null)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Kurum yok" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Kurum yok</SelectItem>
                {(institutions ?? []).filter(i => i.isActive).map(i => (
                  <SelectItem key={i.id} value={String(i.id)}>{i.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* OSS */}
        <div className="flex items-center gap-2">
          <Checkbox id="test-oss" checked={isOss} onCheckedChange={v => setIsOss(!!v)} />
          <Label htmlFor="test-oss" className="font-normal cursor-pointer">ÖSS Kapsamı</Label>
        </div>

        <Button onClick={runTest} disabled={!selectedTreatment || loading} className="w-full">
          {loading ? <><Loader2 className="h-4 w-4 mr-2 animate-spin" />Hesaplanıyor...</> : 'Fiyatı Hesapla'}
        </Button>
      </div>

      {result && (
        <div className="border rounded-lg p-4 space-y-3">
          <div className="flex items-center gap-2">
            <h3 className="font-medium">Sonuç</h3>
            <span className={`text-sm font-medium ${strategyColor[result.strategy] ?? ''}`}>
              {strategyLabel[result.strategy] ?? result.strategy}
            </span>
          </div>
          <div className="grid grid-cols-3 gap-4 text-sm">
            <div>
              <p className="text-xs text-muted-foreground">Uygulanan Fiyat</p>
              <p className="text-2xl font-bold tabular-nums">
                {result.unitPrice.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                <span className="text-sm font-normal text-muted-foreground ml-1">{result.currency}</span>
              </p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Referans Fiyat</p>
              <p className="text-lg font-medium tabular-nums text-muted-foreground">
                {result.referencePrice.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} {result.currency}
              </p>
            </div>
            {result.unitPrice < result.referencePrice && (
              <div>
                <p className="text-xs text-muted-foreground">İndirim</p>
                <p className="text-lg font-medium tabular-nums text-green-600">
                  -{(((result.referencePrice - result.unitPrice) / result.referencePrice) * 100).toFixed(1)}%
                </p>
              </div>
            )}
          </div>
          {result.appliedRuleName && (
            <div className="bg-muted/50 rounded-md px-3 py-2 text-sm flex items-center gap-2">
              <Zap className="h-3.5 w-3.5 text-amber-500" />
              <span className="text-muted-foreground">Uygulanan kural:</span>
              <span className="font-medium">{result.appliedRuleName}</span>
            </div>
          )}
          {result.strategy === 'NoPriceConfigured' && (
            <p className="text-sm text-destructive">
              Bu tedavinin referans fiyat eşleştirmesi yok. /catalog sayfasından eşleştirme yapın.
            </p>
          )}
        </div>
      )}
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
            <TabsTrigger value="test">
              <FlaskConical className="h-3.5 w-3.5 mr-1.5" />
              Fiyat Testi
            </TabsTrigger>
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

        <TabsContent value="test" className="mt-4">
          <PriceTestTab />
        </TabsContent>

      </Tabs>

      <NewListDialog open={newListOpen} onClose={() => setNewListOpen(false)} />
    </div>
  );
}
