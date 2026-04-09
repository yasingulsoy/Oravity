import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';

interface CalendarEvent {
  type: 'created' | 'updated' | 'deleted';
  appointmentId: string;
}

/** Returns a valid (non-expired) access token, refreshing if needed. */
async function getFreshToken(): Promise<string> {
  const { accessToken, refreshToken, setTokens, logout } = useAuthStore.getState();

  // Check if current token is still valid (with 30s buffer)
  if (accessToken) {
    const payload = parseJwt(accessToken);
    const nowSec = Math.floor(Date.now() / 1000);
    if (payload && payload.exp > nowSec + 30) {
      return accessToken;
    }
  }

  // Token expired or near-expiry — try refresh
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

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', {
        accessTokenFactory: getFreshToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEventRef.current(event);
    });

    let stopped = false;
    connection
      .start()
      .catch((err) => {
        if (!stopped) {
          console.warn('SignalR bağlantısı kurulamadı:', err.message);
        }
      });

    return () => {
      stopped = true;
      connection.stop();
    };

    connectionRef.current = connection;
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return connectionRef;
}
