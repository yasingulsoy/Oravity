import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Building2, GitBranch, Users, Shield, KeyRound, Plus, Pencil, Check, X,
  Loader2, UserPlus, Eye, EyeOff, ChevronDown, ChevronRight, AlertTriangle,
  Trash2, ArrowLeft, MapPin, Calendar, User as UserIcon, Info,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  settingsApi,
  type CompanyInfo, type BranchItem, type BranchDetail, type BranchUserInfo,
  type UserItem, type UserDetail,
  type RoleItem, type PermissionItem, type SecurityPolicy,
  type CreateBranchPayload, type UpdateBranchPayload,
  type CreateUserPayload, type UpdateUserPayload,
} from '@/api/settings';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import {
  Card, CardContent, CardHeader, CardTitle, CardDescription,
} from '@/components/ui/card';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';

// ═══════════════════════════════════════════════════════════════════════════════
// ŞİRKET SEKMESI
// ═══════════════════════════════════════════════════════════════════════════════

function CompanyTab() {
  const { data: company, isLoading } = useQuery({
    queryKey: ['settings', 'company'],
    queryFn: () => settingsApi.getCompany().then(r => r.data),
  });

  const qc = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState('');
  const [lang, setLang] = useState('');

  useEffect(() => {
    if (company) { setName(company.name); setLang(company.defaultLanguageCode); }
  }, [company]);

  const mutation = useMutation({
    mutationFn: () => settingsApi.updateCompany({ name: name.trim(), defaultLanguageCode: lang }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['settings', 'company'] }); setEditing(false); toast.success('Şirket bilgileri güncellendi'); },
    onError: () => toast.error('Güncelleme başarısız'),
  });

  if (isLoading) return <LoadingSkeleton rows={4} />;
  if (!company) return <p className="text-muted-foreground py-8 text-center">Şirket bilgisi bulunamadı.</p>;

  return (
    <div className="max-w-2xl space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Building2 className="h-5 w-5" /> Şirket Bilgileri
          </CardTitle>
          <CardDescription>Kliniğinizin temel bilgilerini yönetin.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Şirket Adı">
              {editing ? (
                <Input value={name} onChange={e => setName(e.target.value)} />
              ) : (
                <p className="text-sm font-medium">{company.name}</p>
              )}
            </Field>
            <Field label="Varsayılan Dil">
              {editing ? (
                <Select value={lang} onValueChange={setLang}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="tr">Türkçe</SelectItem>
                    <SelectItem value="en">English</SelectItem>
                    <SelectItem value="de">Deutsch</SelectItem>
                    <SelectItem value="ar">العربية</SelectItem>
                  </SelectContent>
                </Select>
              ) : (
                <p className="text-sm font-medium">{langLabel(company.defaultLanguageCode)}</p>
              )}
            </Field>
            <Field label="Sektör">
              <p className="text-sm font-medium">{company.verticalName}</p>
            </Field>
            <Field label="Durum">
              <Badge variant={company.isActive ? 'default' : 'secondary'}>
                {company.isActive ? 'Aktif' : 'Pasif'}
              </Badge>
            </Field>
            {company.subscriptionEndsAt && (
              <Field label="Abonelik Bitiş">
                <p className="text-sm font-medium">
                  {new Date(company.subscriptionEndsAt).toLocaleDateString('tr-TR')}
                </p>
              </Field>
            )}
          </div>
          <div className="flex gap-2 pt-2">
            {editing ? (
              <>
                <Button size="sm" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                  {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : <Check className="h-4 w-4 mr-1" />}
                  Kaydet
                </Button>
                <Button size="sm" variant="ghost" onClick={() => setEditing(false)}>
                  <X className="h-4 w-4 mr-1" /> İptal
                </Button>
              </>
            ) : (
              <Button size="sm" variant="outline" onClick={() => setEditing(true)}>
                <Pencil className="h-4 w-4 mr-1" /> Düzenle
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// ŞUBELER SEKMESI
// ═══════════════════════════════════════════════════════════════════════════════

function BranchesTab() {
  const qc = useQueryClient();
  const { data: branches, isLoading } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
  });

  const [showCreate, setShowCreate] = useState(false);
  const [editBranch, setEditBranch] = useState<BranchItem | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<BranchItem | null>(null);

  const deleteMut = useMutation({
    mutationFn: (publicId: string) => settingsApi.deleteBranch(publicId),
    onSuccess: () => {
      toast.success('Şube silindi');
      qc.invalidateQueries({ queryKey: ['settings', 'branches'] });
      setDeleteTarget(null);
      if (detailId === deleteTarget?.publicId) setDetailId(null);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: string } })?.response?.data;
      toast.error(typeof msg === 'string' ? msg : 'Şube silinemedi');
    },
  });

  const invalidate = useCallback(() => {
    qc.invalidateQueries({ queryKey: ['settings', 'branches'] });
    if (detailId) qc.invalidateQueries({ queryKey: ['settings', 'branch', detailId] });
  }, [qc, detailId]);

  if (isLoading) return <LoadingSkeleton rows={5} />;

  if (detailId) {
    return (
      <BranchDetailPanel
        publicId={detailId}
        onBack={() => setDetailId(null)}
        onEdit={(b) => { setEditBranch(b); }}
        onDelete={(b) => setDeleteTarget(b)}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-semibold">Şubeler</h3>
          <p className="text-sm text-muted-foreground">Şirketinizdeki tüm şubeleri yönetin.</p>
        </div>
        <Button size="sm" onClick={() => setShowCreate(true)}>
          <Plus className="h-4 w-4 mr-1" /> Yeni Şube
        </Button>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Şube Adı</TableHead>
              <TableHead>Dil</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="text-right">Fiyat Çarpanı</TableHead>
              <TableHead className="text-right">Kullanıcı</TableHead>
              <TableHead className="text-right">Oluşturulma</TableHead>
              <TableHead className="w-28" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {branches?.map(b => (
              <TableRow
                key={b.publicId}
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => setDetailId(b.publicId)}
              >
                <TableCell className="font-medium">{b.name}</TableCell>
                <TableCell>{langLabel(b.defaultLanguageCode)}</TableCell>
                <TableCell>
                  <Badge variant={b.isActive ? 'default' : 'secondary'} className="text-xs">
                    {b.isActive ? 'Aktif' : 'Pasif'}
                  </Badge>
                </TableCell>
                <TableCell className="text-right font-mono">{b.pricingMultiplier.toFixed(2)}</TableCell>
                <TableCell className="text-right">{b.activeUserCount}</TableCell>
                <TableCell className="text-right text-sm text-muted-foreground">
                  {new Date(b.createdAt).toLocaleDateString('tr-TR')}
                </TableCell>
                <TableCell>
                  <div className="flex gap-1 justify-end" onClick={e => e.stopPropagation()}>
                    <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => setEditBranch(b)}>
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                    <Button
                      size="icon" variant="ghost"
                      className="h-8 w-8 text-destructive hover:text-destructive"
                      onClick={() => setDeleteTarget(b)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {branches?.length === 0 && (
              <TableRow><TableCell colSpan={7} className="text-center text-muted-foreground py-8">Henüz şube eklenmemiş.</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <BranchDialog
        open={showCreate}
        onClose={() => setShowCreate(false)}
        onSuccess={() => { invalidate(); setShowCreate(false); }}
      />
      {editBranch && (
        <BranchDialog
          open
          branch={editBranch}
          onClose={() => setEditBranch(null)}
          onSuccess={() => { invalidate(); setEditBranch(null); }}
        />
      )}

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        title="Şubeyi Sil"
        description={`"${deleteTarget?.name}" şubesini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`}
        loading={deleteMut.isPending}
        onConfirm={() => deleteTarget && deleteMut.mutate(deleteTarget.publicId)}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}

function BranchDetailPanel({ publicId, onBack, onEdit, onDelete }: {
  publicId: string;
  onBack: () => void;
  onEdit: (b: BranchItem) => void;
  onDelete: (b: BranchItem) => void;
}) {
  const { data: branch, isLoading } = useQuery({
    queryKey: ['settings', 'branch', publicId],
    queryFn: () => settingsApi.getBranch(publicId).then(r => r.data),
  });

  if (isLoading || !branch) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" size="sm" onClick={onBack}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Geri
        </Button>
        <LoadingSkeleton rows={6} />
      </div>
    );
  }

  const asBranchItem: BranchItem = {
    publicId: branch.publicId,
    name: branch.name,
    defaultLanguageCode: branch.defaultLanguageCode,
    isActive: branch.isActive,
    pricingMultiplier: branch.pricingMultiplier,
    activeUserCount: branch.activeUserCount,
    createdAt: branch.createdAt,
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onBack}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h3 className="text-lg font-semibold">{branch.name}</h3>
            <p className="text-sm text-muted-foreground">Şube Detayları</p>
          </div>
          <Badge variant={branch.isActive ? 'default' : 'secondary'}>
            {branch.isActive ? 'Aktif' : 'Pasif'}
          </Badge>
        </div>
        <div className="flex gap-2">
          <Button size="sm" variant="outline" onClick={() => onEdit(asBranchItem)}>
            <Pencil className="h-4 w-4 mr-1" /> Düzenle
          </Button>
          <Button size="sm" variant="destructive" onClick={() => onDelete(asBranchItem)}>
            <Trash2 className="h-4 w-4 mr-1" /> Sil
          </Button>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Info className="h-4 w-4" /> Genel Bilgiler
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Şube Adı">
                <p className="text-sm font-medium">{branch.name}</p>
              </Field>
              <Field label="Durum">
                <Badge variant={branch.isActive ? 'default' : 'secondary'}>
                  {branch.isActive ? 'Aktif' : 'Pasif'}
                </Badge>
              </Field>
              <Field label="Varsayılan Dil">
                <p className="text-sm font-medium">{langLabel(branch.defaultLanguageCode)}</p>
              </Field>
              <Field label="Fiyat Çarpanı (MULTI)">
                <p className="text-sm font-medium font-mono">{branch.pricingMultiplier.toFixed(2)}</p>
              </Field>
              <Field label="Sektör (Vertical)">
                <p className="text-sm font-medium">{branch.verticalName ?? 'Şirket varsayılanı'}</p>
              </Field>
              <Field label="Aktif Kullanıcı Sayısı">
                <p className="text-sm font-medium">{branch.activeUserCount}</p>
              </Field>
              <Field label="Oluşturulma Tarihi">
                <p className="text-sm font-medium">
                  {new Date(branch.createdAt).toLocaleString('tr-TR')}
                </p>
              </Field>
              <Field label="Son Güncelleme">
                <p className="text-sm font-medium">
                  {branch.updatedAt ? new Date(branch.updatedAt).toLocaleString('tr-TR') : '—'}
                </p>
              </Field>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <MapPin className="h-4 w-4" /> Özet
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Toplam Kullanıcı</span>
              <span className="font-semibold">{branch.activeUserCount}</span>
            </div>
            <Separator />
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Fiyat Çarpanı</span>
              <span className="font-mono font-semibold">{branch.pricingMultiplier.toFixed(2)}x</span>
            </div>
            <Separator />
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Dil</span>
              <span className="font-semibold">{langLabel(branch.defaultLanguageCode)}</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Users className="h-4 w-4" /> Şube Kullanıcıları ({branch.users.length})
          </CardTitle>
          <CardDescription>Bu şubeye atanmış tüm kullanıcılar.</CardDescription>
        </CardHeader>
        <CardContent>
          {branch.users.length === 0 ? (
            <p className="text-sm text-muted-foreground py-6 text-center">Bu şubede henüz kullanıcı yok.</p>
          ) : (
            <div className="rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Ad Soyad</TableHead>
                    <TableHead>E-posta</TableHead>
                    <TableHead>Rol</TableHead>
                    <TableHead>Durum</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {branch.users.map(u => (
                    <TableRow key={`${u.publicId}-${u.roleCode}`}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <div className="h-7 w-7 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-medium text-primary">
                            {u.fullName.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()}
                          </div>
                          <span className="font-medium text-sm">
                            {u.title ? `${u.title} ${u.fullName}` : u.fullName}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">{u.email}</TableCell>
                      <TableCell>
                        <Badge variant="outline" className="text-xs">{u.roleName}</Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={u.isActive ? 'default' : 'secondary'} className="text-xs">
                          {u.isActive ? 'Aktif' : 'Pasif'}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function BranchDialog({ open, branch, onClose, onSuccess }: {
  open: boolean; branch?: BranchItem; onClose: () => void; onSuccess: () => void;
}) {
  const isEdit = !!branch;
  const [name, setName] = useState(branch?.name ?? '');
  const [lang, setLang] = useState(branch?.defaultLanguageCode ?? 'tr');
  const [active, setActive] = useState(branch?.isActive ?? true);
  const [mult, setMult] = useState(String(branch?.pricingMultiplier ?? '1.00'));

  useEffect(() => {
    if (open) {
      setName(branch?.name ?? '');
      setLang(branch?.defaultLanguageCode ?? 'tr');
      setActive(branch?.isActive ?? true);
      setMult(String(branch?.pricingMultiplier ?? '1.00'));
    }
  }, [open, branch]);

  const mutation = useMutation({
    mutationFn: () => {
      if (isEdit) {
        const data: UpdateBranchPayload = { name: name.trim(), defaultLanguageCode: lang, isActive: active, pricingMultiplier: parseFloat(mult) || 1 };
        return settingsApi.updateBranch(branch!.publicId, data);
      }
      const data: CreateBranchPayload = { name: name.trim(), defaultLanguageCode: lang };
      return settingsApi.createBranch(data);
    },
    onSuccess: () => { toast.success(isEdit ? 'Şube güncellendi' : 'Şube oluşturuldu'); onSuccess(); },
    onError: () => toast.error('İşlem başarısız'),
  });

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Şube Düzenle' : 'Yeni Şube'}</DialogTitle>
          <DialogDescription>
            {isEdit ? 'Şube bilgilerini güncelleyin.' : 'Yeni bir şube oluşturun.'}
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label>Şube Adı *</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="ör. Bodrum Şubesi" />
          </div>
          <div className="space-y-1.5">
            <Label>Varsayılan Dil</Label>
            <Select value={lang} onValueChange={setLang}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="tr">Türkçe</SelectItem>
                <SelectItem value="en">English</SelectItem>
                <SelectItem value="de">Deutsch</SelectItem>
                <SelectItem value="ar">العربية</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <Label>Fiyat Çarpanı (MULTI)</Label>
            <Input type="number" step="0.01" min="0.01" value={mult} onChange={e => setMult(e.target.value)} />
            <p className="text-xs text-muted-foreground">Formüllerde MULTI değişkeni olarak kullanılır. (Varsayılan: 1.00)</p>
          </div>
          {isEdit && (
            <div className="flex items-center gap-2">
              <Checkbox checked={active} onCheckedChange={v => setActive(!!v)} />
              <Label>Aktif</Label>
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="ghost" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !name.trim()}>
            {mutation.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
            {isEdit ? 'Güncelle' : 'Oluştur'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// KULLANICILAR SEKMESI
// ═══════════════════════════════════════════════════════════════════════════════

function UsersTab() {
  const qc = useQueryClient();
  const { data: users, isLoading } = useQuery({
    queryKey: ['settings', 'users'],
    queryFn: () => settingsApi.listUsers().then(r => r.data),
  });
  const { data: branches } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
  });
  const { data: roles } = useQuery({
    queryKey: ['settings', 'roles'],
    queryFn: () => settingsApi.listRoles().then(r => r.data),
  });

  const [showCreate, setShowCreate] = useState(false);
  const [editUser, setEditUser] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<UserItem | null>(null);

  const deleteMut = useMutation({
    mutationFn: (publicId: string) => settingsApi.deleteUser(publicId),
    onSuccess: () => {
      toast.success('Kullanıcı silindi');
      qc.invalidateQueries({ queryKey: ['settings', 'users'] });
      setDeleteTarget(null);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: string } })?.response?.data;
      toast.error(typeof msg === 'string' ? msg : 'Kullanıcı silinemedi');
    },
  });

  if (isLoading) return <LoadingSkeleton rows={6} />;

  const filtered = users?.filter(u => {
    const q = search.toLowerCase();
    return !q || u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q);
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div className="flex-1 max-w-sm">
          <Input placeholder="Kullanıcı ara..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <Button size="sm" onClick={() => setShowCreate(true)}>
          <UserPlus className="h-4 w-4 mr-1" /> Yeni Kullanıcı
        </Button>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Ad Soyad</TableHead>
              <TableHead>E-posta</TableHead>
              <TableHead>Roller</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead>Son Giriş</TableHead>
              <TableHead className="w-28" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered?.map(u => (
              <TableRow key={u.publicId}>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-medium text-primary">
                      {u.fullName.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <p className="font-medium leading-tight">{u.title ? `${u.title} ${u.fullName}` : u.fullName}</p>
                      {u.isPlatformAdmin && <Badge variant="destructive" className="text-[10px] mt-0.5">Platform Admin</Badge>}
                    </div>
                  </div>
                </TableCell>
                <TableCell className="text-muted-foreground">{u.email}</TableCell>
                <TableCell>
                  <div className="flex flex-wrap gap-1">
                    {u.roles.map((r, i) => (
                      <Badge key={i} variant="outline" className="text-[10px]">
                        {r.roleName}{r.branchName ? ` — ${r.branchName}` : ''}
                      </Badge>
                    ))}
                    {u.roles.length === 0 && <span className="text-xs text-muted-foreground">—</span>}
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={u.isActive ? 'default' : 'secondary'} className="text-xs">
                    {u.isActive ? 'Aktif' : 'Pasif'}
                  </Badge>
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString('tr-TR') : '—'}
                </TableCell>
                <TableCell>
                  <div className="flex gap-1 justify-end">
                    <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => setEditUser(u.publicId)}>
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                    {!u.isPlatformAdmin && (
                      <Button
                        size="icon" variant="ghost"
                        className="h-8 w-8 text-destructive hover:text-destructive"
                        onClick={() => setDeleteTarget(u)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {filtered?.length === 0 && (
              <TableRow><TableCell colSpan={6} className="text-center text-muted-foreground py-8">Kullanıcı bulunamadı.</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <CreateUserDialog
        open={showCreate}
        onClose={() => setShowCreate(false)}
        branches={branches ?? []}
        roles={roles ?? []}
        onSuccess={() => { qc.invalidateQueries({ queryKey: ['settings', 'users'] }); setShowCreate(false); }}
      />

      {editUser && (
        <EditUserDialog
          publicId={editUser}
          onClose={() => setEditUser(null)}
          branches={branches ?? []}
          roles={roles ?? []}
          onSuccess={() => { qc.invalidateQueries({ queryKey: ['settings', 'users'] }); setEditUser(null); }}
        />
      )}

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        title="Kullanıcıyı Sil"
        description={`"${deleteTarget?.fullName}" kullanıcısını silmek istediğinize emin misiniz? Kullanıcı pasif hale getirilecek ve tüm rol atamaları kaldırılacaktır.`}
        loading={deleteMut.isPending}
        onConfirm={() => deleteTarget && deleteMut.mutate(deleteTarget.publicId)}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}

function CreateUserDialog({ open, onClose, branches, roles, onSuccess }: {
  open: boolean; onClose: () => void; branches: BranchItem[]; roles: RoleItem[]; onSuccess: () => void;
}) {
  const [form, setForm] = useState<CreateUserPayload>({ email: '', fullName: '', password: '' });
  const [showPw, setShowPw] = useState(false);

  useEffect(() => {
    if (open) { setForm({ email: '', fullName: '', password: '' }); setShowPw(false); }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => settingsApi.createUser(form),
    onSuccess: () => { toast.success('Kullanıcı oluşturuldu'); onSuccess(); },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      toast.error(msg ?? 'Kullanıcı oluşturulamadı');
    },
  });

  const set = (patch: Partial<CreateUserPayload>) => setForm(prev => ({ ...prev, ...patch }));

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader><DialogTitle>Yeni Kullanıcı</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Ad Soyad *</Label>
              <Input value={form.fullName} onChange={e => set({ fullName: e.target.value })} />
            </div>
            <div className="space-y-1.5">
              <Label>E-posta *</Label>
              <Input type="email" value={form.email} onChange={e => set({ email: e.target.value })} />
            </div>
            <div className="space-y-1.5">
              <Label>Şifre *</Label>
              <div className="relative">
                <Input type={showPw ? 'text' : 'password'} value={form.password} onChange={e => set({ password: e.target.value })} />
                <button type="button" className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground" onClick={() => setShowPw(!showPw)}>
                  {showPw ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Ünvan</Label>
              <Input value={form.title ?? ''} onChange={e => set({ title: e.target.value || undefined })} placeholder="Dr., Dt., vb." />
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Rol</Label>
              <Select value={form.roleCode ?? ''} onValueChange={v => set({ roleCode: v || undefined })}>
                <SelectTrigger><SelectValue placeholder="Rol seçin" /></SelectTrigger>
                <SelectContent>
                  {roles.map(r => <SelectItem key={r.code} value={r.code}>{r.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Şube</Label>
              <Select value={form.branchPublicId ?? ''} onValueChange={v => set({ branchPublicId: v || undefined })}>
                <SelectTrigger><SelectValue placeholder="Tüm şubeler" /></SelectTrigger>
                <SelectContent>
                  {branches.filter(b => b.isActive).map(b => <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Takvim Rengi</Label>
              <Input type="color" value={form.calendarColor ?? '#4c4cff'} onChange={e => set({ calendarColor: e.target.value })} className="h-9 w-20 p-1" />
            </div>
            <div className="space-y-1.5">
              <Label>Varsayılan Randevu Süresi (dk)</Label>
              <Input type="number" min={5} step={5} value={form.defaultAppointmentDuration ?? ''} onChange={e => set({ defaultAppointmentDuration: parseInt(e.target.value) || undefined })} placeholder="30" />
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="ghost" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !form.email || !form.fullName || !form.password}>
            {mutation.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
            Oluştur
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EditUserDialog({ publicId, onClose, branches, roles, onSuccess }: {
  publicId: string; onClose: () => void; branches: BranchItem[]; roles: RoleItem[]; onSuccess: () => void;
}) {
  const qc = useQueryClient();
  const { data: user, isLoading } = useQuery({
    queryKey: ['settings', 'users', publicId],
    queryFn: () => settingsApi.getUser(publicId).then(r => r.data),
  });

  const [form, setForm] = useState<UpdateUserPayload>({});
  const [roleCode, setRoleCode] = useState('');
  const [branchId, setBranchId] = useState('');

  useEffect(() => {
    if (user) setForm({ fullName: user.fullName, isActive: user.isActive, title: user.title ?? undefined, calendarColor: user.calendarColor ?? undefined, defaultAppointmentDuration: user.defaultAppointmentDuration ?? undefined });
  }, [user]);

  const updateMut = useMutation({
    mutationFn: () => settingsApi.updateUser(publicId, form),
    onSuccess: () => { toast.success('Kullanıcı güncellendi'); onSuccess(); },
    onError: () => toast.error('Güncelleme başarısız'),
  });

  const assignMut = useMutation({
    mutationFn: () => settingsApi.assignRole(publicId, { roleCode, branchPublicId: branchId || undefined }),
    onSuccess: () => { toast.success('Rol atandı'); qc.invalidateQueries({ queryKey: ['settings', 'users', publicId] }); setRoleCode(''); setBranchId(''); },
    onError: () => toast.error('Rol atanamadı'),
  });

  const revokeMut = useMutation({
    mutationFn: (assignmentPublicId: string) => settingsApi.revokeRole(publicId, assignmentPublicId),
    onSuccess: () => { toast.success('Rol kaldırıldı'); qc.invalidateQueries({ queryKey: ['settings', 'users', publicId] }); },
  });

  const set = (patch: Partial<UpdateUserPayload>) => setForm(prev => ({ ...prev, ...patch }));

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
        <DialogHeader><DialogTitle>Kullanıcı Düzenle</DialogTitle></DialogHeader>
        {isLoading || !user ? <LoadingSkeleton rows={4} /> : (
          <div className="space-y-6 py-2">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Ad Soyad</Label>
                <Input value={form.fullName ?? ''} onChange={e => set({ fullName: e.target.value })} />
              </div>
              <div className="space-y-1.5">
                <Label>E-posta</Label>
                <Input value={user.email} disabled className="opacity-60" />
              </div>
              <div className="space-y-1.5">
                <Label>Ünvan</Label>
                <Input value={form.title ?? ''} onChange={e => set({ title: e.target.value || undefined })} placeholder="Dr., Dt." />
              </div>
              <div className="space-y-1.5">
                <Label>Takvim Rengi</Label>
                <Input type="color" value={form.calendarColor ?? '#4c4cff'} onChange={e => set({ calendarColor: e.target.value })} className="h-9 w-20 p-1" />
              </div>
              <div className="flex items-center gap-2 pt-4">
                <Checkbox checked={form.isActive ?? true} onCheckedChange={v => set({ isActive: !!v })} />
                <Label>Aktif</Label>
              </div>
            </div>

            <div className="space-y-3">
              <h4 className="text-sm font-semibold">Atanmış Roller</h4>
              <div className="rounded-lg border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Rol</TableHead>
                      <TableHead>Şube</TableHead>
                      <TableHead>Atanma</TableHead>
                      <TableHead className="w-16" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {user.roleAssignments.filter(a => a.isActive).map(a => (
                      <TableRow key={a.publicId}>
                        <TableCell><Badge variant="outline">{a.roleName}</Badge></TableCell>
                        <TableCell>{a.branchName ?? 'Tüm Şubeler'}</TableCell>
                        <TableCell className="text-sm text-muted-foreground">{new Date(a.assignedAt).toLocaleDateString('tr-TR')}</TableCell>
                        <TableCell>
                          <Button size="sm" variant="ghost" className="text-destructive" onClick={() => revokeMut.mutate(a.publicId)}>
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              <div className="flex gap-2 items-end">
                <div className="space-y-1.5 flex-1">
                  <Label className="text-xs">Rol</Label>
                  <Select value={roleCode} onValueChange={setRoleCode}>
                    <SelectTrigger><SelectValue placeholder="Rol seçin" /></SelectTrigger>
                    <SelectContent>
                      {roles.map(r => <SelectItem key={r.code} value={r.code}>{r.name}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5 flex-1">
                  <Label className="text-xs">Şube</Label>
                  <Select value={branchId} onValueChange={setBranchId}>
                    <SelectTrigger><SelectValue placeholder="Tüm şubeler" /></SelectTrigger>
                    <SelectContent>
                      {branches.filter(b => b.isActive).map(b => <SelectItem key={b.publicId} value={b.publicId}>{b.name}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <Button size="sm" onClick={() => assignMut.mutate()} disabled={!roleCode || assignMut.isPending}>
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        )}
        <DialogFooter>
          <Button variant="ghost" onClick={onClose}>Kapat</Button>
          <Button onClick={() => updateMut.mutate()} disabled={updateMut.isPending}>
            {updateMut.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
            Kaydet
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// ROLLER & İZİNLER SEKMESI
// ═══════════════════════════════════════════════════════════════════════════════

function RolesTab() {
  const { data: roles, isLoading } = useQuery({
    queryKey: ['settings', 'roles'],
    queryFn: () => settingsApi.listRoles().then(r => r.data),
  });
  const { data: permissions } = useQuery({
    queryKey: ['settings', 'permissions'],
    queryFn: () => settingsApi.listPermissions().then(r => r.data),
  });

  const [expanded, setExpanded] = useState<string | null>(null);

  if (isLoading) return <LoadingSkeleton rows={6} />;

  const permByResource = permissions?.reduce<Record<string, PermissionItem[]>>((acc, p) => {
    (acc[p.resource] ??= []).push(p);
    return acc;
  }, {}) ?? {};

  return (
    <div className="space-y-4">
      <div>
        <h3 className="font-semibold">Roller ve İzinler</h3>
        <p className="text-sm text-muted-foreground">Sistemde tanımlı rol şablonları ve her rolün sahip olduğu izinler.</p>
      </div>

      <div className="space-y-3">
        {roles?.map(role => {
          const isOpen = expanded === role.code;
          return (
            <Card key={role.code}>
              <button
                className="w-full text-left px-4 py-3 flex items-center justify-between hover:bg-muted/50 rounded-xl transition-colors"
                onClick={() => setExpanded(isOpen ? null : role.code)}
              >
                <div className="flex items-center gap-3">
                  <KeyRound className="h-4 w-4 text-primary" />
                  <div>
                    <p className="font-medium text-sm">{role.name} <span className="text-muted-foreground font-normal">({role.code})</span></p>
                    {role.description && <p className="text-xs text-muted-foreground">{role.description}</p>}
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <Badge variant="secondary" className="text-xs">{role.permissions.length} izin</Badge>
                  <Badge variant="outline" className="text-xs">{role.activeUserCount} kullanıcı</Badge>
                  {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                </div>
              </button>
              {isOpen && (
                <CardContent className="border-t pt-4">
                  <div className="space-y-3">
                    {Object.entries(permByResource).map(([resource, perms]) => {
                      const active = perms.filter(p => role.permissions.includes(p.code));
                      if (active.length === 0) return null;
                      return (
                        <div key={resource}>
                          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1">{resource}</p>
                          <div className="flex flex-wrap gap-1">
                            {perms.map(p => {
                              const has = role.permissions.includes(p.code);
                              return (
                                <Badge
                                  key={p.code}
                                  variant={has ? 'default' : 'outline'}
                                  className={cn('text-[10px]', !has && 'opacity-30', p.isDangerous && has && 'bg-destructive text-destructive-foreground')}
                                >
                                  {p.isDangerous && <AlertTriangle className="h-2.5 w-2.5 mr-0.5" />}
                                  {p.action}
                                </Badge>
                              );
                            })}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </CardContent>
              )}
            </Card>
          );
        })}
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// GÜVENLİK SEKMESI
// ═══════════════════════════════════════════════════════════════════════════════

function SecurityTab() {
  const { data: branches, isLoading } = useQuery({
    queryKey: ['settings', 'branches'],
    queryFn: () => settingsApi.listBranches().then(r => r.data),
  });

  const [selected, setSelected] = useState<string | null>(null);

  useEffect(() => {
    if (branches?.length && !selected) setSelected(branches[0].publicId);
  }, [branches, selected]);

  if (isLoading) return <LoadingSkeleton rows={4} />;

  return (
    <div className="space-y-4">
      <div>
        <h3 className="font-semibold">Güvenlik Politikaları</h3>
        <p className="text-sm text-muted-foreground">Her şube için güvenlik kurallarını yapılandırın.</p>
      </div>

      <div className="flex gap-2 flex-wrap">
        {branches?.filter(b => b.isActive).map(b => (
          <Button
            key={b.publicId}
            size="sm"
            variant={selected === b.publicId ? 'default' : 'outline'}
            onClick={() => setSelected(b.publicId)}
          >
            {b.name}
          </Button>
        ))}
      </div>

      {selected && <SecurityPolicyEditor branchPublicId={selected} />}
    </div>
  );
}

function SecurityPolicyEditor({ branchPublicId }: { branchPublicId: string }) {
  const qc = useQueryClient();
  const { data: policy, isLoading } = useQuery({
    queryKey: ['settings', 'security-policy', branchPublicId],
    queryFn: () => settingsApi.getSecurityPolicy(branchPublicId).then(r => r.data),
  });

  const [form, setForm] = useState<SecurityPolicy | null>(null);

  useEffect(() => {
    if (policy) setForm({ ...policy });
  }, [policy]);

  const mutation = useMutation({
    mutationFn: () => {
      if (!form) throw new Error('Form boş');
      return settingsApi.updateSecurityPolicy(branchPublicId, {
        twoFaRequired: form.twoFaRequired,
        twoFaSkipInternalIp: form.twoFaSkipInternalIp,
        allowedIpRanges: form.allowedIpRanges,
        sessionTimeoutMinutes: form.sessionTimeoutMinutes,
        maxFailedAttempts: form.maxFailedAttempts,
        lockoutMinutes: form.lockoutMinutes,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['settings', 'security-policy', branchPublicId] });
      toast.success('Güvenlik politikası güncellendi');
    },
    onError: () => toast.error('Güncelleme başarısız'),
  });

  if (isLoading || !form) return <LoadingSkeleton rows={4} />;

  const set = (patch: Partial<SecurityPolicy>) => setForm(prev => prev ? { ...prev, ...patch } : prev);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Shield className="h-5 w-5" /> Güvenlik Ayarları
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="grid gap-5 sm:grid-cols-2">
          <div className="space-y-3">
            <h4 className="text-sm font-semibold">İki Faktörlü Doğrulama (2FA)</h4>
            <div className="flex items-center gap-2">
              <Checkbox checked={form.twoFaRequired} onCheckedChange={v => set({ twoFaRequired: !!v })} />
              <Label>2FA zorunlu</Label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox checked={form.twoFaSkipInternalIp} onCheckedChange={v => set({ twoFaSkipInternalIp: !!v })} />
              <Label>İç ağdan girişte 2FA atla</Label>
            </div>
          </div>

          <div className="space-y-3">
            <h4 className="text-sm font-semibold">Oturum & Kilitleme</h4>
            <div className="space-y-1.5">
              <Label className="text-xs">Oturum Zaman Aşımı (dakika)</Label>
              <Input type="number" min={1} value={form.sessionTimeoutMinutes} onChange={e => set({ sessionTimeoutMinutes: parseInt(e.target.value) || 480 })} />
            </div>
            <div className="space-y-1.5">
              <Label className="text-xs">Maks. Başarısız Deneme</Label>
              <Input type="number" min={1} value={form.maxFailedAttempts} onChange={e => set({ maxFailedAttempts: parseInt(e.target.value) || 5 })} />
            </div>
            <div className="space-y-1.5">
              <Label className="text-xs">Kilitleme Süresi (dakika)</Label>
              <Input type="number" min={1} value={form.lockoutMinutes} onChange={e => set({ lockoutMinutes: parseInt(e.target.value) || 30 })} />
            </div>
          </div>
        </div>

        <div className="space-y-1.5">
          <Label>İzin Verilen IP Aralıkları (JSON)</Label>
          <Input
            value={form.allowedIpRanges ?? ''}
            onChange={e => set({ allowedIpRanges: e.target.value || null })}
            placeholder='["192.168.1.0/24"]'
            className="font-mono text-xs"
          />
          <p className="text-xs text-muted-foreground">CIDR formatında IP aralıkları. Boş bırakılırsa tüm IP'ler kabul edilir.</p>
        </div>

        <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
          Kaydet
        </Button>
      </CardContent>
    </Card>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// SİLME ONAY DİALOGU
// ═══════════════════════════════════════════════════════════════════════════════

function ConfirmDeleteDialog({ open, title, description, loading, onConfirm, onCancel }: {
  open: boolean;
  title: string;
  description: string;
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <AlertDialog open={open} onOpenChange={v => !v && onCancel()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-destructive" />
            {title}
          </AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading} onClick={onCancel}>İptal</AlertDialogCancel>
          <AlertDialogAction
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            disabled={loading}
            onClick={(e) => { e.preventDefault(); onConfirm(); }}
          >
            {loading && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
            Sil
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

// ═══════════════════════════════════════════════════════════════════════════════
// YARDIMCI BİLEŞENLER
// ═══════════════════════════════════════════════════════════════════════════════

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <p className="text-xs font-medium text-muted-foreground">{label}</p>
      {children}
    </div>
  );
}

function LoadingSkeleton({ rows }: { rows: number }) {
  return (
    <div className="space-y-3 py-4">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} className="h-10 w-full" />
      ))}
    </div>
  );
}

function langLabel(code: string) {
  const map: Record<string, string> = { tr: 'Türkçe', en: 'English', de: 'Deutsch', ar: 'العربية' };
  return map[code] ?? code;
}

// ═══════════════════════════════════════════════════════════════════════════════
// ANA SAYFA
// ═══════════════════════════════════════════════════════════════════════════════

export function SettingsPage() {
  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Ayarlar</h1>
        <p className="text-muted-foreground">Şirket, şube, kullanıcı ve güvenlik ayarlarınızı yönetin.</p>
      </div>

      <Tabs defaultValue="company">
        <TabsList>
          <TabsTrigger value="company">
            <Building2 className="h-3.5 w-3.5 mr-1.5" />
            Şirket
          </TabsTrigger>
          <TabsTrigger value="branches">
            <GitBranch className="h-3.5 w-3.5 mr-1.5" />
            Şubeler
          </TabsTrigger>
          <TabsTrigger value="users">
            <Users className="h-3.5 w-3.5 mr-1.5" />
            Kullanıcılar
          </TabsTrigger>
          <TabsTrigger value="roles">
            <KeyRound className="h-3.5 w-3.5 mr-1.5" />
            Roller
          </TabsTrigger>
          <TabsTrigger value="security">
            <Shield className="h-3.5 w-3.5 mr-1.5" />
            Güvenlik
          </TabsTrigger>
        </TabsList>

        <TabsContent value="company" className="mt-4">
          <CompanyTab />
        </TabsContent>
        <TabsContent value="branches" className="mt-4">
          <BranchesTab />
        </TabsContent>
        <TabsContent value="users" className="mt-4">
          <UsersTab />
        </TabsContent>
        <TabsContent value="roles" className="mt-4">
          <RolesTab />
        </TabsContent>
        <TabsContent value="security" className="mt-4">
          <SecurityTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
