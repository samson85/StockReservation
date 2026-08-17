export type TrackingType = "Unit" | "Weight";

export interface Reservation {
  id: number;
  remainingQuantity: number;
  unitCostSnapshot: number;
  createdAt: string;
  createdByUserId: string;
}

export interface PurchaseOrderLine {
  id: number;
  itemId: number;
  sku: string;
  itemName: string;
  trackingType: TrackingType;
  ordered: number;
  reserved: number;
  outstanding: number;
  availableStock: number;
  reservations: Reservation[];
}

export interface PurchaseOrder {
  id: number;
  number: string;
  warehouseId: number;
  warehouseName: string;
  lines: PurchaseOrderLine[];
}

export interface ReservationResult {
  reservationId: number;
  purchaseOrderLineId: number;
  quantityChanged: number;
  remainingToReserve: number;
  reservationRemaining: number;
  availableStock: number;
  costSnapshot: number;
}

export interface WarehouseValue {
  warehouseId: number;
  warehouseName: string;
  committedValue: number;
}
