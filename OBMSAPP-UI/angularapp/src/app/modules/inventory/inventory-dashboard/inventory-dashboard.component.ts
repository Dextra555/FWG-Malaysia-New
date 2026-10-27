import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import {
  ApexNonAxisChartSeries, ApexChart, ApexDataLabels, ApexLegend,
  ApexTooltip, ApexPlotOptions, ApexAxisChartSeries, ApexXAxis,
  ApexYAxis, ApexStroke, ApexFill, ApexGrid
} from 'ng-apexcharts';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { UserAccessModel } from '../../../model/userAccesModel';
import {
  PendingActivityDialogComponent,
  PendingActivityType
} from './pending-activity-dialog/pending-activity-dialog.component';

@Component({
  selector: 'app-inventory-dashboard',
  templateUrl: './inventory-dashboard.component.html',
  styleUrls: ['./inventory-dashboard.component.css']
})
export class InventoryDashboardComponent implements OnInit {

  // ── Auth ─────────────────────────────────────────────────────────────────
  currentUser: string = '';
  userRole: string    = '';
  userAccessModel: UserAccessModel = new UserAccessModel();
  warningMessage: string = '';

  // ── State ─────────────────────────────────────────────────────────────────
  showLoadingSpinner = false;
  lastUpdated: Date | null = null;

  // ── KPI Cards ─────────────────────────────────────────────────────────────
  totalItems       = 0;
  totalStockValue  = 0;
  totalStockQty    = 0;
  pendingTransfers = 0;
  pendingIssues    = 0;
  lowStockItems    = 0;

  // ── Global Summary (right panel) ──────────────────────────────────────────
  globalSummary: any = {};

  // ── Donut Chart ───────────────────────────────────────────────────────────
  donutSeries: ApexNonAxisChartSeries = [];
  donutLabels: string[] = [];
  donutValueLabels: string[] = [];   // RM-formatted labels for legend
  donutTotal = 0;
  donutChart: ApexChart = {
    type: 'donut', height: 280, fontFamily: 'Poppins, sans-serif'
  };
  donutDataLabels: ApexDataLabels = {
    enabled: false
  };
  donutLegend: ApexLegend = {
    show: false   // we render a custom legend to show RM values
  };
  donutTooltip: ApexTooltip = {
    y: { formatter: (v: number) => 'RM ' + this.formatNum(v) }
  };
  donutPlotOptions: ApexPlotOptions = {
    pie: {
      donut: {
        size: '60%',
        labels: {
          show: true,
          total: {
            show: true, label: 'Total', color: '#4D1220',
            formatter: (w: any) => {
              const t = w.globals.seriesTotals.reduce((a: number, b: number) => a + b, 0);
              return 'RM ' + this.formatNum(t);
            }
          }
        }
      }
    }
  };
  donutColors = ['#1e88e5','#e53935','#43a047','#fb8c00','#8e24aa',
                 '#00acc1','#6d4c41','#546e7a','#d81b60','#00897b'];

  // ── Bar Chart ─────────────────────────────────────────────────────────────
  barSeries: ApexAxisChartSeries = [
    { name: 'Inward (Qty)', data: [] },
    { name: 'Outward (Qty)', data: [] }
  ];
  barChart: ApexChart = {
    type: 'bar', height: 280, fontFamily: 'Poppins, sans-serif',
    toolbar: { show: false }
  };
  barDataLabels: ApexDataLabels = { enabled: false };
  barXAxis: ApexXAxis = {
    categories: [],
    labels: { style: { fontSize: '11px', fontFamily: 'Poppins, sans-serif' } }
  };
  barYAxis: ApexYAxis = {
    labels: {
      formatter: (v: number) => v % 1 === 0 ? v.toString() : v.toFixed(1)
    }
  };
  barStroke: ApexStroke = { show: true, width: 2, colors: ['transparent'] };
  barFill: ApexFill = { opacity: 1 };
  barColors = ['#43a047', '#e53935'];
  barGrid: ApexGrid = { row: { colors: ['#f9f9f9', 'transparent'] } };

  // ── Low Stock Alerts ──────────────────────────────────────────────────────
  lowStockAlerts: any[] = [];

  // ── Pending Activities ────────────────────────────────────────────────────
  pendingActivities: any = {};

  // ── Recent Transactions ───────────────────────────────────────────────────
  recentTransactions: any[] = [];

  // ── Stock Qty by Warehouse ────────────────────────────────────────────────
  itemWarehouseRaw: any[] = [];
  whQtyHeaders: string[] = [];    // unique warehouse names
  whQtyRows: any[]       = [];    // pivoted rows: { ItemName, [wh]: qty, Total }

  // ── Quick Access links ────────────────────────────────────────────────────
  quickLinks = [
    { label: 'Item Master',          icon: 'inventory_2',     route: '/inventory/item-master',           color: '#4D1220' },
    { label: 'Opening Stock',        icon: 'open_in_browser', route: '/inventory/opening-stock',         color: '#1976d2' },
    { label: 'Purchase Entry (PO)',  icon: 'shopping_cart',   route: '/inventory/purchase-order',        color: '#388e3c' },
    { label: 'GRN (Stock In)',       icon: 'move_to_inbox',   route: '/inventory/goods-receipt',         color: '#f57c00' },
    { label: 'Stock Transfer',       icon: 'swap_horiz',      route: '/inventory/stock-transfer',        color: '#7b1fa2' },
    { label: 'Stock Issue (Out)',     icon: 'outbox',          route: '/inventory/stock-issue',           color: '#c62828' },
    { label: 'Stock Adjustment',     icon: 'tune',            route: '/inventory/stock-adjustment',      color: '#0277bd' },
    { label: 'Stock Reports',        icon: 'assessment',      route: '/inventory/stock-balance-summary', color: '#00796b' }
  ];

