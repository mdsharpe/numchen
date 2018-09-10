import { Injectable } from '@angular/core';
import { Game } from 'app/shared/models';

@Injectable({
    providedIn: 'root'
})
export class GameEngineService {
    constructor() { }

    public init(game: Game): Game {
        if (typeof game.nextSourceValue !== 'number') {
            game = this.pickNextSource(game);
        }

        return game;
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
