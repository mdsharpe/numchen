import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import * as _ from 'lodash';

import { GameEngineService } from 'app/shared/game-logic';
import { Game } from 'app/shared/models';
import {
    GameViewModel,
    SourceStackViewModel,
    ColumnViewModel,
    GoalStackViewModel
} from 'app/features/board/view-models';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardComponent implements OnInit {
    constructor(
        private readonly _gameEngine: GameEngineService
    ) {
    }

    public game: GameViewModel = null;

    public ngOnInit(): void {
        this._gameEngine.game$.subscribe(o => this.updateGame(o));
        this._gameEngine.init();
    }

    public columnAddClicked(columnIndex: number): void {
        const sourceStack = _.find(
            this.game.sourceStacks,
            o => o.isNext);
        const cardToMove = _.take(sourceStack.cards);

        if (!cardToMove) {
            throw new Error();
        }

        this._gameEngine.moveNextToColumn(columnIndex);
    }

    public columnRemoveClicked(columnIndex: number) {
        this._gameEngine.moveLastToGoal(columnIndex);
    }

    private updateGame(game: Game): void {
        if (!game) {
            this.game = null;
            return;
        }

        this.game = {
            sourceStacks: game.sourceStacks.map<SourceStackViewModel>(
                sourceStack => {
                    return {
                        cards: sourceStack.map(n => { return { value: n }; }),
                        isNext: _.some(sourceStack, n => n === game.nextSourceValue)
                    };
                }),
            columns: game.columns.map<ColumnViewModel>(
                column => {
                    return {
                        cards: column.map(o => { return { value: o }; }),
                        canPush: _.some(game.sourceStacks, o => o.length > 0),
                        canPop: column.length > 0
                            && _.some(game.goalStacks, o =>
                                _.last(o) === _.last(column) - 1
                                || (o.length === 0 && _.last(column) === 1))
                    };
                }),
            goalStacks: game.goalStacks.map<GoalStackViewModel>(
                goalStack => {
                    return {
                        cards: goalStack.map(o => { return { value: o }; })
                    };
                })
        };
    }
}
