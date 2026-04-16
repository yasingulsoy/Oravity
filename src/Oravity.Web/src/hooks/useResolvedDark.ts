import { useSyncExternalStore } from 'react';
import { useUiStore } from '@/store/uiStore';

function getSystemPrefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/**
 * Gerçek görünen tema (light/dark). `system` seçiliyse OS tercihini kullanır.
 * Logolar: 2.png aydınlık, 3.png karanlık — bu hook ile hangi dosyanın gösterileceği seçilir.
 */
export function useResolvedDark(): boolean {
  const theme = useUiStore((s) => s.theme);

  const systemDark = useSyncExternalStore(
    (onChange) => {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      mq.addEventListener('change', onChange);
      return () => mq.removeEventListener('change', onChange);
    },
    getSystemPrefersDark,
    () => false,
  );

  if (theme === 'dark') return true;
  if (theme === 'light') return false;
  return systemDark;
}
