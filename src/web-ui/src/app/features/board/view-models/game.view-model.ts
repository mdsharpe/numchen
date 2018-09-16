import * as Immutable from 'immutable';

import { SourceStackViewModel } from 'app/features/board/view-models/source-stack.view-model';

const defaultGame = Immutable.Record({
    sourceStacks: Immutable.List<SourceStackViewModel>([])
});

export class GameViewModel extends defaultGame {
    sourceStacks: Immutable.List<SourceStackViewModel>;
}
