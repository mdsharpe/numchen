import { Injectable } from '@angular/core';
import { Game, CardStack, CardStackSet } from 'app/shared/models';
import { Observable, BehaviorSubject } from 'rxjs';
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

        let card: number;

        game = <Game>game
            .update(
                'sourceStacks',
                (sourceStacks: CardStackSet) => sourceStacks.update(
                    game.sourceStacks.findIndex(o => o.filter(p => p === game.nextSourceValue).size > 0),
                    (sourceStack: CardStack) => {
                        card = sourceStack.last();
                        return sourceStack.pop();
                    })
            )
            .update(
                'columns',
                (columns: CardStackSet) => columns.update(
                    colIndex,
                    (column: CardStack) => column.push(card))
            );

        game = this.pickNextSource(game);

        this.game$.next(game);
    }

    public moveLastToGoal(colIndex: number): void {
        let game = this.game$.value;

        let card = game.columns.get(colIndex).last();

        let goalIndex = game
            .goalStacks
            .findIndex(
                o => card > 1 ? o.last() === card - 1 : o.isEmpty());

        if (goalIndex >= 0) {
            game = <Game>game
                .update(
                    'columns',
                    (columns: CardStackSet) => columns.update(
                        colIndex,
                        (column: CardStack) => column.pop())
                )
                .update(
                    'goalStacks',
                    (goalStacks: CardStackSet) => goalStacks.update(
                        goalIndex,
                        (goalStack: CardStack) => goalStack.push(card))
                );

            this.game$.next(game);
        }
    }

    private pickNextSource(game: Game): Game {
        const numbers = game.sourceStacks
            .filter(o => o.size > 0)
            .map(o => o.get(o.size - 1));

        const nextSourceValue =
            numbers.size > 0
                ? numbers.get(Math.floor(Math.random() * numbers.size))
                : null;

        return <Game>game.set('nextSourceValue', nextSourceValue);
    }
}
