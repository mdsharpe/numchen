import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { GameFactoryService, GameEngineService } from 'app/shared/game-logic';
import { GameViewModel, SourceStackViewModel, CardViewModel } from 'app/features/board/view-models';
import { Game } from 'app/shared/models';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardComponent implements OnInit {
    constructor(
        private readonly _gameFactory: GameFactoryService,
        private readonly _gameEngine: GameEngineService
    ) {
    }

    public game : GameViewModel;

    public ngOnInit(): void {
        this._gameEngine.game$.subscribe(o => this.updateGame(o));

        // HACK: Start new game
        this._gameEngine.init(
            this._gameFactory.create()
        );
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
                                .set('value', n))
                        )
                )
            )
        ;
    }
}
