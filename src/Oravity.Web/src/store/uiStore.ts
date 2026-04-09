import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/lib/constants';

interface UiState {
  theme: 'light' | 'dark' | 'system';
  setTheme: (theme: 'light' | 'dark' | 'system') => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      theme: 'light',

      setTheme: (theme) => {
        const root = document.documentElement;
        root.classList.remove('light', 'dark');
        if (theme === 'system') {
          const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
          root.classList.add(prefersDark ? 'dark' : 'light');
        } else {
          root.classList.add(theme);
        }
        set({ theme });
      },
    }),
    {
      name: STORAGE_KEYS.UI,
      partialize: (state) => ({ theme: state.theme }),
    },
  ),
);
