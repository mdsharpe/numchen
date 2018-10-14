import * as Immutable from 'immutable';

import { CardViewModel } from 'app/features/board/view-models';

const defaultColumn = Immutable.Record({
    cards: Immutable.List<CardViewModel>([]),
    canPush: false,
    canPop: false
});

export class ColumnViewModel extends defaultColumn {
    cards: Immutable.List<CardViewModel>;
    canPush: boolean;
    canPop: boolean;
}
