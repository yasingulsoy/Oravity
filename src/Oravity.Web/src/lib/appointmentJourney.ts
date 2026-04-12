import {
  CalendarDays, PhoneCall, UserCheck, Stethoscope, CheckCheck,
  LogOut, Ban, XCircle, Clock, ClipboardList,
  type LucideIcon,
} from 'lucide-react';

export interface JourneyStep {
  icon: LucideIcon;
  label: string;
  color: string; // Tailwind text color class
}

/** Randevu statusId → yolculuk adımı */
export function getAppointmentStep(statusId: number): JourneyStep {
  switch (statusId) {
    case 1:  return { icon: CalendarDays, label: 'Planlandı',   color: 'text-slate-500'   };
    case 2:  return { icon: PhoneCall,    label: 'Onaylandı',   color: 'text-blue-500'    };
    case 3:  return { icon: UserCheck,    label: 'Geldi',       color: 'text-amber-500'   };
    case 4:  return { icon: LogOut,       label: 'Ayrıldı',     color: 'text-slate-400'   };
    case 5:  return { icon: Stethoscope,  label: 'Odada',       color: 'text-emerald-500' };
    case 6:  return { icon: Ban,          label: 'İptal',       color: 'text-red-500'     };
    case 7:  return { icon: CheckCheck,   label: 'Tamamlandı',  color: 'text-emerald-600' };
    case 8:  return { icon: XCircle,      label: 'Gelmedi',     color: 'text-orange-500'  };
    default: return { icon: CalendarDays, label: 'Bilinmiyor',  color: 'text-muted-foreground' };
  }
}

/** Vizit (bekleme listesi) statusu → yolculuk adımı */
export function getVisitStep(visitStatus: number): JourneyStep {
  switch (visitStatus) {
    case 1:  return { icon: Clock,       label: 'Bekliyor',       color: 'text-amber-500'   };
    case 2:  return { icon: ClipboardList, label: 'Protokol Açık', color: 'text-blue-500'    };
    case 3:  return { icon: CheckCheck,  label: 'Tamamlandı',     color: 'text-emerald-600' };
    case 4:  return { icon: Ban,         label: 'İptal',          color: 'text-red-500'     };
    default: return { icon: Clock,       label: 'Bekliyor',       color: 'text-muted-foreground' };
  }
}
