import { useEffect, useState } from "react";

type Props = {
  open: boolean;
  title: string;
  max: number;
  step: number;
  actionLabel: string;
  onCancel: () => void;
  onConfirm: (quantity: number) => void;
};

export function QuantityDialog({
  open,
  title,
  max,
  step,
  actionLabel,
  onCancel,
  onConfirm
}: Props) {
  const [value, setValue] = useState("");

  useEffect(() => {
    if (open) setValue("");
  }, [open]);

  if (!open) return null;

  const quantity = Number(value);
  const valid = Number.isFinite(quantity) && quantity > 0 && quantity <= max;

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onCancel}>
      <div className="modal" role="dialog" aria-modal="true" aria-labelledby="quantity-title" onMouseDown={e => e.stopPropagation()}>
        <h2 id="quantity-title">{title}</h2>
        <p className="muted">Maximum: {max.toFixed(3).replace(/\.?(0+)$/, "")}</p>
        <label>
          Quantity
          <input
            autoFocus
            type="number"
            min={step}
            max={max}
            step={step}
            value={value}
            onChange={e => setValue(e.target.value)}
          />
        </label>
        <div className="modal-actions">
          <button className="secondary" onClick={onCancel}>Cancel</button>
          <button disabled={!valid} onClick={() => onConfirm(quantity)}>{actionLabel}</button>
        </div>
      </div>
    </div>
  );
}
