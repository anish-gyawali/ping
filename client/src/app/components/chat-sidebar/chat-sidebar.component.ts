import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import {MatMenuModule} from '@angular/material/menu'
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-chat-sidebar',
  imports: [MatIconModule, MatMenuModule],
  templateUrl: './chat-sidebar.component.html',
  styleUrl:'./chat-sidebar.component.css',
})
export class ChatSidebarComponent {
  authService= inject(AuthService)
  router=inject(Router)
  
  logout(){
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
