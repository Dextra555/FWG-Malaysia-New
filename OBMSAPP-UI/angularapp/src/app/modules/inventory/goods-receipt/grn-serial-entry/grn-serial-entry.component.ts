import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';

@Component({
  selector:    'app-grn-serial-entry',
  templateUrl: './grn-serial-entry.component.html',
  styleUrls:   ['./grn-serial-entry.component.css']
})
export class GrnSerialEntryComponent implements OnInit {
  constructor(private router: Router,
    private titleService: Title) {}

  ngOnInit(): void {
    this.titleService.setTitle('Goods Receipt Note');
    // Serial entry is no longer used â€” redirect back to GRN list
    this.router.navigate(['/inventory/goods-receipt']);
  }
}

