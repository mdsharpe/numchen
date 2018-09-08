import { Injectable } from '@angular/core';
import { Game, SourceStack, Card, Column, GoalStack } from 'app/shared/models';

@Injectable({
    providedIn: 'root'
})
export class GameFactoryService {
    constructor() { }

    public create(): Game {
        const game = new Game();

        for (let i = 0; i < 16; i++) {
            const sourceStack = new SourceStack();
            game.sourceStacks.push(sourceStack);

            for (let j = 0; j < 6; j++) {
                sourceStack.cards.push(new Card(i));
            }
        }

        for (let i = 0; i < 6; i++) {
            game.columns.push(new Column());
            game.goalStacks.push(new GoalStack());
        }

        return game;
    }
}
