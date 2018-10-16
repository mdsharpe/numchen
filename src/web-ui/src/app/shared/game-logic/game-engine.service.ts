import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { produce } from 'immer';
import * as _ from 'lodash';

import { Game } from 'app/shared/models';
import { GameFactoryService } from './game-factory.service';

@Injectable({
    providedIn: 'root'
})
export class GameEngineService {
    constructor(
        private readonly _gameFactory: GameFactoryService
    ) { }

    public readonly game$ = new BehaviorSubject<Game>(null);

    public init(): void {
        this.game$.next(
            this.pickNextSource(
                this._gameFactory.create()
            )
        );
    }

    public moveNextToColumn(colIndex: number): void {
        let game = this.game$.value;

        game = produce(
            this.game$.value,
            draft => {
                const card = _.find(draft.sourceStacks, o => _.some(o, p => p === game.nextSourceValue)).pop();
                draft.columns[colIndex].push(card);
            });

        game = this.pickNextSource(game);

        this.game$.next(game);
    }

    public moveLastToGoal(colIndex: number): void {
        let game = this.game$.value;

        const card = _.last(game.columns[colIndex]);
        
        const goalIndex = game.goalStacks.findIndex(o => card > 1 ? _.last(o) === card - 1 : _.isEmpty(o));

        if (goalIndex >= 0) {
            game = produce(
                game,
                draft => {
                    draft.columns[colIndex].pop();
                    draft.goalStacks[goalIndex].push(card);
                });

            this.game$.next(game);
        }
    }

    private pickNextSource(game: Game): Game {
        const numbers = game.sourceStacks
            .filter(o => o.length > 0)
            .map(o => _.last(o));

        const nextSourceValue =
            numbers.length > 0
                ? numbers[Math.floor(Math.random() * numbers.length)]
                : null;

        return produce(
            game,
            draft => {
                draft.nextSourceValue = nextSourceValue;
            });
    }
}
