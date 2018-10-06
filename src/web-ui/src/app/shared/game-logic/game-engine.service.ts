import { Injectable } from '@angular/core';
import { Game } from 'app/shared/models';
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

    public moveCardToColumn(value: number, colIndex: number): void {
        let game = this.game$.value;

        let card: number;

        game.sourceStacks.update(
            game.sourceStacks.findIndex(o => o.filter(p => p === value).size > 0),
            sourceStack => {
                card = sourceStack.last();
                return sourceStack.pop();
            });

        game.columns.update(colIndex, column => column.push(card));

        this.game$.next(game);
    }

    private pickNextSource(game: Game): Game {
        const numbers = game.sourceStacks
            .filter(o => o.size > 0)
            .map(o => o.get(o.size - 1));

        const nextSourceValue =
            numbers.size > 0
                ? numbers.get(Math.floor(Math.random() * (numbers.size + 1)))
                : null;

        return <Game>game.set('nextSourceValue', nextSourceValue);
    }
}
