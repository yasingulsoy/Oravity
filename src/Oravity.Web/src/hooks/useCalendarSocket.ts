import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';

interface CalendarEvent {
  type: 'created' | 'updated' | 'deleted';
  appointmentId: string;
}

export function useCalendarSocket(onEvent: (event: CalendarEvent) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const accessToken = useAuthStore((s) => s.accessToken);

  useEffect(() => {
    if (!accessToken) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEvent(event);
    });

    connection
      .start()
      .catch((err) => console.warn('SignalR bağlantısı kurulamadı (backend çalışıyor mu?):', err.message));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [accessToken, onEvent]);

  return connectionRef;
}
