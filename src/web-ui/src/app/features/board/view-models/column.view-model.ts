import { CardViewModel } from ".";

export interface ColumnViewModel {
    cards: CardViewModel[];
    canPush: boolean;
    canPop: boolean;
}
