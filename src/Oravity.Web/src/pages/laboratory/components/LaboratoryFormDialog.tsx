import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  laboratoriesApi,
  type LaboratoryItem,
  type CreateLaboratoryPayload,
} from '@/api/laboratories';
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

interface Props {
  open: boolean;
  onClose: () => void;
  editing: LaboratoryItem | null;
  onSubmit: (payload: CreateLaboratoryPayload) => void;
  isPending: boolean;
}

interface FormState {
  name: string;
  code: string;
  phone: string;
  email: string;
  website: string;
  country: string;
  city: string;
  district: string;
  address: string;
  contactPerson: string;
  contactPhone: string;
  workingDays: string;
  workingHours: string;
  paymentTerms: string;
  paymentDays: string;
  notes: string;
}

const EMPTY: FormState = {
  name: '', code: '', phone: '', email: '', website: '',
  country: 'TR', city: '', district: '', address: '',
  contactPerson: '', contactPhone: '',
  workingDays: '', workingHours: '',
  paymentTerms: '', paymentDays: '30',
  notes: '',
};

export function LaboratoryFormDialog({ open, onClose, editing, onSubmit, isPending }: Props) {
  const [form, setForm] = useState<FormState>(EMPTY);

  // When editing, fetch detail to hydrate fields
  const { data: detail } = useQuery({
    queryKey: ['laboratory-detail', editing?.publicId],
    queryFn: () => laboratoriesApi.getDetail(editing!.publicId).then(r => r.data),
    enabled: open && !!editing,
  });

  useEffect(() => {
    if (!open) return;
    if (editing && detail) {
      const l = detail.laboratory;
      setForm({
        name: l.name,
        code: l.code ?? '',
        phone: l.phone ?? '',
        email: l.email ?? '',
        website: l.website ?? '',
        country: l.country ?? 'TR',
        city: l.city ?? '',
        district: l.district ?? '',
        address: l.address ?? '',
        contactPerson: l.contactPerson ?? '',
        contactPhone: l.contactPhone ?? '',
        workingDays: l.workingDays ?? '',
        workingHours: l.workingHours ?? '',
        paymentTerms: l.paymentTerms ?? '',
        paymentDays: String(l.paymentDays ?? 30),
        notes: l.notes ?? '',
      });
    } else if (!editing) {
      setForm(EMPTY);
    }
  }, [open, editing, detail]);

  const set = <K extends keyof FormState>(k: K, v: FormState[K]) =>
    setForm(prev => ({ ...prev, [k]: v }));

  function handleSubmit() {
    if (!form.name.trim()) return;
    onSubmit({
      name: form.name.trim(),
      code: form.code.trim() || null,
      phone: form.phone.trim() || null,
      email: form.email.trim() || null,
      website: form.website.trim() || null,
      country: form.country.trim() || null,
      city: form.city.trim() || null,
      district: form.district.trim() || null,
      address: form.address.trim() || null,
      contactPerson: form.contactPerson.trim() || null,
      contactPhone: form.contactPhone.trim() || null,
      workingDays: form.workingDays.trim() || null,
      workingHours: form.workingHours.trim() || null,
      paymentTerms: form.paymentTerms.trim() || null,
      paymentDays: parseInt(form.paymentDays, 10) || 30,
      notes: form.notes.trim() || null,
    });
  }

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {editing ? 'Laboratuvarı Düzenle' : 'Yeni Laboratuvar'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5 col-span-2">
              <Label>Ad *</Label>
              <Input
                value={form.name}
                onChange={e => set('name', e.target.value)}
                placeholder="Ör. Elit Diş Laboratuvarı"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Kod</Label>
              <Input
                value={form.code}
                onChange={e => set('code', e.target.value)}
                placeholder="LAB-001"
                className="font-mono uppercase"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Ödeme Süresi (gün)</Label>
              <Input
                type="number"
                value={form.paymentDays}
                onChange={e => set('paymentDays', e.target.value)}
              />
            </div>
          </div>

          <div className="space-y-2">
            <div className="text-xs font-semibold text-muted-foreground">İletişim</div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Telefon</Label>
                <Input value={form.phone} onChange={e => set('phone', e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>E-posta</Label>
                <Input
                  type="email"
                  value={form.email}
                  onChange={e => set('email', e.target.value)}
                />
              </div>
              <div className="space-y-1.5 col-span-2">
                <Label>Web Sitesi</Label>
                <Input value={form.website} onChange={e => set('website', e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>Yetkili Kişi</Label>
                <Input
                  value={form.contactPerson}
                  onChange={e => set('contactPerson', e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Yetkili Telefon</Label>
                <Input
                  value={form.contactPhone}
                  onChange={e => set('contactPhone', e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <div className="text-xs font-semibold text-muted-foreground">Adres</div>
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Ülke</Label>
                <Input value={form.country} onChange={e => set('country', e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>Şehir</Label>
                <Input value={form.city} onChange={e => set('city', e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>İlçe</Label>
                <Input value={form.district} onChange={e => set('district', e.target.value)} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Açık Adres</Label>
              <Textarea
                rows={2}
                value={form.address}
                onChange={e => set('address', e.target.value)}
              />
            </div>
          </div>

          <div className="space-y-2">
            <div className="text-xs font-semibold text-muted-foreground">Çalışma</div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Çalışma Günleri</Label>
                <Input
                  placeholder="Pzt-Cmt"
                  value={form.workingDays}
                  onChange={e => set('workingDays', e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Çalışma Saatleri</Label>
                <Input
                  placeholder="09:00-18:00"
                  value={form.workingHours}
                  onChange={e => set('workingHours', e.target.value)}
                />
              </div>
              <div className="space-y-1.5 col-span-2">
                <Label>Ödeme Koşulları</Label>
                <Input
                  placeholder="Ay sonu / Fatura tarihinden 30 gün / vb."
                  value={form.paymentTerms}
                  onChange={e => set('paymentTerms', e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Notlar</Label>
            <Textarea
              rows={2}
              value={form.notes}
              onChange={e => set('notes', e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={handleSubmit} disabled={!form.name.trim() || isPending}>
            {isPending ? 'Kaydediliyor...' : editing ? 'Güncelle' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
