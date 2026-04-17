import { useState, useRef, useCallback } from 'react';
import { NavLink } from 'react-router-dom';
import { ChevronsLeft, ChevronsRight, LogOut, ShieldCheck } from 'lucide-react';
import { useUiStore } from '@/store/uiStore';
import { useAuthStore } from '@/store/authStore';
import { useResolvedDark } from '@/hooks/useResolvedDark';
import { useLogout } from '@/hooks/useAuth';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { cn } from '@/lib/utils';
import { navSections } from './nav-items';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';

export function Sidebar() {
  const pinned = useUiStore((s) => s.sidebarExpanded);
  const togglePin = useUiStore((s) => s.toggleSidebarExpanded);
  const user = useAuthStore((s) => s.user);
  const logoutMutation = useLogout();
  const isDark = useResolvedDark();

  const [hovered, setHovered] = useState(false);
  const leaveTimer = useRef<ReturnType<typeof setTimeout>>(null);

  const expanded = pinned || hovered;

  const handleEnter = useCallback(() => {
    if (leaveTimer.current) clearTimeout(leaveTimer.current);
    setHovered(true);
  }, []);

  const handleLeave = useCallback(() => {
    leaveTimer.current = setTimeout(() => setHovered(false), 200);
  }, []);

  const initials = user
    ? user.name
        .split(' ')
        .filter(Boolean)
        .map((n) => n[0])
        .join('')
        .slice(0, 2)
        .toUpperCase()
    : '?';

  return (
    <TooltipProvider delayDuration={0}>
      <aside
        onMouseEnter={handleEnter}
        onMouseLeave={handleLeave}
        className={cn(
          'sidebar-solid relative flex h-full min-h-0 flex-col border-r border-border/40 shrink-0 transition-[width] duration-300 ease-in-out overflow-hidden',
          expanded ? 'w-[232px]' : 'w-[64px]',
        )}
      >
        {/* Üst şerit — Header ile aynı yükseklik */}
        <div
          className={cn(
            'flex h-14 shrink-0 items-center border-b border-border/40',
            expanded ? 'gap-3 px-3' : 'justify-center px-0',
          )}
        >
          <NavLink
            to="/dashboard"
            className={cn(
              'group flex min-h-0 items-center rounded-xl transition-colors hover:bg-accent/50',
              expanded ? 'min-h-9 flex-1 gap-2.5 px-2' : 'h-9 w-9 shrink-0 justify-center',
            )}
          >
            <div className="relative h-7 w-7 shrink-0 overflow-hidden">
              <img
                src="/logos/2.png"
                alt="Oravity"
                className={cn(
                  'absolute inset-0 h-full w-full object-contain transition-opacity duration-300',
                  isDark ? 'opacity-0' : 'opacity-100',
                )}
              />
              <img
                src="/logos/3.png"
                alt="Oravity"
                className={cn(
                  'absolute inset-0 h-full w-full object-contain transition-opacity duration-300',
                  isDark ? 'opacity-100' : 'opacity-0',
                )}
              />
            </div>
            {expanded && (
              <span className="sidebar-logo-shine select-none truncate whitespace-nowrap text-[15px] font-semibold tracking-tight">
                Oravity
              </span>
            )}
          </NavLink>

          {/* Pin toggle — expanded modda header sağında, collapsed modda gizli */}
          {expanded && (
            <Tooltip>
              <TooltipTrigger
                render={<button type="button" />}
                onClick={togglePin}
                className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              >
                {pinned ? (
                  <ChevronsLeft className="h-4 w-4" />
                ) : (
                  <ChevronsRight className="h-4 w-4" />
                )}
              </TooltipTrigger>
              <TooltipContent side="bottom" sideOffset={6} className="text-xs">
                {pinned ? 'Sabitlemeyi kaldır' : 'Sabitle'}
              </TooltipContent>
            </Tooltip>
          )}
        </div>

        {/* Nav — scrollable */}
        <nav
          className={cn(
            'scrollbar-none flex min-h-0 flex-1 flex-col overflow-y-auto py-3',
            expanded ? 'self-stretch px-2.5' : 'items-center gap-3 px-0',
          )}
        >
          {navSections.map((section, sIdx) => (
            <div key={section.label} className={cn('flex flex-col', expanded ? 'mb-4 last:mb-1' : 'w-full')}>
              {/* Section label — expanded modda yazı, collapsed modda ince ayırıcı */}
              {expanded ? (
                <div className="px-2.5 pb-1.5 pt-1 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/70">
                  {section.label}
                </div>
              ) : (
                sIdx > 0 && (
                  <div aria-hidden className="mx-auto mb-2 h-px w-6 bg-border/60" />
                )
              )}

              <div className={cn('flex flex-col gap-0.5', !expanded && 'items-center')}>
                {section.items.map(({ to, label, icon: Icon }) => {
                  if (expanded) {
                    return (
                      <NavLink
                        key={to}
                        to={to}
                        className={({ isActive }) =>
                          cn(
                            'group relative flex items-center gap-3 whitespace-nowrap rounded-xl px-2.5 py-2 text-[13px] font-medium transition-all duration-200',
                            isActive
                              ? 'bg-accent text-foreground shadow-sm'
                              : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
                          )
                        }
                      >
                        {({ isActive }) => (
                          <>
                            {/* Sol aktif bar */}
                            <span
                              aria-hidden
                              className={cn(
                                'absolute left-0 top-1/2 h-5 w-[3px] -translate-y-1/2 rounded-r-full transition-all duration-200',
                                isActive
                                  ? 'bg-primary opacity-100'
                                  : 'bg-transparent opacity-0 group-hover:bg-primary/40 group-hover:opacity-100',
                              )}
                            />
                            <Icon
                              className={cn(
                                'h-[17px] w-[17px] shrink-0 transition-colors',
                                isActive && 'text-primary',
                              )}
                            />
                            <span className="truncate">{label}</span>
                          </>
                        )}
                      </NavLink>
                    );
                  }

                  // Collapsed — icon + tooltip
                  return (
                    <Tooltip key={to}>
                      <TooltipTrigger
                        render={
                          <NavLink
                            to={to}
                            className={({ isActive }: { isActive: boolean }) =>
                              cn(
                                'relative flex h-9 w-9 items-center justify-center rounded-xl transition-all duration-200',
                                isActive
                                  ? 'bg-accent text-primary shadow-sm'
                                  : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
                              )
                            }
                          />
                        }
                      >
                        <Icon className="h-[18px] w-[18px] shrink-0" />
                      </TooltipTrigger>
                      <TooltipContent
                        side="right"
                        sideOffset={10}
                        className="text-xs font-medium"
                      >
                        {label}
                      </TooltipContent>
                    </Tooltip>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* User card + logout */}
        <div
          className={cn(
            'shrink-0 border-t border-border/40 p-2.5',
            !expanded && 'flex flex-col items-center gap-2 px-0 py-2.5',
          )}
        >
          {expanded ? (
            <div className="flex items-center gap-2.5 rounded-xl p-1.5 transition-colors hover:bg-accent/50">
              <Avatar className="h-8 w-8 border border-border/60">
                <AvatarFallback className="bg-primary/10 text-[11px] font-semibold text-primary">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="flex min-w-0 flex-1 flex-col">
                <div className="flex items-center gap-1">
                  <span className="truncate text-[12.5px] font-medium leading-tight">
                    {user?.name ?? 'Kullanıcı'}
                  </span>
                  {user?.isPlatformAdmin && (
                    <ShieldCheck
                      className="h-3 w-3 shrink-0 text-primary"
                      aria-label="Platform Admin"
                    />
                  )}
                </div>
                <span className="truncate text-[11px] leading-tight text-muted-foreground">
                  {user?.email ?? ''}
                </span>
              </div>
              <Tooltip>
                <TooltipTrigger
                  render={<button type="button" />}
                  onClick={() => logoutMutation.mutate()}
                  disabled={logoutMutation.isPending}
                  className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive disabled:opacity-50"
                >
                  <LogOut className="h-4 w-4" />
                </TooltipTrigger>
                <TooltipContent side="top" sideOffset={6} className="text-xs">
                  Çıkış Yap
                </TooltipContent>
              </Tooltip>
            </div>
          ) : (
            <>
              <Tooltip>
                <TooltipTrigger
                  render={<button type="button" />}
                  className="relative"
                >
                  <Avatar className="h-9 w-9 border border-border/60">
                    <AvatarFallback className="bg-primary/10 text-[11px] font-semibold text-primary">
                      {initials}
                    </AvatarFallback>
                  </Avatar>
                  {user?.isPlatformAdmin && (
                    <span
                      aria-hidden
                      className="absolute -bottom-0.5 -right-0.5 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-background"
                    >
                      <ShieldCheck className="h-3 w-3 text-primary" />
                    </span>
                  )}
                </TooltipTrigger>
                <TooltipContent side="right" sideOffset={10} className="text-xs">
                  <div className="font-medium">{user?.name ?? 'Kullanıcı'}</div>
                  {user?.email && (
                    <div className="text-muted-foreground">{user.email}</div>
                  )}
                </TooltipContent>
              </Tooltip>

              <Tooltip>
                <TooltipTrigger
                  render={<button type="button" />}
                  onClick={togglePin}
                  className="flex h-8 w-9 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                >
                  <ChevronsRight className="h-4 w-4 shrink-0" />
                </TooltipTrigger>
                <TooltipContent side="right" sideOffset={10} className="text-xs">
                  Sabitle
                </TooltipContent>
              </Tooltip>

              <Tooltip>
                <TooltipTrigger
                  render={<button type="button" />}
                  onClick={() => logoutMutation.mutate()}
                  disabled={logoutMutation.isPending}
                  className="flex h-8 w-9 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive disabled:opacity-50"
                >
                  <LogOut className="h-4 w-4 shrink-0" />
                </TooltipTrigger>
                <TooltipContent side="right" sideOffset={10} className="text-xs">
                  Çıkış Yap
                </TooltipContent>
              </Tooltip>
            </>
          )}
        </div>
      </aside>
    </TooltipProvider>
  );
}
