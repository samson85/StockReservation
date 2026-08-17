import { useCallback, useEffect, useState } from "react";
import { QuantityDialog } from "./components/QuantityDialog";
import { api } from "./api";
import type { PurchaseOrder, PurchaseOrderLine, Reservation, WarehouseValue } from "./types";
import "./styles.css";

type Action =
  | { type: "reserve"; line: PurchaseOrderLine }
  | { type: "release"; line: PurchaseOrderLine; reservation: Reservation };

function formatQuantity(value: number) {
  return value.toFixed(3).replace(/\.?(0+)$/, "");
}

function App() {
  const [warehouseId, setWarehouseId] = useState(1);
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [finance, setFinance] = useState<WarehouseValue[]>([]);
  const [tab, setTab] = useState<"reservations" | "finance">("reservations");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [action, setAction] = useState<Action | null>(null);

  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      setError("");
      setOrders(await api.purchaseOrders(warehouseId));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not load purchase orders");
    } finally {
      setLoading(false);
    }
  }, [warehouseId]);

  const loadFinance = useCallback(async () => {
    setLoading(true);
    try {
      setError("");
      setFinance(await api.finance());
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not load finance report");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void loadOrders(); }, [loadOrders]);
  useEffect(() => { if (tab === "finance") void loadFinance(); }, [tab, loadFinance]);

  async function confirmAction(quantity: number) {
    if (!action) return;

    try {
      setError("");
      if (action.type === "reserve") {
        await api.reserve(action.line.id, quantity);
      } else {
        await api.release(action.reservation.id, quantity);
      }

      setAction(null);
      await loadOrders();
      if (tab === "finance") await loadFinance();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Operation failed");
    }
  }

  const dialogMax = action?.type === "reserve"
    ? Math.min(action.line.outstanding, action.line.availableStock)
    : action?.reservation.remainingQuantity ?? 0;

  const dialogStep = action?.type === "reserve" && action.line.trackingType === "Weight"
    ? 0.001
    : action?.type === "release" && action.line.trackingType === "Weight"
      ? 0.001
      : 1;

  return (
    <div className="app">
      <header>
        <div>
          <h1>Stock Reservation</h1>
          <p>Warehouse stock allocation against approved purchase orders</p>
        </div>
        <label>
          Warehouse
          <select value={warehouseId} onChange={e => setWarehouseId(Number(e.target.value))}>
            <option value={1}>Warehouse A</option>
            <option value={2}>Warehouse B</option>
          </select>
        </label>
      </header>

      <nav>
        <button className={tab === "reservations" ? "active" : ""} onClick={() => setTab("reservations")}>Purchase Orders</button>
        <button className={tab === "finance" ? "active" : ""} onClick={() => setTab("finance")}>Finance</button>
      </nav>

      {error && <div className="error">{error}</div>}
      {loading && <div className="loading">Loading…</div>}

      {tab === "reservations" ? (
        <main>
          {!loading && orders.length === 0 && (
            <div className="empty">No approved purchase orders with outstanding lines.</div>
          )}

          {orders.map(order => (
            <section className="card" key={order.id}>
              <div className="card-title">
                <div>
                  <h2>{order.number}</h2>
                  <span>{order.warehouseName}</span>
                </div>
                <span className="status">Approved</span>
              </div>

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Item</th>
                      <th>Tracking</th>
                      <th>Ordered</th>
                      <th>Reserved</th>
                      <th>Outstanding</th>
                      <th>Available</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {order.lines.map(line => (
                      <tr key={line.id}>
                        <td>
                          <strong>{line.sku}</strong><br />
                          {line.itemName}
                          {line.reservations.length > 0 && (
                            <div className="reservation-list">
                              {line.reservations.map(reservation => (
                                <div className="reservation" key={reservation.id}>
                                  <span>
                                    Reservation #{reservation.id}: {formatQuantity(reservation.remainingQuantity)}
                                  </span>
                                  <button
                                    className="link-button"
                                    onClick={() => setAction({ type: "release", line, reservation })}
                                  >
                                    Release
                                  </button>
                                </div>
                              ))}
                            </div>
                          )}
                        </td>
                        <td>{line.trackingType}</td>
                        <td>{formatQuantity(line.ordered)}</td>
                        <td>{formatQuantity(line.reserved)}</td>
                        <td>{formatQuantity(line.outstanding)}</td>
                        <td>{formatQuantity(line.availableStock)}</td>
                        <td>
                          <button
                            disabled={line.outstanding <= 0 || line.availableStock <= 0}
                            onClick={() => setAction({ type: "reserve", line })}
                          >
                            Reserve
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          ))}
        </main>
      ) : (
        <main>
          <div className="summary-grid">
            {finance.map(value => (
              <div className="metric" key={value.warehouseId}>
                <span>{value.warehouseName}</span>
                <strong>${value.committedValue.toFixed(2)}</strong>
                <small>Current committed stock value</small>
              </div>
            ))}
          </div>
          <div className="card">
            <h2>Valuation basis</h2>
            <p>Committed value uses each reservation&apos;s standard-cost snapshot from the time the reservation was created.</p>
          </div>
        </main>
      )}

      <QuantityDialog
        open={action !== null}
        title={action?.type === "reserve" ? "Reserve stock" : "Release reservation"}
        max={dialogMax}
        step={dialogStep}
        actionLabel={action?.type === "reserve" ? "Reserve" : "Release"}
        onCancel={() => setAction(null)}
        onConfirm={quantity => void confirmAction(quantity)}
      />
    </div>
  );
}

export default App;
