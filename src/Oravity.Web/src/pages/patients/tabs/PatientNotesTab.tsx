import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { Plus, Save, Trash2, StickyNote, Pin, BellRing } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import { Checkbox } from '@/components/ui/checkbox';
import { cn } from '@/lib/utils';
import { patientsApi } from '@/api/patients';
import type { PatientNote, NoteType } from '@/types/patient';

export const NOTE_TYPE_LABELS: Record<NoteType, string> = {
  1: 'Genel Not',
  2: 'Klinik Not',
  3: 'Gizli Not',
  4: 'Plan Notu',
  5: 'Tedavi Notu',
  6: 'Ortodonti Notu',
};

export const NOTE_TYPE_COLORS: Record<NoteType, string> = {
  1: 'bg-slate-100 text-slate-700',
  2: 'bg-blue-100 text-blue-700',
  3: 'bg-red-100 text-red-700',
  4: 'bg-violet-100 text-violet-700',
  5: 'bg-emerald-100 text-emerald-700',
  6: 'bg-amber-100 text-amber-700',
};

export function PatientNotesTab({ patientPublicId }: { patientPublicId: string }) {
  const qc = useQueryClient();
  const [newContent, setNewContent] = useState('');
  const [newType, setNewType] = useState<NoteType>(1);
  const [newTitle, setNewTitle] = useState('');
  const [newIsAlert, setNewIsAlert] = useState(false);
  const [adding, setAdding] = useState(false);

  const { data: notes = [], isLoading } = useQuery<PatientNote[]>({
    queryKey: ['patient-notes', patientPublicId],
    queryFn: () => patientsApi.getNotes(patientPublicId).then(r => r.data),
    staleTime: 30_000,
  });

  const createMut = useMutation({
    mutationFn: () =>
      patientsApi.createNote(patientPublicId, {
        type: newType,
        content: newContent.trim(),
        title: newTitle.trim() || undefined,
        isAlert: newIsAlert || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['patient-notes', patientPublicId] });
      setNewContent('');
      setNewTitle('');
      setNewIsAlert(false);
      setAdding(false);
    },
    onError: () => toast.error('Not kaydedilemedi'),
  });

  const deleteMut = useMutation({
    mutationFn: (notePublicId: string) => patientsApi.deleteNote(patientPublicId, notePublicId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['patient-notes', patientPublicId] }),
    onError: () => toast.error('Not silinemedi'),
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {!adding ? (
        <Button size="sm" variant="outline" className="gap-1.5" onClick={() => setAdding(true)}>
          <Plus className="size-3.5" />
          Not Ekle
        </Button>
      ) : (
        <div className="rounded-lg border bg-muted/30 p-4 space-y-3">
          <div className="flex items-center gap-2">
            <select
              value={newType}
              onChange={e => setNewType(Number(e.target.value) as NoteType)}
              className="text-xs border rounded px-2 py-1 bg-background"
            >
              {([1, 2] as NoteType[]).map(t => (
                <option key={t} value={t}>{NOTE_TYPE_LABELS[t]}</option>
              ))}
            </select>
            <Input
              placeholder="Başlık (opsiyonel)"
              value={newTitle}
              onChange={e => setNewTitle(e.target.value)}
              className="h-7 text-sm flex-1"
            />
          </div>
          <Textarea
            placeholder="Not içeriği…"
            value={newContent}
            onChange={e => setNewContent(e.target.value)}
            className="min-h-[80px] text-sm resize-none"
            autoFocus
          />
          <div className="flex items-center gap-2">
            <Checkbox
              id="note-alert-patients"
              checked={newIsAlert}
              onCheckedChange={v => setNewIsAlert(v === true)}
            />
            <label htmlFor="note-alert-patients" className="text-xs text-muted-foreground cursor-pointer select-none flex items-center gap-1">
              <BellRing className="size-3 text-orange-500" />
              Hasta kartı açılırken uyarı olarak göster
            </label>
          </div>
          <div className="flex gap-2">
            <Button
              size="sm"
              disabled={!newContent.trim() || createMut.isPending}
              onClick={() => createMut.mutate()}
            >
              <Save className="size-3.5 mr-1" />
              Kaydet
            </Button>
            <Button size="sm" variant="ghost" onClick={() => { setAdding(false); setNewContent(''); setNewTitle(''); setNewIsAlert(false); }}>
              İptal
            </Button>
          </div>
        </div>
      )}

      {notes.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-10 text-muted-foreground">
          <StickyNote className="size-8 opacity-30" />
          <p className="text-sm">Henüz not eklenmemiş</p>
        </div>
      ) : (
        <div className="space-y-2">
          {notes.map(note => (
            <div
              key={note.publicId}
              className={cn(
                'rounded-lg border bg-background p-3 space-y-1.5',
                note.isAlert && 'border-orange-400 bg-orange-50/50 dark:bg-orange-950/20',
              )}
            >
              <div className="flex items-start gap-2">
                {note.isPinned && <Pin className="size-3 text-amber-500 mt-0.5 shrink-0" />}
                {note.isAlert && <BellRing className="size-3 text-orange-500 mt-0.5 shrink-0" />}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={cn('text-[10px] px-1.5 py-0.5 rounded-full font-medium', NOTE_TYPE_COLORS[note.type])}>
                      {NOTE_TYPE_LABELS[note.type]}
                    </span>
                    {note.title && <span className="text-xs font-semibold truncate">{note.title}</span>}
                    <span className="text-xs text-muted-foreground ml-auto shrink-0">
                      {format(new Date(note.createdAt), 'd MMM yyyy HH:mm', { locale: tr })}
                    </span>
                  </div>
                  <p className="text-sm whitespace-pre-wrap">{note.content}</p>
                  <p className="text-[11px] text-muted-foreground mt-1">{note.createdByName}</p>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6 shrink-0 text-muted-foreground hover:text-destructive"
                  onClick={() => deleteMut.mutate(note.publicId)}
                  disabled={deleteMut.isPending}
                >
                  <Trash2 className="size-3" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
