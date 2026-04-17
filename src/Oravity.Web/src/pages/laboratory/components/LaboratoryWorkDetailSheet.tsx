import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import {
  Send, PackageCheck, Smile, Check, X, Ban, Clock, History, Wrench,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LabWorkStatus,
  type LabWorkTransitionAction,
} from '@/api/laboratories';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
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

const STATUS_LABEL: Record<LabWorkStatus, string> = {
  pending: 'Taslak', sent: 'Gönderildi', in_progress: 'Yapılıyor', ready: 'Hazır',
  received: 'Teslim Alındı', fitted: 'Takıldı', completed: 'Tamamlandı',
  approved: 'Onaylandı', rejected: 'Reddedildi', cancelled: 'İptal',
};

interface ActionDef {
  action: LabWorkTransitionAction;
  label: string;
  icon: React.ElementType;
  variant?: 'default' | 'destructive' | 'outline';
  requireNotes?: boolean;
}

/** Mevcut duruma göre yapılabilecek geçişler. */
function getAvailableActions(status: LabWorkStatus): ActionDef[] {
  switch (status) {
    case 'pending':
      return [
        { action: 'send', label: 'Laboratuvara Gönder', icon: Send },
        { action: 'cancel', label: 'İptal Et', icon: Ban, variant: 'outline' },
      ];
    case 'sent':
      return [
        { action: 'in_progress', label: 'Yapım Başladı', icon: Wrench },
        { action: 'ready', label: 'Hazır', icon: PackageCheck },
        { action: 'cancel', label: 'İptal Et', icon: Ban, variant: 'outline' },
      ];
    case 'in_progress':
      return [
        { action: 'ready', label: 'Hazır', icon: PackageCheck },
        { action: 'cancel', label: 'İptal Et', icon: Ban, variant: 'outline' },
      ];
    case 'ready':
      return [
        { action: 'receive', label: 'Klinikte Teslim Alındı', icon: PackageCheck },
      ];
    case 'received':
      return [
        { action: 'fit', label: 'Hastaya Takıldı', icon: Smile },
      ];
    case 'fitted':
      return [
        { action: 'complete', label: 'Tamamla (Onaya Gönder)', icon: Check },
      ];
    case 'completed':
      return [
        { action: 'approve', label: 'Onayla', icon: Check },
        { action: 'reject', label: 'Reddet', icon: X, variant: 'destructive', requireNotes: true },
      ];
    default:
      return [];
  }
}

