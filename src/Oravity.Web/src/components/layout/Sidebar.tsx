import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Calendar,
  CreditCard,
  ChevronLeft,
  ChevronRight,
  BarChart3,
  ClipboardList,
  Bell,
  CalendarPlus,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useUiStore } from '@/store/uiStore';
import { Button } from '@/components/ui/button';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/patients', label: 'Hastalar', icon: Users },
  { to: '/appointments', label: 'Randevular', icon: Calendar },
  { to: '/treatments', label: 'Tedaviler', icon: ClipboardList },
  { to: '/finance', label: 'Finans', icon: CreditCard },
  { to: '/reports', label: 'Raporlar', icon: BarChart3 },
  { to: '/booking-requests', label: 'Online Talepler', icon: CalendarPlus },
  { to: '/notifications', label: 'Bildirimler', icon: Bell },
];

export function Sidebar() {
  const sidebarOpen = useUiStore((s) => s.sidebarOpen);
  const toggleSidebar = useUiStore((s) => s.toggleSidebar);

  return (
    <aside
      className={cn(
        'flex h-screen flex-col border-r bg-sidebar text-sidebar-foreground transition-all duration-300',
        sidebarOpen ? 'w-64' : 'w-16',
      )}
    >
      <div className="flex h-14 shrink-0 items-center border-b border-border px-4">
        {sidebarOpen ? (
          <>
            <span className="text-lg font-semibold tracking-tight">Oravity</span>
            <Button
              variant="ghost"
              size="icon"
              className="ml-auto h-8 w-8"
              onClick={toggleSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </>
        ) : (
          <div className="flex w-full flex-col items-center gap-1.5">
            <span className="sidebar-logo-shine text-xl font-light leading-none select-none">
              O
            </span>
            <span className="block h-px w-5 bg-foreground/20" />
            <button
              onClick={toggleSidebar}
              className="flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        )}
      </div>

      <nav className="flex-1 space-y-1 p-2">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground',
                !sidebarOpen && 'justify-center px-2',
              )
            }
          >
            <Icon className="h-5 w-5 shrink-0" />
            {sidebarOpen && <span>{label}</span>}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
