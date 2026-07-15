import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  error = '';
  loading = false;

  clearError(): void { this.error = ''; }

  async submit(): Promise<void> {
    this.error = '';
    if (!this.email.trim() || !this.password) { this.error = 'Email and password are required.'; return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim())) { this.error = 'Please enter a valid email address.'; return; }
    this.loading = true;
    try {
      await this.auth.login(this.email, this.password);
      await this.router.navigate(['/']);
    } catch (err: any) {
      const body = err?.error;
      this.error = body?.error || 'Login failed. Please try again.';
    }
    this.loading = false;
  }
}
