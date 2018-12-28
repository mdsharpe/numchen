import { NgModule } from '@angular/core';
import { NgxsModule } from '@ngxs/store';

import { BoardState } from './board.state';
import { BoardShellComponent } from './containers/board-shell/board-shell.component';
import { BoardRoutingModule } from './board-routing.module';
import { SharedModule } from 'app/shared/shared.module';
import { ColumnComponent } from './components/column/column.component';
import { SourceStackComponent } from './components/source-stack/source-stack.component';
import { GoalStackComponent } from './components/goal-stack/goal-stack.component';
import { CardComponent } from './components/card/card.component';
import { ScoreComponent } from './components/score/score.component';
import { UndoButtonComponent } from './components/undo-button/undo-button.component';

@NgModule({
  imports: [
    BoardRoutingModule,
    SharedModule,
    NgxsModule.forFeature([BoardState])
  ],
  declarations: [
    BoardShellComponent,
    ColumnComponent,
    SourceStackComponent,
    GoalStackComponent,
    CardComponent,
    ScoreComponent,
    UndoButtonComponent
  ],
  exports: [BoardShellComponent]
})
export class BoardModule { }
