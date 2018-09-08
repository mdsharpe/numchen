import { Card, CardCollection } from 'app/shared/models';

export class SourceStack implements CardCollection
{
    public readonly cards: Card[] = [];
}
