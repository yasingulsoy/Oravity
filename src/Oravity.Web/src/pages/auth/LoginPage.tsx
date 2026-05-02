import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { Navigate } from 'react-router-dom';
import { ArrowLeft, Building2, Moon, Sun } from 'lucide-react';
import { useLogin } from '@/hooks/useAuth';
import { useResolvedDark } from '@/hooks/useResolvedDark';
import { useAuthStore } from '@/store/authStore';
import { useUiStore } from '@/store/uiStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { getApiErrorMessage } from '@/lib/api-error';
import { cn } from '@/lib/utils';

const loginSchema = z.object({
  email: z.email('Geçerli bir e-posta adresi giriniz'),
  password: z.string().min(6, 'Şifre en az 6 karakter olmalıdır'),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const { mutation: loginMutation, pendingBranches, submitCredentials, selectBranch, clearBranchSelection } = useLogin();
  const { setTheme } = useUiStore();
  const isDark = useResolvedDark();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const onSubmit = (data: LoginForm) => {
    submitCredentials(data);
  };

  return (
    <div className="relative min-h-[100dvh] overflow-hidden bg-muted/30 dark:bg-black">
      {/* Nokta dokusu + radial gecisler */}
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(oklch(0.55_0_0/0.09)_1px,transparent_1px)] bg-[length:24px_24px] dark:bg-[radial-gradient(oklch(1_0_0/0.05)_1px,transparent_1px)]"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_80%_60%_at_50%_-30%,oklch(0.75_0.05_260/0.35),transparent)] dark:bg-[radial-gradient(ellipse_70%_45%_at_50%_-20%,oklch(0.35_0.08_260/0.35),transparent)]"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute bottom-0 left-1/2 h-[40vh] w-[120%] max-w-6xl -translate-x-1/2 rounded-[100%] bg-gradient-to-t from-background to-transparent dark:from-black dark:to-transparent"
        aria-hidden
      />

      <div className="absolute right-4 top-4 z-20 sm:right-6 sm:top-6">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="relative h-11 w-11 rounded-2xl border border-border/70 bg-background/70 shadow-sm backdrop-blur-md hover:bg-accent dark:border-white/10 dark:bg-black/60"
          onClick={() => setTheme(isDark ? 'light' : 'dark')}
          aria-label={isDark ? 'Aydınlık temaya geç' : 'Karanlık temaya geç'}
        >
          <Sun
            className={cn(
              'h-[18px] w-[18px] transition-all duration-300',
              isDark
                ? 'rotate-0 scale-100 opacity-100'
                : 'absolute -rotate-90 scale-0 opacity-0',
            )}
          />
          <Moon
            className={cn(
              'h-[18px] w-[18px] transition-all duration-300',
              isDark
                ? 'absolute rotate-90 scale-0 opacity-0'
                : 'rotate-0 scale-100 opacity-100',
            )}
          />
        </Button>
      </div>

      <div className="relative z-10 flex min-h-[100dvh] items-center justify-center p-4 sm:p-6">
        <div className="w-full max-w-[400px]">
          <div
            className={cn(
              'relative overflow-hidden rounded-[28px] border border-border/80 bg-card/90 shadow-[0_24px_64px_-12px_rgba(15,23,42,0.12)] backdrop-blur-xl',
              'dark:border-white/10 dark:bg-black/80 dark:shadow-[0_24px_80px_-8px_rgba(0,0,0,0.8)]',
            )}
          >
            <div
              className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-primary/50 to-transparent"
              aria-hidden
            />
            <div className="px-8 pb-9 pt-10 sm:px-10 sm:pb-10 sm:pt-11">
              <div className="mb-8 flex flex-col items-center text-center">
                <div
                  className="relative mb-5 flex h-[72px] w-[72px] items-center justify-center overflow-hidden rounded-[22px] border border-border/60 bg-gradient-to-b from-background to-muted/50 shadow-inner ring-1 ring-black/[0.04] dark:border-white/10 dark:from-neutral-900 dark:to-black dark:ring-white/[0.06]"
                  aria-hidden
                >
                  <img
                    src="/logos/2.png"
                    alt=""
                    className={cn(
                      'absolute h-[52px] w-[52px] object-contain transition-opacity duration-300',
                      isDark ? 'opacity-0' : 'opacity-100',
                    )}
                  />
                  <img
                    src="/logos/3.png"
                    alt=""
                    className={cn(
                      'absolute h-[52px] w-[52px] object-contain transition-opacity duration-300',
                      isDark ? 'opacity-100' : 'opacity-0',
                    )}
                  />
                </div>
                <h1 className="sidebar-logo-shine text-[1.65rem] font-semibold tracking-tight sm:text-[1.75rem]">
                  Oravity
                </h1>
                <div className="mt-4 h-px w-10 rounded-full bg-border dark:bg-white/15" />
              </div>

              {pendingBranches ? (
                /* Sube secim asamasi */
                <div>
                  <h2 className="mb-2 text-left text-lg font-semibold tracking-tight text-foreground">
                    Hangi klinikte çalışıyorsunuz?
                  </h2>
                  <p className="mb-5 text-sm text-muted-foreground">
                    Hesabınız birden fazla kliniğe bağlı. Bugün çalışacağınız kliniği seçin.
                  </p>

                  <div className="space-y-2">
                    {pendingBranches.map((branch) => (
                      <button
                        key={branch.id}
                        type="button"
                        onClick={() => selectBranch(branch.id)}
                        className="flex w-full items-center gap-3 rounded-xl border border-border/70 bg-background px-4 py-3 text-left transition-colors hover:border-primary/30 hover:bg-accent disabled:pointer-events-none disabled:opacity-50 dark:border-white/10 dark:bg-black/60 dark:hover:border-primary/30 dark:hover:bg-white/5"
                        disabled={loginMutation.isPending}
                      >
                        <Building2 className="size-4 shrink-0 text-muted-foreground" />
                        <span className="font-medium">{branch.name}</span>
                      </button>
                    ))}
                  </div>

                  {loginMutation.isError && (
                    <p className="mt-4 rounded-xl border border-destructive/25 bg-destructive/10 px-3.5 py-2.5 text-sm text-destructive">
                      {getApiErrorMessage(
                        loginMutation.error,
                        'Giriş başarısız. Lütfen tekrar deneyin.',
                      )}
                    </p>
                  )}

                  <button
                    type="button"
                    onClick={clearBranchSelection}
                    className="mt-4 flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                    disabled={loginMutation.isPending}
                  >
                    <ArrowLeft className="size-3.5" />
                    Geri dön
                  </button>
                </div>
              ) : (
                /* Email/Password formu */
                <>
                  <h2 className="mb-6 text-left text-lg font-semibold tracking-tight text-foreground">
                    Giriş yap
                  </h2>

                  <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                    <div className="space-y-2">
                      <Label htmlFor="email" className="text-sm font-medium">
                        E-posta
                      </Label>
                      <Input
                        id="email"
                        type="email"
                        autoComplete="username"
                        placeholder="ornek@oravity.com"
                        className="h-12 rounded-xl border-border/70 bg-background px-4 text-[15px] shadow-sm transition-shadow focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 dark:border-white/10 dark:bg-black/60"
                        {...register('email')}
                      />
                      {errors.email && (
                        <p className="text-sm text-destructive">{errors.email.message}</p>
                      )}
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="password" className="text-sm font-medium">
                        Şifre
                      </Label>
                      <Input
                        id="password"
                        type="password"
                        autoComplete="current-password"
                        placeholder="••••••••"
                        className="h-12 rounded-xl border-border/70 bg-background px-4 text-[15px] shadow-sm transition-shadow focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 dark:border-white/10 dark:bg-black/60"
                        {...register('password')}
                      />
                      {errors.password && (
                        <p className="text-sm text-destructive">{errors.password.message}</p>
                      )}
                    </div>

                    {loginMutation.isError && (
                      <p className="rounded-xl border border-destructive/25 bg-destructive/10 px-3.5 py-2.5 text-sm text-destructive">
                        {getApiErrorMessage(
                          loginMutation.error,
                          'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.',
                        )}
                      </p>
                    )}

                    <Button
                      type="submit"
                      size="lg"
                      className="mt-1 h-12 w-full rounded-xl text-[15px] font-semibold shadow-md transition-transform active:scale-[0.99]"
                      disabled={loginMutation.isPending}
                    >
                      {loginMutation.isPending ? 'Giriş yapılıyor...' : 'Giriş Yap'}
                    </Button>
                  </form>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
