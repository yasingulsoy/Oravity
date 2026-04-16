import {
  LayoutDashboard, Users, Calendar, CreditCard,
  BarChart3, ClipboardList, CalendarPlus, Stethoscope, Tag, Layers, Settings,
  type LucideIcon,
} from 'lucide-react';

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
}

export const navItems: NavItem[] = [
  { to: '/dashboard',        label: 'Dashboard',       icon: LayoutDashboard },
  { to: '/doctor',           label: 'Hekim Ekranı',    icon: Stethoscope },
  { to: '/patients',         label: 'Hastalar',        icon: Users },
  { to: '/appointments',     label: 'Randevular',      icon: Calendar },
  { to: '/treatments',       label: 'Tedaviler',       icon: ClipboardList },
  { to: '/finance',          label: 'Finans',          icon: CreditCard },
  { to: '/reports',          label: 'Raporlar',        icon: BarChart3 },
  { to: '/booking-requests', label: 'Online Talepler', icon: CalendarPlus },
  { to: '/catalog',          label: 'Tedavi Kataloğu', icon: Layers },
  { to: '/pricing',          label: 'Fiyatlandırma',   icon: Tag },
  { to: '/settings',         label: 'Ayarlar',         icon: Settings },
];
