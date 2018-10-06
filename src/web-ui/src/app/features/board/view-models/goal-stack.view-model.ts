import * as Immutable from 'immutable';

import { CardViewModel } from 'app/features/board/view-models';

const defaultGoalStack = Immutable.Record({
    cards: Immutable.List<CardViewModel>([])
});

export class GoalStackViewModel extends defaultGoalStack {
    cards: Immutable.List<CardViewModel>;
}
