import { Injectable } from '@angular/core';
import { Game } from 'app/shared/models';
import { Observable, BehaviorSubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class GameEngineService {
    constructor() { }

    public readonly game$ = new BehaviorSubject<Game>(null);

    public init(game: Game): void {
        if (typeof game.nextSourceValue !== 'number') {
            game = this.pickNextSource(game);
        }

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
