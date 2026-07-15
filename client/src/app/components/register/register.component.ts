import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  confirm = '';
  error = '';
  backendErrors: string[] = [];
  loading = false;

  clearError(): void { this.error = ''; this.backendErrors = []; }

  async submit(): Promise<void> {
    this.error = '';
    this.backendErrors = [];
    if (!this.email.trim() || !this.password) { this.error = 'Email and password are required.'; return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim())) { this.error = 'Please enter a valid email address.'; return; }
    if (this.password !== this.confirm) { this.error = 'Passwords do not match.'; return; }
    this.loading = true;
    try {
      await this.auth.register(this.email, this.password);
      await this.auth.login(this.email, this.password);
      await this.router.navigate(['/']);
    } catch (err: any) {
      const body = err?.error;
      if (body?.errors?.length) {
        this.backendErrors = body.errors;
      } else {
        this.error = body?.error || 'Registration failed.';
      }
    }
    this.loading = false;
  }
}
