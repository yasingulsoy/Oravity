import { useState } from 'react';
import { FlaskConical, ClipboardList, ShieldCheck } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { LaboratoriesTab } from './components/LaboratoriesTab';
import { LaboratoryWorksTab } from './components/LaboratoryWorksTab';
import { ApprovalAuthoritiesTab } from './components/ApprovalAuthoritiesTab';

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
          <TabsTrigger value="approvers">
            <ShieldCheck className="mr-2 h-4 w-4" />
            Onay Yetkilileri
          </TabsTrigger>
        </TabsList>

        <TabsContent value="labs" className="mt-4">
          <LaboratoriesTab />
        </TabsContent>
        <TabsContent value="works" className="mt-4">
          <LaboratoryWorksTab />
        </TabsContent>
        <TabsContent value="approvers" className="mt-4">
          <ApprovalAuthoritiesTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
