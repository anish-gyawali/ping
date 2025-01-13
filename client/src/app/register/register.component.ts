import { Component, inject } from '@angular/core';
import { AuthServiceService } from '../services/auth-service.service';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MatInputModule} from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  imports: [MatFormFieldModule,MatInputModule,MatButtonModule,FormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  email!: string;
  password!: string;
  fullName!: string;
  profilePicture: string='https://randomuser.me/api/portraits/lego/5.jpg';
  profileImage: File | null = null;

  authService=inject(AuthServiceService);
}
