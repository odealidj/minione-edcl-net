import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { User } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.html',
  styleUrl: './user-management.css'
})
export class UserManagementComponent implements OnInit {
  adminService = inject(AdminService);
  
  users: User[] = [];
  meta: PaginationMeta | null = null;
  isLoading = false;
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;

  roles = ['ADMIN', 'USER', 'DRIVER'];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.adminService.getUsers(this.searchQuery, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.users = res.data;
          this.meta = res.pagination;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load users', err);
        this.isLoading = false;
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

  updateRole(user: User, newRole: string): void {
    this.adminService.updateUserRole(user.id, newRole).subscribe({
      next: (res) => {
        user.role = newRole;
      },
      error: (err) => {
        console.error('Failed to update role', err);
        this.loadUsers(); 
      }
    });
  }
}
