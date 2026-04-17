import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, ShieldCheck, User } from 'lucide-react';
import { toast } from 'sonner';
import {
  laboratoriesApi,
  type UpsertApprovalAuthorityPayload,
} from '@/api/laboratories';
import { settingsApi } from '@/api/settings';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
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

export function ApprovalAuthoritiesTab() {
  const qc = useQueryClient();
  const [formOpen, setFormOpen] = useState(false);
  const [userPublicId, setUserPublicId] = useState('');
  const [branchPublicId, setBranchPublicId] = useState<string>('__all__');
  const [canApprove, setCanApprove] = useState(true);
  const [canReject, setCanReject] = useState(true);
  const [notify, setNotify] = useState(true);

  const { data: authorities, isLoading } = useQuery({
    queryKey: ['lab-approval-authorities'],
    queryFn: () => laboratoriesApi.listApprovalAuthorities().then(r => r.data),
  });

  const { data: users } = useQuery({
    queryKey: ['settings', 'users'],
    queryFn: () => settingsApi.listUsers().then(r => r.data),
    enabled: formOpen,
    staleTime: 5 * 60 * 1000,
  });

  const { data: branches } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
    enabled: formOpen,
    staleTime: 5 * 60 * 1000,
  });

  const upsertMut = useMutation({
    mutationFn: (payload: UpsertApprovalAuthorityPayload) =>
      laboratoriesApi.upsertApprovalAuthority(payload),
    onSuccess: () => {
      toast.success('Yetkili kaydedildi');
      qc.invalidateQueries({ queryKey: ['lab-approval-authorities'] });
      setFormOpen(false);
      setUserPublicId('');
      setBranchPublicId('__all__');
      setCanApprove(true);
      setCanReject(true);
      setNotify(true);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Yetkili kaydedilemedi');
    },
  });

  const removeMut = useMutation({
    mutationFn: (authorityPublicId: string) =>
      laboratoriesApi.removeApprovalAuthority(authorityPublicId),
    onSuccess: () => {
      toast.success('Yetkili kaldırıldı');
      qc.invalidateQueries({ queryKey: ['lab-approval-authorities'] });
    },
    onError: () => toast.error('Kaldırılamadı'),
  });

  const items = authorities ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Laboratuvar iş emirlerini onaylama/reddetme yetkisine sahip kullanıcılar.
        </p>
        <Button onClick={() => setFormOpen(true)}>
          <Plus className="mr-1 h-4 w-4" /> Yetkili Ekle
        </Button>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Kullanıcı</TableHead>
              <TableHead>Kapsam</TableHead>
              <TableHead>Yetkiler</TableHead>
              <TableHead>Bildirim</TableHead>
              <TableHead className="w-16 text-right"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={5}><Skeleton className="h-8 w-full" /></TableCell>
                </TableRow>
              ))
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-12 text-muted-foreground">
                  <ShieldCheck className="h-8 w-8 mx-auto mb-2 opacity-50" />
                  Henüz yetkili eklenmemiş.
                </TableCell>
              </TableRow>
            ) : (
              items.map(a => (
                <TableRow key={a.publicId}>
                  <TableCell>
                    <span className="flex items-center gap-2 font-medium">
                      <User className="h-4 w-4 text-muted-foreground" />
                      {a.userFullName}
                    </span>
                  </TableCell>
                  <TableCell>
                    {a.branchName
                      ? <Badge variant="outline">{a.branchName}</Badge>
                      : <Badge className="bg-blue-100 text-blue-800">Tüm şubeler</Badge>}
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {a.canApprove && <Badge className="bg-green-100 text-green-800 text-[10px]">Onay</Badge>}
                      {a.canReject && <Badge className="bg-red-100 text-red-800 text-[10px]">Ret</Badge>}
                    </div>
                  </TableCell>
                  <TableCell>
                    {a.notificationEnabled
                      ? <Badge variant="secondary" className="text-[10px]">Açık</Badge>
                      : <Badge variant="outline" className="text-[10px]">Kapalı</Badge>}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-destructive hover:text-destructive"
                      onClick={() => removeMut.mutate(a.publicId)}
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

      <Dialog open={formOpen} onOpenChange={(v) => !v && setFormOpen(false)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Yetkili Ekle / Güncelle</DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2">
            <div className="space-y-1.5">
              <Label>Kullanıcı *</Label>
              <Select value={userPublicId} onValueChange={setUserPublicId}>
                <SelectTrigger>
                  <SelectValue placeholder="Kullanıcı seç" />
                </SelectTrigger>
                <SelectContent>
                  {(users ?? []).map(u => (
                    <SelectItem key={u.publicId} value={u.publicId}>
                      {u.fullName}
                      {u.title && <span className="text-muted-foreground ml-1">({u.title})</span>}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Kapsam</Label>
              <Select value={branchPublicId} onValueChange={setBranchPublicId}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="__all__">Tüm şubeler (global)</SelectItem>
                  {(branches ?? []).map(b => (
                    <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-2 pt-2">
              <Checkbox
                checked={canApprove}
                onCheckedChange={v => setCanApprove(!!v)}
                id="canApprove"
              />
              <Label htmlFor="canApprove">Onaylayabilir</Label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                checked={canReject}
                onCheckedChange={v => setCanReject(!!v)}
                id="canReject"
              />
              <Label htmlFor="canReject">Reddedebilir</Label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                checked={notify}
                onCheckedChange={v => setNotify(!!v)}
                id="notify"
              />
              <Label htmlFor="notify">Bildirim açık</Label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setFormOpen(false)}>İptal</Button>
            <Button
              onClick={() =>
                upsertMut.mutate({
                  userPublicId,
                  branchPublicId: branchPublicId === '__all__' ? null : branchPublicId,
                  canApprove,
                  canReject,
                  notificationEnabled: notify,
                })
              }
              disabled={!userPublicId || upsertMut.isPending}
            >
              {upsertMut.isPending ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
