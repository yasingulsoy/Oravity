import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Eye } from 'lucide-react';
import { toast } from 'sonner';
import { consentFormsApi } from '@/api/consent';
import type { ConsentFormTemplateSummary, ConsentFormTemplateDetail } from '@/api/consent';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';

interface FormState {
  code: string;
  name: string;
  language: string;
  version: string;
  contentHtml: string;
  checkboxesJson: string;
  appliesToAllTreatments: boolean;
  showDentalChart: boolean;
  showTreatmentTable: boolean;
  requireDoctorSignature: boolean;
}

const DEFAULT_CHECKBOXES = JSON.stringify([
  { id: 'risk_acknowledged',    label: 'Tedavinin riskleri hakkında bilgilendirildim', isRequired: true },
  { id: 'alternative_informed', label: 'Alternatif tedavi seçenekleri anlatıldı',       isRequired: true },
  { id: 'questions_answered',   label: 'Sorularım yanıtlandı',                           isRequired: false },
  { id: 'data_consent',         label: 'Kişisel verilerimin işlenmesine izin veriyorum', isRequired: true },
], null, 2);

const DEFAULT_CONTENT = `Röntgen çekiminin, teşhis ve tedavi sürecinde ücretsiz olarak gerçekleştirileceği, çekim sonucuna ait görüntü veya çıktıların hasta tarafından talep edilmesi halinde, bu hizmet ilgili tarifeye göre ayrıca ücretlendirileceği açıklandı.

Hasta haklarıyla ilgili olarak bilgilendirildi.

Hastalık, yapılacak olan tedavi işlemi, bu tedavinin neden ve faydaları, işlem sonrası gereken bakım, beklenen riskler konusunda yeterli ve tatmin edici açıklamalar yapılmıştır.

Yukarıdaki bilgileri okudum, anladım ve onaylıyorum.`;

const TEMPLATE_VARIABLES = [
  { variable: '{HastaAdSoyad}',    label: 'Hasta Adı Soyadı' },
  { variable: '{TCKimlikNo}',      label: 'TC Kimlik No' },
  { variable: '{HastaTelefon}',    label: 'Telefon' },
  { variable: '{HastaYas}',        label: 'Yaş' },
  { variable: '{HastaDogumTarihi}', label: 'Doğum Tarihi' },
  { variable: '{AnneAdi}',         label: 'Anne Adı' },
  { variable: '{BabaAdi}',         label: 'Baba Adı' },
  { variable: '{Adres}',           label: 'Adres' },
  { variable: '{Hekim}',           label: 'Hekim' },
  { variable: '{Klinik}',          label: 'Klinik' },
  { variable: '{Sirket}',          label: 'Şirket' },
  { variable: '{Tarih}',           label: 'Tarih' },
  { variable: '{FormNo}',          label: 'Form No' },
];

const EMPTY_FORM: FormState = {
  code: '',
  name: '',
  language: 'TR',
  version: '1.0',
  contentHtml: DEFAULT_CONTENT,
  checkboxesJson: DEFAULT_CHECKBOXES,
  appliesToAllTreatments: true,
  showDentalChart: true,
  showTreatmentTable: true,
  requireDoctorSignature: false,
};

