import { useState, useRef, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { consentInstancesApi } from '@/api/consent';
import type { ConsentPublicDto } from '@/api/consent';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Check, X } from 'lucide-react';

interface ConsentCheckbox {
  id: string;
  label: string;
  isRequired: boolean;
}

function SignatureCanvas({
  label,
  canvasRef,
  onClear,
}: {
  label: string;
  canvasRef: React.RefObject<HTMLCanvasElement | null>;
  onClear: () => void;
}) {
  const isDrawingRef = useRef(false);
  const lastPointRef = useRef<{ x: number; y: number } | null>(null);

  const getPos = (e: React.MouseEvent | React.TouchEvent, canvas: HTMLCanvasElement) => {
    const rect = canvas.getBoundingClientRect();
    if ('touches' in e) {
      return { x: e.touches[0].clientX - rect.left, y: e.touches[0].clientY - rect.top };
    }
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  };

  const startDraw = (e: React.MouseEvent | React.TouchEvent) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    isDrawingRef.current = true;
    lastPointRef.current = getPos(e, canvas);
  };

  const draw = (e: React.MouseEvent | React.TouchEvent) => {
    if (!isDrawingRef.current) return;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const current = getPos(e, canvas);
    const last = lastPointRef.current!;
    ctx.beginPath();
    ctx.moveTo(last.x, last.y);
    ctx.lineTo(current.x, current.y);
    ctx.strokeStyle = '#1a1a1a';
    ctx.lineWidth = 2;
    ctx.lineCap = 'round';
    ctx.stroke();
    lastPointRef.current = current;
  };

  const endDraw = () => {
    isDrawingRef.current = false;
    lastPointRef.current = null;
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label>{label}</Label>
        <button
          className="text-xs text-muted-foreground hover:text-foreground underline"
          onClick={onClear}
        >
          Temizle
        </button>
      </div>
      <div className="border rounded-lg overflow-hidden bg-white touch-none">
        <canvas
          ref={canvasRef}
          width={600}
          height={160}
          className="w-full cursor-crosshair"
          style={{ touchAction: 'none' }}
          onMouseDown={startDraw}
          onMouseMove={draw}
          onMouseUp={endDraw}
          onMouseLeave={endDraw}
          onTouchStart={startDraw}
          onTouchMove={draw}
          onTouchEnd={endDraw}
        />
      </div>
      <p className="text-xs text-muted-foreground text-center">Parmağınızla veya fareyle imzalayın</p>
    </div>
  );
}

function isCanvasEmpty(canvas: HTMLCanvasElement): boolean {
  const ctx = canvas.getContext('2d');
  if (!ctx) return true;
  const data = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
  return !data.some((v) => v !== 0);
}

