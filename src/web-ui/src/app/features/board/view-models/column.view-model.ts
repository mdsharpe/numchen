import * as Immutable from 'immutable';

import { CardViewModel } from 'app/features/board/view-models';

const defaultColumn = Immutable.Record({
    cards: Immutable.List<CardViewModel>([])
});

export class ColumnViewModel extends defaultColumn {
    cards: Immutable.List<CardViewModel>;
}