export function ConsentFormsTab() {
  const qc = useQueryClient();
  const contentRef = useRef<HTMLTextAreaElement>(null);

  const insertVariable = (variable: string) => {
    const el = contentRef.current;
    if (!el) return;
    const start = el.selectionStart ?? el.value.length;
    const end   = el.selectionEnd   ?? el.value.length;
    const newValue = el.value.slice(0, start) + variable + el.value.slice(end);
    set({ contentHtml: newValue });
    // Restore cursor after inserted variable
    requestAnimationFrame(() => {
      el.selectionStart = start + variable.length;
      el.selectionEnd   = start + variable.length;
      el.focus();
    });
  };

  const { data: templates = [], isLoading } = useQuery<ConsentFormTemplateSummary[]>({
    queryKey: ['consent-form-templates'],
    queryFn: () => consentFormsApi.list().then(r => r.data),
  });

  const [editOpen,    setEditOpen]    = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [deleteId,    setDeleteId]    = useState<string | null>(null);
  const [editingId,   setEditingId]   = useState<string | null>(null);
  const [form,        setForm]        = useState<FormState>(EMPTY_FORM);
  const [previewHtml, setPreviewHtml] = useState('');

  const set = (patch: Partial<FormState>) => setForm(prev => ({ ...prev, ...patch }));

  // Fetch detail when editing
  const { data: detail, isLoading: detailLoading } = useQuery<ConsentFormTemplateDetail>({
    queryKey: ['consent-form-template', editingId],
    queryFn: () => consentFormsApi.getById(editingId!).then(r => r.data),
    enabled: !!editingId && editOpen,
  });

  useEffect(() => {
    if (!detail) return;
    set({
      code: detail.code,
      name: detail.name,
      language: detail.language,
      version: detail.version,
      contentHtml: detail.contentHtml,
      checkboxesJson: detail.checkboxesJson,
      appliesToAllTreatments: detail.appliesToAllTreatments,
      showDentalChart: detail.showDentalChart,
      showTreatmentTable: detail.showTreatmentTable,
      requireDoctorSignature: detail.requireDoctorSignature,
    });
  }, [detail]);

  const createMutation = useMutation({
    mutationFn: (data: FormState) => consentFormsApi.create(data),
    onSuccess: () => {
      toast.success('Onam formu şablonu oluşturuldu.');
      setEditOpen(false);
      qc.invalidateQueries({ queryKey: ['consent-form-templates'] });
    },
    onError: (e: any) => toast.error(e?.response?.data?.message ?? 'Oluşturulamadı.'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: FormState }) =>
      consentFormsApi.update(id, data),
    onSuccess: () => {
      toast.success('Onam formu şablonu güncellendi.');
      setEditOpen(false);
      qc.invalidateQueries({ queryKey: ['consent-form-templates'] });
      qc.invalidateQueries({ queryKey: ['consent-form-template', editingId] });
    },
    onError: (e: any) => toast.error(e?.response?.data?.message ?? 'Güncellenemedi.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => consentFormsApi.delete(id),
    onSuccess: () => {
      toast.success('Onam formu şablonu silindi.');
      setDeleteId(null);
      qc.invalidateQueries({ queryKey: ['consent-form-templates'] });
    },
    onError: () => toast.error('Silinemedi.'),
  });

  const openNew = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setEditOpen(true);
  };

  const openEdit = (t: ConsentFormTemplateSummary) => {
    setEditingId(t.publicId);
    setForm(EMPTY_FORM); // will be overwritten by query onSuccess
    setEditOpen(true);
  };

  const openPreview = (t: ConsentFormTemplateSummary) => {
    // Fetch detail for preview
    consentFormsApi.getById(t.publicId).then(r => {
      setPreviewHtml(r.data.contentHtml);
      setPreviewOpen(true);
    });
  };

  const handleSave = () => {
    if (!form.code.trim()) { toast.error('Form kodu zorunludur.'); return; }
    if (!form.name.trim()) { toast.error('Form adı zorunludur.'); return; }
    if (!form.contentHtml.trim()) { toast.error('Form içeriği zorunludur.'); return; }

    if (editingId) {
      updateMutation.mutate({ id: editingId, data: form });
    } else {
      createMutation.mutate(form);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <div>
            <CardTitle>Onam Formu Şablonları</CardTitle>
            <CardDescription>
              Hasta bilgilendirme ve aydınlatılmış onam formlarını yönetin.
            </CardDescription>
          </div>
          <Button size="sm" onClick={openNew}>
            <Plus className="size-3.5 mr-1" />
            Yeni Form
          </Button>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {[1, 2, 3].map(i => <Skeleton key={i} className="h-12 w-full" />)}
            </div>
          ) : templates.length === 0 ? (
            <div className="text-center py-10 text-muted-foreground text-sm">
              Henüz onam formu şablonu eklenmemiş.
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Form Adı</TableHead>
                  <TableHead>Kod</TableHead>
                  <TableHead>Dil</TableHead>
                  <TableHead>Versiyon</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="w-28" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {templates.map(t => (
                  <TableRow key={t.publicId}>
                    <TableCell className="font-medium">{t.name}</TableCell>
                    <TableCell className="font-mono text-xs text-muted-foreground">{t.code}</TableCell>
                    <TableCell>{t.language}</TableCell>
                    <TableCell className="text-muted-foreground">v{t.version}</TableCell>
                    <TableCell>
                      <Badge variant={t.isActive ? 'default' : 'secondary'}>
                        {t.isActive ? 'Aktif' : 'Pasif'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-1">
                        <Button size="sm" variant="ghost" className="h-7 w-7 p-0" onClick={() => openPreview(t)}>
                          <Eye className="size-3.5" />
                        </Button>
                        <Button size="sm" variant="ghost" className="h-7 w-7 p-0" onClick={() => openEdit(t)}>
                          <Pencil className="size-3.5" />
                        </Button>
                        <Button
                          size="sm" variant="ghost"
                          className="h-7 w-7 p-0 text-destructive hover:text-destructive"
                          onClick={() => setDeleteId(t.publicId)}
                        >
                          <Trash2 className="size-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Oluştur / Düzenle dialog */}
      <Dialog open={editOpen} onOpenChange={o => { if (!o) setEditOpen(false); }}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingId ? 'Onam Formu Düzenle' : 'Yeni Onam Formu'}</DialogTitle>
          </DialogHeader>

          {editingId && detailLoading ? (
            <div className="space-y-3 py-4">
              {[1, 2, 3].map(i => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : (
            <div className="space-y-4 py-2">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label>Form Adı <span className="text-destructive">*</span></Label>
                  <Input
                    value={form.name}
                    onChange={e => set({ name: e.target.value })}
                    placeholder="ör. Genel Aydınlatılmış Onam"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label>Kod <span className="text-destructive">*</span></Label>
                  <Input
                    value={form.code}
                    onChange={e => set({ code: e.target.value.toUpperCase() })}
                    placeholder="ör. GENERAL_CONSENT"
                    disabled={!!editingId}
                    className={editingId ? 'opacity-60' : ''}
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label>Dil</Label>
                  <Select value={form.language} onValueChange={v => set({ language: v })}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="TR">Türkçe</SelectItem>
                      <SelectItem value="EN">English</SelectItem>
                      <SelectItem value="DE">Deutsch</SelectItem>
                      <SelectItem value="AR">العربية</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5">
                  <Label>Versiyon</Label>
                  <Input
                    value={form.version}
                    onChange={e => set({ version: e.target.value })}
                    placeholder="1.0"
                  />
                </div>
              </div>

              <div className="space-y-1.5">
                <Label>Form İçeriği <span className="text-destructive">*</span></Label>
                <Textarea
                  ref={contentRef}
                  value={form.contentHtml}
                  onChange={e => set({ contentHtml: e.target.value })}
                  rows={8}
                  placeholder="Form metni..."
                  className="font-mono text-xs"
                />
                <div className="rounded-md border bg-muted/40 p-2 space-y-1.5">
                  <p className="text-xs font-medium text-muted-foreground">Değişkenler — tıklayarak ekleyin:</p>
                  <div className="flex flex-wrap gap-1.5">
                    {TEMPLATE_VARIABLES.map(({ variable, label }) => (
                      <button
                        key={variable}
                        type="button"
                        onClick={() => insertVariable(variable)}
                        className="inline-flex items-center gap-1 rounded border border-border bg-background px-2 py-0.5 text-xs font-mono hover:bg-accent hover:text-accent-foreground transition-colors"
                        title={label}
                      >
                        {variable}
                        <span className="font-sans text-muted-foreground not-italic">{label}</span>
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              <div className="space-y-1.5">
                <Label>Onay Kutucukları (JSON)</Label>
                <Textarea
                  value={form.checkboxesJson}
                  onChange={e => set({ checkboxesJson: e.target.value })}
                  rows={6}
                  className="font-mono text-xs"
                  placeholder='[{"id": "risk", "label": "Riskleri anladım", "isRequired": true}]'
                />
              </div>

              <div className="space-y-2">
                <Label>Seçenekler</Label>
                <div className="space-y-2">
                  {[
                    { key: 'showDentalChart',       label: 'Diş şemasını göster' },
                    { key: 'showTreatmentTable',    label: 'Tedavi tablosunu göster' },
                    { key: 'requireDoctorSignature', label: 'Doktor imzası gerekli' },
                  ].map(({ key, label }) => (
                    <div key={key} className="flex items-center gap-2">
                      <Checkbox
                        id={key}
                        checked={form[key as keyof FormState] as boolean}
                        onCheckedChange={v => set({ [key]: !!v } as Partial<FormState>)}
                      />
                      <label htmlFor={key} className="text-sm cursor-pointer">{label}</label>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>İptal</Button>
            <Button onClick={handleSave} disabled={isPending || (!!editingId && detailLoading)}>
              {isPending ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Önizleme dialog */}
      <Dialog open={previewOpen} onOpenChange={setPreviewOpen}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Form Önizleme</DialogTitle>
          </DialogHeader>
          <div
            className="prose prose-sm max-w-none text-sm whitespace-pre-wrap border rounded-lg p-4"
            dangerouslySetInnerHTML={{ __html: previewHtml || previewHtml.replace(/\n/g, '<br/>') }}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setPreviewOpen(false)}>Kapat</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Sil onayı */}
      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Şablonu Sil</AlertDialogTitle>
            <AlertDialogDescription>
              Bu onam formu şablonu kalıcı olarak silinecek. Devam etmek istiyor musunuz?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteMutation.isPending}>Vazgeç</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={deleteMutation.isPending}
              onClick={() => deleteId && deleteMutation.mutate(deleteId)}
            >
              Evet, Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
