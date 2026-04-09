import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';

interface CalendarEvent {
  type: 'created' | 'updated' | 'deleted';
  appointmentId: string;
}

async function getFreshToken(): Promise<string> {
  const { accessToken, refreshToken, setTokens, logout } = useAuthStore.getState();

  if (accessToken) {
    const payload = parseJwt(accessToken);
    const nowSec = Math.floor(Date.now() / 1000);
    if (payload && payload.exp > nowSec + 30) {
      return accessToken;
    }
  }

  if (!refreshToken) {
    logout();
    window.location.href = '/';
    return '';
  }

  try {
    const { data } = await axios.post('/api/auth/refresh', { refreshToken });
    setTokens(data.accessToken, data.refreshToken);
    return data.accessToken as string;
  } catch {
    logout();
    window.location.href = '/';
    return '';
  }
}

export function useCalendarSocket(onEvent: (event: CalendarEvent) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    const { accessToken } = useAuthStore.getState();
    if (!accessToken) return;

    // React StrictMode'da cleanup bazen negotiation sırasında çağrılır.
    // dismounted flag ile bağlantı başlamadan önce iptal istenirse stop çağırmıyoruz.
    let dismounted = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', { accessTokenFactory: getFreshToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEventRef.current(event);
    });

    // VisitUpdated / ProtocolUpdated events (backend broadcasts these)
    connection.on('VisitUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });
    connection.on('ProtocolUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });

    connectionRef.current = connection;

    connection.start().catch((err) => {
      if (!dismounted) {
        console.warn('SignalR bağlanamadı:', err.message);
      }
    });

    return () => {
      dismounted = true;
      // Yalnızca bağlı veya bağlanıyor durumdaysa durdur
      if (
        connection.state !== signalR.HubConnectionState.Disconnected &&
        connection.state !== signalR.HubConnectionState.Disconnecting
      ) {
        connection.stop();
      }
      connectionRef.current = null;
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return connectionRef;
}
