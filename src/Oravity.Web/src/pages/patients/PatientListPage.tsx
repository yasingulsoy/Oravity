import { useState, useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Plus, Search, Phone, CreditCard, User, Eye, Users } from 'lucide-react';
import { patientsApi } from '@/api/patients';
import type { PatientListRequest } from '@/types/patient';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

// ─── Yardımcı: TC hash ────────────────────────────────────────────────────
async function sha256hex(text: string): Promise<string> {
  const buf = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return Array.from(new Uint8Array(buf))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

// ─── Arama tipi tespiti ───────────────────────────────────────────────────
type SearchMode = 'name' | 'phone' | 'tc' | 'empty';

/**
 * TC Kimlik No: tam olarak 11 hane, asla 0 ile başlamaz.
 * Türk GSM: 05XXXXXXXXX (11 hane, 0 ile başlar) veya 5XXXXXXXXX (10 hane).
 */
function detectMode(raw: string): SearchMode {
  const t = raw.trim();
  if (!t) return 'empty';

  const digits = t.replace(/[\s\-]/g, '');

  if (/^[1-9]\d{10}$/.test(digits)) return 'tc';

  if (
    /^(\+90|0090)\d{10}$/.test(digits) ||
    /^05\d{9}$/.test(digits) ||
    /^5\d{9}$/.test(digits)
  ) return 'phone';

  return 'name';
}

const modeLabel: Record<SearchMode, { label: string; icon: React.ElementType } | null> = {
  empty: null,
  name:  { label: 'Ad / soyad araması',    icon: User       },
  phone: { label: 'Telefon ile arama',      icon: Phone      },
  tc:    { label: 'TC kimlik ile arama',    icon: CreditCard },
};

const genderLabel: Record<string, string> = {
  male: 'Erkek', female: 'Kadın', other: 'Diğer',
};

// ─── Component ───────────────────────────────────────────────────────────
export function PatientListPage() {
  const [raw, setRaw]           = useState('');
  const [page, setPage]         = useState(1);
  const [params, setParams]     = useState<PatientListRequest | null>(null);
  const debounceRef             = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pageSize                = 20;
  const mode                    = detectMode(raw);
  const isSearching             = raw.trim().length >= 3;

  // Arama params'ını debounce ile güncelle — min 3 karakter
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);

    debounceRef.current = setTimeout(async () => {
      const t = raw.trim();

      // 3 karakterden azsa query'yi sıfırla
      if (t.length < 3) {
        setParams(null);
        return;
      }

      let next: PatientListRequest = { page, pageSize };

      if (mode === 'tc') {
        next.tcHash = await sha256hex(t);
      } else if (mode === 'phone') {
        next.phone = t.replace(/[\s\-]/g, '');
      } else {
        const parts = t.split(/\s+/);
        next.firstName = parts[0];
        if (parts.length > 1) next.lastName = parts.slice(1).join(' ');
      }

      setParams(next);
    }, 350);

    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, [raw, page, mode]);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['patients', params],
    queryFn: () => patientsApi.list(params!),
    enabled: params !== null,
  });

  const patients   = data?.data.items ?? [];
  const total      = data?.data.totalCount ?? 0;
  const totalPages = data?.data.totalPages ?? 1;

  function handleSearchChange(value: string) {
    setRaw(value);
    setPage(1);
  }

  const hint = raw.trim() ? modeLabel[mode] : null;

  return (
    <div className="space-y-6">
      {/* Başlık */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Hastalar</h1>
          <p className="text-muted-foreground">Hasta kayıtlarını yönetin</p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Hasta
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-3">
          {/* Arama kutusu */}
          <div className="space-y-1.5">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Ad soyad, telefon numarası veya TC kimlik no girin…"
                value={raw}
                onChange={(e) => handleSearchChange(e.target.value)}
                className="pl-9 pr-4"
                autoComplete="off"
                spellCheck={false}
              />
            </div>

            {/* Arama modu ipucu */}
            {hint ? (
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground pl-1">
                <hint.icon className="h-3.5 w-3.5" />
                <span>{hint.label}</span>
                {!isSearching
                  ? <span className="ml-1 opacity-60">{3 - raw.trim().length} hane daha…</span>
                  : isFetching && <span className="ml-1 opacity-60">aranıyor…</span>
                }
              </div>
            ) : (
              <div className="flex flex-wrap gap-3 pl-1 text-xs text-muted-foreground">
                <span className="flex items-center gap-1"><User className="h-3 w-3" /> Ad soyad</span>
                <span className="flex items-center gap-1"><Phone className="h-3 w-3" /> Telefon numarası</span>
                <span className="flex items-center gap-1"><CreditCard className="h-3 w-3" /> TC kimlik no (11 hane)</span>
              </div>
            )}
          </div>
        </CardHeader>

        <CardContent className="pt-0">
          {/* Arama yapılmadı — boş durum */}
          {!isSearching ? (
            <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
              <Users className="h-10 w-10 opacity-30" />
              <p className="text-sm">Hasta aramak için yukarıdaki alana en az 3 karakter girin.</p>
            </div>
          ) : (
            <>
              {/* Sonuç sayısı */}
              {!isLoading && (
                <p className="text-xs text-muted-foreground mb-3">
                  {total} sonuç bulundu
                </p>
              )}

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Ad Soyad</TableHead>
                    <TableHead>E-posta</TableHead>
                    <TableHead>Telefon</TableHead>
                    <TableHead>Cinsiyet</TableHead>
                    <TableHead className="text-right">İşlemler</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={i}>
                        {Array.from({ length: 5 }).map((_, j) => (
                          <TableCell key={j}><Skeleton className="h-4 w-24" /></TableCell>
                        ))}
                      </TableRow>
                    ))
                  ) : patients.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                        Arama kriterlerine uyan hasta bulunamadı.
                      </TableCell>
                    </TableRow>
                  ) : (
                    patients.map((patient) => (
                      <TableRow key={patient.publicId}>
                        <TableCell>
                          <Link
                            to={`/patients/${patient.publicId}`}
                            className="font-medium text-primary hover:underline"
                          >
                            {patient.firstName} {patient.lastName}
                          </Link>
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {patient.email ?? <span className="opacity-40">—</span>}
                        </TableCell>
                        <TableCell>{patient.phone ?? <span className="opacity-40">—</span>}</TableCell>
                        <TableCell>
                          {patient.gender && (
                            <Badge variant="secondary">
                              {genderLabel[patient.gender] ?? patient.gender}
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell className="text-right">
                          <Button variant="ghost" size="icon" asChild title="Hasta Detayı">
                            <Link to={`/patients/${patient.publicId}`}>
                              <Eye className="h-4 w-4" />
                            </Link>
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Sayfalama */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between pt-4">
                  <span className="text-sm text-muted-foreground">
                    Toplam {total} hasta · Sayfa {page} / {totalPages}
                  </span>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page <= 1}
                      onClick={() => setPage((p) => p - 1)}>
                      Önceki
                    </Button>
                    <Button variant="outline" size="sm" disabled={page >= totalPages}
                      onClick={() => setPage((p) => p + 1)}>
                      Sonraki
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
