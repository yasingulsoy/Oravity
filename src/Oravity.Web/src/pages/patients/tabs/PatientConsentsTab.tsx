import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ClipboardCheck, QrCode, XCircle, Clock, Download,
  Plus, ExternalLink, AlertTriangle,
} from 'lucide-react';
import { consentInstancesApi, consentFormsApi } from '@/api/consent';
import type { ConsentInstanceResponse } from '@/api/consent';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { toast } from 'sonner';

function statusBadge(status: string) {
  switch (status) {
    case 'İmzalandı':
      return <Badge className="bg-emerald-100 text-emerald-700 border-emerald-200"><ClipboardCheck className="size-3 mr-1" />İmzalandı</Badge>;
    case 'İmza Bekliyor':
      return <Badge className="bg-blue-100 text-blue-700 border-blue-200"><QrCode className="size-3 mr-1" />İmza Bekliyor</Badge>;
    case 'Süresi Doldu':
      return <Badge className="bg-amber-100 text-amber-700 border-amber-200"><Clock className="size-3 mr-1" />Süresi Doldu</Badge>;
    case 'İptal':
      return <Badge className="bg-gray-100 text-gray-500 border-gray-200"><XCircle className="size-3 mr-1" />İptal</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}

function QrDialog({ consent, open, onClose }: {
  consent: ConsentInstanceResponse;
  open: boolean;
  onClose: () => void;
}) {
  const url = consent.qrToken
    ? `${window.location.origin}/onam/${consent.qrToken}`
    : null;

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>QR Kod — {consent.formTemplateName}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 text-sm">
          <p className="text-muted-foreground">Form No: {consent.consentCode}</p>
          {url && (
            <>
              <div className="flex justify-center p-4 bg-white border rounded-lg">
                <img
                  src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(url)}`}
                  alt="QR Kod"
                  className="size-44"
                />
              </div>
              <a
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 text-xs text-blue-600 hover:underline break-all"
              >
                <ExternalLink className="size-3 shrink-0" />
                {url}
              </a>
            </>
          )}
          {consent.qrTokenExpiresAt && (
            <p className="text-xs text-muted-foreground">
              Son geçerlilik: {new Date(consent.qrTokenExpiresAt).toLocaleString('tr-TR')}
            </p>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Kapat</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function NewConsentDialog({ patientPublicId, open, onClose }: {
  patientPublicId: string;
  open: boolean;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [templateId, setTemplateId] = useState('');
  const [delivery, setDelivery]     = useState('qr');

  const { data: templates, isLoading } = useQuery({
    queryKey: ['consent-forms-active'],
    queryFn: () => consentFormsApi.list(true).then(r => r.data),
    enabled: open,
  });

  const createMutation = useMutation({
    mutationFn: () => consentInstancesApi.create({
      patientPublicId,
      formTemplatePublicId: templateId,
      itemPublicIds: [],
      deliveryMethod: delivery,
    }),
    onSuccess: () => {
      toast.success('Onam formu oluşturuldu');
      qc.invalidateQueries({ queryKey: ['patient-consents', patientPublicId] });
      onClose();
      setTemplateId('');
    },
    onError: () => toast.error('Onam formu oluşturulamadı'),
  });

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Yeni Onam Formu Oluştur</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Onam Formu Şablonu</Label>
            {isLoading ? (
              <Skeleton className="h-9 w-full" />
            ) : (
              <Select value={templateId} onValueChange={setTemplateId}>
                <SelectTrigger>
                  <SelectValue placeholder="Şablon seçin..." />
                </SelectTrigger>
                <SelectContent>
                  {templates?.map(t => (
                    <SelectItem key={t.publicId} value={t.publicId}>
                      {t.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
          <div className="space-y-1.5">
            <Label>Gönderim Yöntemi</Label>
            <Select value={delivery} onValueChange={setDelivery}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="qr">QR Kod</SelectItem>
                <SelectItem value="sms">SMS</SelectItem>
                <SelectItem value="both">QR + SMS</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button
            disabled={!templateId || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function PatientConsentsTab({ patientPublicId }: { patientPublicId: string }) {
  const qc = useQueryClient();
  const [qrOpen, setQrOpen]     = useState<ConsentInstanceResponse | null>(null);
  const [newOpen, setNewOpen]   = useState(false);
  const [cancelTarget, setCancelTarget] = useState<ConsentInstanceResponse | null>(null);

  const { data: consents, isLoading } = useQuery({
    queryKey: ['patient-consents', patientPublicId],
    queryFn: () => consentInstancesApi.getByPatient(patientPublicId).then(r => r.data),
  });

  const cancelMutation = useMutation({
    mutationFn: (publicId: string) => consentInstancesApi.cancel(publicId),
    onSuccess: () => {
      toast.success('Onam formu iptal edildi');
      qc.invalidateQueries({ queryKey: ['patient-consents', patientPublicId] });
      setCancelTarget(null);
    },
    onError: () => toast.error('İptal işlemi başarısız'),
  });

  const downloadPdf = async (c: ConsentInstanceResponse) => {
    try {
      const res = await consentInstancesApi.downloadPdf(c.publicId);
      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url;
      a.download = `onam-${c.consentCode}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('PDF indirilemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map(i => <Skeleton key={i} className="h-16 w-full" />)}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-semibold">Onam Formları</h3>
        <Button size="sm" onClick={() => setNewOpen(true)}>
          <Plus className="size-4 mr-1" />
          Yeni Onam Formu
        </Button>
      </div>

      {!consents?.length ? (
        <div className="text-center py-12 text-muted-foreground text-sm border rounded-lg">
          Henüz onam formu bulunmuyor.
        </div>
      ) : (
        <div className="border rounded-lg divide-y">
          {consents.map(c => (
            <div key={c.publicId} className="flex items-center gap-3 px-4 py-3">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-medium text-sm">{c.formTemplateName}</span>
                  {statusBadge(c.status)}
                </div>
                <div className="text-xs text-muted-foreground mt-0.5 flex items-center gap-3 flex-wrap">
                  <span>Form No: {c.consentCode}</span>
                  <span>{new Date(c.createdAt).toLocaleDateString('tr-TR')}</span>
                  {c.treatmentPlanPublicId && (
                    <span className="text-blue-600">Tedavi planına bağlı</span>
                  )}
                  {c.signerName && (
                    <span>İmzalayan: {c.signerName}</span>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-1 shrink-0">
                {c.status === 'İmza Bekliyor' && c.qrToken && (
                  <Button size="icon" variant="ghost" className="size-8 text-blue-600"
                    title="QR Kod Göster"
                    onClick={() => setQrOpen(c)}
                  >
                    <QrCode className="size-4" />
                  </Button>
                )}
                {c.status === 'İmzalandı' && (
                  <Button size="icon" variant="ghost" className="size-8 text-emerald-600"
                    title="PDF İndir"
                    onClick={() => downloadPdf(c)}
                  >
                    <Download className="size-4" />
                  </Button>
                )}
                {(c.status === 'İmza Bekliyor' || c.status === 'İmzalandı') && (
                  <Button size="icon" variant="ghost" className="size-8 text-destructive"
                    title="İptal Et"
                    onClick={() => setCancelTarget(c)}
                  >
                    <XCircle className="size-4" />
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* QR dialog */}
      {qrOpen && (
        <QrDialog consent={qrOpen} open onClose={() => setQrOpen(null)} />
      )}

      {/* Yeni onam dialog */}
      <NewConsentDialog
        patientPublicId={patientPublicId}
        open={newOpen}
        onClose={() => setNewOpen(false)}
      />

      {/* İptal onay dialog */}
      <Dialog open={!!cancelTarget} onOpenChange={() => setCancelTarget(null)}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Onam Formunu İptal Et</DialogTitle>
          </DialogHeader>
          <div className="space-y-3 text-sm">
            <div className="flex gap-2 p-3 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
              <AlertTriangle className="size-4 shrink-0 mt-0.5" />
              <p>
                <strong>{cancelTarget?.formTemplateName}</strong> ({cancelTarget?.consentCode}) formunu
                iptal etmek istediğinize emin misiniz?
              </p>
            </div>
            {cancelTarget?.status === 'İmzalandı' && (
              <p className="text-destructive text-xs">
                Bu form imzalanmış. İptal edilirse tedavi geri plana alınabilir hale gelir.
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelTarget(null)}>Vazgeç</Button>
            <Button
              variant="destructive"
              disabled={cancelMutation.isPending}
              onClick={() => cancelMutation.mutate(cancelTarget!.publicId)}
            >
              {cancelMutation.isPending ? 'İptal ediliyor...' : 'İptal Et'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
