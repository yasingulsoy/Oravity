import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search, Plus, ClipboardList } from 'lucide-react';
import { format } from 'date-fns';
import {
  laboratoriesApi,
  type LabWorkStatus,
  type LaboratoryWorkListItem,
} from '@/api/laboratories';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
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
import { LaboratoryWorkDetailSheet } from './LaboratoryWorkDetailSheet';
import { CreateLabWorkDialog } from './CreateLabWorkDialog';

const WORK_TYPE_LABEL: Record<string, string> = {
  prosthetic: 'Protetik', orthodontic: 'Ortodontik', implant: 'İmplant', other: 'Diğer',
};

const STATUS_META: Record<LabWorkStatus, { label: string; className: string }> = {
  pending:     { label: 'Taslak',       className: 'bg-slate-100 text-slate-700' },
  sent:        { label: 'Gönderildi',   className: 'bg-blue-100 text-blue-800' },
  in_progress: { label: 'Yapılıyor',    className: 'bg-amber-100 text-amber-800' },
  ready:       { label: 'Hazır',        className: 'bg-cyan-100 text-cyan-800' },
  received:    { label: 'Teslim Alındı', className: 'bg-indigo-100 text-indigo-800' },
  fitted:      { label: 'Takıldı',      className: 'bg-purple-100 text-purple-800' },
  completed:   { label: 'Tamamlandı',   className: 'bg-green-100 text-green-800' },
  approved:    { label: 'Onaylandı',    className: 'bg-emerald-100 text-emerald-800' },
  rejected:    { label: 'Reddedildi',   className: 'bg-red-100 text-red-800' },
  cancelled:   { label: 'İptal',        className: 'bg-zinc-100 text-zinc-700' },
};

export function LaboratoryWorksTab() {
  const [status, setStatus] = useState<string>('__all__');
  const [search, setSearch] = useState('');
  const [labFilter, setLabFilter] = useState<string>('__all__');
  const [detailPublicId, setDetailPublicId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const { data: labs } = useQuery({
    queryKey: ['laboratories', 'active'],
    queryFn: () => laboratoriesApi.list({ activeOnly: true }).then(r => r.data),
    staleTime: 60_000,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['lab-works', { status, search, labFilter }],
    queryFn: () =>
      laboratoriesApi.listWorks({
        status: status === '__all__' ? undefined : status,
        laboratoryPublicId: labFilter === '__all__' ? undefined : labFilter,
        search: search.trim() || undefined,
        pageSize: 100,
      }).then(r => r.data),
  });

  const items: LaboratoryWorkListItem[] = data?.items ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <div className="relative max-w-xs flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-8"
            placeholder="İş no, hasta, laboratuvar..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">Tüm durumlar</SelectItem>
            {Object.entries(STATUS_META).map(([k, v]) => (
              <SelectItem key={k} value={k}>{v.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={labFilter} onValueChange={setLabFilter}>
          <SelectTrigger className="w-52">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">Tüm laboratuvarlar</SelectItem>
            {(labs ?? []).map(l => (
              <SelectItem key={l.publicId} value={l.publicId}>{l.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>

        <div className="flex-1" />
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="mr-1 h-4 w-4" /> Yeni İş Emri
        </Button>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-32">İş No</TableHead>
              <TableHead>Hasta</TableHead>
              <TableHead>Hekim</TableHead>
              <TableHead>Laboratuvar</TableHead>
              <TableHead>Tip / Diş</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead className="text-right">Tutar</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={8}><Skeleton className="h-8 w-full" /></TableCell>
                </TableRow>
              ))
            ) : items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="text-center py-12 text-muted-foreground">
                  <ClipboardList className="h-8 w-8 mx-auto mb-2 opacity-50" />
                  İş emri bulunamadı.
                </TableCell>
              </TableRow>
            ) : (
              items.map(w => {
                const meta = STATUS_META[w.status];
                return (
                  <TableRow
                    key={w.publicId}
                    className="cursor-pointer"
                    onClick={() => setDetailPublicId(w.publicId)}
                  >
                    <TableCell className="font-mono text-xs">{w.workNo}</TableCell>
                    <TableCell className="font-medium">{w.patientFullName}</TableCell>
                    <TableCell>{w.doctorFullName}</TableCell>
                    <TableCell>{w.laboratoryName}</TableCell>
                    <TableCell>
                      <div className="text-sm">{WORK_TYPE_LABEL[w.workType] ?? w.workType}</div>
                      {w.toothNumbers && (
                        <div className="text-xs text-muted-foreground font-mono">
                          Dişler: {w.toothNumbers}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge className={meta.className}>{meta.label}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {format(new Date(w.createdAt), 'dd.MM.yyyy')}
                    </TableCell>
                    <TableCell className="text-right">
                      {w.totalCost != null
                        ? <span className="font-medium">{w.totalCost.toFixed(2)} {w.currency}</span>
                        : <span className="text-muted-foreground">—</span>}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {data && data.totalCount > items.length && (
        <p className="text-xs text-muted-foreground text-center">
          {items.length} / {data.totalCount} gösteriliyor
        </p>
      )}

      {detailPublicId && (
        <LaboratoryWorkDetailSheet
          publicId={detailPublicId}
          open={!!detailPublicId}
          onClose={() => setDetailPublicId(null)}
        />
      )}

      <CreateLabWorkDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
    </div>
  );
}
