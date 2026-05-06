import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, Building2, Phone, Mail, Globe, MapPin, Clock, CreditCard, StickyNote, FlaskConical } from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LaboratoryItem,
  type CreateLaboratoryPayload,
} from '@/api/laboratories';
import { settingsApi } from '@/api/settings';
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
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface Props {
  open: boolean;
  onClose: () => void;
  editing: LaboratoryItem | null;
  onSubmit: (payload: CreateLaboratoryPayload, selectedBranchPublicIds: string[]) => void;
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

function SectionHeader({ icon: Icon, label }: { icon: React.ElementType; label: string }) {
  return (
    <div className="flex items-center gap-2 pt-1 pb-0.5">
      <Icon className="size-3.5 text-muted-foreground" />
      <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">{label}</span>
      <div className="flex-1 h-px bg-border" />
    </div>
  );
}

export function LaboratoryFormDialog({ open, onClose, editing, onSubmit, isPending }: Props) {
  const qc = useQueryClient();
  const [form, setForm] = useState<FormState>(EMPTY);
  const [selectedBranchIds, setSelectedBranchIds] = useState<string[]>([]);
  const [assignBranchId, setAssignBranchId] = useState('');

  const { data: detail } = useQuery({
    queryKey: ['laboratory-detail', editing?.publicId],
    queryFn: () => laboratoriesApi.getDetail(editing!.publicId).then(r => r.data),
    enabled: open && !!editing,
  });

  const { data: branches = [] } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const assignBranchMut = useMutation({
    mutationFn: () =>
      laboratoriesApi.assignBranch(editing!.publicId, {
        branchPublicId: assignBranchId,
        priority: 1,
        isActive: true,
      }),
    onSuccess: () => {
      toast.success('Şube atandı');
      setAssignBranchId('');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', editing!.publicId] });
      qc.invalidateQueries({ queryKey: ['laboratories'] });
    },
    onError: () => toast.error('Atama yapılamadı'),
  });

  const removeBranchMut = useMutation({
    mutationFn: (assignmentPublicId: string) =>
      laboratoriesApi.removeBranchAssignment(assignmentPublicId),
    onSuccess: () => {
      toast.success('Atama kaldırıldı');
      qc.invalidateQueries({ queryKey: ['laboratory-detail', editing!.publicId] });
      qc.invalidateQueries({ queryKey: ['laboratories'] });
    },
    onError: () => toast.error('Atama kaldırılamadı'),
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
      setSelectedBranchIds([]);
    }
    setAssignBranchId('');
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
    }, selectedBranchIds);
  }

  const assignedBranchIds = new Set((detail?.branchAssignments ?? []).map(a => a.branchPublicId));
  const availableBranches = branches.filter(b => !assignedBranchIds.has(b.publicId));
  const selectedAssignBranchName = availableBranches.find(b => b.publicId === assignBranchId)?.name;

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FlaskConical className="size-4 text-muted-foreground" />
            {editing ? 'Laboratuvarı Düzenle' : 'Yeni Laboratuvar'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-5 py-1">

          {/* Temel bilgiler */}
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label>
                Ad <span className="text-destructive">*</span>
              </Label>
              <Input
                value={form.name}
                onChange={e => set('name', e.target.value)}
                placeholder="Elit Diş Laboratuvarı"
                autoFocus
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Kod</Label>
                <Input
                  value={form.code}
                  onChange={e => set('code', e.target.value.toUpperCase())}
                  placeholder="LAB-001"
                  className="font-mono"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Ödeme Süresi (gün)</Label>
                <Input
                  type="number"
                  min={0}
                  value={form.paymentDays}
                  onChange={e => set('paymentDays', e.target.value)}
                />
              </div>
            </div>
          </div>

          {/* İletişim */}
          <div className="space-y-3">
            <SectionHeader icon={Phone} label="İletişim" />
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Telefon</Label>
                <Input
                  value={form.phone}
                  onChange={e => set('phone', e.target.value)}
                  placeholder="0212 000 00 00"
                />
              </div>
              <div className="space-y-1.5">
                <Label>E-posta</Label>
                <Input
                  type="email"
                  value={form.email}
                  onChange={e => set('email', e.target.value)}
                  placeholder="info@lab.com"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Web Sitesi</Label>
                <Input
                  value={form.website}
                  onChange={e => set('website', e.target.value)}
                  placeholder="www.lab.com"
                />
              </div>
              <div className="space-y-1.5" />
              <div className="space-y-1.5">
                <Label>Yetkili Kişi</Label>
                <Input
                  value={form.contactPerson}
                  onChange={e => set('contactPerson', e.target.value)}
                  placeholder="Ad Soyad"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Yetkili Telefonu</Label>
                <Input
                  value={form.contactPhone}
                  onChange={e => set('contactPhone', e.target.value)}
                  placeholder="0532 000 00 00"
                />
              </div>
            </div>
          </div>

          {/* Adres */}
          <div className="space-y-3">
            <SectionHeader icon={MapPin} label="Adres" />
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Ülke</Label>
                <Input value={form.country} onChange={e => set('country', e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>Şehir</Label>
                <Input value={form.city} onChange={e => set('city', e.target.value)} placeholder="İstanbul" />
              </div>
              <div className="space-y-1.5">
                <Label>İlçe</Label>
                <Input value={form.district} onChange={e => set('district', e.target.value)} placeholder="Kadıköy" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Açık Adres</Label>
              <Textarea
                rows={2}
                value={form.address}
                onChange={e => set('address', e.target.value)}
                placeholder="Sokak, bina no, daire..."
              />
            </div>
          </div>

          {/* Çalışma & Ödeme */}
          <div className="space-y-3">
            <SectionHeader icon={Clock} label="Çalışma & Ödeme" />
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Çalışma Günleri</Label>
                <Input
                  placeholder="Pzt – Cmt"
                  value={form.workingDays}
                  onChange={e => set('workingDays', e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Çalışma Saatleri</Label>
                <Input
                  placeholder="09:00 – 18:00"
                  value={form.workingHours}
                  onChange={e => set('workingHours', e.target.value)}
                />
              </div>
              <div className="space-y-1.5 col-span-2">
                <Label>Ödeme Koşulları</Label>
                <Input
                  placeholder="Ay sonu / Fatura tarihinden 30 gün..."
                  value={form.paymentTerms}
                  onChange={e => set('paymentTerms', e.target.value)}
                />
              </div>
            </div>
          </div>

          {/* Notlar */}
          <div className="space-y-3">
            <SectionHeader icon={StickyNote} label="Notlar" />
            <Textarea
              rows={2}
              value={form.notes}
              onChange={e => set('notes', e.target.value)}
              placeholder="Ek bilgi, özel koşullar..."
            />
          </div>

          {/* Şube Atamaları */}
          <div className="space-y-3">
            <SectionHeader icon={Building2} label="Şube Atamaları" />

            {!editing ? (
              branches.length === 0 ? (
                <p className="text-xs text-muted-foreground italic">Henüz şube tanımlanmamış.</p>
              ) : (
                <div className="border rounded-lg divide-y">
                  {branches.map(b => (
                    <label
                      key={b.publicId}
                      className="flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-muted/40 first:rounded-t-lg last:rounded-b-lg"
                    >
                      <Checkbox
                        checked={selectedBranchIds.includes(b.publicId)}
                        onCheckedChange={checked => {
                          setSelectedBranchIds(prev =>
                            checked ? [...prev, b.publicId] : prev.filter(id => id !== b.publicId),
                          );
                        }}
                      />
                      <span className="text-sm flex-1">{b.name}</span>
                      {!b.isActive && (
                        <Badge variant="secondary" className="text-[10px]">Pasif</Badge>
                      )}
                    </label>
                  ))}
                </div>
              )
            ) : (
              <div className="space-y-2">
                {availableBranches.length > 0 && (
                  <div className="flex items-center gap-2">
                    <Select value={assignBranchId} onValueChange={setAssignBranchId}>
                      <SelectTrigger className="h-9 text-sm flex-1">
                        <SelectValue placeholder="Eklenecek şubeyi seçin…">
                          {selectedAssignBranchName ?? (assignBranchId ? assignBranchId : undefined)}
                        </SelectValue>
                      </SelectTrigger>
                      <SelectContent>
                        {availableBranches.map(b => (
                          <SelectItem key={b.publicId} value={b.publicId}>
                            {b.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button
                      size="sm"
                      className="h-9 shrink-0"
                      disabled={!assignBranchId || assignBranchMut.isPending}
                      onClick={() => assignBranchMut.mutate()}
                    >
                      <Plus className="size-3.5 mr-1" />
                      Ekle
                    </Button>
                  </div>
                )}

                {(detail?.branchAssignments ?? []).length === 0 ? (
                  <p className="text-xs text-muted-foreground italic py-1">Henüz şube atanmamış.</p>
                ) : (
                  <div className="border rounded-lg divide-y">
                    {(detail?.branchAssignments ?? []).map(a => (
                      <div key={a.publicId} className="flex items-center gap-2 px-3 py-2.5">
                        <Building2 className="size-3.5 text-muted-foreground shrink-0" />
                        <span className="flex-1 text-sm">{a.branchName}</span>
                        {a.isActive
                          ? <Badge className="bg-green-100 text-green-800 text-[10px] border-0">Aktif</Badge>
                          : <Badge variant="secondary" className="text-[10px]">Pasif</Badge>}
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                          disabled={removeBranchMut.isPending}
                          onClick={() => removeBranchMut.mutate(a.publicId)}
                        >
                          <Trash2 className="size-3.5" />
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={handleSubmit} disabled={!form.name.trim() || isPending}>
            {isPending
              ? 'Kaydediliyor…'
              : editing
                ? 'Güncelle'
                : selectedBranchIds.length > 0
                  ? `Oluştur ve ${selectedBranchIds.length} Şube Ata`
                  : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
