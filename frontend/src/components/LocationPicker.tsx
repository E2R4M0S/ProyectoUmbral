import { MapContainer, TileLayer, Marker, Popup, useMapEvents } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

import iconUrl from "leaflet/dist/images/marker-icon.png";
import iconRetinaUrl from "leaflet/dist/images/marker-icon-2x.png";
import shadowUrl from "leaflet/dist/images/marker-shadow.png";

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({ iconUrl, iconRetinaUrl, shadowUrl });

interface LocationPickerProps {
  latitude: string;
  longitude: string;
  onLatChange: (v: string) => void;
  onLngChange: (v: string) => void;
}

function ClickHandler({
  onPick,
}: {
  onPick: (lat: number, lng: number) => void;
}) {
  useMapEvents({
    click(e) {
      onPick(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

export function LocationPicker({
  latitude,
  longitude,
  onLatChange,
  onLngChange,
}: LocationPickerProps) {
  const lat = parseFloat(latitude);
  const lng = parseFloat(longitude);
  const hasCoords = !isNaN(lat) && !isNaN(lng);
  const center: [number, number] = hasCoords ? [lat, lng] : [10.48, -66.9];

  function handlePick(pickedLat: number, pickedLng: number) {
    onLatChange(pickedLat.toFixed(6));
    onLngChange(pickedLng.toFixed(6));
  }

  return (
    <div style={{ marginBottom: "0.75rem" }}>
      <label style={{
        display: "block", fontSize: "0.78rem", fontWeight: 600,
        color: "#aaa", marginBottom: 6, textTransform: "uppercase", letterSpacing: 0.5,
      }}>
        Ubicación en el mapa
      </label>

      <div style={{
        width: "100%",
        height: 200,
        borderRadius: 8,
        overflow: "hidden",
        border: "1px solid #0f3460",
        marginBottom: 8,
      }}>
        <MapContainer
          center={center}
          zoom={hasCoords ? 16 : 13}
          scrollWheelZoom={true}
          style={{ width: "100%", height: "100%" }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <ClickHandler onPick={handlePick} />
          {hasCoords && (
            <Marker position={[lat, lng]}>
              <Popup>Ubicación seleccionada</Popup>
            </Marker>
          )}
        </MapContainer>
      </div>

      <p style={{
        margin: 0, fontSize: "0.78rem", color: "#888",
      }}>
        Hacé clic en el mapa para marcar la ubicación del tesoro
      </p>

      {hasCoords && (
        <p style={{
          margin: "4px 0 0", fontSize: "0.78rem", color: "#4caf50",
        }}>
          🌍 {lat}, {lng}
        </p>
      )}
    </div>
  );
}
