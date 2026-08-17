import type { PurchaseOrder, ReservationResult, WarehouseValue } from "./types";

const defaultHeaders: HeadersInit = {
  "Content-Type": "application/json",
  "X-User-Id": "demo-user",
  "X-User-Name": "Demo Operator"
};

async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(url, {
    ...options,
    headers: {
      ...defaultHeaders,
      ...(options.headers ?? {})
    }
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? `Request failed (${response.status})`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  purchaseOrders: (warehouseId: number) =>
    request<PurchaseOrder[]>(`/api/purchase-orders?warehouseId=${warehouseId}`),

  reserve: (lineId: number, quantity: number) =>
    request<ReservationResult>("/api/reservations", {
      method: "POST",
      body: JSON.stringify({ purchaseOrderLineId: lineId, quantity })
    }),

  release: (reservationId: number, quantity: number) =>
    request<ReservationResult>(`/api/reservations/${reservationId}/release`, {
      method: "POST",
      body: JSON.stringify({ quantity })
    }),

  finance: () => request<WarehouseValue[]>("/api/finance/committed-stock-value")
};
