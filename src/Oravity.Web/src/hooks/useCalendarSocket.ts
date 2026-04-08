import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';

interface CalendarEvent {
  type: 'created' | 'updated' | 'deleted';
  appointmentId: string;
}

export function useCalendarSocket(onEvent: (event: CalendarEvent) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    const { accessToken } = useAuthStore.getState();
    if (!accessToken) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', {
        // Always read the latest token from the store — handles token refresh transparently
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEventRef.current(event);
    });

    connection
      .start()
      .catch((err) => console.warn('SignalR bağlantısı kurulamadı (backend çalışıyor mu?):', err.message));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return connectionRef;
}
