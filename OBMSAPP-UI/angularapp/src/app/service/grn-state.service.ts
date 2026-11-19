import { Injectable } from '@angular/core';

export interface GrnLineState {
  POID:         number;
  WarehouseID:  number;
  GRNDate:      any;
  DeliveryNoteRef:   string;
  SupplierInvoiceNo: string;
  Remarks:      string;
  grnNumber:    string;
  lines:        any[];
}

export interface SerialReturnState {
  lineIndex:    number;
  serialNos:    string[];
  receivingQty: number;
  partial:      boolean;
}

@Injectable({ providedIn: 'root' })
export class GrnStateService {

  private _grnState: GrnLineState | null = null;
  private _serialReturn: SerialReturnState | null = null;

  // ── Save full GRN form state before navigating away ─────────────────────
  saveGrnState(state: GrnLineState): void {
    this._grnState = state;
  }

  getGrnState(): GrnLineState | null {
    return this._grnState;
  }

  clearGrnState(): void {
    this._grnState = null;
  }

  // ── Save serial return data from Serial Entry page ───────────────────────
  saveSerialReturn(data: SerialReturnState): void {
    this._serialReturn = data;
  }

  getSerialReturn(): SerialReturnState | null {
    return this._serialReturn;
  }

  clearSerialReturn(): void {
    this._serialReturn = null;
  }
}
