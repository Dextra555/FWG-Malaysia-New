import { Injectable } from '@angular/core';
import { environment } from "../../environments/environment";
import { HttpClient, HttpErrorResponse, HttpParams } from "@angular/common/http";
import { catchError, Observable, throwError } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  apiUrl: string = environment.baseUrl;

  constructor(private httpClient: HttpClient) {
  }

  saveCategory(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateCategory', body);
  }

  getCategories() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetInventoryCategories'
    ).pipe(catchError(this.errorHandle));
  }

  getCategoryByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetInventoryCategoryByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getCategoryByCat(cat: string) {
    const params = { params: new HttpParams({ fromString: "?cat=" + cat }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetCategoriesByCat', params
    ).pipe(catchError(this.errorHandle));
  }

  // Brand Master
  getBrandMasters() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetBrandMasters'
    ).pipe(catchError(this.errorHandle));
  }

  getBrandByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetBrandByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getBrandCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetBrandCode'
    ).pipe(catchError(this.errorHandle));
  }

  saveBrand(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateBrandMaster', body).pipe(catchError(this.errorHandle));
  }

  deleteBrand(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteBrand/${id}`,
      null
    );
  }

  // UOM Master
  getUOMMasters() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUOMMasters'
    ).pipe(catchError(this.errorHandle));
  }

  getUOMByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUOMByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getUOMCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUOMCode'
    ).pipe(catchError(this.errorHandle));
  }

  saveUOM(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateUOMMaster', body).pipe(catchError(this.errorHandle));
  }

  deleteUOM(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteUOM/${id}`,
      null
    );
  }

  // Warehouse Master
  getWarehouseMasters() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetWarehouseMasters'
    ).pipe(catchError(this.errorHandle));
  }

  getWarehouseByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetWarehouseByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getWarehouseCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetWarehouseCode'
    ).pipe(catchError(this.errorHandle));
  }

  saveWarehouse(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateWarehouseMaster', body).pipe(catchError(this.errorHandle));
  }

  deleteWarehouse(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteWarehouse/${id}`,
      null
    );
  }

  saveItem(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateItem', body).pipe(catchError(this.errorHandle));
  }

  getItemCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetItemCode'
    ).pipe(catchError(this.errorHandle));
  }

  getItems() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetInventoryItems'
    ).pipe(catchError(this.errorHandle));
  }

  getItemByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetInventoryItemByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getBranchList() {
    return this.httpClient.get<any>(this.apiUrl + 'Master/GetBranchMasterAll',
    ).pipe(catchError(this.errorHandle));
  }

  getBranchListByUser(userName: string) {
    return this.httpClient.get<any>(this.apiUrl + 'master/GetBranchListByUserName', {
      params: { userName }
    }).pipe(catchError(this.errorHandle));
  }

  saveAsset(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateAssetMaster', body);
  }

  getAssets() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetMasters'
    ).pipe(catchError(this.errorHandle));
  }

  getAssetByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetInventoryAssetByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getSuppliers(category?: string) {
    const params = category
      ? { params: new HttpParams().set('category', category) }
      : {};
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetSuppliers', params)
      .pipe(catchError(this.errorHandle));
  }

  getSupplierCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetSupplierCode'
    ).pipe(catchError(this.errorHandle));
  }

  saveSupplier(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateSupplier', body);
  }

  getSupplierByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetSupplierByID', params
    ).pipe(catchError(this.errorHandle));
  }

  GetPaytoByCategory(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPaytoByCategory', params
    ).pipe(catchError(this.errorHandle));
  }

  getRecipientMaster() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetRecipientMaster'
    ).pipe(catchError(this.errorHandle));
  }

  getRecipientCode() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetRecipientCode'
    ).pipe(catchError(this.errorHandle));
  }

  saveRecipient(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateRecipient', body);
  }

  getRecipients() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetRecipients'
    ).pipe(catchError(this.errorHandle));
  }

  getRecipientByID(id: string) {
    const params = { params: new HttpParams({ fromString: "?ID=" + id }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetRecipientByID', params
    ).pipe(catchError(this.errorHandle));
  }

  getUtilityMaster(invCat: string, supplierCat: string, userId: string): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?invCat=" + invCat + "&supplierCat=" + supplierCat + "&userID=" + userId }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUtilityMasterList', params
    ).pipe(catchError(this.errorHandle));
  }

  GetUtilityDetailsByID(ID: any): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?ID=" + ID }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUtilityDetailsByID', params
    ).pipe(catchError(this.errorHandle));
  }

  GetUtilitySearchList(branch: string, supplier: string, Itype: string) {
    const params = { params: new HttpParams({ fromString: "?branch=" + branch + "&supplier=" + supplier + "&Itype=" + Itype }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUtilitySearchList', params
    ).pipe(catchError(this.errorHandle));
  }


  saveUtility(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateUtility', body);
  }

  GetUtilityDetailListByInvoiceId(InvoiceID: string) {
    const params = { params: new HttpParams({ fromString: "?InvoiceID=" + InvoiceID }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetUtilityDetailListByInvoiceId', params
    ).pipe(catchError(this.errorHandle));
  }


  DeleteUtilityDetailById(id: string): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?Id=" + id }) };
    return this.httpClient.post(this.apiUrl + 'Inventory/DeleteUtilityDetailById', params
    ).pipe(catchError(this.errorHandle));
  }

  GetItemByCategoryId(categoryId: string) {
    const params = { params: new HttpParams({ fromString: "?categoryId=" + categoryId }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetItemByCategoryId', params
    ).pipe(catchError(this.errorHandle));
  }

  GetMaterialMasterList(userId: string): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?userID=" + userId }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaterialMasterList', params
    ).pipe(catchError(this.errorHandle));
  }

  GetMaterialInvoiceByBranch(branchID: string): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?branchID=" + branchID }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaterialInvoiceByBranch', params
    ).pipe(catchError(this.errorHandle));
  }

  GetMaterialInvoiceItemByCategoryID(ID: string): Observable<any> {
    const params = { params: new HttpParams({ fromString: "?ID=" + ID }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaterialInvoiceItemByCategoryID', params
    ).pipe(catchError(this.errorHandle));
  }

  saveMaterial(body: any) {
    return this.httpClient.post(this.apiUrl + 'Inventory/SaveAndUpdateMaterial', body);
  }

  GetMaterialSearchList(branch: string) {
    const params = { params: new HttpParams({ fromString: "?branch=" + branch }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaterialSearchList', params
    ).pipe(catchError(this.errorHandle));
  }

  GetMaterialDetailListByInvoiceId(InvoiceID: string) {
    const params = { params: new HttpParams({ fromString: "?InvoiceID=" + InvoiceID }) };
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaterialDetailListByInvoiceId', params
    ).pipe(catchError(this.errorHandle));
  }
  GetReportHQMaster() {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetReportHQMaster'
    ).pipe(catchError(this.errorHandle));
  }

  // deleteItem(id: number, currentUser: string): Observable<any> {
  //   return this.httpClient.post<any>(`${this.apiUrl}Inventory/DeleteItem/${id}?currentUser=${currentUser}`);
  // }

  // deleteAssetType(id: number): Observable<any> {
  //   return this.httpClient.post(`${this.apiUrl}Inventory/DeleteAsset/${id}`);
  // }

  // deleteRecipientType(id: number): Observable<any> {
  //   return this.httpClient.post(`${this.apiUrl}Inventory/DeleteRecipient/${id}`);
  // }

  // deleteCategory(id: number, currentUser: string): Observable<any> {
  //   return this.httpClient.post<any>(`${this.apiUrl}Inventory/DeleteCategory/${id}?currentUser=${currentUser}`);
  // }

  // deleteSupplierType(id: number): Observable<any> {
  //   return this.httpClient.post(`${this.apiUrl}Inventory/DeleteSupplier/${id}`);
  // }

  deleteItem(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteItem/${id}?currentUser=${encodeURIComponent(currentUser)}`,
      null
    );
  }

  deleteAssetType(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteAsset/${id}`,
      null
    );
  }

  deleteRecipientType(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteRecipient/${id}`,
      null
    );
  }

  deleteCategory(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteCategory/${id}?currentUser=${encodeURIComponent(currentUser)}`,
      null
    );
  }

  deleteSupplierType(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteSupplier/${id}`,
      null
    );
  }

  isCreditorInvoice(invoiceID: number): Observable<boolean> {
    return this.httpClient.get<boolean>(`${this.apiUrl}Inventory/CreditorInvoice_CheckOnBranchPayments/${invoiceID}`);
  }

  deleteExpensesById(id: number, currentUser: string): Observable<boolean> {
    const params = new HttpParams()
      .set('currentUser', currentUser);

    return this.httpClient.get<boolean>(
      `${this.apiUrl}Inventory/DeleteExpensesByID/${id}`,
      { params }
    );
  }

  getUtilitySearchList(payload: any) {
    return this.httpClient.get<any[]>(
      `${this.apiUrl}Inventory/GetUtilityBillSearchList`,
      {
        params: {
          branch: payload.branch,
          supplier: payload.supplier,
          invoiceType: 'U',
          invoiceNo: payload.invoiceNo || '',
          invoiceDate: payload.invoiceDate || '',
          paymentDate: payload.paymentDate || ''
        }
      }
    );
  }

  // ── Part 2: Opening Stock ──────────────────────────────────────────────────

  getOpeningStockList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetOpeningStockList')
      .pipe(catchError(this.errorHandle));
  }

  getOpeningStockByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetOpeningStockByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getOpeningStockCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetOpeningStockCode')
      .pipe(catchError(this.errorHandle));
  }

  saveOpeningStock(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveOpeningStock', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteOpeningStock(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteOpeningStock/${id}?currentUser=${encodeURIComponent(currentUser)}`,
      null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 2: Stock Ledger ───────────────────────────────────────────────────

  getStockLedger(warehouseID: number, itemID: number | null, dateFrom: string,
                 dateTo: string, transactionType: string): Observable<any[]> {
    let params = new HttpParams()
      .set('warehouseID', warehouseID)
      .set('dateFrom', dateFrom)
      .set('dateTo', dateTo)
      .set('transactionType', transactionType || 'ALL');
    if (itemID && itemID > 0) {
      params = params.set('itemID', itemID);
    }
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockLedger', { params })
      .pipe(catchError(this.errorHandle));
  }

  getCurrentStock(warehouseID: number, itemID: number): Observable<any> {
    const params = new HttpParams()
      .set('warehouseID', warehouseID)
      .set('itemID', itemID);
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetCurrentStock', { params })
      .pipe(catchError(this.errorHandle));
  }

  getItemsForStock(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetItemsForStock')
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 3: Purchase Requisition ──────────────────────────────────────────

  getPRList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetPRList')
      .pipe(catchError(this.errorHandle));
  }

  getPRByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPRByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getPRCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPRCode')
      .pipe(catchError(this.errorHandle));
  }

  savePR(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SavePR', body)
      .pipe(catchError(this.errorHandle));
  }

  submitPR(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/SubmitPR/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  approvePR(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/ApprovePR/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  rejectPR(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/RejectPR/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  deletePR(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeletePR/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  /** Returns the processing task for a single PR (claim state). Returns 404 if not found. */
  getPRProcessingTaskByPR(prid: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPRProcessingTaskByPR',
      { params: new HttpParams().set('prid', prid) })
      .pipe(catchError(this.errorHandle));
  }

  /** Atomically claims an unclaimed PR processing task for the current user. */
  claimPR(prid: number): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/ClaimPR/${prid}`, null)
      .pipe(catchError(this.errorHandle));
  }

  getApprovedPRsForPO(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetApprovedPRsForPO')
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 3: Purchase Order ─────────────────────────────────────────────────

  getPOList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetPOList')
      .pipe(catchError(this.errorHandle));
  }

  getPOByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPOByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  /** Returns the existing active PO for a given PRID, or null if none exists. */
  getPOByPRID(prID: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPOByPRID',
      { params: new HttpParams().set('prID', prID) })
      .pipe(catchError(this.errorHandle));
  }

  getPOCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPOCode')
      .pipe(catchError(this.errorHandle));
  }

  savePO(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SavePO', body)
      .pipe(catchError(this.errorHandle));
  }

  /**
   * @deprecated Legacy PO approval — kept for backward compatibility only.
   * The current Part 7 approval workflow uses approvePOWithHistory() instead,
   * which calls /api/Approval/ApprovePOWithHistory and records full history +
   * notifications. Do NOT use this method in new code.
   */
  approvePO(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/ApprovePO/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  cancelPO(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/CancelPO/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Returns POEditabilityDto: { canEdit, isPRLinked, linkedPRNumber, status, blockReason }
   * Called on PO form load to configure field locks and edit-mode.
   */
  getPOEditability(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPOEditability',
      { params: new HttpParams().set('id', id) })
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 3: GRN ────────────────────────────────────────────────────────────

  getGRNList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetGRNList')
      .pipe(catchError(this.errorHandle));
  }

  getGRNByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetGRNByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  /** Returns the first active GRN for a given PO ID, or null if none posted. */
  getGRNByPOID(poID: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetGRNByPOID',
      { params: new HttpParams().set('poID', poID) })
      .pipe(catchError(this.errorHandle));
  }

  getGRNCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetGRNCode')
      .pipe(catchError(this.errorHandle));
  }

  getApprovedPOsForGRN(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetApprovedPOsForGRN')
      .pipe(catchError(this.errorHandle));
  }

  getPOLinesForGRN(poID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetPOLinesForGRN',
      { params: new HttpParams().set('POID', poID) })
      .pipe(catchError(this.errorHandle));
  }

  saveGRN(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveGRN', body)
      .pipe(catchError(this.errorHandle));
  }

  /** Checks if a GRN has downstream dependencies (payment / purchase return).
   *  Returns { canDelete: boolean, blockingMessage: string | null } */
  checkGRNDependency(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/CheckGRNDependency',
      { params: new HttpParams().set('id', id) })
      .pipe(catchError(this.errorHandle));
  }

  deleteGRN(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteGRN/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 3: Purchase Return ────────────────────────────────────────────────

  getPurchaseReturnList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetPurchaseReturnList')
      .pipe(catchError(this.errorHandle));
  }

  getPurchaseReturnByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetPurchaseReturnByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getReturnCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetReturnCode')
      .pipe(catchError(this.errorHandle));
  }

  getGRNsForReturn(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetGRNsForReturn')
      .pipe(catchError(this.errorHandle));
  }

  getGRNLinesForReturn(grnID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetGRNLinesForReturn',
      { params: new HttpParams().set('GRNID', grnID) })
      .pipe(catchError(this.errorHandle));
  }

  savePurchaseReturn(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SavePurchaseReturn', body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 4: Stock Issue ────────────────────────────────────────────────────

  getStockIssueList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockIssueList')
      .pipe(catchError(this.errorHandle));
  }

  getStockIssueByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockIssueByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getStockIssueCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockIssueCode')
      .pipe(catchError(this.errorHandle));
  }

  saveStockIssue(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveStockIssue', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteStockIssue(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteStockIssue/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Returns active employees/guards for the warehouse's branch,
   * filtered by issueToType ('Guard' or 'Staff').
   * Used in New Stock Issue to populate the recipient dropdown.
   */
  getEmployeesByWarehouse(warehouseID: number, issueToType: string): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetEmployeesByWarehouse',
      { params: new HttpParams().set('warehouseID', warehouseID).set('issueToType', issueToType) })
      .pipe(catchError(this.errorHandle));
  }

  getEmployeesByBranch(branchCode: string, issueToType: string = ''): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetEmployeesByBranch',
      { params: new HttpParams().set('branchCode', branchCode).set('issueToType', issueToType) })
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Returns Stock Issue documents issued to the given employee
   * that are not yet linked to an active Uniform Loan.
   * Used in Add Employee Uniform Loan "Link from Stock Issue" panel.
   */
  getPendingStockIssuesForEmployee(employeeID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetPendingStockIssuesForEmployee',
      { params: new HttpParams().set('employeeID', employeeID) })
      .pipe(catchError(this.errorHandle));
  }

  getAllPendingStockIssues(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAllPendingStockIssues')
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Links a StockIssueDoc to a Uniform Loan (SalaryAdvance TransType=4).
   * Called AFTER the loan save succeeds.
   */
  linkStockIssueToUniformLoan(stockIssueDocID: number, salaryAdvanceID: number, linkedBy: string): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/LinkStockIssueToUniformLoan',
      { stockIssueDocID, salaryAdvanceID, linkedBy })
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Returns the monthly inventory-issue timesheet for a branch / year / month.
   * Consumed by InventoryTimesheetComponent.
   */
  getInventoryTimesheet(branch: string, year: number, month: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetInventoryTimesheet',
      { params: new HttpParams().set('branch', branch).set('year', year).set('month', month) })
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 4: Stock Issue Return ─────────────────────────────────────────────

  getIssuableDocsForReturn(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetIssuableDocsForReturn')
      .pipe(catchError(this.errorHandle));
  }

  getIssueDetailsForReturn(issueDocID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetIssueDetailsForReturn',
      { params: new HttpParams().set('issueDocID', issueDocID) })
      .pipe(catchError(this.errorHandle));
  }

  getStockIssueReturnList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockIssueReturnList')
      .pipe(catchError(this.errorHandle));
  }

  getStockIssueReturnByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockIssueReturnByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getStockIssueReturnCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockIssueReturnCode')
      .pipe(catchError(this.errorHandle));
  }

  saveStockIssueReturn(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveStockIssueReturn', body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 4: Stock Transfer ─────────────────────────────────────────────────

  getStockTransferList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockTransferList')
      .pipe(catchError(this.errorHandle));
  }

  getStockTransferByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockTransferByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getStockTransferCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockTransferCode')
      .pipe(catchError(this.errorHandle));
  }

  saveStockTransfer(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveStockTransfer', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteStockTransfer(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteStockTransfer/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 4: Stock Adjustment ───────────────────────────────────────────────

  getStockAdjustmentList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockAdjustmentList')
      .pipe(catchError(this.errorHandle));
  }

  getStockAdjustmentByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockAdjustmentByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getStockAdjustmentCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockAdjustmentCode')
      .pipe(catchError(this.errorHandle));
  }

  saveStockAdjustment(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveStockAdjustment', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteStockAdjustment(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteStockAdjustment/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  submitSA(id: number): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/SubmitSA/${id}`, {})
      .pipe(catchError(this.errorHandle));
  }

  approveSA(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/ApproveSA/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  rejectSA(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/RejectSA/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 4: Damage / Loss ──────────────────────────────────────────────────

  getStockDamageLossList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockDamageLossList')
      .pipe(catchError(this.errorHandle));
  }

  getStockDamageLossByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockDamageLossByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getStockDamageLossCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetStockDamageLossCode')
      .pipe(catchError(this.errorHandle));
  }

  saveStockDamageLoss(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveStockDamageLoss', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteStockDamageLoss(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteStockDamageLoss/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 5: Employee / Guard Item Holding ──────────────────────────────────

  getEmployeeHoldingSummaryList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetEmployeeHoldingSummaryList')
      .pipe(catchError(this.errorHandle));
  }

  getEmployeeHoldingDetail(employeeID: number, issueToType: string = ''): Observable<any[]> {
    let params = new HttpParams().set('employeeID', employeeID);
    if (issueToType) params = params.set('issueToType', issueToType);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetEmployeeHoldingDetail', { params })
      .pipe(catchError(this.errorHandle));
  }

  getAllEmployeeHoldings(issueToType: string = 'ALL'): Observable<any[]> {
    const params = new HttpParams().set('issueToType', issueToType);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAllEmployeeHoldings', { params })
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 5: Stock Balance Summary ─────────────────────────────────────────

  getStockBalanceSummary(warehouseID: number = 0, itemID: number = 0): Observable<any[]> {
    let params = new HttpParams();
    if (warehouseID > 0) params = params.set('warehouseID', warehouseID);
    if (itemID > 0) params = params.set('itemID', itemID);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetStockBalanceSummary', { params })
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 5: Employee Issue Register ───────────────────────────────────────

  getEmployeeIssueRegister(dateFrom: string, dateTo: string,
    employeeID: number = 0, issueToType: string = 'ALL'): Observable<any[]> {
    let params = new HttpParams()
      .set('dateFrom', dateFrom)
      .set('dateTo', dateTo)
      .set('issueToType', issueToType);
    if (employeeID > 0) params = params.set('employeeID', employeeID);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetEmployeeIssueRegister', { params })
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Master ───────────────────────────────────────────────────

  getAssetList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetList')
      .pipe(catchError(this.errorHandle));
  }

  getAssetByIDEnriched(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetByIDEnriched',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getAssetCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetRecord(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAsset', body)
      .pipe(catchError(this.errorHandle));
  }

  deleteAssetRecord(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/DeleteAssetRecord/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Allocation ───────────────────────────────────────────────

  getAssetAllocationList(activeOnly: boolean = false): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetAllocationList',
      { params: new HttpParams().set('activeOnly', activeOnly) })
      .pipe(catchError(this.errorHandle));
  }

  getAssetAllocationByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetAllocationByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getActiveAllocationsByAsset(assetID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetActiveAllocationsByAsset',
      { params: new HttpParams().set('assetID', assetID) })
      .pipe(catchError(this.errorHandle));
  }

  getAllocationCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAllocationCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetAllocation(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetAllocation', body)
      .pipe(catchError(this.errorHandle));
  }

  cancelAssetAllocation(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Inventory/CancelAssetAllocation/${id}?currentUser=${encodeURIComponent(currentUser)}`, null)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Transfer ─────────────────────────────────────────────────

  getAssetTransferList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetTransferList')
      .pipe(catchError(this.errorHandle));
  }

  getAssetTransferByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetTransferByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getTransferCodeAsset(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetTransferCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetTransfer(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetTransfer', body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Return ───────────────────────────────────────────────────

  getAssetReturnList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetReturnList')
      .pipe(catchError(this.errorHandle));
  }

  getAssetReturnByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetReturnByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getReturnCodeAsset(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetReturnCodeAsset')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetReturn(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetReturn', body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Maintenance ──────────────────────────────────────────────

  getAssetMaintenanceList(assetID: number = 0): Observable<any[]> {
    let params = new HttpParams();
    if (assetID > 0) params = params.set('assetID', assetID);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetMaintenanceList', { params })
      .pipe(catchError(this.errorHandle));
  }

  getAssetMaintenanceByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetMaintenanceByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getMaintenanceCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetMaintenanceCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetMaintenance(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetMaintenance', body)
      .pipe(catchError(this.errorHandle));
  }

  completeAssetMaintenance(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/CompleteAssetMaintenance/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Damage ───────────────────────────────────────────────────

  getAssetDamageList(assetID: number = 0): Observable<any[]> {
    let params = new HttpParams();
    if (assetID > 0) params = params.set('assetID', assetID);
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetDamageList', { params })
      .pipe(catchError(this.errorHandle));
  }

  getAssetDamageByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetDamageByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getDamageCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetDamageCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetDamage(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetDamage', body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Disposal ─────────────────────────────────────────────────

  getAssetDisposalList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetDisposalList')
      .pipe(catchError(this.errorHandle));
  }

  getAssetDisposalByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetAssetDisposalByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  getDisposalCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Inventory/GetDisposalCode')
      .pipe(catchError(this.errorHandle));
  }

  saveAssetDisposal(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/SaveAssetDisposal', body)
      .pipe(catchError(this.errorHandle));
  }

  submitAD(id: number): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/SubmitAD/${id}`, {})
      .pipe(catchError(this.errorHandle));
  }

  approveAD(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/ApproveAD/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  rejectAD(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Inventory/RejectAD/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 6: Asset Movement History ────────────────────────────────────────

  getAssetMovementHistory(assetID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetMovementHistory',
      { params: new HttpParams().set('assetID', assetID) })
      .pipe(catchError(this.errorHandle));
  }

  // ── Asset Location Tracking ────────────────────────────────────────────────

  updateAssetLocation(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Inventory/UpdateAssetLocation', body)
      .pipe(catchError(this.errorHandle));
  }

  getAssetLocationLog(assetID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetAssetLocationLog',
      { params: new HttpParams().set('assetID', assetID) })
      .pipe(catchError(this.errorHandle));
  }

  getDefectLocationAssets(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Inventory/GetDefectLocationAssets')
      .pipe(catchError(this.errorHandle));
  }

  // ── Part 7: Approval Workflow ─────────────────────────────────────────────

  getPendingApprovals(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Approval/GetPendingApprovals')
      .pipe(catchError(this.errorHandle));
  }

  getApprovalHistory(documentType: string, documentID: number): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Approval/GetApprovalHistory',
      { params: new HttpParams().set('documentType', documentType).set('documentID', documentID) })
      .pipe(catchError(this.errorHandle));
  }

  getAllApprovalHistory(documentType?: string, documentID?: number, dateFrom?: string, dateTo?: string): Observable<any[]> {
    let params = new HttpParams();
    if (documentType) params = params.set('documentType', documentType);
    if (documentID)   params = params.set('documentID', documentID);
    if (dateFrom)     params = params.set('dateFrom', dateFrom);
    if (dateTo)       params = params.set('dateTo', dateTo);
    return this.httpClient.get<any[]>(this.apiUrl + 'Approval/GetAllApprovalHistory', { params })
      .pipe(catchError(this.errorHandle));
  }

  submitPO(id: number, currentUser: string): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Approval/SubmitPO/${id}`,
      { CurrentUser: currentUser })
      .pipe(catchError(this.errorHandle));
  }

  approvePOWithHistory(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Approval/ApprovePOWithHistory/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  rejectPO(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Approval/RejectPO/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  getApprovalConfigurations(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Approval/GetApprovalConfigurations')
      .pipe(catchError(this.errorHandle));
  }

  updateApprovalConfiguration(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Approval/UpdateApprovalConfiguration/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  // ── Approval Routing (User-Wise) ──────────────────────────────────────────

  getApprovalRoutings(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'Approval/GetApprovalRoutings')
      .pipe(catchError(this.errorHandle));
  }

  isCurrentUserApprover(documentType: string, requesterUser: string): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'Approval/IsCurrentUserApprover', {
      params: { documentType, requesterUser }
    }).pipe(catchError(this.errorHandle));
  }

  saveApprovalRouting(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'Approval/SaveApprovalRouting', body)
      .pipe(catchError(this.errorHandle));
  }

  deactivateApprovalRouting(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Approval/DeactivateApprovalRouting/${id}`, {}
    ).pipe(catchError(this.errorHandle));
  }

  // ── Part 9: Branch Stock Request ──────────────────────────────────────────

  /** Returns all active stock requests (list view, no detail lines). */
  getBSRList(): Observable<any[]> {
    return this.httpClient.get<any[]>(this.apiUrl + 'StockRequest/GetBSRList')
      .pipe(catchError(this.errorHandle));
  }

  /** Returns a single stock request with full line items + available stock. */
  getBSRByID(id: number): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'StockRequest/GetBSRByID',
      { params: new HttpParams().set('ID', id) })
      .pipe(catchError(this.errorHandle));
  }

  /** Returns the next auto-generated SR number (e.g. SR0001). */
  getBSRCode(): Observable<any> {
    return this.httpClient.get<any>(this.apiUrl + 'StockRequest/GetBSRCode')
      .pipe(catchError(this.errorHandle));
  }

  /** Creates a new or updates an existing Draft stock request. */
  saveBSR(body: any): Observable<any> {
    return this.httpClient.post<any>(this.apiUrl + 'StockRequest/SaveBSR', body)
      .pipe(catchError(this.errorHandle));
  }

  /** Submits a Draft BSR for approval (Draft → Submitted or auto-Approved). */
  submitBSR(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}StockRequest/SubmitBSR/${id}`, null)
      .pipe(catchError(this.errorHandle));
  }

  /** Approves a Submitted BSR. Caller must be the configured approver. */
  approveBSR(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}StockRequest/ApproveBSR/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  /** Rejects a Submitted BSR. ApprovalRemarks is mandatory. */
  rejectBSR(id: number, body: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}StockRequest/RejectBSR/${id}`, body)
      .pipe(catchError(this.errorHandle));
  }

  /** Soft-deletes a Draft BSR. */
  deleteBSR(id: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}StockRequest/DeleteBSR/${id}`, null)
      .pipe(catchError(this.errorHandle));
  }

  /**
   * Links an Approved BSR to the StockTransfer that was created to fulfil it.
   * Called after saveStockTransfer() returns the new transfer ID.
   */
  linkBSRTransfer(id: number, linkedTransferID: number): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}StockRequest/LinkBSRTransfer/${id}`,
      { LinkedTransferID: linkedTransferID })
      .pipe(catchError(this.errorHandle));
  }

  private errorHandle(error: HttpErrorResponse) {
    let errorMessage: string = '';
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Client error: ${error.error.message}`;
    } else {
      // Extract the most useful message from the server response
      if (error.error && error.error.details) {
        errorMessage = error.error.details;
      } else if (error.error && error.error.message) {
        errorMessage = error.error.message;
      } else if (typeof error.error === 'string') {
        errorMessage = error.error;
      } else {
        errorMessage = `Server error ${error.status}: ${error.statusText}`;
      }
    }
    return throwError(errorMessage);
  }
}
