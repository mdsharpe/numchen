import { NgModule } from '@angular/core';
import { BoardComponent } from './board.component';
import { BoardRoutingModule } from './board-routing.module';
import { SharedModule } from 'app/shared/shared.module';

@NgModule({
  imports: [
    BoardRoutingModule,
    SharedModule
  ],
  declarations: [BoardComponent],
  exports: [BoardComponent]
})
export class BoardModule { }
