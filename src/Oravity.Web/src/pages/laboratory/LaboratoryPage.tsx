import { useState } from 'react';
import { FlaskConical, ClipboardList } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { LaboratoriesTab } from './components/LaboratoriesTab';
import { LaboratoryWorksTab } from './components/LaboratoryWorksTab';

export function LaboratoryPage() {
  const [tab, setTab] = useState('labs');

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Laboratuvar</h1>
        <p className="text-muted-foreground">
          Dış laboratuvarlar, fiyat listeleri ve iş emirlerini yönetin
        </p>
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="labs">
            <FlaskConical className="mr-2 h-4 w-4" />
            Laboratuvarlar
          </TabsTrigger>
          <TabsTrigger value="works">
            <ClipboardList className="mr-2 h-4 w-4" />
            İş Emirleri
          </TabsTrigger>
        </TabsList>

        <TabsContent value="labs" className="mt-4">
          <LaboratoriesTab />
        </TabsContent>
        <TabsContent value="works" className="mt-4">
          <LaboratoryWorksTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
