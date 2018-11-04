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

    public moveNextToGoal(stackIndex:number):void {
        this._store.dispatch(new actions.MoveNextToGoal(stackIndex));
    }

    private mapStateToViewModel(board: BoardStateModel): GameViewModel | null {
        if (!board) {
            return null;
        }

        return {
            sourceStacks: board.sourceStacks.map<SourceStackViewModel>(
                stack => ({
                    cards: stack.map(n => ({ value: n })),
                    isNext: some(stack, n => n === board.nextSourceValue),
                    canMoveToGoal: stack.length > 0 && some(
                        board.goalStacks,
                        o => (last(o) || 0) === last(stack)! - 1)
                })),
            columns: board.columns.map<ColumnViewModel>(
                col => ({
                    cards: col.map(o => ({ value: o })),
                    canPush: some(board.sourceStacks, o => o.length > 0),
                    canMoveToGoal: col.length > 0 && some(
                        board.goalStacks,
                        o => (last(o) || 0) === last(col)! - 1)
                })),
            goalStacks: board.goalStacks.map<GoalStackViewModel>(
                stack => ({
                    cards: stack.map(o => ({ value: o }))
                })),
            score: sumBy(board.goalStacks, o => o.length)
        };
    }
}
