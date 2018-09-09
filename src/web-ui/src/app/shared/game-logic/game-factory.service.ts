import { Injectable } from '@angular/core';
import { Game, CardStackSet } from 'app/shared/models';
import { pipe } from 'rxjs';
import { flow, range } from 'lodash';
import { List } from 'immutable';

@Injectable({
    providedIn: 'root'
})
export class GameFactoryService {
    public create(): Game {
        return flow([
            this.populateSourceStacks,
            this.populateColumns,
            this.populateGoalStacks
        ])(new Game());
    }

    private populateSourceStacks(game: Game): Game {
        range(1, 17)
            .map(o => {
                return {
                    i: o,
                    stack: List<number>(Array(6).fill(o))
                };
            })
            .forEach(o => game = <Game>game.updateIn(
                ['sourceStacks'],
                (stackSet: CardStackSet) => stackSet.set(o.i, o.stack)));

        return game;
    }

    private populateColumns(game: Game): Game {
        range(0, 6)
            .forEach(o => game = <Game>game.updateIn(
                ['columns'],
                (stackSet: CardStackSet) => stackSet.push(List<number>([]))));

        return game;
    }

    private populateGoalStacks(game: Game): Game {
        range(0, 6)
            .forEach(o => game = <Game>game.updateIn(
                ['goalStacks'],
                (stackSet: CardStackSet) => stackSet.push(List<number>([]))));

        return game;
    }
}
