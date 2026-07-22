import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Manifest, ManifestPart, ManifestKanban } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { CargoService } from '../../../core/services/cargo.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { SearchInputComponent } from '../../../shared/components/search-input/search-input.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-manifest',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, SearchInputComponent, CardComponent],
  templateUrl: './manifest.html',
  styleUrl: './manifest.css'
})
export class ManifestComponent implements OnInit {
  items = signal<Manifest[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;
  expandedManifestId = signal<number | null>(null);
  parts = signal<ManifestPart[]>([]);
  partMeta = signal<PaginationMeta | null>(null);
  partIsLoading = signal(false);
  partSearchQuery = '';
  partCurrentPage = 1;

  expandedPartNo = signal<string | null>(null);
  kanbans = signal<ManifestKanban[]>([]);
  kanbanMeta = signal<PaginationMeta | null>(null);
  kanbanIsLoading = signal(false);
  kanbanSearchQuery = '';
  kanbanCurrentPage = 1;

  private service = inject(CargoService);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getManifests(this.searchQuery, undefined, undefined, undefined, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.items.set(res.data);
          this.meta.set(res.pagination);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load data', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.currentPage = 1;
    this.loadData();
  }

  changePage(page: number): void {
    this.currentPage = page;
    this.loadData();
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadData();
  }

  toggleExpand(manifestId: number): void {
    if (this.expandedManifestId() === manifestId) {
      this.expandedManifestId.set(null);
      this.expandedPartNo.set(null);
    } else {
      this.expandedManifestId.set(manifestId);
      this.expandedPartNo.set(null);
      this.partSearchQuery = '';
      this.partCurrentPage = 1;
      this.loadParts(manifestId);
    }
  }

  loadParts(manifestId: number): void {
    this.partIsLoading.set(true);
    this.service.getManifestParts(manifestId, this.partSearchQuery, this.partCurrentPage, 10).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.parts.set(res.data);
          this.partMeta.set(res.pagination);
        }
        this.partIsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load parts', err);
        this.partIsLoading.set(false);
      }
    });
  }

  onPartSearch(query: string, manifestId: number): void {
    this.partSearchQuery = query;
    this.partCurrentPage = 1;
    this.expandedPartNo.set(null);
    this.loadParts(manifestId);
  }

  changePartPage(page: number, manifestId: number): void {
    this.partCurrentPage = page;
    this.expandedPartNo.set(null);
    this.loadParts(manifestId);
  }

  toggleExpandPart(partNo: string, manifestId: number): void {
    if (this.expandedPartNo() === partNo) {
      this.expandedPartNo.set(null);
    } else {
      this.expandedPartNo.set(partNo);
      this.kanbanSearchQuery = '';
      this.kanbanCurrentPage = 1;
      this.loadKanbans(manifestId, partNo);
    }
  }

  loadKanbans(manifestId: number, partNo: string): void {
    this.kanbanIsLoading.set(true);
    this.service.getManifestKanbans(manifestId, partNo, this.kanbanSearchQuery, this.kanbanCurrentPage, 10).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.kanbans.set(res.data);
          this.kanbanMeta.set(res.pagination);
        }
        this.kanbanIsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load kanbans', err);
        this.kanbanIsLoading.set(false);
      }
    });
  }

  onKanbanSearch(query: string, manifestId: number, partNo: string): void {
    this.kanbanSearchQuery = query;
    this.kanbanCurrentPage = 1;
    this.loadKanbans(manifestId, partNo);
  }

  changeKanbanPage(page: number, manifestId: number, partNo: string): void {
    this.kanbanCurrentPage = page;
    this.loadKanbans(manifestId, partNo);
  }
}
