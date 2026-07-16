import { useEffect, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { GeolocationState } from "../hooks/useGeolocation";

// Fix default marker icon path (Webpack/Vite issue with leaflet assets)
import iconUrl from "leaflet/dist/images/marker-icon.png";
import iconRetinaUrl from "leaflet/dist/images/marker-icon-2x.png";
import shadowUrl from "leaflet/dist/images/marker-shadow.png";

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({ iconUrl, iconRetinaUrl, shadowUrl });

const playerIcon = new L.Icon({
  iconUrl: "data:image/svg+xml," + encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 28 28">
      <circle cx="14" cy="14" r="12" fill="#2196F3" stroke="white" stroke-width="3"/>
      <circle cx="14" cy="14" r="4" fill="white"/>
    </svg>`
  ),
  iconSize: [28, 28],
  iconAnchor: [14, 14],
  popupAnchor: [0, -14],
});

const treasureIcon = new L.Icon({
  iconUrl: "data:image/svg+xml," + encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 36 36">
      <text x="18" y="28" font-size="28" text-anchor="middle">💰</text>
    </svg>`
  ),
  iconSize: [36, 36],
  iconAnchor: [18, 18],
  popupAnchor: [0, -18],
});

interface TreasureMapProps {
  player: GeolocationState;
  treasureLat: number;
  treasureLng: number;
  treasureName?: string;
  minimap?: boolean;
}

function MapBoundsUpdater({
  player,
  treasureLat,
  treasureLng,
}: {
  player: GeolocationState;
  treasureLat: number;
  treasureLng: number;
}) {
  const map = useMap();

  useEffect(() => {
    if (player.latitude !== null && player.longitude !== null) {
      const playerLatLng = L.latLng(player.latitude, player.longitude);
      const treasureLatLng = L.latLng(treasureLat, treasureLng);
      const bounds = L.latLngBounds([playerLatLng, treasureLatLng]);
      map.fitBounds(bounds, { padding: [50, 50], maxZoom: 17 });
    }
  }, [player.latitude, player.longitude, treasureLat, treasureLng, map]);

  return null;
}

function DistanceIndicator({
  player,
  treasureLat,
  treasureLng,
}: {
  player: GeolocationState;
  treasureLat: number;
  treasureLng: number;
}) {
  const [dist, setDist] = useState<number | null>(null);

  useEffect(() => {
    if (player.latitude !== null && player.longitude !== null) {
      const d = L.latLng(player.latitude, player.longitude).distanceTo(
        L.latLng(treasureLat, treasureLng)
      );
      setDist(Math.round(d));
    }
  }, [player.latitude, player.longitude, treasureLat, treasureLng]);

  if (dist === null) return null;

  const label = dist >= 1000
    ? `${(dist / 1000).toFixed(1)} km`
    : `${dist} m`;

  return (
    <div className="treasure-distance-badge">
      🎯 {label}
    </div>
  );
}

export function TreasureMap({
  player,
  treasureLat,
  treasureLng,
  treasureName,
  minimap = true,
}: TreasureMapProps) {
  const [collapsed, setCollapsed] = useState(minimap);

  if (player.error && !player.loading) {
    return (
      <div className="treasure-map-error">
        <span>📍 {player.error}</span>
      </div>
    );
  }

  if (player.loading && player.latitude === null) {
    return (
      <div className="treasure-map-loading">
        <div className="treasure-map-spinner" />
        <span>Obteniendo ubicación GPS...</span>
      </div>
    );
  }

  const centerLat = player.latitude ?? treasureLat;
  const centerLng = player.longitude ?? treasureLng;

  return (
    <div className={`treasure-map-wrapper ${collapsed ? "treasure-map-collapsed" : ""}`}>
      <button
        className="treasure-map-toggle"
        onClick={() => setCollapsed(!collapsed)}
        aria-label={collapsed ? "Expandir mapa" : "Colapsar mapa"}
      >
        {collapsed ? "🗺️" : "✕"}
      </button>

      {!collapsed && (
        <>
          <DistanceIndicator
            player={player}
            treasureLat={treasureLat}
            treasureLng={treasureLng}
          />

          <div className="treasure-map-container">
            <MapContainer
              center={[centerLat, centerLng]}
              zoom={15}
              scrollWheelZoom={true}
              style={{ width: "100%", height: "100%" }}
            >
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />

              {player.latitude !== null && player.longitude !== null && (
                <Marker
                  position={[player.latitude, player.longitude]}
                  icon={playerIcon}
                >
                  <Popup>Tu ubicación actual</Popup>
                </Marker>
              )}

              <Marker
                position={[treasureLat, treasureLng]}
                icon={treasureIcon}
              >
                <Popup>{treasureName ?? "Tesoro"}</Popup>
              </Marker>

              <MapBoundsUpdater
                player={player}
                treasureLat={treasureLat}
                treasureLng={treasureLng}
              />
            </MapContainer>
          </div>
        </>
      )}

      {collapsed && player.latitude !== null && (
        <div className="treasure-map-collapsed-info">
          <DistanceIndicator
            player={player}
            treasureLat={treasureLat}
            treasureLng={treasureLng}
          />
        </div>
      )}
    </div>
  );
}
