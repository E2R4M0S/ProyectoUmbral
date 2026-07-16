import { useState, useEffect, useRef } from "react";

export interface GeolocationState {
  latitude: number | null;
  longitude: number | null;
  accuracy: number | null;
  error: string | null;
  loading: boolean;
  supported: boolean;
}

export function useGeolocation(options?: PositionOptions): GeolocationState {
  const [state, setState] = useState<GeolocationState>({
    latitude: null,
    longitude: null,
    accuracy: null,
    error: null,
    loading: true,
    supported: "geolocation" in navigator,
  });

  const watchIdRef = useRef<number | null>(null);

  useEffect(() => {
    if (!state.supported) {
      setState(prev => ({ ...prev, loading: false, error: "GPS no soportado en este navegador" }));
      return;
    }

    const onSuccess: PositionCallback = (pos) => {
      setState({
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        accuracy: pos.coords.accuracy,
        error: null,
        loading: false,
        supported: true,
      });
    };

    const onError: PositionErrorCallback = (err) => {
      const messages: Record<number, string> = {
        1: "Permiso de GPS denegado. Activá la ubicación en tu dispositivo.",
        2: "Señal de GPS no disponible. Salí a un espacio abierto.",
        3: "Esperando señal de GPS...",
      };
      setState(prev => ({
        ...prev,
        error: messages[err.code] ?? "Error de GPS desconocido.",
        loading: err.code === 3,
      }));
    };

    watchIdRef.current = navigator.geolocation.watchPosition(
      onSuccess,
      onError,
      { enableHighAccuracy: true, maximumAge: 5000, timeout: 15000, ...options },
    );

    return () => {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current);
      }
    };
  }, []);

  return state;
}
