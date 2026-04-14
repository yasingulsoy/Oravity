import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, Plus, Trash2, Link2, AlertCircle, ChevronLeft, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';
import { treatmentsApi, treatmentMappingsApi, type TreatmentCatalogItem, type TreatmentMapping } from '@/api/treatments';
import { pricingApi, type ReferencePriceList, type ReferencePriceItem } from '@/api/pricing';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
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

const QUALITY_OPTIONS = [
  { value: 'exact',       label: 'Birebir (exact)' },
  { value: 'partial',     label: 'Kısmi (partial)' },
  { value: 'approximate', label: 'Yaklaşık (approximate)' },
];

// ─── Add Mapping Dialog ─────────────────────────────────────────────────────

interface AddMappingDialogProps {
  open: boolean;
  onClose: () => void;
  treatment: TreatmentCatalogItem;
  referenceLists: ReferencePriceList[];
  existingMappings: TreatmentMapping[];
}

function AddMappingDialog({ open, onClose, treatment, referenceLists, existingMappings }: AddMappingDialogProps) {
  const qc = useQueryClient();
  const [selectedListId, setSelectedListId] = useState<string>('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [selectedItem, setSelectedItem] = useState<ReferencePriceItem | null>(null);
  const [quality, setQuality] = useState('exact');
  const [notes, setNotes] = useState('');

  const PAGE_SIZE = 20;

  useEffect(() => {
    if (open) {
      setSelectedListId('');
      setSearch('');
      setPage(1);
      setSelectedItem(null);
      setQuality('exact');
      setNotes('');
    }
  }, [open]);

  const { data: itemsPage, isLoading: itemsLoading } = useQuery({
    queryKey: ['pricing', 'ref-items-dialog', selectedListId, search, page],
    queryFn: () => pricingApi.getReferenceItems(Number(selectedListId), { search, page, pageSize: PAGE_SIZE }).then(r => r.data),
    enabled: !!selectedListId,
  });

  const alreadyMappedLists = new Set(existingMappings.map(m => m.referenceListId));

  const createMutation = useMutation({
    mutationFn: () => treatmentMappingsApi.createMapping(treatment.publicId, {
      referenceListId: Number(selectedListId),
      referenceCode: selectedItem!.treatmentCode,
      mappingQuality: quality,
      notes: notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatment-mappings', treatment.publicId] });
      toast.success('Eşleştirme eklendi');
      onClose();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Eşleştirme eklenemedi');
    },
  });

  const totalPages = itemsPage ? Math.ceil(itemsPage.total / PAGE_SIZE) : 0;

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Eşleştirme Ekle</DialogTitle>
          <p className="text-sm text-muted-foreground mt-1">
            <span className="font-medium">{treatment.name}</span> ({treatment.code}) için referans eşleştirme seçin
          </p>
        </DialogHeader>

        <div className="space-y-4">
          {/* List selector */}
          <div className="space-y-1">
            <Label>Referans Liste</Label>
            <Select value={selectedListId} onValueChange={v => { setSelectedListId(v); setSelectedItem(null); setPage(1); setSearch(''); }}>
              <SelectTrigger>
                <SelectValue placeholder="Liste seçin..." />
              </SelectTrigger>
              <SelectContent>
                {referenceLists.map(l => (
                  <SelectItem key={l.id} value={String(l.id)}>
                    <span className="flex items-center gap-2">
                      {l.code} — {l.name}
                      {alreadyMappedLists.has(l.id) && (
                        <Badge variant="secondary" className="text-xs">zaten eşli</Badge>
                      )}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Search in list */}
          {selectedListId && (
            <>
              <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  className="pl-8"
                  placeholder="Kod veya isim ile ara..."
                  value={search}
                  onChange={e => { setSearch(e.target.value); setPage(1); setSelectedItem(null); }}
                />
              </div>

              <div className="border rounded-lg overflow-hidden max-h-52 overflow-y-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-32">Kod</TableHead>
                      <TableHead>İsim</TableHead>
                      <TableHead className="w-24 text-right">Fiyat</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {itemsLoading
                      ? Array.from({ length: 4 }).map((_, i) => (
                        <TableRow key={i}>
                          {[1, 2, 3].map(j => <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>)}
                        </TableRow>
                      ))
                      : itemsPage?.items.length === 0
                      ? (
                        <TableRow>
                          <TableCell colSpan={3} className="text-center text-muted-foreground py-6 text-sm">
                            Sonuç bulunamadı
                          </TableCell>
                        </TableRow>
                      )
                      : itemsPage?.items.map(item => (
                        <TableRow
                          key={item.id}
                          className={`cursor-pointer hover:bg-muted/50 ${selectedItem?.id === item.id ? 'bg-primary/10' : ''}`}
                          onClick={() => setSelectedItem(item)}
                        >
                          <TableCell className="font-mono text-xs">{item.treatmentCode}</TableCell>
                          <TableCell className="text-sm">{item.treatmentName}</TableCell>
                          <TableCell className="text-right text-sm">
                            {item.price > 0 ? item.price.toLocaleString('tr-TR') : '—'}
                          </TableCell>
                        </TableRow>
                      ))
                    }
                  </TableBody>
                </Table>
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between text-sm text-muted-foreground">
                  <span>{itemsPage?.total} kayıt</span>
                  <div className="flex items-center gap-1">
                    <Button size="icon" variant="ghost" className="h-7 w-7"
                      disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                    <span>{page} / {totalPages}</span>
                    <Button size="icon" variant="ghost" className="h-7 w-7"
                      disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}

          {/* Selected item preview */}
          {selectedItem && (
            <div className="rounded-md border bg-muted/30 p-3 text-sm space-y-0.5">
              <p className="font-medium">Seçilen: {selectedItem.treatmentName}</p>
              <p className="text-muted-foreground">Kod: <span className="font-mono">{selectedItem.treatmentCode}</span></p>
            </div>
          )}

          {/* Quality & Notes */}
          {selectedItem && (
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <Label>Eşleştirme Kalitesi</Label>
                <Select value={quality} onValueChange={setQuality}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {QUALITY_OPTIONS.map(o => (
                      <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>Not <span className="text-muted-foreground text-xs">(opsiyonel)</span></Label>
                <Input placeholder="Açıklama..." value={notes} onChange={e => setNotes(e.target.value)} />
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={createMutation.isPending}>İptal</Button>
          <Button
            onClick={() => createMutation.mutate()}
            disabled={createMutation.isPending || !selectedItem}
          >
            {createMutation.isPending ? 'Kaydediliyor...' : 'Eşleştir'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Treatment Mappings Tab ─────────────────────────────────────────────────

export function TreatmentMappingsTab() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [selectedTreatment, setSelectedTreatment] = useState<TreatmentCatalogItem | null>(null);
  const [addDialogOpen, setAddDialogOpen] = useState(false);

  const { data: treatmentsPage, isLoading: treatmentsLoading } = useQuery({
    queryKey: ['treatments', 'list', search],
    queryFn: () => treatmentsApi.list({ search, activeOnly: false, pageSize: 200 }).then(r => r.data),
  });

  const { data: referenceLists } = useQuery({
    queryKey: ['pricing', 'reference-lists'],
    queryFn: () => pricingApi.getReferenceLists().then(r => r.data),
  });

  const { data: mappings, isLoading: mappingsLoading } = useQuery({
    queryKey: ['treatment-mappings', selectedTreatment?.publicId],
    queryFn: () => treatmentMappingsApi.getMappings(selectedTreatment!.publicId).then(r => r.data),
    enabled: !!selectedTreatment,
  });

  // Mapping counts per treatment (fetched lazily — just use mappings for selected)
  // We show a "?" badge for unselected treatments; selecting reveals the real count.

  const deleteMutation = useMutation({
    mutationFn: (mappingId: number) =>
      treatmentMappingsApi.deleteMapping(selectedTreatment!.publicId, mappingId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['treatment-mappings', selectedTreatment?.publicId] });
      toast.success('Eşleştirme silindi');
    },
    onError: () => toast.error('Silme başarısız'),
  });

  const treatments = treatmentsPage?.items ?? [];

  const qualityLabel: Record<string, string> = {
    exact:       'Birebir',
    partial:     'Kısmi',
    approximate: 'Yaklaşık',
  };

  return (
    <div className="flex gap-4 min-h-[500px]">
      {/* Left: Treatment list */}
      <div className="w-72 shrink-0 border rounded-lg flex flex-col">
        <div className="p-3 border-b">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              className="pl-8 h-8 text-sm"
              placeholder="Tedavi ara..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          {treatmentsLoading
            ? Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="px-3 py-2.5 border-b last:border-b-0">
                <Skeleton className="h-4 w-full mb-1" />
                <Skeleton className="h-3 w-20" />
              </div>
            ))
            : treatments.length === 0
            ? (
              <p className="p-4 text-sm text-muted-foreground text-center">Tedavi bulunamadı</p>
            )
            : treatments.map(t => (
              <button
                key={t.publicId}
                className={`w-full text-left px-3 py-2.5 border-b last:border-b-0 hover:bg-muted/50 transition-colors ${
                  selectedTreatment?.publicId === t.publicId ? 'bg-primary/10 border-l-2 border-l-primary' : ''
                }`}
                onClick={() => setSelectedTreatment(t)}
              >
                <p className="text-sm font-medium truncate">{t.name}</p>
                <p className="text-xs text-muted-foreground font-mono">{t.code}</p>
              </button>
            ))
          }
        </div>
      </div>

      {/* Right: Mapping details */}
      <div className="flex-1 border rounded-lg flex flex-col">
        {!selectedTreatment ? (
          <div className="flex-1 flex flex-col items-center justify-center gap-3 text-muted-foreground p-8">
            <Link2 className="h-10 w-10 opacity-30" />
            <div className="text-center">
              <p className="font-medium">Tedavi Seçin</p>
              <p className="text-sm mt-1">
                Soldan bir tedavi seçerek referans listesiyle eşleştirmelerini görüntüleyin ve yönetin.
              </p>
            </div>
          </div>
        ) : (
          <>
            <div className="p-4 border-b flex items-start justify-between gap-4">
              <div>
                <h3 className="font-medium">{selectedTreatment.name}</h3>
                <p className="text-sm text-muted-foreground font-mono mt-0.5">{selectedTreatment.code}</p>
              </div>
              <Button size="sm" onClick={() => setAddDialogOpen(true)}>
                <Plus className="h-4 w-4 mr-1.5" />
                Eşleştirme Ekle
              </Button>
            </div>

            <div className="flex-1 overflow-auto">
              {mappingsLoading ? (
                <div className="p-4 space-y-3">
                  {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
                </div>
              ) : !mappings || mappings.length === 0 ? (
                <div className="flex flex-col items-center justify-center gap-3 p-10 text-muted-foreground">
                  <AlertCircle className="h-8 w-8 opacity-30" />
                  <div className="text-center">
                    <p className="font-medium text-sm">Eşleştirme yok</p>
                    <p className="text-xs mt-1">
                      Bu tedavi henüz hiçbir referans listesiyle eşleştirilmemiş.
                      Fiyatlandırma kuralları bu tedavi için referans fiyatı bulamayacak.
                    </p>
                  </div>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Referans Liste</TableHead>
                      <TableHead className="w-32">Kod</TableHead>
                      <TableHead>Referans İsim</TableHead>
                      <TableHead className="w-28">Kalite</TableHead>
                      <TableHead className="w-12"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {mappings.map(m => (
                      <TableRow key={m.id}>
                        <TableCell>
                          <Badge variant="secondary">{m.referenceListCode}</Badge>
                        </TableCell>
                        <TableCell className="font-mono text-xs">{m.referenceCode}</TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {m.referenceItemName ?? <span className="italic text-xs">bilinmiyor</span>}
                        </TableCell>
                        <TableCell>
                          {m.mappingQuality && (
                            <Badge variant="outline" className="text-xs">
                              {qualityLabel[m.mappingQuality] ?? m.mappingQuality}
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell>
                          <Button
                            size="icon"
                            variant="ghost"
                            className="h-7 w-7 text-destructive hover:text-destructive"
                            onClick={() => deleteMutation.mutate(m.id)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>

            {mappings && mappings.length > 0 && (
              <div className="p-3 border-t bg-muted/20 text-xs text-muted-foreground">
                Bir tedavi birden fazla referans listeyle (TDB, SUT, özel liste) eşleştirilebilir.
                Fiyatlandırma kuralı hangi listeyi kullanacağını formüldeki değişkenle belirler (TDB, SUT, ISAK vb.)
              </div>
            )}
          </>
        )}
      </div>

      {selectedTreatment && (
        <AddMappingDialog
          open={addDialogOpen}
          onClose={() => setAddDialogOpen(false)}
          treatment={selectedTreatment}
          referenceLists={referenceLists ?? []}
          existingMappings={mappings ?? []}
        />
      )}
    </div>
  );
}
