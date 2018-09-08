import { NgModule } from '@angular/core';
import { BoardComponent } from './board.component';
import { BoardRoutingModule } from './board-routing.module';
import { SharedModule } from 'app/shared/shared.module';
import { ColumnComponent } from './column/column.component';
import { SourceStackComponent } from './source-stack/source-stack.component';
import { GoalStackComponent } from './goal-stack/goal-stack.component';
import { CardComponent } from './card/card.component';

@NgModule({
  imports: [
    BoardRoutingModule,
    SharedModule
  ],
  declarations: [
    BoardComponent,
    ColumnComponent,
    SourceStackComponent,
    GoalStackComponent,
    CardComponent
  ],
  exports: [BoardComponent]
})
export class BoardModule { }
