import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { GameEngineService } from 'app/shared/game-logic';
import { GameViewModel, SourceStackViewModel, CardViewModel } from 'app/features/board/view-models';
import { Game } from 'app/shared/models';
import { ColumnViewModel } from 'app/features/board/view-models/column.view-model';
import { GoalStackViewModel } from 'app/features/board/view-models/goal-stack.view-model';

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

    public game : GameViewModel;

    public ngOnInit(): void {
        this._gameEngine.game$.subscribe(o => this.updateGame(o));
        this._gameEngine.init();
    }

    public columnAddClicked(columnIndex: number): void {
        const sourceStack = this.game.sourceStacks
            .filter(o => o.isNext)
            .first();
        const cardToMove = sourceStack.cards.first();

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

        this.game = <GameViewModel>new GameViewModel()
            .set(
                'sourceStacks',
                game.sourceStacks.map(
                    o => new SourceStackViewModel()
                        .set(
                            'cards',
                            o.map(n => new CardViewModel()
                                .set('value', n)
                            )
                        )
                        .set('isNext', o.filter(p => p === game.nextSourceValue).size > 0)
                )
            )
            .set(
                'columns',
                game.columns.map(
                    o => new ColumnViewModel()
                        .set(
                            'cards',
                            o.map(n => new CardViewModel()
                                .set('value', n)
                            )
                        )
                )
            )
            .set(
                'goalStacks',
                game.goalStacks.map(
                    o => new GoalStackViewModel()
                        .set(
                            'cards',
                            o.map(n => new CardViewModel()
                                .set('value', n))
                        )
                )
            )
        ;
    }
}
