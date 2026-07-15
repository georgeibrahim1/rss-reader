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

  async submit(): Promise<void> {
    this.error = '';
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
