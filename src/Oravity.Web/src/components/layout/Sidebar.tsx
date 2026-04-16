import { useState, useRef, useCallback } from 'react';
import { NavLink } from 'react-router-dom';
import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { useUiStore } from '@/store/uiStore';
import { useResolvedDark } from '@/hooks/useResolvedDark';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { navItems } from './nav-items';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';

export function Sidebar() {
  const pinned = useUiStore((s) => s.sidebarExpanded);
  const togglePin = useUiStore((s) => s.toggleSidebarExpanded);
  const isDark = useResolvedDark();

  const [hovered, setHovered] = useState(false);
  const leaveTimer = useRef<ReturnType<typeof setTimeout>>(null);

  const expanded = pinned || hovered;

  const handleEnter = useCallback(() => {
    if (leaveTimer.current) clearTimeout(leaveTimer.current);
    setHovered(true);
  }, []);

  const handleLeave = useCallback(() => {
    leaveTimer.current = setTimeout(() => setHovered(false), 250);
  }, []);

  return (
    <TooltipProvider delayDuration={0}>
      <aside
        onMouseEnter={handleEnter}
        onMouseLeave={handleLeave}
        className={cn(
          'sidebar-solid flex h-full min-h-0 flex-col border-r border-border/40 shrink-0 transition-[width] duration-300 ease-in-out overflow-hidden',
          expanded ? 'w-[200px]' : 'w-[60px]',
        )}
      >
        {/* Üst şerit: Header ile aynı yükseklik (h-14) ve aynı alt çizgi */}
        <div
          className={cn(
            'flex h-14 shrink-0 items-center border-b border-border/40',
            expanded ? 'gap-3 px-2.5' : 'justify-center px-0',
          )}
        >
          <NavLink
            to="/dashboard"
            className={cn(
              'flex min-h-0 items-center rounded-xl hover:bg-accent/50 transition-colors',
              expanded ? 'min-h-9 flex-1 gap-3 px-2' : 'h-9 w-9 shrink-0 justify-center',
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
              <span className="text-[15px] font-semibold tracking-tight select-none sidebar-logo-shine truncate whitespace-nowrap">
                Oravity
              </span>
            )}
          </NavLink>
        </div>

        {/* Nav */}
        <nav
          className={cn(
            'flex min-h-0 flex-1 flex-col gap-0.5 overflow-y-auto py-2 scrollbar-none',
            expanded ? 'self-stretch px-2.5' : 'items-center',
          )}
        >
          {navItems.map(({ to, label, icon: Icon }) => {
            const link = (
              <NavLink
                key={to}
                to={to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center rounded-xl transition-all duration-200 whitespace-nowrap',
                    expanded
                      ? 'gap-3 px-2.5 py-2 text-[13px] font-medium'
                      : 'h-9 w-9 justify-center',
                    isActive
                      ? 'bg-accent text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground hover:bg-accent/50',
                  )
                }
              >
                <Icon className="h-[18px] w-[18px] shrink-0" />
                {expanded && <span className="truncate">{label}</span>}
              </NavLink>
            );

            if (!expanded) {
              return (
                <Tooltip key={to}>
                  <TooltipTrigger asChild>{link}</TooltipTrigger>
                  <TooltipContent side="right" sideOffset={10} className="text-xs">
                    {label}
                  </TooltipContent>
                </Tooltip>
              );
            }

            return link;
          })}
        </nav>

        {/* Pin / Unpin toggle — daraltılmışken tooltip; genişleyince sarmalayıcı kalksın (portal takılmasın) */}
        <div className={cn('pt-2 border-t border-border/40 mt-1', expanded ? 'self-stretch mx-2.5' : 'w-6')}>
          {expanded ? (
            <Button
              variant="ghost"
              size="sm"
              onClick={togglePin}
              className={cn(
                'text-muted-foreground hover:text-foreground w-full h-8 gap-2',
              )}
            >
              {pinned ? (
                <>
                  <ChevronsLeft className="h-4 w-4 shrink-0" />
                  <span className="text-xs whitespace-nowrap">Daralt</span>
                </>
              ) : (
                <>
                  <ChevronsRight className="h-4 w-4 shrink-0" />
                  <span className="text-xs whitespace-nowrap">Sabitle</span>
                </>
              )}
            </Button>
          ) : (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={togglePin}
                  className="text-muted-foreground hover:text-foreground h-8 w-9 p-0 mx-auto flex"
                >
                  <ChevronsRight className="h-4 w-4 shrink-0" />
                </Button>
              </TooltipTrigger>
              <TooltipContent side="right" sideOffset={10} className="text-xs">
                Genişlet
              </TooltipContent>
            </Tooltip>
          )}
        </div>
      </aside>
    </TooltipProvider>
  );
}
