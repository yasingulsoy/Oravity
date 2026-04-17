import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, Building2, Tag, Pencil } from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LaboratoryPriceItem,
  type UpsertPriceItemPayload,
} from '@/api/laboratories';
import { settingsApi } from '@/api/settings';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
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

interface Props {
  publicId: string;
  open: boolean;
  onClose: () => void;
}

export function LaboratoryDetailSheet({ publicId, open, onClose }: Props) {
  const qc = useQueryClient();
  const [priceDialogOpen, setPriceDialogOpen] = useState(false);
  const [editingPrice, setEditingPrice] = useState<LaboratoryPriceItem | null>(null);
  const [assignBranchId, setAssignBranchId] = useState<string>('');
  const [assignPriority, setAssignPriority] = useState<string>('1');

  const { data: detail, isLoading } = useQuery({
    queryKey: ['laboratory-detail', publicId],
    queryFn: () => laboratoriesApi.getDetail(publicId).then(r => r.data),
    enabled: open,
  });

  const { data: branches } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const assignBranchMut = useMutation({
    mutationFn: () =>
      laboratoriesApi.assignBranch(publicId, {
        branchPublicId: assignBranchId,
        priority: parseInt(assignPriority, 10) || 1,
        isActive: true,
      }),
    onSuccess: () => {
      toast.success('Şube atandı');
      setAssignBranchId('');
      setAssignPriority('1');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', publicId] });
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Atama yapılamadı');
    },
  });

  const removeBranchMut = useMutation({
    mutationFn: (assignmentPublicId: string) =>
      laboratoriesApi.removeBranchAssignment(assignmentPublicId),
    onSuccess: () => {
      toast.success('Atama kaldırıldı');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', publicId] });
    },
    onError: () => toast.error('Atama kaldırılamadı'),
  });

  const priceMut = useMutation({
    mutationFn: (payload: UpsertPriceItemPayload) =>
      laboratoriesApi.upsertPriceItem(publicId, payload),
    onSuccess: () => {
      toast.success('Fiyat kalemi kaydedildi');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', publicId] });
      setPriceDialogOpen(false);
      setEditingPrice(null);
    },
    onError: () => toast.error('Fiyat kalemi kaydedilemedi'),
  });

  const deletePriceMut = useMutation({
    mutationFn: (priceItemPublicId: string) =>
      laboratoriesApi.deletePriceItem(priceItemPublicId),
    onSuccess: () => {
      toast.success('Fiyat kalemi silindi');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', publicId] });
    },
    onError: () => toast.error('Silinemedi'),
  });

  const assignedBranchIds = new Set(
    (detail?.branchAssignments ?? []).map(a => a.branchPublicId),
  );
  const availableBranches = (branches ?? []).filter(
    b => !assignedBranchIds.has(b.publicId),
  );

  return (
    <Sheet open={open} onOpenChange={v => !v && onClose()}>
      <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle>
            {isLoading ? 'Yükleniyor...' : detail?.laboratory.name ?? 'Laboratuvar'}
          </SheetTitle>
        </SheetHeader>

        {isLoading ? (
          <div className="space-y-3 mt-4">
            {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
          </div>
        ) : !detail ? (
          <p className="text-sm text-muted-foreground mt-4">Bulunamadı.</p>
        ) : (
          <div className="mt-4 space-y-4">
            <div className="grid grid-cols-2 gap-3 text-sm rounded-md border p-3 bg-muted/30">
              <Info label="Kod" value={detail.laboratory.code ?? '—'} mono />
              <Info label="Ödeme Süresi" value={`${detail.laboratory.paymentDays} gün`} />
              <Info label="Telefon" value={detail.laboratory.phone ?? '—'} />
              <Info label="E-posta" value={detail.laboratory.email ?? '—'} />
              <Info label="Şehir" value={detail.laboratory.city ?? '—'} />
              <Info label="İlçe" value={detail.laboratory.district ?? '—'} />
              <Info
                label="Şube Atamaları"
                value={String(detail.laboratory.assignedBranchCount)}
              />
              <Info
                label="Aktif İş Emri"
                value={String(detail.laboratory.activeWorkCount)}
              />
            </div>

            <Tabs defaultValue="branches">
              <TabsList>
                <TabsTrigger value="branches">
                  <Building2 className="mr-2 h-4 w-4" />
                  Şubeler ({detail.branchAssignments.length})
                </TabsTrigger>
                <TabsTrigger value="prices">
                  <Tag className="mr-2 h-4 w-4" />
                  Fiyat Listesi ({detail.priceItems.length})
                </TabsTrigger>
              </TabsList>

              <TabsContent value="branches" className="mt-3 space-y-3">
                <div className="flex items-end gap-2 rounded-md border p-3">
                  <div className="flex-1 space-y-1">
                    <Label className="text-xs">Şube</Label>
                    <Select value={assignBranchId} onValueChange={setAssignBranchId}>
                      <SelectTrigger>
                        <SelectValue placeholder="Şube seç" />
                      </SelectTrigger>
                      <SelectContent>
                        {availableBranches.length === 0 && (
                          <div className="px-2 py-1 text-xs text-muted-foreground">
                            Atanabilir şube yok
                          </div>
                        )}
                        {availableBranches.map(b => (
                          <SelectItem key={b.publicId} value={b.publicId}>
                            {b.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="w-24 space-y-1">
                    <Label className="text-xs">Öncelik</Label>
                    <Input
                      type="number"
                      value={assignPriority}
                      onChange={e => setAssignPriority(e.target.value)}
                    />
                  </div>
                  <Button
                    onClick={() => assignBranchMut.mutate()}
                    disabled={!assignBranchId || assignBranchMut.isPending}
                  >
                    <Plus className="mr-1 h-4 w-4" /> Ekle
                  </Button>
                </div>

                {detail.branchAssignments.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-6">
                    Henüz şube atanmamış.
                  </p>
                ) : (
                  <div className="border rounded-md divide-y">
                    {detail.branchAssignments.map(a => (
                      <div key={a.publicId} className="flex items-center gap-3 px-3 py-2">
                        <Building2 className="h-4 w-4 text-muted-foreground" />
                        <span className="flex-1 text-sm font-medium">{a.branchName}</span>
                        <Badge variant="outline" className="text-xs">
                          Öncelik: {a.priority}
                        </Badge>
                        {a.isActive ? (
                          <Badge className="bg-green-100 text-green-800 text-[10px]">Aktif</Badge>
                        ) : (
                          <Badge variant="secondary" className="text-[10px]">Pasif</Badge>
                        )}
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-destructive hover:text-destructive"
                          onClick={() => removeBranchMut.mutate(a.publicId)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </TabsContent>

              <TabsContent value="prices" className="mt-3 space-y-3">
                <div className="flex justify-end">
                  <Button
                    size="sm"
                    onClick={() => { setEditingPrice(null); setPriceDialogOpen(true); }}
                  >
                    <Plus className="mr-1 h-4 w-4" /> Kalem Ekle
                  </Button>
                </div>
                {detail.priceItems.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-6">
                    Fiyat kalemi eklenmemiş.
                  </p>
                ) : (
                  <div className="border rounded-md">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Kalem</TableHead>
                          <TableHead>Kategori</TableHead>
                          <TableHead className="text-right">Fiyat</TableHead>
                          <TableHead>Teslim</TableHead>
                          <TableHead className="w-20"></TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {detail.priceItems.map(p => (
                          <TableRow key={p.publicId}>
                            <TableCell>
                              <div className="font-medium text-sm">{p.itemName}</div>
                              {p.itemCode && (
                                <div className="text-xs font-mono text-muted-foreground">{p.itemCode}</div>
                              )}
                            </TableCell>
                            <TableCell className="text-sm">
                              {p.category ?? '—'}
                            </TableCell>
                            <TableCell className="text-right font-medium">
                              {p.price.toFixed(2)} {p.currency}
                            </TableCell>
                            <TableCell className="text-sm">
                              {p.estimatedDeliveryDays ? `${p.estimatedDeliveryDays}g` : '—'}
                            </TableCell>
                            <TableCell className="text-right">
                              <Button
                                size="sm"
                                variant="ghost"
                                onClick={() => { setEditingPrice(p); setPriceDialogOpen(true); }}
                              >
                                <Pencil className="h-4 w-4" />
                              </Button>
                              <Button
                                size="sm"
                                variant="ghost"
                                className="text-destructive hover:text-destructive"
                                onClick={() => deletePriceMut.mutate(p.publicId)}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                )}
              </TabsContent>
            </Tabs>

            <PriceItemDialog
              open={priceDialogOpen}
              onClose={() => { setPriceDialogOpen(false); setEditingPrice(null); }}
              editing={editingPrice}
              onSubmit={(payload) => priceMut.mutate(payload)}
              isPending={priceMut.isPending}
            />
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}

function Info({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className={mono ? 'font-mono text-sm' : 'text-sm'}>{value}</div>
    </div>
  );
}

// ─── Price Item Dialog ────────────────────────────────────────────────────

interface PriceDialogProps {
  open: boolean;
  onClose: () => void;
  editing: LaboratoryPriceItem | null;
  onSubmit: (payload: UpsertPriceItemPayload) => void;
  isPending: boolean;
}

function PriceItemDialog({ open, onClose, editing, onSubmit, isPending }: PriceDialogProps) {
  const [itemName, setItemName] = useState('');
  const [itemCode, setItemCode] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [currency, setCurrency] = useState('TRY');
  const [pricingType, setPricingType] = useState('PerUnit');
  const [estimatedDeliveryDays, setEstimatedDeliveryDays] = useState('');
  const [category, setCategory] = useState('');

  useEffect(() => {
    if (!open) return;
    if (editing) {
      setItemName(editing.itemName);
      setItemCode(editing.itemCode ?? '');
      setDescription(editing.description ?? '');
      setPrice(String(editing.price));
      setCurrency(editing.currency);
      setPricingType(editing.pricingType ?? 'PerUnit');
      setEstimatedDeliveryDays(
        editing.estimatedDeliveryDays != null ? String(editing.estimatedDeliveryDays) : '',
      );
      setCategory(editing.category ?? '');
    } else {
      setItemName(''); setItemCode(''); setDescription('');
      setPrice(''); setCurrency('TRY'); setPricingType('PerUnit');
      setEstimatedDeliveryDays(''); setCategory('');
    }
  }, [open, editing]);

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{editing ? 'Kalemi Düzenle' : 'Yeni Fiyat Kalemi'}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5 col-span-2">
              <Label>Kalem Adı *</Label>
              <Input value={itemName} onChange={e => setItemName(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label>Kod</Label>
              <Input value={itemCode} onChange={e => setItemCode(e.target.value)} className="font-mono" />
            </div>
            <div className="space-y-1.5">
              <Label>Kategori</Label>
              <Input
                placeholder="Zirkonyum / Porselen / Protez..."
                value={category}
                onChange={e => setCategory(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Fiyat *</Label>
              <Input
                type="number"
                step="0.01"
                value={price}
                onChange={e => setPrice(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Para Birimi</Label>
              <Select value={currency} onValueChange={setCurrency}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="TRY">TRY</SelectItem>
                  <SelectItem value="USD">USD</SelectItem>
                  <SelectItem value="EUR">EUR</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Fiyat Tipi</Label>
              <Select value={pricingType} onValueChange={setPricingType}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="PerUnit">Birim</SelectItem>
                  <SelectItem value="PerTooth">Diş başı</SelectItem>
                  <SelectItem value="Package">Paket</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Tahmini Teslim (gün)</Label>
              <Input
                type="number"
                value={estimatedDeliveryDays}
                onChange={e => setEstimatedDeliveryDays(e.target.value)}
              />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label>Açıklama</Label>
            <Textarea
              rows={2}
              value={description}
              onChange={e => setDescription(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button
            onClick={() =>
              onSubmit({
                publicId: editing?.publicId ?? null,
                itemName: itemName.trim(),
                itemCode: itemCode.trim() || null,
                description: description.trim() || null,
                price: parseFloat(price) || 0,
                currency,
                pricingType,
                estimatedDeliveryDays: estimatedDeliveryDays
                  ? parseInt(estimatedDeliveryDays, 10)
                  : null,
                category: category.trim() || null,
                validFrom: null,
                validUntil: null,
                isActive: true,
              })
            }
            disabled={!itemName.trim() || !price || isPending}
          >
            {isPending ? 'Kaydediliyor...' : editing ? 'Güncelle' : 'Ekle'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
