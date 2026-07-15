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
  loading = false;

  async submit(): Promise<void> {
    this.error = '';
    if (this.password !== this.confirm) { this.error = 'Passwords do not match.'; return; }
    this.loading = true;
    try {
      await this.auth.register(this.email, this.password);
      await this.router.navigate(['/']);
    } catch (err: any) {
      const body = err?.error;
      this.error = body?.error || 'Registration failed. Please try again.';
    }
    this.loading = false;
  }
}
