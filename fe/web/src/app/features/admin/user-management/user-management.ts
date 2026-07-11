import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { User } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './user-management.html',
  styleUrl: './user-management.css'
})
export class UserManagementComponent implements OnInit {
  adminService = inject(AdminService);
  
  users = signal<User[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;

  roles = ['ADMIN', 'USER', 'DRIVER'];

  updatingRoleId = signal<number | null>(null);
  toastMessage = signal<{text: string, type: 'success'|'error'} | null>(null);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.adminService.getUsers(this.searchQuery, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.users.set(res.data);
          this.meta.set(res.pagination);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load users', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.currentPage = 1;
    this.loadUsers();
  }

  changePage(page: number): void {
    this.currentPage = page;
    this.loadUsers();
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadUsers();
  }

  updateRole(user: User, newRole: string): void {
    this.updatingRoleId.set(user.id);
    this.adminService.updateUserRole(user.id, newRole).subscribe({
      next: (res) => {
        user.roleCode = newRole;
        this.updatingRoleId.set(null);
        this.showToast(`Role for ${user.name} updated to ${newRole}`, 'success');
      },
      error: (err) => {
        console.error('Failed to update role', err);
        this.updatingRoleId.set(null);
        this.showToast(`Failed to update role for ${user.name}`, 'error');
        // Reload users to revert the dropdown state to the original value
        this.loadUsers(); 
      }
    });
  }

  showToast(text: string, type: 'success'|'error'): void {
    this.toastMessage.set({ text, type });
    setTimeout(() => {
      this.toastMessage.set(null);
    }, 3000);
  }
}
