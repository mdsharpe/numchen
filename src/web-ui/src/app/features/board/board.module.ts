import { NgModule } from '@angular/core';
import { NgxsModule } from '@ngxs/store';

import { BoardState } from './board.state';
import { BoardComponent } from './board.component';
import { BoardRoutingModule } from './board-routing.module';
import { SharedModule } from 'app/shared/shared.module';
import { ColumnComponent } from './column/column.component';
import { SourceStackComponent } from './source-stack/source-stack.component';
import { GoalStackComponent } from './goal-stack/goal-stack.component';
import { CardComponent } from './card/card.component';
import { ScoreComponent } from './score/score.component';

@NgModule({
  imports: [
    BoardRoutingModule,
    SharedModule,
    NgxsModule.forFeature([BoardState])
  ],
  declarations: [
    BoardComponent,
    ColumnComponent,
    SourceStackComponent,
    GoalStackComponent,
    CardComponent,
    ScoreComponent
  ],
  exports: [BoardComponent]
})
export class BoardModule { }
