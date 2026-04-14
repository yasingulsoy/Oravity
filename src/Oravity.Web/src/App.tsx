import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Toaster } from '@/components/ui/sonner';
import { Layout } from '@/components/layout/Layout';
import { PrivateRoute } from '@/components/shared/PrivateRoute';
import { LoginPage } from '@/pages/auth/LoginPage';
import { DashboardPage } from '@/pages/dashboard/DashboardPage';
import { PatientListPage } from '@/pages/patients/PatientListPage';
import { PatientDetailPage } from '@/pages/patients/PatientDetailPage';
import { AppointmentCalendarPage } from '@/pages/appointments/AppointmentCalendarPage';
import { FinancePage } from '@/pages/finance/FinancePage';
import { ReportsPage } from '@/pages/reports/ReportsPage';
import { TreatmentPlansPage } from '@/pages/treatments/TreatmentPlansPage';
import { NotificationsPage } from '@/pages/notifications/NotificationsPage';
import { BookingRequestsPage } from '@/pages/bookings/BookingRequestsPage';
import { DoctorDashboardPage } from '@/pages/doctor/DoctorDashboardPage';
import { ExaminationPage } from '@/pages/doctor/ExaminationPage';
import { PricingPage } from '@/pages/pricing/PricingPage';
import { TreatmentCatalogPage } from '@/pages/treatments/TreatmentCatalogPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<LoginPage />} />

          <Route element={<PrivateRoute />}>
            <Route element={<Layout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/doctor" element={<DoctorDashboardPage />} />
              <Route path="/muayene/:publicId" element={<ExaminationPage />} />
              <Route path="/patients" element={<PatientListPage />} />
              <Route path="/patients/:id" element={<PatientDetailPage />} />
              <Route path="/appointments" element={<AppointmentCalendarPage />} />
              <Route path="/treatments" element={<TreatmentPlansPage />} />
              <Route path="/finance" element={<FinancePage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/booking-requests" element={<BookingRequestsPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />
              <Route path="/pricing" element={<PricingPage />} />
              <Route path="/catalog" element={<TreatmentCatalogPage />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
      <Toaster />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}

export default App;
