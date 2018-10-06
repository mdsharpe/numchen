import * as Immutable from 'immutable';

import { SourceStackViewModel } from 'app/features/board/view-models/source-stack.view-model';
import { ColumnViewModel } from 'app/features/board/view-models/column.view-model';
import { GoalStackViewModel } from 'app/features/board/view-models/goal-stack.view-model';

const defaultGame = Immutable.Record({
    sourceStacks: Immutable.List<SourceStackViewModel>([]),
    columns: Immutable.List<ColumnViewModel>([]),
    goalStacks: Immutable.List<GoalStackViewModel>([])
});

export class GameViewModel extends defaultGame {
    sourceStacks: Immutable.List<SourceStackViewModel>;
    columns: Immutable.List<ColumnViewModel>;
    goalStacks: Immutable.List<GoalStackViewModel>;
}
