import { CardViewModel } from '.';

export interface SourceStackViewModel {
    cards: CardViewModel[];
    isNext: boolean;
    canMoveToGoal: boolean;
}
