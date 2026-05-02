import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import { useAuthStore } from '@/store/authStore';
import { parseJwt } from '@/lib/jwt';

interface CalendarEvent {
  type: 'created' | 'updated' | 'deleted';
  appointmentId: string;
}

export interface PatientCalledEvent {
  patientName: string;
  doctorName: string;
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

export function useCalendarSocket(
  onEvent: (event: CalendarEvent) => void,
  onPatientCalled?: (event: PatientCalledEvent) => void,
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;
  const onPatientCalledRef = useRef(onPatientCalled);
  onPatientCalledRef.current = onPatientCalled;

  useEffect(() => {
    const { accessToken, user } = useAuthStore.getState();
    if (!accessToken) return;

    const branchId = user?.branchId ?? null;
    let active = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/calendar', { accessTokenFactory: getFreshToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.None)
      .build();

    connection.on('CalendarUpdated', (event: CalendarEvent) => {
      onEventRef.current(event);
    });
    connection.on('AppointmentChanged', (event: CalendarEvent) => {
      onEventRef.current(event);
    });
    connection.on('VisitUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });
    connection.on('ProtocolUpdated', () => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
    });
    connection.on('PatientCalled', (event: PatientCalledEvent) => {
      onEventRef.current({ type: 'updated', appointmentId: '' });
      onPatientCalledRef.current?.(event);
    });

    connectionRef.current = connection;

    const connect = async () => {
      try {
        await connection.start();
        if (!active) {
          await connection.stop();
          return;
        }
        // Branch grubuna katıl — olmasaydı hiçbir broadcast gelmezdi
        if (branchId) {
          await connection.invoke('JoinCalendar', branchId);
        }
      } catch {
        // Connect failed or stopped during negotiation — ignore
      }
    };

    connect();

    return () => {
      active = false;
      connectionRef.current = null;
      if (connection.state === signalR.HubConnectionState.Connected) {
        if (branchId) {
          connection.invoke('LeaveCalendar', branchId).catch(() => null);
        }
        connection.stop();
      }
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return connectionRef;
}
