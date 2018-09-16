import * as Immutable from 'immutable';

const defaultCard = Immutable.Record({
    value: 0
});

export class CardViewModel extends defaultCard {
    value: number;
}
