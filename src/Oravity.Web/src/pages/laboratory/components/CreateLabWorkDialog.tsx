import { useEffect, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, Plus, Trash2, User } from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LabWorkItemInput,
  type CreateLabWorkPayload,
} from '@/api/laboratories';
import { patientsApi } from '@/api/patients';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import type { Patient } from '@/types/patient';

interface Props {
  open: boolean;
  onClose: () => void;
}

interface DraftItem {
  key: string;
  labPriceItemPublicId: string | null;
  itemName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  notes: string;
}

function genKey() {
  return `item-${Math.random().toString(36).slice(2, 9)}`;
}

export function CreateLabWorkDialog({ open, onClose }: Props) {
  const qc = useQueryClient();

  // Patient search
  const [patientSearch, setPatientSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);

  // Work form
  const [labId, setLabId] = useState('');
  const [workType, setWorkType] = useState('prosthetic');
  const [deliveryType, setDeliveryType] = useState('pickup');
  const [toothNumbers, setToothNumbers] = useState('');
  const [shadeColor, setShadeColor] = useState('');
  const [doctorNotes, setDoctorNotes] = useState('');
  const [items, setItems] = useState<DraftItem[]>([]);

  useEffect(() => {
    if (!open) {
      setPatientSearch(''); setSelectedPatient(null);
      setLabId('');
      setWorkType('prosthetic'); setDeliveryType('pickup');
      setToothNumbers(''); setShadeColor(''); setDoctorNotes('');
      setItems([]);
    }
  }, [open]);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(patientSearch.trim()), 300);
    return () => clearTimeout(t);
  }, [patientSearch]);

  const { data: patients } = useQuery({
    queryKey: ['patients', 'search', debouncedSearch],
    queryFn: () =>
      patientsApi.list({ search: debouncedSearch, pageSize: 10, page: 1 }).then(r => r.data),
    enabled: open && debouncedSearch.length > 1 && !selectedPatient,
  });

  const { data: labs } = useQuery({
    queryKey: ['laboratories', 'active'],
    queryFn: () => laboratoriesApi.list({ activeOnly: true }).then(r => r.data),
    enabled: open,
    staleTime: 60_000,
  });

  const { data: labDetail } = useQuery({
    queryKey: ['laboratory-detail', labId],
    queryFn: () => laboratoriesApi.getDetail(labId).then(r => r.data),
    enabled: open && !!labId,
  });

  const priceItems = labDetail?.priceItems ?? [];

  const createMut = useMutation({
    mutationFn: (payload: CreateLabWorkPayload) => laboratoriesApi.createWork(payload),
    onSuccess: () => {
      toast.success('İş emri oluşturuldu');
      qc.invalidateQueries({ queryKey: ['lab-works'] });
      onClose();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'İş emri oluşturulamadı');
    },
  });

  function addItem() {
    setItems(prev => [
      ...prev,
      {
        key: genKey(),
        labPriceItemPublicId: null,
        itemName: '',
        quantity: 1,
        unitPrice: 0,
        currency: 'TRY',
        notes: '',
      },
    ]);
  }

  function updateItem(key: string, patch: Partial<DraftItem>) {
    setItems(prev => prev.map(i => (i.key === key ? { ...i, ...patch } : i)));
  }

  function removeItem(key: string) {
    setItems(prev => prev.filter(i => i.key !== key));
  }

  function onSelectPriceItem(key: string, priceItemPublicId: string) {
    const p = priceItems.find(x => x.publicId === priceItemPublicId);
    if (!p) return;
    updateItem(key, {
      labPriceItemPublicId: p.publicId,
      itemName: p.itemName,
      unitPrice: p.price,
      currency: p.currency,
    });
  }

  const canSubmit = useMemo(() => {
    if (!selectedPatient || !labId) return false;
    if (items.length === 0) return false;
    return items.every(i => i.itemName.trim().length > 0 && i.quantity > 0);
  }, [selectedPatient, labId, items]);

  function submit() {
    if (!selectedPatient || !labId) return;
    const labItems: LabWorkItemInput[] = items.map(i => ({
      labPriceItemPublicId: i.labPriceItemPublicId,
      itemName: i.itemName.trim(),
      quantity: i.quantity,
      unitPrice: i.unitPrice,
      currency: i.currency,
      notes: i.notes.trim() || null,
    }));

    createMut.mutate({
      patientPublicId: selectedPatient.publicId,
      laboratoryPublicId: labId,
      branchPublicId: null,
      workType,
      deliveryType,
      toothNumbers: toothNumbers.trim() || null,
      shadeColor: shadeColor.trim() || null,
      doctorNotes: doctorNotes.trim() || null,
      items: labItems,
    });
  }

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Yeni Laboratuvar İş Emri</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {/* Patient */}
          <div className="space-y-1.5">
            <Label>Hasta *</Label>
            {selectedPatient ? (
              <div className="flex items-center gap-2 rounded-md border p-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="flex-1 font-medium">
                  {selectedPatient.firstName} {selectedPatient.lastName}
                </span>
                {selectedPatient.phone && (
                  <Badge variant="outline" className="font-mono text-xs">
                    {selectedPatient.phone}
                  </Badge>
                )}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => { setSelectedPatient(null); setPatientSearch(''); }}
                >
                  Değiştir
                </Button>
              </div>
            ) : (
              <div className="space-y-2">
                <div className="relative">
                  <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input
                    className="pl-8"
                    placeholder="Ad soyad / telefon / TC..."
                    value={patientSearch}
                    onChange={e => setPatientSearch(e.target.value)}
                  />
                </div>
                {debouncedSearch.length > 1 && patients && (
                  <div className="border rounded-md max-h-40 overflow-y-auto">
                    {patients.items.length === 0 ? (
                      <p className="text-sm text-muted-foreground text-center py-4">Sonuç yok</p>
                    ) : (
                      patients.items.map(p => (
                        <button
                          key={p.publicId}
                          type="button"
                          className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-center gap-2 border-b last:border-b-0"
                          onClick={() => setSelectedPatient(p)}
                        >
                          <User className="h-4 w-4 text-muted-foreground" />
                          <span>{p.firstName} {p.lastName}</span>
                          {p.phone && (
                            <span className="text-xs font-mono text-muted-foreground ml-auto">
                              {p.phone}
                            </span>
                          )}
                        </button>
                      ))
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Lab */}
          <div className="space-y-1.5">
            <Label>Laboratuvar *</Label>
            <select
              value={labId}
              onChange={e => setLabId(e.target.value)}
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
            >
              <option value="">Laboratuvar seçin…</option>
              {(labs ?? []).map(l => (
                <option key={l.publicId} value={l.publicId}>{l.name}</option>
              ))}
            </select>
          </div>

          {/* Work type */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>İş Tipi *</Label>
              <select
                value={workType}
                onChange={e => setWorkType(e.target.value)}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
              >
                <option value="prosthetic">Protetik</option>
                <option value="orthodontic">Ortodontik</option>
                <option value="implant">İmplant</option>
                <option value="other">Diğer</option>
              </select>
            </div>
            <div className="space-y-1.5">
              <Label>Teslim Tipi</Label>
              <select
                value={deliveryType}
                onChange={e => setDeliveryType(e.target.value)}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
              >
                <option value="pickup">Elden</option>
                <option value="courier">Kargo/Kurye</option>
                <option value="digital">Dijital</option>
              </select>
            </div>
            <div className="space-y-1.5">
              <Label>Diş Numaraları</Label>
              <Input
                value={toothNumbers}
                onChange={e => setToothNumbers(e.target.value)}
                placeholder="11,12,21 veya 36-37"
                className="font-mono"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Renk (Shade)</Label>
              <Input
                value={shadeColor}
                onChange={e => setShadeColor(e.target.value)}
                placeholder="A2, B1..."
              />
            </div>
          </div>

          {/* Items */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Kalemler *</Label>
              <Button size="sm" variant="outline" onClick={addItem} disabled={!labId}>
                <Plus className="mr-1 h-3.5 w-3.5" /> Kalem
              </Button>
            </div>
            {!labId ? (
              <p className="text-xs text-muted-foreground italic">
                Önce laboratuvar seçin, ardından kalemler eklenebilir.
              </p>
            ) : items.length === 0 ? (
              <p className="text-xs text-muted-foreground italic">
                Henüz kalem yok. "Kalem" ile ekleyin.
              </p>
            ) : (
              <div className="space-y-2">
                {items.map(i => (
                  <div key={i.key} className="rounded-md border p-3 space-y-2">
                    <div className="flex items-center gap-2">
                      <select
                        value={i.labPriceItemPublicId ?? '__free__'}
                        onChange={e => {
                          if (e.target.value === '__free__') {
                            updateItem(i.key, { labPriceItemPublicId: null });
                          } else {
                            onSelectPriceItem(i.key, e.target.value);
                          }
                        }}
                        className="h-9 flex-1 rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                      >
                        <option value="__free__">Serbest kalem</option>
                        {priceItems.map(p => (
                          <option key={p.publicId} value={p.publicId}>
                            {p.itemName} — {p.price.toFixed(2)} {p.currency}
                          </option>
                        ))}
                      </select>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive hover:text-destructive"
                        onClick={() => removeItem(i.key)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                    <div className="grid grid-cols-4 gap-2">
                      <div className="col-span-2 space-y-1">
                        <Label className="text-xs">Kalem Adı</Label>
                        <Input
                          value={i.itemName}
                          onChange={e => updateItem(i.key, { itemName: e.target.value })}
                          disabled={!!i.labPriceItemPublicId}
                        />
                      </div>
                      <div className="space-y-1">
                        <Label className="text-xs">Adet</Label>
                        <Input
                          type="number"
                          min={1}
                          value={i.quantity}
                          onChange={e => updateItem(i.key, { quantity: parseInt(e.target.value, 10) || 1 })}
                        />
                      </div>
                      <div className="space-y-1">
                        <Label className="text-xs">Birim ({i.currency})</Label>
                        <Input
                          type="number"
                          step="0.01"
                          value={i.unitPrice}
                          onChange={e => updateItem(i.key, { unitPrice: parseFloat(e.target.value) || 0 })}
                          disabled={!!i.labPriceItemPublicId}
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="space-y-1.5">
            <Label>Hekim Notları</Label>
            <Textarea
              rows={2}
              value={doctorNotes}
              onChange={e => setDoctorNotes(e.target.value)}
              placeholder="Lab için açıklama..."
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={submit} disabled={!canSubmit || createMut.isPending}>
            {createMut.isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
