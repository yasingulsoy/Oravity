import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/lib/constants';

export type LayoutMode = 'navbar' | 'sidebar';

interface UiState {
  theme: 'light' | 'dark' | 'system';
  layoutMode: LayoutMode;
  sidebarExpanded: boolean;
  setTheme: (theme: 'light' | 'dark' | 'system') => void;
  setLayoutMode: (mode: LayoutMode) => void;
  toggleSidebarExpanded: () => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      theme: 'light',
      layoutMode: 'navbar',
      sidebarExpanded: false,

      setTheme: (theme) => {
        const root = document.documentElement;
        root.classList.remove('light', 'dark');

        let isDark: boolean;
        if (theme === 'system') {
          isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        } else {
          isDark = theme === 'dark';
        }

        root.classList.add(isDark ? 'dark' : 'light');

        const favicon = document.getElementById('app-favicon') as HTMLLinkElement | null;
        if (favicon) favicon.href = isDark ? '/icos/3.ico' : '/icos/2.ico';

        set({ theme });
      },

      setLayoutMode: (layoutMode) => set({ layoutMode }),
      toggleSidebarExpanded: () => set((s) => ({ sidebarExpanded: !s.sidebarExpanded })),
    }),
    {
      name: STORAGE_KEYS.UI,
      partialize: (state) => ({
        theme: state.theme,
        layoutMode: state.layoutMode,
        sidebarExpanded: state.sidebarExpanded,
      }),
    },
  ),
);
