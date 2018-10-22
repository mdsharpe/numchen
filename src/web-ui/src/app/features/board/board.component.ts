import { Component, OnInit, ChangeDetectionStrategy, OnDestroy } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Store } from '@ngxs/store';
import * as _ from 'lodash';

import {
    GameViewModel,
    SourceStackViewModel,
    ColumnViewModel,
    GoalStackViewModel
} from 'app/features/board/view-models';
import { BoardStateModel } from './board.state';
import { MoveNextToColumn, MoveLastToGoal } from './board.actions';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardComponent implements OnInit, OnDestroy {
    constructor(
        private readonly _store: Store
    ) {
    }

    private readonly unsubscribe$ = new Subject<void>();
    public board$: Observable<GameViewModel | null> | null = null;

    public ngOnInit(): void {
        this.board$ = this._store.select<BoardStateModel>(state => state.board)
            .pipe(
                map(this.mapStateToViewModel),
                takeUntil(this.unsubscribe$)
            );
    }

    public ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    public columnAddClicked(columnIndex: number): void {
        this._store.dispatch(new MoveNextToColumn(columnIndex));
    }

    public columnRemoveClicked(columnIndex: number) {
        this._store.dispatch(new MoveLastToGoal(columnIndex));
    }

    private mapStateToViewModel(board: BoardStateModel): GameViewModel | null {
        if (!board) {
            return null;
        }

        return {
            sourceStacks: board.sourceStacks.map<SourceStackViewModel>(
                sourceStack => ({
                    cards: sourceStack.map(n => ({ value: n })),
                    isNext: _.some(sourceStack, n => n === board.nextSourceValue)
                })),
            columns: board.columns.map<ColumnViewModel>(
                column => ({
                    cards: column.map(o => ({ value: o })),
                    canPush: _.some(board.sourceStacks, o => o.length > 0),
                    canPop: column.length > 0 && _.some(
                        board.goalStacks,
                        o => (_.last(o) || 0) === <number>_.last(column) - 1)
                })),
            goalStacks: board.goalStacks.map<GoalStackViewModel>(
                goalStack => ({
                    cards: goalStack.map(o => ({ value: o }))
                })),
            score: _.sumBy(board.goalStacks, o => o.length)
        };
    }
}
