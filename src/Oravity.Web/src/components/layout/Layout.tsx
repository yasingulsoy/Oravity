import { Outlet } from 'react-router-dom';
import { useUiStore } from '@/store/uiStore';
import { Header } from './Header';
import { Sidebar } from './Sidebar';

export function Layout() {
  const layoutMode = useUiStore((s) => s.layoutMode);
  const isSidebar = layoutMode === 'sidebar';

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {isSidebar && <Sidebar />}

      <div className="flex flex-1 flex-col min-w-0">
        <Header />
        <main className="flex-1 overflow-y-auto bg-muted/20 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
