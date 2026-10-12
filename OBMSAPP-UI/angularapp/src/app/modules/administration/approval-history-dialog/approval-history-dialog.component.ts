import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { InventoryService } from '../../../service/inventory.service';

export interface ApprovalHistoryDialogData {
  documentType:   string;
  documentID:     number;
  documentNumber: string;
}

@Component({
  selector: 'app-approval-history-dialog',
  templateUrl: './approval-history-dialog.component.html',
  styleUrls: ['./approval-history-dialog.component.css']
})
export class ApprovalHistoryDialogComponent implements OnInit {
  history: any[]  = [];
  loading         = true;
  errorMessage    = '';
  displayedColumns = ['ActionDate', 'Action', 'ActionBy', 'PreviousStatus', 'NewStatus', 'Comments'];

  constructor(
    public dialogRef: MatDialogRef<ApprovalHistoryDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ApprovalHistoryDialogData,
    private service: InventoryService
  ) {}

  ngOnInit(): void {
    this.service.getApprovalHistory(this.data.documentType, this.data.documentID).subscribe({
      next: (d: any[]) => { this.history = d || []; this.loading = false; },
      error: (err: any) => {
        this.errorMessage = typeof err === 'string' ? err : 'Unable to load approval history.';
        this.loading = false;
      }
    });
  }

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  }

  getActionClass(action: string): string {
    switch (action) {
      case 'Approved':  return 'action-approved';
      case 'Rejected':  return 'action-rejected';
      case 'Submitted': return 'action-submitted';
      case 'Cancelled': return 'action-cancelled';
      default:          return '';
    }
  }

  close(): void { this.dialogRef.close(); }
}
