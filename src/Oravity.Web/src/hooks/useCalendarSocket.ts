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
    if (payload && payload.exp > nowSec + 30) return accessToken;
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

    let active = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', { accessTokenFactory: getFreshToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      // Suppress SignalR's own console output — we handle errors ourselves
      .configureLogging(signalR.LogLevel.None)
      .build();

    // Backend sends "CalendarUpdated" (appointment created/moved/status-changed/cancelled)
    connection.on('CalendarUpdated', (event: CalendarEvent) => {
      onEventRef.current(event);
    });
    // Legacy alias — kept for backwards compatibility
    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEventRef.current(event);
    });
    connection.on('VisitUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });
    connection.on('ProtocolUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });

    connectionRef.current = connection;

    // Async start: if React StrictMode unmounts before start() completes,
    // we let start() finish naturally then stop — avoiding "stopped during negotiation".
    const connect = async () => {
      try {
        await connection.start();
        if (!active) {
          // Effect was cleaned up while we were connecting — stop cleanly now
          await connection.stop();
        }
      } catch {
        // Either connect failed or was stopped during negotiation — ignore
      }
    };

    connect();

    return () => {
      active = false;
      connectionRef.current = null;
      // Only stop if fully connected; if still Connecting, connect() handles it above
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.stop();
      }
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return connectionRef;
}