export function ConsentSigningPage() {
  const { token } = useParams<{ token: string }>();

  const { data: form, isLoading, isError } = useQuery<ConsentPublicDto>({
    queryKey: ['consent-public', token],
    queryFn: () => consentInstancesApi.getPublicForm(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const [signerName, setSignerName] = useState('');
  const [checkboxes, setCheckboxes] = useState<Record<string, boolean>>({});
  const [submitted, setSubmitted]   = useState(false);

  const patientCanvasRef = useRef<HTMLCanvasElement>(null);
  const doctorCanvasRef  = useRef<HTMLCanvasElement>(null);

  const parsedCheckboxes: ConsentCheckbox[] = (() => {
    try { return JSON.parse(form?.checkboxesJson ?? '[]'); }
    catch { return []; }
  })();

  useEffect(() => {
    if (!form) return;
    setSignerName(form.patientName || '');
    const initial: Record<string, boolean> = {};
    parsedCheckboxes.forEach(cb => { initial[cb.id] = false; });
    setCheckboxes(initial);
  }, [form?.checkboxesJson]);

  const clearPatientCanvas = () => {
    const canvas = patientCanvasRef.current;
    if (!canvas) return;
    canvas.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height);
  };

  const clearDoctorCanvas = () => {
    const canvas = doctorCanvasRef.current;
    if (!canvas) return;
    canvas.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height);
  };

  const signMutation = useMutation({
    mutationFn: (data: {
      signerName: string;
      signatureDataBase64: string;
      doctorSignatureDataBase64?: string;
      checkboxAnswersJson: string;
    }) => consentInstancesApi.sign(token!, data).then(r => r.data),
    onSuccess: (result) => {
      if (result.success) setSubmitted(true);
      else alert(result.message);
    },
    onError: () => alert('İmzalama sırasında bir hata oluştu. Lütfen tekrar deneyin.'),
  });

  const handleSubmit = () => {
    const missing = parsedCheckboxes.filter(cb => cb.isRequired && !checkboxes[cb.id]);
    if (missing.length > 0) {
      alert('Lütfen zorunlu kutucukları işaretleyin.');
      return;
    }
    const patientCanvas = patientCanvasRef.current!;
    if (isCanvasEmpty(patientCanvas)) {
      alert('Lütfen hasta imzasını çizin.');
      return;
    }
    if (form?.requireDoctorSignature) {
      const doctorCanvas = doctorCanvasRef.current!;
      if (isCanvasEmpty(doctorCanvas)) {
        alert('Lütfen doktor imzasını çizin.');
        return;
      }
    }

    const signatureDataBase64 = patientCanvas.toDataURL('image/png');
    const doctorSignatureDataBase64 = form?.requireDoctorSignature
      ? doctorCanvasRef.current?.toDataURL('image/png')
      : undefined;
    const checkboxAnswers = JSON.stringify(
      Object.entries(checkboxes).map(([id, checked]) => ({ id, checked }))
    );

    signMutation.mutate({ signerName, signatureDataBase64, doctorSignatureDataBase64, checkboxAnswersJson: checkboxAnswers });
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="w-full max-w-lg space-y-4">
          {[1, 2, 3, 4].map(i => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      </div>
    );
  }

  if (isError || !form) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="text-center space-y-2">
          <X className="size-12 text-destructive mx-auto" />
          <h1 className="text-lg font-semibold">Geçersiz Bağlantı</h1>
          <p className="text-muted-foreground text-sm">Bu onam formu bağlantısı geçersiz veya süresi dolmuş.</p>
        </div>
      </div>
    );
  }

  if (form.status === 'İmzalandı' || submitted) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="text-center space-y-4">
          <div className="size-16 rounded-full bg-emerald-100 flex items-center justify-center mx-auto">
            <Check className="size-8 text-emerald-600" />
          </div>
          <h1 className="text-xl font-semibold">Onam Formu İmzalandı</h1>
          <p className="text-muted-foreground text-sm">
            {form.status === 'İmzalandı' && form.signerName
              ? `Bu form ${form.signerName} tarafından imzalandı.`
              : 'Onam formunuz başarıyla kaydedildi. Teşekkür ederiz.'}
          </p>
          <p className="text-xs text-muted-foreground">Form No: {form.consentCode}</p>
        </div>
      </div>
    );
  }

  if (form.status === 'Süresi Doldu') {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="text-center space-y-2">
          <X className="size-12 text-amber-500 mx-auto" />
          <h1 className="text-lg font-semibold">Süre Doldu</h1>
          <p className="text-muted-foreground text-sm">Bu onam formunun süresi dolmuş. Lütfen kliniği arayın.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8 space-y-6">

        {/* Header */}
        <div className="text-center border-b pb-4">
          <h1 className="text-xl font-bold">ONAM FORMU İMZALAMA</h1>
          <p className="text-sm text-muted-foreground mt-1">Form No: {form.consentCode}</p>
        </div>

        {/* Hasta bilgileri */}
        <div className="space-y-1">
          <h2 className="font-semibold text-sm uppercase tracking-wide text-muted-foreground">Hasta Bilgileri</h2>
          <div className="border rounded-lg p-3 text-sm">
            <p><strong>Adı Soyadı:</strong> {form.patientName}</p>
          </div>
        </div>

        {/* Form içeriği */}
        <div className="space-y-2">
          <h2 className="font-semibold text-sm uppercase tracking-wide text-muted-foreground">Onam Metni</h2>
          <div
            className="border rounded-lg p-4 text-sm prose prose-sm max-w-none"
            dangerouslySetInnerHTML={{ __html: form.formContentHtml }}
          />
        </div>

        {/* Onay checkboxları */}
        {parsedCheckboxes.length > 0 && (
          <div className="space-y-2">
            <h2 className="font-semibold text-sm uppercase tracking-wide text-muted-foreground">Onay</h2>
            <div className="border rounded-lg divide-y">
              {parsedCheckboxes.map(cb => (
                <label key={cb.id} className="flex items-start gap-3 p-3 cursor-pointer">
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 accent-primary"
                    checked={checkboxes[cb.id] ?? false}
                    onChange={(e) => setCheckboxes(prev => ({ ...prev, [cb.id]: e.target.checked }))}
                  />
                  <span className="text-sm">
                    {cb.label}
                    {cb.isRequired && <span className="text-destructive ml-1">*</span>}
                  </span>
                </label>
              ))}
            </div>
            <p className="text-xs text-muted-foreground">* Zorunlu alanlar</p>
          </div>
        )}

        {/* İmzalayan adı */}
        <div className="space-y-2">
          <Label htmlFor="signer-name">İmzalayan Kişi Adı Soyadı</Label>
          <Input
            id="signer-name"
            value={signerName}
            onChange={(e) => setSignerName(e.target.value)}
            placeholder="Ad Soyad"
          />
        </div>

        {/* Hasta imzası */}
        <SignatureCanvas
          label="Hasta / Vasi İmzası"
          canvasRef={patientCanvasRef}
          onClear={clearPatientCanvas}
        />

        {/* Doktor imzası — sadece requireDoctorSignature=true ise */}
        {form.requireDoctorSignature && (
          <SignatureCanvas
            label="Doktor İmzası"
            canvasRef={doctorCanvasRef}
            onClear={clearDoctorCanvas}
          />
        )}

        <Button
          className="w-full"
          size="lg"
          disabled={signMutation.isPending}
          onClick={handleSubmit}
        >
          {signMutation.isPending ? 'Gönderiliyor...' : 'Onayla ve İmzala'}
        </Button>
      </div>
    </div>
  );
}
