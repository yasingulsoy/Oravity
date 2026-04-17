import {
  LayoutDashboard, Users, Calendar, CreditCard,
  BarChart3, ClipboardList, CalendarPlus, Stethoscope, Tag, Layers, Settings,
  FlaskConical,
  type LucideIcon,
} from 'lucide-react';

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
}

export interface NavSection {
  label: string;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    label: 'Ana',
    items: [
      { to: '/dashboard', label: 'Dashboard',    icon: LayoutDashboard },
      { to: '/doctor',    label: 'Hekim Ekranı', icon: Stethoscope },
    ],
  },
  {
    label: 'Hasta Yönetimi',
    items: [
      { to: '/patients',         label: 'Hastalar',        icon: Users },
      { to: '/appointments',     label: 'Randevular',      icon: Calendar },
      { to: '/treatments',       label: 'Tedaviler',       icon: ClipboardList },
      { to: '/booking-requests', label: 'Online Talepler', icon: CalendarPlus },
    ],
  },
  {
    label: 'Finans & Raporlama',
    items: [
      { to: '/finance', label: 'Finans',   icon: CreditCard },
      { to: '/reports', label: 'Raporlar', icon: BarChart3 },
    ],
  },
  {
    label: 'Katalog',
    items: [
      { to: '/catalog',    label: 'Tedavi Kataloğu', icon: Layers },
      { to: '/pricing',    label: 'Fiyatlandırma',   icon: Tag },
      { to: '/laboratory', label: 'Laboratuvar',     icon: FlaskConical },
    ],
  },
  {
    label: 'Sistem',
    items: [
      { to: '/settings', label: 'Ayarlar', icon: Settings },
    ],
  },
];

/** Geriye dönük uyumluluk — Header (navbar modu) düz listeyi kullanıyor. */
export const navItems: NavItem[] = navSections.flatMap((s) => s.items);
