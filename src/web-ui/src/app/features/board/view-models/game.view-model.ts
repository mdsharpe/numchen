import { SourceStackViewModel, ColumnViewModel, GoalStackViewModel } from '.';

export interface GameViewModel {
    sourceStacks: SourceStackViewModel[];
    columns: ColumnViewModel[];
    goalStacks: GoalStackViewModel[];
}
