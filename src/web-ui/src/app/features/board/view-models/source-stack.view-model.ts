import * as Immutable from 'immutable';

import { CardViewModel } from 'app/features/board/view-models';

const defaultSourceStack = Immutable.Record({
    cards: Immutable.List<CardViewModel>([]),
    isNext: false
});

export class SourceStackViewModel extends defaultSourceStack {
    cards: Immutable.List<CardViewModel>;
    isNext: boolean;
}
