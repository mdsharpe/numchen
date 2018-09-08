import { Card, CardCollection } from 'app/shared/models';

export class GoalStack implements CardCollection {
    public readonly cards: Card[] = [];
}
