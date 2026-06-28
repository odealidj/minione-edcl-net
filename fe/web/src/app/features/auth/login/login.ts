import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  constructor(private fb: FormBuilder, private router: Router) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      const { email, password } = this.loginForm.value;
      
      this.authService.login(email, password)
        .pipe(
          finalize(() => {
            this.isLoading = false;
            this.cdr.detectChanges();
          })
        )
        .subscribe({
          next: () => {
            this.router.navigate(['/dashboard']);
          },
          error: (err) => {
            console.error('Login Error:', err);
            if (err.status === 0) {
              this.errorMessage = 'Gagal terhubung ke server (CORS / Network Error). Pastikan Backend menyala di port 5140.';
            } else {
              this.errorMessage = err.error?.message || err.message || 'Login failed. Please check your credentials.';
            }
          }
        });
    }
  }
}
