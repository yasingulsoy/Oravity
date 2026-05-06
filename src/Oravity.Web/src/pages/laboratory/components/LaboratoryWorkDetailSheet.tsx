import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import {
  Send, PackageCheck, Smile, Check, X, Ban, Clock, History, Wrench, Zap,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LabWorkStatus,
  type LabWorkTransitionAction,
  type LaboratoryWorkDetail,
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
import { cn } from '@/lib/utils';

interface Props {
  publicId: string;
  open: boolean;
  onClose: () => void;
}

export const STATUS_LABEL: Record<LabWorkStatus, string> = {
  pending: 'Taslak', sent: 'Gönderildi', in_progress: 'Yapılıyor', ready: 'Hazır',
  received: 'Teslim Alındı', fitted: 'Takıldı', completed: 'Tamamlandı',
  approved: 'Onaylandı', rejected: 'Reddedildi', cancelled: 'İptal',
};

// ─── Workflow stepper ──────────────────────────────────────────────────────

const MAIN_STEPS: { key: LabWorkStatus; label: string }[] = [
  { key: 'pending',     label: 'Taslak'        },
  { key: 'sent',        label: 'Gönderildi'    },
  { key: 'in_progress', label: 'Yapılıyor'     },
  { key: 'ready',       label: 'Hazır'         },
  { key: 'received',    label: 'Alındı'        },
  { key: 'fitted',      label: 'Takıldı'       },
  { key: 'completed',   label: 'Tamamlandı'    },
  { key: 'approved',    label: 'Onaylandı'     },
];

const STEP_ORDER = MAIN_STEPS.map(s => s.key);

function getStepDate(stepKey: LabWorkStatus, work: LaboratoryWorkDetail): Date | null {
  const direct: Partial<Record<LabWorkStatus, string | null>> = {
    pending:   work.createdAt,
    sent:      work.sentToLabAt,
    received:  work.receivedFromLabAt,
    fitted:    work.fittedToPatientAt,
    completed: work.completedAt,
    approved:  work.approvedAt,
  };
  if (direct[stepKey]) return new Date(direct[stepKey]!);
  const h = work.history.find(e => e.newStatus === stepKey);
  return h ? new Date(h.changedAt) : null;
}

function WorkflowStepper({ work }: { work: LaboratoryWorkDetail }) {
  const isCancelled = work.status === 'cancelled';
  const isRejected  = work.status === 'rejected';

  return (
    <div className="rounded-md border bg-muted/20 p-3">
      <div className="flex items-start">
        {MAIN_STEPS.map((step, idx) => {
          const stepIdx    = STEP_ORDER.indexOf(step.key);
          const currentIdx = STEP_ORDER.indexOf(work.status);
          const visited    = work.history.some(h => h.newStatus === step.key) || step.key === 'pending';
          const isCurrent  = step.key === work.status && !isCancelled && !isRejected;
          const isDone     = !isCancelled && !isRejected && stepIdx < currentIdx && visited;
          const isSkipped  = !isCancelled && !isRejected && stepIdx < currentIdx && !visited && step.key !== 'pending';
          const stepDate   = isDone || isCurrent ? getStepDate(step.key, work) : null;

          return (
            <div key={step.key} className="flex items-start flex-1 min-w-0">
              {idx > 0 && (
                <div className={cn(
                  'h-px mt-[11px] flex-1 shrink-0',
                  isDone ? 'bg-primary' : isSkipped ? 'bg-primary/30' : 'bg-border'
                )} />
              )}
              <div className="flex flex-col items-center gap-0.5 shrink-0">
                <div className={cn(
                  'size-5 rounded-full flex items-center justify-center text-[9px] font-bold border-2 transition-colors',
                  isCurrent  ? 'bg-primary border-primary text-primary-foreground' :
                  isDone     ? 'bg-primary border-primary text-primary-foreground' :
                  isSkipped  ? 'border-primary/40 bg-background text-primary/40' :
                               'border-border bg-background text-muted-foreground'
                )}>
                  {isDone ? <Check className="size-2.5" /> :
                   isSkipped ? <span>—</span> :
                   <span>{idx + 1}</span>}
                </div>
                <span className={cn(
                  'text-[9px] leading-tight text-center max-w-[42px] break-words',
                  isCurrent ? 'font-semibold text-primary' :
                  isDone    ? 'text-muted-foreground' :
                              'text-muted-foreground/50'
                )}>
                  {step.label}
                </span>
                {stepDate && (
                  <span className="text-[8px] text-muted-foreground leading-tight">
                    {format(stepDate, 'dd.MM HH:mm')}
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {(isCancelled || isRejected) && (
        <div className="mt-2 flex justify-center">
          <Badge variant="destructive" className="text-xs">
            {isCancelled ? 'İptal Edildi' : 'Reddedildi'}
          </Badge>
        </div>
      )}
    </div>
  );
}

// ─── Actions ──────────────────────────────────────────────────────────────

interface ActionDef {
  action: LabWorkTransitionAction;
  label: string;
  icon: React.ElementType;
  variant?: 'default' | 'destructive' | 'outline';
  requireNotes?: boolean;
  showDialog?: boolean;
}

const FAST_COMPLETE: ActionDef = {
  action: 'fast_complete',
  label: 'Hızlı Tamamla',
  icon: Zap,
  variant: 'outline',
  showDialog: true,
};

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
        FAST_COMPLETE,
        { action: 'cancel', label: 'İptal Et', icon: Ban, variant: 'outline' },
      ];
    case 'in_progress':
      return [
        { action: 'ready', label: 'Hazır', icon: PackageCheck },
        FAST_COMPLETE,
        { action: 'cancel', label: 'İptal Et', icon: Ban, variant: 'outline' },
      ];
    case 'ready':
      return [
        { action: 'receive', label: 'Klinikte Teslim Alındı', icon: PackageCheck },
        FAST_COMPLETE,
      ];
    case 'received':
      return [
        { action: 'fit', label: 'Hastaya Takıldı', icon: Smile },
        FAST_COMPLETE,
      ];
    case 'fitted':
      return [
        { action: 'complete', label: 'Tamamla (Onaya Gönder)', icon: Check },
        FAST_COMPLETE,
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

// ─── History timeline ──────────────────────────────────────────────────────

const HISTORY_ICON: Record<string, React.ElementType> = {
  sent:        Send,
  in_progress: Wrench,
  ready:       PackageCheck,
  received:    PackageCheck,
  fitted:      Smile,
  completed:   Check,
  approved:    Check,
  rejected:    X,
  cancelled:   Ban,
};

// ─── Main component ────────────────────────────────────────────────────────

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
                {work && <Badge variant="outline">{STATUS_LABEL[work.status]}</Badge>}
              </div>
            )}
          </SheetTitle>
        </SheetHeader>

        {isLoading ? (
          <div className="space-y-3 mt-4">
            {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
          </div>
        ) : !work ? (
          <p className="text-sm text-muted-foreground mt-4">Bulunamadı.</p>
        ) : (
          <div className="mt-4 space-y-5">

            {/* Workflow stepper */}
            <WorkflowStepper work={work} />

            {/* Actions */}
            {actions.length > 0 && (
              <div className="rounded-md border bg-muted/30 p-3 space-y-2">
                <div className="text-xs font-semibold text-muted-foreground">İŞLEMLER</div>
                <div className="flex flex-wrap gap-2">
                  {actions.map(a => (
                    <Button
                      key={a.action}
                      size="sm"
                      variant={a.variant ?? 'default'}
                      onClick={() => {
                        if (a.requireNotes || a.showDialog) {
                          setActionDialog(a);
                          setActionNotes('');
                        } else {
                          transitionMut.mutate({ action: a.action });
                        }
                      }}
                      disabled={transitionMut.isPending}
                      className={a.action === 'fast_complete' ? 'text-amber-600 border-amber-300 hover:bg-amber-50' : ''}
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
              <Info label="Hasta"       value={work.patientFullName} />
              <Info label="Hekim"       value={work.doctorFullName} />
              <Info label="Laboratuvar" value={work.laboratoryName} />
              <Info label="Şube"        value={work.branchName} />
              <Info label="İş Tipi"     value={WORK_TYPE_LABEL[work.workType] ?? work.workType} />
              <Info label="Teslim Tipi" value={DELIVERY_TYPE_LABEL[work.deliveryType] ?? work.deliveryType} />
              <Info label="Diş Numaraları" value={work.toothNumbers ?? '—'} />
              <Info label="Renk (Shade)"   value={work.shadeColor ?? '—'} />
              <Info label="Gönderim"
                value={work.sentToLabAt ? format(new Date(work.sentToLabAt), 'dd.MM.yyyy HH:mm') : '—'} />
              <Info label="Tahmini Teslim" value={work.estimatedDeliveryDate ?? '—'} />
              <Info label="Teslim Alınma"
                value={work.receivedFromLabAt ? format(new Date(work.receivedFromLabAt), 'dd.MM.yyyy HH:mm') : '—'} />
              <Info label="Takılma"
                value={work.fittedToPatientAt ? format(new Date(work.fittedToPatientAt), 'dd.MM.yyyy HH:mm') : '—'} />
              <Info label="Toplam"
                value={work.totalCost != null ? `${work.totalCost.toFixed(2)} ${work.currency}` : '—'} />
              <Info label="Oluşturulma"
                value={format(new Date(work.createdAt), 'dd.MM.yyyy HH:mm')} />
            </div>

            {/* Notes */}
            {(work.doctorNotes || work.labNotes || work.approvalNotes) && (
              <div className="space-y-2">
                {work.doctorNotes   && <Notes label="Hekim Notu"       text={work.doctorNotes} />}
                {work.labNotes      && <Notes label="Laboratuvar Notu" text={work.labNotes} />}
                {work.approvalNotes && <Notes label="Onay/Red Notu"    text={work.approvalNotes} />}
              </div>
            )}

            {/* Items */}
            <div>
              <div className="text-xs font-semibold text-muted-foreground mb-2">KALEMLER</div>
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
                        <TableCell colSpan={4} className="text-center py-4 text-muted-foreground text-sm">
                          Kalem yok
                        </TableCell>
                      </TableRow>
                    ) : (
                      work.items.map(i => (
                        <TableRow key={i.publicId}>
                          <TableCell>
                            <div className="font-medium text-sm">{i.itemName}</div>
                            {i.notes && <div className="text-xs text-muted-foreground">{i.notes}</div>}
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

            {/* History timeline */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <History className="h-4 w-4 text-muted-foreground" />
                <span className="text-xs font-semibold text-muted-foreground">DURUM HAREKETLERİ</span>
              </div>
              {work.history.length === 0 ? (
                <p className="text-xs text-muted-foreground italic">Kayıt yok.</p>
              ) : (
                <div className="relative pl-4">
                  <div className="absolute left-1.5 top-1 bottom-1 w-px bg-border" />
                  <div className="space-y-3">
                    {[...work.history].reverse().map((h, idx) => {
                      const IconComp = HISTORY_ICON[h.newStatus] ?? Clock;
                      return (
                        <div key={idx} className="relative">
                          <div className="absolute -left-[11px] top-1.5 size-4 rounded-full border-2 border-background bg-muted flex items-center justify-center">
                            <IconComp className="size-2 text-muted-foreground" />
                          </div>
                          <div className="rounded-md border bg-background p-2.5 ml-2 space-y-0.5">
                            <div className="flex items-center gap-2 flex-wrap">
                              {h.oldStatus && (
                                <>
                                  <Badge variant="outline" className="text-[10px] px-1 py-0">
                                    {STATUS_LABEL[h.oldStatus as LabWorkStatus] ?? h.oldStatus}
                                  </Badge>
                                  <span className="text-[10px] text-muted-foreground">→</span>
                                </>
                              )}
                              <Badge variant="secondary" className="text-[10px] px-1 py-0">
                                {STATUS_LABEL[h.newStatus as LabWorkStatus] ?? h.newStatus}
                              </Badge>
                              <span className="text-[10px] text-muted-foreground ml-auto">
                                {format(new Date(h.changedAt), 'd MMM yyyy HH:mm', { locale: tr })}
                              </span>
                            </div>
                            {h.notes && (
                              <p className="text-xs text-muted-foreground">{h.notes}</p>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </SheetContent>

      {/* Action confirmation / notes dialog */}
      <Dialog open={!!actionDialog} onOpenChange={v => !v && setActionDialog(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{actionDialog?.label}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2">
            {actionDialog?.action === 'fast_complete' && (
              <p className="text-sm text-muted-foreground">
                Ara adımlar atlanarak iş emri doğrudan <strong>Tamamlandı</strong> durumuna geçirilecek.
                Bu işlem yalnızca yetkili kullanıcılar tarafından yapılabilir.
              </p>
            )}
            <div className="space-y-1.5">
              <Label>Not {actionDialog?.requireNotes ? '*' : '(isteğe bağlı)'}</Label>
              <Textarea
                rows={3}
                value={actionNotes}
                onChange={e => setActionNotes(e.target.value)}
                placeholder="Açıklama girin..."
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setActionDialog(null)}>İptal</Button>
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
                (actionDialog?.requireNotes === true && !actionNotes.trim())
              }
            >
              {transitionMut.isPending ? 'İşleniyor...' : 'Onayla'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Sheet>
  );
}

// ─── Helpers ──────────────────────────────────────────────────────────────

const WORK_TYPE_LABEL: Record<string, string> = {
  prosthetic: 'Protetik', orthodontic: 'Ortodontik', implant: 'İmplant', other: 'Diğer',
};

const DELIVERY_TYPE_LABEL: Record<string, string> = {
  pickup: 'Elden', courier: 'Kargo/Kurye', digital: 'Dijital', conventional: 'Elden',
};

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
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground mb-1">{label}</div>
      <p className="text-sm whitespace-pre-wrap">{text}</p>
    </div>
  );
}
