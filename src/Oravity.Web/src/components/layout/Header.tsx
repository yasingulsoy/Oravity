import { NavLink } from 'react-router-dom';
import {
  LogOut, Moon, Sun,
  LayoutDashboard, Users, Calendar, CreditCard,
  BarChart3, ClipboardList, Bell, CalendarPlus, Stethoscope,
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { useUiStore } from '@/store/uiStore';
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

const navItems = [
  { to: '/dashboard',        label: 'Dashboard',      icon: LayoutDashboard },
  { to: '/doctor',           label: 'Hekim Ekranı',   icon: Stethoscope     },
  { to: '/patients',         label: 'Hastalar',        icon: Users           },
  { to: '/appointments',     label: 'Randevular',      icon: Calendar        },
  { to: '/treatments',       label: 'Tedaviler',       icon: ClipboardList   },
  { to: '/finance',          label: 'Finans',          icon: CreditCard      },
  { to: '/reports',          label: 'Raporlar',        icon: BarChart3       },
  { to: '/booking-requests', label: 'Online Talepler', icon: CalendarPlus    },
  { to: '/notifications',    label: 'Bildirimler',     icon: Bell            },
];

export function Header() {
  const user = useAuthStore((s) => s.user);
  const { theme, setTheme } = useUiStore();
  const logoutMutation = useLogout();

  const initials = user
    ? user.name.split(' ').map((n) => n[0]).join('').toUpperCase()
    : '?';

  return (
    <header className="flex h-12 items-center border-b bg-background px-4 gap-4 shrink-0">
      {/* Logo */}
      <span className="text-base font-semibold tracking-tight select-none shrink-0 mr-2">
        Oravity
      </span>

      {/* Nav links */}
      <nav className="flex items-center gap-0.5 flex-1 overflow-x-auto scrollbar-none">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium whitespace-nowrap transition-colors',
                isActive
                  ? 'bg-accent text-accent-foreground'
                  : 'text-muted-foreground hover:bg-accent/60 hover:text-accent-foreground',
              )
            }
          >
            <Icon className="h-4 w-4 shrink-0" />
            <span>{label}</span>
          </NavLink>
        ))}
      </nav>

      {/* Patient search */}
      <PatientSearch />

      {/* Right side */}
      <div className="flex items-center gap-1 shrink-0">
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger className="relative flex h-8 w-8 items-center justify-center rounded-full hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <Avatar className="h-8 w-8">
              <AvatarFallback className="text-xs">{initials}</AvatarFallback>
            </Avatar>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <div className="flex items-center gap-2 p-2">
              <div className="flex flex-col space-y-1">
                <p className="text-sm font-medium">{user?.name}</p>
                <p className="text-xs text-muted-foreground">{user?.email}</p>
              </div>
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={() => logoutMutation.mutate()}
              className="text-destructive"
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
