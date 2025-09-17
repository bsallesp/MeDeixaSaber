import { Component } from '@angular/core';
import {NgOptimizedImage} from '@angular/common';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-top-side',
  imports: [
    NgOptimizedImage,
    RouterLink
  ],
  templateUrl: './top-side.html',
  styleUrl: './top-side.css'
})
export class TopSide {

}
