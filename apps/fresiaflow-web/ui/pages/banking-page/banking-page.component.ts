import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-banking-page',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule
  ],
  templateUrl: './banking-page.component.html',
  styleUrls: ['./banking-page.component.css']
})
export class BankingPageComponent {
}

