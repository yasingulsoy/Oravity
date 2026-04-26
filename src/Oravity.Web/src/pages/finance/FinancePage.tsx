import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { RevenueTab } from './tabs/RevenueTab';
import { PendingCommissionsTab } from './tabs/PendingCommissionsTab';
import { CommissionDistributionsTab } from './tabs/CommissionDistributionsTab';
import { DoctorAccountsTab } from './tabs/DoctorAccountsTab';
import { InstitutionInvoicesTab } from './tabs/InstitutionInvoicesTab';
import { DailyCashTab } from './tabs/DailyCashTab';

export function FinancePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Finans</h1>
        <p className="text-muted-foreground">Gelir, hakediş ve kurum fatura takibi</p>
      </div>

      <Tabs defaultValue="cash">
        <TabsList>
          <TabsTrigger value="cash">Günlük Kasa</TabsTrigger>
          <TabsTrigger value="revenue">Gelir</TabsTrigger>
          <TabsTrigger value="pending">Hekim Hakkı</TabsTrigger>
          <TabsTrigger value="distributions">Hakediş Dağıtımları</TabsTrigger>
          <TabsTrigger value="doctors">Hekim Cari</TabsTrigger>
          <TabsTrigger value="institutions">Kurum Fatura</TabsTrigger>
        </TabsList>

        <TabsContent value="cash" className="pt-4">
          <DailyCashTab />
        </TabsContent>

        <TabsContent value="revenue" className="pt-4">
          <RevenueTab />
        </TabsContent>

        <TabsContent value="pending" className="pt-4">
          <PendingCommissionsTab />
        </TabsContent>

        <TabsContent value="distributions" className="pt-4">
          <CommissionDistributionsTab />
        </TabsContent>

        <TabsContent value="doctors" className="pt-4">
          <DoctorAccountsTab />
        </TabsContent>

        <TabsContent value="institutions" className="pt-4">
          <InstitutionInvoicesTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
