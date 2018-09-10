import * as Immutable from 'immutable';
import { BoardState } from 'app/shared/models';

export type CardStack = Immutable.List<number>;
export type CardStackSet = Immutable.List<CardStack>;

const defaultGame = Immutable.Record({
    sourceStacks: Immutable.List<CardStack>([]),
    columns: Immutable.List<CardStack>([]),
    goalStacks: Immutable.List<CardStack>([]),
    nextSourceValue: null
});

export class Game extends defaultGame {
    sourceStacks: CardStackSet;
    columns: CardStackSet;
    goalStacks: CardStackSet;
    nextSourceValue: number;
}
