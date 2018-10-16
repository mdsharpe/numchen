import { Injectable } from '@angular/core';
import { Game } from 'app/shared/models';
import { flow, range } from 'lodash';
import { produce } from 'immer';

@Injectable({
    providedIn: 'root'
})
export class GameFactoryService {
    public create(): Game {
        return flow([
            this.populateSourceStacks,
            this.populateColumns,
            this.populateGoalStacks
        ])({});
    }

    private populateSourceStacks(game: Game) {
        return produce(
            game,
            draft => {
                draft.sourceStacks = [];

                range(0, 16)
                    .forEach(i => draft.sourceStacks.push(Array(6).fill(i + 1)));
            });
    }

    private populateColumns(game: Game): Game {
        return produce(game, draft => 
            { 
                draft.columns = [];

                range(0, 6)
                    .forEach(i => draft.columns.push([]));
            });
    }

    private populateGoalStacks(game: Game): Game {
        return produce(game, draft => 
            { 
                draft.goalStacks = [];
                
                range(0, 6)
                    .forEach(i => draft.goalStacks.push([]));
            });
    }
}