  constructor(
    private _inventoryService: InventoryService,
    private _masterService: MastermoduleService,
    private _dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') ?? '';
    const rawRole    = sessionStorage.getItem('userrole');
    this.userRole    = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    this.checkAccessAndLoad();
  }

  checkAccessAndLoad(): void {
    if (this.currentUser === 'superadmin' || this.userRole === 'superadmin') {
      this.userAccessModel.readAccess = true;
      this.loadDashboard();
      return;
    }
    this._masterService.getUserAccessRights(this.currentUser, 'Inventory Dashboard').subscribe(
      (data: any) => {
        if (data?.Read) {
          this.userAccessModel.readAccess = true;
          this.loadDashboard();
        } else {
          this.warningMessage =
            `Dear <b>${this.currentUser}</b>, you do not have permission to view the Inventory Dashboard. ` +
            `Please contact your administrator.`;
        }
      },
      () => { this.warningMessage = 'Unable to verify permissions. Please refresh and try again.'; }
    );
  }

  loadDashboard(): void {
    this.showLoadingSpinner = true;
    this._inventoryService.getInventoryDashboard().subscribe(
      (data: any) => {
        this.showLoadingSpinner = false;
        this.lastUpdated = new Date();

        // KPI
        this.totalItems       = data.TotalItems       ?? 0;
        this.totalStockValue  = data.TotalStockValue  ?? 0;
        this.totalStockQty    = data.TotalStockQty    ?? 0;
        this.pendingTransfers = data.PendingTransfers ?? 0;
        this.pendingIssues    = data.PendingIssues    ?? 0;
        this.lowStockItems    = data.LowStockItems    ?? 0;

        // Global summary
        this.globalSummary = data.GlobalSummary ?? {};

        // Donut
        const wh: any[] = data.WarehouseStocks ?? [];
        this.donutSeries      = wh.map(w => +(w.StockValue ?? 0));
        this.donutLabels      = wh.map(w => w.WarehouseName ?? '');
        this.donutValueLabels = wh.map(w => 'RM ' + this.formatNum(+(w.StockValue ?? 0)));
        this.donutTotal       = this.donutSeries.reduce((a, b) => a + b, 0);

        // Bar
        const mov: any[] = data.MonthlyMovements ?? [];
        this.barXAxis  = { ...this.barXAxis, categories: mov.map(m => m.DateLabel ?? '') };
        this.barSeries = [
          { name: 'Inward (Qty)',  data: mov.map(m => +(m.InwardQty  ?? 0)) },
          { name: 'Outward (Qty)', data: mov.map(m => +(m.OutwardQty ?? 0)) }
        ];

        // Low stock
        this.lowStockAlerts = data.LowStockAlerts ?? [];

        // Pending activities
        this.pendingActivities = data.PendingActivities ?? {};

        // Recent transactions
        this.recentTransactions = data.RecentTransactions ?? [];

        // Item/warehouse pivot
        this.buildWhQtyPivot(data.ItemWarehouseQty ?? []);
      },
      () => {
        this.showLoadingSpinner = false;
        this.warningMessage = 'Unable to load dashboard data. Please try again.';
      }
    );
  }

  // ── Pending Activities dialog ─────────────────────────────────────────────

  openPendingActivity(type: PendingActivityType, title: string, count: number): void {
    this._dialog.open(PendingActivityDialogComponent, {
      width: '820px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      panelClass: 'inv-pending-dialog-panel',
      data: { activityType: type, title, count }
    });
  }

  // ── Pivot RS8 into cross-tab rows ──────────────────────────────────────────
  buildWhQtyPivot(raw: any[]): void {
    if (!raw.length) { this.whQtyHeaders = []; this.whQtyRows = []; return; }

    // Collect unique warehouse names (preserve order of first appearance)
    const whSet = new Set<string>();
    raw.forEach(r => whSet.add(r.WarehouseName));
    this.whQtyHeaders = Array.from(whSet);

    // Group by ItemName
    const itemMap = new Map<string, any>();
    raw.forEach(r => {
      if (!itemMap.has(r.ItemName)) {
        itemMap.set(r.ItemName, { ItemName: r.ItemName, TotalQty: r.TotalQty });
      }
      itemMap.get(r.ItemName)![r.WarehouseName] = +(r.Qty ?? 0);
    });
    this.whQtyRows = Array.from(itemMap.values());
  }

  // ── Formatting ─────────────────────────────────────────────────────────────
  formatNum(v: number): string {
    if (!v) return '0';
    return v.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
  }

  formatRM(v: number): string {
    if (!v) return '0.00';
    return v.toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatQty(v: number): string {
    if (v == null) return '0';
    return v % 1 === 0 ? this.formatNum(v) :
      v.toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatDate(d: any): string {
    if (!d) return '';
    const dt = new Date(d);
    return dt.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' })
      .replace(/\//g, '-');
  }

  /** Returns true when a transaction type is an inward movement */
  isInward(code: string): boolean {
    return ['OPENING','STOCK_IN','ISSUE_RETURN','TRANSFER_IN','ADJ_PLUS'].includes(code);
  }

  get currentMonthLabel(): string {
    return new Date().toLocaleString('en-GB', { month: 'long', year: 'numeric' });
  }
}
