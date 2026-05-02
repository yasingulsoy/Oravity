import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, CreditCard, Building2, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  settingsApi,
  type PosTerminalItem, type BankAccountItem, type BankItem,
  type CreatePosTerminalPayload, type CreateBankAccountPayload,
} from '@/api/settings';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';

// ─── Helpers ──────────────────────────────────────────────────────────────

function useBranchId() {
  const accessToken = useAuthStore(s => s.accessToken);
  const user = useAuthStore(s => s.user);
  const jwtPayload = accessToken ? parseJwt(accessToken) : null;
  const branchId = jwtPayload?.branch_id ? parseInt(jwtPayload.branch_id, 10) : user?.branchId;
  return branchId;
}

// ─── POS Terminalleri ────────────────────────────────────────────────────

function PosDialog({
  open, onClose, initial, banks, branchId,
}: {
  open: boolean;
  onClose: () => void;
  initial?: PosTerminalItem;
  banks: BankItem[];
  branchId?: number;
}) {
  const qc = useQueryClient();
  const [name,        setName]        = useState(initial?.name ?? '');
  const [bankPublicId, setBankPublicId] = useState(initial?.bankPublicId ?? '');
  const [terminalId,  setTerminalId]  = useState(initial?.terminalId ?? '');

  const mutation = useMutation({
    mutationFn: () => {
      const payload: CreatePosTerminalPayload = {
        name: name.trim(),
        bankPublicId: bankPublicId || undefined,
        terminalId: terminalId.trim() || undefined,
      };
      return initial
        ? settingsApi.updatePosTerminal(initial.publicId, payload)
        : settingsApi.createPosTerminal(payload);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pos-terminals'] });
      toast.success(initial ? 'POS cihazı güncellendi' : 'POS cihazı eklendi');
      onClose();
    },
    onError: () => toast.error('İşlem başarısız'),
  });

  const isValid = name.trim().length > 0;

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{initial ? 'POS Cihazı Düzenle' : 'POS Cihazı Ekle'}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label>Cihaz Adı *</Label>
            <Input
              placeholder="Akbank POS 1"
              value={name}
              onChange={e => setName(e.target.value)}
              autoFocus
            />
          </div>
          <div className="space-y-1.5">
            <Label>Banka</Label>
            <Select value={bankPublicId} onValueChange={setBankPublicId}>
              <SelectTrigger>
                <SelectValue placeholder="Banka seçin...">
                  {(val: string | null) => val ? (banks.find(b => b.publicId === val)?.shortName ?? val) : undefined}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">— Belirtilmemiş —</SelectItem>
                {banks.map(b => (
                  <SelectItem key={b.publicId} value={b.publicId}>
                    {b.shortName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <Label>Terminal No <span className="text-muted-foreground font-normal">(opsiyonel)</span></Label>
            <Input
              placeholder="AKB-001"
              value={terminalId}
              onChange={e => setTerminalId(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={!isValid || mutation.isPending}>
            {mutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {initial ? 'Kaydet' : 'Ekle'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function PosTerminalsCard({ banks, branchId }: { banks: BankItem[]; branchId?: number }) {
  const qc = useQueryClient();
  const [dialogOpen,  setDialogOpen]  = useState(false);
  const [editTarget,  setEditTarget]  = useState<PosTerminalItem | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<PosTerminalItem | undefined>();

  const { data: terminals, isLoading } = useQuery({
    queryKey: ['pos-terminals'],
    queryFn: () => settingsApi.listPosTerminals().then(r => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (publicId: string) => settingsApi.deletePosTerminal(publicId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pos-terminals'] });
      toast.success('POS cihazı silindi');
      setDeleteTarget(undefined);
    },
    onError: () => toast.error('Silinemedi'),
  });

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <CreditCard className="h-4 w-4" />
            POS Cihazları
          </CardTitle>
          <Button size="sm" onClick={() => { setEditTarget(undefined); setDialogOpen(true); }}>
            <Plus className="h-4 w-4 mr-1" />
            Ekle
          </Button>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-6 text-center text-muted-foreground text-sm">Yükleniyor...</div>
        ) : !terminals?.length ? (
          <div className="p-6 text-center text-muted-foreground text-sm">
            Henüz POS cihazı eklenmemiş.
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/30">
                <TableHead>Cihaz Adı</TableHead>
                <TableHead>Banka</TableHead>
                <TableHead>Terminal No</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {terminals.map(t => (
                <TableRow key={t.publicId}>
                  <TableCell className="font-medium">{t.name}</TableCell>
                  <TableCell className="text-muted-foreground">{t.bankShortName ?? '—'}</TableCell>
                  <TableCell className="font-mono text-xs text-muted-foreground">{t.terminalId ?? '—'}</TableCell>
                  <TableCell>
                    <Badge variant={t.isActive ? 'default' : 'secondary'} className="text-xs">
                      {t.isActive ? 'Aktif' : 'Pasif'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        onClick={() => { setEditTarget(t); setDialogOpen(true); }}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                        onClick={() => setDeleteTarget(t)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      {dialogOpen && (
        <PosDialog
          open={dialogOpen}
          onClose={() => { setDialogOpen(false); setEditTarget(undefined); }}
          initial={editTarget}
          banks={banks}
          branchId={branchId}
        />
      )}

      <AlertDialog open={!!deleteTarget} onOpenChange={v => !v && setDeleteTarget(undefined)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>POS Cihazını Sil</AlertDialogTitle>
            <AlertDialogDescription>
              <strong>{deleteTarget?.name}</strong> cihazını silmek istediğinizden emin misiniz?
              Bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive hover:bg-destructive/90"
              onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.publicId)}
            >
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  );
}

// ─── Banka Hesapları ──────────────────────────────────────────────────────

const CURRENCIES = ['TRY', 'EUR', 'USD', 'GBP', 'CHF'];

function BankAccountDialog({
  open, onClose, initial, banks,
}: {
  open: boolean;
  onClose: () => void;
  initial?: BankAccountItem;
  banks: BankItem[];
}) {
  const qc = useQueryClient();
  const [bankPublicId,  setBankPublicId]  = useState(initial?.bankPublicId ?? '');
  const [accountName,   setAccountName]   = useState(initial?.accountName ?? '');
  const [iban,          setIban]          = useState(initial?.iban ?? '');
  const [currency,      setCurrency]      = useState(initial?.currency ?? 'TRY');

  const mutation = useMutation({
    mutationFn: () => {
      const payload: CreateBankAccountPayload = {
        bankPublicId: bankPublicId || undefined,
        accountName:  accountName.trim(),
        iban:         iban.trim() || undefined,
        currency,
      };
      return initial
        ? settingsApi.updateBankAccount(initial.publicId, payload)
        : settingsApi.createBankAccount(payload);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['bank-accounts'] });
      toast.success(initial ? 'Banka hesabı güncellendi' : 'Banka hesabı eklendi');
      onClose();
    },
    onError: () => toast.error('İşlem başarısız'),
  });

  const isValid = accountName.trim().length > 0;

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{initial ? 'Banka Hesabı Düzenle' : 'Banka Hesabı Ekle'}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label>Banka</Label>
            <Select value={bankPublicId} onValueChange={setBankPublicId}>
              <SelectTrigger>
                <SelectValue placeholder="Banka seçin...">
                  {(val: string | null) => val ? (banks.find(b => b.publicId === val)?.shortName ?? val) : undefined}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">— Belirtilmemiş —</SelectItem>
                {banks.map(b => (
                  <SelectItem key={b.publicId} value={b.publicId}>
                    {b.shortName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <Label>Hesap Adı *</Label>
            <Input
              placeholder="TL Vadesiz"
              value={accountName}
              onChange={e => setAccountName(e.target.value)}
              autoFocus
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Para Birimi</Label>
              <Select value={currency} onValueChange={setCurrency}>
                <SelectTrigger>
                  <SelectValue>
                    {(val: string | null) => val ?? undefined}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {CURRENCIES.map(c => (
                    <SelectItem key={c} value={c}>{c}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>IBAN <span className="text-muted-foreground font-normal">(opsiyonel)</span></Label>
              <Input
                placeholder="TR00 0000..."
                value={iban}
                onChange={e => setIban(e.target.value)}
                className="font-mono text-xs"
              />
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>İptal</Button>
          <Button onClick={() => mutation.mutate()} disabled={!isValid || mutation.isPending}>
            {mutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {initial ? 'Kaydet' : 'Ekle'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function BankAccountsCard({ banks }: { banks: BankItem[] }) {
  const qc = useQueryClient();
  const [dialogOpen,   setDialogOpen]   = useState(false);
  const [editTarget,   setEditTarget]   = useState<BankAccountItem | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<BankAccountItem | undefined>();

  const { data: accounts, isLoading } = useQuery({
    queryKey: ['bank-accounts'],
    queryFn: () => settingsApi.listBankAccounts().then(r => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (publicId: string) => settingsApi.deleteBankAccount(publicId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['bank-accounts'] });
      toast.success('Banka hesabı silindi');
      setDeleteTarget(undefined);
    },
    onError: () => toast.error('Silinemedi'),
  });

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Building2 className="h-4 w-4" />
            Banka Hesapları
          </CardTitle>
          <Button size="sm" onClick={() => { setEditTarget(undefined); setDialogOpen(true); }}>
            <Plus className="h-4 w-4 mr-1" />
            Ekle
          </Button>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-6 text-center text-muted-foreground text-sm">Yükleniyor...</div>
        ) : !accounts?.length ? (
          <div className="p-6 text-center text-muted-foreground text-sm">
            Henüz banka hesabı eklenmemiş.
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/30">
                <TableHead>Banka</TableHead>
                <TableHead>Hesap Adı</TableHead>
                <TableHead>Para Birimi</TableHead>
                <TableHead className="hidden md:table-cell">IBAN</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {accounts.map(a => (
                <TableRow key={a.publicId}>
                  <TableCell className="font-medium">{a.bankShortName ?? '—'}</TableCell>
                  <TableCell>{a.accountName}</TableCell>
                  <TableCell>
                    <Badge variant="outline" className="text-xs">{a.currency}</Badge>
                  </TableCell>
                  <TableCell className="hidden md:table-cell font-mono text-xs text-muted-foreground">
                    {a.iban ?? '—'}
                  </TableCell>
                  <TableCell>
                    <Badge variant={a.isActive ? 'default' : 'secondary'} className="text-xs">
                      {a.isActive ? 'Aktif' : 'Pasif'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        onClick={() => { setEditTarget(a); setDialogOpen(true); }}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                        onClick={() => setDeleteTarget(a)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      {dialogOpen && (
        <BankAccountDialog
          open={dialogOpen}
          onClose={() => { setDialogOpen(false); setEditTarget(undefined); }}
          initial={editTarget}
          banks={banks}
        />
      )}

      <AlertDialog open={!!deleteTarget} onOpenChange={v => !v && setDeleteTarget(undefined)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Banka Hesabını Sil</AlertDialogTitle>
            <AlertDialogDescription>
              <strong>{deleteTarget?.bankShortName} – {deleteTarget?.accountName}</strong> hesabını
              silmek istediğinizden emin misiniz?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive hover:bg-destructive/90"
              onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.publicId)}
            >
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  );
}

// ─── Ana Sekme ────────────────────────────────────────────────────────────

export function FinancialSettingsTab() {
  const branchId = useBranchId();

  const { data: banks } = useQuery({
    queryKey: ['settings-banks'],
    queryFn: () => settingsApi.listBanks().then(r => r.data),
  });

  const bankList = banks ?? [];

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h2 className="text-lg font-semibold">Finansal Ayarlar</h2>
        <p className="text-sm text-muted-foreground mt-1">
          POS cihazları ve banka hesaplarını bu şube için yönetin.
        </p>
      </div>

      {!branchId && (
        <div className="rounded-lg border bg-amber-50 border-amber-200 p-4 text-sm text-amber-800">
          Bu ayarlar şube bazlıdır. Lütfen bir şube bağlamıyla giriş yapın.
        </div>
      )}

      <PosTerminalsCard banks={bankList} branchId={branchId} />
      <BankAccountsCard banks={bankList} />
    </div>
  );
}
