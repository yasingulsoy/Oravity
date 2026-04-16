import { NavLink } from 'react-router-dom';
import {
  LogOut, Moon, Sun, Bell, ChevronDown, PanelLeft, PanelLeftClose,
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { useUiStore } from '@/store/uiStore';
import { useResolvedDark } from '@/hooks/useResolvedDark';
import { useLogout } from '@/hooks/useAuth';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';
import { PatientSearch } from './PatientSearch';
import { navItems } from './nav-items';

export function Header() {
  const user = useAuthStore((s) => s.user);
  const { theme, setTheme, layoutMode, setLayoutMode } = useUiStore();
  const logoutMutation = useLogout();

  const isDark = useResolvedDark();
  const isSidebar = layoutMode === 'sidebar';

  const initials = user
    ? user.name
        .split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
    : '?';

  return (
    <header className="header-glass sticky top-0 z-50 flex h-14 items-center border-b border-border/40 px-4 gap-2 shrink-0">
      {/* Logo — hidden in sidebar mode (logo is in sidebar) */}
      {!isSidebar && (
        <>
          <NavLink to="/dashboard" className="flex items-center gap-2.5 shrink-0 mr-1 group">
            <div className="relative h-8 w-8 overflow-hidden rounded-lg">
              <img
                src="/logos/2.png"
                alt="Oravity"
                className={cn(
                  'absolute inset-0 h-full w-full object-contain p-0.5 transition-opacity duration-300',
                  isDark ? 'opacity-0' : 'opacity-100',
                )}
              />
              <img
                src="/logos/3.png"
                alt="Oravity"
                className={cn(
                  'absolute inset-0 h-full w-full object-contain p-0.5 transition-opacity duration-300',
                  isDark ? 'opacity-100' : 'opacity-0',
                )}
              />
            </div>
            <span className="text-[15px] font-semibold tracking-tight select-none sidebar-logo-shine">
              Oravity
            </span>
          </NavLink>
          <div className="h-5 w-px bg-border/60 mx-1 shrink-0" />
        </>
      )}

      {/* Nav — only in navbar mode */}
      {!isSidebar && (
        <nav className="flex items-center gap-0.5 flex-1 min-w-0 overflow-x-auto scrollbar-none">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'nav-item relative flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-[13px] font-medium whitespace-nowrap transition-all duration-200',
                  isActive
                    ? 'text-foreground bg-accent/80 shadow-sm'
                    : 'text-muted-foreground hover:text-foreground hover:bg-accent/50',
                )
              }
            >
              <Icon className="h-3.5 w-3.5 shrink-0" />
              <span className="hidden xl:inline">{label}</span>
            </NavLink>
          ))}
        </nav>
      )}

      {/* Sidebar modu: hasta araması solda (sidebar bitişi / logo hizası) */}
      {isSidebar && (
        <div className="flex min-w-0 flex-1 items-center pr-2">
          <PatientSearch className="w-full max-w-xl min-w-[12rem]" />
        </div>
      )}

      {/* Sağ: bildirim, tema, kullanıcı — navbar modunda arama da burada */}
      <div className="flex items-center gap-1.5 shrink-0">
        {!isSidebar && <PatientSearch />}

        <div className="h-5 w-px bg-border/60 mx-0.5 shrink-0" />

        {/* Notifications */}
        <NavLink to="/notifications">
          {({ isActive }) => (
            <Button
              variant="ghost"
              size="icon"
              className={cn(
                'h-8 w-8 rounded-lg relative',
                isActive && 'bg-accent/80 text-foreground',
              )}
            >
              <Bell className="h-[15px] w-[15px]" />
            </Button>
          )}
        </NavLink>

        {/* Theme toggle */}
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 rounded-lg"
          onClick={() => setTheme(isDark ? 'light' : 'dark')}
        >
          <Sun
            className={cn(
              'h-[15px] w-[15px] transition-all duration-300',
              isDark
                ? 'rotate-0 scale-100 opacity-100'
                : '-rotate-90 scale-0 opacity-0 absolute',
            )}
          />
          <Moon
            className={cn(
              'h-[15px] w-[15px] transition-all duration-300',
              isDark
                ? 'rotate-90 scale-0 opacity-0 absolute'
                : 'rotate-0 scale-100 opacity-100',
            )}
          />
        </Button>

        {/* User menu */}
        <DropdownMenu>
          <DropdownMenuTrigger className="relative flex h-8 items-center gap-2 rounded-lg pl-1 pr-2 hover:bg-accent/60 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <Avatar className="h-7 w-7 border border-border/60">
              <AvatarFallback className="text-[11px] font-medium bg-primary/10 text-primary">
                {initials}
              </AvatarFallback>
            </Avatar>
            <span className="hidden lg:inline text-[13px] font-medium max-w-[100px] truncate">
              {user?.name?.split(' ')[0]}
            </span>
            <ChevronDown className="h-3 w-3 text-muted-foreground hidden lg:block" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <div className="flex items-center gap-3 p-3">
              <Avatar className="h-9 w-9 border border-border/60">
                <AvatarFallback className="text-xs font-medium bg-primary/10 text-primary">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col min-w-0">
                <p className="text-sm font-medium truncate">{user?.name}</p>
                <p className="text-xs text-muted-foreground truncate">
                  {user?.email}
                </p>
              </div>
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={() => setLayoutMode(isSidebar ? 'navbar' : 'sidebar')}
            >
              {isSidebar ? (
                <>
                  <PanelLeftClose className="mr-2 h-4 w-4" />
                  Navbar menüye dön
                </>
              ) : (
                <>
                  <PanelLeft className="mr-2 h-4 w-4" />
                  Sidebar görünümü
                </>
              )}
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={() => logoutMutation.mutate()}
              className="text-destructive focus:text-destructive"
            >
              <LogOut className="mr-2 h-4 w-4" />
              Çıkış Yap
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
