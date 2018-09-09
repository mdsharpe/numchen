import { Injectable } from '@angular/core';
import { Game } from 'app/shared/models';

@Injectable({
    providedIn: 'root'
})
export class GameEngineService {
    constructor() { }

    public init(game: Game): void {
        // // if (typeof game.nextSourceNumber !== 'number') {
        // //     this.pickNextSource(game);
        // // }
    }

    // // private pickNextSource(game: Game): void {
    // //     const numbers = game.sourceStacks
    // //         .filter(o => o.cards.length > 0)
    // //         .map(o => o.cards[o.cards.length - 1].value);

    // //     if (numbers.length == 0) {
    // //         return;
    // //     }

    // //     const nextIndex = Math.floor(
    // //         Math.random() * (numbers.length + 1));

    // //     game.nextSourceNumber = numbers[nextIndex];
    // // }
}
