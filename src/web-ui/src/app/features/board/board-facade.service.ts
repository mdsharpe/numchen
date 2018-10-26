import { Injectable } from '@angular/core';
import { Store } from '@ngxs/store';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { some, last, sumBy } from 'lodash';

import { BoardStateModel } from './board.state';
import * as actions from './board.actions';
import {
    GameViewModel,
    SourceStackViewModel,
    ColumnViewModel,
    GoalStackViewModel
} from 'app/features/board/view-models';

@Injectable({
    providedIn: 'root'
})
export class BoardFacadeService {
    constructor(
        private readonly _store: Store
    ) {
        this.board$ = this._store.select<BoardStateModel>(state => state.board)
            .pipe(
                map(this.mapStateToViewModel)
            );
    }

    public readonly board$: Observable<GameViewModel | null>;

    public moveNextToColumn(columnIndex: number): void {
        this._store.dispatch(new actions.MoveNextToColumn(columnIndex));
    }

    public moveLastToGoal(columnIndex: number): void {
        this._store.dispatch(new actions.MoveLastToGoal(columnIndex));
    }

    private mapStateToViewModel(board: BoardStateModel): GameViewModel | null {
        if (!board) {
            return null;
        }

        return {
            sourceStacks: board.sourceStacks.map<SourceStackViewModel>(
                sourceStack => ({
                    cards: sourceStack.map(n => ({ value: n })),
                    isNext: some(sourceStack, n => n === board.nextSourceValue)
                })),
            columns: board.columns.map<ColumnViewModel>(
                column => ({
                    cards: column.map(o => ({ value: o })),
                    canPush: some(board.sourceStacks, o => o.length > 0),
                    canPop: column.length > 0 && some(
                        board.goalStacks,
                        o => (last(o) || 0) === <number>last(column) - 1)
                })),
            goalStacks: board.goalStacks.map<GoalStackViewModel>(
                goalStack => ({
                    cards: goalStack.map(o => ({ value: o }))
                })),
            score: sumBy(board.goalStacks, o => o.length)
        };
    }
}