export function LaboratoryWorkDetailSheet({ publicId, open, onClose }: Props) {
  const qc = useQueryClient();
  const [actionDialog, setActionDialog] = useState<ActionDef | null>(null);
  const [actionNotes, setActionNotes] = useState('');

  const { data: work, isLoading } = useQuery({
    queryKey: ['lab-work-detail', publicId],
    queryFn: () => laboratoriesApi.getWorkDetail(publicId).then(r => r.data),
    enabled: open,
  });

  const transitionMut = useMutation({
    mutationFn: ({ action, notes }: { action: LabWorkTransitionAction; notes?: string }) =>
      laboratoriesApi.transitionWork(publicId, { action, notes: notes ?? null }),
    onSuccess: () => {
      toast.success('Durum güncellendi');
      qc.invalidateQueries({ queryKey: ['lab-work-detail', publicId] });
      qc.invalidateQueries({ queryKey: ['lab-works'] });
      setActionDialog(null);
      setActionNotes('');
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Durum değiştirilemedi');
    },
  });

  const actions = work ? getAvailableActions(work.status) : [];

  return (
    <Sheet open={open} onOpenChange={v => !v && onClose()}>
      <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle>
            {isLoading ? 'Yükleniyor...' : (
              <div className="flex items-center gap-3">
                <span className="font-mono text-sm">{work?.workNo}</span>
                {work && (
                  <Badge variant="outline">{STATUS_LABEL[work.status]}</Badge>
                )}
              </div>
            )}
          </SheetTitle>
        </SheetHeader>

        {isLoading ? (
          <div className="space-y-3 mt-4">
            {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
          </div>
        ) : !work ? (
          <p className="text-sm text-muted-foreground mt-4">Bulunamadı.</p>
        ) : (
          <div className="mt-4 space-y-5">
            {/* Actions */}
            {actions.length > 0 && (
              <div className="rounded-md border bg-muted/30 p-3 space-y-2">
                <div className="text-xs font-semibold text-muted-foreground">
                  İŞLEMLER
                </div>
                <div className="flex flex-wrap gap-2">
                  {actions.map(a => (
                    <Button
                      key={a.action}
                      size="sm"
                      variant={a.variant ?? 'default'}
                      onClick={() => {
                        if (a.requireNotes) {
                          setActionDialog(a);
                          setActionNotes('');
                        } else {
                          transitionMut.mutate({ action: a.action });
                        }
                      }}
                      disabled={transitionMut.isPending}
                    >
                      <a.icon className="mr-1 h-4 w-4" />
                      {a.label}
                    </Button>
                  ))}
                </div>
              </div>
            )}

            {/* Summary */}
            <div className="grid grid-cols-2 gap-3 text-sm rounded-md border p-3">
              <Info label="Hasta" value={work.patientFullName} />
              <Info label="Hekim" value={work.doctorFullName} />
              <Info label="Laboratuvar" value={work.laboratoryName} />
              <Info label="Şube" value={work.branchName} />
              <Info label="Tip" value={work.workType} />
              <Info label="Teslim Tipi" value={work.deliveryType} />
              <Info label="Diş Numaraları" value={work.toothNumbers ?? '—'} />
              <Info label="Renk" value={work.shadeColor ?? '—'} />
              <Info
                label="Gönderim Tarihi"
                value={work.sentToLabAt ? format(new Date(work.sentToLabAt), 'dd.MM.yyyy HH:mm') : '—'}
              />
              <Info
                label="Tahmini Teslim"
                value={work.estimatedDeliveryDate ?? '—'}
              />
              <Info
                label="Teslim Alınma"
                value={work.receivedFromLabAt ? format(new Date(work.receivedFromLabAt), 'dd.MM.yyyy HH:mm') : '—'}
              />
              <Info
                label="Takılma"
                value={work.fittedToPatientAt ? format(new Date(work.fittedToPatientAt), 'dd.MM.yyyy HH:mm') : '—'}
              />
              <Info
                label="Toplam"
                value={work.totalCost != null ? `${work.totalCost.toFixed(2)} ${work.currency}` : '—'}
              />
              <Info
                label="Oluşturulma"
                value={format(new Date(work.createdAt), 'dd.MM.yyyy HH:mm')}
              />
            </div>

            {/* Notes */}
            {(work.doctorNotes || work.labNotes || work.approvalNotes) && (
              <div className="space-y-2">
                {work.doctorNotes && (
                  <Notes label="Hekim Notu" text={work.doctorNotes} />
                )}
                {work.labNotes && (
                  <Notes label="Laboratuvar Notu" text={work.labNotes} />
                )}
                {work.approvalNotes && (
                  <Notes label="Onay Notu" text={work.approvalNotes} />
                )}
              </div>
            )}

            {/* Items */}
            <div>
              <div className="text-xs font-semibold text-muted-foreground mb-2">
                KALEMLER
              </div>
              <div className="border rounded-md">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Kalem</TableHead>
                      <TableHead className="text-right">Adet</TableHead>
                      <TableHead className="text-right">Birim</TableHead>
                      <TableHead className="text-right">Toplam</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {work.items.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={4} className="text-center py-4 text-muted-foreground">
                          Kalem yok
                        </TableCell>
                      </TableRow>
                    ) : (
                      work.items.map(i => (
                        <TableRow key={i.publicId}>
                          <TableCell>
                            <div className="font-medium text-sm">{i.itemName}</div>
                            {i.notes && (
                              <div className="text-xs text-muted-foreground">{i.notes}</div>
                            )}
                          </TableCell>
                          <TableCell className="text-right">{i.quantity}</TableCell>
                          <TableCell className="text-right">{i.unitPrice.toFixed(2)} {i.currency}</TableCell>
                          <TableCell className="text-right font-medium">
                            {i.totalPrice.toFixed(2)} {i.currency}
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>
            </div>

            {/* History */}
            <div>
              <div className="flex items-center gap-2 mb-2">
                <History className="h-4 w-4 text-muted-foreground" />
                <span className="text-xs font-semibold text-muted-foreground">
                  DURUM GEÇMİŞİ
                </span>
              </div>
              {work.history.length === 0 ? (
                <p className="text-xs text-muted-foreground italic">Kayıt yok.</p>
              ) : (
                <div className="space-y-1.5">
                  {work.history.map((h, idx) => (
                    <div
                      key={idx}
                      className="flex items-start gap-3 text-xs rounded-md bg-muted/40 px-3 py-2"
                    >
                      <Clock className="h-3.5 w-3.5 mt-0.5 shrink-0 text-muted-foreground" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          {h.oldStatus && (
                            <>
                              <Badge variant="outline" className="text-[10px]">
                                {STATUS_LABEL[h.oldStatus as LabWorkStatus] ?? h.oldStatus}
                              </Badge>
                              <span>→</span>
                            </>
                          )}
                          <Badge variant="secondary" className="text-[10px]">
                            {STATUS_LABEL[h.newStatus as LabWorkStatus] ?? h.newStatus}
                          </Badge>
                        </div>
                        {h.notes && (
                          <p className="text-muted-foreground mt-1">{h.notes}</p>
                        )}
                      </div>
                      <span className="text-muted-foreground shrink-0">
                        {format(new Date(h.changedAt), 'dd.MM HH:mm')}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </SheetContent>

      {/* Notes-required action dialog */}
      <Dialog open={!!actionDialog} onOpenChange={v => !v && setActionDialog(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{actionDialog?.label}</DialogTitle>
          </DialogHeader>
          <div className="space-y-2 py-2">
            <Label>Not / Gerekçe {actionDialog?.requireNotes && '*'}</Label>
            <Textarea
              rows={3}
              value={actionNotes}
              onChange={e => setActionNotes(e.target.value)}
              placeholder="Açıklama girin..."
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setActionDialog(null)}>
              İptal
            </Button>
            <Button
              variant={actionDialog?.variant ?? 'default'}
              onClick={() => {
                if (!actionDialog) return;
                transitionMut.mutate({
                  action: actionDialog.action,
                  notes: actionNotes.trim() || undefined,
                });
              }}
              disabled={
                transitionMut.isPending ||
                (actionDialog?.requireNotes && !actionNotes.trim())
              }
            >
              Onayla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Sheet>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="text-sm">{value}</div>
    </div>
  );
}

function Notes({ label, text }: { label: string; text: string }) {
  return (
    <div className="rounded-md border p-3">
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground mb-1">
        {label}
      </div>
      <p className="text-sm whitespace-pre-wrap">{text}</p>
    </div>
  );
}
