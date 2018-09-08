import { Card, CardCollection } from 'app/shared/models';

export class Column implements CardCollection
{
    public readonly cards: Card[] = [];
}
