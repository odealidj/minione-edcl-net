import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Manifest, ManifestKanban } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { CargoService } from '../../../core/services/cargo.service';

@Component({
  selector: 'app-manifest',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
    this.service.getManifests(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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

  toggleExpand(manifestId: number): void {
    if (this.expandedManifestId() === manifestId) {
      this.expandedManifestId.set(null);
    } else {
      this.expandedManifestId.set(manifestId);
      this.kanbanSearchQuery = '';
      this.kanbanCurrentPage = 1;
      this.loadKanbans(manifestId);
    }
  }

  loadKanbans(manifestId: number): void {
    this.kanbanIsLoading.set(true);
    this.service.getManifestKanbans(manifestId, this.kanbanSearchQuery, this.kanbanCurrentPage, 10).subscribe({
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

  onKanbanSearch(query: string, manifestId: number): void {
    this.kanbanSearchQuery = query;
    this.kanbanCurrentPage = 1;
    this.loadKanbans(manifestId);
  }

  changeKanbanPage(page: number, manifestId: number): void {
    this.kanbanCurrentPage = page;
    this.loadKanbans(manifestId);
  }
}
