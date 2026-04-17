import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, FlaskConical, Building2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type LaboratoryItem,
  type CreateLaboratoryPayload,
} from '@/api/laboratories';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { LaboratoryFormDialog } from './LaboratoryFormDialog';
import { LaboratoryDetailSheet } from './LaboratoryDetailSheet';

export function LaboratoriesTab() {
  const qc = useQueryClient();
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<LaboratoryItem | null>(null);
  const [detailPublicId, setDetailPublicId] = useState<string | null>(null);
  const [toDelete, setToDelete] = useState<LaboratoryItem | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['laboratories'],
    queryFn: () => laboratoriesApi.list().then(r => r.data),
  });

  const createMut = useMutation({
    mutationFn: (payload: CreateLaboratoryPayload) =>
      laboratoriesApi.create(payload),
    onSuccess: () => {
      toast.success('Laboratuvar oluşturuldu');
      qc.invalidateQueries({ queryKey: ['laboratories'] });
      setFormOpen(false);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Laboratuvar oluşturulamadı');
    },
  });

  const updateMut = useMutation({
    mutationFn: ({ publicId, payload }: { publicId: string; payload: CreateLaboratoryPayload & { isActive: boolean } }) =>
      laboratoriesApi.update(publicId, payload),
    onSuccess: () => {
      toast.success('Laboratuvar güncellendi');
      qc.invalidateQueries({ queryKey: ['laboratories'] });
      setFormOpen(false);
      setEditing(null);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Laboratuvar güncellenemedi');
    },
  });

  const deleteMut = useMutation({
    mutationFn: (publicId: string) => laboratoriesApi.delete(publicId),
    onSuccess: () => {
      toast.success('Laboratuvar silindi');
      qc.invalidateQueries({ queryKey: ['laboratories'] });
      setToDelete(null);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Laboratuvar silinemedi');
      setToDelete(null);
    },
  });

  const items = data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Toplam {items.length} laboratuvar
        </p>
        <Button onClick={() => { setEditing(null); setFormOpen(true); }}>
          <Plus className="mr-1 h-4 w-4" /> Yeni Laboratuvar
        </Button>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Ad</TableHead>
              <TableHead>Kod</TableHead>
              <TableHead>Şehir</TableHead>
              <TableHead>Telefon</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="w-28 text-right">İşlem</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={6}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ))
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-12 text-muted-foreground">
                  <FlaskConical className="h-8 w-8 mx-auto mb-2 opacity-50" />
                  Henüz laboratuvar eklenmemiş.
                </TableCell>
              </TableRow>
            ) : (
              items.map(lab => (
                <TableRow
                  key={lab.publicId}
                  className="cursor-pointer"
                  onClick={() => setDetailPublicId(lab.publicId)}
                >
                  <TableCell className="font-medium">
                    <span className="flex items-center gap-2">
                      <Building2 className="h-4 w-4 text-muted-foreground" />
                      {lab.name}
                    </span>
                  </TableCell>
                  <TableCell className="font-mono text-xs">{lab.code ?? '—'}</TableCell>
                  <TableCell>{lab.city ?? '—'}</TableCell>
                  <TableCell>{lab.phone ?? '—'}</TableCell>
                  <TableCell>
                    {lab.isActive
                      ? <Badge className="bg-green-100 text-green-800">Aktif</Badge>
                      : <Badge variant="secondary">Pasif</Badge>}
                  </TableCell>
                  <TableCell
                    className="text-right"
                    onClick={e => e.stopPropagation()}
                  >
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => { setEditing(lab); setFormOpen(true); }}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-destructive hover:text-destructive"
                      onClick={() => setToDelete(lab)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <LaboratoryFormDialog
        open={formOpen}
        onClose={() => { setFormOpen(false); setEditing(null); }}
        editing={editing}
        onSubmit={(payload) => {
          if (editing) {
            updateMut.mutate({
              publicId: editing.publicId,
              payload: { ...payload, isActive: true },
            });
          } else {
            createMut.mutate(payload);
          }
        }}
        isPending={createMut.isPending || updateMut.isPending}
      />

      {detailPublicId && (
        <LaboratoryDetailSheet
          publicId={detailPublicId}
          open={!!detailPublicId}
          onClose={() => setDetailPublicId(null)}
        />
      )}

      <AlertDialog open={!!toDelete} onOpenChange={(v) => !v && setToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Laboratuvar silinsin mi?</AlertDialogTitle>
            <AlertDialogDescription>
              <strong>{toDelete?.name}</strong> silinecek. Bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              onClick={() => toDelete && deleteMut.mutate(toDelete.publicId)}
              disabled={deleteMut.isPending}
            >
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
